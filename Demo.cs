using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace TableEditor
{
    public partial class Demo : Form
    {
        // These are the table editor user control objects...
        TableEditor3D nateDogg, snoopDogg, iceCube, eminem;

        // ...which will get packed into this list
        List<TableEditor3D> tableEditor3dList = new List<TableEditor3D>();

        public Demo()
        {
            InitializeComponent();

            Size        = Init.FormOpeningSize;
            MinimumSize = Init.FormMinimumSize;

            // UI reference to marshall events back to the gui thread
            Timers.TimerOnDelay. uiControl = this;
            Timers.TimerOffDelay.uiControl = this;

            // Required form events
            this.Shown         += Demo_Shown;
            this.HandleCreated += Demo_HandleCreated;
            this.Resize        += Demo_Resize;
            this.FormClosing   += Demo_FormClosing;
        }

        private void Demo_Resize(object sender, EventArgs e)
        {
            // On resize to maximum, set the splitter bar to the edge of the dgv. This only works on the active tab page
            if (WindowState == FormWindowState.Maximized)
            {
                foreach (TableEditor3D table in tableEditor3dList)
                {
                    if (table.Graph3dEnabled)
                        table.FormResizedToMaximum();
                }
            }
        }

        private void Demo_HandleCreated(object sender, EventArgs e)
        {
            // Set options here:
            // Tab page 1
            nateDogg = new TableEditor3D
            {
                InstanceName = "nateDogg",
                Graph3dEnabled   = true,
                UseMyScrollBars  = true,
                UseToolBar       = true,
                UndoEnabled      = true,
                CopyPasteEnabled = true,
                AverageEnabled   = true,
                ColourTheme = ColourScheme.HpNormal
            };
            nateDogg.Initialise();

            // Tab page 2
            snoopDogg = new TableEditor3D
            {
                InstanceName = "snoopDogg",
                Graph3dEnabled   = true,
                UseMyScrollBars  = true,
                UseToolBar       = true,
                UndoEnabled      = true,
                CopyPasteEnabled = true,
                AverageEnabled   = true,
                ColourTheme = ColourScheme.HpNormal
            };
            snoopDogg.Initialise();

            // Tab page 3
            iceCube = new TableEditor3D
            {
                InstanceName = "iceCube",
                Graph3dEnabled   = true,
                UseMyScrollBars  = true,
                UseToolBar       = true,
                UndoEnabled      = true,
                CopyPasteEnabled = true,
                AverageEnabled   = true,
                ColourTheme = ColourScheme.HpNormal
            };
            iceCube.Initialise();

            // Tab page 4
            eminem = new TableEditor3D
            {
                InstanceName = "eminem",
                Graph3dEnabled   = true,
                UseMyScrollBars  = true,
                UseToolBar       = true,
                UndoEnabled      = true,
                CopyPasteEnabled = true,
                AverageEnabled   = true,
                ColourTheme = ColourScheme.HpNormal
            };
            eminem.Initialise();

            // Add each table editor instance to my list
            tableEditor3dList.Add(nateDogg);
            tableEditor3dList.Add(snoopDogg);
            tableEditor3dList.Add(iceCube);
            tableEditor3dList.Add(eminem);

            // Add the user controls to the form
            tabPage1.Controls.Add(nateDogg);  nateDogg.Dock  = DockStyle.Fill;
            tabPage2.Controls.Add(snoopDogg); snoopDogg.Dock = DockStyle.Fill;
            tabPage3.Controls.Add(iceCube);   iceCube.Dock   = DockStyle.Fill;
            tabPage4.Controls.Add(eminem);    eminem.Dock    = DockStyle.Fill;

            // Debug. Can only debug 1 instance at a time. Uncomment if needed
            // Debug(nateDogg);
            // Debug(snoopDogg);
            // Debug(iceCube);
            // Debug(eminem);
        }

        private void Demo_Shown(object sender, EventArgs e)
        {
            // Fucken stupid .Net, need to cycle through all tabs to for dgv colour format to apply
            tabPage4.Show();
            tabPage3.Show();
            tabPage2.Show();
            tabPage1.Show();

            // Sample data button clicks
            //nateDogg. btn_LoadSample3_Click();
            //snoopDogg.btn_LoadSample4_Click();
            //iceCube.  btn_LoadSample1_Click();
            //eminem.   btn_LoadSample2_Click();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 0:

                    break;

                case 1:

                    break;

                case 2:

                    break;

                case 3:

                    break;
            }
        }

        [Conditional("DEBUG")]
        private void Debug(TableEditor3D editor)
        {
            MyDebug debug = new MyDebug(editor);

            // Set false if not using!
            bool debugAll = false;

            // Table editor
            debug.TableEditor_Form = false;
            debug.TableEditor_Mouse = false;
            debug.TableEditor_SplitContainer = false;

            // Dgv control
            debug.DgvCtrl_EventDebug = true;
            debug.DgvCtrl_DataChangedDebug = true;
            debug.DgvCtrl_SelectionChangedDebug = false;
            debug.DgvCtrl_SizeChangedDebug = false;
            debug.DgvCtrl_MouseDebug = false;
            debug.DgvCtrl_incDecTask_Debug = false;
            debug.DgvCtrl_paste_Debug = true;
            debug.DgvCtrl_undo_Debug = false;
            debug.DgvCtrl_DgvData_Debug = false;

            // My events
            debug.DgvCtrl_myEvents_DebugAll = true;
            debug.DgvCtrl_myEvents_DebugDataChngd = true;
            debug.DgvCtrl_myEvents_DebugSizeChngd = false;
            debug.DgvCtrl_myEvents_DebugSelnChngd = false;
            debug.DgvCtrl_myEvents_MuteHghSpd = false;
            debug.DgvCtrl_myEvents_DebugDbncTmr = false;
            debug.DgvCtrl_myEvents_DebugIntTmr = false;

            // Scroll bar control
            debug.ScrollBarCtrl_DebugPosition = false;
            debug.ScrollBarCtrl_DebugExternalEvents = false;
            debug.ScrollBarCtrl_DebugMouseWheel = false;
            debug.ScrollBarCtrl_DebugValues = false;

            // Dgv headers
            debug.DgvHeaders_DebugHeaders = false;

            // Dgv to graph3d interface
            debug.DgvGrph3dIntfc_DebugAll = false;
            debug.DgvGrph3dIntfc_DebugData = false;
            debug.DgvGrph3dIntfc_DebugTimers = false;
            debug.DgvGrph3dIntfc_DebugHoverPoint = false;
            debug.DgvGrph3dIntfc_DebugPointMoveMode = false;
            debug.DgvGrph3dIntfc_DebugSelectionPoints = false;

            // Graph3d
            debug.Graph3dCtrl_DebugData = true;
            debug.Graph3dCtrl_DebugData_WithPrint = false;
            debug.Graph3dCtrl_DebugPointMoveMode = false;
            debug.Graph3dCtrl_DebugPointSelectMode = false;

            // Calling this overrides all the settings above and sets every property in the debug class to true. A fuck
            // tonne of console messages are generated! Wippee! :)
            if (debugAll)
                Debug_SetAllToTrue(debug);
        }
        
        [Conditional("DEBUG")]
        private void Debug_SetAllToTrue(object obj)
        {
            // Get the type of the object
            Type type = obj.GetType();

            // Get all properties of the object
            PropertyInfo[] properties = type.GetProperties();

            // Loop through each property
            foreach (PropertyInfo property in properties)
            {
                // Check if the property is of type bool and if it can be written to
                if (property.PropertyType == typeof(bool) && property.CanWrite)
                {
                    // Set the property value to true
                    property.SetValue(obj, true);
                }
            }
        }

        private void Demo_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (TableEditor3D table in tableEditor3dList)
            {
                table.KillTimers();
            }
        }
    }
}
