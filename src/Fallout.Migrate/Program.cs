using Spectre.Console.Cli;

namespace Fallout.Migrate;

/// <summary>
/// Entry point for the <c>fallout-migrate</c> CLI tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Configures and runs the <see cref="MigrateCommand"/> as a single-command Spectre.Console.Cli app.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the tool.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args)
    {
        var app = new CommandApp<MigrateCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("fallout-migrate");
            config.AddExample("--dry-run");
            config.AddExample("path/to/repo");
        });

        return app.Run(args);
    }
}
