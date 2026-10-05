namespace KiotVietTool.Application.Exceptions;

public sealed class DiscountFeedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
