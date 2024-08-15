using System.Text.Json.Serialization;

namespace ShopifyUnhold.Models;

public class Job
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("done")]
    public required bool Done { get; set; }

    [JsonPropertyName("query")]
    public FulfillmentOrdersResponse? Query { get; set;}
}

public class JobResponse
{
    [JsonPropertyName("job")]
    public required Job Job { get; set; }
}