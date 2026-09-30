namespace KiotVietTool.Application.Exceptions;

public sealed class KiotVietApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
