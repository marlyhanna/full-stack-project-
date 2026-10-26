namespace LibrarySystem.Api.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorDetail>? Details { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

/// <summary>Thrown by services to signal a specific HTTP status + structured error body.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public List<ErrorDetail>? Details { get; }

    public ApiException(int statusCode, string code, string message, List<ErrorDetail>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }
}
