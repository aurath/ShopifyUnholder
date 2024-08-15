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

    public async Task<int> Unhold(IEnumerable<string> ids)
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

        // We get back a job object that needs to be polled until it's done
        var job = response.Data.FulfillmentOrdersReleaseHolds.Job ?? throw new Exception("Job was not populated in response");
        while (job.Done is false)
        {
            await Task.Delay(100);
            job = await UpdateJob(job, sortedIds.Count);
            _logger.LogInformation("Querying job status: {jobStatus}", job.Done);
        }

        _logger.LogInformation("Job is done");

        // Once job.Done is true, the query should be populated with a list of effected objects
        if (job.Query is null) throw new Exception("Query was not populated in done job response");

        // Each id should be present in the list of modified orders in the response
        // Any missing ids will be present in this exclusive intersection
        var missingIds = sortedIds.Except(job.Query.FulfillmentOrders.Nodes.Select(x => x.Id));

        if (missingIds.IsEmpty is false)
        {
            _logger.LogError("Some fulfillment orders were not modified: {missingIds}", missingIds);
            throw new UnmodifiedOrdersException("Some fulfillment orders were not modified", missingIds);
        }

        _logger.LogInformation("Removed holds on {count} orders", job.Query.FulfillmentOrders.Nodes.Count);
        return job.Query.FulfillmentOrders.Nodes.Count;
    }

    private async Task<Job> UpdateJob(Job job, int count)
    {
        var request = new GraphQLRequest
        {
            Query = @"
                    query($id: ID!, $count: Int!) {
                        job(id: $id) {
                            id
                            done
                            query {
                                fulfillmentOrders(first: $count) {
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
                            }
                        }
                    }",
            Variables = new { id = job.Id, count }
        };

        var response = await _client.SendQueryAsync<JobResponse>(request);
        return response.Data.Job;
    }
}