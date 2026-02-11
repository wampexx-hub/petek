using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Petek.Desktop.Services;

public interface IApiClient
{
    void SetAuthToken(string token);
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data);
    Task<T?> PutAsync<T>(string endpoint, object? data);
    Task<bool> DeleteAsync(string endpoint);
    Task<HttpResponseMessage> UploadFileAsync(string endpoint, Stream fileStream, string fileName);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    // Auth token tum ApiClient instance'lari arasinda paylasilir.
    // AddHttpClient<> her resolve'da yeni instance olusturur,
    // ama SetAuthToken sadece AuthenticationService'deki instance'a cagirilir.
    private static string? _sharedAuthToken;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public void SetAuthToken(string token)
    {
        _sharedAuthToken = string.IsNullOrEmpty(token) ? null : token;

        if (string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization = null;
        else
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    private void EnsureAuthHeader()
    {
        if (!string.IsNullOrEmpty(_sharedAuthToken) &&
            _httpClient.DefaultRequestHeaders.Authorization == null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _sharedAuthToken);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        EnsureAuthHeader();
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data)
    {
        EnsureAuthHeader();
        HttpContent? content = null;
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? data)
    {
        EnsureAuthHeader();
        HttpContent? content = null;
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        EnsureAuthHeader();
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    public async Task<HttpResponseMessage> UploadFileAsync(string endpoint, Stream fileStream, string fileName)
    {
        EnsureAuthHeader();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        content.Add(streamContent, "file", fileName);

        return await _httpClient.PostAsync(endpoint, content);
    }
}
