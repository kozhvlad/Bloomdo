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

    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private int _selectedSkinTone;
    [ObservableProperty] private int _selectedBodyType;
    [ObservableProperty] private int _selectedEyeColor;
    [ObservableProperty] private int _selectedEyeStyle;
    [ObservableProperty] private int _selectedHairColor;
    [ObservableProperty] private int _selectedHairStyle;
    [ObservableProperty] private int _selectedGlassesStyle;
    [ObservableProperty] private int _selectedGlassesColor;
    [ObservableProperty] private int _selectedFacialHair;
    [ObservableProperty] private int _selectedFacialHairColor;
    [ObservableProperty] private int _selectedHeadwearStyle;
    [ObservableProperty] private int _selectedHeadwearColor;
    [ObservableProperty] private int _selectedClothingStyle;
    [ObservableProperty] private int _selectedClothingColor;
    [ObservableProperty] private int _selectedBackgroundColor;
    [ObservableProperty] private int _selectedMouthStyle;
    [ObservableProperty] private int _selectedFaceExtra;
    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private string _previewBackgroundHex = "#7E57C2";
    [ObservableProperty] private string _previewSkinHex = "#FDDBB4";
    [ObservableProperty] private string _previewHairHex = "#2C2C2C";
    [ObservableProperty] private string _previewClothingHex = "#66BB6A";
    [ObservableProperty] private string _previewEyeHex = "#5D4037";
    [ObservableProperty] private string _previewGlassesHex = "#263238";
    [ObservableProperty] private string _previewFacialHairHex = "#2C2C2C";
    [ObservableProperty] private string _previewHeadwearHex = "#EF5350";

    public ObservableCollection<AvatarPartOption> SkinTones { get; } = [];
    public ObservableCollection<AvatarPartOption> BodyTypes { get; } = [];
    public ObservableCollection<AvatarPartOption> EyeColors { get; } = [];
    public ObservableCollection<AvatarPartOption> EyeStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> HairColors { get; } = [];
    public ObservableCollection<AvatarPartOption> HairStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> GlassesStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> GlassesColors { get; } = [];
    public ObservableCollection<AvatarPartOption> FacialHairs { get; } = [];
    public ObservableCollection<AvatarPartOption> FacialHairColors { get; } = [];
    public ObservableCollection<AvatarPartOption> HeadwearStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> HeadwearColors { get; } = [];
    public ObservableCollection<AvatarPartOption> ClothingStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> ClothingColors { get; } = [];
    public ObservableCollection<AvatarPartOption> BackgroundColors { get; } = [];
    public ObservableCollection<AvatarPartOption> MouthStyles { get; } = [];
    public ObservableCollection<AvatarPartOption> FaceExtras { get; } = [];

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
            SelectedBodyType = currentAvatar.BodyType;
            SelectedEyeColor = currentAvatar.EyeColor;
            SelectedEyeStyle = currentAvatar.EyeStyle;
            SelectedHairColor = currentAvatar.HairColor;
            SelectedHairStyle = currentAvatar.HairStyle;
            SelectedGlassesStyle = currentAvatar.GlassesStyle;
            SelectedGlassesColor = currentAvatar.GlassesColor;
            SelectedFacialHair = currentAvatar.FacialHair;
            SelectedFacialHairColor = currentAvatar.FacialHairColor;
            SelectedHeadwearStyle = currentAvatar.HeadwearStyle;
            SelectedHeadwearColor = currentAvatar.HeadwearColor;
            SelectedClothingStyle = currentAvatar.ClothingStyle;
            SelectedClothingColor = currentAvatar.ClothingColor;
            SelectedBackgroundColor = currentAvatar.BackgroundColor;
            SelectedMouthStyle = currentAvatar.MouthStyle;
            SelectedFaceExtra = currentAvatar.FaceExtra;
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
        SkinTones.Add(new AvatarPartOption { Id = 6, Label = "Deep", ColorHex = "#3A1F04" });

        BodyTypes.Add(new AvatarPartOption { Id = 0, Label = "Slim", ColorHex = "#78909C" });
        BodyTypes.Add(new AvatarPartOption { Id = 1, Label = "Average", ColorHex = "#78909C" });
        BodyTypes.Add(new AvatarPartOption { Id = 2, Label = "Athletic", ColorHex = "#78909C" });
        BodyTypes.Add(new AvatarPartOption { Id = 3, Label = "Broad", ColorHex = "#78909C" });
        BodyTypes.Add(new AvatarPartOption { Id = 4, Label = "Muscular", ColorHex = "#78909C" });

        EyeColors.Add(new AvatarPartOption { Id = 0, Label = "Brown", ColorHex = "#5D4037" });
        EyeColors.Add(new AvatarPartOption { Id = 1, Label = "Hazel", ColorHex = "#8D6E63" });
        EyeColors.Add(new AvatarPartOption { Id = 2, Label = "Green", ColorHex = "#4CAF50" });
        EyeColors.Add(new AvatarPartOption { Id = 3, Label = "Blue", ColorHex = "#42A5F5" });
        EyeColors.Add(new AvatarPartOption { Id = 4, Label = "Gray", ColorHex = "#78909C" });
        EyeColors.Add(new AvatarPartOption { Id = 5, Label = "Amber", ColorHex = "#FFA000" });
        EyeColors.Add(new AvatarPartOption { Id = 6, Label = "Violet", ColorHex = "#7E57C2" });
        EyeColors.Add(new AvatarPartOption { Id = 7, Label = "Teal", ColorHex = "#26A69A" });
        EyeColors.Add(new AvatarPartOption { Id = 8, Label = "Ice Blue", ColorHex = "#81D4FA" });
        EyeColors.Add(new AvatarPartOption { Id = 9, Label = "Emerald", ColorHex = "#2E7D32" });

        EyeStyles.Add(new AvatarPartOption { Id = 0, Label = "Normal", ColorHex = "#78909C" });
        EyeStyles.Add(new AvatarPartOption { Id = 1, Label = "Happy", ColorHex = "#78909C" });
        EyeStyles.Add(new AvatarPartOption { Id = 2, Label = "Cool", ColorHex = "#78909C" });
        EyeStyles.Add(new AvatarPartOption { Id = 3, Label = "Wink", ColorHex = "#78909C" });
        EyeStyles.Add(new AvatarPartOption { Id = 4, Label = "Surprised", ColorHex = "#78909C" });
        EyeStyles.Add(new AvatarPartOption { Id = 5, Label = "Sleepy", ColorHex = "#78909C" });

        HairColors.Add(new AvatarPartOption { Id = 0, Label = "Black", ColorHex = "#2C2C2C" });
        HairColors.Add(new AvatarPartOption { Id = 1, Label = "Dark Brown", ColorHex = "#4E342E" });
        HairColors.Add(new AvatarPartOption { Id = 2, Label = "Brown", ColorHex = "#6B4226" });
        HairColors.Add(new AvatarPartOption { Id = 3, Label = "Blonde", ColorHex = "#F5D76E" });
        HairColors.Add(new AvatarPartOption { Id = 4, Label = "Red", ColorHex = "#C0392B" });
        HairColors.Add(new AvatarPartOption { Id = 5, Label = "Ginger", ColorHex = "#E65100" });
        HairColors.Add(new AvatarPartOption { Id = 6, Label = "Blue", ColorHex = "#2980B9" });
        HairColors.Add(new AvatarPartOption { Id = 7, Label = "Purple", ColorHex = "#8E44AD" });
        HairColors.Add(new AvatarPartOption { Id = 8, Label = "Pink", ColorHex = "#EC407A" });
        HairColors.Add(new AvatarPartOption { Id = 9, Label = "Green", ColorHex = "#43A047" });
        HairColors.Add(new AvatarPartOption { Id = 10, Label = "Silver", ColorHex = "#B0BEC5" });
        HairColors.Add(new AvatarPartOption { Id = 11, Label = "Platinum", ColorHex = "#F5F5DC" });
        HairColors.Add(new AvatarPartOption { Id = 12, Label = "Teal", ColorHex = "#00897B" });

        HairStyles.Add(new AvatarPartOption { Id = 0, Label = "Short", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 1, Label = "Medium", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 2, Label = "Long", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 3, Label = "Curly", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 4, Label = "Buzz", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 5, Label = "Mohawk", ColorHex = "#78909C" });
        HairStyles.Add(new AvatarPartOption { Id = 6, Label = "Bald", ColorHex = "#78909C" });

        GlassesStyles.Add(new AvatarPartOption { Id = 0, Label = "None", ColorHex = "#B0BEC5" });
        GlassesStyles.Add(new AvatarPartOption { Id = 1, Label = "Round", ColorHex = "#78909C" });
        GlassesStyles.Add(new AvatarPartOption { Id = 2, Label = "Square", ColorHex = "#78909C" });
        GlassesStyles.Add(new AvatarPartOption { Id = 3, Label = "Aviator", ColorHex = "#78909C" });
        GlassesStyles.Add(new AvatarPartOption { Id = 4, Label = "Cat-Eye", ColorHex = "#78909C" });
        GlassesStyles.Add(new AvatarPartOption { Id = 5, Label = "Sunglasses", ColorHex = "#78909C" });

        GlassesColors.Add(new AvatarPartOption { Id = 0, Label = "Black", ColorHex = "#263238" });
        GlassesColors.Add(new AvatarPartOption { Id = 1, Label = "Brown", ColorHex = "#5D4037" });
        GlassesColors.Add(new AvatarPartOption { Id = 2, Label = "Gold", ColorHex = "#FFB300" });
        GlassesColors.Add(new AvatarPartOption { Id = 3, Label = "Silver", ColorHex = "#90A4AE" });
        GlassesColors.Add(new AvatarPartOption { Id = 4, Label = "Blue", ColorHex = "#1565C0" });
        GlassesColors.Add(new AvatarPartOption { Id = 5, Label = "Red", ColorHex = "#C62828" });
        GlassesColors.Add(new AvatarPartOption { Id = 6, Label = "Pink", ColorHex = "#EC407A" });
        GlassesColors.Add(new AvatarPartOption { Id = 7, Label = "Green", ColorHex = "#2E7D32" });
        GlassesColors.Add(new AvatarPartOption { Id = 8, Label = "Purple", ColorHex = "#7E57C2" });

        FacialHairs.Add(new AvatarPartOption { Id = 0, Label = "None", ColorHex = "#B0BEC5" });
        FacialHairs.Add(new AvatarPartOption { Id = 1, Label = "Stubble", ColorHex = "#78909C" });
        FacialHairs.Add(new AvatarPartOption { Id = 2, Label = "Goatee", ColorHex = "#78909C" });
        FacialHairs.Add(new AvatarPartOption { Id = 3, Label = "Full Beard", ColorHex = "#78909C" });
        FacialHairs.Add(new AvatarPartOption { Id = 4, Label = "Mustache", ColorHex = "#78909C" });
        FacialHairs.Add(new AvatarPartOption { Id = 5, Label = "Soul Patch", ColorHex = "#78909C" });

        FacialHairColors.Add(new AvatarPartOption { Id = 0, Label = "Black", ColorHex = "#2C2C2C" });
        FacialHairColors.Add(new AvatarPartOption { Id = 1, Label = "Dark Brown", ColorHex = "#4E342E" });
        FacialHairColors.Add(new AvatarPartOption { Id = 2, Label = "Brown", ColorHex = "#6B4226" });
        FacialHairColors.Add(new AvatarPartOption { Id = 3, Label = "Blonde", ColorHex = "#F5D76E" });
        FacialHairColors.Add(new AvatarPartOption { Id = 4, Label = "Red", ColorHex = "#C0392B" });
        FacialHairColors.Add(new AvatarPartOption { Id = 5, Label = "Ginger", ColorHex = "#E65100" });
        FacialHairColors.Add(new AvatarPartOption { Id = 6, Label = "Gray", ColorHex = "#9E9E9E" });
        FacialHairColors.Add(new AvatarPartOption { Id = 7, Label = "White", ColorHex = "#ECEFF1" });
        FacialHairColors.Add(new AvatarPartOption { Id = 8, Label = "Auburn", ColorHex = "#8D4004" });

        HeadwearStyles.Add(new AvatarPartOption { Id = 0, Label = "None", ColorHex = "#B0BEC5" });
        HeadwearStyles.Add(new AvatarPartOption { Id = 1, Label = "Cap", ColorHex = "#78909C" });
        HeadwearStyles.Add(new AvatarPartOption { Id = 2, Label = "Beanie", ColorHex = "#78909C" });
        HeadwearStyles.Add(new AvatarPartOption { Id = 3, Label = "Crown", ColorHex = "#78909C" });
        HeadwearStyles.Add(new AvatarPartOption { Id = 4, Label = "Headband", ColorHex = "#78909C" });
        HeadwearStyles.Add(new AvatarPartOption { Id = 5, Label = "Bow", ColorHex = "#78909C" });

        HeadwearColors.Add(new AvatarPartOption { Id = 0, Label = "Red", ColorHex = "#EF5350" });
        HeadwearColors.Add(new AvatarPartOption { Id = 1, Label = "Blue", ColorHex = "#42A5F5" });
        HeadwearColors.Add(new AvatarPartOption { Id = 2, Label = "Green", ColorHex = "#66BB6A" });
        HeadwearColors.Add(new AvatarPartOption { Id = 3, Label = "Purple", ColorHex = "#AB47BC" });
        HeadwearColors.Add(new AvatarPartOption { Id = 4, Label = "Orange", ColorHex = "#FFA726" });
        HeadwearColors.Add(new AvatarPartOption { Id = 5, Label = "Black", ColorHex = "#37474F" });
        HeadwearColors.Add(new AvatarPartOption { Id = 6, Label = "Pink", ColorHex = "#EC407A" });
        HeadwearColors.Add(new AvatarPartOption { Id = 7, Label = "Yellow", ColorHex = "#FDD835" });
        HeadwearColors.Add(new AvatarPartOption { Id = 8, Label = "Teal", ColorHex = "#26A69A" });
        HeadwearColors.Add(new AvatarPartOption { Id = 9, Label = "White", ColorHex = "#ECEFF1" });

        ClothingStyles.Add(new AvatarPartOption { Id = 0, Label = "T-Shirt", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 1, Label = "Hoodie", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 2, Label = "Shirt", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 3, Label = "Tank Top", ColorHex = "#78909C" });
        ClothingStyles.Add(new AvatarPartOption { Id = 4, Label = "Sweater", ColorHex = "#78909C" });

        ClothingColors.Add(new AvatarPartOption { Id = 0, Label = "Green", ColorHex = "#66BB6A" });
        ClothingColors.Add(new AvatarPartOption { Id = 1, Label = "Blue", ColorHex = "#42A5F5" });
        ClothingColors.Add(new AvatarPartOption { Id = 2, Label = "Red", ColorHex = "#EF5350" });
        ClothingColors.Add(new AvatarPartOption { Id = 3, Label = "Purple", ColorHex = "#AB47BC" });
        ClothingColors.Add(new AvatarPartOption { Id = 4, Label = "Orange", ColorHex = "#FFA726" });
        ClothingColors.Add(new AvatarPartOption { Id = 5, Label = "Black", ColorHex = "#37474F" });
        ClothingColors.Add(new AvatarPartOption { Id = 6, Label = "White", ColorHex = "#ECEFF1" });
        ClothingColors.Add(new AvatarPartOption { Id = 7, Label = "Pink", ColorHex = "#EC407A" });
        ClothingColors.Add(new AvatarPartOption { Id = 8, Label = "Teal", ColorHex = "#26A69A" });
        ClothingColors.Add(new AvatarPartOption { Id = 9, Label = "Yellow", ColorHex = "#FDD835" });
        ClothingColors.Add(new AvatarPartOption { Id = 10, Label = "Navy", ColorHex = "#1A237E" });
        ClothingColors.Add(new AvatarPartOption { Id = 11, Label = "Maroon", ColorHex = "#880E4F" });

        BackgroundColors.Add(new AvatarPartOption { Id = 0, Label = "Purple", ColorHex = "#7E57C2" });
        BackgroundColors.Add(new AvatarPartOption { Id = 1, Label = "Blue", ColorHex = "#42A5F5" });
        BackgroundColors.Add(new AvatarPartOption { Id = 2, Label = "Green", ColorHex = "#66BB6A" });
        BackgroundColors.Add(new AvatarPartOption { Id = 3, Label = "Orange", ColorHex = "#FFA726" });
        BackgroundColors.Add(new AvatarPartOption { Id = 4, Label = "Pink", ColorHex = "#EC407A" });
        BackgroundColors.Add(new AvatarPartOption { Id = 5, Label = "Teal", ColorHex = "#26A69A" });
        BackgroundColors.Add(new AvatarPartOption { Id = 6, Label = "Red", ColorHex = "#EF5350" });
        BackgroundColors.Add(new AvatarPartOption { Id = 7, Label = "Yellow", ColorHex = "#FDD835" });
        BackgroundColors.Add(new AvatarPartOption { Id = 8, Label = "Navy", ColorHex = "#283593" });
        BackgroundColors.Add(new AvatarPartOption { Id = 9, Label = "Coral", ColorHex = "#FF7043" });
        BackgroundColors.Add(new AvatarPartOption { Id = 10, Label = "Mint", ColorHex = "#80CBC4" });
        BackgroundColors.Add(new AvatarPartOption { Id = 11, Label = "Lavender", ColorHex = "#B39DDB" });

        MouthStyles.Add(new AvatarPartOption { Id = 0, Label = "Smile", ColorHex = "#78909C" });
        MouthStyles.Add(new AvatarPartOption { Id = 1, Label = "Wide Grin", ColorHex = "#78909C" });
        MouthStyles.Add(new AvatarPartOption { Id = 2, Label = "Neutral", ColorHex = "#78909C" });
        MouthStyles.Add(new AvatarPartOption { Id = 3, Label = "Sad", ColorHex = "#78909C" });
        MouthStyles.Add(new AvatarPartOption { Id = 4, Label = "Open", ColorHex = "#78909C" });
        MouthStyles.Add(new AvatarPartOption { Id = 5, Label = "Smirk", ColorHex = "#78909C" });

        FaceExtras.Add(new AvatarPartOption { Id = 0, Label = "None", ColorHex = "#B0BEC5" });
        FaceExtras.Add(new AvatarPartOption { Id = 1, Label = "Freckles", ColorHex = "#78909C" });
        FaceExtras.Add(new AvatarPartOption { Id = 2, Label = "Blush", ColorHex = "#78909C" });
        FaceExtras.Add(new AvatarPartOption { Id = 3, Label = "Eyeliner", ColorHex = "#78909C" });
        FaceExtras.Add(new AvatarPartOption { Id = 4, Label = "Makeup", ColorHex = "#78909C" });
    }

    private void UpdateSelections()
    {
        SelectInCollection(SkinTones, SelectedSkinTone);
        SelectInCollection(BodyTypes, SelectedBodyType);
        SelectInCollection(EyeColors, SelectedEyeColor);
        SelectInCollection(EyeStyles, SelectedEyeStyle);
        SelectInCollection(HairColors, SelectedHairColor);
        SelectInCollection(HairStyles, SelectedHairStyle);
        SelectInCollection(GlassesStyles, SelectedGlassesStyle);
        SelectInCollection(GlassesColors, SelectedGlassesColor);
        SelectInCollection(FacialHairs, SelectedFacialHair);
        SelectInCollection(FacialHairColors, SelectedFacialHairColor);
        SelectInCollection(HeadwearStyles, SelectedHeadwearStyle);
        SelectInCollection(HeadwearColors, SelectedHeadwearColor);
        SelectInCollection(ClothingStyles, SelectedClothingStyle);
        SelectInCollection(ClothingColors, SelectedClothingColor);
        SelectInCollection(BackgroundColors, SelectedBackgroundColor);
        SelectInCollection(MouthStyles, SelectedMouthStyle);
        SelectInCollection(FaceExtras, SelectedFaceExtra);
    }

    private void UpdatePreview()
    {
        PreviewBackgroundHex = GetColorFromCollection(BackgroundColors, SelectedBackgroundColor, "#7E57C2");
        PreviewSkinHex = GetColorFromCollection(SkinTones, SelectedSkinTone, "#FDDBB4");
        PreviewHairHex = GetColorFromCollection(HairColors, SelectedHairColor, "#2C2C2C");
        PreviewClothingHex = GetColorFromCollection(ClothingColors, SelectedClothingColor, "#66BB6A");
        PreviewEyeHex = GetColorFromCollection(EyeColors, SelectedEyeColor, "#5D4037");
        PreviewGlassesHex = GetColorFromCollection(GlassesColors, SelectedGlassesColor, "#263238");
        PreviewFacialHairHex = GetColorFromCollection(FacialHairColors, SelectedFacialHairColor, "#2C2C2C");
        PreviewHeadwearHex = GetColorFromCollection(HeadwearColors, SelectedHeadwearColor, "#EF5350");
    }

    private static string GetColorFromCollection(ObservableCollection<AvatarPartOption> collection, int id, string fallback)
    {
        var item = collection.FirstOrDefault(x => x.Id == id);
        return item?.ColorHex ?? fallback;
    }

    private static void SelectInCollection(ObservableCollection<AvatarPartOption> collection, int selectedId)
    {
        foreach (var item in collection)
            item.IsSelected = item.Id == selectedId;
    }

    [RelayCommand]
    private void SelectTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
            SelectedTab = index;
    }

    [RelayCommand]
    private void SelectSkinTone(AvatarPartOption option) { SelectedSkinTone = option.Id; SelectInCollection(SkinTones, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectBodyType(AvatarPartOption option) { SelectedBodyType = option.Id; SelectInCollection(BodyTypes, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectEyeColor(AvatarPartOption option) { SelectedEyeColor = option.Id; SelectInCollection(EyeColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectEyeStyle(AvatarPartOption option) { SelectedEyeStyle = option.Id; SelectInCollection(EyeStyles, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectHairColor(AvatarPartOption option) { SelectedHairColor = option.Id; SelectInCollection(HairColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectHairStyle(AvatarPartOption option) { SelectedHairStyle = option.Id; SelectInCollection(HairStyles, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectGlassesStyle(AvatarPartOption option) { SelectedGlassesStyle = option.Id; SelectInCollection(GlassesStyles, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectGlassesColor(AvatarPartOption option) { SelectedGlassesColor = option.Id; SelectInCollection(GlassesColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectFacialHair(AvatarPartOption option) { SelectedFacialHair = option.Id; SelectInCollection(FacialHairs, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectFacialHairColor(AvatarPartOption option) { SelectedFacialHairColor = option.Id; SelectInCollection(FacialHairColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectHeadwearStyle(AvatarPartOption option) { SelectedHeadwearStyle = option.Id; SelectInCollection(HeadwearStyles, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectHeadwearColor(AvatarPartOption option) { SelectedHeadwearColor = option.Id; SelectInCollection(HeadwearColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectClothingStyle(AvatarPartOption option) { SelectedClothingStyle = option.Id; SelectInCollection(ClothingStyles, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectClothingColor(AvatarPartOption option) { SelectedClothingColor = option.Id; SelectInCollection(ClothingColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectBackgroundColor(AvatarPartOption option) { SelectedBackgroundColor = option.Id; SelectInCollection(BackgroundColors, option.Id); UpdatePreview(); }

    [RelayCommand]
    private void SelectMouthStyle(AvatarPartOption option) { SelectedMouthStyle = option.Id; SelectInCollection(MouthStyles, option.Id); }

    [RelayCommand]
    private void SelectFaceExtra(AvatarPartOption option) { SelectedFaceExtra = option.Id; SelectInCollection(FaceExtras, option.Id); }

    public AvatarConfig BuildAvatarConfig()
    {
        return new AvatarConfig
        {
            SkinTone = SelectedSkinTone,
            BodyType = SelectedBodyType,
            EyeColor = SelectedEyeColor,
            EyeStyle = SelectedEyeStyle,
            HairColor = SelectedHairColor,
            HairStyle = SelectedHairStyle,
            GlassesStyle = SelectedGlassesStyle,
            GlassesColor = SelectedGlassesColor,
            FacialHair = SelectedFacialHair,
            FacialHairColor = SelectedFacialHairColor,
            HeadwearStyle = SelectedHeadwearStyle,
            HeadwearColor = SelectedHeadwearColor,
            ClothingStyle = SelectedClothingStyle,
            ClothingColor = SelectedClothingColor,
            BackgroundColor = SelectedBackgroundColor,
            MouthStyle = SelectedMouthStyle,
            FaceExtra = SelectedFaceExtra
        };
    }

    public string GetBackgroundColorHex() => GetColorFromCollection(BackgroundColors, SelectedBackgroundColor, "#7E57C2");
    public string GetSkinColorHex() => GetColorFromCollection(SkinTones, SelectedSkinTone, "#FDDBB4");
    public string GetHairColorHex() => GetColorFromCollection(HairColors, SelectedHairColor, "#2C2C2C");
    public string GetClothingColorHex() => GetColorFromCollection(ClothingColors, SelectedClothingColor, "#66BB6A");
    public string GetEyeColorHex() => GetColorFromCollection(EyeColors, SelectedEyeColor, "#5D4037");

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
        _navigationService.NavigateBack();
    }
}
