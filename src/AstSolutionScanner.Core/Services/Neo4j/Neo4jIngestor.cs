using AstSolutionScanner.Core.Models;
using Neo4j.Driver;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AstSolutionScanner.Core.Services.Neo4j;

public static class CypherQueries
{
    public const string InitSchema = @"
        CREATE CONSTRAINT project_name IF NOT EXISTS FOR (p:Project) REQUIRE p.name IS UNIQUE;
        CREATE CONSTRAINT namespace_id IF NOT EXISTS FOR (n:Namespace) REQUIRE (n.projectName, n.name) IS UNIQUE;
        CREATE CONSTRAINT class_symbol IF NOT EXISTS FOR (c:Class) REQUIRE c.symbolId IS UNIQUE;
        CREATE CONSTRAINT method_symbol IF NOT EXISTS FOR (m:Method) REQUIRE m.symbolId IS UNIQUE;
        CREATE CONSTRAINT property_symbol IF NOT EXISTS FOR (p:Property) REQUIRE p.symbolId IS UNIQUE;
        CREATE CONSTRAINT interface_symbol IF NOT EXISTS FOR (i:Interface) REQUIRE i.symbolId IS UNIQUE;
    ";

    public const string UpsertProjectsAndNamespaces = @"
        UNWIND $rows AS row
        MERGE (proj:Project {name: row.projectName})
        MERGE (ns:Namespace {projectName: row.projectName, name: row.namespace})
        MERGE (proj)-[:HAS_NAMESPACE]->(ns)
    ";

    public const string UpsertClasses = @"
        UNWIND $rows AS row
        WITH row WHERE row.type = 'Class'
        MERGE (ns:Namespace {projectName: row.projectName, name: row.namespace})
        MERGE (c:Class {symbolId: row.symbolId})
        SET c += {
            name: row.name,
            projectName: row.projectName,
            namespace: row.namespace,
            filePath: row.filePath,
            accessibility: row.accessibility,
            line: row.line,
            column: row.column,
            fullyQualifiedName: row.fullyQualifiedName,
            baseType: row.baseType,
            attributes: row.attributes,
            modifiers: row.modifiers
        }
        MERGE (ns)-[:DECLARES]->(c)
    ";

    public const string UpsertInterfaces = @"
        UNWIND $rows AS row
        WITH row WHERE row.type = 'Interface'
        MERGE (ns:Namespace {projectName: row.projectName, name: row.namespace})
        MERGE (i:Interface {symbolId: row.symbolId})
        SET i += {
            name: row.name,
            projectName: row.projectName,
            namespace: row.namespace,
            filePath: row.filePath,
            accessibility: row.accessibility,
            line: row.line,
            column: row.column,
            fullyQualifiedName: row.fullyQualifiedName,
            attributes: row.attributes,
            modifiers: row.modifiers
        }
        MERGE (ns)-[:DECLARES]->(i)
    ";

    public const string UpsertInheritance = @"
        UNWIND $rows AS row
        WITH row WHERE row.type IN ['Class', 'Interface']
        MATCH (child {symbolId: row.symbolId})
        
        // Base Type Inheritance
        FOREACH (ignore IN CASE WHEN row.baseType IS NOT NULL AND row.baseType <> 'object' THEN [1] ELSE [] END |
            MERGE (base {symbolId: row.baseType})
            MERGE (child)-[:EXTENDS]->(base)
        )
        
        // Interface Implementation
        FOREACH (ifaceId IN row.interfaces |
            MERGE (iface:Interface {symbolId: ifaceId})
            MERGE (child)-[:IMPLEMENTS]->(iface)
        )
    ";

    public const string UpsertMethods = @"
        UNWIND $rows AS row
        WITH row WHERE row.type = 'Method'
        MERGE (m:Method {symbolId: row.symbolId})
        SET m += {
            name: row.name,
            projectName: row.projectName,
            namespace: row.namespace,
            filePath: row.filePath,
            accessibility: row.accessibility,
            line: row.line,
            column: row.column,
            fullyQualifiedName: row.fullyQualifiedName,
            returnType: row.returnType,
            modifiers: row.modifiers,
            attributes: row.attributes
        }
        WITH row, m
        MATCH (parent {symbolId: row.parentSymbolId})
        MERGE (parent)-[:DECLARES_METHOD]->(m)
    ";

