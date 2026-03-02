using System.CommandLine;
using AstSolutionScanner.Core.Models;
using AstSolutionScanner.Core.Services;
using AstSolutionScanner.Core.Services.Neo4j;
using AstSolutionScanner.Cli.Renderers;

// Main Path Argument
var solutionArg = new Argument<string>("path") { Description = "The path to the .sln or .csproj file." };

// Common Scan Options
var publicOnlyOpt = new Option<bool>("--public-only") { Description = "If true, only public members will be listed." };
var projectFilterOpt = new Option<string?>("--project") { Description = "Filter by project name." };
var namespaceFilterOpt = new Option<string?>("--namespace") { Description = "Filter by namespace." };
var typeOpt = new Option<SymbolType[]?>("--type") { Description = "Filter by symbol type (Class, Method, Interface, Property)." };

// Scan-only Options
var formatOpt = new Option<string>("--format") 
{ 
    Description = "The output format (text|json).",
    DefaultValueFactory = _ => "text"
};
var outputFileOpt = new Option<string?>("--output") { Description = "The file to save the output to." };

// Neo4j Options
var neo4jUriOpt = new Option<string>("--uri") 
{ 
    Description = "Neo4j URI.",
    DefaultValueFactory = _ => "neo4j://localhost:7687"
};
var neo4jUserOpt = new Option<string>("--user") 
{ 
    Description = "Neo4j Username.",
    DefaultValueFactory = _ => "neo4j"
};
var neo4jPassOpt = new Option<string>("--password") { Description = "Neo4j Password." };
var neo4jDbOpt = new Option<string?>("--database") { Description = "Neo4j Database name." };

// --- SCAN COMMAND ---
var scanCommand = new Command("scan", "Scans a solution or project and lists methods.")
{
    solutionArg, publicOnlyOpt, projectFilterOpt, namespaceFilterOpt, typeOpt, formatOpt, outputFileOpt
};

scanCommand.SetAction(async parseResult =>
{
    var path = parseResult.GetValue(solutionArg)!;
    var options = new AstScanOptions
    {
        PublicOnly = parseResult.GetValue(publicOnlyOpt),
        ProjectFilter = parseResult.GetValue(projectFilterOpt),
        NamespaceFilter = parseResult.GetValue(namespaceFilterOpt),
        SymbolTypes = parseResult.GetValue(typeOpt)?.ToList()
    };

    try
    {
        Console.WriteLine($"🔍 Scanning: {path}...");
        var solution = await new SolutionLoader().LoadSolutionAsync(path);
        var symbols = await new AstScanner().GetSymbolsAsync(solution, options);
        await new OutputRenderer().RenderAsync(symbols, parseResult.GetValue(formatOpt)!, parseResult.GetValue(outputFileOpt));
        return 0;
    }
    catch (Exception ex) 
    { 
        Console.WriteLine($"❌ Error: {ex.Message}"); 
        return 1;
    }
});

// --- NEO4J COMMANDS ---
var neo4jInitCommand = new Command("init-schema", "Initializes constraints and indexes in Neo4j.")
{
    neo4jUriOpt, neo4jUserOpt, neo4jPassOpt, neo4jDbOpt
};

neo4jInitCommand.SetAction(async parseResult =>
{
    var options = new Neo4jOptions
    {
        Uri = parseResult.GetValue(neo4jUriOpt)!,
        User = parseResult.GetValue(neo4jUserOpt)!,
        Password = parseResult.GetValue(neo4jPassOpt)!,
        Database = parseResult.GetValue(neo4jDbOpt)
    };

    try
    {
        Console.WriteLine("⚙️ Initializing Neo4j Schema...");
        using var driver = Neo4jDriverFactory.Create(options);
        var ingestor = new Neo4jIngestor(driver, options);
        await ingestor.InitializeSchemaAsync();
        Console.WriteLine("✅ Schema initialized successfully.");
        return 0;
    }
    catch (Exception ex) 
    { 
        Console.WriteLine($"❌ Error: {ex.Message}"); 
        return 1;
    }
});

var neo4jIngestCommand = new Command("ingest", "Scans and ingests metadata directly into Neo4j.")
{
    solutionArg, publicOnlyOpt, projectFilterOpt, namespaceFilterOpt, typeOpt,
    neo4jUriOpt, neo4jUserOpt, neo4jPassOpt, neo4jDbOpt
};

neo4jIngestCommand.SetAction(async parseResult =>
{
    var path = parseResult.GetValue(solutionArg)!;
    var scanOptions = new AstScanOptions
    {
        PublicOnly = parseResult.GetValue(publicOnlyOpt),
        ProjectFilter = parseResult.GetValue(projectFilterOpt),
        NamespaceFilter = parseResult.GetValue(namespaceFilterOpt),
        SymbolTypes = parseResult.GetValue(typeOpt)?.ToList()
    };

    var n4jOptions = new Neo4jOptions
    {
        Uri = parseResult.GetValue(neo4jUriOpt)!,
        User = parseResult.GetValue(neo4jUserOpt)!,
        Password = parseResult.GetValue(neo4jPassOpt)!,
        Database = parseResult.GetValue(neo4jDbOpt)
    };

    try
    {
        Console.WriteLine($"🔍 Scanning & Ingesting: {path}...");
        var solution = await new SolutionLoader().LoadSolutionAsync(path);
        Console.WriteLine($"   (Diag) Solution loaded with {solution.Projects.Count()} project(s).");
        var symbols = await new AstScanner().GetSymbolsAsync(solution, scanOptions);
        
        using var driver = Neo4jDriverFactory.Create(n4jOptions);
        var ingestor = new Neo4jIngestor(driver, n4jOptions);
        await ingestor.IngestAsync(symbols, CancellationToken.None);
        
        Console.WriteLine($"✅ Ingested {symbols.Count()} symbols into Neo4j.");
        return 0;
    }
    catch (Exception ex) 
    { 
        Console.WriteLine($"❌ Error: {ex.Message}"); 
        return 1;
    }
});

var neo4jCommand = new Command("neo4j", "Commands to interact with Neo4j.")
{
    neo4jInitCommand,
    neo4jIngestCommand
};

// --- INFO COMMAND ---
var infoCommand = new Command("info", "Displays diagnostic information about the environment.");
infoCommand.SetAction(parseResult => {
    Console.WriteLine(".NET AST Scanner v0.1");
    Console.WriteLine($"Runtime: {Environment.Version}");
    try { 
        var loader = new SolutionLoader(); 
    } catch (Exception ex) { 
        Console.WriteLine($"MSBuild Registration Error: {ex.Message}"); 
    }
    return 0;
});

// --- ROOT ---
var rootCommand = new RootCommand("dotnet-ast - Knowledge Graph for .NET solutions.")
{
    scanCommand, neo4jCommand, infoCommand
};

var parsed = rootCommand.Parse(args);
return await parsed.InvokeAsync();
