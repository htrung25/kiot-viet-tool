using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class TelegramSetupViewModel(
    ITelegramService telegram,
    IAuthService authService,
    IUserSessionService session,
    INavigationService navigation,
    INotificationService notifications) : ViewModelBase
{
    TelegramChatDto? _chat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsStep4))]
    public partial int Step { get; private set; } = 1;

    [ObservableProperty] public partial string BotToken { get; set; } = "";
    [ObservableProperty] public partial string BotUsername { get; private set; } = "";
    [ObservableProperty] public partial string ChatTitle { get; private set; } = "";
    [ObservableProperty] public partial string Code { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> RecoveryCodes { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    public partial bool HasSavedCodes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(CanFinish))]
    public partial bool IsBusy { get; private set; }

    public bool IsMandatory { get; } = session.CurrentUser?.NeedsTelegram == true;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool IsStep4 => Step == 4;
    public bool IsIdle => !IsBusy;
    public bool CanFinish => HasSavedCodes && IsIdle;
    public string BotLink => $"t.me/{BotUsername}";
    public string RecoveryCodesText => string.Join(Environment.NewLine, RecoveryCodes);
    public string Title => IsMandatory ? "Kết nối Telegram để tiếp tục" : "Kết nối lại Telegram";
    public string Subtitle => IsMandatory
        ? "Mỗi lần đăng nhập, tool gửi mã xác thực qua bot Telegram của bạn. Cần kết nối một lần trước khi dùng tool."
        : "Đổi bot hoặc tài khoản Telegram nhận mã đăng nhập. Kết nối mới thay thế kết nối cũ và tạo bộ mã dự phòng mới.";

    partial void OnBotUsernameChanged(string value) => OnPropertyChanged(nameof(BotLink));
    partial void OnRecoveryCodesChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(RecoveryCodesText));

    [RelayCommand]
    Task VerifyBotAsync(CancellationToken cancellationToken) => RunAsync(async () =>
    {
        var result = await telegram.VerifyBotAsync(BotToken, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }
        BotUsername = result.Value!;
        Step = 2;
    });

    [RelayCommand]
    Task DetectChatAsync(CancellationToken cancellationToken) => RunAsync(async () =>
    {
        var chat = await telegram.DetectChatAsync(BotToken, cancellationToken);
        if (!chat.IsSuccess)
        {
            ErrorMessage = chat.Error;
            return;
        }
        _chat = chat.Value;
        ChatTitle = chat.Value!.Title;
        await SendCodeCoreAsync(cancellationToken);
    });

    [RelayCommand]
    Task SendCodeAsync(CancellationToken cancellationToken) => RunAsync(() => SendCodeCoreAsync(cancellationToken));

    [RelayCommand]
    Task ConfirmAsync(CancellationToken cancellationToken) => RunAsync(async () =>
    {
        var result = await telegram.ConfirmAsync(Code, cancellationToken);
        Code = "";
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }
        RecoveryCodes = result.Value!;
        Step = 4;
    });

    [RelayCommand]
    async Task FinishAsync(CancellationToken cancellationToken)
    {
        notifications.ShowSuccess($"Đã kết nối Telegram với {ChatTitle}.");
        RecoveryCodes = [];
        await navigation.NavigateHomeAsync(cancellationToken);
    }

    [RelayCommand]
    void Back()
    {
        ErrorMessage = null;
        if (Step is 2 or 3) Step--;
    }

    [RelayCommand]
    async Task CancelAsync(CancellationToken cancellationToken)
    {
        if (IsMandatory)
        {
            authService.SignOut();
            await navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
            return;
        }
        await navigation.NavigateToAsync<TelegramSettingsViewModel>(null, cancellationToken);
    }

    async Task SendCodeCoreAsync(CancellationToken cancellationToken)
    {
        if (_chat is null) return;
        var sent = await telegram.SendVerificationCodeAsync(BotToken, _chat, BotUsername, cancellationToken);
        if (!sent.IsSuccess)
        {
            ErrorMessage = sent.Error;
            return;
        }
        Step = 3;
    }

    async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = null;
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }
}
