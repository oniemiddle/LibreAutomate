namespace LibreAutomate.AvaloniaShell.Models;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string EnvironmentName { get; set; } = "Production";

    public FeatureFlagsOptions FeatureFlags { get; set; } = new();
}

public sealed class FeatureFlagsOptions
{
    public bool EnableSettingsPage { get; set; } = true;
}
