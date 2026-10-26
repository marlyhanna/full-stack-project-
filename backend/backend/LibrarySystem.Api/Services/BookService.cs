using LibrarySystem.Api.Data;
using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public interface IBookService
{
    Task<PagedResult<Book>> ListAsync(BooksListQuery query, CancellationToken ct);
    Task<Book> GetByIdAsync(Guid id, CancellationToken ct);
}

public class BookService : IBookService
{
    private readonly LibraryDbContext _db;

    public BookService(LibraryDbContext db) => _db = db;

    public async Task<PagedResult<Book>> ListAsync(BooksListQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var books = _db.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            books = books.Where(b =>
                b.Title.Contains(term) ||
                b.Author.Contains(term) ||
                (b.Category != null && b.Category.Contains(term)));
        }

        if (query.AvailableOnly == true)
        {
            books = books.Where(b => b.IsAvailable && b.AvailableCopies > 0);
        }

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        books = query.SortBy?.ToLowerInvariant() switch
        {
            "author" => descending ? books.OrderByDescending(b => b.Author) : books.OrderBy(b => b.Author),
            "createdat" => descending ? books.OrderByDescending(b => b.CreatedAt) : books.OrderBy(b => b.CreatedAt),
            _ => descending ? books.OrderByDescending(b => b.Title) : books.OrderBy(b => b.Title),
        };

        var totalItems = await books.CountAsync(ct);
        var items = await books.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Book>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<Book> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null)
            throw new ApiException(404, "NOT_FOUND", "Book not found.");
        return book;
    }
}
