using System.Text.Json;

namespace LA;

class McpServer {
	public static void Run(ReadOnlySpan<string> args) {
#if !SCRIPT
		process.thisProcessCultureIsInvariant = true;
		App.InitThisAppFoldersEtc_();
		AppSettings.Load();
#endif
		
		var stdin = Console.OpenStandardInput();
		var stdout = Console.OpenStandardOutput();
		using var reader = new StreamReader(stdin);
		using var writer = new StreamWriter(stdout) { AutoFlush = true };
		
		var mcpServer = new McpServer();
		
		while (reader.ReadLine() is string json) {
			//print.it(json);
			if (mcpServer.MessageReceived(json) is string response) {
				writer.WriteLine(response);
			}
		}
	}
	
	McpTools _tools = new();
	
	/// <summary>
	/// Processes a MCP message. Calls a tool if need, etc.
	/// </summary>
	/// <param name="json">JSON of the received message.</param>
	/// <returns>Response JSON. <c>null</c> it's a notification.</returns>
	public string MessageReceived(string json) {
		var msg = JsonDocument.Parse(json).RootElement;
		
		int id;
		if (!(msg.TryGetProperty("id", out var eId) && eId.TryGetInt32(out id))) { //notification
			return null;
		}
		
		try {
			string method = msg.GetProperty("method").GetString();
			
			if (method == "initialize") {
				return _Send(new {
					protocolVersion = "2025-06-18",
					serverInfo = new { name = "LibreAutomate", version = "1.0" },
					capabilities = new {
						tools = new { },
						//resources = new { },
						//prompts = new { },
						//completions = new { },
						//logging = new { },
					}
				});
			} else if (method == "ping") {
				return _Send(new { });
			} else if (method == "tools/list") {
				return _Send(new { tools = _GenerateToolList() });
			} else if (method == "tools/call") {
				//print.it($"MCP: process={process.thisProcessId}");
				//print.it(json);
				var p = msg.GetProperty("params");
				var text = _CallTool(p.GetProperty("name").GetString(), p.GetProperty("arguments"));
				return _ReturnContent(new { type = "text", text });
			} else {
				return _Error(-32601, "Method not found");
			}
		}
		catch (Exception ex) {
			if (ex is TargetInvocationException tie) ex = tie.InnerException;
			print.warning(ex, "<>Exception in LA MCP server: ");
//#if DEBUG
//			Debugger.Launch();
//#endif
			return _Error(-32603, ex.ToStringWithoutStack());
		}
		
		string _Send(object result) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result });
		
		string _ReturnContent(params object[] content) => _Send(new { content });
		
		string _Error(int code, string message) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } });
	}
	
	static List<object> _GenerateToolList() {
		List<object> tools = [];
		
		foreach (var mi in typeof(McpTools).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
			var toolAttr = mi.GetCustomAttribute<McpAttribute>();
			if (toolAttr == null) continue;
			
			Dictionary<string, object> properties = [];
			List<string> required = [];
			
			foreach (var p in mi.GetParameters()) {
				string type = _GetJsonType(p.ParameterType);
				Dictionary<string, object> propDef = [];
				propDef["type"] = type;
				if (p.GetCustomAttribute<McpAttribute>() is { } attr) {
					propDef["description"] = attr.Description;
					if (attr.EnumValues is string sv) propDef["enum"] = sv.Split('|');
				}
				if (p.ParameterType == typeof(string[])) propDef["items"] = new { type = "string" };
				if (p.HasDefaultValue) propDef["default"] = p.DefaultValue; else required.Add(p.Name);
				properties[p.Name] = propDef;
			}
			
			var inputSchema = new {
				type = "object",
				properties,
				required
			};
			
			tools.Add(new {
				name = mi.Name,
				//title = method.Name,
				description = toolAttr.Description,
				inputSchema
			});
		}
		
		//print.it(JsonSerializer.Serialize(tools, new JsonSerializerOptions() { WriteIndented = true }));
		return tools;
		
		static string _GetJsonType(Type t) {
			if (t.IsByRef) throw new NotSupportedException("ByRef parameter");
			if (t == typeof(string)) return "string";
			if (t == typeof(bool)) return "boolean";
			if (t == typeof(int) || t == typeof(long)) return "integer";
			if (t == typeof(double)) return "number";
			if (t == typeof(string[])) return "array";
			throw new NotSupportedException($"MCP tool parameter type '{t.FullName}' is not supported. Expected string, bool, int, long, double or string[].");
		}
	}
	
	string _CallTool(string name, JsonElement jArgs) {
		var mi = typeof(McpTools).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) ?? throw new ArgumentException($"Tool `{name}` does not exist");
		var ap = mi.GetParameters();
		var ao = ap.Length > 0 ? new object[ap.Length] : null;
		if (ap.Length > 0) {
			for (int i = 0; i < ap.Length; i++) {
				var p = ap[i];
				var t = p.ParameterType;
				object v = null;
				if (jArgs.TryGetProperty(p.Name, out var jArg)) {
					//print.it(jArg.ValueKind);
					if (t == typeof(string)) v = jArg.GetString();
					else if (t == typeof(bool)) v = jArg.GetBoolean();
					else if (t == typeof(int)) v = jArg.GetInt32();
					else if (t == typeof(long)) v = jArg.GetInt64();
					else if (t == typeof(double)) v = jArg.GetDouble();
					else if (t == typeof(string[])) {
						var a1 = new string[jArg.GetArrayLength()];
						int j = 0;
						foreach (var jElem in jArg.EnumerateArray()) {
							a1[j++] = jElem.GetString();
						}
						v = a1;
					} else throw new ArgumentException($"Bad type of argument `{p.Name}`.");
				} else {
					if (p.HasDefaultValue) v = p.DefaultValue;
					else throw new ArgumentException($"Missing argument `{p.Name}`.");
				}
				ao[i] = v;
			}
		}
		return mi.Invoke(_tools, ao) as string;
	}
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
class McpAttribute : Attribute {
	public McpAttribute(string description) => Description = description;
	
	/// <summary>
	/// JSON <c>"description"</c>.
	/// </summary>
	public string Description { get; }
	
	/// <summary>
	/// JSON <c>"enum"</c> like <c>"red|green|blue"</c>.
	/// </summary>
	public string EnumValues { get; init; }
}
