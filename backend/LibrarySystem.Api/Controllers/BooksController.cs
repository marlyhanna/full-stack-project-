using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Models;
using LibrarySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/books")]
[Authorize(Roles = UserRoles.User + "," + UserRoles.Admin)]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;

    public BooksController(IBookService books) => _books = books;

    /// <summary>Browse and filter books. Query: page, pageSize, search, availableOnly, sortBy, sortOrder.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Book>), 200)]
    public async Task<IActionResult> List([FromQuery] BooksListQuery query, CancellationToken ct)
        => Ok(await _books.ListAsync(query, ct));

    /// <summary>Get single book details.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Book), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _books.GetByIdAsync(id, ct));
}
