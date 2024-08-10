using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using Plot3D.Properties;

namespace TableEditor
{
    // Settings dialog
    public partial class UserSettings : Form, ISettings
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region
        // 1. Create a property for each user setting in the user settings class properties
        // 2. In the solution explorer, add the settings property to 'your C# Project' --> Properties --> Settings.settings file
        // 3. Add the settings property in the user settings dialog load and save functions
        // 4. Add the property to the ISettings interface class
        public bool ShowButtonToolTips
        {
            get { return Settings.Default.ShowToolTips; }
            set { Settings.Default.ShowToolTips = value; }
        }

        public bool ShowSamples
        {
            get { return Settings.Default.ShowSamples; }
            set { Settings.Default.ShowSamples = value; }
        }

        public bool ShowGraphPanelOnStart
        {
            get { return Settings.Default.ShowGraphPaneOnStart; }
            set { Settings.Default.ShowGraphPaneOnStart = value; }
        }

        public bool ShowAxis
        {
            get { return Settings.Default.ShowAxis; }
            set { Settings.Default.ShowAxis = value; }
        }

        public bool ShowAxisLabels
        {
            get { return Settings.Default.ShowAxisLabels; }
            set { Settings.Default.ShowAxisLabels = value; }
        }

        public bool MirrorPoints
        {
            get { return Settings.Default.ShowHoverPoint; }
            set { Settings.Default.ShowHoverPoint = value; }
        }

        public bool ShowGraphPosition
        {
            get { return Settings.Default.ShowGraphPosition; }
            set { Settings.Default.ShowGraphPosition = value; }
        }

        public Color GraphPointsColour
        {
            get { return Settings.Default.GraphPointsColour; }
            set { Settings.Default.GraphPointsColour = value; }
        }

        public float GraphPointSize
        {
            get { return Settings.Default.GraphPointSize; }
            set { Settings.Default.GraphPointSize = value; }
        }

        public int SelectRadius
        {
            get { return Settings.Default.SelectRadius; }
            set { Settings.Default.SelectRadius = value; }
        }

        public int Rotation
        {
            get { return Settings.Default.GraphRotation; }
            set { Settings.Default.GraphRotation = value; }
        }

        public int RotationTransposed
        {
            get { return Settings.Default.GraphRotationTransposed; }
            set { Settings.Default.GraphRotationTransposed = value; }
        }

        public int Elevation
        {
            get { return Settings.Default.GraphElevation; }
            set { Settings.Default.GraphElevation = value; }
        }

        public int Zoom
        {
            get { return Settings.Default.GraphZoom; }
            set { Settings.Default.GraphZoom = value; }
        }
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region

        // Application config class
        Configuration config;

        // Push settings event
        public delegate void MyEventHandler(); // Event handler with no args
        public static event MyEventHandler PushSettings;
        #endregion

        //------------------------- Constructor ---------------------------------------------------------------------------------------
        #region
        public UserSettings()
        {
            InitializeComponent();

            // Get the configuration file of this application
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Each textbox and checkbox is assigned a respective event so that when its value is changed in the
            // settings dialog it is pushed out to the various properties straight away so its effect can be evaluated
            // without the need to close and re-open the settings dialog. The event assignment is done in the
            // UserSettings_Load function.
        }
        #endregion

        //------------------------- Methods -------------------------------------------------------------------------------------------
        #region
        public void UserSettings_Load()
        {
            UserSettings_Load(null, null);
        }

        private void UserSettings_Load(object sender, EventArgs e)
        {
            // Uncommenting this line will reset settings to those in the Settings.settings file located under
            // 'Properties' in the solution explorer. Do this if your settings become corrupt
            //Settings.Default.Reset();

            // Load user settings on form load
            cbox_ShowButtonToolTips.Checked = Settings.Default.ShowToolTips;

            cbox_ShowSamples.Checked = Settings.Default.ShowSamples;

            cBox_ShowGraphPanelOnStart.Checked = Settings.Default.ShowGraphPaneOnStart;

            cBox_Graph3D_ShowAxis.Checked = Settings.Default.ShowAxis;

            cBox_Graph3D_ShowAxisLabels.Checked = Settings.Default.ShowAxisLabels;

            cBox_Graph3D_ShowHoverPoint.Checked = Settings.Default.ShowHoverPoint;

            cBox_Graph3D_ShowGraphPosition.Checked = Settings.Default.ShowGraphPosition;

            btn_GraphPointColour.BackColor = Settings.Default.GraphPointsColour;

            textBox_Graph3D_GraphPointSize.Text = Settings.Default.GraphPointSize.ToString();

            textBox_Graph3D_SelectRadius.Text = Settings.Default.SelectRadius.ToString();

            textBox_Graph3D_Rotation.Text = Settings.Default.GraphRotation.ToString();

            textBox_Graph3D_Rotation_Transposed.Text = Settings.Default.GraphRotationTransposed.ToString();

            textBox_Graph3D_Elevation.Text = Settings.Default.GraphElevation.ToString();

            textBox_Graph3D_Zoom.Text = Settings.Default.GraphZoom.ToString();

            // Stupid winforms, this is the only way I could this strategy of auto assigning events to work. If these
            // functions are called initially in the constructor or at the start of this function the settings are
            // completely ignored which fucks everything
            UserSettingsDialog_AutoUnAssignEvents(this);
            UserSettingsDialog_AutoAssignEvents(this);
        }

