namespace MiniCSharp;

public class InternalCompilerException : Exception
{
    public string SourceFile { get; init; }
    public string SourceLine { get; init; }
    public string DebugInformation { get; init; }
    
    public InternalCompilerException()
    {
        SourceFile = string.Empty;
        SourceLine = string.Empty;
        DebugInformation = "<No details>";
    }

    public InternalCompilerException(string? message, string sourceFile = "", string sourceLine = "", string debugInformation = "") : base(message)
    {
        SourceFile = sourceFile;
        SourceLine = sourceLine;
        DebugInformation = debugInformation;
    }

    public InternalCompilerException(string? message, Exception? innerException, string sourceFile = "", string sourceLine = "", string debugInformation = "") : base(message, innerException)
    {
        SourceFile = sourceFile;
        SourceLine = sourceLine;
        DebugInformation = debugInformation;
    }
}