using System.Linq;
using JetBrains.Annotations;
using YqlossClientHarmony.Utilities;

namespace YqlossClientHarmony.Features.ModifyLoadingLevel;

// this is the data structure for each single profile now
// the name is preserved for backward compatibility
[NoReorder]
public class SettingsModifyLoadingLevel
{
    public static SettingsModifyLoadingLevel? GetProfile(string name)
    {
        return name == ""
            ? (SettingsModifyLoadingLevel?)Main.Settings.ModifyLoadingLevelSettings
            : Main.Settings.ModifyLoadingLevelProfiles.FirstOrDefault(it => it.Id == name);
    }

    public static SettingsModifyLoadingLevel GetCurrentProfile()
    {
        var current = Main.Settings.SelectedModifyLoadingLevelProfile;
        var profile = GetProfile(current);
        if (profile is not null) return profile;

        Main.Mod.Logger.Warning(
            $"current ModifyLoadingLevel profile \"{current}\" does not exist, defaulting to the default profile");
        Main.Settings.SelectedModifyLoadingLevelProfile = "";
        Main.Settings.Save(Main.Mod);
        return Main.Settings.ModifyLoadingLevelSettings;
    }

    public static string? RenameCurrentProfile(string newName)
    {
        var current = Main.Settings.SelectedModifyLoadingLevelProfile;
        if (current == "") return I18N.Translate("Gui.EffectRemover.Error.CannotRenameDefaultProfile");

        var profile = GetProfile(current);
        if (profile is null) throw new WTFException("profile is null???");

        if (GetProfile(newName) is not null)
            return I18N.Translate("Gui.EffectRemover.Error.CannotRenameAsExistingName", newName);

        profile.Id = newName;
        Main.Settings.SelectedModifyLoadingLevelProfile = newName;
        Main.Settings.Save(Main.Mod);
        return null;
    }

    public static string? DeleteCurrentProfile()
    {
        var current = Main.Settings.SelectedModifyLoadingLevelProfile;
        if (current == "") return I18N.Translate("Gui.EffectRemover.Error.CannotDeleteDefaultProfile");

        Main.Settings.SelectedModifyLoadingLevelProfile = "";
        Main.Settings.ModifyLoadingLevelProfiles.RemoveAll(it => it.Id == current);
        Main.Settings.Save(Main.Mod);
        return null;
    }

    public static string? DuplicateCurrentProfile()
    {
        var profile = GetProfile(Main.Settings.SelectedModifyLoadingLevelProfile);
        if (profile is null) throw new WTFException("profile is null???");

        var newProfileId = GetAvailableProfileName(profile.Name);
        var newProfile = new SettingsModifyLoadingLevel();
        Reflections.CopyFields(newProfile, profile);
        newProfile.Id = newProfileId;
        Main.Settings.ModifyLoadingLevelProfiles.Add(newProfile);
        Main.Settings.Save(Main.Mod);
        return null;
    }

    public static string GetAvailableProfileName(string name, bool checkOriginal = false)
    {
        if (checkOriginal && GetProfile(name) is null) return name;

        for (var i = 2; i > 0; ++i)
        {
            // I don't want to optimize this
            var profileName = $"{name} ({i})";
            if (GetProfile(profileName) is null) return profileName;
        }

        throw new WTFException("don't create 2147483646 profiles. please.");
    }

    private static Trigger<string, SettingsModifyLoadingLevel> InstanceTrigger { get; } = new(_ => GetCurrentProfile());

    public static SettingsModifyLoadingLevel Instance =>
        InstanceTrigger.Get(Main.Settings.SelectedModifyLoadingLevelProfile);

    public bool Enabled => Main.Enabled && Main.Settings.EnableModifyLoadingLevel;

    public string Id = "";

    public string Name => Id == "" ? I18N.Translate("Gui.EffectRemover.DefaultProfileName") : Id;

    public bool EnableHitsound = false;

    public string Hitsound = "Kick";

    public bool EnableHitsoundVolume = false;

    public int HitsoundVolume = 100;

    public bool DisableTrackTexture = false;

    public bool EnableTrackColorType = false;

    public string TrackColorType = "Single";

    public bool EnableTrackStyle = false;

