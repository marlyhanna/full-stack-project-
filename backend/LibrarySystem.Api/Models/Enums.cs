namespace LibrarySystem.Api.Models;

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}

public static class RequestStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Denied = "Denied";
    public const string Returned = "Returned";
    public const string Expired = "Expired";
}

public static class NotificationType
{
    public const string BorrowRequestCreated = "BorrowRequestCreated";
    public const string BorrowDueReminder = "BorrowDueReminder";
    public const string RequestApproved = "RequestApproved";
    public const string RequestDenied = "RequestDenied";
}
