using ReverseMarkdown;
using HtmlAgilityPack;
using System.Net;

namespace ReverseMarkdown.Converters;

public interface IConverter {
	string Convert(HtmlNode node);
}

public abstract class ConverterBase : IConverter {
	protected ConverterBase(Converter converter) {
		Converter = converter;
	}
	
	protected Converter Converter { get; }
	
	protected string TreatChildren(HtmlNode node) {
		var result = string.Empty;
		
		return !node.HasChildNodes
			? result
			: node.ChildNodes.Aggregate(result, (current, nd) => current + Treat(nd));
	}
	
	private string Treat(HtmlNode node) {
		// TrimNewLine(node);
		var converter = Converter.Lookup(node.Name);
		return converter.Convert(node);
	}
	
	private static void TrimNewLine(HtmlNode node) {
		if (!node.HasChildNodes) return;
		
		if (node.FirstChild.Name == "#text" && (node.FirstChild.InnerText.StartsWith("\r\n") || node.FirstChild.InnerText.StartsWith("\n"))) {
			node.FirstChild.InnerHtml = node.FirstChild.InnerHtml.TrimStart('\r').TrimStart('\n');
		}
		
		if (node.LastChild.Name == "#text" && (node.LastChild.InnerText.EndsWith("\r\n") || node.LastChild.InnerText.EndsWith("\n"))) {
			node.LastChild.InnerHtml = node.LastChild.InnerHtml.TrimEnd('\r').TrimEnd('\n');
		}
	}
	
	protected static string ExtractTitle(HtmlNode node) {
		return node.GetAttributeValue("title", "");
	}
	
	protected static string DecodeHtml(string html) {
		return System.Net.WebUtility.HtmlDecode(html);
	}
	
	public abstract string Convert(HtmlNode node);
}

public class A : ConverterBase {
	public A(Converter converter)
		: base(converter) {
		Converter.Register("a", this);
	}
	
	public override string Convert(HtmlNode node) {
		var name = TreatChildren(node).Trim();
		
		var hasSingleChildImgNode = node.ChildNodes.Count == 1 && node.ChildNodes.Count(n => n.Name.Contains("img")) == 1;
		
		var href = node.GetAttributeValue("href", string.Empty).Trim().Replace("(", "%28").Replace(")", "%29").Replace(" ", "%20");
		var title = ExtractTitle(node);
		title = title.Length > 0 ? $" \"{title}\"" : "";
		var scheme = StringUtils.GetScheme(href);
		
		var isRemoveLinkWhenSameName = Converter.Config.SmartHrefHandling
									&& scheme != string.Empty
									&& Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute)
									&& (
										href.Equals(name, StringComparison.OrdinalIgnoreCase)
										|| href.Equals($"tel:{name}", StringComparison.OrdinalIgnoreCase)
										|| href.Equals($"mailto:{name}", StringComparison.OrdinalIgnoreCase)
									);
		
		if (href.StartsWith("#") //anchor link
			|| isRemoveLinkWhenSameName
			|| string.IsNullOrEmpty(href)) //We would otherwise print empty () here...
		{
			return name;
		}
		
		var useHrefWithHttpWhenNameHasNoScheme = Converter.Config.SmartHrefHandling &&
												 (scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
												 && string.Equals(href, $"{scheme}://{name}", StringComparison.OrdinalIgnoreCase);
		
		// if the anchor tag contains a single child image node don't escape the link text
		var linkText = hasSingleChildImgNode ? name : StringUtils.EscapeLinkText(name);
		
		if (string.IsNullOrEmpty(linkText)) {
			return href;
		}
		
		return useHrefWithHttpWhenNameHasNoScheme ? href : $"[{linkText}]({href}{title})";
	}
}

