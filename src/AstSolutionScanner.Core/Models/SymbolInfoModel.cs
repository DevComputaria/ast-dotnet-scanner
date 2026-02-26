namespace AstSolutionScanner.Core.Models;

public enum SymbolType
{
    Namespace,
    Class,
    Interface,
    Method,
    Property,
    Field
}

public record SymbolInfoModel(
    SymbolType Type,
    string ProjectName,
    string FilePath,
    string Namespace,
    string? ParentName, // Class name for methods, etc.
    string Name,
    string Accessibility,
    int Line,
    int Column,
    string? ReturnType = null,
    List<string>? Parameters = null
);
