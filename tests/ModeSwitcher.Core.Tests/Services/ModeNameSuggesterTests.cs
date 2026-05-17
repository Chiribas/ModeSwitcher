using FluentAssertions;
using ModeSwitcher.Core.Services;
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
}
