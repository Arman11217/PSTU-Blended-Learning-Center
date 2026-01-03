using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBLC.Web.Models.Entities;
using PBLC.Web.Data;

namespace PBLC.Web.Controllers
{
    [Authorize]
    public class QAController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QAController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: QA/Index?courseId=1
        public async Task<IActionResult> Index(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                .Include(c => c.Questions)
                .ThenInclude(q => q.Student)
                .Include(c => c.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound();
            }

            // Check if user has access (teacher or enrolled student)
            var hasAccess = course.TeacherId == currentUser.Id ||
                           course.EnrolledStudents?.Any(s => s.Id == currentUser.Id) == true;

            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Course = course;
            return View(course.Questions?.OrderByDescending(q => q.CreatedAt).ToList());
        }

        // GET: QA/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var question = await _context.Questions
                .Include(q => q.Student)
                .Include(q => q.Course)
                .ThenInclude(c => c.Teacher)
                .Include(q => q.Course)
                .ThenInclude(c => c.EnrolledStudents)
                .Include(q => q.Answers)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            // Check access
            var hasAccess = question.Course.TeacherId == currentUser.Id ||
                           question.Course.EnrolledStudents?.Any(s => s.Id == currentUser.Id) == true;

            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(question);
        }

        // POST: QA/AskQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskQuestion(int courseId, string questionText)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound();
            }

            // Check access
            var hasAccess = course.TeacherId == currentUser.Id ||
                           course.EnrolledStudents?.Any(s => s.Id == currentUser.Id) == true;

            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                TempData["Error"] = "Question cannot be empty.";
                return RedirectToAction("Index", new { courseId });
            }

            var question = new Question
            {
                Content = questionText,
                Title = questionText.Length > 50 ? questionText.Substring(0, 50) + "..." : questionText,
                CourseId = courseId,
                StudentId = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question posted successfully!";
            return RedirectToAction("Index", new { courseId });
        }

        // POST: QA/PostAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAnswer(int questionId, string answerText)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var question = await _context.Questions
                .Include(q => q.Course)
                .ThenInclude(c => c.Teacher)
                .Include(q => q.Course)
                .ThenInclude(c => c.EnrolledStudents)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return NotFound();
            }

            // Check access
            var hasAccess = question.Course.TeacherId == currentUser.Id ||
                           question.Course.EnrolledStudents?.Any(s => s.Id == currentUser.Id) == true;

            if (!hasAccess)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(answerText))
            {
                TempData["Error"] = "Answer cannot be empty.";
                return RedirectToAction("Details", new { id = questionId });
            }

            var answer = new Answer
            {
                Content = answerText,
                QuestionId = questionId,
                UserId = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Answer posted successfully!";
            return RedirectToAction("Details", new { id = questionId });
        }

        // POST: QA/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            // Only the person who asked can delete
            if (question.StudentId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question deleted successfully!";
            return RedirectToAction("Index", new { courseId = question.CourseId });
        }

        // POST: QA/DeleteAnswer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var answer = await _context.Answers.FindAsync(id);

            if (answer == null)
            {
                return NotFound();
            }

            // Only the person who answered can delete
            if (answer.UserId != currentUser.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var questionId = answer.QuestionId;
            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Answer deleted successfully!";
            return RedirectToAction("Details", new { id = questionId });
        }
    }
}