    public const string UpsertProperties = @"
        UNWIND $rows AS row
        WITH row WHERE row.type = 'Property'
        MERGE (p:Property {symbolId: row.symbolId})
        SET p += {
            name: row.name,
            projectName: row.projectName,
            namespace: row.namespace,
            filePath: row.filePath,
            accessibility: row.accessibility,
            line: row.line,
            column: row.column,
            fullyQualifiedName: row.fullyQualifiedName,
            returnType: row.returnType,
            modifiers: row.modifiers,
            attributes: row.attributes
        }
        WITH row, p
        MATCH (parent {symbolId: row.parentSymbolId})
        MERGE (parent)-[:DECLARES_PROPERTY]->(p)
    ";
    
    public const string UpsertCalls = @"
        UNWIND $rows AS row
        WITH row WHERE row.type = 'Method' AND row.calls IS NOT NULL
        UNWIND row.calls AS callSymbolId
        MATCH (caller:Method {symbolId: row.symbolId})
        MERGE (callee:Method {symbolId: callSymbolId})
        MERGE (caller)-[:CALLS]->(callee)
    ";

    // Optimized batch queries for specific types (recommended over complex apoc logic if possible)
    // Clean specialized queries removed in favor of explicit ones above
}

public sealed class Neo4jIngestor
{
    private readonly IDriver _driver;
    private readonly Neo4jOptions _opt;

    public Neo4jIngestor(IDriver driver, Neo4jOptions opt)
    {
        _driver = driver;
        _opt = opt;
    }

    public async Task InitializeSchemaAsync()
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase(_opt.Database ?? "neo4j"));
        
        // Execute constraint creation (one by one or split if needed)
        var queries = CypherQueries.InitSchema.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var query in queries)
        {
            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query));
        }
    }

    public async Task IngestAsync(IEnumerable<SymbolInfoModel> symbols, CancellationToken ct)
    {
        var batches = symbols
            .Select((s, i) => new { s, i })
            .GroupBy(x => x.i / _opt.BatchSize)
            .Select(g => g.Select(x => x.s).ToList());

        foreach (var batch in batches)
        {
            var rows = batch.Select(ToRowMap).ToList();

            await using var session = _driver.AsyncSession(o => o.WithDatabase(_opt.Database ?? "neo4j"));
            
            await session.ExecuteWriteAsync(async tx =>
            {
                // Sequence of insertion to maintain integrity
                await tx.RunAsync(CypherQueries.UpsertProjectsAndNamespaces, new { rows });
                await tx.RunAsync(CypherQueries.UpsertClasses, new { rows });
                await tx.RunAsync(CypherQueries.UpsertInterfaces, new { rows });
                await tx.RunAsync(CypherQueries.UpsertInheritance, new { rows });
                await tx.RunAsync(CypherQueries.UpsertMethods, new { rows });
                await tx.RunAsync(CypherQueries.UpsertProperties, new { rows });
                await tx.RunAsync(CypherQueries.UpsertCalls, new { rows });
            });
        }
    }

    private string? SanitizeId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 4000) return id;

        // Use SHA256 to create a unique but reasonably sized ID for Neo4j indices
        var bytes = Encoding.UTF8.GetBytes(id);
        var hash = SHA256.HashData(bytes);
        return "HUGE_" + Convert.ToHexString(hash);
    }

    private Dictionary<string, object?> ToRowMap(SymbolInfoModel s)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = s.Type.ToString(),
            ["symbolId"] = SanitizeId(s.SymbolId),
            ["projectName"] = s.ProjectName,
            ["namespace"] = s.Namespace,
            ["name"] = s.Name,
            ["parentSymbolId"] = SanitizeId(s.ParentSymbolId),
            ["filePath"] = s.FilePath,
            ["accessibility"] = s.Accessibility,
            ["line"] = s.Line,
            ["column"] = s.Column,
            ["fullyQualifiedName"] = s.FullyQualifiedName,
            ["returnType"] = s.ReturnType,
            ["modifiers"] = s.Modifiers,
            ["attributes"] = s.Attributes,
            ["baseType"] = SanitizeId(s.BaseType),
            ["interfaces"] = s.Interfaces?.Select(SanitizeId).ToList(),
            ["calls"] = s.Calls?.Select(SanitizeId).ToList()
        };
    }
}
