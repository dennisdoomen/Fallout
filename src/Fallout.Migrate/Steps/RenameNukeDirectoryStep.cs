using System.IO;
using Fallout.Common.IO;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Renames the repository's <c>.nuke/</c> directory to <c>.fallout/</c>, or records a warning if
/// both already exist and need a manual merge.
/// </summary>
internal sealed class RenameNukeDirectoryStep : IMigrationStep
{
    /// <inheritdoc />
    public void Execute(MigrationContext context, Summary summary)
    {
        var legacy = context.RootDirectory / ".nuke";
        var canonical = context.RootDirectory / ".fallout";

        if (!legacy.DirectoryExists())
        {
            return;
        }

        if (canonical.DirectoryExists())
        {
            summary.Warnings.Add(
                "Both .nuke/ and .fallout/ exist. Skipped rename; merge their contents manually.");

            return;
        }

        context.Log.WriteLine(
            $"rename {MigrationFileOperations.RelativePath(context.RootDirectory, legacy)} -> {MigrationFileOperations.RelativePath(context.RootDirectory, canonical)}");

        if (!context.DryRun)
        {
            // We purposely use the .NET directory move as this is atomic. Fallout's own
            // Move/MoveDirectory moves them one by one.
            Directory.Move(legacy, canonical);
        }

        summary.DirectoriesRenamed++;
    }
}
