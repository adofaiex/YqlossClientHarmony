using System;
using System.Collections.Generic;

namespace YqlossClientHarmony.Features.ModifyLoadingLevel;

public class EventType(
    string? name,
    Func<SettingsModifyLoadingLevel, bool> settingSelector,
    Func<Dictionary<string, object?>, bool>? filter = null
)
{
    public static EventType[] Types { get; } =
    [
        new(
            "AddDecoration",
            s => s.DisableKillerDecorations,
            o => o.GetValueOrDefault("hitbox", null) is "Kill"
        ),
        new("AddDecoration",
            s => s.DisableOtherDecorations,
            o => o.GetValueOrDefault("hitbox", null) is not "Kill"
        ),
        new("AddText", s => s.DisableAddText),
        new(
            "AddObject",
            s => s.DisableAddObjectFloor,
            o => o.GetValueOrDefault("objectType", null) is not "Planet"
        ),
        new(
            "AddObject",
            s => s.DisableAddObjectPlanet,
            o => o.GetValueOrDefault("objectType", null) is "Planet"
        ),
        new("AddParticle", s => s.DisableAddParticle),
        new("SetHitsound", s => s.DisableSetHitsound),
        new("PlaySound", s => s.DisablePlaySound),
        new("SetPlanetRotation", s => s.DisableSetPlanetRotation),
        new("ScalePlanets", s => s.DisableScalePlanets),
        new("ColorTrack", s => s.DisableColorTrack),
        new("AnimateTrack", s => s.DisableAnimateTrack),
        new("RecolorTrack", s => s.DisableRecolorTrack),
        new("MoveTrack", s => s.DisableMoveTrack),
        new("PositionTrack", s => s.DisablePositionTrack),
        new("MoveDecorations", s => s.DisableMoveDecorations),
        new("SetText", s => s.DisableSetText),
        new("EmitParticle", s => s.DisableEmitParticle),
        new("SetParticle", s => s.DisableSetParticle),
        new("SetObject", s => s.DisableSetObject),
        new("SetDefaultText", s => s.DisableSetDefaultText),
        new("CustomBackground", s => s.DisableCustomBackground),
        new("Flash", s => s.DisableFlash),
        new("MoveCamera", s => s.DisableMoveCamera),
        new("SetFilter", s => s.DisableSetFilter),
        new("SetFilterAdvanced", s => s.DisableSetFilterAdvanced),
        new("HallOfMirrors", s => s.DisableHallOfMirrors),
        new("ShakeScreen", s => s.DisableShakeScreen),
        new("Bloom", s => s.DisableBloom),
        new("ScreenTile", s => s.DisableScreenTile),
        new("ScreenScroll", s => s.DisableScreenScroll),
        new("SetFrameRate", s => s.DisableSetFrameRate),
        new("EditorComment", s => s.DisableEditorComment),
        new("Bookmark", s => s.DisableBookmark),
        new("SetHoldSound", s => s.DisableSetHoldSound),
        new("Hide", s => s.DisableHide),
        new("ScaleRadius", s => s.DisableScaleRadius),
        new("Checkpoint", s => s.DisableCheckpoint)
    ];

    private bool MatchesName(Dictionary<string, object?> actionOrDecoration)
    {
        if (name is null) return true;

        var eventTypeObject = actionOrDecoration.GetValueOrDefault("eventType", null);
        if (eventTypeObject is not string eventType) return false;

        return name == eventType;
    }

    private bool MatchesFilter(Dictionary<string, object?> actionOrDecoration)
    {
        return filter is null || filter(actionOrDecoration);
    }

    public bool Matches(Dictionary<string, object?> actionOrDecoration)
    {
        return settingSelector(SettingsModifyLoadingLevel.Instance) && MatchesName(actionOrDecoration) &&
               MatchesFilter(actionOrDecoration);
    }
}