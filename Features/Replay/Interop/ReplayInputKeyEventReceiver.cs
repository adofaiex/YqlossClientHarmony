using System;
using HarmonyLib;
using UnityEngine;

namespace YqlossClientHarmony.Features.Replay.Interop;

public class ReplayInputKeyEventReceiver(Type type) : IKeyEventReceiver
{
    private Action OnStartInputs { get; } = AccessTools.MethodDelegate<Action>(AccessTools.DeclaredMethod(type, "OnStartInputs"));

    private Action OnEndInputs { get; } = AccessTools.MethodDelegate<Action>(AccessTools.DeclaredMethod(type, "OnEndInputs"));

    private Action<KeyCode> OnKeyPressed { get; } = AccessTools.MethodDelegate<Action<KeyCode>>(AccessTools.DeclaredMethod(type, "OnKeyPressed"));

    private Action<KeyCode> OnKeyReleased { get; } = AccessTools.MethodDelegate<Action<KeyCode>>(AccessTools.DeclaredMethod(type, "OnKeyReleased"));

    public void Begin()
    {
        OnStartInputs();
    }

    public void End()
    {
        OnEndInputs();
    }

    public void OnKey(KeyCode code, bool isKeyDown)
    {
        if (isKeyDown) OnKeyPressed(code);
        else OnKeyReleased(code);
    }
}