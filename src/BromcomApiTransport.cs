using System.Text.Json;

namespace BromcomEssentials;

internal sealed class BromcomApiTransport : IDisposable
{
  private const string ApiBaseUrl = "https://api.bromcomcloud.com";
  private const string PhotoUrl = "https://Cloudmis.Bromcom.com/Nucleus/Framework/Components/Controls/ImageDisplay.ashx";

  private readonly HttpClient _httpClient;
  private readonly bool _ownsHttpClient;
  private readonly string _applicationId;
  private readonly string _applicationSecret;
  private bool _disposed;

  public BromcomApiTransport(string applicationId, string applicationSecret, HttpClient? httpClient)
  {
    _applicationId = applicationId;
    _applicationSecret = applicationSecret;
    _ownsHttpClient = httpClient is null;
    _httpClient = httpClient ?? new();
  }

  public async Task<IReadOnlyList<TModel>> GetAsync<TModel>(string path, int schoolId, string entityFilter, CancellationToken cancellationToken)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    ValidatePath(path);

    var results = new List<TModel>();
    var page = 0;
    List<TModel> data;

    do
    {
      var url = FormattableString.Invariant($"{ApiBaseUrl}{path}?schoolId={schoolId}&entityFilter={Uri.EscapeDataString(entityFilter)}{{page:{page}}}");
      using var request = CreateRequest(HttpMethod.Get, url);
      using var response = await SendAsync(request, path, cancellationToken).ConfigureAwait(false);

      ApiResponse<TModel>? payload;
      try
      {
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        payload = await JsonSerializer.DeserializeAsync<ApiResponse<TModel>>(content, BromcomJsonContext.Default.Options, cancellationToken).ConfigureAwait(false);
      }
      catch (JsonException ex)
      {
        throw new InvalidOperationException($"Invalid JSON returned from '{path}'.", ex);
      }

      if (payload is null) throw new InvalidOperationException($"Empty response payload from '{path}'.");
      if (!payload.Success) throw new InvalidOperationException($"API returned success=false for '{path}'.");
      if (payload.Data is null) throw new InvalidOperationException($"Response data was null for '{path}'.");

      data = payload.Data;
      results.AddRange(data);
      page++;
    } while (data.Count == 100000);

    return results;
  }

  public async Task PostAsync<TModel>(string path, TModel payload, CancellationToken cancellationToken)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    ValidatePath(path);

    using var request = CreateRequest(HttpMethod.Post, $"{ApiBaseUrl}{path}");
    request.Content = JsonContent.Create(payload, options: BromcomJsonContext.Default.Options);
    using var response = await SendAsync(request, path, cancellationToken).ConfigureAwait(false);
  }

  public async Task<PersonPhoto> GetPhotoAsync(string photoId, CancellationToken cancellationToken)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    using var request = new HttpRequestMessage(HttpMethod.Get, $"{PhotoUrl}?EncID={Uri.EscapeDataString(photoId)}");
    using var response = await SendAsync(request, "/Nucleus/Framework/Components/Controls/ImageDisplay.ashx", cancellationToken).ConfigureAwait(false);
    return new PersonPhoto
    {
      Content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false),
      ContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty
    };
  }

  private HttpRequestMessage CreateRequest(HttpMethod method, string url)
  {
    var request = new HttpRequestMessage(method, url);
    request.Headers.Add("ApplicationId", _applicationId);
    request.Headers.Add("ApplicationSecret", _applicationSecret);
    request.Headers.Add("Accept", "application/json");
    return request;
  }

  private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string path, CancellationToken cancellationToken)
  {
    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    if (response.IsSuccessStatusCode) return response;

    var statusCode = response.StatusCode;
    response.Dispose();
    throw new HttpRequestException($"Request to '{path}' failed with status {(int)statusCode} ({statusCode}).", null, statusCode);
  }

  private static void ValidatePath(string path)
  {
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/', StringComparison.Ordinal))
      throw new ArgumentException("Path must start with '/'.", nameof(path));
  }

  public void Dispose()
  {
    if (_disposed) return;
    if (_ownsHttpClient) _httpClient.Dispose();
    _disposed = true;
  }
}
