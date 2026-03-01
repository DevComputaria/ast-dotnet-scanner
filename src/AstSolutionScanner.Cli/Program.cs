using System.CommandLine;
using AstSolutionScanner.Core.Models;
using AstSolutionScanner.Core.Services;
using AstSolutionScanner.Cli.Renderers;

var solutionArg = new Argument<string>("path")
{
    Description = "The path to the .sln or .csproj file."
};
var publicOnlyOpt = new Option<bool>("--public-only")
{
    Description = "If true, only public members will be listed."
};
var projectFilterOpt = new Option<string?>("--project")
{
    Description = "Filter by project name."
};
var namespaceFilterOpt = new Option<string?>("--namespace")
{
    Description = "Filter by namespace."
};
var typeOpt = new Option<SymbolType[]?>("--type")
{
    Description = "Filter by symbol type (Class, Method, Interface, Property)."
};
var formatOpt = new Option<string>("--format")
{
    Description = "The output format (text|json).",
    DefaultValueFactory = _ => "text"
};
var outputFileOpt = new Option<string?>("--output")
{
    Description = "The file to save the output to."
};

var scanCommand = new Command("scan", "Scans a solution or project and lists methods.")
{
    solutionArg,
    publicOnlyOpt,
    projectFilterOpt,
    namespaceFilterOpt,
    typeOpt,
    formatOpt,
    outputFileOpt
};

scanCommand.SetAction(async parseResult =>
{
    var path = parseResult.GetValue(solutionArg)!;
    var publicOnly = parseResult.GetValue(publicOnlyOpt);
    var projectFilter = parseResult.GetValue(projectFilterOpt);
    var namespaceFilter = parseResult.GetValue(namespaceFilterOpt);
    var types = parseResult.GetValue(typeOpt);
    var format = parseResult.GetValue(formatOpt)!;
    var output = parseResult.GetValue(outputFileOpt);

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
});

var infoCommand = new Command("info", "Displays diagnostic information about the environment.");
infoCommand.SetAction(_ =>
{
    Console.WriteLine(".NET AST Scanner v0.1");
    Console.WriteLine($"Runtime: {Environment.Version}");
    Console.WriteLine($"OS: {Environment.OSVersion}");

    try
    {
        var loader = new SolutionLoader();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MSBuild Registration Error: {ex.Message}");
    }
});

var rootCommand = new RootCommand("dotnet-ast - A powerful tool to scan and analyze .NET solutions using Roslyn.")
{
    scanCommand,
    infoCommand
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
