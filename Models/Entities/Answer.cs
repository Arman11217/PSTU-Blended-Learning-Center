namespace PBLC.Web.Models.Entities;

public class Answer
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public string UserId { get; set; } = string.Empty; // Teacher or Student can answer
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAccepted { get; set; } = false; // If this is the accepted answer

    // Navigation Properties
    public Question Question { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
