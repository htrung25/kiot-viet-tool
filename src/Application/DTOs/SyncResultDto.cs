namespace KiotVietTool.Application.DTOs;

public sealed record SyncResultDto(int ChangedProducts, int RemovedProducts, int PriceBooks, bool WasFullSync);
