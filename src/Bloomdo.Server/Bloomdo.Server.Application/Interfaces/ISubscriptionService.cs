using Bloomdo.Shared.DTOs.Subscription;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionStatusResponse> GetStatusAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> IsPremiumAsync(Guid accountId, CancellationToken ct = default);
    Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(Guid accountId, string email, SubscriptionPlan plan, string serverBaseUrl, CancellationToken ct = default);
    Task CancelSubscriptionAsync(Guid accountId, CancellationToken ct = default);
    Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken ct = default);
}
