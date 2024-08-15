namespace ShopifyUnhold.Exceptions;

public class UnmodifiedOrdersException(string message, IEnumerable<string> orders) : Exception(message)
{
    public IEnumerable<string> Orders { get; } = orders;
}
