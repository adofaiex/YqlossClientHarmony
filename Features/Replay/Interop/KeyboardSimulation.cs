using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YqlossClientHarmony.Features.Replay.Interop;

public class KeyboardSimulation : IKeyEventReceiver
{
    private HashSet<byte> PressedKeys { get; } = [];

    public void Begin()
    {
    }

    public void End()
    {
        foreach (var pressedKey in PressedKeys)
            keybd_event(pressedKey, 0, 2, 0);

        PressedKeys.Clear();
    }

    public void OnKey(KeyCode code, bool isKeyDown)
    {
        if (SettingsReplay.Instance.DisableKeyboardSimulation) return;
        var key = (byte)(KeyCodeMapping.GetAsyncKeyCode(code) - 0x1000);
        keybd_event(key, 0, isKeyDown ? 0u : 2u, 0);
        if (isKeyDown) PressedKeys.Add(key);
        else PressedKeys.Remove(key);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);
}