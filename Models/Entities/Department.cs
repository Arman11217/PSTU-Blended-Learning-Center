namespace PBLC.Web.Models.Entities;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? FacultyId { get; set; }

    // Navigation Properties
    public Faculty? Faculty { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
