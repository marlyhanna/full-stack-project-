namespace LibrarySystem.Api.DTOs;

public class BooksListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? AvailableOnly { get; set; }

    /// <summary>title, author, or createdAt</summary>
    public string? SortBy { get; set; }

    /// <summary>asc or desc</summary>
    public string? SortOrder { get; set; } = "asc";
}
