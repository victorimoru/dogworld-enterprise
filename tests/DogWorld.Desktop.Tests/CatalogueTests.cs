using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.ViewModels;
namespace DogWorld.Desktop.Tests;
public class CatalogueTests
{
    [Fact]
    public async Task LoadsTwelvePerPageAndUsesServerTotal()
    {
        var client = new Stub();
        var vm = new CatalogueViewModel(client);
        await vm.LoadAsync(2, TestContext.Current.CancellationToken);
        Assert.Equal((2, 12), client.Request);
        Assert.Equal(2, vm.Page);
        Assert.Equal(5, vm.TotalPages);
        Assert.Single(vm.Dogs);
        Assert.False(vm.HasError);
    }
    [Fact]
    public async Task LoadingIsVisibleAndDuplicateLoadIsIgnored()
    {
        var pending = new TaskCompletionSource<DogPage>();
        var client = new Stub { Handler = _ => pending.Task };
        var vm = new CatalogueViewModel(client);
        var load = vm.LoadAsync(token: TestContext.Current.CancellationToken);
        Assert.True(vm.IsLoading);
        await vm.LoadAsync(2, TestContext.Current.CancellationToken);
        Assert.Equal(1, client.Calls);
        pending.SetResult(new DogPage([], 0));
        await load;
        Assert.False(vm.IsLoading);
        Assert.True(vm.IsEmpty);
    }
    [Fact]
    public async Task FailureCanBeRetried()
    {
        var client = new Stub { Handler = _ => throw new HttpRequestException() };
        var vm = new CatalogueViewModel(client);
        await vm.LoadAsync(token: TestContext.Current.CancellationToken);
        Assert.True(vm.HasError);
        Assert.False(vm.IsEmpty);
        client.Handler = _ => Task.FromResult(new DogPage([new(1,"Bella","Poodle",14)],1));
        await vm.LoadAsync(token: TestContext.Current.CancellationToken);
        Assert.False(vm.HasError);
        Assert.Single(vm.Dogs);
    }
    [Fact]
    public async Task CancellationIsNotShownAsAnApiError()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var vm = new CatalogueViewModel(new Stub { Handler = token => Task.FromCanceled<DogPage>(token) });
        await vm.LoadAsync(token: cts.Token);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
    }
    [Fact]
    public async Task RemovedLastPageReturnsToLastAvailablePage()
    {
        var client = new Stub();
        client.Handler = _ => Task.FromResult(client.Request.Item1 == 5
            ? new DogPage([], 13) : new DogPage([new(13,"Max","Beagle",12)],13));
        var vm = new CatalogueViewModel(client);
        await vm.LoadAsync(5, TestContext.Current.CancellationToken);
        Assert.Equal(2,vm.Page);
        Assert.Equal(2,client.Calls);
        Assert.Single(vm.Dogs);
    }
    private sealed class Stub : IDogCatalogueClient
    {
        public (int,int) Request;
        public int Calls;
        public Func<CancellationToken,Task<DogPage>> Handler = _ => Task.FromResult(new DogPage([new(1,"Bella","Poodle",14)],50));
        public Task<DogPage> GetPageAsync(int page,int pageSize,CancellationToken cancellationToken)
        { Calls++; Request=(page,pageSize); return Handler(cancellationToken); }
    }
}
