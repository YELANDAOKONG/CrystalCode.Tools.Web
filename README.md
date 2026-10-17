# Crystal Code Web

**A Crystal Code external tool set that gives the agent live web search and page fetch.**

This is a native dotnet tool set for [Crystal Code](https://github.com/YELANDAOKONG/CrystalCode). `websearch` queries one of several hosted search backends and returns ranked, model-ready results; `webfetch` reads one HTTP or HTTPS page and converts it to text, Markdown, or raw HTML. The model chooses how much information comes back through `numResults`, `contextMaxCharacters`, and `maxCharacters`. The model-facing names match the Python web tool set.

[Features](#features) · [Tools](#tools) · [Configuration](#configuration) · [Build from source](#build-from-source)

## Features

- **Search the live web.** `websearch` uses Parallel by default and can be switched to Exa, Tavily, Bocha, Perplexity, Jina, or Firecrawl. Parallel and Exa work anonymously; the other providers require an API key.
- **Readable results.** Structured payloads are normalized into `Title`, `URL`, `Published`, and `Highlights` blocks; text from Exa and Jina passes through unchanged.
- **Model-controlled volume.** `numResults` applies to Exa, Tavily, Bocha, Perplexity, Jina, and Firecrawl; Parallel uses its server-side default. `contextMaxCharacters` steers the Exa engine, and `maxCharacters` caps the returned text for every backend with a truncation note.
- **Read specific pages.** `webfetch` performs one HTTP GET, rejects non-text content, retries a Cloudflare challenge once, and converts HTML to plain text or Markdown.
- **Stay bounded.** Search responses are capped at 512 KiB and pages at 5 MiB. Search has a 25-second request timeout; `webfetch` honors its model-visible `timeout`. The host per-call timeout still applies.
- **Keep credentials out of the wire format.** The manifest and tool arguments never carry secrets. Provider keys are read from the process environment and never written to output or logs. A missing required key fails before any network request.
- **Fit the manifest.** The set joins Plan and Work, declares `approval: always`, and loads `websearch` and `webfetch` from one assembly in one load context.

## Tools

| Tool | What it does |
| :--- | :--- |
| `websearch` | Search the live web through Parallel (default), Exa, Tavily, Bocha, Perplexity, Jina, or Firecrawl and return ranked results as text |
| `webfetch` | Fetch one http(s) page as Markdown (default), plain text, or raw HTML |

### websearch arguments

| Argument | Default | Meaning |
| :--- | :--- | :--- |
| `query` | Required | The web search query |
| `numResults` | 8 (1–20) | Requested number of search results; providers may return fewer. Parallel uses its own default |
| `contextMaxCharacters` | 10000 (1–50000) | Context size the search engine returns; currently honored by Exa |
| `type` | `auto` | Search effort: `auto`, `fast`, or `deep`; Exa and Tavily honor all three, Perplexity honors `fast` only (`deep` uses standard web search), and other providers ignore it |
| `livecrawl` | `fallback` | Exa live crawl: `fallback` or `preferred`; other providers ignore it |
| `session_id` | None | Stable conversation id reused across related Parallel calls; other providers ignore it |
| `model_name` | None | Exact model identifier for Parallel product analytics only; other providers ignore it |
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
| `CRYSTAL_WEBSEARCH_PROVIDER` | No | Selects `parallel` (default), `exa`, `tavily`, `bocha`, `perplexity`, `jina`, or `firecrawl` |
| `PARALLEL_API_KEY` | No | Parallel credential; anonymous use works within free-tier limits |
| `EXA_API_KEY` | No | Exa credential; anonymous use works within free-tier limits |
| `TAVILY_API_KEY` | Yes for Tavily | Tavily credential |
| `BOCHA_API_KEY` | Yes for Bocha | Bocha credential |
| `PERPLEXITY_API_KEY` | Yes for Perplexity | Perplexity credential |
| `JINA_API_KEY` | Yes for Jina | Jina credential |
| `FIRECRAWL_API_KEY` | Yes for Firecrawl | Firecrawl credential |
| `CRYSTAL_EXA_MCP_URL` | No | Overrides the Exa endpoint; defaults to `https://mcp.exa.ai/mcp` |
| `CRYSTAL_PARALLEL_MCP_URL` | No | Overrides the Parallel endpoint; defaults to `https://search.parallel.ai/mcp` |
| `CRYSTAL_TAVILY_SEARCH_URL` | No | Overrides the Tavily endpoint; defaults to `https://api.tavily.com/search` |
| `CRYSTAL_BOCHA_SEARCH_URL` | No | Overrides the Bocha endpoint; defaults to `https://api.bocha.cn/v1/web-search` |
| `CRYSTAL_PERPLEXITY_SEARCH_URL` | No | Overrides the Perplexity endpoint; defaults to `https://api.perplexity.ai/search` |
| `CRYSTAL_JINA_MCP_URL` | No | Overrides the Jina endpoint; defaults to `https://mcp.jina.ai/v1` |
| `CRYSTAL_FIRECRAWL_SEARCH_URL` | No | Overrides the Firecrawl endpoint; defaults to `https://api.firecrawl.dev/v2/search` |

Keyed providers validate their key before sending a request. Jina's `search_web` and Firecrawl's search endpoint both require a key; their free tiers are quota-based rather than anonymous.

### Install

Publish the set and place the published files, including `tools.json`, in one directory under `~/.crystal/tools`:

```bash
dotnet publish CrystalCode.Web/CrystalCode.Web.csproj -c Release -o ~/.crystal/tools/web
```

Restart the session, or run `/tools reload`, to load the set. A project set at `<workspace>/.crystal/tools/web/` replaces the home set for that workspace. The directory name is the set identity; this set installs as `web`.

## Build from source

You need the .NET 10 SDK and a sibling checkout of [Crystal](https://github.com/YELANDAOKONG/Crystal) at `../Crystal`. No CrystalCode checkout is needed at build time.

```bash
dotnet build CrystalCode.Web.sln
dotnet test CrystalCode.Web.sln
dotnet publish CrystalCode.Web/CrystalCode.Web.csproj
```

`Crystal.Tools` is referenced as a sibling source project. The production project adds no NuGet packages; the test project uses xUnit and Microsoft.NET.Test.Sdk only. `WebSearchTool` and `WebFetchTool` are public `ITool` types listed in `tools.json`; the host loads this class library in one isolated load context and wraps every call with its own approval, output truncation, and timeout.
