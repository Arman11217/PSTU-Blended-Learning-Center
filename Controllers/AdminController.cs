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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Check if user is Admin
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.FindByIdAsync(userId);
            
            if (currentUser?.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = await _userManager.Users.Include(u => u.Faculty).ToListAsync();
            
            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            ViewBag.TotalDepartments = await _context.Departments.CountAsync();
            ViewBag.TotalTeachers = users.Count(u => u.Role == UserRole.Teacher);
            ViewBag.TotalStudents = users.Count(u => u.Role == UserRole.Student);
            ViewBag.ActiveCourses = await _context.Courses.CountAsync(c => c.IsActive);
            ViewBag.TotalAssignments = await _context.Assignments.CountAsync();
            ViewBag.TotalQuestions = await _context.Questions.CountAsync();
            
            ViewBag.RecentUsers = users.OrderByDescending(u => u.CreatedAt).Take(5).ToList();

            return View();
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users
                .Include(u => u.Faculty)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ManageDepartments()
        {
            var departments = await _context.Departments
                .Include(d => d.Courses)
                .Include(d => d.Faculty)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(departments);
        }

        public async Task<IActionResult> ManageFaculties()
        {
            var faculties = await _context.Faculties
                .Include(f => f.Departments)
                .OrderBy(f => f.Name)
                .ToListAsync();
            return View(faculties);
        }

        [HttpGet]
        public IActionResult CreateFaculty()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateFaculty(Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                faculty.CreatedAt = DateTime.Now;
                _context.Faculties.Add(faculty);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Faculty created successfully!";
                return RedirectToAction(nameof(ManageFaculties));
            }
            return View(faculty);
        }

        public async Task<IActionResult> ManageCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Department)
                    .ThenInclude(d => d.Faculty)
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(courses);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCourseStatus(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                course.IsActive = !course.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Course status updated to {(course.IsActive ? "Active" : "Inactive")}";
            }
            return RedirectToAction(nameof(ManageCourses));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.EnrolledStudents)
                .Include(c => c.Assignments)
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course != null)
            {
                // Check if course has enrolled students
                if (course.EnrolledStudents?.Any() == true)
                {
                    TempData["Error"] = "Cannot delete course with enrolled students. Please unenroll all students first.";
                    return RedirectToAction(nameof(ManageCourses));
                }

                // Check if course has assignments
                if (course.Assignments?.Any() == true)
                {
                    TempData["Error"] = "Cannot delete course with assignments. Please delete all assignments first.";
                    return RedirectToAction(nameof(ManageCourses));
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course deleted successfully!";
            }
            return RedirectToAction(nameof(ManageCourses));
        }

        [HttpGet]
        public IActionResult CreateDepartment()
        {
            ViewBag.Faculties = _context.Faculties.OrderBy(f => f.Name).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                department.CreatedAt = DateTime.Now;
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Department created successfully!";
                return RedirectToAction(nameof(ManageDepartments));
            }
            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> EditFaculty(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
            {
                return NotFound();
            }
            return View(faculty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaculty(Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                var existingFaculty = await _context.Faculties.FindAsync(faculty.Id);
                if (existingFaculty != null)
                {
                    existingFaculty.Name = faculty.Name;
                    existingFaculty.Description = faculty.Description;
                    
                    _context.Update(existingFaculty);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Faculty updated successfully!";
                    return RedirectToAction(nameof(ManageFaculties));
                }
            }
            return View(faculty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaculty(int id)
        {
            var faculty = await _context.Faculties
                .Include(f => f.Departments)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty != null)
            {
                // Check if faculty has departments
                if (faculty.Departments?.Any() == true)
                {
                    TempData["Error"] = "Cannot delete faculty with existing departments. Please delete all departments first.";
                    return RedirectToAction(nameof(ManageFaculties));
                }

                _context.Faculties.Remove(faculty);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Faculty deleted successfully!";
            }
            return RedirectToAction(nameof(ManageFaculties));
        }

        [HttpGet]
        public async Task<IActionResult> EditDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                var existingDept = await _context.Departments.FindAsync(department.Id);
                if (existingDept != null)
                {
                    existingDept.Name = department.Name;
                    existingDept.FacultyId = department.FacultyId;
                    
                    _context.Update(existingDept);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Department updated successfully!";
                    return RedirectToAction(nameof(ManageDepartments));
                }
            }
            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department != null)
            {
                // Check if department has courses
                if (department.Courses?.Any() == true)
                {
                    TempData["Error"] = "Cannot delete department with existing courses. Please delete all courses first.";
                    return RedirectToAction(nameof(ManageDepartments));
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Department deleted successfully!";
            }
            return RedirectToAction(nameof(ManageDepartments));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Check if user is a teacher with assignments
                if (user.Role == UserRole.Teacher)
                {
                    var hasAssignments = await _context.Assignments.AnyAsync(a => a.TeacherId == id);
                    if (hasAssignments)
                    {
                        TempData["Error"] = "Cannot delete teacher with existing assignments. Please delete all assignments first.";
                        return RedirectToAction(nameof(ManageUsers));
                    }
                }

                // Check if user is a student with submissions
                if (user.Role == UserRole.Student)
                {
                    var hasSubmissions = await _context.AssignmentSubmissions.AnyAsync(s => s.StudentId == id);
                    if (hasSubmissions)
                    {
                        TempData["Error"] = "Cannot delete student with existing assignment submissions. Please delete all submissions first.";
                        return RedirectToAction(nameof(ManageUsers));
                    }
                }

                await _userManager.DeleteAsync(user);
                TempData["Success"] = "User deleted successfully!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found!";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Parse the new role
            if (Enum.TryParse<UserRole>(newRole, out var role))
            {
                user.Role = role;
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["Success"] = $"User role changed to {newRole} successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to change user role!";
                }
            }
            else
            {
                TempData["Error"] = "Invalid role specified!";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        public async Task<IActionResult> ViewUserDetails(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Faculty)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}
