namespace KiotVietTool.Application.Exceptions;

public sealed class CloudflareApiException(string message, int? statusCode = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;
}
