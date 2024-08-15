using System.Text.RegularExpressions;

namespace ShopifyUnhold.Cli;

public partial class Range
{
    private readonly List<Chunk> _chunks;

    private Range(List<Chunk> chunks)
    {
        _chunks = chunks;
    }

    public static Range Parse(string[] args)
    {
        var chunks = args
            .SelectMany(x => x.Split(' ')) // # is a comment in powershell, so we have to quote input, this further breaks by the spaces within the quotes
            .Select(ParseChunk)
            .ToList();

        return new Range(chunks);
    }

    private static Chunk ParseChunk(string input)
    {
        input = input.Trim();

        if (input.Contains('-'))
        {
            // Parse range chunk
            var split = input.Split('-');
            var left = Match(split[0]);
            var right = Match(split[1]);
            
            if (left.prefix != right.prefix) throw new FormatException($"Inconsistent prefixes in {input}");
            return new RangeChunk(left.value, right.value, left.prefix);
        }

        // Parse single chunk
        var (prefix, value) = Match(input);
        return new SingleChunk(value, prefix);

        static (string prefix, int value) Match(string chunk)
        {
            var regex = PrefixRegex();
            var match = regex.Match(chunk);

            if (match.Success is false) throw new FormatException($"Bad format in order {chunk}");

            var prefix = match.Groups.GetValueOrDefault("prefix")?.Value ?? string.Empty;
            var value = (match.Groups.GetValueOrDefault("value")?.Value) ?? throw new FormatException($"No digits in order {chunk}");
            return (prefix, int.Parse(value));
        }
    }

    public List<string> ToList() => _chunks.SelectMany(x => x.ToList()).ToList();

    private abstract class Chunk(string? prefix = null)
    {
        protected string Prefix { get; } = prefix ?? string.Empty;

        public abstract IEnumerable<string> ToList();
    }

    private class RangeChunk(int from, int to, string? prefix = null) : Chunk(prefix)
    {
        private readonly int _from = from;

        private readonly int _to = to;

        public override IEnumerable<string> ToList()
        {
            var count = _to - _from;
            return Enumerable.Range(_from, count+1).Select(x => Prefix + x);
        }
    }

    private class SingleChunk(int value, string? prefix = null) : Chunk(prefix)
    {
        private readonly int _value = value;

        public override IEnumerable<string> ToList() => [Prefix + _value];
    }

    [GeneratedRegex("^(?'prefix'\\D*)(?'value'\\d+)$")]
    private static partial Regex PrefixRegex();
}
