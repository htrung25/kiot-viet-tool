using KiotVietTool.Domain.Enums;
using KiotVietTool.Domain.Exceptions;

namespace KiotVietTool.Domain.Entities;

public sealed class DiscountProgram
{
    public const int NameMaxLength = 100;
    public const int NoteMaxLength = 500;
    public const decimal MaxAmount = 1_000_000_000;
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(366);

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public DiscountEnum Type { get; private set; }
    public decimal Value { get; private set; }
    public RoundingEnum Rounding { get; private set; }
    public StartModeEnum StartMode { get; private set; }
    public DateTime? StartAtUtc { get; private set; }
    public DateTime EndAtUtc { get; private set; }
    public ScopeEnum Scope { get; private set; }
    public List<int> CategoryIds { get; private set; } = [];
    public List<long> ProductIds { get; private set; } = [];
    public List<long> ExcludedProductIds { get; private set; } = [];
    public UnitScopeEnum UnitScope { get; private set; }
    public string? Note { get; private set; }
    public ProgramStatusEnum Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private DiscountProgram() { } // EF Core

    public static DiscountProgram Create(string name, DiscountEnum type, decimal value, RoundingEnum rounding,
        StartModeEnum startMode, DateTime? startAtUtc, DateTime endAtUtc, ScopeEnum scope, IEnumerable<int> categoryIds,
        IEnumerable<long> productIds, IEnumerable<long> excludedProductIds, UnitScopeEnum unitScope, string? note, DateTime nowUtc)
    {
        var program = new DiscountProgram { Status = ProgramStatusEnum.Draft, CreatedAtUtc = nowUtc };
        program.Update(name, type, value, rounding, startMode, startAtUtc, endAtUtc, scope, categoryIds, productIds,
            excludedProductIds, unitScope, note, nowUtc);
        return program;
    }

    public void Update(string name, DiscountEnum type, decimal value, RoundingEnum rounding, StartModeEnum startMode,
        DateTime? startAtUtc, DateTime endAtUtc, ScopeEnum scope, IEnumerable<int> categoryIds, IEnumerable<long> productIds,
        IEnumerable<long> excludedProductIds, UnitScopeEnum unitScope, string? note, DateTime nowUtc)
    {
        if (!IsEditable)
            throw new DomainException("Chỉ sửa được toàn bộ thông tin khi chương trình còn là nháp hoặc chưa tới giờ bắt đầu.");

        name = name.Trim();
        EnsureValidName(name);
        EnsureValidValue(type, value);
        if (!Enum.IsDefined(rounding)) throw new DomainException("Kiểu làm tròn không hợp lệ.");
        if (!Enum.IsDefined(unitScope)) throw new DomainException("Đơn vị tính áp dụng không hợp lệ.");
        startAtUtc = startMode == StartModeEnum.Scheduled && startAtUtc is { } start ? TruncateToMinute(start) : null;
        endAtUtc = TruncateToMinute(endAtUtc);
        EnsureValidPeriod(startMode, startAtUtc, endAtUtc, nowUtc);

        List<int> categories = [.. categoryIds.Distinct()];
        List<long> products = [.. productIds.Distinct()];
        switch (scope)
        {
            case ScopeEnum.AllProducts:
                categories.Clear();
                products.Clear();
                break;
            case ScopeEnum.Categories when categories.Count == 0:
                throw new DomainException("Chọn ít nhất một nhóm hàng.");
            case ScopeEnum.Categories:
                products.Clear();
                break;
            case ScopeEnum.Products when products.Count == 0:
                throw new DomainException("Chọn ít nhất một sản phẩm.");
            case ScopeEnum.Products:
                categories.Clear();
                break;
            default:
                throw new DomainException("Phạm vi áp dụng không hợp lệ.");
        }

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note?.Length > NoteMaxLength) throw new DomainException($"Ghi chú tối đa {NoteMaxLength} ký tự.");

