using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Au.Controls;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Net;
using System.Windows.Media;
using ToolLand;

//CONSIDER: Add a menu-button. Menu:
//	Item "Request a recipe for this search query (uses internet)".

namespace LA;

class PanelHelp {
	KTreeView _tv;
	KTextBox _search;
	_Item _root;
	_Item _selectedItem;
	List<_Item> _aiResults;
	bool _showingResults;
	bool _openingItem;
	internal (Button back, Button forward, Button openInBrowser, Button copyForAiChat) buttons_;
	
	public PanelHelp() {
		P = new();
		P.UiaSetName("Help panel");
		
		var b = new wpfBuilder(P).Columns(-1, 0, 0).Brush(SystemColors.ControlBrush);
		
		b.R.Add(out _search).Tooltip("Find documentation.\n\nMiddle-click to clear.").UiaName("Find documentation"); //rejected: watermark
		_search.TextChanged += (_, _) => _SearchInNameOnSearchTextChanged();
		_search.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) _AiSearch(); };
		
		b.Options(modifyPadding: false, margin: new());
		
		b.xAddButtonIcon(EdIcons.AiSearch, _ => _AiSearch(), "AI search\n\nEnter").Margin(right: 3);
		
		_tv = new() { Name = "Help_TOC", SingleClickActivate = true, HotTrack = true, BackgroundColor = 0xf0f8e8, SmallIndent = true };
		b.Row(-1).Add(_tv);
		
		var tb = b.R.xAddToolBar();
		tb.UiaSetName("Debug_toolbar");
		buttons_.back = tb.AddButton("*EvaIcons.ArrowBack" + EdIcons.black, null, "Back", enabled: false);
		buttons_.forward = tb.AddButton("*EvaIcons.ArrowForward" + EdIcons.black, null, "Forward", enabled: false);
		buttons_.openInBrowser = tb.AddButton("*Modern.Browser" + EdIcons.black, null, "Open in web browser", enabled: false);
		buttons_.copyForAiChat = tb.AddButton("*Material.ContentCopy" + EdIcons.black, _CopyResultsForAiChat, "Copy results for AI chat", enabled: false);
		
		b.End();
		
		Panels.PanelManager["Help"].DontActivateFloating = e => e == _tv;
		
		P.IsVisibleChanged += (_, e) => {
			if (e.NewValue is bool y && y) {
				_Load();
				_tv.ItemActivated += e => _OpenItem(e.Item as _Item, false);
			}
		};
		
		//#if DEBUG
		//		_tv.ItemClick += e => {
		//			if (e.Button == MouseButton.Right) {
		//				var m = new popupMenu();
		//				m.Add("DEBUG", disable: true);
		//				//m["Print name words"] = o => _DebugGetWords();
		//				m.Show();
		//			}
		//		};
		//#endif
	}
	
	public UserControl P { get; }
	
	void _Load() {
		try {
			_root = new _Item(null, _DocKind.Folder);
			_TOC(folders.ThisAppBS + "toc.json", _root);
			
			var cookbookRoot = _root.FirstChild;
			var first = cookbookRoot.FirstChild;
			first.Remove();
			_root.AddChild(first, first: true);
			
			cookbookRoot.isExpanded = true;
			cookbookRoot.Next.isExpanded = true; //API
			
			static void _TOC(string jsonFile, _Item root) {
				var json = filesystem.loadText(jsonFile);
				var jRoot = JsonNode.Parse(json).AsArray();
				foreach (JsonObject j in jRoot) {
					var docKind = (string)j["name"] switch { "Cookbook" => _DocKind.Cookbook, "Articles" => _DocKind.Article, "Editor" => _DocKind.Editor, _ => _DocKind.Api };
					_Add(j, root, docKind);
				}
				
				static void _Add(JsonNode j, _Item ip, _DocKind docKind) {
					string name = (string)j["name"], href = (string)j["href"], symKind = null;
					if (docKind == _DocKind.Api) symKind = (string)j["kind"];
					if (j["items"] is JsonArray { Count: > 0 } ja) {
						var i = new _Item(name, _DocKind.Folder, symKind, href);
						ip.AddChild(i);
						foreach (var v in ja) {
							_Add(v, i, docKind);
						}
					} else {
						ip.AddChild(new _Item(name, docKind, symKind, href));
					}
				}
			}
			
			_tv.SetItems(_root.Children());
		}
		catch (Exception e1) { print.it(e1); }
	}
	
	void _OpenItem(_Item item, bool select) {
		if (item == null) return;
		if (item.dir) {
			if (item.href == null) {
				_tv.Expand(item, null);
				return;
			} else if (!item.isExpanded) {
				_tv.Expand(item, true);
			}
		}
		
		if (_showingResults) {
			_selectedItem = item.clonedFrom;
		} else {
			if (select) {
				_openingItem = true;
				_search.Text = "";
				_openingItem = false;
				_tv.Select(item);
			}
			
			_selectedItem = item;
		}
		
		var s1 = item.docKind switch { _DocKind.Cookbook => "cookbook/", _DocKind.Editor => "editor/", _DocKind.Article => "articles/", _ => "api/" };
		var href = item.docKind == _DocKind.Cookbook ? Uri.EscapeDataString(item.name.Replace("C#", "CSharp").Replace(".", "dot-")) + ".html" : item.href; //SYNC: cookbook replace name
		HelpUtil.AuHelp($"{s1}{href}"); //opens in the Read panel or in the web browser, depending on user settings
	}
	
	void _SearchInNameOnSearchTextChanged() {
		_aiResults = null;
		var s = _search.Text.Trim();
		if (s.Length < 2) {
			_tv.SetItems(_root.Children());
			_showingResults = false;
			buttons_.copyForAiChat.IsEnabled = false;
			if (!_openingItem && _selectedItem != null) {
				_tv.Select(_selectedItem);
			}
			return;
		}
		
		//print.clear();
		
		//CONSIDER: use Lucene.
		//rejected: use SQLite FTS5. Tried but didn't like. Lucene is much better.
		
		var root = _SearchContains(_root);
		_Item _SearchContains(_Item parent) {
			_Item R = null;
			for (var n = parent.FirstChild; n != null; n = n.Next) {
				_Item r = null;
				if (n.dir) {
					r = _SearchContains(n);
				}
				if (!n.dir || n.href != null) {
					if (n.name.Contains(s, StringComparison.OrdinalIgnoreCase)) r ??= n.Clone();
				}
				if (r != null) {
					if (R == null) {
						R = parent.Clone();
						R.isExpanded = true;
					}
					R.AddChild(r);
				}
			}
			return R;
		}
		
		//try stemmed fuzzy. Max Levenshtein distance 1 for a word.
		//	rejected: use FuzzySharp. For max distance 1 don't need it.
		bool fuzzy = root == null && s.Length >= 3;
		if (fuzzy) {
			string[] a1 = _Stem(s);
			root = _SearchFuzzy(_root);
			_Item _SearchFuzzy(_Item parent) {
				_Item R = null;
				for (var n = parent.FirstChild; n != null; n = n.Next) {
					_Item r = null;
					if (n.dir) {
						r = _SearchFuzzy(n);
					}
					if (!n.dir || n.href != null) {
						n.stemmedName ??= _Stem(n.name);
						bool allFound = true;
						foreach (var v1 in a1) {
							bool found = false;
							foreach (var v2 in n.stemmedName) {
								if (found = _Match(v1, v2)) break;
							}
							if (!(allFound &= found)) break;
						}
						if (allFound) r = n.Clone();
					}
					if (r != null) {
						if (R == null) {
							R = parent.Clone();
							R.isExpanded = true;
						}
						R.AddChild(r);
					}
				}
				return R;
			}
		}
		
		_tv.SetItems(root?.Children());
		_showingResults = true;
		
		static bool _Match(string s1, string s2) {
			if (s1[0] != s2[0] || Math.Abs(s1.Length - s2.Length) > 1) return false; //the first char must match
			if (s1.Length > s2.Length) Math2.Swap(ref s1, ref s2); //let s1 be the shorter
			
			int ib = 0, ie1 = s1.Length, ie2 = s2.Length;
			while (ib < s1.Length && s1[ib] == s2[ib]) ib++; //skip common prefix
			while (ie1 > ib && s1[ie1 - 1] == s2[--ie2]) ie1--; //skip common suffix
			
			int n = ie1 - ib;
			if (n == 1) return s1.Length == s2.Length || ib == ie1;
			return n == 0;
		}
	}
	
	string[] _Stem(string s) {
		if (_stem.stemmer == null) _stem = (new(), new(), new regexp(@"(*UCP)[^\W_]+"));
		_stem.a.Clear();
		foreach (var v in _stem.rx.FindAll(s.Lower(), 0)) {
			_stem.a.Add(_stem.stemmer.Stem(v));
		}
		return _stem.a.ToArray();
	}
	(Libs.Porter2Stemmer.EnglishPorter2Stemmer stemmer, List<string> a, regexp rx) _stem;
	
	async void _AiSearch() {
		var query = _search.Text.Trim();
		if (query.Length < 2) return;
		
		AI.AiModel.ApiKeys = App.Settings.ai_ak;
		var emModel = AI.AiModel.GetModel<AI.AiEmbeddingModel>(App.Settings.ai_modelEmbed, displayName: true);
		if (emModel == null) {
			_AiSettingsError($"Please go to Options > AI and select models for documentation search.");
			return;
		}
		var rrModel = AI.AiModel.GetModel<AI.AiRerankModel>(App.Settings.ai_modelRerank, displayName: true);
		
		try {
			_ctsAiSearch?.Cancel();
			_ctsAiSearch?.Dispose();
			_ctsAiSearch = new();
			var cancel = _ctsAiSearch.Token;
			
			var em = new AI.Embeddings(emModel);
			var ems = await Task.Run(() => em.GetDocsEmbeddings(cancel));
			
			var r1 = _search.RectInScreen();
			using var osd = osdText.showText("Searching.\nClick to cancel.", -1, new(r1.right, r1.bottom), showMode: OsdMode.ThisThread);
			osd.Clicked += (_, _) => { _ctsAiSearch?.Cancel(); };
			
			int take = 15 + Math.Sqrt(query.Count(c => c <= ' ') + query.Count(c => c is ',' or '.' or ';') * 4).ToInt();
			
			var queryVector = await Task.Run(() => em.CreateEmbedding(query, cancel));
			var topAll = em.GetTopMatches(queryVector, ems, rrModel == null ? 50 : 150);
			if (topAll.Count == 0) return;
			
			Dictionary<string, (float score, bool summary)> dTop = [];
			foreach (var v in topAll) {
				var name = v.f.name;
				bool isSum = name[0] == '+';
				if (isSum) name = name[1..];
				dTop.TryAdd(name, (v.score, isSum));
			}
			var aTop = dTop.Select(o => (name: o.Key, v: o.Value)).OrderByDescending(o => o.v.score).ToArray();
			
			List<_Item> a = [];
			if (rrModel != null) {
				osd.Text = "Reranking.\nClick to cancel.";
				await Task.Run(() => {
					var names = aTop.Select(o => o.name).ToArray();
					var texts = em.GetDocsTexts(names);
					var headers = rrModel.GetHeaders();
					var post = rrModel.GetPostData(query, texts);
					var j = rrModel.Post(post, headers, cancel).Json();
					//print.it(j.ToJsonString(new() { WriteIndented = true }));
					var ar = rrModel.GetResults(j);
					int i = 0;
					float firstScore = 0;
					foreach (var v in ar) {
						if (i == 0) firstScore = v.score;
						if (i++ > take || firstScore - v.score > .3f || v.score < .45f) break;
						_FindAdd(names[v.index]);
					}
				});
			} else {
				float firstScore = aTop[0].v.score;
				foreach (var v in aTop) {
					if (v.v.score < firstScore - .2f) break;
					_FindAdd(v.name);
				}
			}
			
			void _FindAdd(string s) {
				if (s.Starts("[cookbook]")) {
					s = s[11..].Replace("CSharp", "C#").Replace("dot-", "."); //SYNC: cookbook replace name
					if (_FindRecipe(s, true) is { } r) {
						a.Add(r);
					} else {
						Debug_.Print(s);
					}
				} else {
					bool isAPI = s[0] != '[';
					var r = _root.Children().ElementAt(isAPI ? 2 : s[1] == 'a' ? 3 : 4);
					if (isAPI) {
						int i = s.Starts("Au.More") ? 7
							: s.Starts("Au.Types") ? 8
							: s.Starts("Au.Triggers") ? 11
							: 2;
						var ns = s[..i];
						r = r.Children().First(o => o.name == ns);
						r = r.Descendants().First(o => o.ApiFullNameEquals(s));
						s = s[(i + 1)..];
						r = r.Clone(s);
					} else {
						string name = s = s[(s.IndexOf(' ') + 1)..], section = null;
						int i = s.Find(" | ");
						if (i > 0) (section, name) = (name[(i + 3)..], name[..i]);
						if (section == "other") section = null;
						
						r = r.Children().First(o => o.name == name);
						
						string href = r.href;
						if (section != null) {
							s = $"{r.name} | {section}";
							section = section.Lower().Replace(' ', '-').RxReplace(@"[^[:alnum:]\-]", "");
							href = $"{href}#{section}";
						} else {
							s = r.name;
						}
						
						r = new(s, r.docKind, null, href, r);
					}
					a.Add(r);
				}
			}
			
			_tv.SetItems(a);
			_aiResults = a;
			_showingResults = true;
			buttons_.copyForAiChat.IsEnabled = a.Count > 0;
		}
		catch (OperationCanceledException etc) { if (etc.InnerException is TimeoutException) print.it(etc.Message); }
		catch (InvalidCredentialException) {
			var api = emModel.api;
			_AiSettingsError($"Please go to Options > AI and set the API key for {api}.\nYou can create an API key in your account on the {api} website.");
		}
		catch (Exception e1) { print.it(e1); }
		
		void _AiSettingsError(string text) {
			if (!dialog.showOkCancel("AI search error", text, owner: P)) return;
			DOptions.AaShow(DOptions.EPage.AI);
		}
	}
	CancellationTokenSource _ctsAiSearch;
	
	void _CopyResultsForAiChat(Button b) {
		if (!(_aiResults?.Count > 0)) return;
		print.clear();
		
		//TODO
	}
	
	/// <summary>
	/// Finds and opens a recipe.
	/// <para>
	/// If called in LA tools process or in a non-main LA thread, runs async in the main process/thread.
	/// </para>
	/// </summary>
	/// <param name="name">Wildcard or start or any substring of recipe name.</param>
	public static void OpenRecipe(string name) {
		if (process.IsLaMainThread_) Panels.Help._OpenRecipe(name);
		else if (process.IsLaProcess_) App.Dispatcher.InvokeAsync(() => OpenRecipe(name));
		else WndCopyData.Send<char>(ScriptEditor.WndMsg_, 18, name);
	}
	
	void _OpenRecipe(string name) {
		Panels.PanelManager[P].Visible = true;
		_OpenItem(_FindRecipe(name), true);
	}
	
	_Item _FindRecipe(string s, bool exact = false) {
		var d = _root.Descendants().Where(o => o.docKind is _DocKind.Cookbook);
		if (exact) return d.FirstOrDefault(o => !o.dir && o.name == s);
		return d.FirstOrDefault(o => !o.dir && o.name.Like(s, true))
			?? d.FirstOrDefault(o => !o.dir && o.name.Starts(s, true))
			?? d.FirstOrDefault(o => !o.dir && o.name.Find(s, true) >= 0);
	}
	
	public void MenuCommand() {
		Panels.PanelManager[P].Visible = true;
		_search.Focus();
		
		if (_selectedItem == null && !App.Settings.doc_web) {
			_OpenItem(_root.FirstChild, true);
		}
	}
	
	//#if DEBUG
	//	void _DebugGetWords() {
	//		print.clear();
	//		var hs = new HashSet<string>();
	//		foreach (var recipe in _root.Descendants().Where(o => o.docKind is _DocKind.Cookbook)) {
	//			var a = _Stem(recipe.name);
	//			foreach (var s in a)
	//				if (s.Length > 2 && !s[0].IsAsciiDigit()) hs.Add(s);
	//		}
	//		print.it(hs.OrderBy(o => o, StringComparer.OrdinalIgnoreCase));
	//	}
	//#endif
	
	enum _DocKind : byte { Folder, Cookbook, Api, Editor, Article }
	
	class _Item : TreeBase<_Item>, ITreeViewItem {
		internal readonly string name;
		internal readonly _DocKind docKind;
		internal readonly string symKind;
		internal readonly string href;
		internal readonly _Item clonedFrom;
		internal bool isExpanded;
		internal string[] stemmedName;
		
		public _Item(string name, _DocKind docKind, string symKind = null, string href = null, _Item clonedFrom = null) {
			this.name = name;
			this.docKind = docKind;
			this.symKind = symKind;
			this.href = href;
			this.clonedFrom = clonedFrom;
		}
		
		public _Item Clone(string newName = null) => new(newName ?? name, docKind, symKind, href, this);
		
		internal bool dir => docKind == _DocKind.Folder;
		
		#region ITreeViewItem
		
		string ITreeViewItem.DisplayText => name;
		
		object ITreeViewItem.Image
			=> docKind switch {
				_DocKind.Folder => symKind != null ? _KindIcon : EdIcons.FolderArrow(isExpanded),
				_DocKind.Cookbook => name == "Documentation" ? EdIcons.Help : "*BoxIcons.RegularCookie" + EdIcons.darkYellow,
				_DocKind.Api => _KindIcon,
				_DocKind.Editor => "*Material.ApplicationOutline" + EdIcons.blue,
				_DocKind.Article => "*PhosphorIcons.Article" + EdIcons.black,
				_ => null
			};
		
		string _KindIcon => symKind switch {
			"Namespace" => "resources/ci/namespace.xaml",
			"Class" => "resources/ci/class.xaml",
			"Struct" => "resources/ci/structure.xaml",
			"Enum" => "resources/ci/enum.xaml",
			"Interface" => "resources/ci/interface.xaml",
			"Delegate" => "resources/ci/delegate.xaml",
			"Method" or "Constructor" => "resources/ci/method.xaml",
			"Property" or "Indexer" => "resources/ci/property.xaml",
			"Event" => "resources/ci/event.xaml",
			"Field" => "resources/ci/field.xaml",
			"Operator" => "resources/ci/operator.xaml",
			_ => null
		};
		
		void ITreeViewItem.SetIsExpanded(bool yes) { isExpanded = yes; }
		
		bool ITreeViewItem.IsExpanded => isExpanded;
		
		IEnumerable<ITreeViewItem> ITreeViewItem.Items => base.Children();
		
		bool ITreeViewItem.IsFolder => dir;
		
		#endregion
		
		public bool ApiFullNameEquals(string s) {
			if (!s.Ends(name)) return false;
			int i1 = s.Length - name.Length - 1;
			if (s[i1] != '.') return false;
			int level = Level; if (!(level is 3 or 4)) return false;
			var t = this;
			if (level == 4) { //member of a type
				t = Parent; //type
				int i2 = i1 - t.name.Length;
				if (!s.Eq(i2, t.name) || !s.Eq(--i2, '.')) return false;
				i1 = i2;
			}
			t = t.Parent; //namespace
			if (i1 != t.name.Length || !s.Starts(t.name)) return false;
			return true;
		}
	}
}
