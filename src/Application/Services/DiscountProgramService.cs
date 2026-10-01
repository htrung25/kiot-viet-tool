using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class DiscountProgramService(
    IDiscountProgramRepository programs,
    ICatalogRepository catalog,
    TimeProvider timeProvider,
    ILogger<DiscountProgramService> logger) : IDiscountProgramService
{
    const decimal HighDiscountPercent = 50;
    static readonly TimeSpan LongEndedAfter = TimeSpan.FromDays(90);

    public async Task<IReadOnlyList<DiscountProgramListItemDto>> GetListAsync(bool includeLongEnded, CancellationToken cancellationToken = default)
    {
        var now = UtcNow;
        var all = await programs.GetAllAsync(cancellationToken);

        return [.. all
            .Where(p => includeLongEnded || !p.IsFinished || p.EndAtUtc > now - LongEndedAfter)
            .OrderByDescending(p => p.EffectiveStartAt(now)).ThenByDescending(p => p.Id)
            .Select(p => new DiscountProgramListItemDto(p.Id, p.Name, p.Type, p.Value, p.StartMode, p.StartAtUtc, p.EndAtUtc,
                p.Scope, p.Scope == ScopeEnum.Categories ? p.CategoryIds.Count : p.ProductIds.Count, p.Status, p.IsOverdueAt(now)))];
    }

    public async Task<SaveDiscountProgramDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        await programs.GetAsync(id, cancellationToken) is { } p ? ToDto(p) : null;

    public async Task<Result<DiscountPreviewDto>> PreviewAsync(SaveDiscountProgramDto request, CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        return Evaluate(request, context);
    }

    public async Task<Result<int>> SaveAsync(SaveDiscountProgramDto request, CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var evaluation = Evaluate(request, context);
        if (!evaluation.IsSuccess) return Result.Failure<int>(evaluation.Error!);
        var preview = evaluation.Value!;

        var name = request.Name.Trim();
        var now = UtcNow;
        var duplicate = context.Programs.FirstOrDefault(p => p.Id != request.Id && !p.IsFinished && !p.IsEndedAt(now)
            && string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null) return Result.Failure<int>("Đã có chương trình chưa kết thúc mang tên này.");
        if (preview.ConflictCount > 0)
            return Result.Failure<int>($"{preview.ConflictCount} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Xem danh sách ở bước Xem trước và loại trừ các sản phẩm này hoặc chọn thời gian khác.");

        try
        {
            if (request.Id is { } id)
            {
                var existing = await programs.GetAsync(id, cancellationToken);
                if (existing is null) return Result.Failure<int>("Không tìm thấy chương trình.");
                existing.Update(request.Name, request.Type, request.Value, request.Rounding, request.StartMode, request.StartAtUtc,
                    request.EndAtUtc, request.Scope, request.CategoryIds, request.ProductIds, request.ExcludedProductIds,
                    request.UnitScope, request.Note, now);
                await programs.UpdateAsync(existing, cancellationToken);
                logger.LogInformation("Discount program {ProgramId} updated", existing.Id);
                return Result.Success(existing.Id);
            }

            var program = DiscountProgram.Create(request.Name, request.Type, request.Value, request.Rounding, request.StartMode,
                request.StartAtUtc, request.EndAtUtc, request.Scope, request.CategoryIds, request.ProductIds,
                request.ExcludedProductIds, request.UnitScope, request.Note, now);
            await programs.AddAsync(program, cancellationToken);
            logger.LogInformation("Discount program {ProgramId} created as draft", program.Id);
            return Result.Success(program.Id);
        }
        catch (DomainException ex)
        {
            return Result.Failure<int>(ex.Message);
        }
    }

    public async Task<Result<SaveDiscountProgramDto>> DuplicateAsync(int id, CancellationToken cancellationToken = default)
    {
        var program = await programs.GetAsync(id, cancellationToken);
        if (program is null) return Result.Failure<SaveDiscountProgramDto>("Không tìm thấy chương trình.");

        var now = UtcNow;
        var name = $"Bản sao của {program.Name}";
        if (name.Length > DiscountProgram.NameMaxLength) name = name[..DiscountProgram.NameMaxLength];
        var duration = program.EndAtUtc - program.EffectiveStartAt(now);
        return Result.Success(ToDto(program) with
        {
            Id = null,
            Name = name,
            StartMode = StartModeEnum.Immediately,
            StartAtUtc = null,
            EndAtUtc = now + (duration >= DiscountProgram.MinDuration ? duration : TimeSpan.FromDays(1)),
        });
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var program = await programs.GetAsync(id, cancellationToken);
        if (program is null) return Result.Failure("Không tìm thấy chương trình.");
        try { program.EnsureCanDelete(); }
        catch (DomainException ex) { return Result.Failure(ex.Message); }

        await programs.DeleteAsync(program, cancellationToken);
        logger.LogInformation("Discount program {ProgramId} deleted", id);
        return Result.Success();
    }

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    async Task<PricingContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var products = await catalog.GetAllProductsAsync(cancellationToken);
        var categories = await catalog.GetCategoriesAsync(cancellationToken);
        var priceBooks = await catalog.GetPriceBookEntitiesAsync(cancellationToken);
        var items = await catalog.GetPriceBookItemsAsync(cancellationToken);
        var allPrograms = await programs.GetAllAsync(cancellationToken);
        return new PricingContext(products, categories.ToLookup(c => c.ParentId, c => c.Id), priceBooks,
            items.ToLookup(i => i.PriceBookId), allPrograms);
    }

    Result<DiscountPreviewDto> Evaluate(SaveDiscountProgramDto request, PricingContext context)
    {
        DiscountProgram program;
        try
        {
            program = DiscountProgram.Create(request.Name, request.Type, request.Value, request.Rounding, request.StartMode,
                request.StartAtUtc, request.EndAtUtc, request.Scope, request.CategoryIds, request.ProductIds,
                request.ExcludedProductIds, request.UnitScope, request.Note, UtcNow);
        }
        catch (DomainException ex)
        {
            return Result.Failure<DiscountPreviewDto>(ex.Message);
        }
        return Result.Success(EvaluateProgram(program, context, request.Id));
    }

    DiscountPreviewDto EvaluateProgram(DiscountProgram program, PricingContext context, int? programId = null)
    {
        var now = UtcNow;
        var selfId = programId ?? program.Id;
        var start = program.EffectiveStartAt(now);
        var conflicts = new Dictionary<long, string>();
        foreach (var other in context.Programs.Where(p => p.Id != selfId && !p.IsFinished && !p.IsEndedAt(now)
                     && p.OverlapsWith(start, program.EndAtUtc, now)))
            foreach (var product in ResolveScope(other, context).Where(x => other.Quote(x).IsApplied))
                conflicts.TryAdd(product.Id, other.Name);

        var rows = ResolveScope(program, context)
            .Select(product => ToRow(product, program.Quote(product), conflicts))
            .OrderBy(r => r.Reason is not null).ThenBy(r => r.Code, StringComparer.Ordinal)
            .ToList();
        var applied = rows.Where(r => r.DiscountedPrice is not null).Select(r => r.ProductId).ToHashSet();

        var overlapping = context.PriceBooks
            .Where(b => !b.IsGlobal && b.IsActive
                && (b.StartAtUtc ?? DateTime.MinValue) < program.EndAtUtc && start < (b.EndAtUtc ?? DateTime.MaxValue))
            .Select(b => new OverlappingPriceBookDto(b.Name, context.ItemsByPriceBook[b.Id].Count(i => applied.Contains(i.ProductId))))
            .Where(b => b.ProductCount > 0)
            .ToList();

        return new DiscountPreviewDto(
            rows,
            applied.Count,
            rows.Count - applied.Count,
            rows.Sum(r => r.DiscountAmount ?? 0),
            rows.Count(r => r.ConflictProgramName is not null),
            overlapping);
    }

    static DiscountPreviewRowDto ToRow(Product product, DiscountProgram.PriceQuote quote, Dictionary<long, string> conflicts)
    {
        var conflict = quote.IsApplied ? conflicts.GetValueOrDefault(product.Id) : null;
        var highDiscount = quote.DiscountedPrice is { } price && (product.BasePrice - price) * 100 >= HighDiscountPercent * product.BasePrice;
        return new DiscountPreviewRowDto(product.Id, product.Code, product.FullName, product.Unit, product.BasePrice,
            quote.DiscountedPrice, quote.Reason, quote.Reason is { } reason ? ReasonText(reason) : null, highDiscount, conflict);
    }

    static IEnumerable<Product> ResolveScope(DiscountProgram program, PricingContext context)
    {
        switch (program.Scope)
        {
            case ScopeEnum.Categories:
                var categoryIds = new HashSet<int>();
                var pending = new Stack<int>(program.CategoryIds);
                while (pending.TryPop(out var id))
                    if (categoryIds.Add(id))
                        foreach (var child in context.ChildCategories[id]) pending.Push(child);
                return context.Products.Where(p => !p.IsDeleted && p.CategoryId is { } c && categoryIds.Contains(c));

            case ScopeEnum.Products:
                var selected = program.ProductIds.ToHashSet();
                var unitGroups = context.Products.Where(p => selected.Contains(p.Id)).Select(p => p.MasterUnitId ?? p.Id).ToHashSet();
                return context.Products.Where(p => selected.Contains(p.Id) || (!p.IsDeleted && unitGroups.Contains(p.MasterUnitId ?? p.Id)));

            default:
                return context.Products.Where(p => !p.IsDeleted);
        }
    }

    static SaveDiscountProgramDto ToDto(DiscountProgram p) =>
        new(p.Id, p.Name, p.Type, p.Value, p.Rounding, p.StartMode, p.StartAtUtc, p.EndAtUtc, p.Scope, p.CategoryIds,
            p.ProductIds, p.ExcludedProductIds, p.UnitScope, p.Note);

    static string ReasonText(ExclusionReasonEnum reason) => reason switch
    {
        ExclusionReasonEnum.Inactive => "Ngừng kinh doanh",
        ExclusionReasonEnum.NotForSale => "Không bán trực tiếp",
        ExclusionReasonEnum.NoBasePrice => "Chưa có giá bán",
        ExclusionReasonEnum.Combo => "Hàng combo",
        ExclusionReasonEnum.Service => "Hàng dịch vụ",
        ExclusionReasonEnum.Deleted => "Không còn trên KiotViet",
        ExclusionReasonEnum.NotBaseUnit => "Không phải đơn vị cơ bản",
        ExclusionReasonEnum.ManuallyExcluded => "Loại trừ thủ công",
        ExclusionReasonEnum.InvalidDiscountedPrice => "Giá sau giảm không hợp lệ",
        ExclusionReasonEnum.DiscountTooSmall => "Mức giảm quá nhỏ sau làm tròn",
        _ => reason.ToString(),
    };

    sealed record PricingContext(
        IReadOnlyList<Product> Products,
        ILookup<int?, int> ChildCategories,
        IReadOnlyList<PriceBook> PriceBooks,
        ILookup<long, PriceBookItem> ItemsByPriceBook,
        IReadOnlyList<DiscountProgram> Programs);
}
