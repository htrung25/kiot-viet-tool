using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class DiscountProgramListViewModel(
    IDiscountProgramService programs,
    IPriceDeploymentService deployment,
    INavigationService navigation,
    IDialogService dialogs,
    INotificationService notifications) : ViewModelBase
{
    IReadOnlyList<DiscountProgramItem> _all = [];

    public IReadOnlyList<FilterOption<ProgramStatusEnum?>> StatusOptions { get; } =
        [new("Tất cả trạng thái", null), .. Enum.GetValues<ProgramStatusEnum>()
            .Select(s => new FilterOption<ProgramStatusEnum?>(DisplayFormat.Status(s), s))];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrograms), nameof(ShowNoResults))]
    public partial IReadOnlyList<DiscountProgramItem> Programs { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(ShowNoResults))]
    public partial bool HasData { get; private set; }

    [ObservableProperty] public partial FilterOption<ProgramStatusEnum?>? SelectedStatus { get; set; }
    [ObservableProperty] public partial bool IncludeLongEnded { get; set; }

    public bool HasPrograms => Programs.Count > 0;
    public bool ShowEmptyState => !HasData;
    public bool ShowNoResults => HasData && !HasPrograms;

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        SelectedStatus = StatusOptions[0];
        deployment.Changed += OnDeploymentChanged;
        await ReloadAsync(cancellationToken);
    }

    async void OnDeploymentChanged(object? sender, EventArgs e)
    {
        if (navigation.CurrentViewModel != this)
        {
            deployment.Changed -= OnDeploymentChanged;
            return;
        }
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ReloadAsync(CancellationToken.None));
    }

    [RelayCommand]
    Task OpenAsync(DiscountProgramItem item) =>
        navigation.NavigateToAsync<DiscountProgramDetailViewModel>(new ProgramDetailRequest(item.Id));

    partial void OnSelectedStatusChanged(FilterOption<ProgramStatusEnum?>? value) => ApplyFilter();

    async partial void OnIncludeLongEndedChanged(bool value) => await ReloadAsync(CancellationToken.None);

    [RelayCommand]
    Task CreateAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<DiscountProgramEditorViewModel>(null, cancellationToken);

    [RelayCommand]
    Task EditAsync(DiscountProgramItem item) =>
        navigation.NavigateToAsync<DiscountProgramEditorViewModel>(item.Id);

    [RelayCommand]
    async Task DuplicateAsync(DiscountProgramItem item)
    {
        var result = await programs.DuplicateAsync(item.Id);
        if (!result.IsSuccess)
        {
            await dialogs.ShowErrorAsync(result.Error!);
            return;
        }
        await navigation.NavigateToAsync<DiscountProgramEditorViewModel>(result.Value);
    }

    [RelayCommand]
    async Task DeleteAsync(DiscountProgramItem item)
    {
        var confirmed = await dialogs.ConfirmAsync("Xoá chương trình?",
            $"Chương trình nháp \"{item.Name}\" sẽ bị xoá khỏi tool. Chương trình chưa từng triển khai nên KiotViet không bị ảnh hưởng.",
            "Xoá chương trình", destructive: true);
        if (!confirmed) return;

        var result = await programs.DeleteAsync(item.Id);
        if (!result.IsSuccess)
        {
            await dialogs.ShowErrorAsync(result.Error!);
            return;
        }
        notifications.ShowSuccess("Đã xoá chương trình.");
        await ReloadAsync(CancellationToken.None);
    }

    [RelayCommand]
    void ClearFilters() => SelectedStatus = StatusOptions[0];

    async Task ReloadAsync(CancellationToken cancellationToken)
    {
        _all = [.. (await programs.GetListAsync(IncludeLongEnded, cancellationToken)).Select(p => new DiscountProgramItem(p))];
        HasData = _all.Count > 0;
        ApplyFilter();
    }

    void ApplyFilter() =>
        Programs = SelectedStatus?.Value is { } status ? [.. _all.Where(p => p.Program.Status == status)] : _all;
}
