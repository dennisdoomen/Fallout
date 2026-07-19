using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fallout.Common.Git;
using Xunit;

namespace Fallout.Common.Tests;

public class GitRepositoryTest
{
    [Theory]
    [InlineData("https://github.com/nuke-build", "github.com", "nuke-build")]
    [InlineData("https://github.com/nuke-build/", "github.com", "nuke-build")]
    [InlineData("https://github.com/nuke-build/nuke", "github.com", "nuke-build/nuke")]
    [InlineData("https://github.com/nuke-build/nuke.git", "github.com", "nuke-build/nuke")]
    [InlineData("https://user:pass@github.com/nuke-build/nuke.git", "github.com", "nuke-build/nuke")]
    [InlineData(" https://github.com/TdMxm/nuke.git", "github.com", "TdMxm/nuke")]
    [InlineData("git@git.test.org:test", "git.test.org", "test")]
    [InlineData("git@git.test.org/test", "git.test.org", "test")]
    [InlineData("git@git.test.org/test/", "git.test.org", "test")]
    [InlineData("git@git.test.org/test.git", "git.test.org", "test")]
    [InlineData("ssh://git@git.test.org/test.git", "git.test.org", "test")]
    [InlineData("ssh://git@git.test.org:1234/test.git", "git.test.org", "test")]
    [InlineData("ssh://git.test.org/test/test", "git.test.org", "test/test")]
    [InlineData("ssh://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("https://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("git://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("git://git.test.org/test/test", "git.test.org", "test/test")]
    public void FromUrlTest(string url, string endpoint, string identifier)
    {
        var repository = GitRepository.FromUrl(url);
        repository.Endpoint.Should().Be(endpoint);
        repository.Identifier.Should().Be(identifier);
    }

    [Theory]
    [InlineData("https://github.com/nuke-build", GitProtocol.Https)]
    [InlineData("git@git.test.org:test", GitProtocol.Ssh)]
    [InlineData("ssh://git.test.org:1234/test/test", GitProtocol.Ssh)]
    [InlineData("git://git.test.org:1234/test/test", GitProtocol.Ssh)]
    public void FromUrlProtocolTest(string url, GitProtocol protocol)
    {
        var repository = GitRepository.FromUrl(url);
        repository.Protocol.Should().Be(protocol);
    }

    [Fact]
    public void FromDirectoryTest()
    {
        var repository = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory()).NotNull();
        repository.Endpoint.Should().NotBeNullOrEmpty();
        repository.Identifier.Should().NotBeNullOrEmpty();
        repository.LocalDirectory.Should().NotBeNull();
        repository.Head.Should().NotBeNullOrEmpty();
        repository.Commit.Should().NotBeNullOrEmpty();
        repository.Tags.Should().NotBeNull();
    }

    [Fact]
    public void FromDirectoryTest_WithDuplicateVscodeMergeBaseKey_DoesNotThrow()
    {
        // VS Code's Git extension is known to append a duplicate `vscode-merge-base` line to the
        // `[branch "<name>"]` section instead of updating it in place, which used to crash
        // GetRemoteNameAndBranch's ToDictionary call. See regression report for details.
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            RunGit(directory, "init --quiet --initial-branch=main");
            RunGit(directory, "config user.email test@example.com");
            RunGit(directory, "config user.name Test");
            File.WriteAllText(Path.Combine(directory, "file.txt"), "content");
            RunGit(directory, "add file.txt");
            RunGit(directory, "commit --quiet -m initial");
            RunGit(directory, "remote add origin https://github.com/nuke-build/nuke.git");
            RunGit(directory, "config branch.main.remote origin");
            RunGit(directory, "config branch.main.merge refs/heads/main");

            var configPath = Path.Combine(directory, ".git", "config");
            File.AppendAllText(
                configPath,
                "\tvscode-merge-base = origin/main" + Environment.NewLine +
                "\tvscode-merge-base = origin/main" + Environment.NewLine);

            var repository = GitRepository.FromLocalDirectory(directory);

            repository.Branch.Should().Be("main");
            repository.Identifier.Should().Be("nuke-build/nuke");
        }
        finally
        {
            ForceDeleteDirectory(directory);
        }
    }

    private static void ForceDeleteDirectory(string directory)
    {
        // Git marks object files read-only, which trips up Directory.Delete on Windows.
        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(directory, recursive: true);
    }

    private static void RunGit(string workingDirectory, string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo).NotNull();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, process.StandardError.ReadToEnd());
    }
}
