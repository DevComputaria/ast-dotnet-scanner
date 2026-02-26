namespace AstSolutionScanner.Core.Models;

public class AstScanOptions
{
    public string? ProjectFilter { get; init; }
    public bool PublicOnly { get; init; }
    public string? MethodNamePattern { get; init; }
    public string? NamespaceFilter { get; init; }
    public bool IncludeLocation { get; init; } = true;
    public List<SymbolType>? SymbolTypes { get; init; }
}