    public string TrackStyle = "Standard";

    public bool EnableTrackColor = false;

    public string TrackColor = "DEBB7B";

    public bool EnableSecondaryTrackColor = false;

    public string SecondaryTrackColor = "FFFFFF";

    public bool EnableColorAnimDuration = false;

    public double ColorAnimDuration = 2;

    public bool EnableTrackGlowIntensity = false;

    public double TrackGlowIntensity = 100;

    public bool EnableTrackAnimation = false;

    public string TrackAnimation = "None";

    public bool EnableBeatsAhead = false;

    public double BeatsAhead = 3;

    public bool EnableTrackDisappearAnimation = false;

    public string TrackDisappearAnimation = "None";

    public bool EnableBeatsBehind = false;

    public double BeatsBehind = 4;

    public bool DisableBgImage = false;

    public bool EnableBackgroundColor = false;

    public string BackgroundColor = "000000";

    public bool EnableShowDefaultBgTile = false;

    public bool ShowDefaultBgTile = true;

    public bool EnableDefaultBgTileColor = false;

    public string DefaultBgTileColor = "101121";

    public bool EnableDefaultBgShapeType = false;

    public string DefaultBgShapeType = "Default";

    public bool EnableDefaultBgShapeColor = false;

    public string DefaultBgShapeColor = "FFFFFF";

    public bool EnableRelativeTo = false;

    public string RelativeTo = "Player";

    public bool EnablePosition = false;

    public double PositionX = 0.0;

    public double PositionY = 0.0;

    public bool EnableRotation = false;

    public double Rotation = 0.0;

    public bool EnableZoom = false;

    public double Zoom = 100.0;

    public bool EnablePulseOnFloor = false;

    public bool PulseOnFloor = true;

    public bool DisableBgVideo = false;

    public bool EnableFloorIconOutlines = false;

    public bool FloorIconOutlines = false;

    public bool EnableStickToFloors = false;

    public bool StickToFloors = true;

    public bool EnablePlanetEase = false;

    public string PlanetEase = "Linear";

    public bool EnablePlanetEaseParts = false;

    public int PlanetEaseParts = 1;

    public bool EnablePlanetEasePartBehavior = false;

    public string PlanetEasePartBehavior = "Mirror";

    public bool EnableDefaultTextColor = false;

    public string DefaultTextColor = "FFFFFF";

    public bool EnableDefaultTextShadowColor = false;

    public string DefaultTextShadowColor = "00000050";

    public bool EnableCongratsText = false;

    public string CongratsText = "";

    public bool EnablePerfectText = false;

    public string PerfectText = "";

    public bool DisableKillerDecorations = false;

    public bool DisableOtherDecorations = false;

    public bool DisableAddText = false;

    public bool DisableAddObjectFloor = false;

    public bool DisableAddObjectPlanet = false;

    public bool DisableAddParticle = false;

    public bool DisableSetHitsound = false;

    public bool DisablePlaySound = false;

    public bool DisableSetHoldSound = false;

    public bool DisableSetPlanetRotation = false;

    public bool DisableScalePlanets = false;

    public bool DisableScaleRadius = false;

    public bool DisableMoveCamera = false;

    public bool DisableCustomBackground = false;

    public bool DisableHide = false;

    public bool DisableMoveTrack = false;

    public bool DisablePositionTrack = false;

    public bool DisableColorTrack = false;

    public bool DisableAnimateTrack = false;

    public bool DisableRecolorTrack = false;

    public bool DisableMoveDecorations = false;

    public bool DisableSetText = false;

    public bool DisableEmitParticle = false;

    public bool DisableSetParticle = false;

    public bool DisableSetObject = false;

    public bool DisableSetDefaultText = false;

    public bool DisableFlash = false;

    public bool DisableSetFilter = false;

    public bool DisableSetFilterAdvanced = false;

    public bool DisableHallOfMirrors = false;

    public bool DisableShakeScreen = false;

    public bool DisableBloom = false;

    public bool DisableScreenTile = false;

    public bool DisableScreenScroll = false;

    public bool DisableSetFrameRate = false;

    public bool DisableEditorComment = false;

    public bool DisableBookmark = false;

    public bool DisableCheckpoint = false;
}