using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using static YqlossClientHarmony.Gui.YCHLayout;

namespace YqlossClientHarmony.Gui;

// System.Windows.Forms is only available on Windows, so dialogs are drawn with Unity IMGUI instead.
// dialogs can be queued from any thread and are drawn by DialogRenderer on the main thread.
public static class DialogManager
{
    private class DialogEntry
    {
        public string Title { get; set; } = "";

        public string Text { get; set; } = "";

        public List<(string Label, ButtonStyle Style, Action? Action)> Buttons { get; } = [];
    }

    private static readonly object SyncRoot = new();

    private static readonly Queue<DialogEntry> Queue = new();

    private static DialogEntry? _current;

    public static bool IsOpen
    {
        get
        {
            lock (SyncRoot) return _current is not null || Queue.Count > 0;
        }
    }

    public static void Alert(string title, string text)
    {
        var entry = new DialogEntry { Title = title, Text = text };
        entry.Buttons.Add((I18N.Translate("Dialog.Common.Ok"), ButtonStyle.Primary, null));
        Enqueue(entry);
    }

    public static void Confirm(string title, string text, Action onConfirm)
    {
        var entry = new DialogEntry { Title = title, Text = text };
        entry.Buttons.Add((I18N.Translate("Dialog.Common.Cancel"), ButtonStyle.Element, null));
        entry.Buttons.Add((I18N.Translate("Dialog.Common.Confirm"), ButtonStyle.Primary, onConfirm));
        Enqueue(entry);
    }

    private static void Enqueue(DialogEntry entry)
    {
        lock (SyncRoot) Queue.Enqueue(entry);
    }

    public static void Draw()
    {
        DialogEntry? entry;

        lock (SyncRoot)
        {
            if (_current is null && Queue.Count > 0) _current = Queue.Dequeue();
            entry = _current;
        }

        if (entry is null) return;

        Action? invoke = null;
        var dismissed = false;

        {
            var previousColor = GUI.color;
            GUI.color = new Color(0F, 0F, 0F, 0.6F);
            GUI.DrawTexture(new Rect(0F, 0F, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        var width = (float)UnityModManager.UI.Scale(420);
        var area = new Rect(
            (Screen.width - width) / 2F,
            Screen.height * 0.25F,
            width,
            Screen.height * 0.5F
        );

        GUILayout.BeginArea(area);

        try
        {
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                Text(entry.Title, TextStyle.Subtitle, WidthMax);
                Separator();
                Text(entry.Text, options: WidthMax);
                Separator();

                Begin(ContainerDirection.Horizontal, options: WidthMax);
                {
                    Fill();

                    foreach (var (label, style, action) in entry.Buttons)
                        if (Button(label, style, WidthMin))
                        {
                            dismissed = true;
                            invoke = action;
                            break;
                        }
                }
                End();
            }
            End();
        }
        finally
        {
            GUILayout.EndArea();
        }

        if (!dismissed) return;

        lock (SyncRoot) _current = null;
        invoke?.Invoke();
    }
}

// keeps DialogManager rendered no matter which screen the game is showing
public class DialogRenderer : MonoBehaviour
{
    private void OnGUI()
    {
        try
        {
            DialogManager.Draw();
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Error($"failed to draw dialog: {exception}");
        }
    }
}
