namespace PBLC.Web.Models.Entities;

public class Question
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;

    // Navigation Properties
    public Course Course { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
