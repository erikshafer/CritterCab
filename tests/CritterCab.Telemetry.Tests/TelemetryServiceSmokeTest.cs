using Alba;
using Shouldly;
using Xunit;

namespace CritterCab.Telemetry.Tests;

public class TelemetryServiceSmokeTest
{
    [Fact]
    public async Task health_endpoint_returns_healthy()
    {
        await using var host = await AlbaHost.For<Program>();

        var result = await host.Scenario(s =>
        {
            s.Get.Url("/health");
            s.StatusCodeShouldBeOk();
        });

        result.ReadAsText().ShouldContain("Healthy");
    }
}
