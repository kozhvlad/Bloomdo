using System.Diagnostics;
using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Bloomdo.Client.Application.ViewModels.OnbordingComponents;

public partial class OnboardingViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly PageViewModel[] _steps;

    [ObservableProperty]
    private PageViewModel _currentStep;

    [ObservableProperty]
    private int _currentStepIndex = 0;

    [ObservableProperty]
    private int _totalSteps;

    [ObservableProperty]
    private int _currentStepNumber = 1;

    [ObservableProperty]
    private bool _hasPrevious;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _showProgressBar;

    public OnboardingViewModel(IServiceProvider serviceProvider, INavigationService navigationService)
    {
        _navigationService = navigationService;

        _steps =
        [
            serviceProvider.GetRequiredService<WelcomeStepViewModel>(),
            serviceProvider.GetRequiredService<AskNameStepViewModel>(),
            serviceProvider.GetRequiredService<SetGoalsStepViewModel>()
        ];

        foreach (var step in _steps)
        {
            switch (step)
            {
                case WelcomeStepViewModel w:
                    w.Parent = this;
                    break;
                case AskNameStepViewModel a:
                    a.Parent = this;
                    break;
                case SetGoalsStepViewModel s:
                    s.Parent = this;
                    break;
            }
        }

        TotalSteps = _steps.Length;
        ProgressTotal = Math.Max(0, TotalSteps - 1);
        CurrentStep = _steps[0];
        HasPrevious = false;
        ProgressValue = 0;
        ShowProgressBar = false;
    }

    [RelayCommand]
    private void NextStep()
    {
        Debug.WriteLine($"NextStep called. CurrentStepIndex: {CurrentStepIndex}, TotalSteps: {_steps.Length}");

        if (CurrentStepIndex < _steps.Length - 1)
        {
            CurrentStepIndex++;
            CurrentStep = _steps[CurrentStepIndex];
            CurrentStepNumber = CurrentStepIndex + 1;
            HasPrevious = CurrentStepIndex > 0;
            ProgressValue = CurrentStepIndex;
            ShowProgressBar = CurrentStepIndex > 0;
            Debug.WriteLine($"Moved to step {CurrentStepIndex}");
        }
        else
        {
            Debug.WriteLine("Onboarding complete - calling NavigationService.OnboardingComplete");
            _navigationService.OnboardingComplete();
        }
    }

    [RelayCommand]
    private void Skip()
    {
        Debug.WriteLine("Onboarding skipped by user");
        _navigationService.OnboardingComplete();
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            CurrentStep = _steps[CurrentStepIndex];
            CurrentStepNumber = CurrentStepIndex + 1;
            HasPrevious = CurrentStepIndex > 0;
            ProgressValue = CurrentStepIndex;
            ShowProgressBar = CurrentStepIndex > 0;
        }
    }
}
