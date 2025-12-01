extern alias CAW;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.Runtime.Loader;
using Au.Controls;

namespace LA;

class PanelRead {
	WebBrowser _wb;
	
	public PanelRead() {
		//P.UiaSetName("Read panel"); //no UIA element for Panel
#if DEBUG
		_PreviewCurrentRecipeScript();
#endif
	}
	
	public Grid P { get; } = new();
	
	void _BeforeOpeningArticle() {
		var p = Panels.PanelManager[P];
		p.Visible = true;
		if (p.Floating) {
			wnd w = p.Content.Hwnd(), wmain = App.Hmain;
			if (w.IsMinimized) {
				w.ShowNotMinimized();
			} else if (!w.IsOwnedBy(wmain, 0) && !w.IsTopmost) {
				if (w.ClientRectInScreen.IntersectsWith(wmain.ClientRectInScreen)) w.ActivateL(true);
			}
		}
	}
	
#if DEBUG
	unsafe void _PreviewCurrentRecipeScript() {
		string prevText = null;
		SciCode prevDoc = null;
		
		App.Timer1sWhenVisible += () => {
			if (!P.IsVisible) return;
			if (App.Model.WorkspaceName != "Cookbook") {
				if (!(App.Model.WorkspaceName == "ok" && App.Model.CurrentFile is { IsExternal: true, IsScript: true } cf && cf.Ancestors().Any(o => o.Name is "cookbook_files"))) return;
			}
			var doc = Panels.Editor.ActiveDoc;
			if (doc == null || !doc.EFile.IsScript || doc.EFile.Parent.Name == "-") return;
			
			string text = doc.aaaText;
			if (text == prevText) return;
			if (_IsCaretInPossiblyUnfinishedTag(doc, text)) return; //avoid printing debug info for invalid links etc
			prevText = text;
			
			string refresh = null;
			if (doc == prevDoc) {
				refresh = "?refresh=true";
			} else {
				prevDoc = doc;
			}
			OpenDocUrl(DocsHttpServer.LocalBaseUri + "cookbook/preview.html" + refresh);
		};
		
		bool _IsCaretInPossiblyUnfinishedTag(SciCode doc, string text) {
			int i = doc.aaaCurrentPos16;
			i = i == 0 ? -1 : text.LastIndexOfAny(['<', '>', '\n'], i - 1);
			if (i > 0 && text[i] == '<' && text.Eq(text.LastIndexOf('\n', i) + 1, "///")) {
				//print.it("tag");
				return true;
			}
			return false;
		}
	}
#endif
	
	public void OpenDocUrl(string uri) {
		if (_wb == null) _CreateWebBrowser();
		_BeforeOpeningArticle();
		_wb.Navigate(uri);
	}
	
