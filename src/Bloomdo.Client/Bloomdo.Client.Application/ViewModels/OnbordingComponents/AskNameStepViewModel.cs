using System.Text.RegularExpressions;
using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.OnbordingComponents;

public partial class AskNameStepViewModel : PageViewModel
{
    private readonly IPreferencesService _preferencesService;

    public OnboardingViewModel? Parent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    [NotifyPropertyChangedFor(nameof(NameError))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    [NotifyPropertyChangedFor(nameof(TagError))]
    [NotifyPropertyChangedFor(nameof(TagPreview))]
    private string _tag = string.Empty;

    public AskNameStepViewModel(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    partial void OnNameChanged(string value)
    {
        ContinueCommand.NotifyCanExecuteChanged();
    }

    partial void OnTagChanged(string value)
    {
        ContinueCommand.NotifyCanExecuteChanged();
    }

    public string TrimmedName => (_name).Trim();
    public string TrimmedTag => (_tag).Trim().TrimStart('@').ToLowerInvariant();

    private static readonly Regex TagRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);

    public bool IsNameValid => !string.IsNullOrWhiteSpace(TrimmedName);
    public bool IsTagValid => TagRegex.IsMatch(TrimmedTag);
    public bool IsValid => IsNameValid && IsTagValid;
    public bool IsInvalid => !IsValid;

    public string? NameError => string.IsNullOrWhiteSpace(Name) ? null :
        !IsNameValid ? "Name can't be empty" : null;

    public string? TagError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Tag)) return null;
            if (TrimmedTag.Length < 3) return "Tag must be at least 3 characters";
            if (TrimmedTag.Length > 20) return "Tag must be 20 characters or less";
            if (!TagRegex.IsMatch(TrimmedTag)) return "Only letters, numbers and underscores";
            return null;
        }
    }

    public string TagPreview => !string.IsNullOrWhiteSpace(TrimmedTag) ? $"@{TrimmedTag}" : "@";

    [RelayCommand(CanExecute = nameof(IsValid))]
    private void Continue()
    {
        if (!IsValid) return;

        // Persist for use in Registration
        _preferencesService.Set("Onboarding_Name", TrimmedName);
        _preferencesService.Set("Onboarding_Tag", TrimmedTag);

        Parent?.NextStepCommand.Execute(null);
    }

    [RelayCommand]
    private void Back()
    {
        Parent?.PreviousStepCommand.Execute(null);
    }
}
