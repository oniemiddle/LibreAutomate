/// Generates article "Menu commands" from current LA UI (Panels.Menu).
/// Executed by `Au docs.cs`.

/*/ role editorExtension; testInternal Au.Editor; r Au.Editor.dll; r Au.Controls.dll; /*/

using LA;
using System.Windows.Controls;

//print.clear();
_BuidlDynamicMenus();
string title = "Menu commands";
StringBuilder b = new($"""
---
uid: menu_commands
---

<!-- auto-generated -->

# {title}


""");
foreach (MenuItem v in Panels.Menu.Items) {
	if (v.Role is not MenuItemRole.TopLevelHeader) break;
	_MenuItem(v, 0);
}
//print.it(b);
//print.scrollToTop();
_Save();

void _MenuItem(MenuItem mi, int level) {
	var text = mi.Header.ToString().Replace("_", "");
	b.Append(' ', level * 2).Append("- ").Append(_Escape(text));
	if (mi.IsCheckable) b.Append("  (option)");
	if (mi.ToolTip?.ToString() is string tt) {
		var lines = tt.Lines();
		for (int i = 0; i < lines.Length; i++) {
			var line = lines[i];
			if (i == 0 && line.Starts(text)) continue;
			line = _Escape(line);
			b.AppendLine("  ").Append(' ', (level + 1) * 2).Append(line);
		}
	}
	b.AppendLine();
	if (mi.Role is MenuItemRole.SubmenuHeader or MenuItemRole.TopLevelHeader) {
		foreach (var v in mi.Items.OfType<MenuItem>()) {
			_MenuItem(v, level + 1);
		}
	}
}

static string _Escape(string s) {
	s = s.Replace("<", @"\<");
	if (s.Contains('"')) {
		s = s.RxReplace(@"""(image:|\*icon)""", "`$0`");
		//print.it(s);
	}
	return s;
}

void _BuidlDynamicMenus() {
	var mi = App.Commands["New"].MenuItem;
	FilesModel.FillMenuNew(mi);
}

void _Save() {
	var dir = @"C:\code\au\Other\DocFX\_doc\editor\";
	var menusMd = dir + title + ".md";
	filesystem.saveText(menusMd, b.ToString());
	//var tocYml = dir + "toc.yml"; //added manually
}
