using GraphQL;
using GraphQL.Client.Abstractions;
using ShopifyUnhold.Models;
using ShopifyUnhold.Exceptions;
using Microsoft.Extensions.Logging;

namespace ShopifyUnhold;

public class FulfillmentOrderFinder(IGraphQLClient client, ILogger<FulfillmentOrderFinder> logger)
{
    private readonly IGraphQLClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger<FulfillmentOrderFinder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<string>> Find(IEnumerable<string> names, string locationName)
    {
        // Do/while loop handles pagination, each chunk of orders is appended to the list
        FulfillmentOrderConnection data;
        var orders = new List<FulfillmentOrder>();
        string? cursor = null;
        do
        {
            data = await GetOrdersPage(cursor);
            _logger.LogInformation("Got page of held orders, size: {pageSize}", data.Nodes.Count);
            cursor = data.PageInfo.EndCursor;
            orders.AddRange(data.Nodes);
            
        } while (data.PageInfo.HasNextPage);

        _logger.LogInformation("Total orders found: {orderCount}", orders.Count);

        // Pull out the order IDs with the names we are looking for
        var matchedOrderIds = orders
            .Where(x => x.AssignedLocation.Name == locationName)
            .IntersectBy(names, x => x.OrderName)
            .Select(x => x.Id)
            .ToList();

        _logger.LogInformation("Matched {matchCount} order names to held orders", matchedOrderIds.Count);

        if (names.Count() != matchedOrderIds.Count)
        {
            // Some names weren't found
            var missingNames = names
                .Except(orders.Select(x => x.OrderName))
                .ToList();
            
            _logger.LogError("Failed to match {failedCount} order names", missingNames.Count);
            throw new FulfillmentOrdersNotFoundException("Failed to find fulfillment orders for some order names", missingNames);
        }

        return matchedOrderIds;
    }

    private async Task<FulfillmentOrderConnection> GetOrdersPage(string? cursor)
    {
        var request = new GraphQLRequest
        {
            Query = @"
                query ($cursor: String) {
	                manualHoldsFulfillmentOrders(first: 250, after: $cursor){
		                nodes {
			                id
			                orderName
                            assignedLocation {
                                name
                            }
		                }
		                pageInfo {
			                hasNextPage
			                endCursor
		                }
	                }
                }",
            Variables = new { cursor }
        };

        var response = await _client.SendQueryAsync<ManualHoldsFulfillmentOrdersResponse>(request);
        return response.Data.FulfillmentOrders;
    }
}
