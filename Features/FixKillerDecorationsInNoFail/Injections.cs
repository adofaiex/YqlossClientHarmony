using System.Reflection;
using HarmonyLib;

namespace YqlossClientHarmony.Features.FixKillerDecorationsInNoFail;

public static class Injections
{
    // 3.4.0 adds a mandatory HitMargin parameter to scrHitErrorMeter.AddHit,
    // so the right overload is picked at runtime
    private static readonly MethodInfo AddHit = (
        AccessTools.Method(
            typeof(scrHitErrorMeter),
            nameof(scrHitErrorMeter.AddHit),
            [typeof(float), typeof(HitMargin), typeof(float), typeof(scrPlanet), typeof(scrFloor)]
        ) ?? AccessTools.Method(
            typeof(scrHitErrorMeter),
            nameof(scrHitErrorMeter.AddHit),
            [typeof(float), typeof(float), typeof(scrPlanet), typeof(scrFloor)]
        )
    )!;

    private static readonly bool AddHitHasHitMargin = AddHit.GetParameters().Length == 5;

    private static void AddErrorMeterHit(scrHitErrorMeter errorMeter, scrPlanet? planet)
    {
        object?[] arguments = AddHitHasHitMargin
            ? [float.NegativeInfinity, HitMargin.FailOverload, 1F, planet, null]
            : [float.NegativeInfinity, 1F, planet, null];
        AddHit.Invoke(errorMeter, arguments);
    }

    [HarmonyPatch(typeof(scrDecoration), nameof(scrDecoration.HitboxTriggerAction))]
    public static class Inject_scrDecoration_HitboxTriggerAction
    {
        public static void Prefix(
            scrDecoration __instance,
            out HitboxType __state,
            scrPlanet? planet
        )
        {
            __state = __instance.hitbox;

            if (!SettingsFixKillerDecorationsInNoFail.Instance.Enabled) return;
            if (!Adofai.Controller.gameworld) return;
            if (__instance.hitbox != HitboxType.Kill) return;
            if (RDC.auto) return;
            if (!ADOBase.controller.noFail) return;

            __instance.hitbox = HitboxType.None;

            if (planet != null && planet.iFrames > 0) return;
            if (__instance.hitOnce) return;

            Interoperation.ReplayIgnoreJudgement = true;
            planet?.player?.marginTracker?.AddHit(HitMargin.FailOverload);
            AddErrorMeterHit(Adofai.Controller.errorMeter, planet);
            planet?.MarkFail()?.BlinkForSeconds(3);
            Interoperation.ReplayIgnoreJudgement = false;
        }

        public static void Postfix(
            scrDecoration __instance,
            HitboxType __state
        )
        {
            __instance.hitbox = __state;
        }
    }
}