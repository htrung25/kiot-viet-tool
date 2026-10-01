using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IProgramPriceRepository
{
    Task<IReadOnlyList<ProgramProductPrice>> GetAsync(int programId, CancellationToken cancellationToken);
    Task SaveAsync(DiscountProgram program, IReadOnlyCollection<ProgramProductPrice> prices, CancellationToken cancellationToken);
}
