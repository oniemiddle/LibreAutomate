using LibreAutomate.AvaloniaShell.ViewModels;

namespace LibreAutomate.AvaloniaShell.Services;

public interface INavigationService
{
    event EventHandler<ViewModelBase>? CurrentPageChanged;

    ViewModelBase CurrentPage { get; }

    void NavigateTo<TPage>() where TPage : ViewModelBase;
}
