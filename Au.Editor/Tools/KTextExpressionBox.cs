using System.Windows.Controls;
using Au.Controls;
using System.Windows;

namespace ToolLand;

/// <summary>
/// <see cref="KTextBox"/> that supports C# expression in text.
/// Just adds context menu item "Expressions..." that shows how to enter an expression; see <see cref="SetExpressionContextMenu"/>.
/// </summary>
class KTextExpressionBox : KTextBox {
	protected override void OnContextMenuOpening(ContextMenuEventArgs e) {
		SetExpressionContextMenu(this);
		base.OnContextMenuOpening(e);
	}
	
	/// <summary>
	/// Replaces the standard context menu. Adds standard items + item "Expressions..." that shows how to enter an expression.
	/// Does nothing if <c>t.ContextMenu?.HasItems == true</c>.
	/// </summary>
	public static void SetExpressionContextMenu(TextBox t) {
		if (t.ContextMenu?.HasItems == true) return;
		var m = t.xAddCutCopyPasteToContextMenu(addClear: t is KTextBox, setStateNow: true);
		m.xAddSeparator();
		m.xAdd("Expressions...", null, (_, _) => { dialog.show("Expressions", """
In this text field you can enter literal text or a C# expression.
Literal text in code will be enclosed in "" or @"" and escaped if need.
Expression will be added to code without changes.

Examples of all supported expressions:

@@expression
@"verbatim string"
$"interpolated string like {variable} text {expression} text"
$@"interpolated verbatim string like {variable} text {expression} text"

Real expression examples:

@"\bregular expression\b"
@@Environment.GetEnvironmentVariable("API_KEY")
@@"line1\r\nline2"
""", owner: t); });
	}
}

/// <summary>
/// Editable <b>ComboBox</b> that supports C# expression in text.
/// Just adds context menu item "Expressions..." that shows how to enter an expression.
/// </summary>
class KComboExpressionBox : ComboBox {
	public KComboExpressionBox() {
		IsEditable = true;
	}
	
	public override void OnApplyTemplate() {
		base.OnApplyTemplate();
		if (GetTemplateChild("PART_EditableTextBox") is TextBox t) {
			t.ContextMenu = new();
			t.ContextMenuOpening += (_, _) => KTextExpressionBox.SetExpressionContextMenu(t);
		}
	}
}
