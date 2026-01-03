namespace PBLC.Web.Models.Entities;

public class Lecture
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentUrl { get; set; } // PDF, PPT, DOC file path
    public string? VideoUrl { get; set; }
    public int CourseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrderNumber { get; set; } // For ordering lectures

    // Navigation Property
    public Course Course { get; set; } = null!;
}
