namespace KiotVietTool.Desktop.Models;

public sealed record FilterOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}
