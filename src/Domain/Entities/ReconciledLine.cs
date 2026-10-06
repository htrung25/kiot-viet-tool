namespace KiotVietTool.Domain.Entities;

public sealed record ReconciledLine(long ProductId, string Code, string Name, decimal Quantity, decimal Price, decimal ExpectedDiscount,
    string? ProgramName);
