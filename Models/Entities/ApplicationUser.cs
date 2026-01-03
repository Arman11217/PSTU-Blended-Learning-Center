using Microsoft.AspNetCore.Identity;
using PBLC.Web.Models.Enums;

namespace PBLC.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? FacultyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Faculty? Faculty { get; set; }
    public ICollection<Course> TaughtCourses { get; set; } = new List<Course>();
    public ICollection<Course> EnrolledCourses { get; set; } = new List<Course>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
