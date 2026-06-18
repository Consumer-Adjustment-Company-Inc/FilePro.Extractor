using FilePro.Extractor.Core.Extraction;

namespace FilePro.Extractor.Tests;

public class SeparatorOptionTests
{
    [Theory]
    [InlineData(",", ',')]
    [InlineData(";", ';')]
    [InlineData("|", '|')]
    [InlineData(" ", ' ')]
    public void TryParse_SingleChar_ReturnsThatChar(string raw, char expected)
    {
        bool ok = SeparatorOption.TryParse(raw, out char separator, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, separator);
    }

    [Theory]
    [InlineData("\\t")]   // literal backslash-t, the portable TSV form
    [InlineData("tab")]
    [InlineData("TAB")]
    [InlineData("Tab")]
    public void TryParse_TabAliases_ReturnTabChar(string raw)
    {
        bool ok = SeparatorOption.TryParse(raw, out char separator, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal('\t', separator);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryParse_MissingValue_FailsWithError(string? raw)
    {
        bool ok = SeparatorOption.TryParse(raw, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(";;")]
    [InlineData("ab")]
    [InlineData("::")]
    public void TryParse_MultiChar_FailsWithError(string raw)
    {
        bool ok = SeparatorOption.TryParse(raw, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("\"")]   // collides with the field-wrapper
    [InlineData("\r")]   // collides with the CRLF row terminator
    [InlineData("\n")]
    [InlineData("\0")]
    public void TryParse_ReservedChars_FailsWithError(string raw)
    {
        bool ok = SeparatorOption.TryParse(raw, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_FailedParse_LeavesSeparatorAtDefault()
    {
        SeparatorOption.TryParse("nope", out char separator, out _);

        Assert.Equal(SeparatorOption.Default, separator);
        Assert.Equal(',', SeparatorOption.Default);
    }
}
