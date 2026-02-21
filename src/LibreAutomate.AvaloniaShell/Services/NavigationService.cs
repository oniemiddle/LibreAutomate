using LibreAutomate.AvaloniaShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LibreAutomate.AvaloniaShell.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase _currentPage;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _currentPage = _serviceProvider.GetRequiredService<HomePageViewModel>();
    }

    public event EventHandler<ViewModelBase>? CurrentPageChanged;

    public ViewModelBase CurrentPage => _currentPage;

    public void NavigateTo<TPage>() where TPage : ViewModelBase
    {
        var nextPage = _serviceProvider.GetRequiredService<TPage>();
        if (ReferenceEquals(_currentPage, nextPage))
        {
            return;
        }

        _currentPage = nextPage;
        CurrentPageChanged?.Invoke(this, _currentPage);
    }
}
