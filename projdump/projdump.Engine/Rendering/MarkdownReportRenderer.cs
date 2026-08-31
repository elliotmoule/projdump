using System.Text;
using projdump.Engine.Shared;

namespace projdump.Engine.Rendering;

public static class MarkdownReportRenderer
{
    public static (string Output, int EstimatedTokens) Render(ReportRenderRequest request)
    {
        StringBuilder sb = new();

        // Header
        string modeLabel = request.Slim ? " (slim)" : "";
        sb.AppendLine($"# {request.InputFileInfo.Name} - {(request.IsSolution ? "App Solution" : "App Project")}{modeLabel}");
        sb.AppendLine();

        if (request.Slim)
        {
            sb.AppendLine("> **Slim mode:** file contents are omitted. Each entry shows the file name, path, and size.");
            sb.AppendLine();
        }

        // Token estimate placeholder - filled in at the end
        const string tokenPlaceholderLine = "> **Estimated tokens:** ~4,849  _(character count ÷ 4 — treat as a rough guide)_";
        sb.AppendLine(tokenPlaceholderLine);
        sb.AppendLine();

		// Active flags note
		var activeFlags = new List<string>();
		if (request.Slim) activeFlags.Add("`--slim`");
		if (request.ExcludeTests) activeFlags.Add("`--exclude-tests`");
		if (request.SearchForReadme) activeFlags.Add("`--find-readme`");
		if (request.ScopeDir != null) activeFlags.Add($"`--scope {request.ScopeDir}`");
		foreach (var dir in request.ExcludeDirs) activeFlags.Add($"`--exclude-dir {dir}`");
		if (activeFlags.Count > 0)
        {
            sb.AppendLine($"> **Flags:** {string.Join(", ", activeFlags)}");
            sb.AppendLine();
        }

        // File Summary Table
        sb.AppendLine("## Project Summary");
        var stats = request.AllFiles
            .GroupBy(f => f.Extension.ToLower())
            .Select(g => new { Ext = string.IsNullOrEmpty(g.Key) ? "No Ext" : g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);

        sb.AppendLine("| File Extension | Count |");
        sb.AppendLine("| :--- | :--- |");
        foreach (var stat in stats)
            sb.AppendLine($"| {stat.Ext} | {stat.Count} |");
        sb.AppendLine();

        // File Structure
        sb.AppendLine("## Project Structure");
        sb.AppendLine("```text");
        foreach (var file in request.AllFiles)
            sb.AppendLine(Path.GetRelativePath(request.RootDir.FullName, file.FullName));
        sb.AppendLine("```");
        sb.AppendLine();

		// README / Documentation
		if (request.ReadmeFiles.Count > 0)
		{
			sb.AppendLine("## Documentation");
			foreach (var file in request.ReadmeFiles)
			{
				string relativePath = Path.GetRelativePath(request.RootDir.FullName, file.FullName);

				// A relative path that climbs out of the root means the file was pulled in from
				// an ancestor directory, so it isn't part of the project's own file listing.
				bool isOutsideProject = relativePath.StartsWith("..", StringComparison.Ordinal);

				sb.AppendLine($"### {file.Name}");
				sb.AppendLine($"**Path:** `{(isOutsideProject ? file.FullName : relativePath)}`");
				sb.AppendLine();

				if (isOutsideProject)
				{
					sb.AppendLine("> **Sourced from outside the project tree.** Found by searching parent directories for a README; it is not included in the file listing or extension counts above.");
					sb.AppendLine();
				}

				if (request.Slim)
					sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(file.Length)}_");
				else
					sb.AppendLine(File.ReadAllText(file.FullName).Trim());

				sb.AppendLine();
			}
		}

		// Solution Configuration
		if (request.IsSolution)
        {
            sb.AppendLine("## Solution Configuration");
            string slnLang = request.Extension == ".slnx" ? "xml" : "text";
            sb.AppendLine($"### {request.InputFileInfo.Name}");
            sb.AppendLine($"**Path:** `{request.InputFileInfo.Name}`");
            sb.AppendLine();
            if (request.Slim)
            {
                sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(request.InputFileInfo.Length)}_");
            }
            else
            {
                sb.AppendLine($"```{slnLang}");
                sb.AppendLine(File.ReadAllText(request.InputFileInfo.FullName).Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        // Project Dependencies
        sb.AppendLine("## Project Dependencies");
        foreach (var proj in request.ProjFiles)
        {
            string relativePath = Path.GetRelativePath(request.RootDir.FullName, proj.FullName);
            sb.AppendLine($"### {proj.Name}");
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            if (request.Slim)
            {
                sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(proj.Length)}_");
            }
            else
            {
                sb.AppendLine("```xml");
                sb.AppendLine(File.ReadAllText(proj.FullName).Trim());
                sb.AppendLine("```");
            }
        }
        sb.AppendLine();

        // Configuration Files
        if (request.ConfigFiles.Count > 0)
        {
            sb.AppendLine("## Configuration");
            foreach (var file in request.ConfigFiles)
            {
                string relativePath = Path.GetRelativePath(request.RootDir.FullName, file.FullName);
                sb.AppendLine($"### {file.Name}");
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                if (request.Slim)
                {
                    sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(file.Length)}_");
                }
                else
                {
                    string lang = FormatHelpers.GetMarkdownLanguage(file.Extension);
                    sb.AppendLine($"```{lang}");
                    sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                    sb.AppendLine("```");
                }
                sb.AppendLine();
            }
        }

        // App Code
        sb.AppendLine("## App Code");
        sb.AppendLine();
        foreach (var file in request.CodeFiles)
        {
            string relativePath = Path.GetRelativePath(request.RootDir.FullName, file.FullName);
            sb.AppendLine($"### {file.Name}");
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            if (request.Slim)
            {
                sb.AppendLine($"_File size: {FormatHelpers.FormatFileSize(file.Length)}_");
            }
            else
            {
                string lang = FormatHelpers.GetMarkdownLanguage(file.Extension);
                sb.AppendLine($"```{lang}");
                sb.AppendLine(File.ReadAllText(file.FullName).Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        // Token estimate (Rough heuristic: GPT/Claude tokenisers average ~4 chars per token for code)
        string output = sb.ToString();
        int estimatedTokens = (int)Math.Ceiling(output.Length / 4.0);
        string tokenLine = $"> **Estimated tokens:** ~{estimatedTokens:N0}  _(character count ÷ 4 — treat as a rough guide)_";
        output = output.Replace(tokenPlaceholderLine, tokenLine);

        return (output, estimatedTokens);
    }
}