	void _CreateWebBrowser() {
		//_WebBrowserRegistrySettingsForThisProcess.EnsureValid();
		
		//P.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
		_wb = new() { ObjectForScripting = new _WebBrowserBridge() };
		P.Children.Add(_wb);
		
		_wb.Navigating += (sender, e) => {
			//bad: WebBrowser on every navigation briefly displays "wait" cursor. Even on link click.
			//	Unsuccessfully tried to prevent it. Can temporarily hide (API ShowCursor), but it's worse.
			
			var uri = e.Uri.ToString();
			//print.it("Navigating", uri);
			
			var baseUri = DocsHttpServer.LocalBaseUri;
			if (baseUri == null) {
				e.Cancel = true;
				run.itSafe(uri);
				return;
			}
			
			if (uri.Starts(baseUri)) {
#if DEBUG
				if (e.Uri.Query == "?refresh=true" && e.Uri.AbsolutePath.Ends("/preview.html") && true == _wb.Source?.AbsolutePath.Ends("/preview.html")) {
					_scrollPos = _GetScrollPos();
					if (_scrollPos > 0) {
						if (_ies.Is0) _ies = ((wnd)_wb.Handle).Child(cn: "Internet Explorer_Server");
						_ies.Send(Api.WM_SETREDRAW);
					}
					//note: don't use _wb.Refresh. It's several times slower, and there is no LoadCompleted event.
					//rejected: autoscroll. Even if works perfectly, often it is more annoying than useful.
				}
#endif
				return;
			}
			
			e.Cancel = true;
			
			if (!uri.Starts("https://www.libreautomate.com/") || 0 == uri.Eq(30, false, "api", "editor", "articles", "cookbook")) {
				run.itSafe(uri);
				return;
			}
			
			try {
				_wb.Navigate(baseUri + uri[30..]);
			}
			catch (Exception ex) { print.warning(ex); }
		};
		
		_wb.Navigated += (sender, e) => {
			//print.it("Navigated", _wb.CanGoBack, _wb.CanGoForward);
			P.Dispatcher.InvokeAsync(_LoadCompleted2); //in some cases this event is after LoadCompleted
		};
		
		_wb.LoadCompleted += (_, e) => {
			if (e.Uri == null) return;
			//print.it("LoadCompleted", _wb.CanGoBack, _wb.CanGoForward, e.Uri);
			_Zoom(VisualTreeHelper.GetDpi(_wb).PixelsPerDip);
#if DEBUG
			if (_scrollPos > 0 && e.Uri.ToString().Ends("/preview.html?refresh=true")) {
				_SetScrollPos(_scrollPos);
				_ies.Send(Api.WM_SETREDRAW, 1);
				unsafe { Api.RedrawWindow(_ies, flags: Api.RDW_ERASE | Api.RDW_FRAME | Api.RDW_INVALIDATE | Api.RDW_ALLCHILDREN); }
			}
#endif
			P.Dispatcher.InvokeAsync(_LoadCompleted2);
		};
		
		var tb = Panels.Help.buttons_.toolbar.ToolBars[0];
		Panels.Help.buttons_.back = tb.AddButton("*EvaIcons.ArrowBack" + EdIcons.black, null, "Back", enabled: false);
		Panels.Help.buttons_.forward = tb.AddButton("*EvaIcons.ArrowForward" + EdIcons.black, null, "Forward", enabled: false);
		Panels.Help.buttons_.openInBrowser = tb.AddButton("*Modern.Browser" + EdIcons.black, null, "Open in web browser", enabled: false);
		Panels.Help.buttons_.back.Click += (_, _) => { try { _wb.GoBack(); } catch { } };
		Panels.Help.buttons_.forward.Click += (_, _) => { try { _wb.GoForward(); } catch { } };
		Panels.Help.buttons_.openInBrowser.Click += (_, _) => { _OpenInWebBrowser(); };
		Panels.Help.buttons_.toolbar.Visibility = Visibility.Visible;
		
		void _LoadCompleted2() {
			Panels.Help.buttons_.back.IsEnabled = _wb.CanGoBack;
			Panels.Help.buttons_.forward.IsEnabled = _wb.CanGoForward;
			Panels.Help.buttons_.openInBrowser.IsEnabled = true;
		}
		
		_wb.DpiChanged += (_, e) => { _Zoom(e.NewDpi.PixelsPerDip); };
		
		void _Zoom(double zoom) {
			try {
				//if (_wb.Document != null) _wb.InvokeScript("eval", $"document.body.style.zoom = {zoom};"); //does not work well
				
				dynamic iwb = _wb.GetType().InvokeMember("AxIWebBrowser2", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic, null, _wb, null);
				object o = (zoom * 100).ToInt();
				iwb.ExecWB(63, 2, o, (nint)0); //OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DONTPROMPTUSER
			}
			catch (Exception ex) { Debug_.Print(ex); }
		}
	}
	
#if DEBUG
	wnd _ies;
	int _scrollPos;
	
	int _GetScrollPos() {
		try {
			if (_wb.Document is api.IHTMLDocument3 d && d.documentElement is api.IHTMLElement2 e) {
				return e.scrollTop;
			}
		}
		catch { }
		return 0;
	}
	
	void _SetScrollPos(int pos) {
		try {
			if (_wb.Document is api.IHTMLDocument3 d && d.documentElement is api.IHTMLElement2 e) {
				e.scrollTop = pos;
			}
		}
		catch { }
	}
	
	unsafe class api : NativeApi {
		[ComImport, Guid("3050f485-98b5-11cf-bb82-00aa00bdce0b")]
		internal interface IHTMLDocument3 {
			void releaseCapture();
			void recalc(short fForce);
			nint createTextNode(string text);
			object documentElement { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
			//others deleted
		}
		
