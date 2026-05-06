using System.Drawing;

namespace TableEditor.Settings;

// Contract that any settings provider must satisfy. Both SettingsService and the legacy dialog
// implement this so call sites can be decoupled from whichever concrete source is active.
// Steps to add a new setting:
//   1. Add the property here.
//   2. Add the backing entry to Properties/Settings.settings.
//   3. Map it inside SettingsService.
//   4. Wire the control in UserSettingsDialog.
public interface ISettings
{
    bool ShowButtonToolTips { get; set; }

    bool ShowSamples { get; set; }

    bool ShowGraphPanelOnStart { get; set; }

    bool ShowAxis { get; set; }

    bool ShowAxisLabels { get; set; }

    // Named MirrorPoints in the UI; stored as ShowHoverPoint in the settings file — kept as-is
    // to avoid migrating user setting data.
    bool MirrorPoints { get; set; }

    bool ShowGraphPosition { get; set; }

    Color GraphPointsColour { get; set; }

    float GraphPointSize { get; set; }

    int SelectRadius { get; set; }

    int Rotation { get; set; }

    int RotationTransposed { get; set; }

    int Elevation { get; set; }

    int Zoom { get; set; }
}
