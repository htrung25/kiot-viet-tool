namespace KiotVietTool.Domain.Entities;

public sealed record CheckoutLine(long ProductId, string Code, string Name, decimal Quantity, decimal Price);
