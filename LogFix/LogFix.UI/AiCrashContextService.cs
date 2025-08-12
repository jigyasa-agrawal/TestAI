// AiCrashContextService.cs
// A lightweight service to turn a stack trace + local repo into paste-ready string output
// Works with C#/.NET stack traces and JS-style browser frames.
// .NET 6+ compatible.

using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System;
using CrashContext;

public class AiCrashContextService : IAiCrashContextService
{
    static readonly Regex CsRegex = new(@"^\n?\t*at\s+(?<qual>[\u00C0-\u024F\w\.+`]+)\.(?<method>[\w`]+)\s*\([^)]*\)(?:\s+in\s+(?<file>.*?):line\s+(?<line>\d+))?\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex JsRegex = new(@"^\n?\t*at\s+(?:(?<func>[\u00C0-\u024F\w\.$<>\[\]]+)\s+\()?(?<path>(?:[A-Za-z]:[\\/]|/|\.{1,2}[\\/]|https?://).+?):(?<line>\d+)(?::\d+)?\)?\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public string BuildMarkdown(string stackTraceText, CrashContextOptions options)
    {
        var frames = Symbolicate(stackTraceText, options);
        var sb = new StringBuilder(16 * 1024);

        sb.AppendLine("# AI Crash Context");
        sb.AppendLine();
        sb.AppendLine("## Original error & stack trace");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(stackTraceText.TrimEnd());
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Symbolicated snippets");

        if (frames.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("_No file/line patterns were detected in the stack trace._");
        }

        for (var i = 0; i < frames.Count; i++)
        {
            var f = frames[i];
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"### Frame {i + 1}");
            sb.AppendLine($"**Reported**: `{EscapeMd(f.Frame.FilePath)}` : line **{f.Frame.Line}**");
            sb.AppendLine();

            if (f.ResolvedPath is null)
            {
                sb.AppendLine("_File not found in repo; please verify the path/commit._");
                continue;
            }

            sb.AppendLine($"**Resolved path**: `{EscapeMd(NormalizePath(f.ResolvedPath))}`  ");
            if (f.StartLine.HasValue && f.EndLine.HasValue)
                sb.AppendLine($"**Showing lines {f.StartLine}–{f.EndLine} (context {options.ContextLines})**");
            sb.AppendLine();

            var lang = GuessFenceLanguage(f.ResolvedPath);
            sb.AppendLine($"```{lang}");
            sb.AppendLine(f.SnippetBody ?? string.Empty);
            sb.AppendLine("```");
        }

        var md = sb.ToString();
        if (!string.IsNullOrWhiteSpace(options.OutputMarkdownPath))
        {
            EnsureDirectory(Path.GetDirectoryName(options.OutputMarkdownPath!));
            File.WriteAllText(options.OutputMarkdownPath!, md, Encoding.UTF8);
        }
        return md;
    }

    public IReadOnlyList<SymbolicatedFrame> Symbolicate(string stackTraceText, CrashContextOptions options)
    {
        if (options is null) options = new CrashContextOptions();
        var repoRoot = Path.GetFullPath(options.RepoRoot ?? ".");
        var frames = ParseFrames(stackTraceText);

        var selected = new List<FrameInfo>(capacity: Math.Min(frames.Count, options.MaxFrames));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in frames)
        {
            var key = $"{f.FilePath}|{f.Line}";
            if (seen.Contains(key)) continue;
            seen.Add(key);
            selected.Add(f);
            if (selected.Count >= options.MaxFrames) break;
        }

        var result = new List<SymbolicatedFrame>(selected.Count);
        foreach (var f in selected)
        {
            var resolved = FindFile(repoRoot, f.FilePath);
            if (resolved is null)
            {
                result.Add(new SymbolicatedFrame(f, null, null, null, null));
                continue;
            }

            var snippet = ExtractSnippet(resolved, f.Line, options.ContextLines);
            if (snippet is null)
            {
                result.Add(new SymbolicatedFrame(f, resolved, null, null, null));
                continue;
            }

            result.Add(new SymbolicatedFrame(
                f,
                resolved,
                snippet.Value.StartLine,
                snippet.Value.EndLine,
                snippet.Value.Body
            ));
        }

        return result;
    }

    public static List<FrameInfo> ParseFrames(string stackTraceText)
    {
        var frames = new List<FrameInfo>(32);
        var lines = stackTraceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var ln in lines)
        {
            // C# frame
            var m = CsRegex.Match(ln);
            if (m.Success && int.TryParse(m.Groups["line"].Value, out var csLine) && !string.IsNullOrWhiteSpace(m.Groups["file"].Value))
            {
                frames.Add(new FrameInfo(
                    Raw: ln,
                    FilePath: m.Groups["file"].Value.Trim(),
                    Line: csLine,
                    Kind: "cs"
                ));
                continue;
            }

            // JS frame
            var j = JsRegex.Match(ln);
            if (j.Success && int.TryParse(j.Groups["line"].Value, out var jsLine))
            {
                frames.Add(new FrameInfo(
                    Raw: ln,
                    FilePath: j.Groups["path"].Value.Trim(),
                    Line: jsLine,
                    Kind: "js"
                ));
            }
        }

        return frames;
    }

    public static string? FindFile(string repoRoot, string reportedPath)
    {
        if (string.IsNullOrWhiteSpace(reportedPath)) return null;

        // 1) direct path
        try
        {
            if (File.Exists(reportedPath))
                return Path.GetFullPath(reportedPath);
        }
        catch { /* ignore */ }

        // 2) Strip url scheme or drive and match by trailing parts
        var tail = reportedPath;
        var schemeIdx = tail.IndexOf("://", StringComparison.Ordinal);
        if (schemeIdx >= 0) tail = tail[(schemeIdx + 3)..];

        tail = tail.Replace("\\", "/");
        var tailParts = tail.Split('/', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToArray();
        if (tailParts.Length == 0) return null;

        var fileName = tailParts[^1];
        var candidates = SafeEnumerateFiles(repoRoot, fileName);
        if (candidates.Count == 0) return null;
        if (tailParts.Length == 1) return candidates[0];

        // Score by matching suffix parts
        string ScorePath(string path)
        {
            var parts = path.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
            var i = parts.Length - 1;
            var j = tailParts.Length - 1;
            var score = 0;
            while (i >= 0 && j >= 0 && string.Equals(parts[i], tailParts[j], StringComparison.OrdinalIgnoreCase))
            {
                score++;
                i--; j--;
            }
            // prefer shorter paths when score ties (more specific)
            return score.ToString("D4") + "|" + parts.Length.ToString("D6");
        }

        candidates.Sort((a, b) => string.Compare(ScorePath(b), ScorePath(a), StringComparison.Ordinal));
        return candidates[0];
    }

    public static (int StartLine, int EndLine, string Body)? ExtractSnippet(string fullPath, int line, int context)
    {
        try
        {
            var raw = File.ReadAllLines(fullPath);
            if (raw.Length == 0) return (1, 1, string.Empty);

            var n = raw.Length;
            var idx = Math.Max(1, Math.Min(line, n));
            var start = Math.Max(1, idx - context);
            var end = Math.Min(n, idx + context);

            var sb = new StringBuilder((end - start + 1) * 80);
            for (var i = start; i <= end; i++)
            {
                sb.Append(i.ToString().PadLeft(6));
                sb.Append("  ");
                sb.AppendLine(raw[i - 1]);
            }
            return (start, end, sb.ToString());
        }
        catch
        {
            return null;
        }
    }

    static List<string> SafeEnumerateFiles(string root, string fileName)
    {
        var list = new List<string>(8);
        try
        {
            foreach (var p in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                list.Add(Path.GetFullPath(p));
        }
        catch { /* ignore permission issues */ }
        return list;
    }

    static string GuessFenceLanguage(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "cs" => "csharp",
            "ts" => "ts",
            "tsx" => "tsx",
            "js" => "js",
            "jsx" => "jsx",
            "razor" or "cshtml" => "razor",
            "html" => "html",
            "css" => "css",
            "json" => "json",
            _ => string.Empty
        };
    }

    static string EscapeMd(string s)
    {
        return s.Replace("`", "\u0060");
    }

    static string NormalizePath(string p)
    {
        return p.Replace("\\", "/");
    }

    static void EnsureDirectory(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
}
