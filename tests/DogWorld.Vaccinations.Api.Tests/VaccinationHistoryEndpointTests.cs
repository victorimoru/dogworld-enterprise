using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VaccinationProgram = DogWorld.Vaccinations.Api.Program;

namespace DogWorld.Vaccinations.Api.Tests;

public class VaccinationHistoryEndpointTests
{
    // First contract increment: the test host starts with no vaccination records.
    // SQL persistence and seeded-record scenarios follow in a separate increment.
    [Fact]
    public async Task PositiveDogIdWithNoRecords_Returns200AndEmptyJsonArray()
    {
        using var factory = new WebApplicationFactory<VaccinationProgram>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/dogs/42/vaccinations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Equal(0, body.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveDogId_Returns400ProblemDetails(int dogId)
    {
        using var factory = new WebApplicationFactory<VaccinationProgram>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/api/dogs/{dogId}/vaccinations");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
    }
}
