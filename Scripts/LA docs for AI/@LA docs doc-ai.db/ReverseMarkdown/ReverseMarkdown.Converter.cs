/// This is a fork of https://github.com/mysticmind/reversemarkdown-net
/// Using source code because had to fix bugs.
/// Moved all classes to 2 files.
/// Dependency: HtmlAgilityPack.

using System.Reflection;
using HtmlAgilityPack;
using ReverseMarkdown.Converters;

namespace ReverseMarkdown;

public class Converter {
	protected readonly IDictionary<string, IConverter> Converters = new Dictionary<string, IConverter>();
	protected readonly IConverter PassThroughTagsConverter;
	protected readonly IConverter DropTagsConverter;
	protected readonly IConverter ByPassTagsConverter;
	
	public Converter() : this(new Config()) { }
	
	public Converter(Config config) : this(config, null) { }
	
	public Converter(Config config, params Assembly[] additionalAssemblies) {
		Config = config;
		
		var assemblies = new List<Assembly>()
		{
				typeof(IConverter).GetTypeInfo().Assembly
			};
		
		if (!(additionalAssemblies is null))
			assemblies.AddRange(additionalAssemblies);
		
		var types = new List<Type>();
		// instantiate all converters excluding the unknown tags converters
		foreach (var assembly in assemblies) {
			foreach (var converterType in assembly.GetTypes()
				.Where(t => t.GetTypeInfo().GetInterfaces().Contains(typeof(IConverter)) &&
				!t.GetTypeInfo().IsAbstract
				&& t != typeof(PassThrough)
				&& t != typeof(Drop)
				&& t != typeof(ByPass))) {
				// Check to see if any existing types are children/equal to
				// the type to add.
				if (types.Any(e => converterType.IsAssignableFrom(e)))
					// If they are, ignore the type.
					continue;
				
				// See if there is a type that is a parent of the
				// current type.
				var toRemove = types.FirstOrDefault(e => e.IsAssignableFrom(converterType));
				// if there is ...
				if (!(toRemove is null))
					// ... remove the parent.
					types.Remove(toRemove);
				
				// finally, add the type.
				types.Add(converterType);
			}
		}
		
		// For each type to register ...
		foreach (var converterType in types)
			// ... activate them
			Activator.CreateInstance(converterType, this);
		
		// register the unknown tags converters
		PassThroughTagsConverter = new PassThrough(this);
		DropTagsConverter = new Drop(this);
		ByPassTagsConverter = new ByPass(this);
	}
	
	public Config Config { get; protected set; }
	
	public virtual string Convert(HtmlNode root) {
		var converter = Lookup(root.Name);
		var result = converter.Convert(root);
		
		// cleanup multiple new lines
		result = Regex.Replace(result, @"(^\p{Zs}*(\r\n|\n)){2,}", Environment.NewLine, RegexOptions.Multiline);
		
		return result;
	}
	
	public virtual void Register(string tagName, IConverter converter) {
		Converters[tagName] = converter;
	}
	
	public virtual IConverter Lookup(string tagName) {
		// if a tag is in the pass through list then use the pass through tags converter
		if (Config.PassThroughTags.Contains(tagName)) {
			return PassThroughTagsConverter;
		}
		
		return Converters.TryGetValue(tagName, out var converter) ? converter : GetDefaultConverter(tagName);
	}
	
	private IConverter GetDefaultConverter(string tagName) {
		switch (Config.UnknownTags) {
		case Config.UnknownTagsOption.PassThrough:
			return PassThroughTagsConverter;
		case Config.UnknownTagsOption.Drop:
			return DropTagsConverter;
		case Config.UnknownTagsOption.Bypass:
			return ByPassTagsConverter;
		default:
			throw new UnknownTagException(tagName);
		}
	}
}

public class Config {
	public UnknownTagsOption UnknownTags { get; set; } = UnknownTagsOption.PassThrough;
	
	/// <summary>
	/// How to handle &lt;a&gt; tag href attribute
	/// <para>false - Outputs [{name}]({href}{title}) even if name and href is identical. This is the default option.</para>
	/// true - If name and href equals, outputs just the `name`. Note that if Uri is not well formed as per <see cref="Uri.IsWellFormedUriString"/> (i.e string is not correctly escaped like `http://example.com/path/file name.docx`) then markdown syntax will be used anyway.
	/// <para>If href contains http/https protocol, and name doesn't but otherwise are the same, output href only</para>
	/// If tel: or mailto: scheme, but afterwards identical with name, output name only.
	/// </summary>
	public bool SmartHrefHandling { get; set; } = false;
	
	public TableWithoutHeaderRowHandlingOption TableWithoutHeaderRowHandling { get; set; } =
		TableWithoutHeaderRowHandlingOption.Default;
	
