namespace PBLC.Web.Models.Entities;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public int CourseId { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int MaxMarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Course Course { get; set; } = null!;
    public ApplicationUser Teacher { get; set; } = null!;
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
