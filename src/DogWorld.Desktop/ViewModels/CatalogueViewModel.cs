using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWorld.Contracts;
using DogWorld.Desktop.Services;
using System.Text.Json;
namespace DogWorld.Desktop.ViewModels;

public sealed class CatalogueViewModel : ObservableObject, IDisposable
{
    private readonly IDogCatalogueClient client;
    private readonly CancellationTokenSource lifetime = new();
    private bool disposed;
    private bool loaded;
    private int requestedPage = 1;
    public const int PageSize = 8;
    public IReadOnlyList<AvailableDog> Dogs { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public bool HasError { get; private set; }
    public int Page { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool IsEmpty => loaded && !IsLoading && !HasError && Dogs.Count == 0;
    public bool ShowDogs => !IsLoading && !HasError && Dogs.Count > 0;
    public string CatalogueSummary => loaded ? $"{TotalCount} companions available" : "Your next chapter starts here";
    public string PageLabel => $"{Page} / {TotalPages}";
    public string PageSummary => HasError ? "Catalogue unavailable" : IsLoading ? "Loading companions…" : TotalCount == 0
        ? "No dogs to display" : $"Showing {(Page - 1) * PageSize + 1}–{(Page - 1) * PageSize + Dogs.Count} of {TotalCount}";
    public string Status => IsLoading ? "Connecting to DogWorld…" : HasError ? "Unable to reach the catalogue • try again"
        : loaded ? "●  Catalogue up to date" : "Ready";
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }

    public CatalogueViewModel(IDogCatalogueClient client)
    {
        this.client = client;
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync(requestedPage), () => !IsLoading && !disposed);
        PreviousCommand = new AsyncRelayCommand(() => LoadAsync(Page - 1), () => !IsLoading && !HasError && Page > 1 && !disposed);
        NextCommand = new AsyncRelayCommand(() => LoadAsync(Page + 1), () => !IsLoading && !HasError && Page < TotalPages && !disposed);
    }

    public async Task LoadAsync(int page = 1, CancellationToken token = default)
    {
        if (disposed || IsLoading) return;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        requestedPage = page;
        IsLoading = true;
        HasError = false;
        NotifyState();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        try
        {
            // The collection may shrink between requests. Each correction moves backwards.
            while (true)
            {
                var result = await client.GetPageAsync(page, PageSize, linked.Token);
                linked.Token.ThrowIfCancellationRequested();
                var last = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
                if (page > last) { page = last; continue; }
                Dogs = result.Dogs;
                TotalCount = result.TotalCount;
                Page = requestedPage = page;
                loaded = true;
                break;
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            NotifyState();
        }
    }

    private void NotifyState()
    {
        OnPropertyChanged(string.Empty);
        RefreshCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
        NotifyState();
    }
}
