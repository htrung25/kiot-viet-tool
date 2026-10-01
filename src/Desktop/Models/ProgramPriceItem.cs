using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public sealed record ProgramPriceItem(ProgramPriceDto Price)
{
    public string Code => Price.Code;
    public string Name => Price.Name;
    public string OriginalPriceText => DisplayFormat.Money(Price.OriginalPrice);
    public string DiscountedPriceText => DisplayFormat.Money(Price.DiscountedPrice);
    public bool HasError => Price.LastError is not null;
    public string? ErrorText => Price.LastError;
    public string StateText => Price.State switch
    {
        PriceStateEnum.Pending => "Chờ đổi giá",
        PriceStateEnum.Applied when Price.LastError is not null => "Chưa trả được giá",
        PriceStateEnum.Applied => "Đang giảm giá",
        PriceStateEnum.Failed => "Lỗi đổi giá",
        PriceStateEnum.Restored => "Đã trả giá gốc",
        PriceStateEnum.ChangedManually => "Giá bị sửa trên KiotViet",
        PriceStateEnum.Kept => "Giữ giá KiotViet",
        _ => Price.State.ToString(),
    };
    public bool IsGood => Price.State is PriceStateEnum.Applied or PriceStateEnum.Restored && Price.LastError is null;
}
