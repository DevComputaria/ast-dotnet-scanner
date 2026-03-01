using System.Text.Json;
using AstSolutionScanner.Core.Models;

namespace AstSolutionScanner.Cli.Renderers;

public class OutputRenderer
{
    public async Task RenderAsync(IEnumerable<SymbolInfoModel> symbols, string format, string? outputFile)
    {
        string content;

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            content = JsonSerializer.Serialize(symbols, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
        }
        else
        {
            // Default: Table/Text
            var lines = new List<string>();
            lines.Add($"{"Type",-10} | {"Member",-40} | {"Parent",-25} | {"Line",-5}");
            lines.Add(new string('-', 90));

            foreach (var s in symbols)
            {
                lines.Add($"{s.Type,-10} | {s.Name,-40} | {s.ParentName,-25} | {s.Line,-5}");
            }
            content = string.Join(Environment.NewLine, lines);
        }

        if (!string.IsNullOrEmpty(outputFile))
        {
            await File.WriteAllTextAsync(outputFile, content);
            Console.WriteLine($"✅ Output saved to: {outputFile}");
        }
        else
        {
            Console.WriteLine(content);
            Console.WriteLine();
            Console.WriteLine($"Total symbols found: {symbols.Count()}");
        }
    }
}
