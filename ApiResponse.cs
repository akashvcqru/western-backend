namespace western_backend
{
    /// <summary>
    /// Standard pagination metadata returned alongside list responses.
    /// </summary>
    public class PaginationMeta
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int Limit { get; set; }
    }

    /// <summary>
    /// Consistent success envelope: { success, message, data }
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    /// <summary>
    /// Paginated success envelope: { success, message, data, pagination }
    /// </summary>
    public class PaginatedApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<T> Data { get; set; } = new();
        public PaginationMeta Pagination { get; set; } = new();
    }

    /// <summary>
    /// Error envelope: { success: false, message, errors }
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Static factory for building response envelopes.
    /// </summary>
    public static class ApiResponse
    {
        public static ApiResponse<T> Success<T>(T data, string message = "Data fetched successfully")
            => new() { Success = true, Message = message, Data = data };

        public static PaginatedApiResponse<T> Paginated<T>(
            List<T> data,
            int currentPage,
            int totalItems,
            int limit,
            string message = "Data fetched successfully")
        {
            int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
            return new()
            {
                Success = true,
                Message = message,
                Data = data,
                Pagination = new PaginationMeta
                {
                    CurrentPage = currentPage,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    Limit = limit
                }
            };
        }

        public static ErrorResponse Error(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors ?? new() };
    }
}
