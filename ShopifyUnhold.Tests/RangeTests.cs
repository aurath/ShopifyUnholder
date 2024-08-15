namespace ShopifyUnhold.Tests;

public class RangeTests
{
    [Theory]
    [InlineData("#1001", new string[] {"#1001"})]
    [InlineData("#1001 #1002", new string[] {"#1001", "#1002"})]
    [InlineData("#1001 #1005-#1007", new string[] {"#1001", "#1005", "#1006", "#1007"})]
    [InlineData("#A1001 #A1005-#A1007", new string[] { "#A1001", "#A1005", "#A1006", "#A1007" })]
    [InlineData("#1001-#1003 #1005-#1007", new string[] { "#1001", "#1002", "#1003", "#1005", "#1006", "#1007" })]
    public void Range_ParseValid_Success(string input, string[] expected)
    {
        string[] args = [input];
        var range = Cli.Range.Parse(args).ToList();

        Assert.Equal(expected, range);
    }
}
