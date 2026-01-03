namespace PBLC.Web.Models.Entities;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DepartmentId { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Department Department { get; set; } = null!;
    public ApplicationUser Teacher { get; set; } = null!;
    public ICollection<ApplicationUser> EnrolledStudents { get; set; } = new List<ApplicationUser>();
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
