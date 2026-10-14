using System.Text.Json;
using DogWorld.Api.Resilience;
using DogWorld.Contracts;
using Polly;

namespace DogWorld.Api.Clients;

public sealed class VaccinationsApiClient(HttpClient http, ResiliencePipeline<HttpResponseMessage>? pipeline = null) : IVaccinationsApiClient
{
    private readonly ResiliencePipeline<HttpResponseMessage> resilience = pipeline ?? VaccinationsApiResiliencePolicy.Create();

    public async Task<IReadOnlyList<VaccinationRecordResponse>> GetByDogIdAsync(
        int dogId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dogId);
        // Buffer the body inside the total timeout; HttpClient propagates W3C context.
        using var response = await resilience.ExecuteAsync(
            async token => await http.GetAsync($"api/dogs/{dogId}/vaccinations", token),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var records = await response.Content.ReadFromJsonAsync<VaccinationRecordResponse[]>(cancellationToken)
            ?? throw new JsonException("Expected a vaccination history array.");
        if (records.Any(record => record is null || record.DogId != dogId))
            throw new JsonException("Vaccination history contains a record for a different dog.");
        return records;
    }
}
