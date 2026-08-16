using System.Collections.Generic;
using System.Linq;
using YqlossClientHarmony.Features.ModifyLoadingLevel;
using static YqlossClientHarmony.Gui.YCHLayout;
using static YqlossClientHarmony.Gui.YCHLayoutPreset;
using static YqlossClientHarmony.Utilities.SettingUtil;

namespace YqlossClientHarmony.Gui.Pages;

public static class EffectRemoverPage
{
    private static bool _expandLevel;
    private static bool _expandLevelSong;
    private static bool _expandLevelTrack;
    private static bool _expandLevelBackground;
    private static bool _expandLevelCamera;
    private static bool _expandLevelMisc;
    private static bool _expandDecorations;
    private static bool _expandEvents;
    private static bool _expandEventAuditory;
    private static bool _expandEventPlanet;
    private static bool _expandEventTrack;
    private static bool _expandEventDecoration;
    private static bool _expandEventMiscVisual;
    private static bool _expandEventEffect;
    private static bool _expandEventMisc;

    private static string _newName = "";

    private static string? LastSelectedProfile;

    private static bool ShowRename { get; set; }

    private static string? LastError { get; set; }

    private static SizesGroup.Holder Group { get; } = new();

    private static IReadOnlyList<(string, string)> ProfileEntries => Main.Settings.ModifyLoadingLevelProfiles
        .Select(it => (it.Id, it.Name))
        .Prepend(("", Main.Settings.ModifyLoadingLevelSettings.Name))
        .ToList();

