using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Models;
using LibrarySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Authorize]
public class BorrowingRequestsController : ControllerBase
{
    private readonly IBorrowingRequestService _requests;
    private readonly ICurrentUserService _currentUser;

    public BorrowingRequestsController(IBorrowingRequestService requests, ICurrentUserService currentUser)
    {
        _requests = requests;
        _currentUser = currentUser;
    }

    /// <summary>Submit a borrowing request. Sent to RabbitMQ for admin notification processing.</summary>
    [HttpPost("api/borrow")]
    [Authorize(Roles = UserRoles.User)]
    [ProducesResponseType(typeof(BorrowingRequest), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    public async Task<IActionResult> Borrow(BorrowingRequestCreate dto, CancellationToken ct)
    {
        var created = await _requests.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>List and review all borrowing requests (admin).</summary>
    [HttpGet("api/requests")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<BorrowingRequest>), 200)]
    public async Task<IActionResult> List([FromQuery] RequestsListQuery query, CancellationToken ct)
        => Ok(await _requests.ListAsync(query, ct));

    /// <summary>View current user's borrowing history and statuses.</summary>
    [HttpGet("api/requests/my")]
    [Authorize(Roles = UserRoles.User)]
    [ProducesResponseType(typeof(PagedResult<BorrowingRequest>), 200)]
    public async Task<IActionResult> ListMine([FromQuery] RequestsListQuery query, CancellationToken ct)
        => Ok(await _requests.ListForUserAsync(_currentUser.UserId, query, ct));

    /// <summary>Get one borrowing request. Users may only view their own; admins may view any.</summary>
    [HttpGet("api/requests/{id:guid}")]
    [ProducesResponseType(typeof(BorrowingRequest), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(id, ct);
        if (!_currentUser.IsAdmin && request.UserId != _currentUser.UserId)
            throw new ApiException(403, "FORBIDDEN", "You may only view your own borrowing requests.");
        return Ok(request);
    }

    /// <summary>Approve a pending borrowing request.</summary>
    [HttpPut("api/requests/{id:guid}/approve")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(BorrowingRequest), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    public async Task<IActionResult> Approve(Guid id, BorrowRequestApprove dto, CancellationToken ct)
        => Ok(await _requests.ApproveAsync(id, dto, ct));

    /// <summary>Deny a pending borrowing request.</summary>
    [HttpPut("api/requests/{id:guid}/deny")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(BorrowingRequest), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Deny(Guid id, BorrowRequestDeny dto, CancellationToken ct)
        => Ok(await _requests.DenyAsync(id, dto, ct));
}
