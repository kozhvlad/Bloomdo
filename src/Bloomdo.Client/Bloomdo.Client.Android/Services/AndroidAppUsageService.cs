using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Android.App.Usage;
using Android.Util;
using Bloomdo.Core.Interfaces;
using Bloomdo.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bloomdo.Android.Services
{
    public sealed class AndroidAppUsageService(Context context) : IAppUsageService
    {
	    public Task<IReadOnlyList<AppUsageInfo>> GetTodayUsageAsync()
        {
            var list = new List<AppUsageInfo>();

            if (!HasUsageAccess())
            {
                try
                {
                    var intent = new Intent(Settings.ActionUsageAccessSettings);
                    intent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(intent);
                }
                catch (Exception ex)
                {
                    Log.Warn("Bloomdo", $"Cannot open usage access settings: {ex.Message}");
                }

                return Task.FromResult((IReadOnlyList<AppUsageInfo>)list);
            }

            var usageStatsManager = (UsageStatsManager)context.GetSystemService(Context.UsageStatsService)!;
            var cal = Java.Util.Calendar.Instance;
            cal.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            cal.Set(Java.Util.CalendarField.HourOfDay, 0);
            cal.Set(Java.Util.CalendarField.Minute, 0);
            cal.Set(Java.Util.CalendarField.Second, 0);
            cal.Set(Java.Util.CalendarField.Millisecond, 0);
            var start = cal.TimeInMillis;
            var end = Java.Lang.JavaSystem.CurrentTimeMillis();

            var pm = context.PackageManager!;

            string? launcherPkg = null;
            try
            {
                var intent = new Intent(Intent.ActionMain);
                intent.AddCategory(Intent.CategoryHome);
                var resolveInfo = pm.ResolveActivity(intent, PackageInfoFlags.MatchDefaultOnly);
                launcherPkg = resolveInfo?.ActivityInfo?.PackageName;
            }
            catch { }

            try
            {
                var events = usageStatsManager.QueryEvents(start, end);
                var eventObj = new UsageEvents.Event();
                var resumeTimes = new Dictionary<string, long>();
                var durations = new Dictionary<string, long>();

                while (events.HasNextEvent)
                {
                    events.GetNextEvent(eventObj);
                    var t = eventObj.TimeStamp;
                    var pkg = eventObj.PackageName;

                    if (eventObj.EventType == UsageEventType.ScreenNonInteractive)
                    {
                        var openPkgs = resumeTimes.Keys.ToList();
                        foreach (var open in openPkgs)
                        {
                            var startTs = resumeTimes[open];
                            var delta = t - startTs;
                            if (!durations.ContainsKey(open))
                            {
                                durations[open] = 0;
                            }

                            durations[open] += delta;
                        }
                        resumeTimes.Clear();
                        continue;
                    }

                    if (string.IsNullOrEmpty(pkg))
                    {
                        continue;
                    }

                    if (pkg == launcherPkg)
                    {
                        continue;
                    }

                    if (pkg == "com.android.systemui")
                    {
                        continue;
                    }

                    if (eventObj.EventType == UsageEventType.ActivityResumed)
                    {
                        var stuckPkgs = resumeTimes.Keys.ToList();
                        foreach (var stuck in stuckPkgs)
                        {
                            if (stuck != pkg)
                            {
                                var startTs = resumeTimes[stuck];
                                var delta = t - startTs;
                                if (!durations.ContainsKey(stuck))
                                {
                                    durations[stuck] = 0;
                                }

                                durations[stuck] += delta;
                                resumeTimes.Remove(stuck);
                            }
                        }
                        
                        resumeTimes[pkg] = t;
                    }
                    else if (eventObj.EventType == UsageEventType.ActivityPaused)
                    {
                        if (resumeTimes.TryGetValue(pkg, out var startTs))
                        {
                            var delta = t - startTs;
                            if (!durations.ContainsKey(pkg))
                            {
                                durations[pkg] = 0;
                            }

                            durations[pkg] += delta;
                            resumeTimes.Remove(pkg);
                        }
                    }
                }

                foreach (var kv in resumeTimes)
                {
                    var pkg = kv.Key;
                    var startTs = kv.Value;
                    if (!durations.ContainsKey(pkg))
                    {
                        durations[pkg] = 0;
                    }

                    durations[pkg] += Math.Max(0, end - startTs);
                }

                foreach (var kv in durations)
                {
                    var pkg = kv.Key;
                    var ms = kv.Value;
                    if (ms <= 0)
                    {
                        continue;
                    }
                    
                    string? label = null;
                    try
                    {
                        var appInfo = pm.GetApplicationInfo(pkg, 0);
                        label = pm.GetApplicationLabel(appInfo);
                    }
                    catch { }
                    
                    list.Add(new AppUsageInfo
                    {
                        PackageName = pkg,
                        AppLabel = label,
                        ForegroundTime = TimeSpan.FromMilliseconds(ms)
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Bloomdo", $"QueryEvents failed: {ex.Message}");
            }

            if (list.Count == 0)
            {
                try
                {
                    var aggregated = usageStatsManager.QueryAndAggregateUsageStats(start, end);
                    foreach (var kv in aggregated)
                    {
                        var pkg = kv.Key;
                        var s = kv.Value;
                        if (s == null)
                        {
                            continue;
                        }

                        try
                        {
                            if (pm.GetLaunchIntentForPackage(pkg) == null)
                            {
                                continue;
                            }
                        }
                        catch { continue; }

                        var fgMs = s.TotalTimeInForeground;
                        if (fgMs <= 0)
                        {
                            continue;
                        }

                        string? label = null;
                        try
                        {
                            var appInfo = pm.GetApplicationInfo(pkg, 0);
                            label = pm.GetApplicationLabel(appInfo);
                        }
                        catch { }

                        list.Add(new AppUsageInfo
                        {
                            PackageName = pkg,
                            AppLabel = label,
                            ForegroundTime = TimeSpan.FromMilliseconds(fgMs)
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Bloomdo", $"Aggregated fallback failed: {ex.Message}");
                }
            }

            list = list.OrderByDescending(x => x.ForegroundTime).ToList();
            return Task.FromResult((IReadOnlyList<AppUsageInfo>)list);
        }

        private bool HasUsageAccess()
        {
            try
            {
                var appOps = (AppOpsManager)context.GetSystemService(Context.AppOpsService)!;
                var mode = appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, global::Android.OS.Process.MyUid(), context.PackageName);
                return mode == AppOpsManagerMode.Allowed;
            }
            catch
            {
                return false;
            }
        }

        public Task<int> GetPickupsTodayAsync()
        {
            if (!HasUsageAccess())
            {
                return Task.FromResult(0);
            }

            var usageStatsManager = (UsageStatsManager)context.GetSystemService(Context.UsageStatsService)!;
            var cal = Java.Util.Calendar.Instance;
            cal.TimeInMillis = Java.Lang.JavaSystem.CurrentTimeMillis();
            cal.Set(Java.Util.CalendarField.HourOfDay, 0);
            cal.Set(Java.Util.CalendarField.Minute, 0);
            cal.Set(Java.Util.CalendarField.Second, 0);
            cal.Set(Java.Util.CalendarField.Millisecond, 0);
            var start = cal.TimeInMillis;
            var end = Java.Lang.JavaSystem.CurrentTimeMillis();

            var pickups = 0;
            try
            {
                var events = usageStatsManager.QueryEvents(start, end);
                var eventObj = new UsageEvents.Event();
                long lastPickupTime = 0;
                var lastEventWasScreenOff = false;

                while (events.HasNextEvent)
                {
                    events.GetNextEvent(eventObj);
                    
                    var isPickup = false;

                    if (eventObj.EventType == UsageEventType.ScreenInteractive || 
                        eventObj.EventType == UsageEventType.KeyguardHidden)
                    {
                        isPickup = true;
                        lastEventWasScreenOff = false;
                    }
                    else if (eventObj.EventType == UsageEventType.ActivityResumed)
                    {
                        if (lastEventWasScreenOff)
                        {
                            isPickup = true;
                            lastEventWasScreenOff = false;
                        }
                    }
                    else if (eventObj.EventType == UsageEventType.ScreenNonInteractive)
                    {
                        lastEventWasScreenOff = true;
                    }

                    if (isPickup)
                    {
                        if (eventObj.TimeStamp - lastPickupTime > 5000)
                        {
                            pickups++;
                            lastPickupTime = eventObj.TimeStamp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Bloomdo", $"GetPickupsTodayAsync failed: {ex.Message}");
            }

            return Task.FromResult(pickups);
        }
    }
}
