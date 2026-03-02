using System.Text.Json.Serialization;

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

public record ParameterInfoModel(
    string Name,
    string Type,
    bool IsNullable,
    bool HasDefaultValue,
    string? DefaultValue,
    string Modifier // in, ref, out, params, none
);

public record SymbolInfoModel(
    SymbolType Type,
    string ProjectName,
    string FilePath,
    string Namespace,
    string? ParentName,
    string Name,
    string Accessibility,
    int Line,
    int Column,
    string? ReturnType = null,
    
    // V2 Meta
    string? SymbolId = null,
    string? ParentSymbolId = null,
    string? FullyQualifiedName = null,
    List<string>? Modifiers = null,
    List<string>? Attributes = null,
    string? BaseType = null,
    List<string>? Interfaces = null,
    List<ParameterInfoModel>? StructuredParameters = null,
    List<string>? Calls = null,
    
    // Legacy support (to be deprecated)
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    List<string>? Parameters = null
);