        Name = name;
        Type = type;
        Value = value;
        Rounding = rounding;
        StartMode = startMode;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        Scope = scope;
        CategoryIds = categories;
        ProductIds = products;
        ExcludedProductIds = [.. excludedProductIds.Distinct()];
        UnitScope = unitScope;
        Note = note;
        UpdatedAtUtc = nowUtc;
    }

    public bool IsDraft => Status == ProgramStatusEnum.Draft;

    public bool IsEditable => Status is ProgramStatusEnum.Draft or ProgramStatusEnum.Scheduled;

    public bool IsFinished => Status is ProgramStatusEnum.Ended or ProgramStatusEnum.Stopped;

    public bool HoldsKiotVietPrices => Status is ProgramStatusEnum.Applying or ProgramStatusEnum.Running
        or ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.Restoring or ProgramStatusEnum.RestoreFailed;

    public DateTime EffectiveStartAt(DateTime nowUtc) => StartAtUtc ?? nowUtc;

    public bool IsEndedAt(DateTime nowUtc) => EndAtUtc <= nowUtc;

    public bool IsOverdueAt(DateTime nowUtc) => HoldsKiotVietPrices && IsEndedAt(nowUtc);

    public bool OverlapsWith(DateTime startUtc, DateTime endUtc, DateTime nowUtc) =>
        EffectiveStartAt(nowUtc) < endUtc && startUtc < EndAtUtc;

    public void EnsureCanDelete()
    {
        if (!IsEditable)
            throw new DomainException("Chỉ xoá được chương trình chưa áp giá lên KiotViet. Chương trình đã áp giá hãy dùng Dừng chương trình.");
    }

    public PriceQuote Quote(Product product)
    {
        if (product.IsDeleted) return PriceQuote.Excluded(ExclusionReasonEnum.Deleted);
        if (!product.IsActive) return PriceQuote.Excluded(ExclusionReasonEnum.Inactive);
        if (!product.AllowsSale) return PriceQuote.Excluded(ExclusionReasonEnum.NotForSale);
        if (product.Type == ProductEnum.Combo) return PriceQuote.Excluded(ExclusionReasonEnum.Combo);
        if (product.Type == ProductEnum.Service) return PriceQuote.Excluded(ExclusionReasonEnum.Service);
        if (UnitScope == UnitScopeEnum.BaseUnitOnly && !product.IsBaseUnit) return PriceQuote.Excluded(ExclusionReasonEnum.NotBaseUnit);
        if (product.BasePrice <= 0) return PriceQuote.Excluded(ExclusionReasonEnum.NoBasePrice);
        if (ExcludedProductIds.Contains(product.Id)) return PriceQuote.Excluded(ExclusionReasonEnum.ManuallyExcluded);

        var price = CalculateDiscountedPrice(product.BasePrice, Type, Value, Rounding);
        if (price <= 0) return PriceQuote.Excluded(ExclusionReasonEnum.InvalidDiscountedPrice);
        if (price >= product.BasePrice) return PriceQuote.Excluded(ExclusionReasonEnum.DiscountTooSmall);
        return new PriceQuote(price, null);
    }

    public static decimal CalculateDiscountedPrice(decimal basePrice, DiscountEnum type, decimal value, RoundingEnum rounding)
    {
        var raw = type == DiscountEnum.Percent ? basePrice * (1 - value / 100m) : basePrice - value;
        var step = rounding == RoundingEnum.None ? 1m : (decimal)(int)rounding;
        return Math.Floor(raw / step) * step;
    }

    public static void EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên chương trình không được để trống.");
        if (name.Trim().Length > NameMaxLength) throw new DomainException($"Tên chương trình tối đa {NameMaxLength} ký tự.");
    }

    public static void EnsureValidValue(DiscountEnum type, decimal value)
    {
        switch (type)
        {
            case DiscountEnum.Percent when value is <= 0 or >= 100:
                throw new DomainException("Mức giảm phải lớn hơn 0 và nhỏ hơn 100%.");
            case DiscountEnum.Percent when decimal.Round(value, 2) != value:
                throw new DomainException("Mức giảm % tối đa 2 chữ số thập phân.");
            case DiscountEnum.Amount when value is <= 0 or > MaxAmount:
                throw new DomainException("Số tiền giảm phải lớn hơn 0 và không quá 1.000.000.000 ₫.");
            case DiscountEnum.Amount when decimal.Truncate(value) != value:
                throw new DomainException("Số tiền giảm phải là số nguyên.");
            case DiscountEnum.Percent or DiscountEnum.Amount:
                return;
            default:
                throw new DomainException("Loại giảm giá không hợp lệ.");
        }
    }

    public static void EnsureValidPeriod(StartModeEnum startMode, DateTime? startAtUtc, DateTime endAtUtc, DateTime nowUtc)
    {
        if (!Enum.IsDefined(startMode)) throw new DomainException("Cách bắt đầu không hợp lệ.");
        DateTime start;
        if (startMode == StartModeEnum.Scheduled)
        {
            start = startAtUtc ?? throw new DomainException("Chọn thời điểm bắt đầu.");
            if (start <= nowUtc) throw new DomainException("Thời điểm bắt đầu không được ở quá khứ.");
        }
        else start = nowUtc;

        if (endAtUtc - start < MinDuration)
            throw new DomainException("Thời điểm kết thúc phải sau thời điểm bắt đầu ít nhất 5 phút.");
        if (endAtUtc - start > MaxDuration)
            throw new DomainException("Chương trình kéo dài tối đa 366 ngày.");
    }

    static DateTime TruncateToMinute(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, value.Kind);

    public readonly record struct PriceQuote(decimal? DiscountedPrice, ExclusionReasonEnum? Reason)
    {
        public bool IsApplied => DiscountedPrice is not null;

        public static PriceQuote Excluded(ExclusionReasonEnum reason) => new(null, reason);
    }
}
