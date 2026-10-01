using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountProgramService
{
    Task<IReadOnlyList<DiscountProgramListItemDto>> GetListAsync(bool includeLongEnded, CancellationToken cancellationToken = default);
    Task<SaveDiscountProgramDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<DiscountPreviewDto>> PreviewAsync(SaveDiscountProgramDto request, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveAsync(SaveDiscountProgramDto request, CancellationToken cancellationToken = default);
    Task<Result<SaveDiscountProgramDto>> DuplicateAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