public class Blockquote : ConverterBase {
	public Blockquote(Converter converter) : base(converter) {
		Converter.Register("blockquote", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node).TrimStart('\r', '\n');
		
		// get the lines based on carriage return and prefix "> " to each line
		var lines = content.ReadLines().Select(item => "> " + item + Environment.NewLine);
		
		// join all the lines to a single line
		var result = lines.Aggregate(string.Empty, (current, next) => current + next);
		
		return $"{Environment.NewLine}{Environment.NewLine}{result}{Environment.NewLine}";
	}
}

public class Br : ConverterBase {
	string[] parentList = new string[] { "strong", "b", "em", "i" };
	
	public Br(Converter converter) : base(converter) {
		Converter.Register("br", this);
	}
	
	public override string Convert(HtmlNode node) {
		var parentName = node.ParentNode.Name.ToLowerInvariant();
		if (parentList.Contains(parentName)) {
			return "";
		}
		
		return Environment.NewLine;
	}
}

public class ByPass : ConverterBase {
	public ByPass(Converter converter) : base(converter) {
		Converter.Register("#document", this);
		Converter.Register("html", this);
		Converter.Register("body", this);
		Converter.Register("span", this);
		Converter.Register("thead", this);
		Converter.Register("tbody", this);
	}
	
	public override string Convert(HtmlNode node) {
		return TreatChildren(node);
	}
}

public class Code : ConverterBase {
	public Code(Converter converter) : base(converter) {
		Converter.Register("code", this);
	}
	
	public override string Convert(HtmlNode node) {
		return "`" + WebUtility.HtmlDecode(node.InnerText) + "`";
	}
}

public class Div : ConverterBase {
	List<string> blockTags = new List<string>{"pre","p","ol","ul","table"};
	
	public Div(Converter converter) : base(converter) {
		Converter.Register("div", this);
		Converter.Register("details", this);
		Converter.Register("summary", this);
	}
	
	public override string Convert(HtmlNode node) {
		string content;
		
		do {
			if (node.ChildNodes.Count == 1 && node.FirstChild.Name == "div") {
				node = node.FirstChild;
				continue;
			}
			
			content = TreatChildren(node);
			break;
		} while (true);
		
		// if there is a block child then ignore adding the newlines for div
		if ((node.ChildNodes.Count == 1 && blockTags.Contains(node.FirstChild.Name))) {
			return content;
		}
		
		var prefix = Environment.NewLine;
		
		if (Td.FirstNodeWithinCell(node)) {
			prefix = string.Empty;
		}
		
		return $"{prefix}{content}{(Td.LastNodeWithinCell(node) ? "" : Environment.NewLine)}";
	}
}

public class Dl : ConverterBase {
	public Dl(Converter converter) : base(converter) {
		Converter.Register("dl", this);
	}
	
	public override string Convert(HtmlNode node) {
		var prefixSuffix = Environment.NewLine;
		return $"{prefixSuffix}{TreatChildren(node)}{prefixSuffix}";
	}
}

public class Dt : ConverterBase {
	public Dt(Converter converter) : base(converter) {
		Converter.Register("dt", this);
	}
	
	public override string Convert(HtmlNode node) {
		var prefix = "- ";
		var content = TreatChildren(node);
		return $"{prefix}{content.Chomp()}{Environment.NewLine}";
	}
}

public class Dd : ConverterBase {
	public Dd(Converter converter) : base(converter) {
		Converter.Register("dd", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node);
		return $"    - {content.Chomp()}{Environment.NewLine}";
	}
}

public class Drop : ConverterBase {
	public Drop(Converter converter) : base(converter) {
		Converter.Register("style", this);
		Converter.Register("script", this);
		Converter.Register("#comment", this);
	}
	
	public override string Convert(HtmlNode node) {
		return "";
	}
}

public class Em : ConverterBase {
	public Em(Converter converter) : base(converter) {
		var elements = new[] { "em", "i" };
		
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node);
		
		if (string.IsNullOrEmpty(content.Trim()) || AlreadyItalic(node)) {
			return content;
		}
		
