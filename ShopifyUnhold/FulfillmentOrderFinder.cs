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

    public async Task<FulfillmentOrderResults> Find(IEnumerable<string> names, string locationName)
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
        var ordersAtLocation = orders.Where(x => x.AssignedLocation.Name == locationName);
        var matchedOrderIds = ordersAtLocation
            .IntersectBy(names, x => x.OrderName)
            .Select(x => x.Id)
            .ToList();

        _logger.LogInformation("Matched {matchCount} order names to held orders", matchedOrderIds.Count);

        // Check for missing names
        var missingNames = names
            .Except(ordersAtLocation.Select(x => x.OrderName))
            .ToList();

        if (missingNames.Count is not 0)
        {
            _logger.LogWarning("Failed to match {failedCount} order names", missingNames.Count);
            _logger.LogWarning("{missingOrders}", System.Text.Json.JsonSerializer.Serialize(missingNames));
        }

        return new FulfillmentOrderResults(matchedOrderIds, missingNames);
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

    public class FulfillmentOrderResults(IEnumerable<string> fulFillmentOrders, IEnumerable<string> missingNames)
    {
        public List<string> FulfillmentOrders { get; } = fulFillmentOrders.ToList();

        public List<string> MissingNames { get; } = missingNames.ToList();
    }
}
