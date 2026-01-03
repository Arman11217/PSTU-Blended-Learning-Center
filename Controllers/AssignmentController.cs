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
    public class AssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AssignmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Assignment/SelectCourse
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
                .Include(c => c.Assignments)
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();

            ViewBag.ActionType = "create";
            return View(courses);
        }

        // GET: Assignment/SelectCourseForReview
        public async Task<IActionResult> SelectCourseForReview()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var courses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.EnrolledStudents)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Submissions)
                .Where(c => c.TeacherId == currentUser.Id)
                .ToListAsync();

            ViewBag.ActionType = "review";
            return View("SelectCourse", courses);
        }

        // GET: Assignment/Create?courseId=1
        public async Task<IActionResult> Create(int courseId)
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

        // POST: Assignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title, string description, DateTime dueDate, int totalMarks)
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

            if (dueDate < DateTime.Now)
            {
                ModelState.AddModelError("", "Due date must be in the future.");
                ViewBag.Course = course;
                return View();
            }

            var assignment = new Assignment
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                MaxMarks = totalMarks,
                CourseId = courseId,
                TeacherId = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment created successfully!";
            return RedirectToAction("MyCourses", "Teacher");
        }

        // GET: Assignment/Submit/5
        public async Task<IActionResult> Submit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Student)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .ThenInclude(c => c.EnrolledStudents)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            // Check if student is enrolled in the course
            if (assignment.Course.EnrolledStudents?.All(s => s.Id != currentUser.Id) != false)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if already submitted
            var existingSubmission = assignment.Submissions?.FirstOrDefault(s => s.StudentId == currentUser.Id);
            if (existingSubmission != null)
            {
                TempData["Error"] = "You have already submitted this assignment.";
                return RedirectToAction("Details", "Course", new { id = assignment.CourseId });
            }

            ViewBag.Assignment = assignment;
            return View();
        }

        // POST: Assignment/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentId, string? content, IFormFile? file)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Student)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .ThenInclude(c => c.EnrolledStudents)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            // Check if student is enrolled
            if (assignment.Course.EnrolledStudents?.All(s => s.Id != currentUser.Id) != false)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if already submitted
            var existingSubmission = assignment.Submissions?.FirstOrDefault(s => s.StudentId == currentUser.Id);
            if (existingSubmission != null)
            {
                TempData["Error"] = "You have already submitted this assignment.";
                return RedirectToAction("Details", "Course", new { id = assignment.CourseId });
            }

            if (string.IsNullOrWhiteSpace(content) && file == null)
            {
                ModelState.AddModelError("", "Please provide submission content or upload a file.");
                ViewBag.Assignment = assignment;
                return View();
            }

            string? filePath = null;

            // Handle file upload
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "submissions");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = $"/uploads/submissions/{fileName}";
            }

            var submission = new AssignmentSubmission
            {
                AssignmentId = assignmentId,
                StudentId = currentUser.Id,
                Comments = content,
                SubmissionUrl = filePath,
                SubmittedAt = DateTime.Now
            };

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment submitted successfully!";
            return RedirectToAction("EnrolledCourses", "Student");
        }

        // GET: Assignment/Evaluate/5
        public async Task<IActionResult> Evaluate(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            if (submission.Assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(submission);
        }

        // POST: Assignment/Evaluate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluate(int id, int marksObtained, string? feedback)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            if (submission.Assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (marksObtained < 0 || marksObtained > submission.Assignment.MaxMarks)
            {
                ModelState.AddModelError("", $"Marks must be between 0 and {submission.Assignment.MaxMarks}.");
                return View(submission);
            }

            submission.ObtainedMarks = marksObtained;
            submission.Feedback = feedback;

            _context.Update(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment evaluated successfully!";
            return RedirectToAction("Submissions", new { assignmentId = submission.AssignmentId });
        }

        // GET: Assignment/Submissions/5
        public async Task<IActionResult> Submissions(int assignmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            if (assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(assignment);
        }

        // GET: Assignment/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            if (assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Course = assignment.Course;
            return View(assignment);
        }

        // POST: Assignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string description, DateTime dueDate, int totalMarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            if (assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (dueDate < DateTime.Now)
            {
                ModelState.AddModelError("", "Due date must be in the future.");
                ViewBag.Course = assignment.Course;
                return View(assignment);
            }

            assignment.Title = title;
            assignment.Description = description;
            assignment.DueDate = dueDate;
            assignment.MaxMarks = totalMarks;

            _context.Update(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment updated successfully!";
            return RedirectToAction("MyCourses", "Teacher");
        }

        // POST: Assignment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            if (assignment.Course.TeacherId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if there are any submissions
            if (assignment.Submissions != null && assignment.Submissions.Any())
            {
                TempData["Error"] = "Cannot delete assignment with existing submissions.";
                return RedirectToAction("MyCourses", "Teacher");
            }

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment deleted successfully!";
            return RedirectToAction("MyCourses", "Teacher");
        }
    }
}
