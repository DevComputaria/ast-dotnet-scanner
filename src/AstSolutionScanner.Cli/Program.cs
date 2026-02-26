using System.CommandLine;
using AstSolutionScanner.Core.Models;
using AstSolutionScanner.Core.Services;
using AstSolutionScanner.Cli.Renderers;

var rootCommand = new RootCommand("dotnet-ast - A powerful tool to scan and analyze .NET solutions using Roslyn.");

var scanCommand = new Command("scan", "Scans a solution or project and lists methods.");

var solutionArg = new Argument<string>("path", "The path to the .sln or .csproj file.");
var publicOnlyOpt = new Option<bool>("--public-only", "If true, only public members will be listed.");
var projectFilterOpt = new Option<string?>("--project", "Filter by project name.");
var namespaceFilterOpt = new Option<string?>("--namespace", "Filter by namespace.");
var typeOpt = new Option<SymbolType[]?>("--type", "Filter by symbol type (Class, Method, Interface, Property).");
var formatOpt = new Option<string>("--format", () => "text", "The output format (text|json).");
var outputFileOpt = new Option<string?>("--output", "The file to save the output to.");

scanCommand.AddArgument(solutionArg);
scanCommand.AddOption(publicOnlyOpt);
scanCommand.AddOption(projectFilterOpt);
scanCommand.AddOption(namespaceFilterOpt);
scanCommand.AddOption(typeOpt);
scanCommand.AddOption(formatOpt);
scanCommand.AddOption(outputFileOpt);

scanCommand.SetHandler(async (string path, bool publicOnly, string? projectFilter, string? namespaceFilter, SymbolType[]? types, string format, string? output) =>
{
    try 
    {
        Console.WriteLine($"🔍 Scanning: {path}...");
        
        var loader = new SolutionLoader();
        var solution = await loader.LoadSolutionAsync(path);

        var scanner = new AstScanner();
        var options = new AstScanOptions
        {
            PublicOnly = publicOnly,
            ProjectFilter = projectFilter,
            NamespaceFilter = namespaceFilter,
            SymbolTypes = types?.ToList()
        };

        var symbols = await scanner.GetSymbolsAsync(solution, options);
        
        var renderer = new OutputRenderer();
        await renderer.RenderAsync(symbols, format, output);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error: {ex.Message}");
        Console.ResetColor();
    }
}, solutionArg, publicOnlyOpt, projectFilterOpt, namespaceFilterOpt, typeOpt, formatOpt, outputFileOpt);

var infoCommand = new Command("info", "Displays diagnostic information about the environment.");
infoCommand.SetHandler(() => 
{
    Console.WriteLine(".NET AST Scanner v0.1");
    Console.WriteLine($"Runtime: {Environment.Version}");
    Console.WriteLine($"OS: {Environment.OSVersion}");
    // MSBuild info could be added here if we want to be more detailed
});

rootCommand.AddCommand(scanCommand);
rootCommand.AddCommand(infoCommand);

return await rootCommand.InvokeAsync(args);
