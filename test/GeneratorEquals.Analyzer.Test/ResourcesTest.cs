namespace GeneratorEquals.Analyzer.Test;

public class ResourcesTest
{
    [Fact]
    public void Resources_CanAccessGE001Strings()
    {
        // Verify all GE001 resource strings are accessible
        Assert.NotEmpty(Resources.GE001Title);
        Assert.NotEmpty(Resources.GE001MessageFormat);
        Assert.NotEmpty(Resources.GE001Description);
        Assert.NotEmpty(Resources.GE001Category);

        // Verify message format contains placeholders
        Assert.Contains("{0}", Resources.GE001MessageFormat);
        Assert.Contains("{1}", Resources.GE001MessageFormat);
    }
}
