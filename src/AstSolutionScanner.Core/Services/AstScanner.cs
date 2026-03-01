using AstSolutionScanner.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AstSolutionScanner.Core.Services;

public interface IAstScanner
{
    Task<IEnumerable<SymbolInfoModel>> GetSymbolsAsync(Solution solution, AstScanOptions options);
}

public class AstScanner : IAstScanner
{
    private static readonly SymbolDisplayFormat CanonicalFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public async Task<IEnumerable<SymbolInfoModel>> GetSymbolsAsync(Solution solution, AstScanOptions options)
    {
        var symbols = new List<SymbolInfoModel>();

        foreach (var project in solution.Projects)
        {
            if (!string.IsNullOrEmpty(options.ProjectFilter) && 
                !project.Name.Contains(options.ProjectFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var document in project.Documents)
            {
                if (document.SourceCodeKind != SourceCodeKind.Regular) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                var semanticModel = await document.GetSemanticModelAsync();
                if (semanticModel == null) continue;

                var nodes = syntaxRoot.DescendantNodes().ToList();
                
                foreach (var node in nodes)
                {
                    ISymbol? symbol = node switch
                    {
                        MethodDeclarationSyntax m => semanticModel.GetDeclaredSymbol(m),
                        ClassDeclarationSyntax c => semanticModel.GetDeclaredSymbol(c),
                        InterfaceDeclarationSyntax i => semanticModel.GetDeclaredSymbol(i),
                        PropertyDeclarationSyntax p => semanticModel.GetDeclaredSymbol(p),
                        _ => null
                    };

                    if (symbol == null) continue;

                    var type = symbol switch
                    {
                        IMethodSymbol => SymbolType.Method,
                        INamedTypeSymbol s when s.TypeKind == TypeKind.Class => SymbolType.Class,
                        INamedTypeSymbol s when s.TypeKind == TypeKind.Interface => SymbolType.Interface,
                        IPropertySymbol => SymbolType.Property,
                        _ => (SymbolType?)null
                    };

                    if (type == null) continue;

                    if (options.SymbolTypes != null && options.SymbolTypes.Any() && !options.SymbolTypes.Contains(type.Value)) continue;
                    if (options.PublicOnly && symbol.DeclaredAccessibility != Accessibility.Public) continue;

                    var namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? "";
                    if (!string.IsNullOrEmpty(options.NamespaceFilter) && 
                        !namespaceName.Contains(options.NamespaceFilter, StringComparison.OrdinalIgnoreCase)) continue;

                    if (!string.IsNullOrEmpty(options.MethodNamePattern) && 
                        !symbol.Name.Contains(options.MethodNamePattern, StringComparison.OrdinalIgnoreCase)) continue;

                    var parentName = symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "";
                    var lineSpan = node.GetLocation().GetLineSpan();

                    string? returnType = null;
                    List<string>? parameters = null;
                    List<ParameterInfoModel>? structuredParameters = null;
                    List<string>? modifiers = GetModifiers(symbol);
                    List<string>? attributes = symbol.GetAttributes().Select(a => a.AttributeClass?.Name ?? "").ToList();
                    string? baseType = null;
                    List<string>? interfaces = null;
                    List<string>? calls = null;

                    if (symbol is INamedTypeSymbol typeSymbol)
                    {
                        baseType = typeSymbol.BaseType?.ToDisplayString();
                        interfaces = typeSymbol.AllInterfaces.Select(i => i.ToDisplayString()).ToList();
                    }

                    if (symbol is IMethodSymbol methodSymbol)
                    {
                        returnType = methodSymbol.ReturnType.ToDisplayString();
                        parameters = methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}").ToList();
                        structuredParameters = methodSymbol.Parameters.Select(p => new ParameterInfoModel(
                            p.Name,
                            p.Type.ToDisplayString(),
                            p.Type.IsReferenceType || p.NullableAnnotation == NullableAnnotation.Annotated,
                            p.HasExplicitDefaultValue,
                            p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                            p.RefKind.ToString().ToLower()
                        )).ToList();

                        // Call discovery (Simplified)
                        calls = node.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .Select(inv => semanticModel.GetSymbolInfo(inv).Symbol)
                            .Where(s => s != null)
                            .Select(s => s!.ToDisplayString(CanonicalFormat))
                            .Distinct()
                            .ToList();
                    }
                    else if (symbol is IPropertySymbol propertySymbol)
                    {
                        returnType = propertySymbol.Type.ToDisplayString();
                    }

                    symbols.Add(new SymbolInfoModel(
                        type.Value,
                        project.Name,
                        document.FilePath ?? "Unknown",
                        namespaceName,
                        parentName,
                        symbol.Name,
                        symbol.DeclaredAccessibility.ToString(),
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        returnType,
                        
                        // V2 Meta
                        SymbolId: symbol.ToDisplayString(CanonicalFormat),
                        FullyQualifiedName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Modifiers: modifiers,
                        Attributes: attributes,
                        BaseType: baseType,
                        Interfaces: interfaces,
                        StructuredParameters: structuredParameters,
                        Calls: calls,
                        
                        Parameters: parameters
                    ));
                }
            }
        }

        return symbols;
    }

    private List<string> GetModifiers(ISymbol symbol)
    {
        var list = new List<string>();
        if (symbol.IsStatic) list.Add("static");
        if (symbol.IsAbstract) list.Add("abstract");
        if (symbol.IsSealed) list.Add("sealed");
        if (symbol.IsVirtual) list.Add("virtual");
        if (symbol.IsOverride) list.Add("override");
        
        if (symbol is IMethodSymbol m && m.IsAsync) list.Add("async");
        
        return list;
    }
}
