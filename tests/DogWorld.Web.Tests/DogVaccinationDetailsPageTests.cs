using System.Net;
using System.Text;
using Bunit;
using DogWorld.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWorld.Web.Tests;

public class DogVaccinationDetailsPageTests : TestContext
{
    [Theory]
    [InlineData("null", "Vaccination history is currently unavailable")]
    [InlineData("[]", "No vaccination records yet")]
    [InlineData("[{\"id\":1,\"dogId\":1,\"vaccineName\":\"Rabies\",\"administeredOn\":\"2026-09-15\",\"nextDueOn\":null}]", "Not recorded")]
    public void ShowsDistinctHistoryStates(string records, string expected)
    {
        var json = """{"id":1,"name":"Bella","breed":"Beagle","ageInMonths":14,"status":"Available","vaccinations":RECORDS}""".Replace("RECORDS", records);
        Services.AddSingleton(new HttpClient(new Handler(json)) { BaseAddress = new("https://api.example.test/") });
        var page = RenderComponent<Details>(p => p.Add(x => x.Id, 1));
        page.WaitForAssertion(() => Assert.Contains(expected, page.Find("section[aria-label='Vaccination history']").TextContent));
        if (records.StartsWith("[{"))
        {
            Assert.Contains("Rabies", page.Markup);
            Assert.Equal("2026-09-15", page.Find(".vaccination-history time").GetAttribute("datetime"));
        }
    }
    private sealed class Handler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
