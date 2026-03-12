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

    public AuthHeaderHandler(IAccessTokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var didProactiveRefresh = false;

        // Proactive refresh: if the token is about to expire, refresh before sending
        if (_tokenManager is AccessTokenManager concrete && concrete.IsAccessTokenExpiringSoon && _tokenManager.IsAuthenticated)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthHandler] Proactive refresh triggered for {request.RequestUri}");
            didProactiveRefresh = await TryRefreshTokenAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[AuthHandler] Proactive refresh result: {didProactiveRefresh}");
        }

        AttachToken(request);

        var tokenPreview = _tokenManager.AuthToken is { Length: > 20 } t ? $"{t[..10]}...{t[^10..]}" : "(empty)";
        System.Diagnostics.Debug.WriteLine($"[AuthHandler] Sending {request.Method} {request.RequestUri} with token: {tokenPreview}");

        var response = await base.SendAsync(request, cancellationToken);

        System.Diagnostics.Debug.WriteLine($"[AuthHandler] Response: {(int)response.StatusCode} for {request.RequestUri}");

        // Reactive refresh: if the server returned 401, try refreshing and retry once.
        // Allow retry even right after proactive refresh (the fresh token may have been
        // rejected due to clock skew). Skip only if we already did a reactive refresh
        // for THIS request (didProactiveRefresh tracks whether we already refreshed once).
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && _tokenManager.IsAuthenticated
            && !didProactiveRefresh)
        {
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                using var retryRequest = await CloneRequestAsync(request);
                AttachToken(retryRequest);
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized
                 && _tokenManager.IsAuthenticated
                 && didProactiveRefresh)
        {
            // Proactive refresh succeeded but server still returned 401.
            // The fresh token may not be valid yet (clock skew). Retry once
            // WITHOUT another refresh — just re-send with the same token.
            using var retryRequest = await CloneRequestAsync(request);
            AttachToken(retryRequest);

            // Small delay to let server clock catch up (ClockSkew = Zero on server)
            await Task.Delay(500, cancellationToken);

            response = await base.SendAsync(retryRequest, cancellationToken);

            // If still 401, try a full refresh cycle as last resort
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await TryRefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    using var lastRetry = await CloneRequestAsync(request);
                    AttachToken(lastRetry);
                    response = await base.SendAsync(lastRetry, cancellationToken);
                }
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
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: another call may have already refreshed
            if (_tokenManager is AccessTokenManager concrete && !concrete.IsAccessTokenExpiringSoon)
                return true;

            return await _tokenManager.RefreshTokenAsync();
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
