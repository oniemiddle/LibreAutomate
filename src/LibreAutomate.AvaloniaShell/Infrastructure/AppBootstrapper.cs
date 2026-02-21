using LibreAutomate.AvaloniaShell.Models;
using LibreAutomate.AvaloniaShell.Services;
using LibreAutomate.AvaloniaShell.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibreAutomate.AvaloniaShell.Infrastructure;

internal static class AppBootstrapper
{
    public static IHost BuildHost(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("LIBREAUTOMATE_ENV")
            ?? Environments.Production;

        var builder = Host.CreateApplicationBuilder(args);
        builder.Environment.EnvironmentName = environmentName;

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "LIBREAUTOMATE_");

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<HomePageViewModel>();
        builder.Services.AddSingleton<SettingsPageViewModel>();
        builder.Services.AddSingleton<MainWindowViewModel>();

        return builder.Build();
    }
}
