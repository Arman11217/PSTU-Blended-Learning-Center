using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBLC.Web.Data;

namespace PBLC.Web.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                    .ThenInclude(s => s.Faculty)
                .Include(c => c.Lectures)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }
    }
}
