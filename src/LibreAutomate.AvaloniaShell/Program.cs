using Avalonia;
using LibreAutomate.AvaloniaShell.Infrastructure;

namespace LibreAutomate.AvaloniaShell;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            return BuildAvaloniaApp(args).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            StartupFailureNotifier.Notify(ex);
            return -1;
        }
    }

    public static AppBuilder BuildAvaloniaApp(string[] args)
    {
        var host = AppBootstrapper.BuildHost(args);
        GlobalExceptionHandler.Register(host.Services);

        return AppBuilder.Configure(() => new App(host))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
