using System;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities.Collections;
using static Fallout.CodeGeneration.CodeGenerator;
using static Fallout.CodeGeneration.ReferenceUpdater;
using static Fallout.Common.Tools.Git.GitTasks;

partial class Build
{
    AbsolutePath SpecificationsDirectory => RootDirectory / "src" / "Fallout.Common" / "Tools";
    AbsolutePath ReferencesDirectory => RootDirectory / "docs" / "cli-tools";

    Target References => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .Executes(() =>
        {
            ReferencesDirectory.CreateOrCleanDirectory();

            UpdateReferences(SpecificationsDirectory, ReferencesDirectory);
        });

    Target GenerateTools => _ => _
        .Executes(() => GenerateAllTools());

    // CI gate: `GenerateTools` only runs when a contributor remembers to invoke it, so a .json
    // spec edited without regenerating its .Generated.cs could merge silently and ship stale
    // wrapper code. Regenerating from a known-clean checkout and asserting the working copy is
    // still clean afterward catches that drift before merge.
    Target VerifyGeneratedTools => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .Executes(() =>
        {
            GenerateAllTools();

            Assert.True(
                GitHasCleanWorkingCopy(),
                "Generated tool wrappers are out of sync with their .json specs. Run './build.ps1 GenerateTools' locally and commit the result.");
        });

    void GenerateAllTools()
    {
        SpecificationsDirectory.GlobFiles("*/*.json").ForEach(x =>
            GenerateCode(
                x,
                namespaceProvider: x => $"Fallout.Common.Tools.{x.Name}",
                sourceFileProvider: x => GitRepository.SetBranch(MainBranch).GetGitHubBrowseUrl(x.SpecificationFile)));
    }
}
