/// <google WebView2 site:microsoft.com>WebView2<> is a web browser control based on Chromium and Microsoft Edge. NuGet: <+nuget>webview\Microsoft.Web.WebView2<>.

/*/ nuget webview\Microsoft.Web.WebView2; /*/

using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;

if (!_IsWebView2Available()) return;

WebView2 k = new();

var b = new wpfBuilder("Window").WinSize(800, 600);

b.StartStack();
b.AddButton(out var bBack, "Back", _ => { if (k.CanGoBack) k.GoBack(); }).Width(70).Disabled();
b.AddButton(out var bForward, "Forward", _ => { if (k.CanGoForward) k.GoForward(); }).Width(70).Disabled();
b.End();

b.Row(-1).Add(out k);

b.End();

b.Loaded += () => {
	k.CreationProperties = new() { UserDataFolder = folders.ThisAppDataRoaming + "WebView2" };
	k.Source = new("https://www.libreautomate.com");
};

k.CoreWebView2InitializationCompleted += (_, _) => {
	var core = k.CoreWebView2;
	
	core.HistoryChanged += (_, _) => {
		bBack.IsEnabled = core.CanGoBack;
		bForward.IsEnabled = core.CanGoForward;
	};
	
	//example of custom items in the context menu
	core.ContextMenuRequested += (_, e) => {
		var items = e.MenuItems;
		var mi1 = core.Environment.CreateContextMenuItem("Open in browser", null, CoreWebView2ContextMenuItemKind.Command);
		mi1.CustomItemSelected += (_, _) => { run.itSafe(core.Source); };
		items.Add(mi1);
	};
};

if (!b.ShowDialog()) return;

/// WebView2 is available on Windows 11 and most Windows 10 computers. On other computers need to download/install.

static bool _IsWebView2Available() {
	try { if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null) return true; } catch { }
	print.it("<>Info: To show web content in this app, download/install <google WebView2 site:microsoft.com>WebView2<>.");
	return false;
}
