using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace AstSolutionScanner.Core.Services;

public interface ISolutionLoader
{
    Task<Solution> LoadSolutionAsync(string path);
}

public class SolutionLoader : ISolutionLoader
{
    public SolutionLoader()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    public async Task<Solution> LoadSolutionAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Solution path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("Solution file not found.", path);

        using var workspace = MSBuildWorkspace.Create();
        
        // Connect to any diagnostic events if needed
        workspace.WorkspaceFailed += (sender, e) => 
        {
            Console.WriteLine($"Workspace warning: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(path);
        return solution;
    }
}
