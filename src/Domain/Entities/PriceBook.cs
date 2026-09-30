namespace KiotVietTool.Domain.Entities;

public sealed class PriceBook
{
    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; }
    public bool IsGlobal { get; private set; }
    public DateTime? StartAtUtc { get; private set; }
    public DateTime? EndAtUtc { get; private set; }
    public bool ForAllCustomerGroups { get; private set; }
    public bool ForAllUsers { get; private set; }
    public List<long> BranchIds { get; private set; } = [];
    public List<long> CustomerGroupIds { get; private set; } = [];
    public List<long> UserIds { get; private set; } = [];
    public bool IsDeleted { get; private set; }

    private PriceBook() { } // EF Core

    public static PriceBook Create(long id, string name, bool isActive, bool isGlobal, DateTime? startAtUtc, DateTime? endAtUtc,
        bool forAllCustomerGroups, bool forAllUsers, IEnumerable<long> branchIds, IEnumerable<long> customerGroupIds,
        IEnumerable<long> userIds) =>
        new()
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            IsGlobal = isGlobal,
            StartAtUtc = startAtUtc,
            EndAtUtc = endAtUtc,
            ForAllCustomerGroups = forAllCustomerGroups,
            ForAllUsers = forAllUsers,
            BranchIds = [.. branchIds],
            CustomerGroupIds = [.. customerGroupIds],
            UserIds = [.. userIds],
        };

    public bool ForAllBranches => BranchIds.Count == 0;

    public bool IsExpiredAt(DateTime nowUtc) => EndAtUtc is { } end && end <= nowUtc;

    public void MarkDeleted() => IsDeleted = true;
}
