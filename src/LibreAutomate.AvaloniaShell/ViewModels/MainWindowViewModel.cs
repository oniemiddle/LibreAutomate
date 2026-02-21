using LibreAutomate.AvaloniaShell.Models;
using LibreAutomate.AvaloniaShell.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows.Input;

namespace LibreAutomate.AvaloniaShell.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly AppOptions _appOptions;
    private ViewModelBase _currentPage;

    public MainWindowViewModel(
        INavigationService navigationService,
        IOptions<AppOptions> appOptions,
        ILogger<MainWindowViewModel> logger)
    {
        _navigationService = navigationService;
        _logger = logger;
        _appOptions = appOptions.Value;

        Title = $"LibreAutomate Shell ({_appOptions.EnvironmentName})";

        _currentPage = _navigationService.CurrentPage;
        _navigationService.CurrentPageChanged += OnCurrentPageChanged;

        NavigateHomeCommand = new DelegateCommand(() => NavigateTo<HomePageViewModel>());
        NavigateSettingsCommand = new DelegateCommand(
            () => NavigateTo<SettingsPageViewModel>(),
            () => _appOptions.FeatureFlags.EnableSettingsPage);
    }

    public string Title { get; }

    public bool IsSettingsEnabled => _appOptions.FeatureFlags.EnableSettingsPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public ICommand NavigateHomeCommand { get; }

    public ICommand NavigateSettingsCommand { get; }

    private void NavigateTo<TPage>() where TPage : ViewModelBase
    {
        _logger.LogInformation("Navigate to {Page}", typeof(TPage).Name);
        _navigationService.NavigateTo<TPage>();
    }

    private void OnCurrentPageChanged(object? sender, ViewModelBase viewModel)
    {
        CurrentPage = viewModel;
    }
}
