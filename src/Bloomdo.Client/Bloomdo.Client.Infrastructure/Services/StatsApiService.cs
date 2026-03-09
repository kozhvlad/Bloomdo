using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Client.Infrastructure.Services;

public class StatsApiService(HttpClient httpClient) : IStatsApiService
{
    public async Task<bool> SyncUsageAsync(SyncUsageRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Stats.Sync, request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SyncUsage failed: {ex.Message}");
            return false;
        }
    }

    public async Task<DailyStatsResponse?> GetDailyStatsAsync(DateOnly date, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiRoutes.Stats.Daily}?date={date:yyyy-MM-dd}";
            var response = await httpClient.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<DailyStatsResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetDailyStats failed: {ex.Message}");
            return null;
        }
    }

    public async Task<MonthCalendarResponse?> GetMonthCalendarAsync(int year, int month, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiRoutes.Stats.Calendar}?year={year}&month={month}";
            var response = await httpClient.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<MonthCalendarResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetMonthCalendar failed: {ex.Message}");
            return null;
        }
    }
}
