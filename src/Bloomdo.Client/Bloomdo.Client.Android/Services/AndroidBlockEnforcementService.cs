using Android.Content;
using Android.OS;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Android.Services;

public sealed class AndroidBlockEnforcementService(Context context) : IBlockEnforcementService
{
    public void Start()
    {
        var intent = new Intent(context, typeof(BlockEnforcementForegroundService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public void Stop()
    {
        var intent = new Intent(context, typeof(BlockEnforcementForegroundService));
        context.StopService(intent);
    }
}
