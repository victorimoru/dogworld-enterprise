using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using System.Text.Json;
namespace DogWorld.Desktop.ViewModels;

public sealed class DogDetailsViewModel : ObservableObject, IDisposable
{
    private readonly IDogDetailsClient client;
    private CancellationTokenSource? request;
    private int selectedId;
    private bool disposed;
    public DogDetails? Dog { get; private set; }
    public bool IsOpen { get; private set; }
    public bool IsLoading { get; private set; }
    public bool HasError { get; private set; }
    public bool NotFound { get; private set; }
    public bool HasDog => Dog is not null;
    public bool HasBreed => Dog?.BreedDetails is not null;
    public bool BreedUnavailable => HasDog && !HasBreed;
    public bool VaccinationsUnavailable => HasDog && Dog!.Vaccinations is null;
    public bool NoVaccinations => Dog?.Vaccinations?.Count == 0;
    public IReadOnlyList<VaccinationRow> Vaccinations => Dog?.Vaccinations?.Select(record => new VaccinationRow(
        record.VaccineName, record.AdministeredOn.ToString("dd MMM yyyy"),
        record.NextDueOn?.ToString("dd MMM yyyy") ?? "Not recorded")).ToArray() ?? [];
    public IAsyncRelayCommand RetryCommand { get; }
    public IRelayCommand CloseCommand { get; }

    public DogDetailsViewModel(IDogDetailsClient client)
    {
        this.client = client;
        RetryCommand = new AsyncRelayCommand(RetryAsync, () => IsOpen && !IsLoading && !disposed);
        CloseCommand = new RelayCommand(Close);
    }

    public async Task SelectAsync(int id)
    {
        if(disposed) return;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        request?.Cancel();
        var current = new CancellationTokenSource();
        request = current;
        selectedId = id;
        IsOpen = IsLoading = true;
        HasError = NotFound = false;
        Dog = null;
        NotifyState();
        try
        {
            var dog = await client.GetAsync(id,current.Token);
            current.Token.ThrowIfCancellationRequested();
            if(!ReferenceEquals(request,current)) return;
            Dog = dog;
            NotFound = dog is null;
        }
        catch(OperationCanceledException) when(current.IsCancellationRequested) { }
        catch(Exception ex) when(ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            if(ReferenceEquals(request,current)) HasError = true;
        }
        finally
        {
            if(ReferenceEquals(request,current))
            {
                request = null;
                IsLoading = false;
                NotifyState();
            }
            current.Dispose();
        }
    }

    public Task RetryAsync() => IsOpen && selectedId > 0 ? SelectAsync(selectedId) : Task.CompletedTask;
    public void Close()
    {
        request?.Cancel();
        request = null;
        Dog = null;
        IsOpen = IsLoading = HasError = NotFound = false;
        NotifyState();
    }
    private void NotifyState()
    {
        OnPropertyChanged(string.Empty);
        RetryCommand.NotifyCanExecuteChanged();
    }
    public void Dispose()
    {
        if(disposed) return;
        disposed = true;
        Close();
    }
}
public sealed record VaccinationRow(string Name,string Administered,string NextDue);
