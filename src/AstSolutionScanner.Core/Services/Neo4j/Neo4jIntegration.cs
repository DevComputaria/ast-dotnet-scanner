using Neo4j.Driver;

namespace AstSolutionScanner.Core.Services.Neo4j;

public sealed class Neo4jOptions
{
    public required string Uri { get; init; }
    public required string User { get; init; }
    public required string Password { get; init; }
    public string? Database { get; init; }
    public TimeSpan MaxRetryTime { get; init; } = TimeSpan.FromSeconds(60);
    public int BatchSize { get; init; } = 1000;
}

public static class Neo4jDriverFactory
{
    public static IDriver Create(Neo4jOptions opt)
    {
        return GraphDatabase.Driver(
            opt.Uri,
            AuthTokens.Basic(opt.User, opt.Password),
            o => o.WithMaxTransactionRetryTime(opt.MaxRetryTime)
        );
    }
}
