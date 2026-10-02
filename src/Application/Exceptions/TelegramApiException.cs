namespace KiotVietTool.Application.Exceptions;

public sealed class TelegramApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
