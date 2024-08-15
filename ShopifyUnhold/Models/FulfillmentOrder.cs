using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShopifyUnhold.Models;

public class FulfillmentOrder
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("orderName")]
    public required string OrderName { get; set; }
}

public class FulfillmentOrdersResponse
{
    [JsonPropertyName("fulfillmentOrders")]
    public required FulfillmentOrderConnection FulfillmentOrders { get; set; }
}

public class FulfillmentOrderConnection
{
    [JsonPropertyName("nodes")]
    public required IList<FulfillmentOrder> Nodes { get; set; }

    [JsonPropertyName("pageInfo")]
    public required PageInfo PageInfo { get; set; }
}

public class PageInfo
{
    [JsonPropertyName("hasNextPage")]
    public required bool HasNextPage { get; set; }

    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; set; }
}