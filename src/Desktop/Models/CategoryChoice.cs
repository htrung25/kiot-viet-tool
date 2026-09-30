using CommunityToolkit.Mvvm.ComponentModel;

namespace KiotVietTool.Desktop.Models;

public sealed partial class CategoryChoice(int id, string name, int depth) : ObservableObject
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public Avalonia.Thickness Indent { get; } = new(depth * 20, 0, 0, 0);

    [ObservableProperty] public partial bool IsSelected { get; set; }
}
