using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Enums;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public sealed record DiscountProgramItem(DiscountProgramListItemDto Program)
{
    public int Id => Program.Id;
    public string Name => Program.Name;
    public string ValueText => DisplayFormat.DiscountValue(Program.Type, Program.Value);
    public string StartText => "Từ " + DisplayFormat.DateTime(Program.StartAtUtc);
    public string EndText => "Đến " + DisplayFormat.DateTime(Program.EndAtUtc);
    public string DetailsText => $"{ScopeText} · {Program.PriceBookName}";
    public string ScopeText => Program.Scope switch
    {
        ScopeEnum.Categories => $"{Program.ScopeItemCount} nhóm hàng",
        ScopeEnum.Products => $"{Program.ScopeItemCount} sản phẩm",
        _ => "Toàn bộ sản phẩm",
    };
    public string StatusText => DisplayFormat.Status(Program.Status);
    public bool IsDraft => Program.Status == ProgramDisplayStatusEnum.Draft;
    public bool IsActiveStatus => Program.Status is ProgramDisplayStatusEnum.Scheduled or ProgramDisplayStatusEnum.Running;
}
