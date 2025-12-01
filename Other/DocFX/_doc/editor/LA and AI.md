---
uid: ai
---

# LibreAutomate and AI

## LA features that use AI

### LA documentation search



### Improving AI chat experience



### MCP server

External AI apps and tools, installed on your computer, can automatically retrieve LA documentation via MCP (Model Context Protocol). Examples: Copilot in Visual Studio, Claude Desktop, n8n. It improves AI chat answers, generated code, AI decisions, etc.

LA has an MCP server that includes tools to get LA documentation. Whenever AI needs information about LA automation API or other features, it calls a tool with a search query (AI-generated). The tool finds relevant LA documentation articles and returns them to the AI. The tool uses the same AI search engine as in the **Help** panel.

LA MCP server details:
- Type: `stdio`.
- Command path: the main LA program file. Usually `C:/Program Files/LibreAutomate/Au.Editor.exe`.
- Command arguments: `/mcp`.

AI apps/tools have a MCP configuration UI or file. Add the LA MCP server there. Example MCP server entry in file `C:\Users\Me\.mcp.json` used by VS Copilot:

```json
    "LibreAutomate": {
      "type": "stdio",
      "command": "C:/Program Files/LibreAutomate/Au.Editor.exe /mcp",
      "disabled": false
    }
```

A MCP client (AI app/tool) runs the program and communicates with it via standard input/output streams. It discovers the tools, and calls them when AI asks.

Tell AI to use the MCP tools. For example add "Use LibreAutomate MCP" in your chat message. Also AI apps like VS Copilot usually have an "AI instructions" file where you can add information about the MCP tools. For example VS Copilot uses file `.github\copilot-instructions.md` in the solution directory. Tell AI to use the file.

Example instructions file:

```md
# Custom Instructions

## C# Instructions
Applies to: `**/*.cs`

You help LibreAutomate users write C# code and use the app.

Environment: Windows 11, C# 14, .NET 9.

C# code that uses LibreAutomate usually is a script.

Script code should be concise and easy to understand even for non-programmers. Top-level statements; type definitions below. No try/catch at the top level of code.

Prefer LibreAutomate APIs. They're optimized for reliability, simplicity and concise code. Many methods are high-level; just find the right one.

To get information about LibreAutomate API or IDE, use MCP server `LibreAutomate`. You can use it in two ways:
- Call tool `find_la_docs`. It finds and returns the requested information.
- Call tool `get_la_docs_toc` to get the table of contents of the LibreAutomation documentation. Select up to 100 article names, and call tool `get_la_docs` to get the articles.

Don't need `using` directives for LibreAutomate API.

To create WPF windows, use class `wpfBuilder` from LibreAutomate. XAML is used rarely and requires `XamlReader`.

Many LibreAutomate parameter types have implicit conversion operators. Use it to make the code more concise.

LibreAutomate users also can use:
- Libraries from NuGet. Tool to install: menu **Tools > NuGet**.
- Windows API. Add declarations in `unsafe class api : NativeApi { }`.

In your messages prefer ASCII punctuation (`'`, `-`). Don't use emoji in code.

When user asks "do something", usually it actually means "create code to do something", or "explain how to do something".

When user message contains a file path or URL, don't try to load/fetch the file, unless the user asks to do it. The path/URL is likely meant to use in generated code.

In agent mode you may edit files, and build if need. Don't run the program.

```

To see how AI uses LA MCP tools, check **Options > AI > MCP > Print tool calls**. When a tool is called, it prints the tool name, arguments and results.

Before installing a new LibreAutomate version, close apps that use the MCP server.

### Find icon by name or image



## AI setup

