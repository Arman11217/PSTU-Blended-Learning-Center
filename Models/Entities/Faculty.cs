namespace PBLC.Web.Models.Entities;

public class Faculty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
