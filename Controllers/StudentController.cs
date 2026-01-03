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
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.FindByIdAsync(userId);
            
            if (currentUser?.Role != UserRole.Student)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                    .ThenInclude(c => c.Teacher)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Assignment)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null) return NotFound();

            var enrolledCourses = student.EnrolledCourses?.Count ?? 0;
            var allAssignments = await _context.Assignments
                .Where(a => student.EnrolledCourses.Select(c => c.Id).Contains(a.CourseId))
                .ToListAsync();

            var submissions = student.Submissions?.ToList() ?? new List<AssignmentSubmission>();
            var pendingAssignments = allAssignments.Count(a => 
                !submissions.Any(s => s.AssignmentId == a.Id) && a.DueDate > DateTime.Now);
            var completedAssignments = submissions.Count;

            var gradedSubmissions = submissions.Where(s => s.ObtainedMarks.HasValue).ToList();
            var averageGrade = gradedSubmissions.Any() 
                ? (int)gradedSubmissions.Average(s => (double)s.ObtainedMarks.Value / s.Assignment.MaxMarks * 100)
                : 0;

            ViewBag.EnrolledCourses = enrolledCourses;
            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.CompletedAssignments = completedAssignments;
            ViewBag.AverageGrade = averageGrade;
            ViewBag.Courses = student.EnrolledCourses?.Take(4).ToList();

            var pendingAssignmentsList = allAssignments
                .Where(a => !submissions.Any(s => s.AssignmentId == a.Id))
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToList();
            ViewBag.PendingAssignmentsList = pendingAssignmentsList;

            ViewBag.UpcomingDeadlines = allAssignments
                .Where(a => a.DueDate > DateTime.Now)
                .OrderBy(a => a.DueDate)
                .Take(3)
                .ToList();

            ViewBag.OverallProgress = completedAssignments > 0 && allAssignments.Count > 0
                ? (int)((double)completedAssignments / allAssignments.Count * 100)
                : 0;

            ViewBag.RecentGrades = submissions
                .Where(s => s.ObtainedMarks.HasValue)
                .OrderByDescending(s => s.EvaluatedAt)
                .Take(5)
                .ToList();

            return View();
        }

        public async Task<IActionResult> EnrolledCourses()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                    .ThenInclude(c => c.Teacher)
                .Include(u => u.EnrolledCourses)
                    .ThenInclude(c => c.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return View(student?.EnrolledCourses?.ToList() ?? new List<Course>());
        }

        public async Task<IActionResult> AvailableCourses()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var enrolledCourseIds = student?.EnrolledCourses?.Select(c => c.Id).ToList() ?? new List<int>();

            var availableCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Teacher)
                .Where(c => c.IsActive && !enrolledCourseIds.Contains(c.Id))
                .ToListAsync();

            return View(availableCourses);
        }

        [HttpPost]
        public async Task<IActionResult> EnrollCourse(int courseId)
        {
            var userId = _userManager.GetUserId(User);
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var course = await _context.Courses.FindAsync(courseId);

            if (student != null && course != null)
            {
                if (student.EnrolledCourses == null)
                    student.EnrolledCourses = new List<Course>();

                student.EnrolledCourses.Add(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Successfully enrolled in the course!";
            }

            return RedirectToAction(nameof(EnrollInCourses));
        }

        // New cascading enrollment page
        public async Task<IActionResult> EnrollInCourses()
        {
            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            return View();
        }

        // API endpoint to get departments by faculty
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByFaculty(int facultyId)
        {
            var departments = await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .OrderBy(d => d.Name)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();
            
            return Json(departments);
        }

        // API endpoint to get courses by department
        [HttpGet]
        public async Task<IActionResult> GetCoursesByDepartment(int departmentId)
        {
            var userId = _userManager.GetUserId(User);
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var enrolledCourseIds = student?.EnrolledCourses?.Select(c => c.Id).ToList() ?? new List<int>();

            var courses = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Department)
                .Where(c => c.DepartmentId == departmentId && c.IsActive && !enrolledCourseIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new { 
                    id = c.Id, 
                    name = c.Name,
                    code = c.Code,
                    description = c.Description,
                    teacherName = c.Teacher != null ? c.Teacher.FullName : "N/A"
                })
                .ToListAsync();
            
            return Json(courses);
        }

        public async Task<IActionResult> MyAssignments()
        {
            var userId = _userManager.GetUserId(User);
            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .Where(s => s.StudentId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }

        public async Task<IActionResult> Performance()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                    .ThenInclude(c => c.Assignments)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Assignment)
                        .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null) return NotFound();

            // Calculate statistics
            var allSubmissions = student.Submissions?.Where(s => s.ObtainedMarks.HasValue).ToList() ?? new List<AssignmentSubmission>();
            
            ViewBag.TotalSubmissions = allSubmissions.Count;
            ViewBag.TotalCourses = student.EnrolledCourses?.Count ?? 0;
            
            if (allSubmissions.Any())
            {
                var averagePercentage = allSubmissions.Average(s => 
                    (double)s.ObtainedMarks!.Value / s.Assignment.MaxMarks * 100);
                ViewBag.AverageGrade = averagePercentage;
                
                var highestPercentage = allSubmissions.Max(s => 
                    (double)s.ObtainedMarks!.Value / s.Assignment.MaxMarks * 100);
                ViewBag.HighestGrade = highestPercentage;
                
                var lowestPercentage = allSubmissions.Min(s => 
                    (double)s.ObtainedMarks!.Value / s.Assignment.MaxMarks * 100);
                ViewBag.LowestGrade = lowestPercentage;
            }
            else
            {
                ViewBag.AverageGrade = 0;
                ViewBag.HighestGrade = 0;
                ViewBag.LowestGrade = 0;
            }

            // Course-wise performance
            var coursePerformance = student.EnrolledCourses?
                .Select(c => new
                {
                    Course = c,
                    Submissions = allSubmissions.Where(s => s.Assignment.CourseId == c.Id).ToList(),
                    Average = allSubmissions.Where(s => s.Assignment.CourseId == c.Id).Any()
                        ? allSubmissions.Where(s => s.Assignment.CourseId == c.Id)
                            .Average(s => (double)s.ObtainedMarks!.Value / s.Assignment.MaxMarks * 100)
                        : 0
                })
                .ToList();
            
            ViewBag.CoursePerformance = coursePerformance;
            ViewBag.RecentSubmissions = allSubmissions.OrderByDescending(s => s.EvaluatedAt).Take(5).ToList();

            return View();
        }

        public async Task<IActionResult> CourseDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.FindByIdAsync(userId);
            
            if (currentUser?.Role != UserRole.Student)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if student is enrolled in this course
            var student = await _userManager.Users
                .Include(u => u.EnrolledCourses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var isEnrolled = student?.EnrolledCourses?.Any(c => c.Id == id) ?? false;
            
            if (!isEnrolled)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction(nameof(EnrolledCourses));
            }

            // Redirect to Course controller's Details action
            return RedirectToAction("Details", "Course", new { id = id });
        }
    }
}
