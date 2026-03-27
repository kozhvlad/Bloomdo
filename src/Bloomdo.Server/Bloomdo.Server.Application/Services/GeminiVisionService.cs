using System.Text.Json;
using System.Text.Json.Serialization;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Shared.DTOs.Activities;
using Google.GenAI;
using Google.GenAI.Types;

namespace Bloomdo.Server.Application.Services;

public class GeminiVisionService(IGeminiSettings geminiSettings) : IVisionService
{
    private const string Model = "gemini-2.5-flash";
    private const float ConfidenceThreshold = 0.65f;

    private static readonly Dictionary<VerificationTemplate, string> TemplateCriteria = new()
    {
        [VerificationTemplate.Workout]    = "a person exercising, working out at a gym, doing sports, or engaged in any physical activity",
        [VerificationTemplate.Meal]       = "food, a prepared meal, or a plate of food",
        [VerificationTemplate.Workspace]  = "a desk, computer setup, or organized workspace",
        [VerificationTemplate.Reading]    = "a book, magazine, e-reader, or other reading material",
        [VerificationTemplate.Outdoors]   = "an outdoor scene such as nature, a park, street, or open sky",
        [VerificationTemplate.Sleep]      = "a bed, pillow, or a sleeping environment",
        [VerificationTemplate.Meditation] = "a calm, quiet environment suitable for meditation, or a person meditating",
        [VerificationTemplate.Cleaning]   = "a cleaning activity, cleaning supplies, or a tidy and organized space",
        [VerificationTemplate.Water]      = "a glass, cup, or bottle of water",
        [VerificationTemplate.ColdShower] = "a shower, bathtub, or bathroom",
        [VerificationTemplate.Study]      = "study materials such as notebooks, textbooks, notes, or a person studying",
        [VerificationTemplate.Custom]     = string.Empty
    };

    public async Task<VisionResult> VerifyAsync(byte[] imageBytes, VerificationTemplate template,
        string? customCriteria, CancellationToken ct = default)
    {
        var criteria = template == VerificationTemplate.Custom
            ? customCriteria ?? string.Empty
            : TemplateCriteria[template];

        var prompt = $$"""
            You are a photo verification assistant. Analyze the provided image and determine if it shows: {{criteria}}.

            Respond ONLY with a valid JSON object — no markdown, no text outside JSON:
            {"verified": true, "confidence": 0.9, "explanation": "The image shows..."}

            Rules:
            - "verified": true if the image clearly shows the required content, false otherwise
            - "confidence": your certainty from 0.0 to 1.0
            - "explanation": 1-2 sentences describing what you see and your decision
            """;

        var contents = new List<Content>
        {
            new()
            {
                Role = "user",
                Parts =
                [
                    new Part { Text = prompt },
                    new Part { InlineData = new Blob { MimeType = "image/jpeg", Data = imageBytes } }
                ]
            }
        };

        var config = new GenerateContentConfig { Temperature = 0.1f, MaxOutputTokens = 256 };
        var apiKeys = geminiSettings.ApiKeys;

        for (var i = 0; i < apiKeys.Count; i++)
        {
            var client = new Client(apiKey: apiKeys[i]);

            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: Model,
                    contents: contents,
                    config: config
                );

                var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (text is null) continue;

                return ParseResult(text);
            }
            catch (ClientError) when (i < apiKeys.Count - 1)
            {
                // Key rate-limited or inactive — try next
            }
        }

        throw new InvalidOperationException("Gemini Vision API is unavailable: all API keys exhausted.");
    }

    private static VisionResult ParseResult(string text)
    {
        // Strip markdown code blocks if present
        var json = text.Trim();
        if (json.StartsWith("```"))
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];
        }

        try
        {
            var dto = JsonSerializer.Deserialize<GeminiVisionResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
                return new VisionResult(VerificationStatus.Rejected, "Could not parse model response.", 0f);

            var status = dto.Verified && dto.Confidence >= ConfidenceThreshold
                ? VerificationStatus.Verified
                : dto.Verified && dto.Confidence < ConfidenceThreshold
                    ? VerificationStatus.LowConfidence
                    : VerificationStatus.Rejected;

            return new VisionResult(status, dto.Explanation, dto.Confidence);
        }
        catch (JsonException)
        {
            return new VisionResult(VerificationStatus.Rejected, "Could not parse model response.", 0f);
        }
    }

    private sealed class GeminiVisionResponse
    {
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;
    }
}
