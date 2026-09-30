namespace KiotVietTool.Domain.Entities;

public sealed class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public int? ParentId { get; private set; }

    private Category() { } // EF Core

    public static Category Create(int id, string name, int? parentId) =>
        new() { Id = id, Name = name, ParentId = parentId };
}
