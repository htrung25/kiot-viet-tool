using CommunityToolkit.Mvvm.ComponentModel;

namespace KiotVietTool.Desktop.Models;

public sealed partial class NotificationEventChoice(string title, string description, bool isSelected) : ObservableObject
{
    public string Title { get; } = title;
    public string Description { get; } = description;

    [ObservableProperty] public partial bool IsSelected { get; set; } = isSelected;
}
