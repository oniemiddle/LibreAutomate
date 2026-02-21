using System.Runtime.InteropServices;

namespace LibreAutomate.AvaloniaShell.Infrastructure;

internal static partial class StartupFailureNotifier
{
    public static void Notify(Exception ex)
    {
        var message = $"应用启动失败，请查看日志。\n\n{ex.Message}";
        Console.Error.WriteLine(ex);

        if (OperatingSystem.IsWindows())
        {
            MessageBox(IntPtr.Zero, message, "LibreAutomate 启动失败", 0x00000010);
        }
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
