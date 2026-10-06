using System.Text.Json;

using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Domain.Entities;

public sealed class DiscountFeedSnapshot
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public int Id { get; private set; }
    public long Revision { get; private set; }
    public DateTime PublishedAtUtc { get; private set; }
    public string ProgramsJson { get; private set; } = "[]";

    IReadOnlyList<CheckoutProgram>? _programs;

    private DiscountFeedSnapshot() { } // EF Core

    public static DiscountFeedSnapshot Create(long revision, DateTime publishedAtUtc, IEnumerable<CheckoutProgram> programs) =>
        new() { Revision = revision, PublishedAtUtc = publishedAtUtc, ProgramsJson = JsonSerializer.Serialize(programs.ToList(), Json) };

    public IReadOnlyList<CheckoutProgram> Programs =>
        _programs ??= JsonSerializer.Deserialize<List<CheckoutProgram>>(ProgramsJson, Json) ?? [];

    // Must stay identical to computeDiscount in extension/discount.js.
    public (decimal Amount, IReadOnlyList<ReconciledLine> Lines) Evaluate(IReadOnlyList<CheckoutLine> lines, DateTime atUtc)
    {
        var byProduct = new Dictionary<long, CheckoutProgram>();
        foreach (var program in Programs.Where(p => p.StartAtUtc <= atUtc && atUtc < p.EndAtUtc))
            foreach (var id in program.ProductIds)
                byProduct.TryAdd(id, program);

        var total = 0m;
        var result = new List<ReconciledLine>(lines.Count);
        foreach (var line in lines)
        {
            var discount = 0m;
            CheckoutProgram? applied = null;
            if (byProduct.TryGetValue(line.ProductId, out var program) && line.Quantity > 0 && line.Price > 0)
            {
                var perUnit = program.Type == DiscountEnum.Percent ? line.Price * program.Value / 100
                    : program.Value < line.Price ? program.Value : 0;
                discount = perUnit * line.Quantity;
                applied = discount > 0 ? program : null;
            }
            total += discount;
            result.Add(new ReconciledLine(line.ProductId, line.Code, line.Name, line.Quantity, line.Price, discount, applied?.Name));
        }
        return (decimal.Round(total, 2, MidpointRounding.AwayFromZero), result);
    }
}
