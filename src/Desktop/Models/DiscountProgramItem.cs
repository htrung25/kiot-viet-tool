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
    public string StatusText => DisplayFormat.Phase(Program.Phase);
    public bool IsEditable => Program.Phase == ProgramPhaseEnum.Draft;
    public bool IsActiveStatus => Program.Phase is ProgramPhaseEnum.Upcoming or ProgramPhaseEnum.Live;
}
