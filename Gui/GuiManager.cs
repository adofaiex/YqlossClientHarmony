using System;
using UnityEngine;
using YqlossClientHarmony.Gui.Pages;
using YqlossClientHarmony.Utilities;
using static YqlossClientHarmony.Gui.YCHLayout;

namespace YqlossClientHarmony.Gui;

public static class GuiManager
{
    private static int _currentPage;

    private static bool _patchFailuresExpanded;

    private static string[] PageNames =>
    [
        I18N.Translate("Page.Language.Name"),
        I18N.Translate("Page.Replay.Name"),
        I18N.Translate("Page.EffectRemover.Name"),
        I18N.Translate("Page.OtherFeatures.Name")
    ];

    private static Action[] PageRenderers { get; } =
    [
        LanguagePage.Draw,
        ReplayPage.Draw,
        EffectRemoverPage.Draw,
        OtherFeaturesPage.Draw
    ];

    public static void Draw()
    {
        EnsureTexturesAlive();

        Begin(ContainerDirection.Vertical, ContainerStyle.Padding);
        {
            Begin(ContainerDirection.Horizontal);
            {
                Space(4);
                Selector(ref _currentPage, PageNames, options: WidthMin);
                Fill();
            }
            End();

            if (PatchManager.Failures.Count > 0) DrawPatchFailures();

            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                PageRenderers[_currentPage]();
            }
            End();
        }
        End();
    }

    private static void DrawPatchFailures()
    {
        Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
        {
            Begin(ContainerDirection.Horizontal, options: WidthMax);
            {
                if (ArrowButton(_patchFailuresExpanded ? ArrowStyle.Down : ArrowStyle.Right))
                    _patchFailuresExpanded = !_patchFailuresExpanded;

                Text(I18N.Translate("Gui.PatchManager.Failed", PatchManager.Failures.Count), TextStyle.Subtitle, WidthMax);
            }
            End();

            if (_patchFailuresExpanded)
            {
                foreach (var failure in PatchManager.Failures)
                {
                    Begin(ContainerDirection.Horizontal, options: WidthMax);
                    {
                        Icon(IconStyle.Error);
                        Text(failure.Type.Name, options: WidthMax);
                    }
                    End();
                }

                Text(I18N.Translate("Gui.PatchManager.Hint"), TextStyle.Secondary, WidthMax);

                Begin(ContainerDirection.Horizontal, options: WidthMax);
                {
                    Fill();

                    if (Button(I18N.Translate("Gui.PatchManager.Copy"), ButtonStyle.Element, WidthMin))
                    {
                        GUIUtility.systemCopyBuffer = PatchManager.GetFailureReport();
                        Main.Mod.Logger.Log("patch failure report copied to clipboard");
                    }
                }
                End();
            }
        }
        End();
    }
}