        private void UserSettings_Save(object sender, FormClosingEventArgs e)
        {
            Settings.Default.ShowToolTips = cbox_ShowButtonToolTips.Checked;

            Settings.Default.ShowSamples = cbox_ShowSamples.Checked;

            Settings.Default.ShowGraphPaneOnStart = cBox_ShowGraphPanelOnStart.Checked;

            Settings.Default.ShowAxis = cBox_Graph3D_ShowAxis.Checked;

            Settings.Default.ShowAxisLabels = cBox_Graph3D_ShowAxisLabels.Checked;

            Settings.Default.ShowHoverPoint = cBox_Graph3D_ShowHoverPoint.Checked;

            Settings.Default.ShowGraphPosition = cBox_Graph3D_ShowGraphPosition.Checked;

            Settings.Default.GraphPointsColour = btn_GraphPointColour.BackColor;

            Settings.Default.GraphPointSize = float.Parse(textBox_Graph3D_GraphPointSize.Text);

            Settings.Default.SelectRadius = int.Parse(textBox_Graph3D_SelectRadius.Text);

            Settings.Default.GraphRotation = int.Parse(textBox_Graph3D_Rotation.Text);

            Settings.Default.GraphRotationTransposed = int.Parse(textBox_Graph3D_Rotation_Transposed.Text);

            Settings.Default.GraphElevation = int.Parse(textBox_Graph3D_Elevation.Text);

            Settings.Default.GraphZoom = int.Parse(textBox_Graph3D_Zoom.Text);

            Settings.Default.Save();

            // Notify any subscribers to update their class instance settings
            PushSettings?.Invoke();
        }

        private void btn_GraphPointColour_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();

            // Show the ColorDialog and wait for the user's action
            DialogResult result = colorDialog.ShowDialog();

