using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public sealed record DiscountProgramItem(DiscountProgramListItemDto Program)
{
    public int Id => Program.Id;
    public string Name => Program.Name;
    public string ValueText => DisplayFormat.DiscountValue(Program.Type, Program.Value);
    public string StartText => Program.StartAtUtc is { } start
        ? "Từ " + DisplayFormat.DateTime(start)
        : "Từ lúc áp dụng";
    public string EndText => "Đến " + DisplayFormat.DateTime(Program.EndAtUtc);
    public string DetailsText => Program.Scope switch
    {
        ScopeEnum.Categories => $"{Program.ScopeItemCount} nhóm hàng",
        ScopeEnum.Products => $"{Program.ScopeItemCount} sản phẩm",
        _ => "Toàn bộ sản phẩm",
    };
    public string StatusText => Program.IsOverdue ? "Quá hạn — chưa trả giá" : DisplayFormat.Status(Program.Status);
    public bool IsEditable => Program.Status is ProgramStatusEnum.Draft or ProgramStatusEnum.Scheduled;
    public bool IsActiveStatus => !Program.IsOverdue && Program.Status is ProgramStatusEnum.Scheduled or ProgramStatusEnum.Running;
}
