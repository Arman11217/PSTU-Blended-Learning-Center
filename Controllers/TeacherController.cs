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
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.FindByIdAsync(userId);
            
            if (currentUser?.Role != UserRole.Teacher)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var courses = await _context.Courses
                .Include(c => c.EnrolledStudents)
                .Include(c => c.Assignments)
                .Include(c => c.Lectures)
                .Where(c => c.TeacherId == userId)
                .ToListAsync();

            var totalStudents = courses.SelectMany(c => c.EnrolledStudents).Distinct().Count();
            var totalAssignments = await _context.Assignments.Where(a => a.TeacherId == userId).CountAsync();
            var pendingSubmissions = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Where(s => s.Assignment.TeacherId == userId && s.ObtainedMarks == null)
                .CountAsync();

            ViewBag.MyCourses = courses.Count;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalAssignments = totalAssignments;
            ViewBag.PendingSubmissions = pendingSubmissions;
            ViewBag.Courses = courses.Take(4).ToList();

            var upcomingAssignments = await _context.Assignments
                .Where(a => a.TeacherId == userId && a.DueDate > DateTime.Now)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToListAsync();
            ViewBag.UpcomingAssignments = upcomingAssignments;

            return View();
        }

        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);
            var courses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.EnrolledStudents)
                .Where(c => c.TeacherId == userId)
                .ToListAsync();
            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByFaculty(int facultyId)
        {
            var departments = await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .Select(d => new { id = d.Id, name = d.Name })
                .OrderBy(d => d.name)
                .ToListAsync();
            return Json(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("Teacher");
            ModelState.Remove("Department");
            ModelState.Remove("EnrolledStudents");
            ModelState.Remove("Lectures");
            ModelState.Remove("Assignments");
            ModelState.Remove("Questions");
            ModelState.Remove("TeacherId");

            if (ModelState.IsValid)
            {
                course.TeacherId = _userManager.GetUserId(User);
                course.CreatedAt = DateTime.Now;
                course.IsActive = true;
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course created successfully!";
                return RedirectToAction(nameof(MyCourses));
            }
            
            // Log validation errors for debugging
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Console.WriteLine($"Validation Error: {error.ErrorMessage}");
            }
            
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(course);
        }
    }
}
