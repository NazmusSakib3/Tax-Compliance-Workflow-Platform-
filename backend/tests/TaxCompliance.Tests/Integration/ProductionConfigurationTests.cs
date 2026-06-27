using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TaxCompliance.Tests.Integration;

public class ProductionConfigurationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public ProductionConfigurationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CorsPreflight_ShouldAllowConfiguredProductionOrigin()
    {
        await using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "https://app.example.com"
                });
            });
        });

        using var client = configuredFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "https://app.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins.Should().ContainSingle("https://app.example.com");
    }
}
