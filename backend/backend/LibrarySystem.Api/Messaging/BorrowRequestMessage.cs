namespace LibrarySystem.Api.Messaging;

public class BorrowRequestMessage
{
    public Guid RequestId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int BorrowingPeriodDays { get; set; }
    public DateTime RequestedAt { get; set; }
}
