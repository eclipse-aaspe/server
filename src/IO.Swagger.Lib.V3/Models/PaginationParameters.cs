namespace IO.Swagger.Models;

/// <summary>
/// Represents parameters for pagination, including cursor position and result limit.
/// </summary>
public class PaginationParameters
{
    private const int MaxResultSize = 500;
    private int _limit = MaxResultSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationParameters"/> class.
    /// </summary>
    /// <param name="cursor">The position from which to resume a result listing, as a string.</param>
    /// <param name="limit">The maximum size of the result list.</param>
    public PaginationParameters(string? cursor, int? limit)
    {
        if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out int parsedCursor))
        {
            Cursor = parsedCursor;
        }
        else
        {
            Cursor = 0;
        }
        Cursor = Cursor;

        
        _limit = limit.HasValue ? limit.Value : MaxResultSize;
    }

    /// <summary>
    /// Gets or sets the maximum size of the result list. 
    /// If the value exceeds <see cref="MaxResultSize"/>, it will be set to <see cref="MaxResultSize"/>.
    /// </summary>
    public int Limit
    {
        get => _limit;
        set => _limit = (value > MaxResultSize) ? MaxResultSize : value;
    }

    /// <summary>
    /// Gets or sets the position from which to resume a result listing.
    /// </summary>
    public int Cursor { get; set; }
}