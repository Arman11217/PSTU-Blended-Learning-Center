using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBLC.Web.Models.Entities;
using PBLC.Web.Models.Enums;
using PBLC.Web.Data;

namespace PBLC.Web.Controllers
{
    [Authorize]
    public class LectureController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public LectureController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Lecture/SelectCourse
        public async Task<IActionResult> SelectCourse()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var courses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.EnrolledStudents)
                .Include(c => c.Lectures)
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();

            return View(courses);
        }

        // GET: Lecture/Upload?courseId=1
        public async Task<IActionResult> Upload(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null || course.TeacherId != currentUser.Id)
            {
                return NotFound();
            }

            ViewBag.Course = course;
            return View();
        }

        // POST: Lecture/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int courseId, string title, string? description, IFormFile? file, string? videoUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null || course.TeacherId != currentUser.Id)
            {
                return NotFound();
            }

            if (file == null && string.IsNullOrWhiteSpace(videoUrl))
            {
                ModelState.AddModelError("", "Please upload a file or provide a video URL.");
                ViewBag.Course = course;
                return View();
            }

            string? filePath = null;
            string? fileName = null;

            // Handle file upload
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lectures");
                Directory.CreateDirectory(uploadsFolder);

                fileName = $"{Guid.NewGuid()}_{file.FileName}";
                filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = $"/uploads/lectures/{fileName}";
            }

            var lecture = new Lecture
            {
                Title = title,
                Description = description,
                ContentUrl = filePath,
                VideoUrl = videoUrl,
                CourseId = courseId,
                CreatedAt = DateTime.Now
            };

            _context.Lectures.Add(lecture);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lecture uploaded successfully!";
            return RedirectToAction("MyCourses", "Teacher");
        }

        // GET: Lecture/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var lecture = await _context.Lectures
                .Include(l => l.Course)
                .ThenInclude(c => c.Teacher)
                .Include(l => l.Course)
                .ThenInclude(c => c.EnrolledStudents)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lecture == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user has access (teacher of the course or enrolled student)
            var hasAccess = lecture.Course.TeacherId == currentUser.Id ||
                           lecture.Course.EnrolledStudents?.Any(s => s.Id == currentUser.Id) == true;

            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(lecture.ContentUrl))
            {
                TempData["Error"] = "No file available for this lecture.";
                return RedirectToAction("Details", "Course", new { id = lecture.CourseId });
            }

            var filePath = Path.Combine(_environment.WebRootPath, lecture.ContentUrl.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Details", "Course", new { id = lecture.CourseId });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // POST: Lecture/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var lecture = await _context.Lectures
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lecture == null)
            {
                return NotFound();
            }

            if (lecture.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Delete file if exists
            if (!string.IsNullOrWhiteSpace(lecture.ContentUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, lecture.ContentUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lecture deleted successfully!";
            return RedirectToAction("Details", "Course", new { id = lecture.CourseId });
        }
    }
}