	/// <summary>
	/// Option to set a default GFM code block language if class based language markers are not available
	/// </summary>
	public string DefaultCodeBlockLanguage { get; set; }
	
	/// <summary>
	/// Option to pass a list of tags to pass through as is without any processing
	/// </summary>
	public string[] PassThroughTags { get; set; } = {  };
	
	public enum UnknownTagsOption {
		/// <summary>
		/// Include the unknown tag completely into the result. That is, the tag along with the text will be left in output.
		/// </summary>
		PassThrough,
		
		/// <summary>
		/// Drop the unknown tag and its content
		/// </summary>
		Drop,
		
		/// <summary>
		/// Ignore the unknown tag but try to convert its content
		/// </summary>
		Bypass,
		
		/// <summary>
		/// Raise an error to let you know
		/// </summary>
		Raise
	}
	
	public enum TableWithoutHeaderRowHandlingOption {
		/// <summary>
		/// By default, first row will be used as header row
		/// </summary>
		Default,
		
		/// <summary>
		/// An empty row will be added as the header row
		/// </summary>
		EmptyRow
	}
	
	/// <summary>
	/// Set this flag to handle table header column with column spans
	/// </summary>
	public bool TableHeaderColumnSpanHandling { get; set; } = true;
}

public static class StringUtils {
	public static string Chomp(this string content, bool all = false) {
		if (all) return content.ReplaceLineEndings("");
		return content.Trim();
	}
	
	public static IEnumerable<string> ReadLines(this string content) {
		string line;
		using (var sr = new StringReader(content))
			while ((line = sr.ReadLine()) != null)
				yield return line;
	}
	
	/// <summary>
	/// <para>Gets scheme for provided uri string to overcome different behavior between windows/linux. https://github.com/dotnet/corefx/issues/1745</para>
	/// Assume http for url starting with //
	/// <para>Assume file for url starting with /</para>
	/// Otherwise give what <see cref="Uri.Scheme" /> gives us.
	/// <para>If non parseable by Uri, return empty string. Will never return null</para>
	/// </summary>
	/// <returns></returns>
	public static string GetScheme(string url) {
		var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out Uri uri);
		//IETF RFC 3986
		if (Regex.IsMatch(url, "^//[^/]")) {
			return "http";
		}
		//Unix style path
		else if (Regex.IsMatch(url, "^/[^/]")) {
			return "file";
		} else if (isValidUri) {
			return uri.Scheme;
		} else {
			return String.Empty;
		}
	}
	
	/// <summary>
	/// Escape/clean characters which would break the [] section of a markdown []() link
	/// </summary>
	public static string EscapeLinkText(string rawText) {
		return Regex.Replace(rawText, @"\r?\n\s*\r?\n", Environment.NewLine, RegexOptions.Singleline)
			.Replace("[", @"\[")
			.Replace("]", @"\]");
	}
	
	public static Dictionary<string, string> ParseStyle(string style) {
		if (string.IsNullOrEmpty(style)) {
			return new Dictionary<string, string>();
		}
		
		var styles = style.Split(';');
		return styles.Select(styleItem => styleItem.Split(':'))
			.Where(styleParts => styleParts.Length == 2)
			.DistinctBy(styleParts => styleParts[0])
			.ToDictionary(styleParts => styleParts[0], styleParts => styleParts[1]);
	}
	
	public static int LeadingSpaceCount(this string content) {
		var leadingSpaces = 0;
		foreach (var c in content) {
			if (c == ' ') {
				leadingSpaces++;
			} else {
				break;
			}
		}
		return leadingSpaces;
	}
	
	public static int TrailingSpaceCount(this string content) {
		var trailingSpaces = 0;
		for (var i = content.Length - 1; i >= 0; i--) {
			if (content[i] == ' ') {
				trailingSpaces++;
			} else {
				break;
			}
		}
		return trailingSpaces;
	}
	
	public static string EmphasizeContentWhitespaceGuard(this string content, string emphasis, string nextSiblingSpaceSuffix = "") {
		var leadingSpaces = new string(' ', content.LeadingSpaceCount());
		var trailingSpaces = new string(' ', content.TrailingSpaceCount());
		
		return $"{leadingSpaces}{emphasis}{content.Chomp(all: true)}{emphasis}{(trailingSpaces.Length > 0 ? trailingSpaces : nextSiblingSpaceSuffix)}";
	}
	
	private static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector) {
		return enumerable.GroupBy(keySelector).Select(grp => grp.First());
	}
}

public class UnknownTagException : Exception {
	public UnknownTagException(string tagName) : base($"Unknown tag: {tagName}") {
	}
}

public class UnsupportedTagException : Exception {
	internal UnsupportedTagException(string message) : base(message) {
	}
}