            // If the user selects a color, set it as the selected color
            if (result == DialogResult.OK)
            {
                GraphPointsColour = colorDialog.Color;

                // Sets the background color of the button
                btn_GraphPointColour.BackColor = GraphPointsColour;
            }
        }

        private void UserSettingsDialog_AutoAssignEvents(Control control)
        {
            // This code iterates through each top level controls child controls (if any). It is recursively called to
            // allow it to bury itself down to the last nested control. Once it reaches as far down as it can go, it
            // pops back out to the top level where it moves onto the next top level control.

            // Assign the keydown event to all text boxes
            foreach (Control childControl in control.Controls)
            {
                // Check if the child control is a TextBox or CheckBox
                if (childControl is TextBox textBox)
                {
                    textBox.KeyDown += UserSettingsDialog_Textbox_KeyDown;
                }
                else if (childControl is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += UserSettingsDialog_Checkbox_CheckChanged;
                }

                // Recursively call this method to iterate through nested controls
                UserSettingsDialog_AutoAssignEvents(childControl);
            }
        }

        private void UserSettingsDialog_AutoUnAssignEvents(Control control)
        {
            // This code iterates through each top level controls child controls (if any). It is recursively called to
            // allow it to bury itself down to the last nested control. Once it reaches as far down as it can go, it
            // pops back out to the top level where it moves onto the next top level control.

            // Assign the keydown event to all text boxes
            foreach (Control childControl in control.Controls)
            {
                // Check if the child control is a TextBox or CheckBox
                if (childControl is TextBox textBox)
                {
                    textBox.KeyDown -= UserSettingsDialog_Textbox_KeyDown;
                }
                else if (childControl is CheckBox checkBox)
                {
                    checkBox.CheckedChanged -= UserSettingsDialog_Checkbox_CheckChanged;
                }

                // Recursively call this method to iterate through nested controls
                UserSettingsDialog_AutoAssignEvents(childControl);
            }
        }

        private void UserSettingsDialog_Textbox_KeyDown(object sender, KeyEventArgs e)
        {
            // There is no need to add the key down event to the textbox in the designer. The keydown event is assigned
            // automatically in the constructor

            TextBox textBox = (TextBox)sender;

            string textBoxName = textBox.Name;

            switch (textBoxName)
            {
                case "textBox_Graph3D_GraphPointSize":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (float.TryParse(textBox_Graph3D_GraphPointSize.Text, out float graphPointSize))
                        {
                            graphPointSize = Math.Max(graphPointSize, 0.4F);
                            graphPointSize = Math.Min(graphPointSize, 2.0F);

                            textBox_Graph3D_GraphPointSize.Text = graphPointSize.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_GraphPointSize.Text = GraphPointSize.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                case "textBox_Graph3D_SelectRadius":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (int.TryParse(textBox_Graph3D_SelectRadius.Text, out int SelectRadius))
                        {
                            SelectRadius = Math.Max(SelectRadius, 1);
                            SelectRadius = Math.Min(SelectRadius, 100);

                            textBox_Graph3D_SelectRadius.Text = SelectRadius.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_SelectRadius.Text = SelectRadius.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                case "textBox_Graph3D_Rotation":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (int.TryParse(textBox_Graph3D_Rotation.Text, out int Rotation))
                        {
                            Rotation = Math.Max(Rotation, 0);
                            Rotation = Math.Min(Rotation, 359);

                            textBox_Graph3D_Rotation.Text = Rotation.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_Rotation.Text = Rotation.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                case "textBox_Graph3D_Rotation_Transposed":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (int.TryParse(textBox_Graph3D_Rotation_Transposed.Text, out int RotationTransposed))
                        {
                            RotationTransposed = Math.Max(RotationTransposed, 0);
                            RotationTransposed = Math.Min(RotationTransposed, 359);

                            textBox_Graph3D_Rotation_Transposed.Text = RotationTransposed.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_Rotation_Transposed.Text = RotationTransposed.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                case "textBox_Graph3D_Elevation":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (int.TryParse(textBox_Graph3D_Elevation.Text, out int Elevation))
                        {
                            Elevation = Math.Max(Elevation, 0);
                            Elevation = Math.Min(Elevation, 359);

                            textBox_Graph3D_Elevation.Text = Elevation.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_Elevation.Text = Elevation.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                case "textBox_Graph3D_Zoom":
                    #region
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (int.TryParse(textBox_Graph3D_Zoom.Text, out int Zoom))
                        {
                            Zoom = Math.Max(Zoom, 500);
                            Zoom = Math.Min(Zoom, 3500);

                            textBox_Graph3D_Zoom.Text = Zoom.ToString();

                            label2.Focus();
                        }
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        textBox_Graph3D_Zoom.Text = Zoom.ToString();

                        label2.Focus();
                    }
                    break;
                #endregion

                default:
                    throw new Exception("Please check textbox name or event assignment");
            }

            // Pressing enter pushes the settings
            if (e.KeyCode == Keys.Enter)
            {
                UserSettings_Save(null, null);
            }
        }

        private void UserSettingsDialog_Checkbox_CheckChanged(object sender, EventArgs e)
        {
            // There is no need to add the check changed event to the textbox in the designer. The keydown event is
            // assigned automatically in the constructor

            CheckBox checkBox = (CheckBox)sender;

            string checkBoxName = checkBox.Name;

            switch (checkBoxName)
            {
                case "cbox_ShowButtonToolTips":
                    #region

                    break;
                #endregion

                case "cBox_ShowGraphPanelOnStart":
                    #region

                    break;
                #endregion

                case "cBox_Graph3D_ShowAxis":
                    #region

                    break;
                #endregion

                case "cBox_Graph3D_ShowAxisLabels":
                    #region

                    break;
                #endregion

                case "cBox_Graph3D_ShowGraphPosition":
                    #region

                    break;
                #endregion

                case "cBox_Graph3D_ShowHoverPoint":
                    #region

                    break;
                #endregion

                case "cbox_ShowSamples":
                    #region

                    break;
                #endregion

                default:
                    throw new Exception("Please check checkbox name or event assignment");
            }

            // Check changed pushes the settings
            UserSettings_Save(null, null);
        }
        #endregion
    }
    #endregion

    public interface ISettings
    #region
    {
        // 1. Create a property for each user setting in the user settings class properties
        // 2. In the solution explorer, add the settings property to 'your C# Project' --> Properties --> Settings.settings file
        // 3. Add the settings property in the user settings dialog load and save functions
        // 4. Add the property to the ISettings interface class

        #region
        bool ShowButtonToolTips { get; set; }

        bool ShowSamples { get; set; }

        bool ShowGraphPanelOnStart { get; set; }

        bool ShowAxis { get; set; }

        bool ShowAxisLabels { get; set; }

        bool MirrorPoints { get; set; }

        bool ShowGraphPosition { get; set; }

        Color GraphPointsColour { get; set; }

        float GraphPointSize { get; set; }

        int SelectRadius { get; set; }

        int Rotation { get; set; }

        int RotationTransposed { get; set; }

        int Elevation { get; set; }

        int Zoom { get; set; }

        void UserSettings_Load();
        #endregion
    }
    #endregion
}