    public static void Draw()
    {
        var settings = SettingsModifyLoadingLevel.Instance;
        var group = Group.Begin();

        Begin(ContainerDirection.Vertical);
        {
            Text(I18N.Translate("Page.EffectRemover.Name"), TextStyle.Title);
            Separator();
            SwitchOption(group, ref Main.Settings.EnableModifyLoadingLevel, "Setting.ModifyLoadingLevel.Enabled");
            Separator();
            Text(I18N.Translate("Gui.EffectRemover.SelectProfile"), TextStyle.Subtitle);

            var groupProfileActions = group.Group;
            Begin(ContainerDirection.Horizontal, sizes: groupProfileActions, options: WidthMin);
            PushAlign(0.5);
            {
                if (Button(I18N.Translate("Gui.EffectRemover.SelectProfile.DuplicateProfile"), options: WidthMin))
                    LastError = SettingsModifyLoadingLevel.DuplicateCurrentProfile();
                if (Button(I18N.Translate("Gui.EffectRemover.SelectProfile.DeleteProfile"), options: WidthMin))
                    LastError = SettingsModifyLoadingLevel.DeleteCurrentProfile();

                if (
                    Main.Settings.SelectedModifyLoadingLevelProfile != "" &&
                    Button(I18N.Translate(ShowRename ? "Gui.EffectRemover.SelectProfile.HideRename" : "Gui.EffectRemover.SelectProfile.ShowRename"), options: WidthMin)
                )
                {
                    ShowRename = !ShowRename;
                    _newName = SettingsModifyLoadingLevel.Instance.Name;
                }
            }
            PopAlign();
            End();

            if ((LastSelectedProfile, LastSelectedProfile = Main.Settings.SelectedModifyLoadingLevelProfile).Item1 != LastSelectedProfile)
            {
                ShowRename = false;
                _newName = SettingsModifyLoadingLevel.Instance.Name;
            }

            var groupRename = group.Group;
            if (Main.Settings.SelectedModifyLoadingLevelProfile != "" && ShowRename)
            {
                Begin(ContainerDirection.Horizontal, sizes: groupRename, options: WidthMin);
                PushAlign(0.5);
                {
                    TextField(ref _newName, options: WidthMin);
                    if (Button(I18N.Translate("Gui.EffectRemover.SelectProfile.RenameProfile"), options: WidthMin))
                        if ((LastError = SettingsModifyLoadingLevel.RenameCurrentProfile(_newName)) is null)
                            ShowRename = false;
                }
                PopAlign();
                End();
            }

            var errorGroup = group.Group;
            if (LastError is not null)
                if (IconText(errorGroup, IconStyle.Error, LastError))
                    LastError = null;

            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                Begin(ContainerDirection.Vertical, options: WidthMin);
                {
                    Save |= Selector(ref Main.Settings.SelectedModifyLoadingLevelProfile, ProfileEntries, options: WidthMin);
                }
                End();
            }
            End();

            Separator();
            IconText(group, IconStyle.Warning, "Gui.EffectRemover.LevelSettingsValueWarning");
            Separator();

            var groupLevel = group.Group;
            if (Collapse(group, ref _expandLevel, "Gui.EffectRemover.LevelSettings", TextStyle.Subtitle))
            {
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    var groupSong = groupLevel.Group;
                    if (Collapse(groupSong, ref _expandLevelSong, "Gui.EffectRemover.LevelSettings.SongSettings", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            CheckboxTextOption(groupSong, ref settings.EnableHitsound, ref settings.Hitsound, "Setting.ModifyLoadingLevel.Hitsound");
                            Separator();
                            CheckboxIntOption(groupSong, ref settings.EnableHitsoundVolume, ref settings.HitsoundVolume, "Setting.ModifyLoadingLevel.HitsoundVolume");
                        }
                        End();
                    }

                    var groupTrack = groupLevel.Group;
                    if (Collapse(groupTrack, ref _expandLevelTrack, "Gui.EffectRemover.LevelSettings.TrackSettings", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupTrack, ref settings.DisableTrackTexture, "Setting.ModifyLoadingLevel.TrackTexture");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableTrackColorType, ref settings.TrackColorType, "Setting.ModifyLoadingLevel.TrackColorType");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableTrackStyle, ref settings.TrackStyle, "Setting.ModifyLoadingLevel.TrackStyle");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableTrackColor, ref settings.TrackColor, "Setting.ModifyLoadingLevel.TrackColor");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableSecondaryTrackColor, ref settings.SecondaryTrackColor, "Setting.ModifyLoadingLevel.SecondaryTrackColor");
                            Separator();
                            CheckboxDoubleOption(groupTrack, ref settings.EnableColorAnimDuration, ref settings.ColorAnimDuration, "Setting.ModifyLoadingLevel.ColorAnimDuration");
                            Separator();
                            CheckboxDoubleOption(groupTrack, ref settings.EnableTrackGlowIntensity, ref settings.TrackGlowIntensity, "Setting.ModifyLoadingLevel.TrackGlowIntensity");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableTrackAnimation, ref settings.TrackAnimation, "Setting.ModifyLoadingLevel.TrackAnimation");
                            Separator();
                            CheckboxDoubleOption(groupTrack, ref settings.EnableBeatsAhead, ref settings.BeatsAhead, "Setting.ModifyLoadingLevel.BeatsAhead");
                            Separator();
                            CheckboxTextOption(groupTrack, ref settings.EnableTrackDisappearAnimation, ref settings.TrackDisappearAnimation, "Setting.ModifyLoadingLevel.TrackDisappearAnimation");
                            Separator();
                            CheckboxDoubleOption(groupTrack, ref settings.EnableBeatsBehind, ref settings.BeatsBehind, "Setting.ModifyLoadingLevel.BeatsBehind");
                        }
                        End();
                    }

                    var groupBackground = groupLevel.Group;
                    if (Collapse(groupBackground, ref _expandLevelBackground, "Gui.EffectRemover.LevelSettings.BackgroundSettings", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupBackground, ref settings.DisableBgImage, "Setting.ModifyLoadingLevel.BgImage");
                            Separator();
                            CheckboxTextOption(groupBackground, ref settings.EnableBackgroundColor, ref settings.BackgroundColor, "Setting.ModifyLoadingLevel.BackgroundColor");
                            Separator();
                            CheckboxSwitchOption(groupBackground, ref settings.EnableShowDefaultBgTile, ref settings.ShowDefaultBgTile, "Setting.ModifyLoadingLevel.ShowDefaultBgTile");
                            Separator();
                            CheckboxTextOption(groupBackground, ref settings.EnableDefaultBgTileColor, ref settings.DefaultBgTileColor, "Setting.ModifyLoadingLevel.DefaultBgTileColor");
                            Separator();
                            CheckboxTextOption(groupBackground, ref settings.EnableDefaultBgShapeType, ref settings.DefaultBgShapeType, "Setting.ModifyLoadingLevel.DefaultBgShapeType");
                            Separator();
                            CheckboxTextOption(groupBackground, ref settings.EnableDefaultBgShapeColor, ref settings.DefaultBgShapeColor, "Setting.ModifyLoadingLevel.DefaultBgShapeColor");
                        }
                        End();
                    }

                    var groupCamera = groupLevel.Group;
                    if (Collapse(groupCamera, ref _expandLevelCamera, "Gui.EffectRemover.LevelSettings.CameraSettings", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            CheckboxTextOption(groupCamera, ref settings.EnableRelativeTo, ref settings.RelativeTo, "Setting.ModifyLoadingLevel.RelativeTo");
                            Separator();
                            Begin(ContainerDirection.Horizontal, sizes: groupCamera, options: WidthMax);
                            PushAlign(0.5);
                            {
                                Save |= Checkbox(ref settings.EnablePosition);
                                OptionNameDescription("Setting.ModifyLoadingLevel.Position", false);
                                Fill();
                                Begin(ContainerDirection.Vertical, options: WidthMin);
                                {
                                    Begin(ContainerDirection.Horizontal, sizes: groupCamera, options: WidthMax);
                                    {
                                        Text(I18N.Translate("Setting.ModifyLoadingLevel.Position.X"), options: WidthMin);
                                        Save |= StructField(ref settings.PositionX, DoubleFormat(), WidthMin);
                                    }
                                    End();
                                    Begin(ContainerDirection.Horizontal, sizes: groupCamera, options: WidthMax);
                                    {
                                        Text(I18N.Translate("Setting.ModifyLoadingLevel.Position.Y"), options: WidthMin);
                                        Save |= StructField(ref settings.PositionY, DoubleFormat(), WidthMin);
                                    }
                                    End();
                                }
                                End();
                            }
                            PopAlign();
                            End();
                            Separator();
                            CheckboxDoubleOption(groupCamera, ref settings.EnableRotation, ref settings.Rotation, "Setting.ModifyLoadingLevel.Rotation");
                            Separator();
                            CheckboxDoubleOption(groupCamera, ref settings.EnableZoom, ref settings.Zoom, "Setting.ModifyLoadingLevel.Zoom");
                            Separator();
                            CheckboxSwitchOption(groupCamera, ref settings.EnablePulseOnFloor, ref settings.PulseOnFloor, "Setting.ModifyLoadingLevel.PulseOnFloor");
                        }
                        End();
                    }


                    var groupMisc = groupLevel.Group;
                    if (Collapse(groupMisc, ref _expandLevelMisc, "Gui.EffectRemover.LevelSettings.MiscSettings", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupMisc, ref settings.DisableBgVideo, "Setting.ModifyLoadingLevel.BgVideo");
                            Separator();
                            CheckboxSwitchOption(groupMisc, ref settings.EnableFloorIconOutlines, ref settings.FloorIconOutlines, "Setting.ModifyLoadingLevel.FloorIconOutlines");
                            Separator();
                            CheckboxSwitchOption(groupMisc, ref settings.EnableStickToFloors, ref settings.StickToFloors, "Setting.ModifyLoadingLevel.StickToFloors");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnablePlanetEase, ref settings.PlanetEase, "Setting.ModifyLoadingLevel.PlanetEase");
                            Separator();
                            CheckboxIntOption(groupMisc, ref settings.EnablePlanetEaseParts, ref settings.PlanetEaseParts, "Setting.ModifyLoadingLevel.PlanetEaseParts");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnablePlanetEasePartBehavior, ref settings.PlanetEasePartBehavior, "Setting.ModifyLoadingLevel.PlanetEasePartBehavior");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnableDefaultTextColor, ref settings.DefaultTextColor, "Setting.ModifyLoadingLevel.DefaultTextColor");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnableDefaultTextShadowColor, ref settings.DefaultTextShadowColor, "Setting.ModifyLoadingLevel.DefaultTextShadowColor");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnableCongratsText, ref settings.CongratsText, "Setting.ModifyLoadingLevel.CongratsText");
                            Separator();
                            CheckboxTextOption(groupMisc, ref settings.EnablePerfectText, ref settings.PerfectText, "Setting.ModifyLoadingLevel.PerfectText");
                        }
                        End();
                    }
                }
                End();
            }

            var groupDecorations = group.Group;
            if (Collapse(groupDecorations, ref _expandDecorations, "Gui.EffectRemover.Decorations", TextStyle.Subtitle))
            {
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    SwitchOption(groupDecorations, ref settings.DisableKillerDecorations, "Setting.ModifyLoadingLevel.KillerDecorations");
                    Separator();
                    SwitchOption(groupDecorations, ref settings.DisableOtherDecorations, "Setting.ModifyLoadingLevel.OtherDecorations");
                    Separator();
                    SwitchOption(groupDecorations, ref settings.DisableAddText, "Setting.ModifyLoadingLevel.AddText");
                    Separator();
                    SwitchOption(groupDecorations, ref settings.DisableAddObjectFloor, "Setting.ModifyLoadingLevel.AddObjectFloor");
                    Separator();
                    SwitchOption(groupDecorations, ref settings.DisableAddObjectPlanet, "Setting.ModifyLoadingLevel.AddObjectPlanet");
                    Separator();
                    SwitchOption(groupDecorations, ref settings.DisableAddParticle, "Setting.ModifyLoadingLevel.AddParticle");
                }
                End();
            }

            var groupEvents = group.Group;
            if (Collapse(groupEvents, ref _expandEvents, "Gui.EffectRemover.Events", TextStyle.Subtitle))
            {
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    var groupEventAuditory = groupEvents.Group;
                    if (Collapse(groupEventAuditory, ref _expandEventAuditory, "Gui.EffectRemover.AuditoryEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventAuditory, ref settings.DisableSetHitsound, "Setting.ModifyLoadingLevel.SetHitsound");
                            Separator();
                            SwitchOption(groupEventAuditory, ref settings.DisablePlaySound, "Setting.ModifyLoadingLevel.PlaySound");
                            Separator();
                            SwitchOption(groupEventAuditory, ref settings.DisableSetHoldSound, "Setting.ModifyLoadingLevel.SetHoldSound");
                        }
                        End();
                    }

                    var groupEventPlanet = groupEvents.Group;
                    if (Collapse(groupEventPlanet, ref _expandEventPlanet, "Gui.EffectRemover.PlanetEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventPlanet, ref settings.DisableSetPlanetRotation, "Setting.ModifyLoadingLevel.SetPlanetRotation");
                            Separator();
                            SwitchOption(groupEventPlanet, ref settings.DisableScalePlanets, "Setting.ModifyLoadingLevel.ScalePlanets");
                            Separator();
                            SwitchOption(groupEventPlanet, ref settings.DisableScaleRadius, "Setting.ModifyLoadingLevel.ScaleRadius");
                        }
                        End();
                    }

                    var groupEventTrack = groupEvents.Group;
                    if (Collapse(groupEventTrack, ref _expandEventTrack, "Gui.EffectRemover.TrackEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventTrack, ref settings.DisableHide, "Setting.ModifyLoadingLevel.Hide");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisableMoveTrack, "Setting.ModifyLoadingLevel.MoveTrack");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisablePositionTrack, "Setting.ModifyLoadingLevel.PositionTrack");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisableColorTrack, "Setting.ModifyLoadingLevel.ColorTrack");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisableAnimateTrack, "Setting.ModifyLoadingLevel.AnimateTrack");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisableRecolorTrack, "Setting.ModifyLoadingLevel.RecolorTrack");
                            Separator();
                            SwitchOption(groupEventTrack, ref settings.DisableCheckpoint, "Setting.ModifyLoadingLevel.Checkpoint");
                        }
                        End();
                    }

                    var groupEventDecoration = groupEvents.Group;
                    if (Collapse(groupEventDecoration, ref _expandEventDecoration, "Gui.EffectRemover.DecorationEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventDecoration, ref settings.DisableMoveDecorations, "Setting.ModifyLoadingLevel.MoveDecorations");
                            Separator();
                            SwitchOption(groupEventDecoration, ref settings.DisableSetText, "Setting.ModifyLoadingLevel.SetText");
                            Separator();
                            SwitchOption(groupEventDecoration, ref settings.DisableEmitParticle, "Setting.ModifyLoadingLevel.EmitParticle");
                            Separator();
                            SwitchOption(groupEventDecoration, ref settings.DisableSetParticle, "Setting.ModifyLoadingLevel.SetParticle");
                            Separator();
                            SwitchOption(groupEventDecoration, ref settings.DisableSetObject, "Setting.ModifyLoadingLevel.SetObject");
                            Separator();
                            SwitchOption(groupEventDecoration, ref settings.DisableSetDefaultText, "Setting.ModifyLoadingLevel.SetDefaultText");
                        }
                        End();
                    }

                    var groupEventMiscVisual = groupEvents.Group;
                    if (Collapse(groupEventMiscVisual, ref _expandEventMiscVisual, "Gui.EffectRemover.MiscVisualEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventMiscVisual, ref settings.DisableMoveCamera, "Setting.ModifyLoadingLevel.MoveCamera");
                            Separator();
                            SwitchOption(groupEventMiscVisual, ref settings.DisableCustomBackground, "Setting.ModifyLoadingLevel.CustomBackground");
                        }
                        End();
                    }

                    var groupEventEffect = groupEvents.Group;
                    if (Collapse(groupEventEffect, ref _expandEventEffect, "Gui.EffectRemover.EffectEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventEffect, ref settings.DisableFlash, "Setting.ModifyLoadingLevel.Flash");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableSetFilter, "Setting.ModifyLoadingLevel.SetFilter");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableSetFilterAdvanced, "Setting.ModifyLoadingLevel.SetFilterAdvanced");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableHallOfMirrors, "Setting.ModifyLoadingLevel.HallOfMirrors");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableShakeScreen, "Setting.ModifyLoadingLevel.ShakeScreen");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableBloom, "Setting.ModifyLoadingLevel.Bloom");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableScreenTile, "Setting.ModifyLoadingLevel.ScreenTile");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableScreenScroll, "Setting.ModifyLoadingLevel.ScreenScroll");
                            Separator();
                            SwitchOption(groupEventEffect, ref settings.DisableSetFrameRate, "Setting.ModifyLoadingLevel.SetFrameRate");
                        }
                        End();
                    }

                    var groupEventMisc = groupEvents.Group;
                    if (Collapse(groupEventMisc, ref _expandEventMisc, "Gui.EffectRemover.MiscEvents", TextStyle.Subtitle))
                    {
                        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                        {
                            SwitchOption(groupEventMisc, ref settings.DisableEditorComment, "Setting.ModifyLoadingLevel.EditorComment");
                            Separator();
                            SwitchOption(groupEventMisc, ref settings.DisableBookmark, "Setting.ModifyLoadingLevel.Bookmark");
                        }
                        End();
                    }
                }
                End();
            }
        }
        End();
    }
}