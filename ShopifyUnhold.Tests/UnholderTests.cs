using GraphQL;
using GraphQL.Client.Abstractions;
using Moq;
using ShopifyUnhold.Models;

namespace ShopifyUnhold.Tests
{
    public class UnholderTests
    {
        [Fact]
        public async Task Unhold_WithValidResponse_ReturnsWithoutExceptions()
        {
            // Arrange
            var ids = Enumerable.Range(0, 10).Select(x => $"gid://shopify/FulfillmentOrder/{x}");

            var mutationResponse = new GraphQLResponse<FulfillmentOrdersReleaseHoldsResponse>
            {
                Data = new FulfillmentOrdersReleaseHoldsResponse
                {
                    FulfillmentOrdersReleaseHolds = new FulfillmentOrdersReleaseHolds
                    {
                        Job = new Job
                        {
                            Done = false,
                            Id = ""
                        },
                        UserErrors = []
                    }
                }
            };

            var queryResponse = new GraphQLResponse<JobResponse>
            {
                Data = new JobResponse
                {
                    Job = new Job
                    {
                        Done = true,
                        Id = "",
                        Query = new FulfillmentOrdersResponse
                        {
                            FulfillmentOrders = new FulfillmentOrderConnection
                            {
                                Nodes = ids.Select(id => new FulfillmentOrder 
                                { 
                                    Id = id, 
                                    OrderName = string.Empty,
                                    AssignedLocation = new FulfillmentLocation { Name = string.Empty }
                                }).ToList(),
                                PageInfo = new PageInfo { HasNextPage = false }
                            }
                        }
                    }
                }
            };

            var mockClient = new Mock<IGraphQLClient>();
            mockClient
                .Setup(x => x.SendMutationAsync<FulfillmentOrdersReleaseHoldsResponse>(It.IsAny<GraphQLRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mutationResponse)
                .Verifiable();

            mockClient
                .Setup(x => x.SendQueryAsync<JobResponse>(It.IsAny<GraphQLRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse)
                .Verifiable();

            var sut = new Unholder(mockClient.Object);

            // Act
            await sut.Unhold(ids);

            // Assert
            mockClient.Verify();
        }
    }
}