		var spaceSuffix = (node.NextSibling?.Name == "i" || node.NextSibling?.Name == "em")
			? " "
			: "";
		
		var emphasis = "*";
		return content.EmphasizeContentWhitespaceGuard(emphasis, spaceSuffix);
	}
	
	private static bool AlreadyItalic(HtmlNode node) {
		return node.Ancestors("i").Any() || node.Ancestors("em").Any();
	}
}

public class H : ConverterBase {
	public H(Converter converter) : base(converter) {
		var elements = new[] { "h1", "h2", "h3", "h4", "h5", "h6" };
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		// Headings inside tables are not supported as markdown, so just ignore the heading and convert children
		if (node.Ancestors("table").Any()) {
			return TreatChildren(node);
		}
		
		var prefix = new string('#', System.Convert.ToInt32(node.Name.Substring(1)));
		
		return $"{Environment.NewLine}{prefix} {TreatChildren(node)}{Environment.NewLine}";
	}
}

public class Hr : ConverterBase {
	public Hr(Converter converter) : base(converter) {
		Converter.Register("hr", this);
	}
	
	public override string Convert(HtmlNode node) {
		return $"{Environment.NewLine}* * *{Environment.NewLine}";
	}
}

public class Ignore : ConverterBase {
	public Ignore(Converter converter) : base(converter) {
		var elements = new[] { "colgroup", "col" };
		
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		return "";
	}
}

public class Img : ConverterBase {
	public Img(Converter converter) : base(converter) {
		Converter.Register("img", this);
	}
	
	public override string Convert(HtmlNode node) {
		var alt = node.GetAttributeValue("alt", string.Empty);
		var src = node.GetAttributeValue("src", string.Empty);
		
		var title = ExtractTitle(node);
		title = title.Length > 0 ? $" \"{title}\"" : "";
		
		return $"![{StringUtils.EscapeLinkText(alt)}]({src}{title})";
	}
}

public class Li : ConverterBase {
	public Li(Converter converter) : base(converter) {
		Converter.Register("li", this);
	}
	
	public override string Convert(HtmlNode node) {
		// Standardize whitespace before inner lists so that the following are equivalent
		//   <li>Foo<ul><li>...
		//   <li>Foo\n    <ul><li>...
		foreach (var innerList in node.SelectNodes(".//ul|.//ol") ?? Enumerable.Empty<HtmlNode>()) {
			if (innerList.PreviousSibling?.NodeType == HtmlNodeType.Text) {
				innerList.PreviousSibling.InnerHtml = innerList.PreviousSibling.InnerHtml.Trim();
			}
		}
		
		var content = TreatChildren(node);
		var prefix = PrefixFor(node);
		
		return $"{prefix}{content.Trim()}".Replace("\n", "\n  ") + Environment.NewLine;
	}
	
	private string PrefixFor(HtmlNode node) {
		if (node.ParentNode != null && node.ParentNode.Name == "ol") {
			// index are zero based hence add one
			var index = node.ParentNode.SelectNodes("./li").IndexOf(node) + 1;
			return $"{index}. ";
		} else {
			return "- ";
		}
	}
}

public class Ol : ConverterBase {
	public Ol(Converter converter) : base(converter) {
		var elements = new[] { "ol", "ul" };
		
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		// Lists inside tables are not supported as markdown, so leave as HTML
		if (node.Ancestors("table").Any()) {
			return node.OuterHtml;
		}
		
		string prefixSuffix = Environment.NewLine;
		
		// Prevent blank lines being inserted in nested lists
		string parentName = node.ParentNode.Name.ToLowerInvariant();
		if (parentName == "ol" || parentName == "ul") {
			prefixSuffix = "";
		}
		
		return $"{prefixSuffix}{TreatChildren(node)}{prefixSuffix}";
	}
}

class P : ConverterBase {
	public P(Converter converter) : base(converter) {
		Converter.Register("p", this);
	}
	
