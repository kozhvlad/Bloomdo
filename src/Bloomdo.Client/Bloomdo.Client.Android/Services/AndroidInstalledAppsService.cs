using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Android.Services;

public sealed class AndroidInstalledAppsService(Context context) : IInstalledAppsService
{
    public Task<IReadOnlyList<InstalledAppInfo>> GetInstalledAppsAsync()
    {
        var pm = context.PackageManager!;
        var intent = new Intent(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);

        var resolveInfos = pm.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);

        string? ownPackage = context.PackageName;
        string? launcherPkg = null;
        try
        {
            var homeIntent = new Intent(Intent.ActionMain);
            homeIntent.AddCategory(Intent.CategoryHome);
            var homeResolve = pm.ResolveActivity(homeIntent, PackageInfoFlags.MatchDefaultOnly);
            launcherPkg = homeResolve?.ActivityInfo?.PackageName;
        }
        catch { }

        var apps = new List<InstalledAppInfo>();

        foreach (var info in resolveInfos)
        {
            var pkg = info.ActivityInfo?.PackageName;
            if (string.IsNullOrEmpty(pkg)) continue;
            if (pkg == ownPackage) continue;
            if (pkg == launcherPkg) continue;
            if (pkg == "com.android.systemui") continue;
            if (pkg == "com.android.settings") continue;

            string label;
            try
            {
                label = info.LoadLabel(pm)?.ToString() ?? pkg;
            }
            catch
            {
                label = pkg;
            }

            if (apps.All(a => a.PackageName != pkg))
            {
                apps.Add(new InstalledAppInfo
                {
                    PackageName = pkg,
                    AppLabel = label
                });
            }
        }

        var sorted = apps.OrderBy(a => a.AppLabel).ToList();
        return Task.FromResult((IReadOnlyList<InstalledAppInfo>)sorted);
    }
}
