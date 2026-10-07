# CrystalCode Web

**A Crystal Code external tool set that gives the agent live web search and page fetch.**

This is a native dotnet tool set for [Crystal Code](https://github.com/YELANDAOKONG/CrystalCode). `websearch` queries the hosted Parallel or Exa Search MCP endpoint and returns ranked, model-ready results; `webfetch` reads one HTTP or HTTPS page and converts it to text, Markdown, or raw HTML. The model chooses how much information comes back through `numResults`, `contextMaxCharacters`, and `maxCharacters`. The model-facing names match the Python web tool set.

[Features](#features) · [Tools](#tools) · [Configuration](#configuration) · [Build from source](#build-from-source)

## Features

- **Search the live web.** `websearch` calls the hosted Parallel Search MCP by default and Exa when the operator selects it. Parallel works anonymously.
- **Model-controlled volume.** `numResults` and `contextMaxCharacters` steer how much the Exa engine returns, and `maxCharacters` caps the returned text for either backend with a truncation note.
- **Read specific pages.** `webfetch` performs one HTTP GET, rejects non-text content, retries a Cloudflare challenge once, and converts HTML to plain text or Markdown.
- **Stay bounded.** Search responses are capped at 512 KiB and pages at 5 MiB. Search has a 25-second request timeout; `webfetch` honors its model-visible `timeout`. The host per-call timeout still applies.
- **Keep credentials out of the wire format.** The manifest and tool arguments never carry secrets. `EXA_API_KEY` and `PARALLEL_API_KEY` are read from the process environment and never written to output or logs.
- **Fit the manifest.** The set joins Plan and Work, declares `approval: always`, and loads `websearch` and `webfetch` from one assembly in one load context.

## Tools

| Tool | What it does |
| :--- | :--- |
| `websearch` | Search the live web through Parallel (default) or Exa and return ranked results as text |
| `webfetch` | Fetch one http(s) page as Markdown (default), plain text, or raw HTML |

### websearch arguments

| Argument | Default | Meaning |
| :--- | :--- | :--- |
| `query` | Required | The web search query |
| `numResults` | 8 (1–20) | Number of search results; the Exa backend honors it |
| `contextMaxCharacters` | 10000 (1–50000) | Context size the search engine returns; the Exa backend honors it |
| `type` | `auto` | Exa effort: `auto`, `fast`, or `deep` |
| `livecrawl` | `fallback` | Exa live crawl: `fallback` or `preferred` |
| `session_id` | None | Stable conversation id reused across related Parallel calls |
| `model_name` | None | Exact model identifier for Parallel product analytics only |
| `maxCharacters` | None (1–100000) | Cap on the returned text; the tool truncates and notes it |

### webfetch arguments

| Argument | Default | Meaning |
| :--- | :--- | :--- |
| `url` | Required | The http or https URL to fetch; credentials in the URL are rejected |
| `format` | `markdown` | Returned representation: `text`, `markdown`, or `html` |
| `timeout` | 30 (1–120) | Request timeout in seconds |
| `maxCharacters` | None (1–100000) | Cap on the returned page text; the tool truncates and notes it |

## Configuration

| Variable | Required | Purpose |
| :--- | :--- | :--- |
| `CRYSTAL_WEBSEARCH_PROVIDER` | No | Selects `parallel` (default) or `exa` |
| `EXA_API_KEY` | No | Exa credential; anonymous use works within free-tier limits |
| `PARALLEL_API_KEY` | No | Parallel credential; anonymous use works within free-tier limits |
| `CRYSTAL_EXA_MCP_URL` | No | Overrides the Exa endpoint; defaults to `https://mcp.exa.ai/mcp` |
| `CRYSTAL_PARALLEL_MCP_URL` | No | Overrides the Parallel endpoint; defaults to `https://search.parallel.ai/mcp` |

### Install

Publish the set and place the published files, including `tools.json`, in one directory under `~/.crystal/tools`:

```bash
dotnet publish CrystalCode.Web/CrystalCode.Web.csproj -c Release -o ~/.crystal/tools/CrystalCode.Web
```

Restart the session, or run `/tools reload`, to load the set. A project set at `<workspace>/.crystal/tools/CrystalCode.Web/` replaces the home set for that workspace. The directory name is the set identity; keep it `CrystalCode.Web`.

## Build from source

You need the .NET 10 SDK and a sibling checkout of [Crystal](https://github.com/YELANDAOKONG/Crystal) at `../Crystal`. No CrystalCode checkout is needed at build time.

```bash
dotnet build CrystalCode.Web.sln
dotnet test CrystalCode.Web.sln
dotnet publish CrystalCode.Web/CrystalCode.Web.csproj
```

`Crystal.Tools` is referenced as a sibling source project. The production project adds no NuGet packages; the test project uses xUnit and Microsoft.NET.Test.Sdk only. `WebSearchTool` and `WebFetchTool` are public `ITool` types listed in `tools.json`; the host loads this class library in one isolated load context and wraps every call with its own approval, output truncation, and timeout.
