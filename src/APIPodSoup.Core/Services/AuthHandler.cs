using System.Net.Http.Headers;
using APIPodSoup.Core.Models;
using Microsoft.Extensions.Options;

namespace APIPodSoup.Core.Services;

/// <summary>
/// Delegating handler that injects the current API key into every outgoing request.
/// Uses IOptionsMonitor so it reflects changes saved in the Settings page in real time.
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<AppSettings> _settings;

    public AuthHandler(IOptionsMonitor<AppSettings> settings)
    {
        _settings = settings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _settings.CurrentValue.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
