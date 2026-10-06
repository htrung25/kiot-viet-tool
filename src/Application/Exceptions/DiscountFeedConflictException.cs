using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Exceptions;

public sealed class DiscountFeedConflictException(DiscountFeedRemoteStateDto current)
    : Exception($"Discount feed is at revision {current.Revision}")
{
    public DiscountFeedRemoteStateDto Current { get; } = current;
}
