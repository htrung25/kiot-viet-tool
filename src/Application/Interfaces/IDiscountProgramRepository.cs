using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountProgramRepository
{
    Task<IReadOnlyList<DiscountProgram>> GetAllAsync(CancellationToken cancellationToken);
    Task<DiscountProgram?> GetAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(DiscountProgram program, CancellationToken cancellationToken);
    Task UpdateAsync(DiscountProgram program, CancellationToken cancellationToken);
    Task DeleteAsync(DiscountProgram program, CancellationToken cancellationToken);
}
