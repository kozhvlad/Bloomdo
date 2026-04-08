using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Client.Infrastructure.Services;
using Bloomdo.Shared.DTOs.Blocks;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Android.Services;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeSpecialUse)]
public class BlockEnforcementForegroundService : Service
{
    private const int NotificationId = 9001;
    private const string ChannelId = "bloomdo_enforcement";
    private const string OwnPackage = "com.CompanyName.Bloomdo.Client";
    private static readonly TimeSpan UsageSaveInterval = TimeSpan.FromMinutes(15);
    private Timer? _timer;
    private List<BlockRuleResponse> _cachedRules = [];
    private Dictionary<Guid, bool> _cachedGroupCompletion = [];
    private string? _lastBlockedPackage;
    private DateTime _lastUsageSaveUtc = DateTime.MinValue;

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
            LoadGroupCompletion();
            if (_cachedRules.Count == 0) return;

            var foregroundPkg = GetForegroundPackage();
            if (string.IsNullOrEmpty(foregroundPkg)) return;

            // Never block our own app
            if (foregroundPkg == OwnPackage) return;

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

            // Periodically save usage data locally so it survives reboots
            SaveUsageLocallyIfDue();
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"Enforcement tick error: {ex.Message}");
        }
    }

    private bool ShouldBlock(string packageName)
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
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
                    if (rule.FocusDurationMinutes.HasValue && rule.FocusStartedAtUtc.HasValue)
                    {
                        var elapsed = utcNow - rule.FocusStartedAtUtc.Value;
                        if (elapsed.TotalMinutes < rule.FocusDurationMinutes.Value)
                            return true;
                    }
                    break;

                case BlockType.Bloomdo:
                    if (rule.RequiredActivityGroupId.HasValue &&
                        _cachedGroupCompletion.TryGetValue(rule.RequiredActivityGroupId.Value, out var groupDone) && groupDone)
                        break; // Group completed — allow access
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

    private void SaveUsageLocallyIfDue()
    {
        if (DateTime.UtcNow - _lastUsageSaveUtc < UsageSaveInterval) return;
        _lastUsageSaveUtc = DateTime.UtcNow;

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

            var pm = PackageManager!;
            var stats = usm.QueryAndAggregateUsageStats(startOfDay, now);
            var apps = new List<LocalAppUsageEntry>();

            foreach (var kv in stats)
            {
                var pkg = kv.Key;
                var s = kv.Value;
                if (s == null) continue;

                try
                {
                    if (pm.GetLaunchIntentForPackage(pkg) == null) continue;
                }
                catch { continue; }

                var fgMs = s.TotalTimeInForeground;
                if (fgMs <= 0) continue;

                string? label = null;
                try
                {
                    var appInfo = pm.GetApplicationInfo(pkg, 0);
                    label = pm.GetApplicationLabel(appInfo);
                }
                catch { }

                apps.Add(new LocalAppUsageEntry
                {
                    PackageName = pkg,
                    AppLabel = label,
                    ForegroundSeconds = (int)(fgMs / 1000)
                });
            }

            if (apps.Count > 0)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                LocalUsageStore.SaveSnapshotDirect(today, 0, apps);
                Log.Info("Bloomdo", $"Saved local usage snapshot: {apps.Count} apps");
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"SaveUsageLocally error: {ex.Message}");
        }
    }

    private string? GetForegroundPackage()
    {
        try
        {
            var usm = (UsageStatsManager)GetSystemService(UsageStatsService)!;
            var now = Java.Lang.JavaSystem.CurrentTimeMillis();
            // Use 30-minute window so apps open longer than a few seconds are still detected
            var events = usm.QueryEvents(now - 30 * 60 * 1000, now);
            var ev = new UsageEvents.Event();
            string? currentForeground = null;

            while (events.HasNextEvent)
            {
                events.GetNextEvent(ev);
                if (ev.EventType == UsageEventType.ActivityResumed)
                    currentForeground = ev.PackageName;
                else if (ev.EventType == UsageEventType.ActivityPaused && ev.PackageName == currentForeground)
                    currentForeground = null;
            }

            return currentForeground;
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

    private void LoadGroupCompletion()
    {
        try
        {
            var filePath = System.IO.Path.Combine(
                Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "group_completion.json");

            if (!System.IO.File.Exists(filePath))
            {
                _cachedGroupCompletion = [];
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);
            _cachedGroupCompletion = JsonSerializer.Deserialize<Dictionary<Guid, bool>>(json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? [];
        }
        catch (Exception ex)
        {
            Log.Warn("Bloomdo", $"LoadGroupCompletion error: {ex.Message}");
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
