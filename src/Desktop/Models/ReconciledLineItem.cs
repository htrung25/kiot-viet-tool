using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Desktop.Models;

public sealed record ReconciledLineItem(ReconciledLine Line)
{
    public string Code => Line.Code;
    public string Name => Line.Name;
    public string QuantityText => Line.Quantity.ToString("0.###", DisplayFormat.Vietnamese);
    public string PriceText => DisplayFormat.Money(Line.Price);
    public string ExpectedText => Line.ExpectedDiscount > 0 ? DisplayFormat.Money(Line.ExpectedDiscount) : "—";
    public string ProgramName => Line.ProgramName ?? "";
}
