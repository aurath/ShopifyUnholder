using System.Text.Json.Serialization;

namespace ShopifyUnhold.Models;

public class FulfillmentOrdersReleaseHoldsResponse
{
    [JsonPropertyName("fulfillmentOrdersReleaseHolds")]
    public required FulfillmentOrdersReleaseHolds FulfillmentOrdersReleaseHolds { get; set; }
}

public class FulfillmentOrdersReleaseHolds
{
    [JsonPropertyName("job")]
    public Job? Job { get; set; }

    [JsonPropertyName("userErrors")]
    public required IList<UserError> UserErrors { get; set; }
}

