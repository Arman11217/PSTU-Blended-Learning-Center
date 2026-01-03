namespace PBLC.Web.Models.Entities;

public class AssignmentSubmission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? SubmissionUrl { get; set; } // File path of submitted work
    public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int? ObtainedMarks { get; set; }
    public string? Feedback { get; set; }
    public DateTime? EvaluatedAt { get; set; }

    // Navigation Properties
    public Assignment Assignment { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
}
