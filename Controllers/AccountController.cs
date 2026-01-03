using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PBLC.Web.Models.Entities;
using PBLC.Web.Models.Enums;
using PBLC.Web.Models;
using Microsoft.EntityFrameworkCore;
using PBLC.Web.Data;

namespace PBLC.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Find user by username
                var user = await _userManager.FindByNameAsync(model.Username);
                
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName, 
                        model.Password, 
                        model.RememberMe, 
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // Redirect based on user role
                        if (user.Role == UserRole.Admin)
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else if (user.Role == UserRole.Teacher)
                        {
                            return RedirectToAction("Dashboard", "Teacher");
                        }
                        else if (user.Role == UserRole.Student)
                        {
                            return RedirectToAction("Dashboard", "Student");
                        }

                        return RedirectToLocal(returnUrl);
                    }
                }
                
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate email domain - must end with pstu.ac.bd (allows subdomains like @cse.pstu.ac.bd, @esdm.pstu.ac.bd, etc.)
                if (!model.Email.EndsWith("pstu.ac.bd", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Email", "Only PSTU email addresses (e.g., @pstu.ac.bd, @cse.pstu.ac.bd, @esdm.pstu.ac.bd) are allowed.");
                    ViewBag.Faculties = await _context.Faculties.ToListAsync();
                    return View(model);
                }

                // Check if username already exists
                var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    ViewBag.Faculties = await _context.Faculties.ToListAsync();
                    return View(model);
                }

                // Check if email already exists (one email = one account)
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "This email address is already registered. Each email can only have one account.");
                    ViewBag.Faculties = await _context.Faculties.ToListAsync();
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Username,  // Now using username instead of email
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = model.Role,
                    FacultyId = model.FacultyId,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect based on role
                    if (user.Role == UserRole.Admin)
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else if (user.Role == UserRole.Teacher)
                    {
                        return RedirectToAction("Dashboard", "Teacher");
                    }
                    else if (user.Role == UserRole.Student)
                    {
                        return RedirectToAction("Dashboard", "Student");
                    }

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var faculty = await _context.Faculties.FindAsync(user.FacultyId);

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = user.Role,
                FacultyName = faculty?.Name ?? "Not Assigned",
                JoinedDate = user.CreatedAt,
                IsActive = user.IsActive
            };

            // Add role-based statistics
            if (user.Role == UserRole.Teacher)
            {
                var courses = await _context.Courses
                    .Include(c => c.EnrolledStudents)
                    .Include(c => c.Assignments)
                    .Where(c => c.TeacherId == user.Id)
                    .ToListAsync();

                ViewBag.TotalCourses = courses.Count;
                ViewBag.TotalStudents = courses.SelectMany(c => c.EnrolledStudents ?? new List<ApplicationUser>()).Distinct().Count();
                ViewBag.TotalAssignments = courses.Sum(c => c.Assignments?.Count ?? 0);
            }
            else if (user.Role == UserRole.Student)
            {
                var enrolledCourses = await _context.Courses
                    .Include(c => c.EnrolledStudents)
                    .Where(c => c.EnrolledStudents!.Any(s => s.Id == user.Id))
                    .ToListAsync();

                var submissions = await _context.AssignmentSubmissions
                    .Where(s => s.StudentId == user.Id)
                    .ToListAsync();

                ViewBag.EnrolledCourses = enrolledCourses.Count;
                ViewBag.TotalSubmissions = submissions.Count;
                ViewBag.GradedSubmissions = submissions.Count(s => s.ObtainedMarks != null);
            }
            else if (user.Role == UserRole.Admin)
            {
                ViewBag.TotalUsers = await _context.Users.CountAsync();
                ViewBag.TotalCourses = await _context.Courses.CountAsync();
                ViewBag.TotalDepartments = await _context.Departments.CountAsync();
                ViewBag.TotalFaculties = await _context.Faculties.CountAsync();
            }

            return View(model);
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Full name cannot be empty.";
                return RedirectToAction("Profile");
            }

            user.FullName = fullName;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to update profile.";
            }

            return RedirectToAction("Profile");
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "All fields are required.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirm password do not match.";
                return RedirectToAction("Profile");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return RedirectToAction("Profile");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Password changed successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to change password. Current password may be incorrect.";
            }

            return RedirectToAction("Profile");
        }
    }
}
