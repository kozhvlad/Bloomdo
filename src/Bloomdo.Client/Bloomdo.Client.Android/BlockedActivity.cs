using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Bloomdo.Client.Android;

[Activity(Theme = "@android:style/Theme.NoTitleBar.Fullscreen", LaunchMode = global::Android.Content.PM.LaunchMode.SingleInstance)]
public class BlockedActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        layout.SetGravity(GravityFlags.Center);
        layout.SetBackgroundColor(Color.ParseColor("#1a1a2e"));
        layout.SetPadding(64, 0, 64, 0);

        var icon = new TextView(this)
        {
            TextSize = 64
        };
        icon.SetText("\ud83d\udd12", TextView.BufferType.Normal);
        icon.Gravity = GravityFlags.Center;
        layout.AddView(icon);

        var spacer1 = new Space(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 48)
        };
        layout.AddView(spacer1);

        var title = new TextView(this)
        {
            TextSize = 28
        };
        title.SetText("App Blocked", TextView.BufferType.Normal);
        title.SetTextColor(Color.White);
        title.Gravity = GravityFlags.Center;
        layout.AddView(title);

        var spacer2 = new Space(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 16)
        };
        layout.AddView(spacer2);

        var subtitle = new TextView(this)
        {
            TextSize = 16
        };
        subtitle.SetText("This app is currently blocked by Bloomdo", TextView.BufferType.Normal);
        subtitle.SetTextColor(Color.ParseColor("#aaaaaa"));
        subtitle.Gravity = GravityFlags.Center;
        layout.AddView(subtitle);

        var spacer3 = new Space(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 48)
        };
        layout.AddView(spacer3);

        var button = new Button(this);
        button.SetText("Go Back", TextView.BufferType.Normal);
        button.SetTextColor(Color.White);
        button.SetBackgroundColor(Color.ParseColor("#2E7D32"));
        var buttonParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        buttonParams.SetMargins(32, 0, 32, 0);
        button.LayoutParameters = buttonParams;
        button.Click += (_, _) =>
        {
            var home = new Intent(Intent.ActionMain);
            home.AddCategory(Intent.CategoryHome);
            home.SetFlags(ActivityFlags.NewTask);
            StartActivity(home);
            Finish();
        };
        layout.AddView(button);

        SetContentView(layout);
    }

    public override void OnBackPressed()
    {
        var home = new Intent(Intent.ActionMain);
        home.AddCategory(Intent.CategoryHome);
        home.SetFlags(ActivityFlags.NewTask);
        StartActivity(home);
        Finish();
    }
}
