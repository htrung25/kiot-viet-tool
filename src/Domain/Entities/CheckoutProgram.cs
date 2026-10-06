using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Domain.Entities;

public sealed record CheckoutProgram(int Id, string Name, DiscountEnum Type, decimal Value, DateTime StartAtUtc, DateTime EndAtUtc,
    IReadOnlyList<long> ProductIds);