	public override string Convert(HtmlNode node) {
		var newlineBefore = Td.FirstNodeWithinCell(node) ? "" : Environment.NewLine;
		var newlineAfter = NewlineAfter(node);
		var content = TreatChildren(node);
		
		return $"{newlineBefore}{content}{newlineAfter}";
	}
	
	private static string NewlineAfter(HtmlNode node) {
		return Td.LastNodeWithinCell(node) ? "" : Environment.NewLine;
	}
}

public class PassThrough : ConverterBase {
	public PassThrough(Converter converter)
		: base(converter) {
	}
	
	public override string Convert(HtmlNode node) {
		return node.OuterHtml;
	}
}

public class Pre : ConverterBase {
	public Pre(Converter converter) : base(converter) {
		Converter.Register("pre", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = DecodeHtml(node.InnerText).TrimEnd();
		
		return $"{Environment.NewLine}{Environment.NewLine}```csharp{Environment.NewLine}{content}{Environment.NewLine}```{Environment.NewLine}";
	}
}

public class S : ConverterBase {
	public S(Converter converter) : base(converter) {
		Converter.Register("s", this);
		Converter.Register("del", this);
		Converter.Register("strike", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node);
		if (string.IsNullOrEmpty(content) || AlreadyStrikethrough(node)) {
			return content;
		}
		
		var emphasis = "~~";
		return content.EmphasizeContentWhitespaceGuard(emphasis);
	}
	
	private static bool AlreadyStrikethrough(HtmlNode node) {
		return node.Ancestors("s").Any() || node.Ancestors("del").Any() || node.Ancestors("strike").Any();
	}
}

public class Strong : ConverterBase {
	public Strong(Converter converter) : base(converter) {
		var elements = new[] { "strong", "b" };
		
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node);
		if (string.IsNullOrEmpty(content) || AlreadyBold(node)) {
			return content;
		}
		
		var spaceSuffix = (node.NextSibling?.Name == "strong" || node.NextSibling?.Name == "b")
			? " "
			: "";
		
		var emphasis = "**";
		return content.EmphasizeContentWhitespaceGuard(emphasis, spaceSuffix);
	}
	
	private static bool AlreadyBold(HtmlNode node) {
		return node.Ancestors("strong").Any() || node.Ancestors("b").Any();
	}
}

public class Sup : ConverterBase {
	public Sup(Converter converter) : base(converter) {
		Converter.Register("sup", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node);
		if (string.IsNullOrEmpty(content) || AlreadySup(node)) {
			return content;
		}
		
		return $"^{content.Chomp(all: true)}^";
	}
	
	private static bool AlreadySup(HtmlNode node) {
		return node.Ancestors("sup").Any();
	}
}

public class Table : ConverterBase {
	public Table(Converter converter) : base(converter) {
		Converter.Register("table", this);
	}
	
	public override string Convert(HtmlNode node) {
		// if table does not have a header row , add empty header row if set in config
		var useEmptyRowForHeader = this.Converter.Config.TableWithoutHeaderRowHandling ==
								Config.TableWithoutHeaderRowHandlingOption.EmptyRow;
		
		var emptyHeaderRow = HasNoTableHeaderRow(node) && useEmptyRowForHeader
			? EmptyHeader(node)
			: string.Empty;
		
		return $"{Environment.NewLine}{Environment.NewLine}{emptyHeaderRow}{TreatChildren(node)}{Environment.NewLine}";
	}
	
	private static bool HasNoTableHeaderRow(HtmlNode node) {
		var thNode = node.SelectNodes(".//th")?.FirstOrDefault();
		return thNode == null;
	}
	
