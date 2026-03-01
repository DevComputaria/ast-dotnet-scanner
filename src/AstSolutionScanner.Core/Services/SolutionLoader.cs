using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace AstSolutionScanner.Core.Services;

public interface ISolutionLoader
{
    Task<Solution> LoadSolutionAsync(string path);
}

public class SolutionLoader : ISolutionLoader, IDisposable
{
    private MSBuildWorkspace? _workspace;

    public SolutionLoader()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var instance = MSBuildLocator.RegisterDefaults();
            Console.WriteLine($"   (Log) MSBuild Registered: {instance.Name} ({instance.Version}) from {instance.MSBuildPath}");
        }
    }

    public async Task<Solution> LoadSolutionAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        var extension = Path.GetExtension(path).ToLower();
        
        var properties = new Dictionary<string, string> 
        {
            { "CheckForSystemRuntimeDependency", "true" }
        };
        
        // Keep the workspace alive as a member variable
        _workspace = MSBuildWorkspace.Create(properties);
        
        _workspace.RegisterWorkspaceFailedHandler(e => 
        {
            var color = e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{e.Diagnostic.Kind}] MSBuild: {e.Diagnostic.Message}");
            Console.ResetColor();
        });

        if (extension == ".sln" || extension == ".slnx")
        {
            return await _workspace.OpenSolutionAsync(path);
        }
        else if (extension == ".csproj")
        {
            var project = await _workspace.OpenProjectAsync(path);
            return project.Solution;
        }
        else
        {
            throw new NotSupportedException($"Extension {extension} is not supported.");
        }
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
