using System;
using System.Drawing;

namespace TableEditor.Settings;

// Non-UI singleton that owns all user preferences. It delegates storage to the auto-generated
// Plot3D.Properties.Settings class so the underlying .settings file and roaming profile are
// unchanged. All consumers should reference SettingsService.Default rather than
// Settings.Default directly, so the event bus (SettingsChanged) works uniformly.
public class SettingsService : ISettings
{
    // Singleton — constructed once at class-load time, thread-safe by CLR guarantee.
    public static readonly SettingsService Default = new SettingsService();

    // Private constructor enforces singleton usage.
    private SettingsService() { }

    // Fired after Save() persists values. Subscribers (e.g. TableEditor3D instances) should
    // read from SettingsService.Default and repaint / re-configure themselves.
    public event EventHandler SettingsChanged;

    // Persists the current property values and notifies all registered listeners.
    public void Save()
    {
        Plot3D.Properties.Settings.Default.Save();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ---- ISettings implementation — each property is a thin pass-through ----
    // Naming mismatches between the interface and the settings file are documented on each member.

    // Stored as ShowToolTips in settings — the UI label "Show button tool tips" drove the interface name.
    public bool ShowButtonToolTips
    {
        get { return Plot3D.Properties.Settings.Default.ShowToolTips; }
        set { Plot3D.Properties.Settings.Default.ShowToolTips = value; }
    }

    public bool ShowSamples
    {
        get { return Plot3D.Properties.Settings.Default.ShowSamples; }
        set { Plot3D.Properties.Settings.Default.ShowSamples = value; }
    }

    // Stored as ShowGraphPaneOnStart — "Pane" vs "Panel" mismatch kept to avoid data migration.
    public bool ShowGraphPanelOnStart
    {
        get { return Plot3D.Properties.Settings.Default.ShowGraphPaneOnStart; }
        set { Plot3D.Properties.Settings.Default.ShowGraphPaneOnStart = value; }
    }

    public bool ShowAxis
    {
        get { return Plot3D.Properties.Settings.Default.ShowAxis; }
        set { Plot3D.Properties.Settings.Default.ShowAxis = value; }
    }

    public bool ShowAxisLabels
    {
        get { return Plot3D.Properties.Settings.Default.ShowAxisLabels; }
        set { Plot3D.Properties.Settings.Default.ShowAxisLabels = value; }
    }

    // Stored as ShowHoverPoint — original name referred to a hover effect; the UI was later
    // relabelled "Mirror Points" to describe the bidirectional table/graph highlight behaviour.
    public bool MirrorPoints
    {
        get { return Plot3D.Properties.Settings.Default.ShowHoverPoint; }
        set { Plot3D.Properties.Settings.Default.ShowHoverPoint = value; }
    }

    public bool ShowGraphPosition
    {
        get { return Plot3D.Properties.Settings.Default.ShowGraphPosition; }
        set { Plot3D.Properties.Settings.Default.ShowGraphPosition = value; }
    }

    public Color GraphPointsColour
    {
        get { return Plot3D.Properties.Settings.Default.GraphPointsColour; }
        set { Plot3D.Properties.Settings.Default.GraphPointsColour = value; }
    }

    public float GraphPointSize
    {
        get { return Plot3D.Properties.Settings.Default.GraphPointSize; }
        set { Plot3D.Properties.Settings.Default.GraphPointSize = value; }
    }

    public int SelectRadius
    {
        get { return Plot3D.Properties.Settings.Default.SelectRadius; }
        set { Plot3D.Properties.Settings.Default.SelectRadius = value; }
    }

    // Stored as GraphRotation.
    public int Rotation
    {
        get { return Plot3D.Properties.Settings.Default.GraphRotation; }
        set { Plot3D.Properties.Settings.Default.GraphRotation = value; }
    }

    // Stored as GraphRotationTransposed — used when the table axes are swapped.
    public int RotationTransposed
    {
        get { return Plot3D.Properties.Settings.Default.GraphRotationTransposed; }
        set { Plot3D.Properties.Settings.Default.GraphRotationTransposed = value; }
    }

    // Stored as GraphElevation.
    public int Elevation
    {
        get { return Plot3D.Properties.Settings.Default.GraphElevation; }
        set { Plot3D.Properties.Settings.Default.GraphElevation = value; }
    }

    // Stored as GraphZoom.
    public int Zoom
    {
        get { return Plot3D.Properties.Settings.Default.GraphZoom; }
        set { Plot3D.Properties.Settings.Default.GraphZoom = value; }
    }
}
