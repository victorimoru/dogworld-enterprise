using System.Net;
using System.Text;
using DogWorld.Api.Clients;

namespace DogWorld.Api.Tests.Clients;

public class VaccinationsApiClientTests
{
    [Theory]
    [InlineData("[]", 0)]
    [InlineData("[{\"id\":1,\"dogId\":7,\"vaccineName\":\"Rabies\",\"administeredOn\":\"2026-09-15\",\"nextDueOn\":null}]", 1)]
    public async Task RequestsDogHistoryAndReadsDates(string json, int count)
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("/api/dogs/7/vaccinations", request.RequestUri!.AbsolutePath);
            return Response(HttpStatusCode.OK, json);
        })) { BaseAddress = new("https://vaccines.example.test/") };
        var result = await new VaccinationsApiClient(http).GetByDogIdAsync(7);
        Assert.Equal(count, result.Count);
        if (count > 0) Assert.Equal(new DateOnly(2026, 9, 15), result[0].AdministeredOn);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("not-json")]
    [InlineData("[{\"id\":1,\"dogId\":8,\"vaccineName\":\"Rabies\",\"administeredOn\":\"2026-09-15\",\"nextDueOn\":null}]")]
    public async Task InvalidPayloadIsNotEmptyHistory(string json)
    {
        using var http = new HttpClient(new Handler(_ => Response(HttpStatusCode.OK, json))) { BaseAddress = new("https://vaccines.example.test/") };
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => new VaccinationsApiClient(http).GetByDogIdAsync(7));
    }

    [Fact]
    public async Task RetriesTransientFailuresButDoesNotTreat404AsEmpty()
    {
        var attempts = 0;
        using var http = new HttpClient(new Handler(_ => ++attempts < 3 ? Response(HttpStatusCode.ServiceUnavailable, "") : Response(HttpStatusCode.OK, "[]"))) { BaseAddress = new("https://vaccines.example.test/") };
        Assert.Empty(await new VaccinationsApiClient(http).GetByDogIdAsync(7));
        Assert.Equal(3, attempts);
        using var missing = new HttpClient(new Handler(_ => Response(HttpStatusCode.NotFound, ""))) { BaseAddress = http.BaseAddress };
        await Assert.ThrowsAsync<HttpRequestException>(() => new VaccinationsApiClient(missing).GetByDogIdAsync(7));
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(response(request));
    }
}
