using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Api.DTOs;

public class BorrowingRequestCreate
{
    [Required]
    public Guid BookId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Range(1, 30, ErrorMessage = "Must be between 1 and 30")]
    public int BorrowingPeriodDays { get; set; }
}

public class BorrowRequestApprove
{
    [Required]
    public Guid ApprovedByAdminId { get; set; }
    public string? ApprovalNote { get; set; }
}

public class BorrowRequestDeny
{
    [Required]
    public Guid DeniedByAdminId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class RequestsListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Pending, Approved, Denied, Returned, Expired</summary>
    public string? Status { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BookId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