	private static string EmptyHeader(HtmlNode node) {
		var firstRow = node.SelectNodes(".//tr")?.FirstOrDefault();
		
		if (firstRow == null) {
			return string.Empty;
		}
		
		var colCount = firstRow.ChildNodes.Count(n => n.Name.Contains("td"));
		
		var headerRowItems = new List<string>();
		var underlineRowItems = new List<string>();
		
		for (var i = 0; i < colCount; i++) {
			headerRowItems.Add("");
			underlineRowItems.Add("---");
		}
		
		var headerRow = $"| {string.Join(" | ", headerRowItems)} |{Environment.NewLine}";
		var underlineRow = $"| {string.Join(" | ", underlineRowItems)} |{Environment.NewLine}";
		
		return headerRow + underlineRow;
	}
}

public class Td : ConverterBase {
	public Td(Converter converter) : base(converter) {
		var elements = new[] { "td", "th" };
		
		foreach (var element in elements) {
			Converter.Register(element, this);
		}
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node)
			.Chomp()
			.Replace(Environment.NewLine, "<br>");
		
		var colSpan = GetColSpan(node);
		return string.Concat(Enumerable.Repeat($" {content} |", colSpan));
	}
	
	/// <summary>
	/// Given node within td tag, checks if newline should be prepended. Will not prepend if this is the first node after any whitespace
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static bool FirstNodeWithinCell(HtmlNode node) {
		var parentName = node.ParentNode.Name;
		// If p is at the start of a table cell, no leading newline
		if (parentName == "td" || parentName == "th") {
			var pNodeIndex = node.ParentNode.ChildNodes.GetNodeIndex(node);
			var firstNodeIsWhitespace = node.ParentNode.FirstChild.Name == "#text" && Regex.IsMatch(node.ParentNode.FirstChild.InnerText, @"^\s*$");
			if (pNodeIndex == 0 || (firstNodeIsWhitespace && pNodeIndex == 1)) return true;
		}
		return false;
	}
	/// <summary>
	/// Given node within td tag, checks if newline should be appended. Will not append if this is the last node before any whitespace
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static bool LastNodeWithinCell(HtmlNode node) {
		var parentName = node.ParentNode.Name;
		if (parentName == "td" || parentName == "th") {
			var pNodeIndex = node.ParentNode.ChildNodes.GetNodeIndex(node);
			var cellNodeCount = node.ParentNode.ChildNodes.Count;
			var lastNodeIsWhitespace = node.ParentNode.LastChild.Name == "#text" && Regex.IsMatch(node.ParentNode.LastChild.InnerText, @"^\s*$");
			if (pNodeIndex == cellNodeCount - 1 || (lastNodeIsWhitespace && pNodeIndex == cellNodeCount - 2)) return true;
		}
		return false;
	}
	
	private int GetColSpan(HtmlNode node) {
		var colSpan = 1;
		
		if (Converter.Config.TableHeaderColumnSpanHandling && node.Name == "th") {
			colSpan = node.GetAttributeValue("colspan", 1);
		}
		return colSpan;
	}
}

public class Text : ConverterBase {
	
	public Text(Converter converter) : base(converter) {
		Converter.Register("#text", this);
	}
	
	public override string Convert(HtmlNode node) {
		return node.InnerText == string.Empty ? TreatEmpty(node) : TreatText(node);
	}
	
	private string TreatText(HtmlNode node) {
		// Prevent &lt; and &gt; from being converted to < and > as this will be interpreted as HTML by markdown
		string content = node.InnerText
			.Replace("&lt;", "%3C")
			.Replace("&gt;", "%3E");
		
		content = DecodeHtml(content);
		
		// Not all renderers support hex encoded characters, so convert back to escaped HTML
		content = content
			.Replace("%3C", "&lt;")
			.Replace("%3E", "&gt;");
		
		//strip leading spaces and tabs for text within list item
		var parent = node.ParentNode;
		
		switch (parent.Name) {
		case "table":
		case "thead":
		case "tbody":
		case "ol":
		case "ul":
		case "th":
		case "tr":
			content = content.Trim();
			break;
		}
		
		if (parent.Ancestors("th").Any() || parent.Ancestors("td").Any()) {
			content = ReplaceNewlineChars(parent, content);
		}
		
		if (parent.Name != "a") {
			content = EscapeKeyChars(content);
		}
		
		content = PreserveKeyCharsWithinBackTicks(content);
		
		return content;
	}
	
