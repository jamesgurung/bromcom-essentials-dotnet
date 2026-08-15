namespace BromcomEssentials;

/// <summary>Client for retrieving basic data from the Bromcom Partner API.</summary>
public partial class BromcomClient : IDisposable
{
  private readonly BromcomApiTransport _transport;
  private bool _disposed;

  /// <summary>Initialises a new instance of the <see cref="BromcomClient"/> class.</summary>
  /// <param name="applicationId">The Bromcom Partner API application ID.</param>
  /// <param name="applicationSecret">The Bromcom Partner API application secret.</param>
  /// <param name="httpClient">Optional HTTP client to use for requests. When omitted, the client creates and owns one.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="applicationId"/> or <paramref name="applicationSecret"/> is blank.</exception>
  public BromcomClient(string applicationId, string applicationSecret, HttpClient? httpClient = null)
  {
    if (string.IsNullOrWhiteSpace(applicationId)) throw new ArgumentException("Application ID is required.", nameof(applicationId));
    if (string.IsNullOrWhiteSpace(applicationSecret)) throw new ArgumentException("Application secret is required.", nameof(applicationSecret));
    _transport = new(applicationId, applicationSecret, httpClient);
  }

  private Task<IReadOnlyList<TModel>> GetAsync<TModel>(string path, int schoolId, string? entityFilter, string? dateFieldPrefix,
    CancellationToken cancellationToken) =>
    _transport.GetAsync<TModel>(path, schoolId, entityFilter ?? BuildDefaultEntityFilter(dateFieldPrefix), cancellationToken);

  private void ValidateRequest(int schoolId)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(schoolId);
  }

  /// <summary>Releases the owned HTTP client, if this instance created one.</summary>
  public void Dispose()
  {
    if (_disposed) return;
    _transport.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }
}
