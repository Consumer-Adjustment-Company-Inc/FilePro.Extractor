using FilePro.Extractor.Core.Extraction;

namespace FilePro.Extractor.Tests;

public class ExtractionOptionsTests
{
    [Fact]
    public void Separator_DefaultsToComma_NotNul()
    {
        // Regression guard: a Core consumer that omits Separator must NOT get '\0'
        // (which would emit NUL-delimited, corrupt CSV).
        var options = new ExtractionOptions { Root = "/tmp" };

        Assert.Equal(',', options.Separator);
        Assert.NotEqual('\0', options.Separator);
    }

    [Fact]
    public void IncludeDeleted_DefaultsToTrue()
    {
        var options = new ExtractionOptions { Root = "/tmp" };

        Assert.True(options.IncludeDeleted);
    }
}
