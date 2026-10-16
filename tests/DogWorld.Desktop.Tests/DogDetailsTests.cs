using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using DogWorld.Desktop.ViewModels;
namespace DogWorld.Desktop.Tests;
public class DogDetailsTests
{
    [Fact]
    public async Task SelectionLoadsAggregatedDetails()
    {
        using var vm = new DogDetailsViewModel(new Client((id, _) => Task.FromResult<DogDetails?>(Dog(id))));
        await vm.SelectAsync(7);
        Assert.True(vm.IsOpen);
        Assert.False(vm.IsLoading);
        Assert.Equal(7, vm.Dog!.Id);
    }
    [Fact]
    public async Task UnavailableAndEmptyVaccinationsRemainDistinct()
    {
        using var vm = new DogDetailsViewModel(new Client((id, _) => Task.FromResult<DogDetails?>(id == 1 ? Dog(id) : Dog(id) with { Vaccinations = [] })));
        await vm.SelectAsync(1);
        Assert.True(vm.VaccinationsUnavailable);
        Assert.False(vm.NoVaccinations);
        await vm.SelectAsync(2);
        Assert.False(vm.VaccinationsUnavailable);
        Assert.True(vm.NoVaccinations);
    }
    [Fact]
    public async Task NotFoundHasItsOwnState()
    {
        using var vm = new DogDetailsViewModel(new Client((_, _) => Task.FromResult<DogDetails?>(null)));
        await vm.SelectAsync(1);
        Assert.True(vm.NotFound);
        Assert.False(vm.HasError);
    }
    [Fact]
    public async Task FailureRetriesSameSelection()
    {
        var calls = 0;
        using var vm = new DogDetailsViewModel(new Client((id, _) => ++calls == 1 ? throw new HttpRequestException() : Task.FromResult<DogDetails?>(Dog(id))));
        await vm.SelectAsync(8);
        Assert.True(vm.HasError);
        await vm.RetryAsync();
        Assert.False(vm.HasError);
        Assert.Equal(8, vm.Dog!.Id);
    }
    [Fact]
    public async Task NewSelectionCancelsOldRequestAndIgnoresLateResult()
    {
        var old = new TaskCompletionSource<DogDetails?>();
        CancellationToken oldToken = default;
        using var vm = new DogDetailsViewModel(new Client((id, token) =>
        {
            if(id == 1) { oldToken = token; return old.Task; }
            return Task.FromResult<DogDetails?>(Dog(id));
        }));
        var first = vm.SelectAsync(1);
        Assert.True(vm.IsLoading);
        await vm.SelectAsync(2);
        Assert.True(oldToken.IsCancellationRequested);
        old.SetResult(Dog(1));
        await first;
        Assert.Equal(2,vm.Dog!.Id);
        Assert.False(vm.IsLoading);
    }
    [Fact]
    public async Task CloseCancelsAndDoesNotReopenOnLateResponse()
    {
        var pending = new TaskCompletionSource<DogDetails?>();
        CancellationToken token = default;
        using var vm = new DogDetailsViewModel(new Client((_, ct) => { token=ct; return pending.Task; }));
        var load = vm.SelectAsync(1);
        vm.Close();
        Assert.True(token.IsCancellationRequested);
        pending.SetResult(Dog(1));
        await load;
        Assert.False(vm.IsOpen);
        Assert.Null(vm.Dog);
        Assert.False(vm.HasError);
    }
    private static DogDetails Dog(int id) => new(id,"Bella","Poodle",14,"Available");
    private sealed class Client(Func<int,CancellationToken,Task<DogDetails?>> call) : IDogDetailsClient
    {
        public Task<DogDetails?> GetAsync(int id,CancellationToken token) => call(id,token);
    }
}
