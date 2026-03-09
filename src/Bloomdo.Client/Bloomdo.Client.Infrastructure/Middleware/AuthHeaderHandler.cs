using System.Net;
using System.Net.Http.Headers;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Infrastructure.Services;

namespace Bloomdo.Client.Infrastructure.Middleware;

/// <summary>
/// Attaches the Bearer token to outgoing requests.
/// Handles proactive token refresh before expiry and automatic retry on 401.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAccessTokenManager _tokenManager;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static DateTime _lastRefreshTime = DateTime.MinValue;

    public AuthHeaderHandler(IAccessTokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Proactive refresh: if the token is about to expire, refresh before sending
        if (_tokenManager is AccessTokenManager concrete && concrete.IsAccessTokenExpiringSoon && _tokenManager.IsAuthenticated)
        {
            await TryRefreshTokenAsync(cancellationToken);
        }

        AttachToken(request);

        var response = await base.SendAsync(request, cancellationToken);

        // Reactive refresh: if the server returned 401, try refreshing and retry once
        // Skip if we just refreshed less than 5 seconds ago (avoid infinite loop)
        if (response.StatusCode == HttpStatusCode.Unauthorized && _tokenManager.IsAuthenticated
            && (DateTime.UtcNow - _lastRefreshTime).TotalSeconds > 5)
        {
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                // Clone the request (the original has already been sent and cannot be reused)
                using var retryRequest = await CloneRequestAsync(request);
                AttachToken(retryRequest);
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private void AttachToken(HttpRequestMessage request)
    {
        if (_tokenManager.IsAuthenticated && !string.IsNullOrEmpty(_tokenManager.AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenManager.AuthToken);
        }
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Ensure only one refresh is in-flight at a time
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: another call may have already refreshed
            if (_tokenManager is AccessTokenManager concrete && !concrete.IsAccessTokenExpiringSoon)
                return true;

            var result = await _tokenManager.RefreshTokenAsync();
            if (result)
                _lastRefreshTime = DateTime.UtcNow;
            return result;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content != null)
        {
            var content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var prop in original.Options)
        {
            clone.Options.TryAdd(prop.Key, prop.Value);
        }

        clone.Version = original.Version;

        return clone;
    }
}
