using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class ModeNameSuggesterTests
{
    [Theory]
    [InlineData("localhost (qwen2.5-coder:14b)", "localhost_qwen2.5-coder_14b")]
    [InlineData("api.openai.com (gpt-4)", "api.openai.com_gpt-4")]
    [InlineData("hello", "hello")]
    [InlineData("a/b\\c?d*e", "a_b_c_d_e")]
    [InlineData("multiple   spaces", "multiple_spaces")]
    [InlineData("__leading_and_trailing__", "leading_and_trailing")]
    public void ToFolderName_SanitisesAndCollapses(string input, string expected)
    {
        ModeNameSuggester.ToFolderName(input).Should().Be(expected);
    }

    [Fact]
    public void ToFolderName_AllNonAscii_ReturnsEmpty()
    {
        ModeNameSuggester.ToFolderName("Жанклод").Should().Be("");
    }

    [Fact]
    public void SuggestFromSettings_ExtractsHostAndModel()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var json = """
            {
              "env": {
                "ANTHROPIC_BASE_URL": "http://localhost:11434",
                "model": "qwen2.5-coder:14b"
              }
            }
            """;
        fsMock.OpenRead("settings.json")
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

        var result = ModeNameSuggester.SuggestFromSettings("settings.json", fsMock);

        result.Should().Be("localhost (qwen2.5-coder:14b)");
    }

    [Fact]
    public void SuggestFromSettings_FallsBackToTopLevelModel()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var json = """
            {
              "env": { "ANTHROPIC_BASE_URL": "https://api.openai.com" },
              "model": "gpt-4"
            }
            """;
        fsMock.OpenRead(Arg.Any<string>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

        ModeNameSuggester.SuggestFromSettings("s.json", fsMock)
            .Should().Be("api.openai.com (gpt-4)");
    }

    [Fact]
    public void SuggestFromSettings_MissingFile_ReturnsNull()
    {
        var fsMock = Substitute.For<IFileSystem>();
        fsMock.OpenRead(Arg.Any<string>()).Returns(x => throw new FileNotFoundException());

        ModeNameSuggester.SuggestFromSettings("missing.json", fsMock).Should().BeNull();
    }

    [Fact]
    public void SuggestFromSettings_MalformedJson_ReturnsNull()
    {
        var fsMock = Substitute.For<IFileSystem>();
        fsMock.OpenRead(Arg.Any<string>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{ not json")));

        ModeNameSuggester.SuggestFromSettings("bad.json", fsMock).Should().BeNull();
    }

    [Fact]
    public void SuggestFromSettings_NoBaseUrl_ReturnsNull()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var json = """{ "env": { "model": "x" } }""";
        fsMock.OpenRead(Arg.Any<string>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

        ModeNameSuggester.SuggestFromSettings("s.json", fsMock).Should().BeNull();
    }

    [Fact]
    public void SuggestFromSettings_NoModel_ReturnsNull()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var json = """{ "env": { "ANTHROPIC_BASE_URL": "http://x" } }""";
        fsMock.OpenRead(Arg.Any<string>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

        ModeNameSuggester.SuggestFromSettings("s.json", fsMock).Should().BeNull();
    }
}
