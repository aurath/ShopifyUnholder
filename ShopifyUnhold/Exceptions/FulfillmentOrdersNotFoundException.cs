namespace ShopifyUnhold.Exceptions;

public class FulfillmentOrdersNotFoundException(string message, IEnumerable<string> names) : Exception(message)
{
    public IEnumerable<string> Names { get; } = names;
}
