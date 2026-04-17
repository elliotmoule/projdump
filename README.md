# projdump

A .NET CLI tool that distils a Visual Studio solution or project into a single structured markdown file, making it easy to provide codebase context to an LLM.

## What it does

Pointing `projdump` at a `.sln`, `.slnx`, or `.csproj` file produces a self-contained markdown document containing:

- **Project summary** — file extension breakdown table
- **Project structure** — full relative file tree
- **Documentation** — contents of any `.md` files found
- **Solution / project configuration** — the `.sln` or `.slnx` file
- **Project dependencies** — all `.csproj` contents
- **Configuration files** — `appsettings.json`, `launchSettings.json`, `web.config`, Docker files, and other well-known config files
- **App code** — all `.cs`, `.xaml`, `.cshtml`, `.css`, `.js`, and `.ts` files, ordered by significance (entry points → interfaces → models → helpers → everything else)
- **Token estimate** — a rough token count in the header so you know if you're within context window budget before pasting

## Usage

```
projdump <path-to-solution-or-project> [output-path]
```

**Examples**

```bash
# Dump an entire solution to app-solution.md alongside the .sln file
projdump MyApp.sln

# Dump a single project to app-project.md alongside the .csproj file
projdump src/MyApp.Api/MyApp.Api.csproj

# Write the output to a specific file
projdump MyApp.sln C:\context\myapp-context.md

# Write the output to a directory (filename is inferred)
projdump MyApp.sln C:\context\
```

## What gets excluded

To keep the output clean and token-efficient, the following are automatically skipped:

| Category | Examples |
| :--- | :--- |
| Build artifacts | `bin/`, `obj/` |
| Auto-generated code | `*.designer.cs`, `*.g.cs`, `*.g.i.cs` |
| Boilerplate | `AssemblyInfo.cs`, `GlobalUsings.cs` |
| Minified assets | `*.min.js`, `*.min.css` |
| EF Core migrations | `Migrations/` |
| VCS / IDE folders | `.git/`, `.vs/`, `.vscode/` |
| Dependencies | `node_modules/` |

## Building

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download) or later.

```bash
git clone https://github.com/your-username/projdump.git
cd projdump
dotnet build
```

Run directly:

```bash
dotnet run -- MyApp.sln
```

Or publish a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Output structure

```
# MySolution.sln - App Solution

> Estimated tokens: ~12,400 _(character count ÷ 4 — treat as a rough guide)_

## Project Summary
## Project Structure
## Documentation
## Solution Configuration
## Project Dependencies
## Configuration
## App Code
```

## Token estimate

The estimate at the top of every output is calculated as `character count ÷ 4`, which is a reasonable heuristic for mixed code and prose across GPT and Claude tokenisers. Treat it as a ballpark — actual token counts will vary by model.

## License

MIT
