namespace LibrarySystem.Api.DTOs;

public class NotificationsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? RecipientUserId { get; set; }

    /// <summary>User or Admin</summary>
    public string? RecipientRole { get; set; }
    public bool? IsRead { get; set; }
}
