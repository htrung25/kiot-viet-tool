namespace KiotVietTool.Desktop.Services;

public interface IDialogService
{
    void ShowInfo(string message);
    void ShowError(string message);
    bool Confirm(string message);
}