	private string EscapeKeyChars(string content) {
		//if (content.Contains('*')) print.it("Error in docs: * not in <c>: " + content);
		//if (content.Contains('_')) print.it("Error in docs: _ not in <c>: " + content);
		
		return content;
	}
	
	private static string TreatEmpty(HtmlNode node) {
		var content = "";
		
		var parent = node.ParentNode;
		
		if (parent.Name == "ol" || parent.Name == "ul") {
			content = "";
		} else if (node.InnerText == " ") {
			content = " ";
		}
		
		return content;
	}
	
	private static string PreserveKeyCharsWithinBackTicks(string content) {
		
		content = rx.Replace(content, p => p.Value.Replace(@"\*", "*").Replace(@"\_", "_"));
		
		return content;
	}
	static Regex rx = new Regex("`.*?`");
	
	private static string ReplaceNewlineChars(HtmlNode parentNode, string content) {
		if (parentNode.Name != "p" && parentNode.Name != "#document") return content;
		
		content = content.Replace("\r\n", "<br>");
		content = content.Replace("\n", "<br>");
		
		return content;
	}
}

public class Tr : ConverterBase {
	public Tr(Converter converter) : base(converter) {
		Converter.Register("tr", this);
	}
	
	public override string Convert(HtmlNode node) {
		var content = TreatChildren(node).TrimEnd();
		var underline = "";
		
		if (string.IsNullOrWhiteSpace(content)) {
			return "";
		}
		
		if (IsTableHeaderRow(node) || UseFirstRowAsHeaderRow(node)) {
			underline = UnderlineFor(node, Converter.Config.TableHeaderColumnSpanHandling);
		}
		
		return $"|{content}{Environment.NewLine}{underline}";
	}
	
	private bool UseFirstRowAsHeaderRow(HtmlNode node) {
		var tableNode = node.Ancestors("table").FirstOrDefault();
		var firstRow = tableNode?.SelectSingleNode(".//tr");
		
		if (firstRow == null) {
			return false;
		}
		
		var isFirstRow = firstRow == node;
		var hasNoHeaderRow = tableNode.SelectNodes(".//th")?.FirstOrDefault() == null;
		
		return isFirstRow
			&& hasNoHeaderRow
			&& Converter.Config.TableWithoutHeaderRowHandling ==
			Config.TableWithoutHeaderRowHandlingOption.Default;
	}
	
	private static bool IsTableHeaderRow(HtmlNode node) {
		return node.ChildNodes.FindFirst("th") != null;
	}
	
	private static string UnderlineFor(HtmlNode node, bool tableHeaderColumnSpanHandling) {
		var nodes = node.ChildNodes.Where(x => x.Name == "th" || x.Name == "td").ToList();
		
		var cols = new List<string>();
		foreach (var nd in nodes) {
			var colSpan = GetColSpan(nd, tableHeaderColumnSpanHandling);
			var styles = StringUtils.ParseStyle(nd.GetAttributeValue("style", ""));
			styles.TryGetValue("text-align", out var align);
			
			string content;
			switch (align?.Trim()) {
			case "left":
				content = ":---";
				break;
			case "right":
				content = "---:";
				break;
			case "center":
				content = ":---:";
				break;
			default:
				content = "---";
				break;
			}
			
			for (var i = 0; i < colSpan; i++) {
				cols.Add(content);
			}
		}
		
		var colsAggregated = string.Join(" | ", cols);
		
		return $"| {colsAggregated} |{Environment.NewLine}";
	}
	
	private static int GetColSpan(HtmlNode node, bool tableHeaderColumnSpanHandling) {
		var colSpan = 1;
		
		if (tableHeaderColumnSpanHandling && node.Name == "th") {
			colSpan = node.GetAttributeValue("colspan", 1);
		}
		return colSpan;
	}
}
