using System.Diagnostics;
using Bloomdo.Client.Core;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Application.ViewModels.OnbordingComponents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly IPreferencesService _preferencesService;
    private readonly Func<INavigationService> _navigationServiceFactory;
    private INavigationService? _navigationService;
    private SynchronizationContext? _uiContext;

    private const string OnboardingCompletedKey = "OnboardingCompleted";

    [ObservableProperty]
    private IPage _currentViewModel = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverlayOpen))]
    private ObservableObject? _overlayContent;

    public bool IsOverlayOpen => OverlayContent is not null;

    /// <summary>
    /// Action invoked when the overlay is dismissed (background tap or close).
    /// </summary>
    public Action? OnOverlayClosed { get; set; }

    [RelayCommand]
    private void CloseOverlay()
    {
        OverlayContent = null;
        var callback = OnOverlayClosed;
        OnOverlayClosed = null;
        callback?.Invoke();
    }

    public ShellViewModel(
        IAccessTokenManager tokenManager,
        IPreferencesService preferencesService,
        Func<INavigationService> navigationServiceFactory)
    {
        _tokenManager = tokenManager;
        _preferencesService = preferencesService;
        _navigationServiceFactory = navigationServiceFactory;
    }

    public async Task InitializeAsync()
    {
        Debug.WriteLine("ShellViewModel.InitializeAsync started");

        _uiContext = SynchronizationContext.Current;
        _navigationService ??= _navigationServiceFactory();
        _tokenManager.SessionInvalidated += OnSessionInvalidated;

        // First launch → show onboarding
        if (!IsOnboardingCompleted())
        {
            Debug.WriteLine("First launch, navigating to OnboardingViewModel");
            _navigationService.NavigateTo<OnboardingViewModel>();
            return;
        }

        // Dev shortcut — skip auth
        if (AppConfig.SkipAuthentication)
        {
            Debug.WriteLine("SkipAuthentication enabled, navigating to MainViewModel");
            _navigationService.NavigateTo<MainViewModel>();
            return;
        }

        try
        {
            await _tokenManager.InitializeAsync();

            if (_tokenManager.IsAuthenticated)
            {
                Debug.WriteLine("User is authenticated, navigating to MainViewModel");
                _navigationService.NavigateTo<MainViewModel>();
            }
            else
            {
                Debug.WriteLine("User is not authenticated, navigating to LoginViewModel");
                _navigationService.NavigateTo<LoginViewModel>();
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Network error during initialization: {ex.Message}");
            _navigationService.NavigateTo<NoConnectionViewModel>();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Debug.WriteLine($"Timeout during initialization: {ex.Message}");
            _navigationService.NavigateTo<NoConnectionViewModel>();
        }
    }

    private void OnSessionInvalidated()
    {
        Debug.WriteLine("Session invalidated, navigating to LoginViewModel");

        void Navigate()
        {
            _navigationService ??= _navigationServiceFactory();
            _navigationService.NavigateTo<LoginViewModel>();
        }

        if (_uiContext != null)
        {
            _uiContext.Post(_ => Navigate(), null);
        }
        else
        {
            Navigate();
        }
    }

    public void SetViewModel(IPage viewModel)
    {
        Debug.WriteLine($"ShellViewModel.SetViewModel called with {viewModel?.GetType().Name ?? "null"}");
        CurrentViewModel?.OnDisappearing();
        CurrentViewModel = viewModel;
        CurrentViewModel.OnAppearing();
        Debug.WriteLine($"CurrentViewModel is now {CurrentViewModel?.GetType().Name ?? "null"}");
    }

    public void CompleteOnboarding()
    {
        _preferencesService.Set(OnboardingCompletedKey, true);
    }

    private bool IsOnboardingCompleted()
    {
        if (AppConfig.ForceShowOnboarding)
            return false;

        return _preferencesService.Get(OnboardingCompletedKey, false);
    }

    public void ResetOnboarding()
    {
        _preferencesService.Set(OnboardingCompletedKey, false);
    }
}