		[ComImport, Guid("3050f434-98b5-11cf-bb82-00aa00bdce0b")]
		internal interface IHTMLElement2 {
			string scopeName { get; }
			void setCapture(short containerCapture);
			void releaseCapture();
			object onlosecapture { set; get; }
			string componentFromPoint(int x, int y);
			void doScroll(object component);
			object onscroll { set; get; }
			object ondrag { set; get; }
			object ondragend { set; get; }
			object ondragenter { set; get; }
			object ondragover { set; get; }
			object ondragleave { set; get; }
			object ondrop { set; get; }
			object onbeforecut { set; get; }
			object oncut { set; get; }
			object onbeforecopy { set; get; }
			object oncopy { set; get; }
			object onbeforepaste { set; get; }
			object onpaste { set; get; }
			nint currentStyle { get; }
			object onpropertychange { set; get; }
			nint getClientRects();
			nint getBoundingClientRect();
			void setExpression(string propname, string expression, string language);
			object getExpression(string propname);
			short removeExpression(string propname);
			short tabIndex { set; get; }
			void focus();
			string accessKey { set; get; }
			object onblur { set; get; }
			object onfocus { set; get; }
			object onresize { set; get; }
			void blur();
			void addFilter([MarshalAs(UnmanagedType.IUnknown)] object pUnk);
			void removeFilter([MarshalAs(UnmanagedType.IUnknown)] object pUnk);
			int clientHeight { get; }
			int clientWidth { get; }
			int clientTop { get; }
			int clientLeft { get; }
			short attachEvent(string @event, [MarshalAs(UnmanagedType.IDispatch)] object pDisp);
			void detachEvent(string @event, [MarshalAs(UnmanagedType.IDispatch)] object pDisp);
			object readyState { get; }
			object onreadystatechange { set; get; }
			object onrowsdelete { set; get; }
			object onrowsinserted { set; get; }
			object oncellchange { set; get; }
			string dir { set; get; }
			[return: MarshalAs(UnmanagedType.IDispatch)] object createControlRange();
			int scrollHeight { get; }
			int scrollWidth { get; }
			int scrollTop { set; get; }
			int scrollLeft { set; get; }
			void clearAttributes();
			void mergeAttributes(nint mergeThis);
			object oncontextmenu { set; get; }
			nint insertAdjacentElement(string where, nint insertedElement);
			nint applyElement(nint apply, string where);
			string getAdjacentText(string where);
			string replaceAdjacentText(string where, string newText);
			short canHaveChildren { get; }
			int addBehavior(string bstrUrl, in object pvarFactory);
			short removeBehavior(int cookie);
			nint runtimeStyle { get; }
			[return: MarshalAs(UnmanagedType.IDispatch)] object get_behaviorUrns();
			string tagUrn { set; get; }
			object onbeforeeditfocus { set; get; }
			int readyStateValue { get; }
			nint getElementsByTagName(string v);
		}
		
	}
#endif
	
	void _OpenInWebBrowser() {
		if (_wb.Source is { } uri) run.itSafe(HelpUtil.AuHelpUrl(uri.AbsolutePath[1..]));
	}
	
	#region called by JavaScript
	
	//contextFlag: 0: no selection, 1: selection, 2: inside <pre> (and no selection)
	internal void ShowContextMenu_(int x, int y, int contextFlag, string selectedText) {
		//print.it(contextFlag, selectedText, _wb.Source.AbsolutePath);
		
		var m = new popupMenu();
		
		m["Back\tAlt+Left", disable: !_wb.CanGoBack] = o => { try { _wb.GoBack(); } catch { } };
		m["Forward\tAlt+Right", disable: !_wb.CanGoForward] = o => { try { _wb.GoForward(); } catch { } };
		m["Open in web browser"] = o => { _OpenInWebBrowser(); };
		m.Separator();
		m[contextFlag == 2 ? "Copy code" : "Copy\tCtrl+C", disable: contextFlag is 0] = o => { try { clipboard.text = _SelectedText(); } catch { } };
		m["New script", disable: contextFlag != 2] = o => {
			var name = pathname.getNameNoExt(Uri.UnescapeDataString(_wb.Source.AbsolutePath));
			App.Model.NewItem("Script.cs", null, name + ".cs", true, new(true, _SelectedText()));
		};
		
		m.Show(owner: P);
		
		string _SelectedText() {
			//restore code indentation tabs. The script replaced tabs with spaces, because IE does not support CSS tab-size.
			var s = selectedText;
			if (App.Settings.ci_formatTabIndent) s = s.RxReplace(@"(?m)^(    )+", m => new string('\t', m.Length / 4));
			return s;
		}
	}
	
	#endregion
}

#pragma warning disable CS1591 //Missing XML comment for publicly visible type or member
[ComVisible(true)]
public class _WebBrowserBridge { //note: must be public
	#region called by JavaScript
	
	public void ShowContextMenu(int x, int y, int contextFlag, string selectedText) {
		Panels.Read.ShowContextMenu_(x, y, contextFlag, selectedText);
	}
	
	public void NugetLinkClicked(string package) {
		DNuget.ShowSingle(package);
	}
	
	#endregion
}

class DocsHttpServer : HttpServerSession {
	static bool s_running;
	static int s_port;
	
