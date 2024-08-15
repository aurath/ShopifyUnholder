using System.Collections.Immutable;
using GraphQL;
using GraphQL.Client.Abstractions;
using ShopifyUnhold.Models;
using ShopifyUnhold.Exceptions;
using Microsoft.Extensions.Logging;

namespace ShopifyUnhold;

public class Unholder(IGraphQLClient client, ILogger<Unholder> logger)
{
    private readonly IGraphQLClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger<Unholder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Unhold(IEnumerable<string> ids)
    {
        // Enumerate, de-duplicate, and sort the incoming ids
        var sortedIds = ids.ToImmutableSortedSet();
        var externalId = Guid.NewGuid();

        _logger.LogInformation("Starting unhold operation on {orderCount} orders", ids.Count());
        
        var request = new GraphQLRequest
        {
            Query = @"
                    mutation fulfillmentOrdersReleaseHolds($ids: [ID!]!) {
                        fulfillmentOrdersReleaseHolds(ids: $ids) {
                            job {
                                id
                                done
                            }
                            userErrors {
                                field
                                message
                            }
                        }
                    }",
            OperationName = "fulfillmentOrdersReleaseHolds",
            Variables = new { externalId, ids = sortedIds }
        };

        var response = await _client.SendMutationAsync<FulfillmentOrdersReleaseHoldsResponse>(request);
        _logger.LogInformation("Got unhold job: {job}", response.Data.FulfillmentOrdersReleaseHolds.Job?.Id);

        // Check for user errors
        var errors = response.Data.FulfillmentOrdersReleaseHolds.UserErrors;
        if (errors.Any())
        {
            _logger.LogError("User errors were present in the response: {errors}", errors);
            throw new UserErrorsException("User errors were present in the response", errors);
        }

        _logger.LogInformation("Unhold API request sent");
    }


}