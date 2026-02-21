using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibreAutomate.AvaloniaShell.Infrastructure;

internal static class GlobalExceptionHandler
{
    private static ILogger? _logger;

    public static void Register(IServiceProvider services)
    {
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger("GlobalException");

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                _logger?.LogCritical(ex, "AppDomain unhandled exception.");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "TaskScheduler unobserved exception.");
            args.SetObserved();
        };
    }

    public static void RegisterUiThread(IServiceProvider services)
    {
        _logger ??= services.GetService<ILoggerFactory>()?.CreateLogger("GlobalException");

        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "UI thread exception.");
            args.Handled = true;
        };
    }
}