	public static void StartOrSwitch() {
		if (App.Settings.doc_web || s_running) {
			_Switch();
		} else {
			s_running = true;
			run.thread(() => {
				try {
					Listen<DocsHttpServer>(0, "127.0.0.1", listener => {
						s_port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
						_Switch();
					});
				}
				catch (Exception ex) {
					print.warning(ex);
				}
				App.Settings.doc_web = true;
				_Switch();
				s_running = false;
			}, sta: false);
		}
	}
	
	public static string LocalBaseUri { get; private set; }
	
	static void _Switch() {
		if (App.Settings.doc_web) {
			LocalBaseUri = null;
			HelpUtil.AuHelpEvent_ -= AuHelpEvent;
		} else {
			LocalBaseUri = $"http://127.0.0.1:{s_port}/";
			HelpUtil.AuHelpEvent_ += AuHelpEvent;
		}
	}
	
	static void AuHelpEvent(HelpUtil.AuHelpEventArgs_ e) {
		//print.it("OnAuHelp", e.Url);
		e.Cancel = true;
		Panels.Read.OpenDocUrl(e.Url);
	}
	
	protected override void MessageReceived(HSMessage m, HSResponse r) {
		if (m.Method != "GET") { r.Status = System.Net.HttpStatusCode.MethodNotAllowed; return; }
		
		var path = m.TargetPath; if (path.Ends('/')) path += "index.html";
		path = path[1..];
		//print.it(path);
		
#if DEBUG
		if (path == "cookbook/preview.html") {
			r.SetContentText(_GetPreviewHtml(), "text/html; charset=utf-8");
			return;
		}
		//if (path == "styles/docfx.vendor.min.css") {
		//	r.SetContentText(filesystem.loadText(@"C:\Temp\Au\DocFX\site\styles\docfx.vendor.min.css"), "text/css");
		//	r.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
		//	return;
		//}
#endif
		
		lock (typeof(DocsHttpServer)) { //lock sqlite. WebBrowser uses 2 HTTP connections when retrieving a html file + its css etc files the first time. Then we have 2 DocsHttpServer instances running simultaneously in 2 threads.
			using var db = new sqlite(folders.ThisAppBS + "doc-html.db", SLFlags.SQLITE_OPEN_READONLY); //fast, don't keep open
			if (db.Get(out byte[] content, "SELECT text FROM doc WHERE name=?", path)) {
				r.Content = content;
				var ext = pathname.getExtension(path).Lower();
				var ct = ext switch {
					".html" => "text/html; charset=utf-8",
					".css" => "text/css",
					".js" => "text/javascript",
					".png" => "image/png",
					_ => null
				};
				if (ct != null) r.Headers["Content-Type"] = ct; else if (!(ext is ".eot")) Debug_.Print(path);
			} else {
				Debug_.Print("NOT FOUND: " + path);
				r.Status = System.Net.HttpStatusCode.NotFound;
			}
		}
	}
	
#if DEBUG
	static string _GetPreviewHtml() {
		//print.clear();
		try {
			var doc = Panels.Editor.ActiveDoc;
			string name = doc.EFile.DisplayName, code = doc.Dispatcher.Invoke(() => doc.aaaText);
			
			if (_RecipeCodeToHtml == null) {
				AssemblyLoadContext.Default.LoadFromAssemblyPath(@"C:\code\ok\.nuget\-\Markdig.dll");
				var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(@"C:\code\ok\dll\AuDocsLib.dll");
				_RecipeCodeToHtml = asm.GetType("ADL.AuDocsShared").GetMethod("RecipeCodeToHtml").CreateDelegate<Func<string, string, string>>();
			}
			//perf.first();
			var html = _RecipeCodeToHtml(name, code);
			//perf.nw();
			//print.it(html);
			return html;
		}
		catch (Exception ex) { print.it(ex); throw; }
	}
	
	static Func<string, string, string> _RecipeCodeToHtml;
#endif
}

//static file class _WebBrowserRegistrySettingsForThisProcess {
//	public static void EnsureValid() {
//		var ieVer = _GetIEVersion()?.Major ?? 0; if (ieVer < 8) return;
//		ieVer *= 1000;
//		var e = process.thisExeName;

//		_Set("FEATURE_BROWSER_EMULATION", ieVer);
//		//_Set("FEATURE_96DPI_PIXEL", 1); //documented as obsolete. Does not support per-monitor DPI.

//		void _Set(string key, int value) {
//			var rk = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\" + key;
//			if (Registry.GetValue(rk, e, null) is int t && t == value) return;
//			Registry.SetValue(rk, e, value);
//		}
//	}

//	static Version _GetIEVersion() {
//		var s = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer", "svcVersion", null) as string; //IE10+
//		s ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer", "Version", null) as string;
//		return s == null ? null : new(s);
//	}
//}
