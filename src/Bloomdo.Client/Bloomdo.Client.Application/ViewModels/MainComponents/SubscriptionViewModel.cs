using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Subscription;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class SubscriptionViewModel : PageViewModel
{
    private readonly ISubscriptionApiService _subscriptionApiService;
    private readonly IToastService _toastService;
    private readonly IBrowserService _browserService;
    private readonly IConnectivityService? _connectivityService;
    private readonly ILocalSubscriptionStore? _localSubscriptionStore;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPremium;

    [ObservableProperty]
    private bool _willCancel;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _expiryText = string.Empty;

    [ObservableProperty]
    private string _currentPlanText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsYearlySelected))]
    private bool _isMonthlySelected = true;

    public bool IsYearlySelected => !IsMonthlySelected;

    [ObservableProperty]
    private bool _isCheckoutLoading;

    [ObservableProperty]
    private bool _isOffline;

    public SubscriptionViewModel(
        ISubscriptionApiService subscriptionApiService,
        IToastService toastService,
        IBrowserService browserService,
        IConnectivityService? connectivityService = null,
        ILocalSubscriptionStore? localSubscriptionStore = null)
    {
        _subscriptionApiService = subscriptionApiService;
        _toastService = toastService;
        _browserService = browserService;
        _connectivityService = connectivityService;
        _localSubscriptionStore = localSubscriptionStore;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        IsOffline = _connectivityService is not null && !_connectivityService.IsOnline;
        if (IsOffline)
            _ = LoadStatusFromCacheAsync();
        else
            _ = LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        IsLoading = true;
        try
        {
            var status = await _subscriptionApiService.GetStatusAsync();
            if (status is not null)
            {
                ApplyStatus(status);

                if (_localSubscriptionStore is not null)
                    _ = _localSubscriptionStore.SaveAsync(status);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load subscription status failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatusFromCacheAsync()
    {
        if (_localSubscriptionStore is null) return;

        IsLoading = true;
        try
        {
            var status = await _localSubscriptionStore.LoadAsync();
            if (status is not null)
                ApplyStatus(status);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load subscription status from cache failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyStatus(SubscriptionStatusResponse status)
    {
        IsPremium = status.IsPremium;
        WillCancel = status.WillCancel;

        if (status.IsPremium)
        {
            CurrentPlanText = status.Plan switch
            {
                SubscriptionPlan.Monthly => "Monthly Plan",
                SubscriptionPlan.Yearly => "Yearly Plan",
                _ => "Active"
            };

            StatusText = status.WillCancel
                ? "Cancels at end of period"
                : "Active";

            if (status.CurrentPeriodEnd.HasValue)
            {
                var daysLeft = (status.CurrentPeriodEnd.Value - DateTime.UtcNow).Days;
                ExpiryText = status.WillCancel
                    ? $"Access until {status.CurrentPeriodEnd.Value:MMM dd, yyyy} ({daysLeft} days left)"
                    : $"Renews {status.CurrentPeriodEnd.Value:MMM dd, yyyy}";
            }
        }
        else
        {
            StatusText = "Free";
            CurrentPlanText = string.Empty;
            ExpiryText = string.Empty;
        }
    }

    [RelayCommand]
    private void SelectMonthly()
    {
        IsMonthlySelected = true;
    }

    [RelayCommand]
    private void SelectYearly()
    {
        IsMonthlySelected = false;
    }

    [RelayCommand]
    private async Task Subscribe()
    {
        if (IsCheckoutLoading) return;

        IsCheckoutLoading = true;
        try
        {
            var plan = IsMonthlySelected ? SubscriptionPlan.Monthly : SubscriptionPlan.Yearly;
            var result = await _subscriptionApiService.CreateCheckoutSessionAsync(plan);

            if (result is not null && !string.IsNullOrEmpty(result.CheckoutUrl))
            {
                await _browserService.OpenAsync(new Uri(result.CheckoutUrl));
            }
            else
            {
                _toastService.ShowError("Failed to start checkout. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Subscribe failed: {ex.Message}");
            _toastService.ShowError("Something went wrong. Please try again.");
        }
        finally
        {
            IsCheckoutLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelSubscription()
    {
        if (!IsPremium) return;

        IsLoading = true;
        try
        {
            var success = await _subscriptionApiService.CancelSubscriptionAsync();
            if (success)
            {
                _toastService.ShowSuccess("Subscription will cancel at end of billing period.");
                await LoadStatusAsync();
            }
            else
            {
                _toastService.ShowError("Failed to cancel. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cancel failed: {ex.Message}");
            _toastService.ShowError("Something went wrong.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshStatus()
    {
        await LoadStatusAsync();
    }
}
