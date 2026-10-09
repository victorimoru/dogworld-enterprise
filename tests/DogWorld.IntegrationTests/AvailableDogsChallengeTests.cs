using System.Data.Common;
using System.Net;
using System.Text.Json;
using DogWorld.Api.Data;
using DogWorld.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DogWorld.IntegrationTests;

[Trait("Category", "SqlServer")]
public class AvailableDogsChallengeTests : IAsyncLifetime
{
    private readonly string _databaseName = $"DogWorldChallengeTests_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private bool _created;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("DOGWORLD_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set DOGWORLD_TEST_SQLSERVER to a local development SQL Server connection.");

        var connection = new SqlConnectionStringBuilder(configured);
        if (!string.IsNullOrEmpty(connection.AttachDBFilename))
            throw new InvalidOperationException("File-attached databases are not supported.");
        connection.InitialCatalog = _databaseName;
        connection.Pooling = false;
        _connectionString = connection.ConnectionString;
        await using var context = CreateContext();
        _created = await context.Database.EnsureCreatedAsync();
        Assert.True(_created);
    }

    public async Task DisposeAsync()
    {
        if (!_created) return;
        await using var context = CreateContext();
        Assert.Equal(_databaseName, context.Database.GetDbConnection().Database);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task GetAvailableDogs_WhenSqlFails_ReturnsGeneric500WithoutInternalDetails()
    {
        var fault = new SqlFaultInterceptor();
        using var host = CreateHost(fault);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/api/dogs");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("detail", out var detail)
            && !string.IsNullOrWhiteSpace(detail.GetString()));
        Assert.DoesNotContain(SqlFaultInterceptor.PrivateMessage, body);
        Assert.DoesNotContain(_databaseName, body);
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailableDogs_ReturnsJsonFieldsInNameThenIdOrder_ExcludingAdoptedDogs()
    {
        var zoe = Dog("Zoe", "Beagle", 30, DogStatus.Available);
        var bellaOne = Dog("Bella", "Poodle", 12, DogStatus.Available);
        var bellaTwo = Dog("Bella", "Labrador", 18, DogStatus.Available);
        await using (var context = CreateContext())
        {
            context.Dogs.AddRange(zoe, bellaOne, bellaTwo, Dog("Aaron", "Boxer", 20, DogStatus.Adopted));
            await context.SaveChangesAsync();
        }
        var expected = new[] { bellaOne, bellaTwo }.OrderBy(dog => dog.Id).Append(zoe).ToArray();
        using var host = CreateHost();
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/api/dogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = json.RootElement.EnumerateArray().ToArray();
        Assert.Equal(expected.Length, rows.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Id, rows[index].GetProperty("id").GetInt32());
            Assert.Equal(expected[index].Name, rows[index].GetProperty("name").GetString());
            Assert.Equal(expected[index].Breed, rows[index].GetProperty("breed").GetString());
            Assert.Equal(expected[index].AgeInMonths, rows[index].GetProperty("ageInMonths").GetInt32());
        }
    }

    [Fact]
    public async Task GetAvailableDogs_WhenDatabaseIsEmpty_Returns200WithEmptyJsonArray()
    {
        using var host = CreateHost();
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/api/dogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetAvailableDogs_WhenRequestIsCancelled_CancelsSqlCommand()
    {
        var probe = new CancellationProbe();
        using var host = CreateHost(probe);
        using var client = host.CreateClient();
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync("/api/dogs", cancellation.Token);

        try
        {
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => { using var response = await request.WaitAsync(TimeSpan.FromSeconds(10)); });
            // A cancelled HttpClient task alone does not prove SQL cancellation.
            await probe.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(probe.CommandToken.IsCancellationRequested);
        }
        finally
        {
            cancellation.Cancel();
        }
    }

    private DogWorldDbContext CreateContext() => new(new DbContextOptionsBuilder<DogWorldDbContext>()
        .UseSqlServer(_connectionString).Options);

    private IChallengeHost CreateHost(DbCommandInterceptor? interceptor = null)
    {
        var entryPoint = typeof(DogWorldDbContext).Assembly.EntryPoint!.DeclaringType!;
        var type = typeof(ChallengeHost<>).MakeGenericType(entryPoint);
        return (IChallengeHost)Activator.CreateInstance(type, _connectionString, interceptor)!;
    }

    private static Dog Dog(string name, string breed, int age, DogStatus status) => new()
    {
        Name = name, Breed = breed, AgeInMonths = age, Status = status
    };

    public interface IChallengeHost : IDisposable
    {
        HttpClient CreateClient();
    }

    public sealed class ChallengeHost<TEntryPoint>(string connectionString, DbCommandInterceptor? interceptor)
        : WebApplicationFactory<TEntryPoint>, IChallengeHost where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:Initialize"] = "false",
                    ["ApplicationInsights:Enabled"] = "false"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DogWorldDbContext>();
                services.RemoveAll<DbContextOptions<DogWorldDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<DogWorldDbContext>>();
                services.AddDbContext<DogWorldDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                    if (interceptor is not null) options.AddInterceptors(interceptor);
                });
            });
        }
    }

    private sealed class SqlFaultInterceptor : DbCommandInterceptor
    {
        public const string PrivateMessage = "PRIVATE_DATABASE_DIAGNOSTIC_51001";
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            command.CommandText = $"THROW 51001, '{PrivateMessage}', 1;";
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CancellationProbe : DbCommandInterceptor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken CommandToken { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            command.CommandText = "WAITFOR DELAY '00:00:20'; " + command.CommandText;
            CommandToken = cancellationToken;
            Started.TrySetResult();
            return ValueTask.FromResult(result);
        }

        public override Task CommandCanceledAsync(DbCommand command, CommandEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Cancelled.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
