using System.Net;
using System.Text;
using Bunit;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class DogBreedDetailsPageTests : TestContext
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisplaysBreedProfileOrFriendlyFallback(bool hasProfile)
    {
        var profile = hasProfile ? """{"id":"beagle","name":"Beagle","breedGroup":"Hound","temperament":"Curious and friendly","lifeExpectancy":"12–15 years","weightRange":"9–11 kg","origin":"England"}""" : "null";
        var json = """{"id":1,"name":"Bella","breed":"Beagle","ageInMonths":14,"status":"Available","breedDetails":PROFILE}""".Replace("PROFILE", profile);
        Services.AddSingleton(new HttpClient(new Handler(json)) { BaseAddress = new Uri("https://api.example.test/") });
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 1));
        page.WaitForAssertion(() => Assert.Equal("Bella", page.Find("h1").TextContent));
        var section = page.Find("section[aria-label='Breed information']");
        if (hasProfile)
        {
            foreach (var value in new[] { "Hound", "Curious and friendly", "12–15 years", "9–11 kg", "England" })
                Assert.Contains(value, section.TextContent);
        }
        else Assert.Contains("Breed information is currently unavailable", section.TextContent);
    }
    private sealed class Handler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
