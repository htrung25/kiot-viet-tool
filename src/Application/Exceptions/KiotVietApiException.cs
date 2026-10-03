namespace KiotVietTool.Application.Exceptions;

public sealed class KiotVietApiException(string message, Exception? innerException = null, int? statusCode = null)
    : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;

    public bool IsRejected => StatusCode is 400 or 404 or 420;
}
