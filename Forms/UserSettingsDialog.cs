﻿using System;
using System.Windows.Forms;
using TableEditor.Settings;

namespace TableEditor.Forms;

// Pure-view settings dialog. All storage is delegated to SettingsService.Default — this form
// only reads values on Load and writes them back on close. The old static PushSettings event
// is replaced by SettingsService.SettingsChanged which any number of controls can subscribe to.
public partial class UserSettingsDialog : Form
{
    public UserSettingsDialog()
    {
        InitializeComponent();
    }

    // ---- Load / Save -----------------------------------------------------------------------

    private void UserSettingsDialog_Load(object sender, EventArgs e)
    {
        // Uncommenting the line below resets all settings to the defaults defined in
        // Properties/Settings.settings. Use this if the persisted values become corrupt.
        // Plot3D.Properties.Settings.Default.Reset();

        // Populate every control from the service; no logic here — just assignment.
        cbox_ShowButtonToolTips.Checked = SettingsService.Default.ShowButtonToolTips;
        cbox_ShowSamples.Checked = SettingsService.Default.ShowSamples;
        cBox_ShowGraphPanelOnStart.Checked = SettingsService.Default.ShowGraphPanelOnStart;
        cBox_Graph3D_ShowAxis.Checked = SettingsService.Default.ShowAxis;
        cBox_Graph3D_ShowAxisLabels.Checked = SettingsService.Default.ShowAxisLabels;
        cBox_Graph3D_ShowHoverPoint.Checked = SettingsService.Default.MirrorPoints;
        cBox_Graph3D_ShowGraphPosition.Checked = SettingsService.Default.ShowGraphPosition;
        btn_GraphPointColour.BackColor = SettingsService.Default.GraphPointsColour;
        textBox_Graph3D_GraphPointSize.Text = SettingsService.Default.GraphPointSize.ToString();
        textBox_Graph3D_SelectRadius.Text = SettingsService.Default.SelectRadius.ToString();
        textBox_Graph3D_Rotation.Text = SettingsService.Default.Rotation.ToString();
        textBox_Graph3D_Rotation_Transposed.Text = SettingsService.Default.RotationTransposed.ToString();
        textBox_Graph3D_Elevation.Text = SettingsService.Default.Elevation.ToString();
        textBox_Graph3D_Zoom.Text = SettingsService.Default.Zoom.ToString();

        // Auto-assign events after all controls are populated; doing this earlier would fire
        // change callbacks before the initial values are set, which triggers premature saves.
        UserSettingsDialog_AutoUnAssignEvents(this);
        UserSettingsDialog_AutoAssignEvents(this);
    }

    private void UserSettings_Save(object sender, FormClosingEventArgs e)
    {
        // Write every control value back to the service, then let the service persist and
        // broadcast the SettingsChanged event to all registered TableEditor3D instances.
        SettingsService.Default.ShowButtonToolTips = cbox_ShowButtonToolTips.Checked;
        SettingsService.Default.ShowSamples = cbox_ShowSamples.Checked;
        SettingsService.Default.ShowGraphPanelOnStart = cBox_ShowGraphPanelOnStart.Checked;
        SettingsService.Default.ShowAxis = cBox_Graph3D_ShowAxis.Checked;
        SettingsService.Default.ShowAxisLabels = cBox_Graph3D_ShowAxisLabels.Checked;
        SettingsService.Default.MirrorPoints = cBox_Graph3D_ShowHoverPoint.Checked;
        SettingsService.Default.ShowGraphPosition = cBox_Graph3D_ShowGraphPosition.Checked;
        SettingsService.Default.GraphPointsColour = btn_GraphPointColour.BackColor;
        SettingsService.Default.GraphPointSize = float.Parse(textBox_Graph3D_GraphPointSize.Text);
        SettingsService.Default.SelectRadius = int.Parse(textBox_Graph3D_SelectRadius.Text);
        SettingsService.Default.Rotation = int.Parse(textBox_Graph3D_Rotation.Text);
        SettingsService.Default.RotationTransposed = int.Parse(textBox_Graph3D_Rotation_Transposed.Text);
        SettingsService.Default.Elevation = int.Parse(textBox_Graph3D_Elevation.Text);
        SettingsService.Default.Zoom = int.Parse(textBox_Graph3D_Zoom.Text);

        SettingsService.Default.Save();
    }

    // ---- Control events --------------------------------------------------------------------

    private void btn_GraphPointColour_Click(object sender, EventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog();

        DialogResult result = colorDialog.ShowDialog();

        // Only apply the colour if the user actually confirmed the dialog.
        if (result == DialogResult.OK)
        {
            SettingsService.Default.GraphPointsColour = colorDialog.Color;
            btn_GraphPointColour.BackColor = colorDialog.Color;
        }
    }

    // ---- Automatic event wiring ------------------------------------------------------------
    // Controls are wired after Load so that the initial population does not trigger saves.
    // The unassign pass runs first to guard against calling this more than once.

    private void UserSettingsDialog_AutoAssignEvents(Control control)
    {
        // Recurse into the control tree, wiring the appropriate event to each TextBox and
        // CheckBox so that any change is immediately pushed to the live settings.
        foreach (Control child in control.Controls)
        {
            if (child is TextBox textBox)
                textBox.KeyDown += UserSettingsDialog_Textbox_KeyDown;
            else if (child is CheckBox checkBox)
                checkBox.CheckedChanged += UserSettingsDialog_Checkbox_CheckChanged;

            UserSettingsDialog_AutoAssignEvents(child);
        }
    }

