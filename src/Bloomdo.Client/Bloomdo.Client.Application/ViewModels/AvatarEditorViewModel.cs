using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class AvatarEditorViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;
    private readonly IProfileApiService _profileApiService;
    private Action<AvatarConfig>? _onSaved;

    [ObservableProperty] private int _selectedSkinTone;
    [ObservableProperty] private int _selectedHairStyle;
    [ObservableProperty] private int _selectedHairColor;
    [ObservableProperty] private int _selectedEyeStyle;
    [ObservableProperty] private int _selectedAccessory;
    [ObservableProperty] private int _selectedClothingStyle;
    [ObservableProperty] private int _selectedClothingColor;
    [ObservableProperty] private int _selectedBackgroundColor;
    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private string _previewBackgroundHex = "#7E57C2";
    [ObservableProperty] private string _previewSkinHex = "#FDDBB4";
    [ObservableProperty] private string _previewHairHex = "#2C2C2C";
    [ObservableProperty] private string _previewClothingHex = "#66BB6A";
    [ObservableProperty] private string _previewHairStyleLabel = "Short";
    [ObservableProperty] private string _previewAccessoryLabel = "None";

    public ObservableCollection<AvatarPartOption> SkinTones { get; } = [];
    public ObservableCollection<AvatarPartOption> HairStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> HairColors { get; } = [];
    public ObservableCollection<AvatarPartOption> EyeStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> Accessories { get; } = [];
    public ObservableCollection<AvatarPartOption> ClothingStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> ClothingColors { get; } = [];
    public ObservableCollection<AvatarPartOption> BackgroundColors { get; } = [];

    public AvatarEditorViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IProfileApiService profileApiService)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _profileApiService = profileApiService;

        InitializeOptions();
    }

    public void Initialize(AvatarConfig? currentAvatar, Action<AvatarConfig> onSaved)
    {
        _onSaved = onSaved;

        if (currentAvatar != null)
        {
            SelectedSkinTone = currentAvatar.SkinTone;
            SelectedHairStyle = currentAvatar.HairStyle;
            SelectedHairColor = currentAvatar.HairColor;
            SelectedEyeStyle = currentAvatar.EyeStyle;
            SelectedAccessory = currentAvatar.Accessory;
            SelectedClothingStyle = currentAvatar.ClothingStyle;
            SelectedClothingColor = currentAvatar.ClothingColor;
            SelectedBackgroundColor = currentAvatar.BackgroundColor;
        }

        UpdateSelections();
        UpdatePreview();
    }

    private void InitializeOptions()
    {
        SkinTones.Add(new AvatarPartOption { Id = 0, Label = "Light", ColorHex = "#FDDBB4" });
        SkinTones.Add(new AvatarPartOption { Id = 1, Label = "Fair", ColorHex = "#E8B98D" });
        SkinTones.Add(new AvatarPartOption { Id = 2, Label = "Medium", ColorHex = "#D4915A" });
        SkinTones.Add(new AvatarPartOption { Id = 3, Label = "Tan", ColorHex = "#B07040" });
        SkinTones.Add(new AvatarPartOption { Id = 4, Label = "Brown", ColorHex = "#8B5E3C" });
        SkinTones.Add(new AvatarPartOption { Id = 5, Label = "Dark", ColorHex = "#5C3A1E" });

        HairStyles.Add(new AvatarPartOption { Id = 0, Label = "Short", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 1, Label = "Medium", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 2, Label = "Long", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 3, Label = "Curly", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 4, Label = "Buzz", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 5, Label = "None", ColorHex = "#78909C" });

        HairColors.Add(new AvatarPartOption { Id = 0, Label = "Black", ColorHex = "#2C2C2C" });
        HairColors.Add(new AvatarPartOption { Id = 1, Label = "Brown", ColorHex = "#6B4226" });
        HairColors.Add(new AvatarPartOption { Id = 2, Label = "Blonde", ColorHex = "#F5D76E" });
        HairColors.Add(new AvatarPartOption { Id = 3, Label = "Red", ColorHex = "#C0392B" });
        HairColors.Add(new AvatarPartOption { Id = 4, Label = "Blue", ColorHex = "#2980B9" });
        HairColors.Add(new AvatarPartOption { Id = 5, Label = "Purple", ColorHex = "#8E44AD" });

        EyeStyles.Add(new AvatarPartOption { Id = 0, Label = "Normal", ColorHex = "#5D4037" });
        EyeStyles.Add(new AvatarPartOption { Id = 1, Label = "Happy", ColorHex = "#66BB6A" });
        EyeStyles.Add(new AvatarPartOption { Id = 2, Label = "Cool", ColorHex = "#42A5F5" });
        EyeStyles.Add(new AvatarPartOption { Id = 3, Label = "Wink", ColorHex = "#FFA726" });

        Accessories.Add(new AvatarPartOption { Id = 0, Label = "None", ColorHex = "#B0BEC5" });
        Accessories.Add(new AvatarPartOption { Id = 1, Label = "Glasses", ColorHex = "#42A5F5" });
        Accessories.Add(new AvatarPartOption { Id = 2, Label = "Sunglasses", ColorHex = "#37474F" });
        Accessories.Add(new AvatarPartOption { Id = 3, Label = "Cap", ColorHex = "#EF5350" });
        Accessories.Add(new AvatarPartOption { Id = 4, Label = "Headband", ColorHex = "#AB47BC" });

        ClothingStyles.Add(new AvatarPartOption { Id = 0, Label = "T-Shirt", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 1, Label = "Hoodie", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 2, Label = "Shirt", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 3, Label = "Tank Top", ColorHex = "#78909C" });

        ClothingColors.Add(new AvatarPartOption { Id = 0, Label = "Green", ColorHex = "#66BB6A" });
        ClothingColors.Add(new AvatarPartOption { Id = 1, Label = "Blue", ColorHex = "#42A5F5" });
        ClothingColors.Add(new AvatarPartOption { Id = 2, Label = "Red", ColorHex = "#EF5350" });
        ClothingColors.Add(new AvatarPartOption { Id = 3, Label = "Purple", ColorHex = "#AB47BC" });
        ClothingColors.Add(new AvatarPartOption { Id = 4, Label = "Orange", ColorHex = "#FFA726" });
        ClothingColors.Add(new AvatarPartOption { Id = 5, Label = "Black", ColorHex = "#37474F" });

        BackgroundColors.Add(new AvatarPartOption { Id = 0, Label = "Purple", ColorHex = "#7E57C2" });
        BackgroundColors.Add(new AvatarPartOption { Id = 1, Label = "Blue", ColorHex = "#42A5F5" });
        BackgroundColors.Add(new AvatarPartOption { Id = 2, Label = "Green", ColorHex = "#66BB6A" });
        BackgroundColors.Add(new AvatarPartOption { Id = 3, Label = "Orange", ColorHex = "#FFA726" });
        BackgroundColors.Add(new AvatarPartOption { Id = 4, Label = "Pink", ColorHex = "#EC407A" });
        BackgroundColors.Add(new AvatarPartOption { Id = 5, Label = "Teal", ColorHex = "#26A69A" });
    }

    private void UpdateSelections()
    {
        SelectInCollection(SkinTones, SelectedSkinTone);
        SelectInCollection(HairStyles, SelectedHairStyle);
        SelectInCollection(HairColors, SelectedHairColor);
        SelectInCollection(EyeStyles, SelectedEyeStyle);
        SelectInCollection(Accessories, SelectedAccessory);
        SelectInCollection(ClothingStyles, SelectedClothingStyle);
        SelectInCollection(ClothingColors, SelectedClothingColor);
        SelectInCollection(BackgroundColors, SelectedBackgroundColor);
    }

    private void UpdatePreview()
    {
        PreviewBackgroundHex = GetBackgroundColorHex();
        PreviewSkinHex = GetSkinColorHex();
        PreviewHairHex = GetHairColorHex();
        PreviewClothingHex = GetClothingColorHex();

        var hair = HairStyles.FirstOrDefault(h => h.Id == SelectedHairStyle);
        PreviewHairStyleLabel = hair?.Label ?? "Short";

        var acc = Accessories.FirstOrDefault(a => a.Id == SelectedAccessory);
        PreviewAccessoryLabel = acc?.Label ?? "None";
    }

    private static void SelectInCollection(ObservableCollection<AvatarPartOption> collection, int selectedId)
    {
        foreach (var item in collection)
            item.IsSelected = item.Id == selectedId;
    }

    [RelayCommand]
    private void SelectSkinTone(AvatarPartOption option)
    {
        SelectedSkinTone = option.Id;
        SelectInCollection(SkinTones, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectHairStyle(AvatarPartOption option)
    {
        SelectedHairStyle = option.Id;
        SelectInCollection(HairStyles, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectHairColor(AvatarPartOption option)
    {
        SelectedHairColor = option.Id;
        SelectInCollection(HairColors, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectEyeStyle(AvatarPartOption option)
    {
        SelectedEyeStyle = option.Id;
        SelectInCollection(EyeStyles, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectAccessory(AvatarPartOption option)
    {
        SelectedAccessory = option.Id;
        SelectInCollection(Accessories, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectClothingStyle(AvatarPartOption option)
    {
        SelectedClothingStyle = option.Id;
        SelectInCollection(ClothingStyles, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectClothingColor(AvatarPartOption option)
    {
        SelectedClothingColor = option.Id;
        SelectInCollection(ClothingColors, option.Id);
        UpdatePreview();
    }

    [RelayCommand]
    private void SelectBackgroundColor(AvatarPartOption option)
    {
        SelectedBackgroundColor = option.Id;
        SelectInCollection(BackgroundColors, option.Id);
        UpdatePreview();
    }

    public AvatarConfig BuildAvatarConfig()
    {
        return new AvatarConfig
        {
            SkinTone = SelectedSkinTone,
            HairStyle = SelectedHairStyle,
            HairColor = SelectedHairColor,
            EyeStyle = SelectedEyeStyle,
            Accessory = SelectedAccessory,
            ClothingStyle = SelectedClothingStyle,
            ClothingColor = SelectedClothingColor,
            BackgroundColor = SelectedBackgroundColor
        };
    }

    public string GetBackgroundColorHex()
    {
        var bg = BackgroundColors.FirstOrDefault(b => b.Id == SelectedBackgroundColor);
        return bg?.ColorHex ?? "#7E57C2";
    }

    public string GetSkinColorHex()
    {
        var skin = SkinTones.FirstOrDefault(s => s.Id == SelectedSkinTone);
        return skin?.ColorHex ?? "#FDDBB4";
    }

    public string GetHairColorHex()
    {
        var hair = HairColors.FirstOrDefault(h => h.Id == SelectedHairColor);
        return hair?.ColorHex ?? "#2C2C2C";
    }

    public string GetClothingColorHex()
    {
        var clothing = ClothingColors.FirstOrDefault(c => c.Id == SelectedClothingColor);
        return clothing?.ColorHex ?? "#66BB6A";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        IsSaving = true;

        try
        {
            var avatarConfig = BuildAvatarConfig();
            var request = new UpdateProfileRequest { Avatar = avatarConfig };
            var result = await _profileApiService.UpdateProfileAsync(request);

            if (result != null)
            {
                _onSaved?.Invoke(avatarConfig);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save avatar failed: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateTo<MainComponents.MainViewModel>();
    }
}
