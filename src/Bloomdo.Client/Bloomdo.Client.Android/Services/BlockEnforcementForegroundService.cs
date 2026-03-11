using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Bloomdo.Shared.DTOs.Blocks;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Android.Services;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeSpecialUse)]
public class BlockEnforcementForegroundService : Service
{
    private const int NotificationId = 9001;
    private const string ChannelId = "bloomdo_enforcement";
    private Timer? _timer;
    private List<BlockRuleResponse> _cachedRules = [];
    private string? _lastBlockedPackage;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();
        StartForeground(NotificationId, notification, ForegroundService.TypeSpecialUse);

        LoadRules();
        _timer = new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _timer?.Dispose();
        _timer = null;
        base.OnDestroy();
    }

    private void OnTick(object? state)
    {
        try
        {
            LoadRules();
            if (_cachedRules.Count == 0) return;

            var foregroundPkg = GetForegroundPackage();
            if (string.IsNullOrEmpty(foregroundPkg)) return;

            if (ShouldBlock(foregroundPkg))
            {
                if (_lastBlockedPackage != foregroundPkg)
                {
                    _lastBlockedPackage = foregroundPkg;
                    ShowBlockedScreen();
                }
            }
            else
            {
                _lastBlockedPackage = null;
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"Enforcement tick error: {ex.Message}");
        }
    }

    private bool ShouldBlock(string packageName)
    {
        var now = DateTime.Now;
        var today = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        foreach (var rule in _cachedRules)
        {
            if (!rule.IsActive) continue;
            if (!rule.BlockedPackages.Contains(packageName)) continue;

            switch (rule.Type)
            {
                case BlockType.Schedule:
                    if (IsInSchedule(rule, currentTime, today))
                        return true;
                    break;

                case BlockType.Limit:
                    if (rule.DailyLimitMinutes.HasValue && IsOverDailyLimit(packageName, rule.DailyLimitMinutes.Value))
                        return true;
                    break;

                case BlockType.Focus:
                    if (rule.FocusDurationMinutes.HasValue)
                        return true;
                    break;

                case BlockType.Bloomdo:
                    return true;
            }
        }

        return false;
    }

    private static bool IsInSchedule(BlockRuleResponse rule, TimeOnly currentTime, DayOfWeek today)
    {
        if (!rule.StartTime.HasValue || !rule.EndTime.HasValue) return false;
        if (rule.Days is not null && !rule.Days.Contains(today)) return false;

        var start = rule.StartTime.Value;
        var end = rule.EndTime.Value;

        if (start <= end)
            return currentTime >= start && currentTime <= end;

        // Overnight schedule (e.g., 22:00 - 07:00)
        return currentTime >= start || currentTime <= end;
    }

    private bool IsOverDailyLimit(string packageName, int limitMinutes)
    {
        try
        {
            var usm = (UsageStatsManager)GetSystemService(UsageStatsService)!;
            var cal = Java.Util.Calendar.Instance;
            cal.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            cal.Set(Java.Util.CalendarField.HourOfDay, 0);
            cal.Set(Java.Util.CalendarField.Minute, 0);
            cal.Set(Java.Util.CalendarField.Second, 0);
            cal.Set(Java.Util.CalendarField.Millisecond, 0);
            var startOfDay = cal.TimeInMillis;
            var now = Java.Lang.JavaSystem.CurrentTimeMillis();

            var stats = usm.QueryAndAggregateUsageStats(startOfDay, now);
            if (stats.TryGetValue(packageName, out var usage) && usage is not null)
            {
                var usedMinutes = usage.TotalTimeInForeground / 60_000;
                return usedMinutes >= limitMinutes;
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"Usage check error: {ex.Message}");
        }

        return false;
    }

    private string? GetForegroundPackage()
    {
        try
        {
            var usm = (UsageStatsManager)GetSystemService(UsageStatsService)!;
            var now = Java.Lang.JavaSystem.CurrentTimeMillis();
            var events = usm.QueryEvents(now - 5000, now);
            var ev = new UsageEvents.Event();
            string? lastResumed = null;

            while (events.HasNextEvent)
            {
                events.GetNextEvent(ev);
                if (ev.EventType == UsageEventType.ActivityResumed)
                    lastResumed = ev.PackageName;
            }

            return lastResumed;
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"GetForegroundPackage error: {ex.Message}");
            return null;
        }
    }

    private void ShowBlockedScreen()
    {
        var intent = new Intent(this, typeof(BlockedActivity));
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        StartActivity(intent);
    }

    private void LoadRules()
    {
        try
        {
            var filePath = System.IO.Path.Combine(
                Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "block_rules.json");

            if (!System.IO.File.Exists(filePath))
            {
                _cachedRules = [];
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);
            _cachedRules = JsonSerializer.Deserialize<List<BlockRuleResponse>>(json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? [];
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"LoadRules error: {ex.Message}");
        }
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(ChannelId, "Block Enforcement",
            NotificationImportance.Low)
        {
            Description = "Monitors and enforces app blocking rules"
        };

        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        manager.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var builder = new Notification.Builder(this, ChannelId)
            .SetContentTitle("Bloomdo")
            .SetContentText("App blocking is active")
            .SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleLock)
            .SetOngoing(true);

        return builder.Build();
    }
}
