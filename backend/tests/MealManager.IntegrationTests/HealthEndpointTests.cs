using System.Net;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>Health endpoints via the sqlite factory (readiness has no Postgres dependency there).</summary>
public sealed class HealthEndpointTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnHealthy(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
