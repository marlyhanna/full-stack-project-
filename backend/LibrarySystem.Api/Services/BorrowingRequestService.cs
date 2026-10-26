using LibrarySystem.Api.Data;
using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Hubs;
using LibrarySystem.Api.Messaging;
using LibrarySystem.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public interface IBorrowingRequestService
{
    Task<BorrowingRequest> CreateAsync(BorrowingRequestCreate dto, CancellationToken ct);
    Task<PagedResult<BorrowingRequest>> ListAsync(RequestsListQuery query, CancellationToken ct);
    Task<PagedResult<BorrowingRequest>> ListForUserAsync(Guid userId, RequestsListQuery query, CancellationToken ct);
    Task<BorrowingRequest> GetByIdAsync(Guid id, CancellationToken ct);
    Task<BorrowingRequest> ApproveAsync(Guid id, BorrowRequestApprove dto, CancellationToken ct);
    Task<BorrowingRequest> DenyAsync(Guid id, BorrowRequestDeny dto, CancellationToken ct);
}

public class BorrowingRequestService : IBorrowingRequestService
{
    private readonly LibraryDbContext _db;
    private readonly IBorrowRequestPublisher _publisher;
    private readonly IHubContext<NotificationHub> _hub;

    public BorrowingRequestService(LibraryDbContext db, IBorrowRequestPublisher publisher, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _publisher = publisher;
        _hub = hub;
    }

    public async Task<BorrowingRequest> CreateAsync(BorrowingRequestCreate dto, CancellationToken ct)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == dto.BookId, ct)
            ?? throw new ApiException(400, "INVALID_BOOK", "bookId does not reference an existing book.",
                new() { new ErrorDetail { Field = "bookId", Message = "Must reference an existing book" } });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId && u.IsActive, ct)
            ?? throw new ApiException(400, "INVALID_USER", "userId does not reference an active user.",
                new() { new ErrorDetail { Field = "userId", Message = "Must reference an active user" } });

        if (!book.IsAvailable || book.AvailableCopies < 1)
            throw new ApiException(422, "NO_COPIES_AVAILABLE", "This book has no available copies right now.");

        var request = new BorrowingRequest
        {
            BookId = book.Id,
            BookTitle = book.Title,
            UserId = user.Id,
            Status = RequestStatus.Pending,
            BorrowingPeriodDays = dto.BorrowingPeriodDays
        };

        _db.BorrowingRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        // Hand off to RabbitMQ; the consumer notifies admins asynchronously.
        _publisher.Publish(new BorrowRequestMessage
        {
            RequestId = request.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            UserId = user.Id,
            UserFullName = user.FullName,
            BorrowingPeriodDays = request.BorrowingPeriodDays,
            RequestedAt = request.RequestedAt
        });

        return request;
    }

    public async Task<PagedResult<BorrowingRequest>> ListAsync(RequestsListQuery query, CancellationToken ct)
        => await BuildPagedResult(_db.BorrowingRequests.AsNoTracking(), query, ct);

    public async Task<PagedResult<BorrowingRequest>> ListForUserAsync(Guid userId, RequestsListQuery query, CancellationToken ct)
        => await BuildPagedResult(_db.BorrowingRequests.AsNoTracking().Where(r => r.UserId == userId), query, ct);

    private static async Task<PagedResult<BorrowingRequest>> BuildPagedResult(
        IQueryable<BorrowingRequest> requests, RequestsListQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        if (!string.IsNullOrWhiteSpace(query.Status))
            requests = requests.Where(r => r.Status == query.Status);
        if (query.UserId.HasValue)
            requests = requests.Where(r => r.UserId == query.UserId);
        if (query.BookId.HasValue)
            requests = requests.Where(r => r.BookId == query.BookId);
        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            requests = requests.Where(r => r.RequestedAt >= from);
        }
        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            requests = requests.Where(r => r.RequestedAt <= to);
        }

        requests = requests.OrderByDescending(r => r.RequestedAt);

        var totalItems = await requests.CountAsync(ct);
        var items = await requests.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<BorrowingRequest>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<BorrowingRequest> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var request = await _db.BorrowingRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null)
            throw new ApiException(404, "NOT_FOUND", "Borrowing request not found.");
        return request;
    }

    public async Task<BorrowingRequest> ApproveAsync(Guid id, BorrowRequestApprove dto, CancellationToken ct)
    {
        var request = await _db.BorrowingRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new ApiException(404, "NOT_FOUND", "Borrowing request not found.");

        if (request.Status != RequestStatus.Pending)
            throw new ApiException(409, "CONFLICT", "Only pending requests can be approved.");

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.ApprovedByAdminId && u.Role == UserRoles.Admin, ct)
            ?? throw new ApiException(400, "INVALID_ADMIN", "approvedByAdminId must reference an Admin user.",
                new() { new ErrorDetail { Field = "approvedByAdminId", Message = "Must be an Admin user" } });

        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == request.BookId, ct)
            ?? throw new ApiException(422, "BOOK_UNAVAILABLE", "The related book no longer exists.");

        if (book.AvailableCopies < 1)
            throw new ApiException(422, "NO_COPIES_AVAILABLE", "This book has no available copies right now.");

        book.AvailableCopies -= 1;
        book.IsAvailable = book.AvailableCopies > 0;
        book.UpdatedAt = DateTime.UtcNow;

        request.Status = RequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = admin.Id;
        request.DueDate = DateTime.UtcNow.AddDays(request.BorrowingPeriodDays);

        _db.Notifications.Add(new Notification
        {
            RecipientUserId = request.UserId,
            RecipientRole = UserRoles.User,
            Type = Models.NotificationType.RequestApproved,
            Title = "Borrow request approved",
            Message = $"Your request to borrow \"{request.BookTitle}\" was approved. Due back {request.DueDate:yyyy-MM-dd}."
        });

        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group($"user:{request.UserId}").SendAsync("ReceiveNotification", new
        {
            type = Models.NotificationType.RequestApproved,
            requestId = request.Id,
            bookTitle = request.BookTitle,
            dueDate = request.DueDate
        }, ct);

        return request;
    }

    public async Task<BorrowingRequest> DenyAsync(Guid id, BorrowRequestDeny dto, CancellationToken ct)
    {
        var request = await _db.BorrowingRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new ApiException(404, "NOT_FOUND", "Borrowing request not found.");

        if (request.Status != RequestStatus.Pending)
            throw new ApiException(409, "CONFLICT", "Only pending requests can be denied.");

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.DeniedByAdminId && u.Role == UserRoles.Admin, ct)
            ?? throw new ApiException(400, "INVALID_ADMIN", "deniedByAdminId must reference an Admin user.",
                new() { new ErrorDetail { Field = "deniedByAdminId", Message = "Must be an Admin user" } });

        request.Status = RequestStatus.Denied;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = admin.Id;
        request.DenyReason = dto.Reason;

        _db.Notifications.Add(new Notification
        {
            RecipientUserId = request.UserId,
            RecipientRole = UserRoles.User,
            Type = Models.NotificationType.RequestDenied,
            Title = "Borrow request denied",
            Message = $"Your request to borrow \"{request.BookTitle}\" was denied. Reason: {dto.Reason}"
        });

        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group($"user:{request.UserId}").SendAsync("ReceiveNotification", new
        {
            type = Models.NotificationType.RequestDenied,
            requestId = request.Id,
            bookTitle = request.BookTitle,
            reason = dto.Reason
        }, ct);

        return request;
    }
}
