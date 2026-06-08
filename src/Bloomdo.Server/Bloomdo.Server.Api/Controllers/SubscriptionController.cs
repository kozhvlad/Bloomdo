using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet(ApiRoutes.Subscription.Status)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var status = await subscriptionService.GetStatusAsync(accountId.Value, ct);
        return Ok(status);
    }

    [HttpPost(ApiRoutes.Subscription.Checkout)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst("email")?.Value
                    ?? string.Empty;

        var serverBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = await subscriptionService.CreateCheckoutSessionAsync(accountId.Value, email, request.Plan, serverBaseUrl, ct);
        return Ok(response);
    }

    [HttpPost(ApiRoutes.Subscription.Cancel)]
    public async Task<IActionResult> CancelSubscription(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await subscriptionService.CancelSubscriptionAsync(accountId.Value, ct);
        return Ok();
    }

    [HttpPost(ApiRoutes.Subscription.Webhook)]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        Console.WriteLine($"[Stripe Webhook] Received webhook. Signature present: {!string.IsNullOrEmpty(signature)}, Body length: {json.Length}");

        try
        {
            await subscriptionService.HandleWebhookAsync(json, signature, ct);
            Console.WriteLine("[Stripe Webhook] Webhook processed successfully.");
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Stripe Webhook] ERROR processing webhook: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet(ApiRoutes.Subscription.CheckoutSuccess)]
    [AllowAnonymous]
    public async Task<IActionResult> CheckoutSuccess([FromQuery(Name = "session_id")] string? sessionId, CancellationToken ct)
    {
        // Fallback path for local dev where Stripe webhooks can't reach localhost.
        // Idempotent — webhook may still fire later and will no-op.
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            try
            {
                await subscriptionService.ActivateFromCheckoutSessionAsync(sessionId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stripe Success] Activation failed for session {sessionId}: {ex.Message}");
            }
        }

        var html = """
            <!DOCTYPE html>
            <html><head>
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Payment Successful</title>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #1a1a2e; color: #fff; text-align: center; }
                    .container { padding: 2rem; }
                    .icon { font-size: 4rem; margin-bottom: 1rem; }
                    h1 { color: #4ade80; margin-bottom: 0.5rem; }
                    p { color: #a0a0b0; line-height: 1.6; }
                </style>
            </head><body>
                <div class="container">
                    <div class="icon">✅</div>
                    <h1>Payment Successful!</h1>
                    <p>Your Bloomdo Plus subscription is now active.<br>You can close this tab and return to the app.</p>
                </div>
            </body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet(ApiRoutes.Subscription.CheckoutCancel)]
    [AllowAnonymous]
    public IActionResult CheckoutCancel()
    {
        var html = """
            <!DOCTYPE html>
            <html><head>
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Checkout Cancelled</title>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #1a1a2e; color: #fff; text-align: center; }
                    .container { padding: 2rem; }
                    .icon { font-size: 4rem; margin-bottom: 1rem; }
                    h1 { color: #f59e0b; margin-bottom: 0.5rem; }
                    p { color: #a0a0b0; line-height: 1.6; }
                </style>
            </head><body>
                <div class="container">
                    <div class="icon">↩️</div>
                    <h1>Checkout Cancelled</h1>
                    <p>No charges were made.<br>You can close this tab and return to the app.</p>
                </div>
            </body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }
}
