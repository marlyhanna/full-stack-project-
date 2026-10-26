namespace LibrarySystem.Api.Models;

public class BorrowingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }

  
    public string BookTitle { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Status { get; set; } = Models.RequestStatus.Pending;

    public int BorrowingPeriodDays { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? DenyReason { get; set; }
    public DateTime? DueDate { get; set; }

    public Book? Book { get; set; }
    public User? User { get; set; }
}
