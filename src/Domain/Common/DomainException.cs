namespace KiotVietTool.Domain.Common;

/// <summary>Business rule violation. Message is user-facing (Vietnamese).</summary>
public sealed class DomainException(string message) : Exception(message);