    private void UserSettingsDialog_AutoUnAssignEvents(Control control)
    {
        // Mirror of AutoAssignEvents — removes handlers before re-adding them to ensure no
        // handler is attached more than once.
        foreach (Control child in control.Controls)
        {
            if (child is TextBox textBox)
                textBox.KeyDown -= UserSettingsDialog_Textbox_KeyDown;
            else if (child is CheckBox checkBox)
                checkBox.CheckedChanged -= UserSettingsDialog_Checkbox_CheckChanged;

            UserSettingsDialog_AutoUnAssignEvents(child);
        }
    }

    private void UserSettingsDialog_Textbox_KeyDown(object sender, KeyEventArgs e)
    {
        // Handlers are wired dynamically in Load, not in the Designer, so the switch
        // dispatches by control name instead of per-control event methods.
        TextBox textBox = (TextBox)sender;

        switch (textBox.Name)
        {
            case "textBox_Graph3D_GraphPointSize":
                if (e.KeyCode == Keys.Enter)
                {
                    if (float.TryParse(textBox_Graph3D_GraphPointSize.Text, out float graphPointSize))
                    {
                        graphPointSize = System.Math.Max(graphPointSize, 0.4F);
                        graphPointSize = System.Math.Min(graphPointSize, 2.0F);
                        textBox_Graph3D_GraphPointSize.Text = graphPointSize.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_GraphPointSize.Text = SettingsService.Default.GraphPointSize.ToString();
                    label2.Focus();
                }
                break;

            case "textBox_Graph3D_SelectRadius":
                if (e.KeyCode == Keys.Enter)
                {
                    if (int.TryParse(textBox_Graph3D_SelectRadius.Text, out int selectRadius))
                    {
                        selectRadius = System.Math.Max(selectRadius, 1);
                        selectRadius = System.Math.Min(selectRadius, 100);
                        textBox_Graph3D_SelectRadius.Text = selectRadius.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_SelectRadius.Text = SettingsService.Default.SelectRadius.ToString();
                    label2.Focus();
                }
                break;

            case "textBox_Graph3D_Rotation":
                if (e.KeyCode == Keys.Enter)
                {
                    if (int.TryParse(textBox_Graph3D_Rotation.Text, out int rotation))
                    {
                        rotation = System.Math.Max(rotation, 0);
                        rotation = System.Math.Min(rotation, 359);
                        textBox_Graph3D_Rotation.Text = rotation.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_Rotation.Text = SettingsService.Default.Rotation.ToString();
                    label2.Focus();
                }
                break;

            case "textBox_Graph3D_Rotation_Transposed":
                if (e.KeyCode == Keys.Enter)
                {
                    if (int.TryParse(textBox_Graph3D_Rotation_Transposed.Text, out int rotationTransposed))
                    {
                        rotationTransposed = System.Math.Max(rotationTransposed, 0);
                        rotationTransposed = System.Math.Min(rotationTransposed, 359);
                        textBox_Graph3D_Rotation_Transposed.Text = rotationTransposed.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_Rotation_Transposed.Text = SettingsService.Default.RotationTransposed.ToString();
                    label2.Focus();
                }
                break;

            case "textBox_Graph3D_Elevation":
                if (e.KeyCode == Keys.Enter)
                {
                    if (int.TryParse(textBox_Graph3D_Elevation.Text, out int elevation))
                    {
                        elevation = System.Math.Max(elevation, 0);
                        elevation = System.Math.Min(elevation, 359);
                        textBox_Graph3D_Elevation.Text = elevation.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_Elevation.Text = SettingsService.Default.Elevation.ToString();
                    label2.Focus();
                }
                break;

            case "textBox_Graph3D_Zoom":
                if (e.KeyCode == Keys.Enter)
                {
                    if (int.TryParse(textBox_Graph3D_Zoom.Text, out int zoom))
                    {
                        zoom = System.Math.Max(zoom, 500);
                        zoom = System.Math.Min(zoom, 3500);
                        textBox_Graph3D_Zoom.Text = zoom.ToString();
                        label2.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    textBox_Graph3D_Zoom.Text = SettingsService.Default.Zoom.ToString();
                    label2.Focus();
                }
                break;

            default:
                throw new Exception("Unhandled text box name — check control name or event wiring: " + textBox.Name);
        }

        // Pressing Enter commits the validated value immediately so the live graph updates
        // without requiring the dialog to close first.
        if (e.KeyCode == Keys.Enter)
            UserSettings_Save(null, null);
    }

    private void UserSettingsDialog_Checkbox_CheckChanged(object sender, EventArgs e)
    {
        // Every checkbox change is immediately persisted so the live graph reflects the new
        // setting without requiring the dialog to close.
        CheckBox checkBox = (CheckBox)sender;

        switch (checkBox.Name)
        {
            case "cbox_ShowButtonToolTips":
                break;
            case "cBox_ShowGraphPanelOnStart":
                break;
            case "cBox_Graph3D_ShowAxis":
                break;
            case "cBox_Graph3D_ShowAxisLabels":
                break;
            case "cBox_Graph3D_ShowGraphPosition":
                break;
            case "cBox_Graph3D_ShowHoverPoint":
                break;
            case "cbox_ShowSamples":
                break;
            default:
                throw new Exception("Unhandled checkbox name — check control name or event wiring: " + checkBox.Name);
        }

        UserSettings_Save(null, null);
    }
}
