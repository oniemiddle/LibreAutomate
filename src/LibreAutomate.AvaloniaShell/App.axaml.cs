using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibreAutomate.AvaloniaShell.Infrastructure;
using LibreAutomate.AvaloniaShell.ViewModels;
using LibreAutomate.AvaloniaShell.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LibreAutomate.AvaloniaShell;

public partial class App : Application
{
    private readonly IHost _host;

    public App(IHost host)
    {
        _host = host;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        GlobalExceptionHandler.RegisterUiThread(_host.Services);
        base.OnFrameworkInitializationCompleted();
    }
}
