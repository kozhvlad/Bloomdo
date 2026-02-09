using System.Net.Http.Headers;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Infrastructure.Middleware;

public class AuthHeaderHandler(IAccessTokenManager authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.AuthToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", authService.AuthToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}