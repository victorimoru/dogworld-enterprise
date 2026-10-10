using System.Net;
using System.Text.Json;
using DogWorld.Api.Resilience;
using DogWorld.Contracts;
using Polly;

namespace DogWorld.Api.Clients;

public sealed class BreedsApiClient(HttpClient http, ResiliencePipeline<HttpResponseMessage>? pipeline = null) : IBreedsApiClient
{
    private readonly ResiliencePipeline<HttpResponseMessage> resilience = pipeline ?? BreedsApiResiliencePolicy.Create();

    public async Task<BreedProfileResponse?> GetBreedByIdAsync(string breedId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breedId);
        var path = $"api/breeds/{Uri.EscapeDataString(breedId)}";

        // GetAsync buffers the body inside the total timeout. Each retry creates a new request.
        // HttpClient's diagnostics handler propagates the current W3C trace context.

        using var response = await resilience.ExecuteAsync(
            async token => await http.GetAsync(path, token), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BreedProfileResponse>(cancellationToken)
            ?? throw new JsonException("Expected a breed profile.");
    }
}
