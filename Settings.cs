using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityModManagerNet;
using YqlossClientHarmony.Features.BlockUnintentionalEscape;
using YqlossClientHarmony.Features.LimitRecolorTrack;
using YqlossClientHarmony.Features.ModifyLoadingLevel;
using YqlossClientHarmony.Features.PlaySoundOnGameEnd;
using YqlossClientHarmony.Features.Replay;

namespace YqlossClientHarmony;

[NoReorder]
public class Settings : UnityModManager.ModSettings
{
    public string Language = I18N.DefaultLanguage;

    public bool EnableFixKillerDecorationsInNoFail = false;

    public bool EnableModifyLoadingLevel = false;

    public string SelectedModifyLoadingLevelProfile = "";

    // the default profile stored apart from other profiles
    // the name is preserved for backward compatibility
    public SettingsModifyLoadingLevel ModifyLoadingLevelSettings = new();

    public bool HasAddedPresetProfile;

    // all other profiles than the default one
    public List<SettingsModifyLoadingLevel> ModifyLoadingLevelProfiles = [];

    public bool EnableReplay = false;

    public SettingsReplay ReplaySettings = new();

    public bool EnableBlockUnintentionalEscape = false;

    public SettingsBlockUnintentionalEscape BlockUnintentionalEscapeSettings = new();

    public bool EnablePlaySoundOnGameEnd = false;

    public SettingsPlaySoundOnGameEnd PlaySoundOnGameEndSettings = new();

    public bool EnableLimitRecolorTrack = false;

    public SettingsLimitRecolorTrack LimitRecolorTrackSettings = new();

    public void OnLoad(UnityModManager.ModEntry modEntry)
    {
        if (!HasAddedPresetProfile)
        {
            ModifyLoadingLevelProfiles.Add(
                new SettingsModifyLoadingLevel
                {
                    Id = "YCH",
                    DisableBgImage = true,
                    EnableBackgroundColor = true,
                    BackgroundColor = "222222",
                    EnableShowDefaultBgTile = true,
                    ShowDefaultBgTile = false,
                    EnableDefaultBgShapeType = true,
                    DefaultBgShapeType = "Disabled",
                    EnableRelativeTo = true,
                    EnablePosition = true,
                    EnableRotation = true,
                    EnableZoom = true,
                    Zoom = 400,
                    EnablePulseOnFloor = true,
                    DisableKillerDecorations = true,
                    DisableOtherDecorations = true,
                    DisableAddText = true,
                    DisableAddParticle = true,
                    DisableHide = true,
                    DisableRecolorTrack = true,
                    DisableMoveDecorations = true,
                    DisableSetText = true,
                    DisableEmitParticle = true,
                    DisableSetParticle = true,
                    DisableSetObject = true,
                    DisableSetDefaultText = true,
                    DisableMoveCamera = true,
                    DisableCustomBackground = true,
                    DisableFlash = true,
                    DisableSetFilter = true,
                    DisableSetFilterAdvanced = true,
                    DisableHallOfMirrors = true,
                    DisableShakeScreen = true,
                    DisableBloom = true,
                    DisableScreenTile = true,
                    DisableScreenScroll = true,
                    DisableSetFrameRate = true,
                    DisableEditorComment = true,
                    DisableBookmark = true
                }
            );
            HasAddedPresetProfile = true;
            Save(modEntry);
        }
    }

    public void OnChange()
    {
        OnSettingChange?.Invoke();
    }

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public event Action? OnSettingChange;
}