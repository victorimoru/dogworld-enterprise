using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DogWorld.IntegrationTests;

public class AvailableDogsEndpointTests
{
    [Fact]
    public async Task GetAvailableDogs_ReturnsOk()
    {
        // The generated API Program is internal. Resolve it here so this Red
        // step needs no production changes just to expose an entry point.
        var entryPointType = Assembly.Load("DogWorld.Api").EntryPoint!.DeclaringType!;

        var factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPointType);
        using var factory = (IDisposable)Activator.CreateInstance(factoryType)!;
        var createClient = factoryType.GetMethod("CreateClient", [typeof(WebApplicationFactoryClientOptions)])!;
        using var client = (HttpClient)createClient.Invoke(factory,
            [new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }])!;

        using var response = await client.GetAsync("/api/dogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
