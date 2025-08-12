// AiCrashContextService.cs
// A lightweight service to turn a stack trace + local repo into paste-ready Markdown
// Works with C#/.NET stack traces and JS-style browser frames.
// .NET 6+ compatible.

namespace CrashContext;

/// <summary>
/// Options for building AI crash context.
/// </summary>
public class CrashContextOptions
{
    /// <summary>Absolute or relative path to the repo root to search for files.</summary>
    public string RepoRoot { get; set; } = ".";

    /// <summary>Lines of context to include before and after the target line.</summary>
    public int ContextLines { get; set; } = 30;

    /// <summary>Max number of frames to include from the top of the stack.</summary>
    public int MaxFrames { get; set; } = 6;

    /// <summary>Optional output file path for the generated Markdown. If null, caller can use the returned string.</summary>
    public string? OutputMarkdownPath { get; set; }
}

public record FrameInfo(
    string Raw,
    string FilePath,
    int Line,
    string Kind // "cs" or "js"
);

public record SymbolicatedFrame(
    FrameInfo Frame,
    string? ResolvedPath,
    int? StartLine,
    int? EndLine,
    string? SnippetBody
);

public interface IAiCrashContextService
{
    /// <summary>Builds a Markdown document that includes the original stack trace and code snippets around the top frames.</summary>
    string BuildMarkdown(string stackTraceText, CrashContextOptions options);

    /// <summary>Symbolicates frames without producing Markdown (useful for custom UIs).</summary>
    IReadOnlyList<SymbolicatedFrame> Symbolicate(string stackTraceText, CrashContextOptions options);
}

