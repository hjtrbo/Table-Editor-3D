using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

// Microtimer library
using MicroLibrary;

// My timers
using Timers;

// Plot 3D
using Plot3D;

// Plot 3D classes
using cColorScheme = Plot3D.Editor3D.cColorScheme;
using cObject3D    = Plot3D.Editor3D.cObject3D;
using cPoint3D     = Plot3D.Editor3D.cPoint3D;
using cSurfaceData = Plot3D.Editor3D.cSurfaceData;
using eColorScheme = Plot3D.Editor3D.eColorScheme;
using eInvalidate  = Plot3D.Editor3D.eInvalidate;
using eLegendPos   = Plot3D.Editor3D.eLegendPos;
using eMouseCtrl   = Plot3D.Editor3D.eMouseCtrl;
using eNormalize   = Plot3D.Editor3D.eNormalize;
using ePolygonMode = Plot3D.Editor3D.ePolygonMode;

// Plot 3D enums
using eRaster   = Plot3D.Editor3D.eRaster;
using eSelEvent = Plot3D.Editor3D.eSelEvent;
using eSelType  = Plot3D.Editor3D.eSelType;
using eTooltip  = Plot3D.Editor3D.eTooltip;

// Keyboard
using Key      = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;

// Interface
using MyPoint  = TableEditor.DgvToGraph3d.MyPoint;
using MyPoints = TableEditor.DgvToGraph3d.MyPoints;
using System.Security.Cryptography;

namespace TableEditor
{
    // A 3d map editor designed to accompany tuning software that lack 3d graph support.
    //
    // Written for my own personal use. It's not perfect but good enough considering it's written by a hack with no
    // formal programming experience. 

    #region IMPLEMENTATION NOTES
    //
    // Setup notes for embedding this control in the hosting form
    //

    //
    // This user control size and location:
    //      Aim for a form opening size that achieves around > 700 x 420 for this user control. A main form size of
    //      880 x 600 worked well.
    //      Set this user control to Dock = Dock.Fill (not mandatory)
    //      If embedding into a tabPage:
    //          Set the tapPages padding and margin attributes to 0
    //          Set the user controls location, margin and padding attributes to 0
    //          In the main forms OnShown() event, cycle through all tab pages that contain this user control. I've
    //          never worked out why this needs to be done but if you don't things turns to shit.
    //

    //
    // Table editor user control minimum required settings. Copy these to the host forms Load() event
    //      nateDogg.Graph3dEnabled    = true;
    //      nateDogg.UseMyScrollBars   = true;
    //      nateDogg.HideToolBar       = false;
    //      nateDogg.UndoEnabled       = true;
    //      nateDogg.UseHPColourTheme  = true;
    //      nateDogg.CopyPasteSetMode  = CopyPasteMode.All;
    //
    #endregion

    #region Global Enums
    public enum CopyPasteMode
    {
        All,
        Copy,
        None
    }

    public enum RefreshMode
    {
        All,
        ExternallySetNumberFormat,
        Partial,
        WidthColour,
        ColourOnly,
        DpAdjust,
        StyleWidthSize,
        AverageTool
    }

    public enum FormatTarget
    {
        RowHeaders,
        ColHeaders,
        AllHeaders,
        Cells,
        All,
        None
    }

    public enum DpDirection
    {
        Decrement,
        Increment
    }

    public enum ColourScheme
    {
        None,
        HpNormal,
        HpEdited,
        HpScanner
    }
    #endregion

    public partial class TableEditor3D : UserControl
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region
        // Feature setup, these all need to be set in the hosting forms DGV_Builder function
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string ClassName { get; set; } = "TableEditor3D";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string InstanceName { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Graph3dEnabled { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool UseMyScrollBars { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Font DgvFont { get { return dgvCtrl.Font; } set { dgvCtrl.Font = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int hTextPadding { get { return dgvCtrl.hTextPadding; } set { dgvCtrl.hTextPadding = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int vTextPadding { get { return dgvCtrl.vTextPadding; } set { dgvCtrl.vTextPadding = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(true)]
        public bool UseToolBar
        {
            get { return useToolBar; } // Get needed for designer to add this entry
            set
            {
                useToolBar = value;

                if (useToolBar)
                    toolBar.Show();
                else
                    toolBar.Hide();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool UndoEnabled { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool CopyPasteEnabled { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool AverageEnabled { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ColourScheme ColourTheme { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public CopyPasteMode CopyPasteSetMode { set { copyPasteMode = value; } }
        //
        // Misc.
        //
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool HasData { get { return dgvCtrl.DgvHasData; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Size DgvSize { get { return dgvCtrl.DgvSize; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DgvCtrl.DgvNumFormat DgvNumberFormat { get { return dgvCtrl.dgvNumFormat; } set { dgvCtrl.dgvNumFormat = value; } }
        //
        // Graph3d point manipulation mode
        //
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Graph3dPointSelectMode { get { return graph3dPointSelectMode; } set { graph3dPointSelectMode = value; dgvGrph3dIntfc.PointSelectMode = value; graph3dCtrl.graph3d.PointSelectMode = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Graph3dPointMoveMode
        {
            get
            {
                return graph3dPointMoveMode;
            }
            set
            {
                graph3dPointMoveMode = value;
                dgvGrph3dIntfc.PointMoveMode = value;
                graph3dCtrl.graph3d.PointMoveMode = value;

                // Button back colour
                if (Graph3dPointMoveMode)
                    btn_Graph3d_PointMoveMode.ImageIndex = 9; // active
                else
                    btn_Graph3d_PointMoveMode.ImageIndex = 8;
            }
        }
        //
        // ISettings implementation
        //
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowSamples { get { return showSamples; } set { showSamples = value; ShowSampleButtons(); } } // For dev testing

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowButtonToolTips { get { return toolTip.Active; } set { toolTip.Active = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowGraphPanel { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowAxis { get { return showAxis; } set { showAxis = value; if (graph3dCtrl != null) graph3dCtrl.ShowAxis = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowAxisLabels { get { return showAxisLabels; } set { showAxisLabels = value; if (graph3dCtrl != null) graph3dCtrl.ShowAxisLabels = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool MirrorPoints
        {
            get { return mirrorPoints; }
            set
            {
                mirrorPoints = value;

                if (dgvCtrl != null)
                    dgvCtrl.ClearSelection();

                if (graph3dCtrl != null)
                    graph3dCtrl.ClearGraphSelection();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowGraphPosition { get { return showGraphPosition; } set { showGraphPosition = value; if (graph3dCtrl != null) graph3dCtrl.ShowGraphPosition = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Color GraphPointsColour { get { return graphPointsColour; } set { graphPointsColour = value; if (graph3dCtrl != null) graph3dCtrl.SelectPointColour = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public float GraphPointSize { get { return graphPointSize; } set { graphPointSize = value; if (graph3dCtrl != null) graph3dCtrl.SelectPointSize = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int SelectRadius { get { return selectRadius; } set { selectRadius = value; if (graph3dCtrl != null) graph3dCtrl.SelectRadius = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int Rotation { get { return rotation; } set { rotation = value; if (graph3dCtrl != null) graph3dCtrl.Rotation = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int RotationTransposed { get { return rotationTransposed; } set { rotationTransposed = value; if (graph3dCtrl != null) graph3dCtrl.RotationTransposed = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int Elevation { get { return elevation; } set { elevation = value; if (graph3dCtrl != null) graph3dCtrl.Elevation = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int Zoom { get { return zoom; } set { zoom = value; if (graph3dCtrl != null) graph3dCtrl.Zoom = value; } }

        // Debug
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DebugSplitContainer { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DebugForm { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DebugMouse { get; set; }
        #endregion

        //------------------------- Public --------------------------------------------------------------------------------------------
        #region

        public double[] ReadFromDgv_RowLabels()
        {
            double[] y = new double[0];

            try
            {
                y = dgvCtrl.ReadRowHeaders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"External read from the table editor failed.\r\n \r\n" +
                    $"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return y;
        }

        public double[] ReadFromDgv_ColLabels()
        {
            double[] x = new double[0];

            try
            {
                x = dgvCtrl.ReadColHeaders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"External read from the table editor failed.\r\n \r\n" +
                    $"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return x;
        }

        public double[,] ReadFromDgv_TableData()
        {
            double[,] z = new double[0, 0];

            try
            {
                z = dgvCtrl.ReadDataTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"External read from the table editor failed.\r\n \r\n" +
                    $"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return z;
        }

        public (double[], double[], double[,]) ReadFromDgv()
        {
            double[] d1 = ReadFromDgv_RowLabels();
            double[] d2 = ReadFromDgv_ColLabels();
            double[,] d3 = ReadFromDgv_TableData();

            return (d1, d2, d3);
        }

        public void DgvClearSelection()
        {
            dgvCtrl.ClearSelection();
        }

        public class ScrollBarCntrls
        #region
        {
            // Packages up the scroll bars and dgv headers objects

            public DataGridView RowHeader { get; set; }
            public DataGridView ColHeader { get; set; }
            public Panel BlankingPanel { get; set; }
            public HScrollBar hScrollBar { get; set; }
            public VScrollBar vScrollBar { get; set; }
            public SplitContainer splitContainer { get; set; }
            public string InstanceName { get; set; }

            public ScrollBarCntrls() { }
        }
        #endregion

        public class DgvHeaderCntrls
        #region
        {
            // Packages up the scroll bars and dgv headers objects

            public DataGridView RowHeader { get; set; }
            public DataGridView ColHeader { get; set; }
            public Panel BlankingPanel { get; set; }
            public HScrollBar hScrollBar { get; set; }
            public VScrollBar vScrollBar { get; set; }
            public SplitContainer splitContainer { get; set; }
            public string InstanceName { get; set; }

            public DgvHeaderCntrls() { }
        }
        #endregion
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region
        // Classes
        public DgvCtrl dgvCtrl;
        public Graph3dCtrl graph3dCtrl;
        public DgvToGraph3d dgvGrph3dIntfc;
        private AverageTool averageTool;
        public ScrollBarCntrls ScrollBarControls;
        public DgvHeaderCntrls DgvHeaderControls;
        private UserSettings userSettings;

        // Backing fields
        bool showSamples;
        bool useToolBar;
        bool graph3dPointSelectMode;
        bool graph3dPointMoveMode;
        bool showAxis;
        bool showAxisLabels;
        bool mirrorPoints;
        float graphPointSize;
        int selectRadius;
        Color graphPointsColour;
        bool showGraphPosition;
        int rotation;
        int rotationTransposed;
        int elevation;
        int zoom;
        CopyPasteMode copyPasteMode;

        // Misc.
        bool transposeXY;
        int lastSplitterDistance = 0;

        #region Enums
        enum InterpolateMode
        {
            Vertical,
            Horizontal,
            All
        }

        enum GraphDisplay
        {
            Nothing,
            Right,
            Fill,
            Hide,
        }
        GraphDisplay graphDisplay;
        #endregion

        #region Sample data tables
        readonly double[] sampleDataColHeaders = { 500, 700, 900, 1200, 1600, 2000, 2400, 2800, 3200, 4100, 5000, 5800 };           // X
        readonly double[] sampleDataRowHeaders = { 0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 800 }; // Y
        readonly double[,] sampleTableData =   {{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                                                    { 40, 40.67, 34, 30.67, 27.17, 25.17, 23.5, 22.5, 20, 15.33, 14, 0 },
                                                    { 0, 43.83, 41.5, 38.17, 34.83, 32.5, 30.2, 29.75, 26.67, 19.33, 18, 0 },
                                                    { 0, 50.83, 47, 45.17, 43.17, 39.4, 38.5, 35.5, 33.6, 28, 27, 0 },
                                                    { 0, 57.5, 54.4, 52.5, 49.17, 47.83, 44.8, 41, 40, 32.5, 31, 0 },
                                                    { 0, 0, 60.5, 60.33, 56.33, 54.5, 52, 50.67, 45.4, 38, 39, 0 },
                                                    { 0, 0, 0, 67.5, 63.17, 61.83, 59.4, 55, 48.75, 45.33, 39.5, 0 },
                                                    { 0, 0, 69, 73.83, 0, 0, 0, 0, 0, 47.5, 43, 0 },
                                                    { 59, 0, 0, 0, 0, 0, 0, 0, 62.75, 0, 0, 0 },
                                                    { 0, 68, 0, 85.33, 79.6, 78.67, 76.33, 75.2, 70.4, 57.4, 53, 0 },
                                                    { 0, 0, 0, 88.17, 84.17, 83.25, 82.17, 78.2, 70.5, 62, 61.25, 0 },
                                                    { 0, 0, 0, 95, 91.4, 88.67, 86.4, 81.67, 75, 70.4, 67.33, 0 },
                                                    { 0, 0, 0, 95.8, 96, 95.83, 96, 93.17, 83.25, 72.25, 69.33, 0 },
                                                    { 95, 0, 0, 96, 96.25, 97, 96, 96.17, 88.6, 82.5, 76, 0 },
                                                    { 95, 95, 0, 0, 0, 0, 0, 96, 93.67, 93, 85.5, 0 },
                                                    { 95.4, 0, 0, 0, 0, 0, 0, 0, 95, 94.33, 93, 0 }};
        readonly double[,] sampleTableData2 =   {{ 28.9, 27.5, 24.2, 20.7, 17.5, 15.2, 13.3, 11.3,8.8, 6.4, 4.6, 0.9 },
                                                     { 35.0, 32.9, 29.8, 25.3, 22.4, 20.0, 18.4, 16.7, 14.0, 11.1, 9.4,  8.7 },
                                                     { 40.0, 40.7, 34.0, 30.7, 27.2, 25.2, 23.5, 22.5, 20.0, 15.3, 14.0, 16 },
                                                     { 42.7, 43.8, 41.5, 38.2, 34.8, 32.5, 30.2, 29.8, 26.7, 19.3, 18.0, 19.7 },
                                                     { 45.4, 50.8, 47.0, 45.2, 43.2, 39.4, 38.5, 35.5, 33.6, 28.0, 27.0, 25.3 },
                                                     { 48.1, 57.5, 54.4, 52.5, 49.2, 47.8, 44.8, 41.0, 40.0, 32.5, 31.0, 32.3 },
                                                     { 50.9, 59.6, 60.5, 60.3, 56.3, 54.5, 52.0, 50.7, 45.4, 38.0, 36.0, 36.5 },
                                                     { 53.6, 61.7, 64.8, 67.5, 63.2, 61.8, 59.4, 55.0, 48.8, 45.3, 39.5, 40.5 },
                                                     { 56.3, 63.8, 69.0, 73.8, 68.8, 67.3, 64.5, 61.0, 55.8, 47.5, 43.0, 43.5 },
                                                     { 59.0, 65.9, 72.8, 79.7, 74.0, 72.8, 70.2, 68.2, 62.8, 52.5, 48.0, 48 },
                                                     { 66.2, 68.0, 76.7, 85.3, 79.6, 78.7, 76.3, 75.2, 70.4, 57.4, 53.0, 54.1 },
                                                     { 73.4, 73.4, 81.4, 88.2, 84.2, 83.3, 82.2, 78.2, 70.5, 62.0, 61.3, 60.5 },
                                                     { 80.6, 78.8, 86.2, 95.0, 91.4, 88.7, 86.4, 81.7, 75.0, 70.4, 67.3, 66 },
                                                     { 87.8, 84.2, 90.9, 95.8, 96.0, 95.8, 96.0, 93.2, 83.3, 72.3, 69.3, 70.9 },
                                                     { 95.0, 89.6, 95.7, 96.0, 96.3, 97.0, 96.0, 96.2, 88.6, 82.5, 76.0, 76.9 },
                                                     { 95.0, 95.0, 95.2, 95.3, 95.5, 95.7, 95.8, 96.0, 93.7, 93.0, 85.5, 84.8 },
                                                     { 95.0, 95.0, 95.0, 95.0, 95.0, 95.0, 95.0, 95.0, 95.0, 94.3, 93.0, 89.3 }};
        readonly double[] sampleDataColHeaders3 = { 500, 750, 1000, 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 3250, 3500, 3750, 4000, 4250, 4500, 4750, 5000, 5250, 5500, 5750, 6000, 6250, 6500, 6750, 7000 };
        readonly double[] sampleDataRowHeaders3 = { 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 205, 210 };
        readonly double[,] sampleTableData3 = {{ 377, 557, 749, 834, 915, 992, 1076, 1120, 1170, 1228, 1293, 1340, 1378, 1407, 1427, 1437, 1470, 1507, 1536, 1555, 1567, 1569, 1563, 1548, 1525, 1493, 1452 },
                                                    { 536, 698, 826, 909, 987, 1062, 1134, 1178, 1230, 1289, 1355, 1439, 1482, 1515, 1539, 1554, 1583, 1620, 1647, 1666, 1676, 1677, 1670, 1654, 1629, 1596, 1554 },
                                                    { 673, 817, 897, 977, 1053, 1125, 1190, 1235, 1288, 1348, 1416, 1527, 1574, 1612, 1640, 1659, 1687, 1722, 1748, 1766, 1775, 1775, 1766, 1749, 1723, 1689, 1645 },
                                                    { 787, 914, 963, 1040, 1113, 1182, 1245, 1292, 1346, 1407, 1475, 1604, 1655, 1697, 1729, 1752, 1781, 1815, 1840, 1856, 1864, 1862, 1853, 1834, 1807, 1772, 1727 },
                                                    { 879, 987, 1022, 1096, 1167, 1234, 1298, 1346, 1402, 1464, 1534, 1668, 1724, 1770, 1806, 1834, 1865, 1897, 1921, 1936, 1943, 1940, 1929, 1910, 1881, 1845, 1799 },
                                                    { 948, 1038, 1075, 1146, 1214, 1279, 1351, 1400, 1456, 1520, 1591, 1721, 1781, 1831, 1872, 1903, 1939, 1970, 1992, 2006, 2012, 2008, 1996, 1975, 1946, 1908, 1861 },
                                                    { 994, 1067, 1121, 1191, 1256, 1318, 1402, 1452, 1510, 1575, 1646, 1762, 1826, 1881, 1926, 1961, 2003, 2033, 2054, 2067, 2071, 2066, 2053, 2031, 2000, 1961, 1913 },
                                                    { 1018, 1073, 1162, 1229, 1292, 1351, 1452, 1504, 1562, 1628, 1701, 1791, 1860, 1918, 1968, 2008, 2057, 2085, 2106, 2117, 2120, 2114, 2099, 2076, 2044, 2004, 1955 },
                                                    { 1027, 1076, 1160, 1287, 1375, 1422, 1498, 1556, 1611, 1662, 1709, 1786, 1888, 1964, 2014, 2037, 2080, 2116, 2141, 2156, 2162, 2156, 2141, 2116, 2080, 2035, 1979 },
                                                    { 1035, 1082, 1200, 1328, 1416, 1464, 1523, 1579, 1631, 1678, 1721, 1801, 1905, 1984, 2035, 2061, 2100, 2136, 2162, 2178, 2184, 2180, 2166, 2141, 2107, 2062, 2007 },
                                                    { 1057, 1102, 1237, 1365, 1454, 1502, 1553, 1606, 1654, 1698, 1739, 1812, 1919, 2000, 2054, 2082, 2114, 2151, 2178, 2195, 2202, 2199, 2185, 2161, 2128, 2084, 2030 },
                                                    { 1092, 1135, 1269, 1399, 1488, 1537, 1588, 1637, 1682, 1723, 1760, 1821, 1930, 2012, 2069, 2099, 2123, 2161, 2189, 2207, 2214, 2212, 2199, 2176, 2143, 2100, 2047 },
                                                    { 1138, 1213, 1318, 1424, 1506, 1562, 1600, 1683, 1741, 1775, 1783, 1849, 1945, 2022, 2078, 2115, 2137, 2173, 2200, 2218, 2225, 2223, 2211, 2189, 2158, 2117, 2066 },
                                                    { 1193, 1275, 1374, 1466, 1533, 1574, 1630, 1717, 1778, 1815, 1826, 1879, 1969, 2040, 2091, 2121, 2150, 2187, 2215, 2233, 2242, 2240, 2229, 2209, 2178, 2138, 2088 },
                                                    { 1262, 1351, 1441, 1518, 1570, 1597, 1663, 1752, 1817, 1857, 1872, 1911, 1996, 2061, 2106, 2130, 2162, 2200, 2229, 2248, 2258, 2257, 2247, 2227, 2197, 2158, 2109 },
                                                    { 1344, 1439, 1518, 1580, 1617, 1629, 1698, 1790, 1858, 1901, 1919, 1946, 2025, 2084, 2123, 2142, 2174, 2213, 2243, 2263, 2273, 2273, 2264, 2245, 2216, 2178, 2129 },
                                                    { 1438, 1540, 1604, 1652, 1674, 1672, 1735, 1831, 1901, 1948, 1969, 1983, 2056, 2109, 2142, 2155, 2185, 2225, 2256, 2276, 2287, 2289, 2280, 2262, 2234, 2197, 2149 },
                                                    { 1533, 1617, 1685, 1704, 1713, 1710, 1784, 1854, 1915, 1969, 2016, 2035, 2078, 2116, 2149, 2177, 2203, 2241, 2270, 2290, 2301, 2303, 2295, 2279, 2253, 2218, 2173 },
                                                    { 1599, 1682, 1738, 1755, 1761, 1757, 1811, 1880, 1941, 1995, 2041, 2075, 2113, 2146, 2173, 2195, 2219, 2258, 2288, 2309, 2320, 2322, 2315, 2299, 2274, 2240, 2196 },
                                                    { 1648, 1730, 1784, 1799, 1804, 1798, 1838, 1907, 1968, 2022, 2068, 2113, 2145, 2173, 2195, 2212, 2235, 2275, 2305, 2326, 2338, 2341, 2335, 2320, 2295, 2261, 2219 },
                                                    { 1681, 1763, 1823, 1837, 1840, 1833, 1868, 1936, 1997, 2050, 2095, 2148, 2176, 2198, 2214, 2226, 2251, 2291, 2322, 2344, 2357, 2360, 2355, 2340, 2316, 2283, 2241 },
                                                    { 1698, 1778, 1857, 1869, 1870, 1861, 1898, 1966, 2027, 2079, 2125, 2182, 2204, 2220, 2232, 2238, 2266, 2307, 2339, 2361, 2375, 2379, 2374, 2360, 2337, 2304, 2263 },
                                                    { 1708, 1806, 1874, 1895, 1902, 1895, 1937, 1979, 2030, 2090, 2160, 2212, 2224, 2234, 2242, 2249, 2268, 2315, 2351, 2378, 2393, 2399, 2394, 2378, 2352, 2316, 2269 },
                                                    { 1721, 1823, 1895, 1918, 1928, 1923, 1959, 2000, 2050, 2109, 2177, 2225, 2238, 2248, 2257, 2264, 2283, 2330, 2365, 2390, 2405, 2409, 2403, 2387, 2359, 2322, 2274 },
                                                    { 1733, 1839, 1912, 1938, 1950, 1948, 1979, 2019, 2068, 2126, 2193, 2237, 2250, 2261, 2271, 2279, 2302, 2347, 2381, 2405, 2419, 2422, 2415, 2397, 2369, 2330, 2281 },
                                                    { 1744, 1853, 1926, 1955, 1969, 1969, 1997, 2036, 2084, 2141, 2207, 2247, 2261, 2273, 2283, 2292, 2322, 2366, 2399, 2422, 2435, 2437, 2429, 2410, 2381, 2341, 2291 },
                                                    { 1752, 1866, 1937, 1968, 1985, 1987, 2013, 2051, 2098, 2154, 2219, 2255, 2270, 2283, 2294, 2303, 2345, 2387, 2420, 2442, 2453, 2454, 2445, 2425, 2395, 2354, 2303 },
                                                    { 1764, 1867, 1950, 1977, 1995, 2003, 2025, 2070, 2118, 2170, 2225, 2265, 2266, 2277, 2299, 2332, 2351, 2403, 2443, 2469, 2482, 2482, 2469, 2443, 2403, 2350, 2284 },
                                                    { 1776, 1878, 1954, 1984, 2003, 2013, 2035, 2079, 2126, 2176, 2230, 2267, 2271, 2285, 2310, 2346, 2374, 2427, 2466, 2493, 2506, 2507, 2494, 2467, 2428, 2375, 2309 },
                                                    { 1786, 1887, 1958, 1989, 2010, 2021, 2044, 2086, 2132, 2181, 2233, 2268, 2275, 2292, 2320, 2359, 2395, 2448, 2488, 2515, 2529, 2529, 2516, 2490, 2451, 2399, 2333 },
                                                    { 1794, 1894, 1960, 1993, 2016, 2029, 2052, 2093, 2137, 2185, 2236, 2268, 2278, 2299, 2330, 2372, 2414, 2468, 2508, 2535, 2549, 2550, 2537, 2512, 2473, 2420, 2355 },
                                                    { 1801, 1898, 1961, 1996, 2020, 2035, 2059, 2099, 2142, 2188, 2238, 2268, 2281, 2305, 2339, 2384, 2432, 2486, 2526, 2554, 2568, 2569, 2556, 2531, 2492, 2440, 2375 },
                                                    { 1805, 1901, 1961, 1997, 2023, 2039, 2065, 2104, 2145, 2190, 2239, 2268, 2284, 2310, 2348, 2396, 2448, 2502, 2543, 2570, 2585, 2586, 2574, 2549, 2510, 2459, 2394 },
                                                    { 1807, 1902, 1960, 1997, 2025, 2043, 2071, 2108, 2148, 2191, 2238, 2267, 2286, 2315, 2356, 2407, 2462, 2516, 2557, 2585, 2600, 2601, 2590, 2565, 2527, 2475, 2411 },
                                                    { 1807, 1900, 1957, 1996, 2026, 2045, 2075, 2111, 2150, 2192, 2238, 2265, 2287, 2320, 2363, 2417, 2474, 2529, 2570, 2598, 2613, 2615, 2604, 2579, 2541, 2490, 2426 },
                                                    { 1805, 1897, 1953, 1994, 2025, 2046, 2079, 2113, 2150, 2191, 2236, 2263, 2288, 2324, 2370, 2427, 2485, 2540, 2581, 2610, 2625, 2627, 2616, 2591, 2554, 2503, 2439 },
                                                    { 1800, 1891, 1948, 1990, 2023, 2046, 2081, 2114, 2150, 2190, 2233, 2260, 2288, 2327, 2376, 2436, 2494, 2549, 2591, 2619, 2635, 2637, 2626, 2602, 2565, 2514, 2450 },
                                                    { 1794, 1884, 1941, 1985, 2020, 2044, 2083, 2114, 2149, 2188, 2229, 2257, 2288, 2329, 2382, 2445, 2501, 2556, 2598, 2627, 2643, 2646, 2635, 2611, 2574, 2523, 2460 },
                                                    { 1786, 1874, 1933, 1979, 2015, 2041, 2084, 2114, 2147, 2184, 2225, 2253, 2287, 2332, 2387, 2453, 2506, 2562, 2604, 2633, 2649, 2652, 2642, 2618, 2581, 2531, 2468 },
                                                    { 1776, 1863, 1924, 1971, 2009, 2037, 2083, 2112, 2145, 2180, 2219, 2248, 2285, 2333, 2392, 2461, 2510, 2566, 2608, 2638, 2654, 2657, 2647, 2623, 2587, 2537, 2474 },
                                                    { 1764, 1849, 1913, 1963, 2002, 2031, 2082, 2110, 2141, 2175, 2213, 2243, 2283, 2334, 2396, 2468, 2512, 2568, 2611, 2640, 2657, 2660, 2650, 2627, 2591, 2541, 2478 }};
        readonly double[] sampleDataColHeaders4 = { 200, 400, 650, 800, 1000, 1200, 1600, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 6000, 7000, 8000 }; // X
        readonly double[] sampleDataRowHeaders4 = { 0, 1, 2, 3, 4, 5, 6, 7, 8 }; // Y
        readonly string[] sampleDataRowHeadersText4 = { "1st Gear", "2nd Gear", "3rd Gear", "4th Gear", "5th Gear", "6th Gear", "Neutral", "Park", "Reverse" };
        readonly double[,] sampleTableData4 = {{ 120, 45, 25, 25, 25, 10, -10, -22, -22, -25, -29, -30, -30, -30, -30, -30, -30 },
                                                { 120, 45, 25, 22, 11, -10, -14, -22, -22, -25, -29, -30, -30, -30, -30, -30, -30 },
                                                { 120, 45, 25, 22, 11, -10, -14, -22, -22, -25, -29, -30, -30, -30, -30, -30, -30 },
                                                { 120, 45, 25, 22, 11, -10, -18, -25, -26, -29, -32, -32, -32, -32, -32, -32, -32 },
                                                { 120, 45, 25, 22, 11, -10, -20, -27, -30, -32, -34, -34, -34, -34, -34, -34, -34 },
                                                { 120, 45, 25, 22, 11, -10, -20, -27, -30, -32, -34, -34, -34, -34, -34, -34, -34 },
                                                { 100, 45, 10, 0, -10, -25, -40, -62, -62, -65, -90, -90, -95, -105, -115, -120, -120 },
                                                { 120, 45, 25, 25, 25, 10, -10, -22, -22, -25, -29, -30, -30, -30, -30, -30, -30 },
                                                { 100, 45, 10, 0, -10, -25, -40, -62, -62, -65, -90, -90, -95, -105, -115, -120, -120 }};
    #endregion
    #endregion

        //------------------------- Constructor, General ------------------------------------------------------------------------------
        #region
        public TableEditor3D()
            {
                InitializeComponent();

                UserSettings_Load();
            }

        public void Initialise() // Create class instance, set properties, then call this method on the main form isHandleCreated event
        {
            // Add the scroll bar controls to this class so it can be passed down to the dgv which acts as a gateway
            // to the scrollbars and headers 
            if (UseMyScrollBars)
                ScrollBarControls = new ScrollBarCntrls
                {
                    vScrollBar = vScrollBar,
                    hScrollBar = hScrollBar,
                    splitContainer = splitContainer1
                };

            if (UseMyScrollBars)
                DgvHeaderControls = new DgvHeaderCntrls
                {
                    RowHeader = rowHeader,
                    ColHeader = colHeader,
                    BlankingPanel = blankingPanel,
                    vScrollBar = vScrollBar,
                    hScrollBar = hScrollBar,
                    splitContainer = splitContainer1
                };

            dgvCtrl = new DgvCtrl
            {
                Dgv = DgvTable,
                UseMyScrollBars = UseMyScrollBars,
                ScrollBarCntrls = ScrollBarControls,
                DgvHeaderCntrls = DgvHeaderControls,
                UndoEnabled     = UndoEnabled,
                CopyPasteEnabled = CopyPasteEnabled,
                ColourTheme  = ColourTheme,
                InstanceName = InstanceName
            };
            dgvCtrl.Initialise(); // Sets up the dgvCtrl class

            if (Graph3dEnabled)
            {
                graph3dCtrl    = new Graph3dCtrl(Graph3d_UserControl, InstanceName);
                dgvGrph3dIntfc = new DgvToGraph3d(dgvCtrl, graph3dCtrl, InstanceName);
                Graph3d_Initialise();
            }

            // Form button visibilty
            if (UndoEnabled) dgvCtrl.FormButton_UndoEnabled += DgvCtrl_FormButton_UndoEnabled;
            if (UndoEnabled) dgvCtrl.FormButton_RedoEnabled += DgvCtrl_FormButton_RedoEnabled;

            // Assign the context menu strip
            this.ContextMenuStrip = contextMenuStrip;

            // Subscribe to the user settings event, all instances use the same user settings
            UserSettings.PushSettings += UserSettings_Load;

            // Sets up the split container initial state
            SplitContainer_Initialise();

            // Timer, set up is done via the designer
            Main_Timer.Start(); // 200ms
        }

        private void Graph3d_Initialise()
        {
            // Focus events
            dgvCtrl.dgv.MouseEnter += Dgv_MouseEnter;
            dgvCtrl.dgv.MouseLeave += Dgv_MouseLeave;
            graph3dCtrl.graph3d.MouseEnter += Graph3d_MouseEnter;
            graph3dCtrl.graph3d.MouseLeave += Graph3d_MouseLeave;

            // Dgv left click to cancel point select mode buttons
            dgvCtrl.dgv.MouseClick += Dgv_MouseClick;

            // Sets splitter to the edge of the dgv
            splitContainer1.DoubleClick += SplitContainer1_DoubleClick;

            // Graph3d keyboard events for point selection mode buttons
            graph3dCtrl.graph3d.KeyDown += Graph3d_KeyDown;
            graph3dCtrl.graph3d.KeyUp += Graph3d_KeyUp;

            // Editor3D backcolor done here so I can see it in the form designer
            Graph3d_UserControl.BackColor = SystemColors.Control;

            graph3dCtrl.Zoom = Zoom;
            graph3dCtrl.Elevation = Elevation;
            graph3dCtrl.Rotation = Rotation;
            graph3dCtrl.RotationTransposed = RotationTransposed;
            graph3dCtrl.ShowAxis = ShowAxis;
            graph3dCtrl.ShowAxisLabels = ShowAxisLabels;
            dgvGrph3dIntfc.MirrorPoints = MirrorPoints;
            graph3dCtrl.MirrorPoints = MirrorPoints;
            //graph3dCtrl.ShowToolTip =
            graph3dCtrl.ShowGraphPosition = ShowGraphPosition;
            graph3dCtrl.SelectPointColour = GraphPointsColour;
            graph3dCtrl.SelectPointSize = GraphPointSize;
            //graph3dCtrl.ShowLegend = 
            //graph3dCtrl.MirrorXAxis = 
            //graph3dCtrl.MirrorYAxis = 
            graph3dCtrl.InstanceName = InstanceName;
        }

        private void SplitContainer_Initialise()
        {
            if (!UseMyScrollBars)
            {
                splitContainer1.Panel2Collapsed = true;
                return;
            }

            // Splitcontainer minimum panel sizes. Panel 2 min size is also the graph3d min size
            splitContainer1.Panel1MinSize = Init.SplitContainerPanel1MinSize;
            splitContainer1.Panel2MinSize = Init.SplitContainerPanel2MinSize;

            // Initial splitter distance on form opening
            splitContainer1.SplitterDistance = Init.SplitContainerSplitterDistance;

            // Split container initial opening state, controlled by user setting 'Show graph panel on start'
            if (Graph3dEnabled && ShowGraphPanel)
            {
                splitContainer1.Panel2Collapsed = false;
                graphDisplay = GraphDisplay.Right; // current display state
            }
            else
            {
                splitContainer1.Panel2Collapsed = true;
            }
        }

        // Splitter distance calc
        public int CalcSplitterDistance()
        {
            // Ideally want to open splitter up onto the edge of the dgv. Also need to check if the vertical scroll bar
            // is showing.
            int scrlBarWdth = 0;
            if (UseMyScrollBars)
                scrlBarWdth = vScrollBar.Visible ? vScrollBar.Width : 0;

            int calc1 = ClientRectangle.Width - Init.Graph3dMinimumSize.Width - splitContainer1.SplitterWidth;
            int calc2 = dgvCtrl.DgvSize.Width + splitContainer1.SplitterWidth + scrlBarWdth;

            int result = 0;
            if (dgvCtrl.DgvHasData)
            {
                if (calc1 > calc2)
                {
                    if (calc2 > 0)
                        result = calc2;
                }
                else
                {
                    if (calc1 > 0)
                        result = calc1;
                }
            }

            if (DebugSplitContainer)
            {
                Console.WriteLine($"{InstanceName} - SplitterDistance {result}");
                Console.WriteLine($"{InstanceName} - calc1 {calc1}, calc2 {calc2}");
            }

            return result;
        }

        // String override to display instance name in the designer locals window
        public override string ToString()
        {
            return InstanceName;
        }

        // Loads the user settings that are set in the user settings dialog
        public void UserSettings_Load()
        {
            userSettings = new UserSettings();

            ShowSamples = userSettings.ShowSamples;
            ShowButtonToolTips = userSettings.ShowButtonToolTips;
            ShowGraphPanel = userSettings.ShowGraphPanelOnStart;
            ShowAxis = userSettings.ShowAxis;
            ShowAxisLabels = userSettings.ShowAxisLabels;
            MirrorPoints = userSettings.MirrorPoints;
            ShowGraphPosition = userSettings.ShowGraphPosition;
            GraphPointsColour = userSettings.GraphPointsColour;
            GraphPointSize = userSettings.GraphPointSize;
            SelectRadius = userSettings.SelectRadius;
            Rotation = userSettings.Rotation;
            RotationTransposed = userSettings.RotationTransposed;
            Elevation = userSettings.Elevation;
            Zoom = userSettings.Zoom;
        }

        public void KillTimers()
        {
            // Call from the main form closing event, this function stops any timers that may be running. As these
            // timers are running on a different thread if they are not stopped they continue to run after the
            // application has closed. 

            // temp
            //ton.Stop();

            // MyEvent timers
            dgvCtrl.myEvents.tmr_DgvSizeChanged_Intermittent.Stop();
            dgvCtrl.myEvents.tmr_DgvDataChanged_Debounced.Stop();

            // Inc dec timer
            dgvCtrl.incDecTask.tmr_IncDec.Stop();

            // Dgv to graph3d interface
            if (Graph3dEnabled)
                dgvGrph3dIntfc.tmr_zValuesToDgv_Intermittent.Stop();
        }
        #endregion

        //------------------------- Main Task Timer -----------------------------------------------------------------------------------
        #region
        private void Main_Timer_Tick(object sender, EventArgs e)
        {
            // ----------------------------------------------------
            // Transpose xy button
            // ----------------------------------------------------
            // Monitor transpose state for the button highlight
            if (Graph3dEnabled && !graph3dCtrl.TransposeXY)
                btn_Plot3d_TransposeXY.ImageIndex = 4;
            else
                btn_Plot3d_TransposeXY.ImageIndex = 5;

            // ----------------------------------------------------
            // Average tool button
            // ----------------------------------------------------
            // Must have axis headers to use this tool
            if (AverageEnabled && dgvCtrl.DgvHasData)
                btn_AverageTool.Enabled = true;
            else
                btn_AverageTool.Enabled = false;

            // ----------------------------------------------------
            // Point move button
            // ----------------------------------------------------
            // Enables point move button if dgv myPoints is > 0
            if (Graph3dEnabled && UseToolBar && dgvGrph3dIntfc.dgv_Selections.Count() > 0)
            {
                // Enables point move button
                btn_Graph3d_PointMoveMode.Enabled = true;
            }
            else
            {
                // Disables point move button
                btn_Graph3d_PointMoveMode.Enabled = false;

                // And also turns off the point move mode
                if (Graph3dEnabled)
                    Graph3dPointMoveMode = false;
            }

            // ----------------------------------------------------
            // Scrollbars disabled
            // ----------------------------------------------------
            if (!UseMyScrollBars)
            {
                rowHeader.Visible = false;
                colHeader.Visible = false;
                blankingPanel.Visible = false;
                hScrollBar.Visible = false;
                vScrollBar.Visible = false;
            }

            // ----------------------------------------------------
            // Graph3d disabled
            // ----------------------------------------------------
            if (!Graph3dEnabled || !UseToolBar)
            {
                // Disable related graph3d buttons
                btn_Graph3D_Instructions.Enabled = false;
                btn_Graph3d_PointMoveMode.Enabled = false;
                btn_Graph3d_PointSelectMode.Enabled = false;
                btn_Graph3D_ResetView.Enabled = false;
                btn_Plot3d_TransposeXY.Enabled = false;
                btn_RotateGraphDockedLocation.Enabled = false;
            }

            // ----------------------------------------------------
            // Undo button
            // ----------------------------------------------------
            if (!UndoEnabled || !UseToolBar)
            {
                btn_Undo.Enabled = false;
                btn_Redo.Enabled = false;
            }

            // ----------------------------------------------------
            // Copy / paste buttons
            // ----------------------------------------------------
            if (!CopyPasteEnabled || !UseToolBar)
            {
                btn_Undo.Enabled = false;
                btn_Paste.Enabled = false;
            }
        }
        #endregion

        //------------------------- Events --------------------------------------------------------------------------------------------
        #region
        public void FormResizedToMaximum()
        {
            if (DebugSplitContainer)
                Console.WriteLine($"{InstanceName} - FormResizedToMaximum() {DgvSize.Width}");

            splitContainer1.SplitterDistance = CalcSplitterDistance();
        }

        private void DgvCtrl_FormButton_UndoEnabled(object sender, bool e)
        {
            btn_Undo.Enabled = e;
        }

        private void DgvCtrl_FormButton_RedoEnabled(object sender, bool e)
        {
            btn_Redo.Enabled = e;
        }

        private void DgvCtrl_FormButton_AverageEnabled(object sender, bool e)
        {
            btn_AverageTool.Enabled = e;
        }

        private void SplitContainer1_DoubleClick(object sender, EventArgs e)
        {
            if (DebugSplitContainer)
                Console.WriteLine($"{InstanceName} - SplitContainer1_DoubleClick() {DgvSize.Width}");

            splitContainer1.SplitterDistance = CalcSplitterDistance();
        }

        private void Dgv_MouseEnter(object sender, EventArgs e)
        {
            dgvCtrl.dgv.Focus();
        }

        private void Dgv_MouseLeave(object sender, EventArgs e)
        {
            if (DebugMouse)
                Console.WriteLine($"{InstanceName} - Dgv_MouseLeave()");

            if (!this.contextMenuStrip.Visible)
                dgvGrph3dIntfc.ClearHoverPoints();

            this.Focus();
        }

        private void Graph3d_MouseEnter(object sender, EventArgs e)
        {
            graph3dCtrl.graph3d.Focus();
        }

        private void Graph3d_MouseLeave(object sender, EventArgs e)
        {
            dgvGrph3dIntfc.ClearHoverPoints();

            this.Focus();
        }

        private void Dgv_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Left click in the dgv cancels the point modes
                Graph3dPointSelectMode = false;
                Graph3dPointMoveMode = false;

                // Disables point move button
                //btn_Graph3d_PointMoveMode.Enabled = false;

                // Resets point select backcolour
                btn_Graph3d_PointSelectMode.ImageIndex = 6;
            }
        }

        private void Graph3d_KeyDown(object sender, KeyEventArgs e)
        {
            // If alt key is pressed reset point select mode. This also knocks out point move mode
            if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu)
            {
                // Turns off point select mode
                Graph3dPointSelectMode = false;

                // Turns off point move mode
                Graph3dPointMoveMode = false;

                // Disable point select mode button. When alt key is released it will re-enable
                btn_Graph3d_PointSelectMode.Enabled = false;

                // Disable point move mode button because the point select mode is off
                btn_Graph3d_PointMoveMode.Enabled = false;

                // Resets point select backcolour
                btn_Graph3d_PointSelectMode.ImageIndex = 6;
            }
        }

        private void Graph3d_KeyUp(object sender, KeyEventArgs e)
        {
            // Re-enable point select mode button if the alt key is released
            if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu)
                btn_Graph3d_PointSelectMode.Enabled = true;
        }
        #endregion

        //------------------------- Context Menu --------------------------------------------------------------------------------------
        #region
        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (DebugMouse)
                Console.WriteLine($"{InstanceName} - contextMenuStrip_Opening()");

            // Shot gun disable all items
            ToolStrip_CopyWithAxis.Enabled = false;
            ToolStrip_Copy.Enabled = false;
            ToolStrip_Paste.Enabled = false;
            ToolStrip_PasteWithXYAxis.Enabled = false;
            ToolStrip_PasteSpecial.Enabled = false; // hides access to paste special sub menu
            ToolStrip_AddtnlPasteFcns.Enabled = false; // hides access to additional functions sub menu

            if (CopyPasteEnabled)
            {
                if (dgvCtrl.DgvHasData)
                {
                    if (copyPasteMode == CopyPasteMode.All)
                    {
                        ToolStrip_CopyWithAxis.Enabled = true;
                        ToolStrip_Copy.Enabled = true;
                        ToolStrip_PasteSpecial.Enabled = true;
                        ToolStrip_PasteWithXYAxis.Enabled = true;
                        ToolStrip_Paste.Enabled = true;
                        ToolStrip_AddtnlPasteFcns.Enabled = true;

                        // Additional functions sub menu
                        ToolStrip_PasteXAxisPCMTEC.Enabled = true;
                        ToolStrip_PasteXAxis.Enabled = true;
                        ToolStrip_PasteYAxis.Enabled = true;
                        ToolStrip_ClearTable.Enabled = true;
                    }
                    else if (copyPasteMode == CopyPasteMode.Copy)
                    {
                        ToolStrip_CopyWithAxis.Enabled = true;
                        ToolStrip_Copy.Enabled = true;
                    }
                }
                else // The dgv is empty, only items that support building a new table are enabled
                {
                    ToolStrip_PasteWithXYAxis.Enabled = true;
                    ToolStrip_AddtnlPasteFcns.Enabled = true;

                    // Limit options on additional functions sub menu
                    ToolStrip_PasteXAxisPCMTEC.Enabled = false;
                    ToolStrip_PasteXAxis.Enabled = false;
                    ToolStrip_PasteYAxis.Enabled = false;
                    ToolStrip_PasteTable.Enabled = true;
                    ToolStrip_PasteTableWithRowAxis.Enabled = true;
                    ToolStrip_PasteTableWithColAxis.Enabled = true;
                    ToolStrip_ClearTable.Enabled = false;
                }
            }
        }

        private void CopyWithAxis_Click(object sender, EventArgs e)
        {
            Copy.CopyClipboard(dgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Include, UseMyScrollBars); // dgv.CopyClipboard_WithAxis();
        }

        private void CopyWithNoAxis_Click(object sender, EventArgs e)
        {
            Copy.CopyClipboard(dgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, UseMyScrollBars);
        }

        private void PasteTableWithXYAxis_Click(object sender, EventArgs e)
        {
            //MyStopWatch stopWatch = new MyStopWatch();

            //if (DebugForm)
            //    stopWatch.Start();

            DgvNumberFormat.CelLckOut = false;

            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteTableWithXYAxis);

            //if (DebugForm)
            //{
            //    Console.WriteLine($"{InstanceName} - PasteTableWithXYAxis {stopWatch.Get()}");
            //    stopWatch.Stop();
            //}
        }

        private void PasteTableWithXAxis_Click(object sender, EventArgs e)
        {
            DgvNumberFormat.CelLckOut = false;

            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteTableWithXAxis);
        }

        private void PasteTableWithYAxis_Click(object sender, EventArgs e)
        {
            DgvNumberFormat.CelLckOut = false;

            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteTableWithYAxis);
        }

        private void PasteTableWithNoAxis_Click(object sender, EventArgs e)
        {
            DgvNumberFormat.CelLckOut = false;

            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteTableWithNoAxis);

            dgvCtrl.Refresh(RefreshMode.All);
        }

        private void Paste_Click(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteToCurrentCell);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }

        private void PasteXAxis_PCMTEC_Click(object sender, EventArgs e)
        {
            PasteWithXAxisPcmtecDialog dialog = new PasteWithXAxisPcmtecDialog(dgvCtrl);

            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            dialog.ShowDialog();

            if (dialog.X_Axis != null)
            {
                dgvCtrl.WriteColHeaderLabels(dialog.X_Axis, dialog.NumberFormat);
            }

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);
        }

        private void PasteXAxis_Click(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteXAxis);

            dgvCtrl.Refresh(RefreshMode.All);
        }

        private void PasteYAxis_Click(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteYAxis);

            dgvCtrl.Refresh(RefreshMode.All);
        }

        private void Paste_MultiplyByPercent(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteSpecial_MultiplyByPercent);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }

        private void Paste_MultiplyByPercentHalf(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteSpecial_MultiplyByPercentHalf);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }

        private void Paste_Add(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteSpecial_Add);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }

        private void Paste_Subtract(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteSpecial_Subtract);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }

        private void ClearTable_Click(object sender, EventArgs e)
        {
            DgvNumberFormat.CelLckOut = false;

            dgvCtrl.myEvents.Pause_SelectionToGraph3d();

            dgvCtrl.ResetDataTable();

            dgvCtrl.StyleOverrides(dgvCtrl.dgv);

            graph3dCtrl.Reset();

            dgvCtrl.myEvents.Resume_SelectionToGraph3d();
        }

        private void btn_Copy_Click(object sender, EventArgs e)
        {
            Copy.CopyClipboard(dgvCtrl, Copy.SelectMode.SelectedCells, Copy.Headers.Exclude, UseMyScrollBars);
        }

        private void btn_Paste_Click(object sender, EventArgs e)
        {
            dgvCtrl.paste.ParseClipboardToDgv(dgvCtrl, Paste.Mode.PasteToCurrentCell);

            dgvCtrl.Refresh(RefreshMode.StyleWidthSize);
        }
        #endregion

        //------------------------- Samples -------------------------------------------------------------------------------------------
        #region
        private void ShowSampleButtons()
        {
            if (ShowSamples)
            {
                btn_LoadSample1.Show();
                btn_LoadSample2.Show();
                btn_LoadSample3.Show();
                btn_LoadSample4.Show();
            }
            else
            {
                btn_LoadSample1.Hide();
                btn_LoadSample2.Hide();
                btn_LoadSample3.Hide();
                btn_LoadSample4.Hide();
            }
        }

        public void btn_LoadSample1_Click(object sender = null, EventArgs e = null) // Missing VTT 
        {
            // Load the sample data to the dgv
            dgvCtrl.WriteToDataGridView(sampleDataRowHeaders, sampleDataColHeaders, sampleTableData, RefreshMode.All);

            if (UseMyScrollBars)
            {
                dgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(sampleDataRowHeaders));
                dgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(sampleDataColHeaders));
            }

            dgvCtrl.SetCellWidths();
        }

        public void btn_LoadSample2_Click(object sender = null, EventArgs e = null) // VTT table
        {
            // Load the sample data to the dgv
            dgvCtrl.WriteToDataGridView(sampleDataRowHeaders, sampleDataColHeaders, sampleTableData2, RefreshMode.All);

            if (UseMyScrollBars)
            {
                dgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(sampleDataRowHeaders));
                dgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(sampleDataColHeaders));
            }

            dgvCtrl.SetCellWidths();
        }

        public void btn_LoadSample3_Click(object sender = null, EventArgs e = null) // VE table
        {
            // Load the sample data to the dgv
            dgvCtrl.WriteToDataGridView(sampleDataRowHeaders3, sampleDataColHeaders3, sampleTableData3, RefreshMode.All);

            if (UseMyScrollBars)
            {
                dgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(DgvData.ConvertNumericHeadersToText(sampleDataRowHeaders3));
                dgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(sampleDataColHeaders3));
            }

            dgvCtrl.SetCellWidths();
        }

        public void btn_LoadSample4_Click(object sender = null, EventArgs e = null) // Throttle follower
        {
            // Load the sample data to the dgv
            dgvCtrl.WriteToDataGridView(sampleDataRowHeaders4, sampleDataColHeaders4, sampleTableData4, RefreshMode.All);

            dgvCtrl.dgvHeaders.WriteScrollBarRowHeaders(sampleDataRowHeadersText4);
            dgvCtrl.dgvHeaders.WriteScrollBarColHeaders(DgvData.ConvertNumericHeadersToText(sampleDataColHeaders4));

            dgvCtrl.SetCellWidths();
        }
        #endregion

        //------------------------- Table interpolation -------------------------------------------------------------------------------
        #region
        private void btn_FillMissingDataGaps_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                dgvCtrl.WriteToDataTable(LinearInterpolation.AutoInterpolate(dgvCtrl.ReadDataTable()));

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                dgvCtrl.Refresh(RefreshMode.ColourOnly);
            }
        }

        private void btn_MissingNeighbourFill_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                dgvCtrl.WriteToDataTable(LinearInterpolation.MissingNeighbour(dgvCtrl.ReadDataTable()));

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                dgvCtrl.Refresh(RefreshMode.ColourOnly);
            }
        }

        private void btn_All_Smooth_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            MovingAverage.Smooth(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, MovingAverage.Mode.All);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }

        private void btn_H_Smooth_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            MovingAverage.Smooth(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, MovingAverage.Mode.Horizontal);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }

        private void btn_V_Smooth_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            MovingAverage.Smooth(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, MovingAverage.Mode.All);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }

        private void btn_H_Interpolate_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            LinearInterpolation.Interpolate(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, LinearInterpolation.Mode.Horizontal);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }

        private void btn_V_Interpolate_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            LinearInterpolation.Interpolate(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, LinearInterpolation.Mode.Vertical);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }

        private void btn_All_Interpolate_Click(object sender, EventArgs e)
        {
            dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

            LinearInterpolation.Interpolate(dgvCtrl.dgv, dgvCtrl.SelectedCellCollection, LinearInterpolation.Mode.All);

            dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

            dgvCtrl.Refresh(RefreshMode.ColourOnly);
        }
        #endregion

        //------------------------- Math buttons --------------------------------------------------------------------------------------
        #region
        private void btn_SetSelectedCellsValue_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, adjustValue);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.Partial);
            }
        }

        private void btn_Multiply_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    double.TryParse(cell.Value.ToString(), out double cellValue);
                    cellValue *= adjustValue;

                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void btn_Divide_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                if (adjustValue == 0) return; // divide by 0!

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    double.TryParse(cell.Value.ToString(), out double cellValue);
                    cellValue /= adjustValue;

                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void btn_Add_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    double.TryParse(cell.Value.ToString(), out double cellValue);
                    cellValue += adjustValue;

                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void btn_Subtract_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    double.TryParse(cell.Value.ToString(), out double cellValue);
                    cellValue -= adjustValue;

                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, cellValue);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void btn_ClipMax_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    // Read
                    double value = (double)dgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

                    // Adjust
                    if (value > adjustValue)
                    {
                        value = adjustValue;
                    }

                    // Write
                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void btn_ClipMin_Click(object sender, EventArgs e)
        {
            if (dgvCtrl.DgvHasData && dgvCtrl.SelectedCellCollection.Count > 0 && textBox_Adjust.Text.Length > 0)
            {
                dgvCtrl.Events_CellDataAndSelectionChanged_Pause();

                double.TryParse(textBox_Adjust.Text, out double adjustValue);

                foreach (DataGridViewCell cell in dgvCtrl.SelectedCellCollection)
                {
                    double value = (double)dgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

                    if (value < adjustValue)
                    {
                        value = adjustValue;
                    }

                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
                }

                dgvCtrl.Events_CellDataAndSelectionChanged_Resume(true);

                // Only the cell format needs adjusting
                dgvCtrl.dgvNumFormat.Target = FormatTarget.Cells;

                dgvCtrl.Refresh(RefreshMode.WidthColour);
            }
        }

        private void textBox_Adjust_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the entered key is a digit, a decimal point, a + or - key, or the backspace key
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.') &&
                (e.KeyChar != '+') && (e.KeyChar != '-'))
            {
                e.Handled = true; // Ignore the key press
            }

            // If a decimal point is already present, allow only one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true; // Ignore the key press
            }
        }

        private void btn_AverageTool_Click(object sender = null, EventArgs e = null)
        {
            if (!AverageEnabled)
                return;

            // Create a new average tool if not already loaded
            if (averageTool == null)
                averageTool = new AverageTool();

            // Check for new dimensions. If different or first load, load a new table
            if (dgvCtrl.ReadRowHeaders().Length != averageTool.RowHeaders?.Length || averageTool.RowHeaders == null ||
                dgvCtrl.ReadColHeaders().Length != averageTool.ColHeaders?.Length || averageTool.ColHeaders == null)
            {
                averageTool.LoadTable(dgvCtrl.ReadRowHeaders(), dgvCtrl.ReadColHeaders(), DgvNumberFormat, true);
            }

            // If there is new axis headers, load those
            if (!dgvCtrl.ReadRowHeaders().SequenceEqual(averageTool.RowHeaders) || !dgvCtrl.ReadColHeaders().SequenceEqual(averageTool.ColHeaders))
            {
                averageTool.LoadTable(dgvCtrl.ReadRowHeaders(), dgvCtrl.ReadColHeaders(), DgvNumberFormat, false);
            }

            averageTool.Show();
        }
        #endregion

        //------------------------- Inc dec dp buttons --------------------------------------------------------------------------------
        #region
        private void btn_IncDp_Click(object sender, EventArgs e)
        {
            dgvCtrl.AdjustDecimalPlaces(DpDirection.Increment);
        }

        private void btn_DecDp_Click(object sender, EventArgs e)
        {
            dgvCtrl.AdjustDecimalPlaces(DpDirection.Decrement);
        }
        #endregion

        //------------------------- Graph3D button clicks -----------------------------------------------------------------------------
        #region
        private void btn_Graph3d_PointSelectMode_Click(object sender, EventArgs e)
        {
            // Toggle point select mode
            Graph3dPointSelectMode = !Graph3dPointSelectMode;

            if (Graph3dPointSelectMode)
            {
                // Sets point select backcolour
                btn_Graph3d_PointSelectMode.ImageIndex = 7;
            }
            else
            {
                // Resets point select backcolour
                btn_Graph3d_PointSelectMode.ImageIndex = 6;
            }

            // Enables point move button if dgv myPoints is > 0
            if (Graph3dPointSelectMode && dgvGrph3dIntfc.dgv_Selections.Count > 0)
            {
                // Enables point move button
                btn_Graph3d_PointMoveMode.Enabled = true;
            }
            else
            {
                // Disables point move button
                btn_Graph3d_PointMoveMode.Enabled = false;

                // And also turns off the point move mode
                Graph3dPointMoveMode = false;
            }
        }

        private void btn_Graph3d_PointMoveMode_Click(object sender, EventArgs e)
        {
            // Toggle point move mode
            Graph3dPointMoveMode = !Graph3dPointMoveMode;
        }

        private void btn_Graph3D_ResetView_Click(object sender, EventArgs e)
        {
            graph3dCtrl.ResetViewPosition();

            graph3dCtrl.DrawPlot();
        }

        private void btn_Graph3D_Instructions_Click(object sender, EventArgs e)
        {
            // Check if the form exists, if not the foreach loop will finish and the form
            // will be created
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm.Name == "Graph3D_Instructions")
                {
                    openForm.Focus();
                    return;
                }
            }

            Graph3D_Instructions graph3D_Instructions = new Graph3D_Instructions();

            graph3D_Instructions.Show();

            graph3D_Instructions.DeselectAllText = true;
        }

        private void btn_Plot3d_Transpose_Click(object sender, EventArgs e)
        {
            // Return if no data or not drawn
            if (!dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn)
                return;

            // Transpose bit flip flop
            transposeXY = !transposeXY;

            // Clear the hover points out
            dgvGrph3dIntfc.ClearHoverPoints();

            // Deselect all dgv points
            DgvClearSelection();

            // Deselect all graph points
            graph3dCtrl.ClearGraphSelection();

            // Refresh the axis & z values
            dgvGrph3dIntfc.X_AxisLabels = ReadFromDgv_ColLabels();
            dgvGrph3dIntfc.Y_AxisLabels = ReadFromDgv_RowLabels();
            dgvGrph3dIntfc.Z_Values = ReadFromDgv_TableData();

            graph3dCtrl.X_AxisLabels = ReadFromDgv_ColLabels();
            graph3dCtrl.Y_AxisLabels = ReadFromDgv_RowLabels();
            graph3dCtrl.Z_Values = ReadFromDgv_TableData();

            // Feed out status to the class properties
            dgvCtrl.TransposeXY = transposeXY;
            graph3dCtrl.TransposeXY = transposeXY;
            dgvGrph3dIntfc.TransposeXY = transposeXY;
        }

        private void btn_Options_Click(object sender, EventArgs e)
        {
            //UserSettings userSettingsForm = new UserSettings(); 

            userSettings.StartPosition = FormStartPosition.CenterScreen;

            userSettings.ShowDialog(); // Always on top and always has focus
        }

        private void btn_RotateGraphDockedLocation_Click(object sender, EventArgs e)
        {
            if (!graph3dCtrl.IsDrawn)
                graphDisplay = GraphDisplay.Hide;

            if (DebugSplitContainer)
                Console.WriteLine($"{InstanceName} - GraphDisplay {graphDisplay}");

            // Index increment or loop around
            if (graphDisplay == GraphDisplay.Hide)
                graphDisplay = GraphDisplay.Right;
            else
                graphDisplay++;

            switch (graphDisplay)
            {
                case GraphDisplay.Right:
                    // Get dgv width. Ideally want to open splitter up onto the edge of the dgv. Also need to check if
                    // the vertical scroll bar is showing. If there is a saved distance, use that first
                    if (lastSplitterDistance != 0)
                        splitContainer1.SplitterDistance = lastSplitterDistance;
                    else
                        splitContainer1.SplitterDistance = CalcSplitterDistance();

                    splitContainer1.Panel2Collapsed = false;
                    break;

                case GraphDisplay.Fill:
                    lastSplitterDistance = splitContainer1.SplitterDistance; // Save

                    splitContainer1.Panel1Collapsed = true;
                    break;

                case GraphDisplay.Hide:
                    splitContainer1.Panel2Collapsed = true;
                    splitContainer1.Panel1Collapsed = false;
                    break;
            }
        }
        #endregion

        //------------------------- DGV button clicks ---------------------------------------------------------------------------------
        #region
        private void btn_ClearAllSelections_Click(object sender, EventArgs e)
        {
            dgvCtrl.ClearSelection();

            if (Graph3dEnabled)
            {
                graph3dCtrl.ClearGraphSelection();
                dgvGrph3dIntfc.ClearHoverPoints();
            }
        }

        private void btn_Undo_Click(object sender, EventArgs e)
        {
            dgvCtrl.Undo_Get();
        }

        private void btn_Redo_Click(object sender, EventArgs e)
        {
            dgvCtrl.Redo_Get();
        }

        private void btn_TableEditMode_Click(object sender, EventArgs e)
        {
            // Get the current colour mode
            ColourScheme colourScheme = dgvCtrl.ColourTheme;

            // Toggle the colour mode
            if (colourScheme == ColourScheme.HpNormal || colourScheme == ColourScheme.HpEdited)
                colourScheme = ColourScheme.HpScanner;
            else
                colourScheme = ColourScheme.HpNormal;

            // Set the colour
            dgvCtrl.SetCellColour(colourScheme);
        }
        #endregion
    }
    #endregion

    public class DgvCtrl
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region
        // Parent class Instance Name
        public string ClassName { get; set; } = "DgvCtrl";
        public string InstanceName { get; set; }

        // Dgv
        public DataGridView Dgv { set { dgv = value; } }
        public Size DgvSize
        {
            get
            {
                return dgv.Size;
            }
        }
        public bool DgvHasData { get; set; }
        public int RowCount
        {
            get { return dgv.Rows.Count; }
        }
        public int ColumnCount
        {
            get { return dgv.Columns.Count; }
        }
        public bool ReadOnly { set { dgv.ReadOnly = value; } }
        public bool Focused { get { return dgv.Focused; } set { dgv.Focus(); } }
        public bool TransposeXY { get; set; }
        public ScrollBars ScrollBars { get; set; } // Dgv scroll bars property
        public TableEditor3D.ScrollBarCntrls ScrollBarCntrls { get; set; }
        public TableEditor3D.DgvHeaderCntrls DgvHeaderCntrls { get; set; }

        // Appearance
        public Color CellsForeColour
        {
            set
            {
                defaultForeColour = value;

                dgv.SuspendLayout();

                foreach (DataGridViewRow row in dgv.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.ForeColor = value;
                    }

                dgv.ResumeLayout(true);
            }
        }
        public Color CellsBackColour
        {
            set
            {
                defaultBackColour = value;

                dgv.SuspendLayout();

                foreach (DataGridViewRow row in dgv.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.BackColor = value;
                    }

                dgv.ResumeLayout(true);
            }
        }
        public Color CellsSelectionForeColour
        {
            set
            {
                defaultSelectionForeColour = value;

                dgv.SuspendLayout();

                foreach (DataGridViewRow row in dgv.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.SelectionForeColor = value;
                    }

                dgv.ResumeLayout(true);
            }
        }
        public Color CellsSelectionBackColour
        {
            set
            {
                defaultSelectionBackColour = value;

                dgv.SuspendLayout();

                foreach (DataGridViewRow row in dgv.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Style.SelectionBackColor = value;
                    }

                dgv.ResumeLayout(true);
            }
        }
        public int RowHeight { get { return dgv.Rows[0].Height; } }
        public int ColumnWidth { get { return dgv.Columns[0].Width; } }
        public int RowHeaderWidth { get { return dgv.Rows[0].HeaderCell.Size.Width; } }
        public int ColHeaderWidth { get { return dgv.Columns[0].HeaderCell.Size.Width; } }
        public string RowHeaderFormat { get { return Utils.GetNumberFormat(dgv.Rows[0].HeaderCell.Value.ToString()); } }
        public string ColHeaderFormat { get { return Utils.GetNumberFormat(dgv.Columns[0].HeaderText); } }
        public string DataTableFormat { get { return Utils.GetNumberFormat(dgv.Rows[0].Cells[0].FormattedValue.ToString()); } }
        //public string[] RowHeadersText { get; set; }
        //public string[] ColHeadersText { get; set; }
        public int hTextPadding { get; set; } = H_TEXT_PADDING;
        public int vTextPadding { get; set; } = V_TEXT_PADDING;
        public Font Font
        {
            get { return dgv.DefaultCellStyle.Font; }
            set { font = value; dgv.DefaultCellStyle.Font = value; }
        }

        // Data
        public bool Z_RemoteValuesChanging { get; set; } // myinterface -> this
        public Point TopLeftCellAddress { get { return GetTopLeftCellAddress(dgv.SelectedCells); } }
        public Point SelectedCellAddress { get; private set; }
        public DataGridViewCell CurrentCell { get { return dgv.CurrentCell; } }
        public DataGridViewSelectedCellCollection SelectedCellCollection
        {
            get { return dgv.SelectedCells; }
        }

        // Options & settings
        public bool UndoEnabled { get; set; }
        public bool CopyPasteEnabled { get; set; }
        public bool UseMyScrollBars { get; set; } // Passed down from table editor
        public ColourScheme ColourTheme { get; set; }
        public bool InitialiseDgvCtrl { set { Initialise(); } }

        // Debug, set in the main table editor class
        public bool EventDebug { get; set; }
        public bool DataChangedDebug { get; set; }
        public bool SelectionChangedDebug { get; set; }
        public bool SizeChangedDebug { get; set; }
        public bool DebugMouse { get; set; }
        public bool DgvData_Debug { get; set; }
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region Variables

        // Classes
        public Copy copy;
        public Paste paste;
        public IncDec incDecTask;
        public DgvNumFormat dgvNumFormat;
        public DataGridView dgv;
        public MyEvents myEvents;
        public ScrollBarCtrl scrollBarCtrl;
        public DgvHeadersCtrl dgvHeaders;

        // Data table
        public DataTable dt;

        //
        bool keepCellsSelectedAfterEdit = false;
        DataGridViewCell currentCell_Copy;
        DataGridViewSelectedCellCollection selectedCellsCollection_Copy;

        // Undo - redo
        public Undo undo; // Undo class
        public Redo redo; // Redo class
        public bool InitImage_ONS;

        // Events
        public event EventHandler<bool> FormButton_UndoEnabled; // to table editor
        public event EventHandler<bool> FormButton_RedoEnabled; // to table editor

        // Dgv status
        bool userCellEditPending = false;

        // Selection
        List<int> selectedRows = new List<int>();
        List<int> selectedCols = new List<int>();

        // Initial font, widths and heights. Initial values provided
        Font font = new Font("Calibri", 8.75f, FontStyle.Regular);
        const int ROW_HEIGHT = 18;
        const int COLUMN_HEADER_HEIGHT = 20;
        const int MINIMUM_ROW_HEADER_WIDTH = 42;
        const int MINIMUM_COLUMN_WIDTH = 38;
        const int H_TEXT_PADDING = 6;
        const int V_TEXT_PADDING = 2;
        const int H_BORDER_PADDING = 2;
        const int V_BORDER_PADDING = 2;
        int columnWidth = MINIMUM_COLUMN_WIDTH;
        int rowHeaderWidth = MINIMUM_ROW_HEADER_WIDTH;
        Color defaultForeColour = SystemColors.ControlText;
        Color defaultBackColour = SystemColors.Window;
        Color defaultSelectionForeColour = SystemColors.HighlightText;
        Color defaultSelectionBackColour = SystemColors.Highlight;
        #endregion

        //------------------------- Constructor ---------------------------------------------------------------------------------------
        #region
        public DgvCtrl()
        {
            // Empty constructor to enable object initialiser syntax. 
            /*
                InstanceName     = "nateDogg",
                Graph3dEnabled   = true,
                UseMyScrollBars  = true,
                HideToolBar      = false,
                UndoEnabled      = true,
                CopyPasteEnabled = true,
                AverageEnabled   = true,
                ColourTheme      = ColourScheme.HpTuners
            */

            // Call initialise too
            //nateDogg.Initialise();
        }

        public DgvCtrl(DataGridView dgv, TableEditor3D.ScrollBarCntrls scrollBarControls, bool undoEnabled, bool copyPasteEnabled, bool useMyScrollBars, ColourScheme colourTheme, string instanceName)
        {
            // Write arguments to the properties
            Dgv = dgv;
            ScrollBarCntrls = scrollBarControls;
            UndoEnabled = undoEnabled;
            CopyPasteEnabled = copyPasteEnabled;
            UseMyScrollBars = useMyScrollBars;
            ColourTheme = colourTheme;
            InstanceName = instanceName;

            Initialise();
        }

        public void Initialise()
        {
            // Name of winform user control passed in here 
            this.Dgv = dgv;

            // Underlying data table
            this.dt = new DataTable();

            // Bind the DataTable to the DataGridView
            this.dgv.DataSource = dt;

            // Events class
            myEvents = new MyEvents(this);

            // Number format class
            dgvNumFormat = new DgvNumFormat();

            // Inc dec task
            incDecTask = new IncDec(this);

            // Undo / paste
            if (UndoEnabled) undo = new Undo();
            if (UndoEnabled) redo = new Redo();
            if (CopyPasteEnabled) copy = new Copy();
            if (CopyPasteEnabled) paste = new Paste();

            //
            if (UseMyScrollBars) scrollBarCtrl = new ScrollBarCtrl(this, InstanceName);
            if (UseMyScrollBars) scrollBarCtrl.Initiate();

            // Headers class
            if (UseMyScrollBars) dgvHeaders = new DgvHeadersCtrl(this, InstanceName);

            // Debug names for the sub classes
            AssignInstanceNames();

            // Double buffers the dgv
            EnableDoubleBuffering();

            // Sets up the dt to be 1x1 on app load
            ResetDataTable();

            // My dgv style
            StyleOverrides(dgv);

            // Brings the dgv border into the dgv bounds
            SetDgvSize();

            // Always enabled
            incDecTask.IncDec_Incremental_NDR += IncDec_Incremental_NDR;
            incDecTask.IncDec_Completed_NDR += IncDec_Completed_NDR;

            // Paste notification event
            if (CopyPasteEnabled)
                paste.Paste_NDR += Paste_Completed_NDR;

            // Event fired when the undo data has been written to the dgv
            if (UndoEnabled)
                undo.NDR += Undo_Completed_NDR;

            // Row / column events used for cell selections. If scroll bar controller is in use this event is disabled
            // as the scroll bar controller will handle the clicks
            if (!UseMyScrollBars)
            {
                this.dgv.RowHeaderMouseClick += RowHeader_CellClick;
                this.dgv.ColumnHeaderMouseClick += ColHeader_CellClick;
            }

            // MyEvents - out
            myEvents.DgvDataChanged_Debounced += MyEvents_Dgv_NDR_Debounced;
            myEvents.DgvDataChanged_Immediate += MyEvents_Dgv_NDR_Immediate;
            myEvents.DgvSizeChanged_Intermittent += MyEvents_Dgv_NewSize_Intermittent;

            // Dgv - Events
            this.dgv.CellValueChanged += Dgv_CellValueChanged;
            this.dt.RowChanged += Dt_RowChanged;

            // Dgv - Cell action events
            this.dgv.CellMouseDown += Dgv_MouseDown;
            this.dgv.SelectionChanged += Dgv_SelectionChanged;
            this.dgv.SizeChanged += Dgv_SizeChanged;
            this.dgv.KeyDown += Dgv_KeyDown;

            // Dgv - Events to prevent text being entered into the cells
            this.dgv.KeyUp += Dgv_KeyUp;
            this.dgv.EditingControlShowing += Dgv_EditingControlShowing;
            this.dgv.CellEndEdit += Dgv_CellEndEdit;
            this.dgv.CellValidating += Dgv_CellValidating;
            this.dgv.CellClick += Dgv_Table_CellClick;
            this.dgv.CellDoubleClick += Dgv_Table_CellDoubleClick;
            this.dgv.MouseUp += Dgv_MouseUp;
        }
        #endregion

        //------------------------- Undo ----------------------------------------------------------------------------------------------
        #region

        public void Undo_Set(DgvData e)
        {
            // Return if not enabled, this can be set at initialisation time if undo is not required for this class instance
            if (!UndoEnabled)
                return;

            // If we have no data there is no point continuing. Or if the user is manipulating points, same deal
            if (!DgvHasData || incDecTask.Mode.Enabled || Z_RemoteValuesChanging)
            {
                if (undo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Undo returned");

                return;
            }

            // Add the current dgv state to the undo buffer
            if (undo.CanSet)
            {
                if (undo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Set()");

                // Load the data into the undo buffer
                undo.Set(e);
            }
            else
                if (undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo cannot be set");

            // Event that is subscribed to by the form to enable the undo button when undo data is on the undo stack.
            // When stack depth is > 1 I.e initial image + new image this property will be true
            FormButton_UndoEnabled?.Invoke(this, undo.CanDo);
        }

        public void Undo_Get()
        {
            // Return if not enabled, this can be set at initialisation time if undo is not required for this class instance
            if (!UndoEnabled)
                return;

            if (undo.CanDo)
            {
                if (undo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Undo Get()");

                // Load the current state to the redo stack
                Redo_Set(myEvents.BuildEventArgs_DgvDataChanged_Event());

                // Call the get function. This will make the undo data available via the undoData class properties
                DgvData undoData = undo.Get();

                // Set number format, the write to dgv function will pick this up when it runs through the refresh part.
                // There is a time saving to be had by only updating the format as needed inside the Refresh() part of
                // the write to dgv function. 
                dgvNumFormat.RowHdrFormat = undoData.RowHeaderFormat;
                dgvNumFormat.ColHdrFormat = undoData.ColHeaderFormat;
                dgvNumFormat.CellFormat = undoData.TableDataFormat;

                // Write the undo data out to the dgv, this will also trigger the new data events to run
                WriteToDataGridView(undoData.RowHeaders, undoData.ColHeaders, undoData.TableData, RefreshMode.Partial);

                // Push the update to the dgv headers
                myEvents.Req_DgvDataChanged_ToHeaders_Event(undoData);

                // All events will have finished, clear the status 
                undo.InProgress = false;
            }

            // Event that is subscribed to by the form to enable the undo button when undo data is on the undo stack.
            // When stack depth is > 1 I.e initial image + new image this property will be true
            FormButton_UndoEnabled?.Invoke(this, undo.CanDo);
        }

        public void Redo_Set(DgvData e)
        {
            // Return if not enabled, this can be set at initialisation time if undo is not required for this class instance
            if (!UndoEnabled)
                return;

            // If we have no data there is no point continuing. Or if the user is manipulating points, same deal
            if (!DgvHasData || incDecTask.Mode.Enabled || Z_RemoteValuesChanging)
            {
                if (redo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Redo returned");

                // Reset the undo in progress handshake. When this set request is called, it is confirmation the undo
                // data has been written
                //if (redo.InProgress)
                //    redo.InProgress = false;

                return;
            }

            // Add the current dgv state to the undo buffer
            if (redo.CanSet)
            {
                if (redo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Redo_Set()");

                // Load the data into the undo buffer
                redo.Set(e);
            }
            else
                if (redo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Redo cannot be set");

            // Event that is subscribed to by the form to enable the undo button when undo data is on the undo stack.
            // When stack depth is > 1 I.e initial image + new image this property will be true
            FormButton_RedoEnabled?.Invoke(this, redo.CanDo);
        }

        public void Redo_Get()
        {
            // Return if not enabled, this can be set at initialisation time if undo is not required for this class instance
            if (!UndoEnabled)
                return;

            if (redo.CanDo)
            {
                if (redo.Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Redo Get()");

                // Call the get function. This will make the undo data available via the undoData class properties
                DgvData redoData = redo.Get();

                // Load the current state back to the undo stack
                undo.Set(myEvents.BuildEventArgs_DgvDataChanged_Event());

                // Set number format, the write to dgv function will pick this up when it runs through the refresh part.
                // There is a time saving to be had by only updating the format as needed inside the Refresh() part of
                // the write to dgv function. 
                dgvNumFormat.RowHdrFormat = redoData.RowHeaderFormat;
                dgvNumFormat.ColHdrFormat = redoData.ColHeaderFormat;
                dgvNumFormat.CellFormat = redoData.TableDataFormat;

                // Write the undo data out to the dgv, this will also trigger the new data events to run
                WriteToDataGridView(redoData.RowHeaders, redoData.ColHeaders, redoData.TableData, RefreshMode.Partial);

                // Push the update to the dgv headers
                myEvents.Req_DgvDataChanged_ToHeaders_Event(redoData);
            }

            // Event that is subscribed to by the form to enable the undo button when undo data is on the undo stack.
            // When stack depth is > 1 I.e initial image + new image this property will be true
            FormButton_RedoEnabled?.Invoke(this, redo.CanDo);
        }
        #endregion

        //------------------------- DGV Formatting ------------------------------------------------------------------------------------
        #region 
        public class DgvNumFormat
        #region
        {
            public string InstanceName { get; set; }
            public bool CelLckOut { get; set; }
            public string RowHdrFormat { get; set; }
            public string ColHdrFormat { get; set; }
            public string CellFormat { get { return cellFormat; } set { if (!CelLckOut) cellFormat = value; } }
            public FormatTarget Target { get; set; }
            private string RowHdrFormat_Prev { get; set; }
            private string ColHdrFormat_Prev { get; set; }
            private string CellFormat_Prev { get; set; }

            string cellFormat;

            /// <summary>
            /// Compares the set number format(s) to the previously set number formats to determine the new number
            /// format target. Row, column or cell format properties must be set before calling Update()
            /// </summary>
            public void Update()
            {
                // Flag check to determine number format
                if (RowHdrFormat_Prev != RowHdrFormat || ColHdrFormat_Prev != ColHdrFormat || CellFormat_Prev != CellFormat)
                {
                    if (RowHdrFormat_Prev != RowHdrFormat && ColHdrFormat_Prev == ColHdrFormat && CellFormat_Prev == CellFormat)
                    {
                        Target = FormatTarget.RowHeaders;
                        goto Prev;
                    }

                    if (RowHdrFormat_Prev == RowHdrFormat && ColHdrFormat_Prev != ColHdrFormat && CellFormat_Prev == CellFormat)
                    {
                        Target = FormatTarget.ColHeaders;
                        goto Prev;
                    }

                    if (RowHdrFormat_Prev == RowHdrFormat && ColHdrFormat_Prev == ColHdrFormat && CellFormat_Prev != CellFormat)
                    {
                        Target = FormatTarget.Cells;
                        goto Prev;
                    }

                    if (RowHdrFormat_Prev != RowHdrFormat && ColHdrFormat_Prev != ColHdrFormat && CellFormat_Prev == CellFormat)
                    {
                        Target = FormatTarget.AllHeaders;
                        goto Prev;
                    }

                    if (RowHdrFormat_Prev != RowHdrFormat && ColHdrFormat_Prev != ColHdrFormat && CellFormat_Prev != CellFormat)
                    {
                        Target = FormatTarget.All;
                        goto Prev;
                    }
                }
                else
                {
                    Target = FormatTarget.None;
                }

            Prev:
                RowHdrFormat_Prev = RowHdrFormat;
                ColHdrFormat_Prev = ColHdrFormat;
                CellFormat_Prev = CellFormat;
            }

            public DgvNumFormat()
            {
                // Initialise all properties
                CelLckOut = false;
                RowHdrFormat = "N0";
                ColHdrFormat = "N0";
                CellFormat = "N0";
                Target = FormatTarget.All;
            }
        }
        #endregion

        public void Refresh(RefreshMode refreshMode)
        {
            // Info: SetCellWidths() calls SetDgvSize()

            // Suspend layout
            this.dgv.SuspendLayout();

            // SetNumberFormat triggers the cell changed event which is undesireable. Unload event 
            dgv.CellValueChanged -= Dgv_CellValueChanged;

            switch (refreshMode)
            {
                case RefreshMode.All:

                    StyleOverrides(dgv); // Important, prevents selection of a column header destroying the table
                    dgvNumFormat.RowHdrFormat = Utils.FormatDouble(ReadRowHeaders());
                    dgvNumFormat.ColHdrFormat = Utils.FormatDouble(ReadColHeaders());
                    dgvNumFormat.CellFormat   = Utils.FormatDouble(dt);
                    SetNumberFormat_v1(dgvNumFormat);
                    SetCellWidths();
                    SetCellColour(ColourTheme);

                    break;

                case RefreshMode.Partial:

                    // The user has manually adjusted the dp setting if the lockout bit is set. Should that be the case,
                    // we don't want to intefere with there settings
                    SetNumberFormat_v1(dgvNumFormat);
                    SetCellWidths();
                    SetCellColour(ColourTheme);

                    break;

                case RefreshMode.WidthColour:

                    SetCellWidths();
                    SetCellColour(ColourTheme);

                    break;

                case RefreshMode.ColourOnly: // this will cause the cell widths to auto adjust, fuck it

                    SetCellColour(ColourTheme);

                    break;

                case RefreshMode.DpAdjust:

                    SetNumberFormat_v1(dgvNumFormat);
                    SetCellWidths();

                    break;

                case RefreshMode.StyleWidthSize:

                    StyleOverrides(dgv); // Important, prevents selection of a column header destroying the table
                    SetCellWidths();
                    SetCellColour(ColourTheme);

                    break;

                case RefreshMode.AverageTool:

                    StyleOverrides(dgv); // Important, prevents selection of a column header destroying the table
                    SetNumberFormat_v1(dgvNumFormat);
                    SetCellWidths_NoHeaders();

                    break;
            }

            // Resume layout
            this.dgv.ResumeLayout(true);

            // Reload cell changed event
            dgv.CellValueChanged -= Dgv_CellValueChanged;
            dgv.CellValueChanged += Dgv_CellValueChanged;
        }

        public void StyleOverrides(DataGridView _dgv)
        {
            // These settings override the designer settings. They are changes from the default settings that are common
            // across all the data grid views in this project
            //
            // Sets dgv display properties. Some are fixed here, whilst some are user adjustable with default values in
            // the properties backing fields. 

            // Columns
            _dgv.ColumnHeadersDefaultCellStyle.Font = font;
            _dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgv.ColumnHeadersHeight = COLUMN_HEADER_HEIGHT + V_TEXT_PADDING;
            //dgv.ColumnHeadersDefaultCellStyle.Format = "N0";
            foreach (DataGridViewColumn column in _dgv.Columns)
            {
                column.Width = columnWidth;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // Rows
            _dgv.RowHeadersDefaultCellStyle.Font = font;
            _dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            _dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _dgv.RowTemplate.Height = ROW_HEIGHT + V_TEXT_PADDING;
            _dgv.RowHeadersWidth = rowHeaderWidth;
            //dgv.RowHeadersDefaultCellStyle.Format = "N0";
            foreach (DataGridViewRow row in _dgv.Rows)
            {
                row.Height = ROW_HEIGHT + V_TEXT_PADDING;
            }

            // Cells
            _dgv.DefaultCellStyle.Font = font;
            _dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            //dgv.DefaultCellStyle.Format    = numberFormat_Cells;
            foreach (DataGridViewRow row in _dgv.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                { // Default colours
                    cell.Style.ForeColor = defaultForeColour;
                    cell.Style.BackColor = defaultBackColour;
                    cell.Style.SelectionForeColor = defaultSelectionForeColour;
                    cell.Style.SelectionBackColor = defaultSelectionBackColour;
                }

            // Control
            _dgv.BackgroundColor = SystemColors.Control;
            _dgv.GridColor = Color.LightGray;
            _dgv.BorderStyle = BorderStyle.Fixed3D;
            _dgv.ScrollBars = ScrollBars;
            _dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Behaviour
            _dgv.AllowUserToAddRows = false;
            _dgv.AllowUserToDeleteRows = false;
            _dgv.AllowUserToOrderColumns = false;
            _dgv.AllowUserToResizeColumns = false;
            _dgv.AllowUserToResizeRows = false;
            _dgv.ReadOnly = false; // If false allows user to edit cells
            _dgv.ShowCellToolTips = false;
            _dgv.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            // Cunt. This code snippet gets rid of the stupid triangle in the row header cell when row height is
            // > 16. 
            _dgv.RowHeadersDefaultCellStyle.Padding = new Padding(1, 0, 0, 4);

            // Re-paints the dgv
            _dgv.Refresh();
        }

        public void SetNumberFormat_v1(DgvNumFormat format)
        {
            format.Update();

            // Having consistency issues. Setting to ALL to get me by until I fix it
            format.Target = FormatTarget.All;

            if (format.Target == FormatTarget.None)
                return;

            if (format.Target == FormatTarget.All || format.Target == FormatTarget.AllHeaders || format.Target == FormatTarget.RowHeaders)
            {
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    // Check if the cell contains a numeric value (double) 
                    // ? allows expression to return null without throwing an exception
                    if (double.TryParse(dgv.Rows[i].HeaderCell.Value?.ToString(), out double doubleValue))
                    {
                        // Set the format for the cell to display with the set format
                        dgv.Rows[i].HeaderCell.Value = doubleValue.ToString(format.RowHdrFormat);
                    }
                }
                dgv.RowHeadersDefaultCellStyle.Format = format.RowHdrFormat; // Required for scroll bar controller
            }

            if (format.Target == FormatTarget.All || format.Target == FormatTarget.AllHeaders || format.Target == FormatTarget.ColHeaders)
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    // Check if the cell contains a numeric value (double)
                    if (double.TryParse(dgv.Columns[i].HeaderText?.ToString(), out double doubleValue))
                    {
                        // Set the format for the cell to display with the set format
                        dgv.Columns[i].HeaderText = doubleValue.ToString(format.ColHdrFormat);
                    }
                }
                dgv.ColumnHeadersDefaultCellStyle.Format = format.ColHdrFormat; // Required for scroll bar controller
            }

            if (format.Target == FormatTarget.All || format.Target == FormatTarget.Cells)
            {
                //for (int i = 0; i < dgv.Rows.Count; i++)
                //{
                //    for (int j = 0; j < dgv.Columns.Count; j++)
                //    {
                //        // Check if the cell contains a numeric value (double)
                //        if (double.TryParse(dgv.Rows[i].Cells[j].Value?.ToString(), out double doubleValue))
                //        {
                //            // Set the format for the cell to display with the set format
                //            //dgv.Rows[i].DefaultCellStyle.Format = format.CellFormat; // this is way faster
                //            //dgv.Rows[i].Cells[j].Style.Format = format.CellFormat; // this method takes forever
                //        }
                //    }
                //}
                dgv.DefaultCellStyle.Format = format.CellFormat;
            }
        }

        public void SetNumberFormat_v2(DgvNumFormat format)
        {
            // Don't use. It's super slow

            Task.Run(() =>
            {
                bool targetAll = format.Target == FormatTarget.All;
                bool targetHeaders = targetAll || format.Target == FormatTarget.AllHeaders;
                bool targetRowHeaders = targetAll || format.Target == FormatTarget.RowHeaders;
                bool targetColumnHeaders = targetAll || format.Target == FormatTarget.ColHeaders;
                bool targetCells = targetAll || format.Target == FormatTarget.Cells;

                var rowHeaderUpdates = new List<(DataGridViewRow, string)>();
                var columnHeaderUpdates = new List<(DataGridViewColumn, string)>();
                var cellUpdates = new List<(DataGridViewCell, string)>();

                // Process row headers if needed
                if (targetHeaders || targetRowHeaders)
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (double.TryParse(row.HeaderCell.Value?.ToString(), out double doubleValue))
                        {
                            rowHeaderUpdates.Add((row, doubleValue.ToString(format.RowHdrFormat)));
                        }
                    }
                }

                // Process column headers if needed
                if (targetHeaders || targetColumnHeaders)
                {
                    foreach (DataGridViewColumn column in dgv.Columns)
                    {
                        if (double.TryParse(column.HeaderText?.ToString(), out double doubleValue))
                        {
                            columnHeaderUpdates.Add((column, doubleValue.ToString(format.ColHdrFormat)));
                        }
                    }
                }

                // Process cells if needed
                if (targetCells)
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (double.TryParse(cell.Value?.ToString(), out double doubleValue))
                            {
                                cellUpdates.Add((cell, format.CellFormat));
                            }
                        }
                    }
                }

                // Marshal updates to the UI thread
                dgv.Invoke(new Action(() =>
                {
                    foreach (var (row, value) in rowHeaderUpdates)
                    {
                        row.HeaderCell.Value = value;
                    }

                    foreach (var (column, value) in columnHeaderUpdates)
                    {
                        column.HeaderText = value;
                    }

                    foreach (var (cell, formatStr) in cellUpdates)
                    {
                        cell.Style.Format = formatStr;
                    }
                }));
            });
        }

        private void SetCellWidths(DgvData e)
        {
            if (SizeChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - GetHeaderCellWidths()");

            int rowHeaderWidth = 0;
            int colHeaderWidth = 0;

            int rowHeaderWidth_Prev = RowHeaderWidth;
            int colHeaderWidth_Prev = ColHeaderWidth;

            // Get the largest row header width
            foreach (string s in e.RowHeadersText)
            {
                // Get the preferred size of the cell's content using TextRenderer.MeasureText
                Size textSize = TextRenderer.MeasureText(s, Font);

                // Only update when the preferred size is larger than a previous cell
                if ((textSize.Width + H_TEXT_PADDING) > rowHeaderWidth)
                {
                    rowHeaderWidth = textSize.Width + H_TEXT_PADDING;
                }
            }

            // Get the largest column header width
            foreach (string s in e.ColHeadersText)
            {
                // Get the preferred size of the cell's content using TextRenderer.MeasureText
                Size textSize = TextRenderer.MeasureText(s, Font);

                // Only update when the preferred size is larger than a previous cell
                if ((textSize.Width + H_TEXT_PADDING) > colHeaderWidth)
                {
                    colHeaderWidth = textSize.Width + H_TEXT_PADDING;
                }
            }

            // Gets the row and column widths highest value. Existing dgv column width is also checked
            rowHeaderWidth = Math.Max(rowHeaderWidth, MINIMUM_ROW_HEADER_WIDTH);
            colHeaderWidth = Math.Max(Math.Max(ColumnWidth, colHeaderWidth), MINIMUM_COLUMN_WIDTH);

            // Set the widths of the dgv headers, the scroll bar headers will follow this
            dgv.RowHeadersWidth = rowHeaderWidth;
            foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHeaderWidth;

            // Update the client size
            SetDgvSize();
        }

        private void SetCellWidths_NoHeaders()
        {
            if (SizeChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvCellWidths()");

            int rowHeaderWidth = 0;
            int colHeaderWidth = 0;
            int dgvCellWidth = 0;// ColumnWidth;
            string s;

            // Get the largest dgv row header width
            foreach (DataGridViewRow row in dgv.Rows)
            {
                // Get the content of the cell
                s = row.HeaderCell.Value?.ToString() ?? string.Empty;

                // Convert to double so we can set the format
                double.TryParse(s, out double cellValue);

                // Convert back to string with the desired formatting
                s = cellValue.ToString(dgvNumFormat.RowHdrFormat);

                // Get the preferred size of the cell's content using TextRenderer.MeasureText
                Size textSize = TextRenderer.MeasureText(s, Font);

                // Only update when the preferred size is larger than a previous cell
                if ((textSize.Width + H_TEXT_PADDING) > rowHeaderWidth)
                {
                    rowHeaderWidth = textSize.Width + H_TEXT_PADDING;
                }
            }

            // Get the largest dgv column header width
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                // Get the content of the cell
                s = column.HeaderText?.ToString() ?? string.Empty;

                // Convert to double so we can set the format
                double.TryParse(s, out double cellValue);

                // Convert back to string with the desired formatting
                s = cellValue.ToString(dgvNumFormat.ColHdrFormat);

                // Get the preferred size of the cell's content using TextRenderer.MeasureText
                Size textSize = TextRenderer.MeasureText(s, Font);

                // Only update when the preferred size is larger than a previous cell
                if ((textSize.Width + H_TEXT_PADDING) > colHeaderWidth)
                {
                    colHeaderWidth = textSize.Width + H_TEXT_PADDING;
                }
            }

            // Get the largest dgv cell width
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                for (int j = 0; j < dgv.Columns.Count; j++)
                {
                    DataGridViewCell cell = dgv.Rows[i].Cells[j];

                    // Get the content of the cell
                    s = cell.Value?.ToString() ?? string.Empty;

                    // Set the correct number format (avoids a double that I want displayed as a
                    // decimal being interpreted in scientific notation)
                    if (!s.Equals(string.Empty))
                        s = double.Parse(s).ToString(dgvNumFormat.CellFormat);

                    // Get the preferred size of the cell's content using TextRenderer.MeasureText
                    Size textSize = TextRenderer.MeasureText(s, cell.InheritedStyle.Font);

                    // Only update when the preferred size is larger than a previous cell
                    if ((textSize.Width + H_TEXT_PADDING) > dgvCellWidth)
                    {
                        dgvCellWidth = textSize.Width + H_TEXT_PADDING;
                    }
                }
            }

            // Gets the row and column widths highest value
            rowHeaderWidth = Math.Max(rowHeaderWidth, MINIMUM_ROW_HEADER_WIDTH);
            colHeaderWidth = Math.Max(Math.Max(dgvCellWidth, colHeaderWidth), MINIMUM_COLUMN_WIDTH);

            // Set the widths of the dgv
            dgv.RowHeadersWidth = this.rowHeaderWidth;
            foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHeaderWidth;

            // Update the client size
            SetDgvSize();
        }

        public void SetCellWidths()
        {
            if (SizeChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvCellWidths()");

            int rowHeaderWidth = 0;
            int colHeaderWidth = 0;
            int dgvCellWidth = 0;// ColumnWidth;
            string[] rows, cols;

            if (UseMyScrollBars)
            {
                rows = dgvHeaders.ReadRowHeaders();
                cols = dgvHeaders.ReadColHeaders();
            }
            else
            {
                rows = Array.ConvertAll(ReadRowHeaders(), x => x.ToString(dgvNumFormat.RowHdrFormat));
                cols = Array.ConvertAll(ReadColHeaders(), x => x.ToString(dgvNumFormat.ColHdrFormat));
            }

            // Get the largest dgv row header width
            if (rows != null)
                foreach (string s in rows)
                {
                    // Get the preferred size of the cell's content using TextRenderer.MeasureText
                    Size textSize = TextRenderer.MeasureText(s, Font);

                    // Only update when the preferred size is larger than a previous cell
                    if ((textSize.Width + H_TEXT_PADDING) > rowHeaderWidth)
                    {
                        rowHeaderWidth = textSize.Width + H_TEXT_PADDING;
                    }
                }

            // Get the largest dgv column header width
            if (cols != null)
                foreach (string s in cols)
                { 
                    // Get the preferred size of the cell's content using TextRenderer.MeasureText
                    Size textSize = TextRenderer.MeasureText(s, Font);

                    // Only update when the preferred size is larger than a previous cell
                    if ((textSize.Width + H_TEXT_PADDING) > colHeaderWidth)
                    {
                        colHeaderWidth = textSize.Width + H_TEXT_PADDING;
                    }
                }

            // Get the largest dgv cell width
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                for (int j = 0; j < dgv.Columns.Count; j++)
                {
                    DataGridViewCell cell = dgv.Rows[i].Cells[j];

                    // Get the content of the cell
                    string s = cell.Value?.ToString() ?? string.Empty;

                    // Set the correct number format (avoids a double that I want displayed as a
                    // decimal being interpreted in scientific notation)
                    if (!s.Equals(string.Empty))
                        s = double.Parse(s).ToString(dgvNumFormat.CellFormat);

                    // Get the preferred size of the cell's content using TextRenderer.MeasureText
                    Size textSize = TextRenderer.MeasureText(s, cell.InheritedStyle.Font);

                    // Only update when the preferred size is larger than a previous cell
                    if ((textSize.Width + H_TEXT_PADDING) > dgvCellWidth)
                    {
                        dgvCellWidth = textSize.Width + H_TEXT_PADDING;
                    }
                }
            }

            // Gets the row and column widths highest value
            rowHeaderWidth = Math.Max(rowHeaderWidth, MINIMUM_ROW_HEADER_WIDTH);
            colHeaderWidth = Math.Max(Math.Max(dgvCellWidth, colHeaderWidth), MINIMUM_COLUMN_WIDTH);

            // Set the widths of the dgv
            dgv.RowHeadersWidth = rowHeaderWidth;
            foreach (DataGridViewColumn column in dgv.Columns) column.Width = colHeaderWidth;

            // Update the client size
            SetDgvSize();
        }

        private void SetDgvSize()
        {
            if (SizeChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - AutoSetDgvSize()");

            // Calculate total width and height of DataGridView
            Size size = new Size(Point.Empty);

            Size sizePrev = dgv.Size;

            if (dgv.Rows.Count != 0)
            {
                size.Width = dgv.RowHeadersWidth + dgv.Columns[0].Width * dgv.Columns.Count;
                size.Height = dgv.ColumnHeadersHeight + dgv.Rows[0].Height * dgv.Rows.Count;
            }
            else
            {
                return; //dgv.Size;
            }

            // Adjust the calculated width and height for borders etc.
            size.Width += H_BORDER_PADDING;
            size.Height += V_BORDER_PADDING;

            // Set the dgv size
            dgv.Size = size;

            //return size;
        }

        private void HideZeros()
        {
            for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < dt.Columns.Count; columnIndex++)
                {
                    try
                    {
                        double value = Convert.ToDouble(dt.Rows[rowIndex][columnIndex]);

                        if (value != 0.0)
                            continue; // Skip this iteration

                        // Set cell colours to hide the entry
                        dgv.Rows[rowIndex].Cells[columnIndex].Style.ForeColor = defaultBackColour;
                        dgv.Rows[rowIndex].Cells[columnIndex].Style.BackColor = defaultBackColour;
                        dgv.Rows[rowIndex].Cells[columnIndex].Style.SelectionForeColor = SystemColors.Highlight;
                        dgv.Rows[rowIndex].Cells[columnIndex].Style.SelectionBackColor = SystemColors.Highlight;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}");
                    }
                }
            }
        }

        public void SetCellColour(ColourScheme colourScheme)
        {
            if (!DgvHasData)
                return;

            // Property update
            ColourTheme = colourScheme;

            Color cellColour = Color.FromArgb(0, 0, 0);

            // Locals
            int r = 0, g = 0, b = 0;
            double ratio;
            DataGridViewCell cell;
            double tableMinValue = double.PositiveInfinity;
            double tableMaxValue = double.NegativeInfinity;

            // HP editor style
            // Colour range is 0, 255, 0 (green) -> 255, 165, 0 (orange)
            // Midpoint determines if we move R or G. If ratio <= midpoint move R, else move G. B is always 0

            // HP editor edited values style
            // Positive range 255, 218, 218 --> 255, 160, 160
            // Negative range 219, 219, 255 --> 179, 179, 255
            // Midpoint determines if we move positive range or negative range. If ratio <= midpoint move negative range, else move positive range

            // HP scanner fuel trim error style
            // Redish through greenish :)

            // HP Editor green / orange colour scheme
            Color GrnOrg_MaxPos = Color.FromArgb(255, 165, 0); // Orange
            Color GrnOrg_MinPos = Color.FromArgb(255, 255, 0);
            Color GrnOrg_MinNeg = Color.FromArgb(255, 255, 0);
            Color GrnOrg_MaxNeg = Color.FromArgb(0, 255, 0); // Green

            // HP Editor purple / pink colour scheme
            Color PrplPnk_MaxPos = Color.FromArgb(255, 160, 160); // Pink
            Color PrplPnk_MinPos = Color.FromArgb(255, 218, 218);
            Color PrplPnk_MinNeg = Color.FromArgb(219, 219, 255);
            Color PrplPnk_MaxNeg = Color.FromArgb(179, 179, 255); // Purple

            // HP Scanner red / green colour scheme
            Color RedGrn_MaxPos = Color.FromArgb(255, 0, 0); // Red
            Color RedGrn_MinPos = Color.FromArgb(255, 255, 255); // White
            Color RedGrn_MinNeg = Color.FromArgb(255, 255, 255);
            Color RedGrn_MaxNeg = Color.FromArgb(0, 255, 0); // Green

            // Find max and min values of the table
            switch (colourScheme)
            {
                case ColourScheme.HpNormal:
                case ColourScheme.HpEdited:
                    (tableMinValue, tableMaxValue) = GetTableMinMaxValues();
                    break;
                case ColourScheme.HpScanner:
                    tableMinValue = -25; tableMaxValue = 25;
                    break;
            }

            // Loop through the whole table and apply the set colour format
            for (int rowIndex = 0; rowIndex < dgv.Rows.Count; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < dgv.Columns.Count; columnIndex++)
                {
                    // Retrieves the current cell object
                    cell = dgv.Rows[rowIndex].Cells[columnIndex];

                    // Parse out the cell value to a double
                    double.TryParse(cell.Value.ToString(), out double tableCellValue);

                    // Calculate the color based on the value's position within the specified range which can be the min
                    // and max table values or fixed values. The ratio is between 0-1
                    ratio = (double)(tableCellValue - tableMinValue) / (tableMaxValue - tableMinValue);

                    // Use the normalised ratio (0 to 1) to determine the colour blend
                    switch (colourScheme)
                    {
                        case ColourScheme.HpNormal:
                            cellColour = GetColourValue(GrnOrg_MaxPos, GrnOrg_MinPos, GrnOrg_MinNeg, GrnOrg_MaxNeg, 0.66, ratio);
                            break;
                        case ColourScheme.HpEdited:
                            cellColour = GetColourValue(PrplPnk_MaxPos, PrplPnk_MinPos, PrplPnk_MinNeg, PrplPnk_MaxNeg, 0.41, ratio);
                            break;
                        case ColourScheme.HpScanner:
                            cellColour = GetColourValue(RedGrn_MaxPos, RedGrn_MinPos, RedGrn_MinNeg, RedGrn_MaxNeg, 0.5, ratio);
                            break;
                    }

                    // Stops the horrible in your face red
                    if (colourScheme == ColourScheme.HpNormal)
                        if (r == 255 && g == 0 && b == 0)
                            r = 0; g = 255; b = 0;

                    // Assign the new colour to the cell
                    cell.Style.BackColor = cellColour;
                }
            }
        }

        private (double, double) GetTableMinMaxValues()
        {
            double tableMinValue = 0;
            double tableMaxValue = 0;

            // Loops through the whole table and returns the min and max values
            for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < dt.Columns.Count; columnIndex++)
                {
                    double value = (double)dt.Rows[rowIndex][columnIndex];

                    tableMinValue = Math.Min(tableMinValue, value);
                    tableMaxValue = Math.Max(tableMaxValue, value);
                }
            }

            return (tableMinValue, tableMaxValue);
        }

        private Color GetColourValue(Color maxPos, Color minPos, Color minNeg, Color maxNeg, double midPoint, double normRatio)
        {
            double colourRatio;
            int r = 0, g = 0, b = 0;

            // Sets the colour ratio according to the midpoint.
            // For a normalised ratio > midPoint,  colourRatio = 1 -> 0
            // For a normalised ratio <= midPoint, colourRatio = 1 -> 0
            if (normRatio > midPoint)
                colourRatio = (normRatio - midPoint) / (1 - midPoint);
            else
                colourRatio = normRatio / midPoint;

            // Gets the r, g, b value for the normalised ratio based on the inputs
            // if normRatio > midPoint,  maxPos -> minPos
            // if normRatio <= midPoint, minNeg -> maxNeg
            if (normRatio > midPoint)
            {
                if (maxPos.R - minPos.R != 0) { r = (int)(minPos.R + colourRatio * (maxPos.R - minPos.R)); } else r = maxPos.R;
                if (maxPos.G - minPos.G != 0) { g = (int)(minPos.G + colourRatio * (maxPos.G - minPos.G)); } else g = maxPos.G;
                if (maxPos.B - minPos.B != 0) { b = (int)(minPos.B + colourRatio * (maxPos.B - minPos.B)); } else b = maxNeg.B;
            }
            else
            {
                if (maxNeg.R - minNeg.R != 0) { r = (int)(maxNeg.R - colourRatio * (maxNeg.R - minNeg.R)); } else r = maxNeg.R;
                if (maxNeg.G - minNeg.G != 0) { g = (int)(maxNeg.G - colourRatio * (maxNeg.G - minNeg.G)); } else g = maxNeg.G;
                if (maxNeg.B - minNeg.B != 0) { b = (int)(maxNeg.B - colourRatio * (maxNeg.B - minNeg.B)); } else b = maxNeg.B;
            }

            // Limits for float, int over reach on calcs
            if (r < 0) { r = 0; }
            if (g < 0) { g = 0; }
            if (b < 0) { b = 0; }
            if (r > 255) { r = 255; }
            if (g > 255) { g = 255; }
            if (b > 255) { b = 255; }

            return Color.FromArgb(r, g, b);
        }
        #endregion

        //------------------------- Data Table Write Functions ------------------------------------------------------------------------
        #region 
        public void WriteHeaders(double[] row_Labels, double[] column_Labels)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteHeaders()");

            // Redimension data table if the dimension has changed
            if (row_Labels.Length != dt.Rows.Count || column_Labels.Length != dt.Columns.Count)
                ReDimensionDataTable_v2(row_Labels.Length, column_Labels.Length);

            WriteRowHeaderLabels(row_Labels);

            WriteColHeaderLabels(column_Labels);
        }

        public void WriteRowHeaderLabels(double[] row_Labels, string format = "999")
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteRowHeaderLabels()");

            // Length must match current table dimension
            if (row_Labels.Length != dt.Rows.Count)
                return;

            if (format == "999")
                for (int i = 0; i < row_Labels.Length; i++)
                {
                    dgv.Rows[i].HeaderCell.Value = row_Labels[i].ToString();
                }
            else
                for (int i = 0; i < row_Labels.Length; i++)
                {
                    dgv.Rows[i].HeaderCell.Value = row_Labels[i].ToString(format);
                }
        }

        public void WriteColHeaderLabels(double[] column_Labels, string format = "999")
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteColHeaderLabels()");

            // Length must match current table dimension
            if (column_Labels.Length != dt.Columns.Count)
                return;

            if (format == "999")
                for (int i = 0; i < column_Labels.Length; i++)
                {
                    dgv.Columns[i].HeaderText = column_Labels[i].ToString();
                }
            else
                for (int i = 0; i < column_Labels.Length; i++)
                {
                    dgv.Columns[i].HeaderText = column_Labels[i].ToString(format);
                }
        }

        public void WriteToDataGridView(double[] rowLabels, double[] columnLabels, double[,] values, RefreshMode refreshMode = RefreshMode.All)
        {
            // Clear the data present flag
            DgvHasData = false;

            // Suspend layout
            this.dgv.SuspendLayout();

            // Pause events
            Events_CellDataAndSelectionChanged_Pause();

            ReDimensionDataTable_v2(rowLabels.Length, columnLabels.Length);

            WriteToDataTable(values);

            WriteRowHeaderLabels(rowLabels);

            WriteColHeaderLabels(columnLabels);

            // Dgv data flag. This needs to be above Refresh() as the colour format function looks for this flag to be
            // true
            DgvHasData = true;

            Refresh(refreshMode); // low 0.02 - 0.05s, high 0.017 - 0.19s

            // Resume layout
            this.dgv.ResumeLayout(true);

            // Resume events
            Events_CellDataAndSelectionChanged_Resume(true);
        }

        public void WriteToDataGridView(double[] rowLabels, double[] columnLabels, DataTable values, RefreshMode refreshMode = RefreshMode.All)
        {
            // Clear the data present flag
            DgvHasData = false;

            // Suspend layout
            this.dgv.SuspendLayout();

            // Pause events
            Events_CellDataAndSelectionChanged_Pause();

            ReDimensionDataTable_v2(rowLabels.Length, columnLabels.Length);

            WriteToDataTable(values);

            WriteRowHeaderLabels(rowLabels);

            WriteColHeaderLabels(columnLabels);

            Refresh(refreshMode);

            // Resume layout
            this.dgv.ResumeLayout(true);

            // Dgv data flag
            DgvHasData = true;

            // Resume events
            Events_CellDataAndSelectionChanged_Resume(true);
        }

        public void WriteToDataTable(double[,] values)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteToDataTable1()");

            int rowLength = values.GetLength(0);
            int columnLength = values.GetLength(1);

            if (rowLength < 1 || columnLength < 1)
            {
                return;
            }

            // Source data dimensions must match dt dimensions. 
            if (rowLength != dt.Rows.Count || columnLength != dt.Columns.Count)
            {
                return;
            }

            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < columnLength; j++)
                {
                    dt.Rows[i][j] = values[i, j];  // dt write
                }
            }
        }

        public void WriteToDataTable(DataTable values)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteToDataTable2()");

            // I can't get either of these copy / import methods to work when I call undo_Get(). The dt populates with
            // the new data no worries but it does not display properly. The only data that shows is the previous data
            // but the dgv has the correct dimensions. Needs more time on it to work out where it's going wrong

            // Copies one dt to another including the schema of the incoming data table
            //dt = values.Copy();

            // Copy shema to match the incoming source dt.
            dt = values.Clone();

            foreach (DataRow row in values.Rows)
            {
                dt.ImportRow(row); // Imports each row
            }
        }

        public void WriteDt(int row, int col, double value)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - WriteDt() X{col} Y{row}");

            // Ensure the row and column indices are within bounds
            if (row >= 0 && row < dt.Rows.Count && col >= 0 && col < dt.Columns.Count)
            {
                dt.Rows[row][col] = value;
            }
            else
            {
                throw new IndexOutOfRangeException("Row or column index is out of range.");
            }
        }
        #endregion

        //------------------------- Data Table Read Functions -------------------------------------------------------------------------
        #region 
        public double[] ReadRowHeaders()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaders()");

            int i = 0;

            if (!DgvHasData) return null;

            double[] rowLabels = new double[dgv.Rows.Count];

            // Return entire row
            for (i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].HeaderCell.Value != null)
                {
                    if (double.TryParse(dgv.Rows[i].HeaderCell.Value.ToString(), out double result))
                    {
                        rowLabels[i] = result;
                    }
                }
                else
                {
                    return null;
                }
            }

            return rowLabels;
        }

        public double ReadRowHeaderAtIndex(int index)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaderAtIndex()");

            if (!DgvHasData || index == -1) return -1;

            // Return just 1 cell value
            if (dgv.Rows[index].HeaderCell.Value != null)
            {
                if (double.TryParse(dgv.Rows[index].HeaderCell.Value.ToString(), out double result))
                {
                    return result;
                }
            }
            else
            {
                return double.NaN;
            }

            return double.NaN;
        }

        public double[] ReadColHeaders()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadColHeaders()");

            int i = 0;

            if (!DgvHasData) return null;

            double[] columnLabels = new double[dgv.Columns.Count];

            // Return entire column value
            for (i = 0; i < dgv.Columns.Count; i++)
            {
                if (dgv.Columns[i].HeaderText != null)
                {
                    if (double.TryParse(dgv.Columns[i].HeaderText, out double result))
                    {
                        columnLabels[i] = result;
                    }
                }
                else
                {
                    return null;
                }
            }

            return columnLabels;
        }

        public double ReadColumnHeaderAtIndex(int index)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadColumnHeaderAtIndex()");

            if (!DgvHasData || index == -1) return -1;

            // Return just 1 cell value
            if (dgv.Columns[index].HeaderText != null)
            {
                if (double.TryParse(dgv.Columns[index].HeaderText, out double value))
                {
                    return value;
                }
            }
            else
            {
                return double.NaN;
            }

            return double.NaN;
        }

        public double[,] ReadDataTable()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataTable1()");

            if (!DgvHasData) return null;

            // Source data dimensions must match dt dimensions!
            int rowLength = dt.Rows.Count;
            int columnLength = dt.Columns.Count;
            double[,] values = new double[rowLength, columnLength];
            int i = 0, j = 0;

            for (i = 0; i < rowLength; i++)
            {
                for (j = 0; j < columnLength; j++)
                {
                    if (dt.Rows[i][j] != DBNull.Value)
                    {
                        values[i, j] = double.Parse(dt.Rows[i][j].ToString());
                    }
                }
            }

            return values;
        }

        public double[,] ReadDataTable(DataTable dt)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataTable2()");

            // Source data dimensions must match dt dimensions!
            int rowLength = dt.Rows.Count;
            int columnLength = dt.Columns.Count;
            double[,] values = new double[rowLength, columnLength];
            int i = 0, j = 0;

            for (i = 0; i < rowLength; i++)
            {
                for (j = 0; j < columnLength; j++)
                {
                    if (dt.Rows[i][j] != DBNull.Value)
                    {
                        values[i, j] = double.Parse(dt.Rows[i][j].ToString());
                    }
                }
            }

            return values;
        }

        public double[,] ReadDataGridView()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadDataGridView()");

            int i, j;

            if (!DgvHasData) return null;

            double[] rowHeaders = ReadRowHeaders();
            double[] columnHeaders = ReadColHeaders();
            double[,] dataTable = ReadDataTable();

            int rowCount = rowHeaders.Length;
            int columnCount = columnHeaders.Length;

            // Create a new double[,] array with rowHeaders, columnHeaders, and dataTable values
            double[,] combinedArray = new double[rowCount, columnCount + 1];

            // Copy row headers to the first column
            for (i = 0; i < rowCount; i++)
            {
                combinedArray[i, 0] = rowHeaders[i];
            }

            // Copy column headers to the first row
            for (j = 0; j < columnCount; j++)
            {
                combinedArray[0, j + 1] = columnHeaders[j];
            }

            // Copy values from the dataTable to the combinedArray
            for (i = 0; i < rowCount; i++)
            {
                for (j = 0; j < columnCount; j++)
                {
                    combinedArray[i, j + 1] = dataTable[i, j];
                }
            }

            return combinedArray;
        }

        public double ReadCellAtAddress(Point addr)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadCellAtAddress()");

            if (addr.Y > dgv.Rows.Count || addr.X > dgv.Columns.Count)
            {
                return double.NaN;
            }

            return (double)dt.Rows[addr.Y][addr.X];
        }

        public double ReadDt(int row, int col)
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadDt() X{col} Y{row}");

            // Ensure the row and column indices are within bounds
            if (row >= 0 && row < dt.Rows.Count && col >= 0 && col < dt.Columns.Count)
            {
                if (dt.Rows[row][col] == DBNull.Value)
                    return 0.0;
                else
                    return (double)dt.Rows[row][col];
            }
            else
            {
                throw new IndexOutOfRangeException("Row or column index is out of range.");
            }
        }
        #endregion

        //------------------------- DGV General Functions -----------------------------------------------------------------------------
        #region
        private void AssignInstanceNames()
        {
            myEvents.InstanceName = InstanceName;
            incDecTask.InstanceName = InstanceName;
            dgvNumFormat.InstanceName = InstanceName;
            if (CopyPasteEnabled) copy.InstanceName = InstanceName;
            if (CopyPasteEnabled) paste.InstanceName = InstanceName;
            if (UndoEnabled) undo.InstanceName = InstanceName;
        }

        private void EnableDoubleBuffering()
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dgv, new object[] { true });

            dgv.GetType().InvokeMember("SetStyle",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                null, dgv, new object[]
                {
                            ControlStyles.OptimizedDoubleBuffer |
                            ControlStyles.AllPaintingInWmPaint, true
                });

            // Idea from graph3d line 4184
            //SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void ClearDataTable()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ClearDataTable()");

            for (int i = 0; i < dt.Rows.Count; i++)
                for (int j = 0; j < dt.Columns.Count; j++)
                    dt.Rows[i][j] = DBNull.Value;
        }

        public void ResetDataTable()
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ResetDataTable()");

            // Initial table size
            ReDimensionDataTable_v2(1, 1);

            // Clear row header
            dgv.Rows[0].HeaderCell.Value = String.Empty;

            // Reset column header
            dgv.Columns[0].HeaderText = "0";

            // Clear data table
            dt.Rows[0][0] = DBNull.Value;

            // Resize the DGV form control
            //SetCellWidths();

            // Clear the data present flag
            DgvHasData = false;

            // Clear the decimal place lockout flag
            dgvNumFormat.CelLckOut = false;
        }

        public bool ReDimensionDataTable_v1(int rowLabelLength, int columnLabelLength)
        {
            bool dimensionChanged = false;

            // Create columns only if they don't already exist
            for (int i = 0; i < columnLabelLength; i++)
            {
                string columnName = i.ToString();

                // Check if the column already exists
                if (!dt.Columns.Contains(columnName))
                {
                    dt.Columns.Add(columnName, typeof(double)); // typeof(double) is very important! Biggest impact for me was
                    dimensionChanged = true;                    // with formatting not behaving between the dgv and dt
                }
            }

            // Add rows if they don't exist
            while (dt.Rows.Count < rowLabelLength)
            {
                dt.Rows.Add(new object[columnLabelLength]);
                dimensionChanged = true;
            }

            // Remove excess columns
            for (int i = dt.Columns.Count - 1; i >= columnLabelLength; i--)
            {
                dt.Columns.RemoveAt(i);
                dimensionChanged = true;
            }

            // Remove excess rows
            for (int i = dt.Rows.Count - 1; i >= rowLabelLength; i--)
            {
                dt.Rows.RemoveAt(i);
                dimensionChanged = true;
            }

            return dimensionChanged;
        }

        public bool ReDimensionDataTable_v2(int rowLength, int colLength) // 0.05s
        {
            if (DgvData_Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReDimensionDataTable_v2()");

            // Continue only if the dimensions need changing, else return false indicating no dimension change
            if (rowLength == dt.Rows.Count && colLength == dt.Columns.Count)
                return false;

            // This strategy of re-dimensioning the data table is 0.45s faster than my old strategy where I was keeping
            // the data table and adding or removing rows and columns. The longest operation was removing columns.
            // Instead, a the existing dt variable has a new dt created and rows and columns are added to match the
            // target dimensions. Before the new dt is created the exusting dt is detached from the dgv. On finishing
            // the new dt the dt is then reattached to the dgv.

            // Temporarily detach the data table from the dgv
            dgv.DataSource = null;

            // New data table
            dt = new DataTable();

            // Create columns
            for (int i = 0; i < colLength; i++)
            {
                string columnName = i.ToString();

                dt.Columns.Add(columnName, typeof(double));
            }

            // Add rows
            for (int i = 0; i < rowLength; i++)
            {
                DataRow newRow = dt.NewRow();
                dt.Rows.Add(newRow);
            }

            // Reattach the data table to the dgv
            dgv.DataSource = dt;

            return true;
        }

        public void AdjustDecimalPlaces(DpDirection direction) // N.B. dataTableNumberFormat is modified internally
        {
            // If no data, return
            if (!DgvHasData)
            {
                return;
            }

            // Set the lockout flag when the user clicks an increment or decrement button.
            // This flag locks out changing the cell number format when performing certain
            // operations like interpolating. The premise is to reduce the busyness of
            // format changes.
            // Unlock to allow dp adjustments by the user
            dgvNumFormat.CelLckOut = false;

            // Retrieve the current displayed number format from the first cell. All the other cells are the same
            string s = Utils.GetNumberFormat(dgv.Rows[0].Cells[0]);

            // Split the string using N as the separator and count current decimal places in the
            // fractional part
            string[] parts = s.Split('N');

            // The 2nd array value contains the number of decimal places
            int dp = Convert.ToInt32(parts[1]);

            // Increment or decrement
            if (direction == DpDirection.Increment)
            {
                dp++;
            }

            if (direction == DpDirection.Decrement)
            {
                if (dp > 0)
                {
                    dp--;
                }
            }

            // Build the new format string
            s = "N" + dp.ToString();

            // Set the new number format
            dgvNumFormat.CellFormat = s;

            // dgv refresh before setting the lockout bit
            Refresh(RefreshMode.DpAdjust);

            // Lock to prevent auto format dp adjustments by the program
            dgvNumFormat.CelLckOut = true;
        }

        public void ClearSelection()
        {
            dgv.ClearSelection();
        }

        public Point GetTopLeftCellAddress(DataGridViewSelectedCellCollection selectedCells)
        {
            if (selectedCells.Count == 0)
                return new System.Drawing.Point(-1, -1); // Return an invalid point if no cells are selected

            // Get the DataGridView instance from any selected cell
            DataGridView dataGridView = selectedCells[0].DataGridView;

            // Get the top-left selected cell coordinates
            int minRowIndex = int.MaxValue;
            int minColumnIndex = int.MaxValue;

            foreach (DataGridViewCell cell in selectedCells)
            {
                if (cell.RowIndex < minRowIndex)
                    minRowIndex = cell.RowIndex;
                if (cell.ColumnIndex < minColumnIndex)
                    minColumnIndex = cell.ColumnIndex;
            }

            if (SelectionChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - GetTopLeftCellAddress() X{minColumnIndex} Y{minRowIndex}");

            // Return the top-left cell coordinates as a Point
            return new System.Drawing.Point(minColumnIndex, minRowIndex);
        }

        public void SetDgvCurrentCell(int row, int col)
        {
            dgv.CurrentCell = dgv.Rows[row].Cells[col];
        }
        #endregion

        //------------------------- My Events -----------------------------------------------------------------------------------------
        #region
        public class MyEvents
        #region
        {
            #region Public
            // Facility to fire events with custom event arguments and timer off delays. High frequency events such as
            // cell selection changed, cell value changed etc are debounced using a timerOff timerOn pattern before
            // firing the respective event
            public string ClassName { get; set; } = "MyEvnts";
            public string InstanceName { get { return instancename; } set { instancename = value; AssignTimerInstanceNames(); } }
            private bool PauseAllEvents { get; set; }
            private bool PauseDataEventsFromGraph3d { get; set; }
            private bool PauseSelnEventsFromGraph3d { get; set; }
            private bool PauseSelnEventsToGraph3d { get; set; }
            private bool PauseSelnEventsFromDgvCtrl { get; set; }
            public bool DebugAll { get; set; }
            public bool DebugDataChanged { get; set; }
            public bool DebugSizeChanged { get; set; }
            public bool DebugSelnChanged { get; set; }
            public bool DebugMuteHighSpeed { get; set; }
            public bool DebugDbncTmr { get { return debugDbncTmr; } set { debugDbncTmr = value; tmr_DgvDataChanged_Debounced.Debug = value; tmr_SelectionChanged_Debounced.Debug = value; } }
            public bool DebugIntTmr { get { return debugIntTmr; } set { debugIntTmr = value; tmr_DgvSizeChanged_Intermittent.Debug = value; tmr_SelectionChanged_Intermittent.Debug = value; } }

            public void AssignTimerInstanceNames()
            {
                tmr_DgvDataChanged_Debounced.DebugInstanceName = instancename;
                tmr_DgvSizeChanged_Intermittent.DebugInstanceName = instancename;
                tmr_SelectionChanged_Debounced.DebugInstanceName = instancename;
                tmr_SelectionChanged_Intermittent.DebugInstanceName = instancename;
            }

            // Constructor
            public MyEvents(DgvCtrl dgvCtrl)
            {
                // Class instance
                this.dgvCtrl = dgvCtrl;

                // Timers setup. Each event gets its own timer. The invoking of each event is done in the events
                // respective timer tick. Note; the instance name is assigned in a seperate function
                //
                // New data -Debounced
                tmr_DgvDataChanged_Debounced = new TimerOffDelay
                {
                    Preset = 50,
                    DebugTimerName = "tmr_DgvDataChanged_Debounced",
                    OnTimingDone = Raise_DgvDataChanged_Debounced_Event
                };

                // New size - Intermittent
                tmr_DgvSizeChanged_Intermittent = new TimerOnDelay
                {
                    Preset = 50,
                    AutoStop = true,
                    AutoStop_CountsPreset = 4,
                    DebugTimerName = "tmr_DgvSizeChanged_Intermittent",
                    OnTimingDone = Raise_DgvSizeChanged_Intermittent_Event
                };

                // New selection - Intermittent
                tmr_SelectionChanged_Intermittent = new TimerOnDelay
                {
                    Preset = 50,
                    AutoStop = true,
                    AutoStop_CountsPreset = 4,
                    DebugTimerName = "tmr_SelectionChanged_Intermittent",
                    OnTimingDone = Raise_DgvSelectionChanged_Intermittent_Event
                };

                // New selection - Debounced
                tmr_SelectionChanged_Debounced = new TimerOffDelay
                {
                    Preset = 50,
                    DebugTimerName = "tmr_SelectionChanged_Debounced",
                    OnTimingDone = Raise_DgvSelectionChanged_Debounced_Event
                };
            }
            #endregion

            #region Variables

            // Class instances
            DgvCtrl dgvCtrl;
            DgvData dgvData = new DgvData();
            DgvData dgvDataPrev = new DgvData();
            SizeEventArgs sizeEventArgs = new SizeEventArgs();
            SelectEventArgs selectEventArgs = new SelectEventArgs();

            // Previous dgv values
            //double[] colHeaders_Prev = new double[0];
            //double[] rowHeaders_Prev = new double[0];
            //double[,] dt_Prev        = new double[0, 0];

            // Timer
            public TimerOffDelay tmr_DgvDataChanged_Debounced;
            public TimerOnDelay tmr_DgvSizeChanged_Intermittent;
            public TimerOffDelay tmr_SelectionChanged_Debounced;
            public TimerOnDelay tmr_SelectionChanged_Intermittent;

            // Backing fields
            bool debugDbncTmr;
            bool debugIntTmr;
            string instancename;

            // Define the public events
            public event EventHandler<DgvData> DgvDataChanged_Debounced;
            public event EventHandler<DgvData> DgvDataChanged_Immediate;
            public event EventHandler<SizeEventArgs> DgvSizeChanged_Immediate;
            public event EventHandler<SizeEventArgs> DgvSizeChanged_Intermittent;
            public event EventHandler<SelectEventArgs> DgvSelectionChanged_Immediate;
            public event EventHandler<SelectEventArgs> DgvSelectionChanged_ToGraph3d_Immediate;
            public event EventHandler<SelectEventArgs> DgvSelectionChanged_Intermittent;
            public event EventHandler<SelectEventArgs> DgvSelectionChanged_Debounced;
            public event EventHandler<DgvData> DgvDataChangedToHeaders;
            #endregion

            #region Functions
            //------------------------- Pause / Resume --------------------------------------------------------------------------------
            #region
            // Pauses / resume all events, when resuming all the events are fired once
            public void Pause_All()
            {
                if (!PauseAllEvents && (DebugAll || DebugDataChanged || DebugSizeChanged || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Pause_All()");

                PauseAllEvents = true;
            }
            public void Resume_All()
            {
                if (DebugAll || DebugDataChanged || DebugSizeChanged || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Resume_All()");

                PauseAllEvents = false;

                // Raise events to catch everyone up
                Req_DgvDataChanged_Event();
                Req_DgvSizeChanged_Event();
                Req_DgvSelectionChanged_Event();
            }

            // Pause / resume data change event whilst graph3d points are being dragged
            public void Pause_DataFromGraph3d()
            {
                if (!PauseDataEventsFromGraph3d && (DebugAll || DebugDataChanged || DebugDbncTmr || DebugIntTmr))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Pause_DataFromGraph3d");

                PauseDataEventsFromGraph3d = true;
            }
            public void Resume_DataFromGraph3d()
            {
                if (DebugAll || DebugDataChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Resume_DataFromGraph3d()");

                // Do not call Req_DgvDataChanged_Event. It fucks up the point drag. Just setting the property to
                // un-pause is fine cuz. This flag is used to inhibit the data change event.
                PauseDataEventsFromGraph3d = false;
            }

            // Pause / resume selection changed events when point hovering over the graph
            public void Pause_SelectionFromGraph3d()
            {
                if (!PauseSelnEventsFromGraph3d && (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionFromGraph3d()");

                PauseSelnEventsFromGraph3d = true;
            }
            public void Resume_SelectionFromGraph3d()
            {
                if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionFromGraph3d()");

                PauseSelnEventsFromGraph3d = false;
            }
            public void Pause_SelectionToGraph3d()
            {
                if (!PauseSelnEventsFromGraph3d && (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionToGraph3d()");

                PauseSelnEventsToGraph3d = true;
            }
            public void Resume_SelectionToGraph3d()
            {
                if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionToGraph3d()");

                PauseSelnEventsToGraph3d = false;
            }
            public void Pause_SelectionFromDgvCtrl()
            {
                if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Pause_SelectionFromDgvCtrl()");

                PauseSelnEventsFromDgvCtrl = true;
            }
            public void Resume_SelectionFromDgvCtrl()
            {
                if (DebugAll || DebugSelnChanged || DebugDbncTmr || DebugIntTmr)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Resume_SelectionFromDgvCtrl()");

                PauseSelnEventsFromDgvCtrl = false;
            }

            #endregion

            //------------------------- Data Changed ----------------------------------------------------------------------------------
            #region
            public void Req_DgvDataChanged_Event()
            {
                if (PauseAllEvents || PauseDataEventsFromGraph3d)
                    return;

                if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvDataChanged_Event()");

                // Start the debounce timer for the debounced event. Repeated start calls are ignored until the time
                // period passed
                if (DebugAll || (DebugDataChanged && !tmr_DgvDataChanged_Debounced.TimerTiming))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Started timer_DgvDataChanged_Debounced");

                tmr_DgvDataChanged_Debounced.Start();

                // Raise the immediate event
                Raise_DgvDataChanged_Immediate_Event();
            }
            public void Req_DgvDataChanged_Event(DgvData e)
            {
                if (PauseAllEvents || PauseDataEventsFromGraph3d)
                    return;

                dgvData = e;

                if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvDataChanged_Event()");

                // Start the debounce timer for the debounced event. Repeated start calls are ignored until the time
                // period passed
                if (DebugAll || (DebugDataChanged && !tmr_DgvDataChanged_Debounced.TimerTiming))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Started timer_DgvDataChanged_Debounced");

                tmr_DgvDataChanged_Debounced.Start();

                // Raise the immediate event
                Raise_DgvDataChanged_Immediate_Event();
            }
            public void Req_DgvDataChanged_ToHeaders_Event(DgvData e)
            {
                if (PauseAllEvents || PauseDataEventsFromGraph3d)
                    return;

                Raise_DgvDataChanged_ToHeaders_Event(e);
            }

            public DgvData BuildEventArgs_DgvDataChanged_Event()
            {
                DgvData e = new DgvData();

                // Values
                e.RowHeaders = dgvCtrl.ReadRowHeaders();
                e.ColHeaders = dgvCtrl.ReadColHeaders();
                e.TableData = dgvCtrl.ReadDataTable();

                // Formatting
                e.RowHeaderFormat = dgvCtrl.RowHeaderFormat;
                e.ColHeaderFormat = dgvCtrl.ColHeaderFormat;
                e.TableDataFormat = dgvCtrl.DataTableFormat;

                // Convert values to text headers
                if (dgvCtrl.UseMyScrollBars)
                {
                    e.RowHeadersText = dgvCtrl.dgvHeaders.ReadRowHeaders();
                    e.ColHeadersText = dgvCtrl.dgvHeaders.ReadColHeaders();
                }
                else
                {
                    e.RowHeadersText = Array.ConvertAll(dgvCtrl.ReadRowHeaders(), x => x.ToString(dgvCtrl.dgvNumFormat.RowHdrFormat));
                    e.ColHeadersText = Array.ConvertAll(dgvCtrl.ReadColHeaders(), x => x.ToString(dgvCtrl.dgvNumFormat.ColHdrFormat));
                }

                // Store the current values to compare against next time
                dgvDataPrev = e.Copy();

                //if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
                //{
                //    Console.WriteLine($"{InstanceName} - {ClassName} - DgvDataChanged EventArgs built");
                //}

                // Return with the event args
                return e;
            }

            private void Raise_DgvDataChanged_Debounced_Event()
            {
                if (DebugAll || DebugDataChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_Debounced_Event()");

                // Fire the event to any subscriber
                DgvDataChanged_Debounced?.Invoke(this, BuildEventArgs_DgvDataChanged_Event());
            }
            private void Raise_DgvDataChanged_Immediate_Event()
            {
                if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_Immediate_Event()");

                // Fire the event to any subscriber
                DgvDataChanged_Immediate?.Invoke(this, BuildEventArgs_DgvDataChanged_Event());
            }
            private void Raise_DgvDataChanged_ToHeaders_Event(DgvData e)
            {
                if (DebugAll || (DebugDataChanged && !DebugMuteHighSpeed))
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raise_DgvDataChanged_ToHeaders_Event()");

                // Fire the event to the headers
                DgvDataChangedToHeaders?.Invoke(this, e);
            }
            #endregion

            //------------------------- Size Changed ----------------------------------------------------------------------------------
            #region
            public void Req_DgvSizeChanged_Event()
            {
                if (PauseAllEvents)
                    return;

                if (DebugAll || DebugSizeChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvSizeChanged_Event()");

                // Build the event args
                BuildEventArgs_DgvSizeChanged_Event();

                if ((DebugAll || DebugSizeChanged) && !tmr_DgvSizeChanged_Intermittent.TimerTiming)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSizeChanged_Intermittent");

                // Intermittent timer, runs to completion each call. Each call resets the auto stop counter
                tmr_DgvSizeChanged_Intermittent.Start();

                // Raise the event
                Raise_DgvSizeChanged_Immediate_Event();
            }

            private void BuildEventArgs_DgvSizeChanged_Event()
            {
                // Build the event arguments that a subscriber can read
                sizeEventArgs = new SizeEventArgs();

                // This instance of the DgvCtrl class
                sizeEventArgs.Sender = dgvCtrl;

                // Values
                sizeEventArgs.DgvSize = dgvCtrl.dgv.Size;
                sizeEventArgs.DgvDisplayRectangle = dgvCtrl.dgv.DisplayRectangle;
                sizeEventArgs.DgvClientRectangle = dgvCtrl.dgv.ClientRectangle;
                sizeEventArgs.DgvLocation = dgvCtrl.dgv.Location;
                sizeEventArgs.DgvRowHeaderWidth = dgvCtrl.dgv.RowHeadersWidth;
                sizeEventArgs.DgvColumnHeaderWidth = dgvCtrl.dgv.Columns[0].Width;
                sizeEventArgs.DgvColumnWidth = dgvCtrl.dgv.Columns[0].Width;
                sizeEventArgs.DgvRowHeight = dgvCtrl.dgv.Rows[0].Height;
                sizeEventArgs.DgvColumnHeaderHeight = dgvCtrl.dgv.ColumnHeadersHeight;

                if (DebugAll || DebugSizeChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - DgvSizeChanged EventArgs built");
            }

            private void Raise_DgvSizeChanged_Immediate_Event()
            {
                if (DebugAll || DebugSizeChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSizeChanged_Immediate_Event");

                // Fire the event to any subscriber
                DgvSizeChanged_Immediate?.Invoke(this, sizeEventArgs);
            }
            private void Raise_DgvSizeChanged_Intermittent_Event()
            {
                if (DebugAll || DebugSizeChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSizeChanged_Intermittent_Event");

                // Fire the event to any subscriber
                DgvSizeChanged_Intermittent?.Invoke(this, sizeEventArgs);
            }
            #endregion

            //------------------------- Selection Changed -----------------------------------------------------------------------------
            #region
            public void Req_DgvSelectionChanged_Event(object sender, EventArgs e)
            {
                Req_DgvSelectionChanged_Event();
            }
            public void Req_DgvSelectionChanged_Event()
            {
                if (PauseAllEvents || PauseSelnEventsFromGraph3d || PauseSelnEventsFromDgvCtrl)
                    return;

                if (DebugAll || DebugSelnChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Req_DgvSelectionChanged_Event()");

                // Build the event args
                BuildEventArgs_DgvSelectionChanged_Event();

                if ((DebugAll || DebugSelnChanged) && !tmr_SelectionChanged_Intermittent.TimerTiming)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSelectionChanged_Intermittent");

                if ((DebugAll || DebugSelnChanged) && !tmr_SelectionChanged_Intermittent.TimerTiming)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Started tmr_DgvSelectionChanged_Debounced");

                // Timers start
                tmr_SelectionChanged_Intermittent.Start();
                tmr_SelectionChanged_Debounced.Start();

                // Raise the event
                Raise_DgvSelectionChanged_Event();
            }

            private void BuildEventArgs_DgvSelectionChanged_Event()
            {
                // Build the event arguments that a subscriber can read
                selectEventArgs = new SelectEventArgs();

                // This instance of the DgvCtrl class
                selectEventArgs.Sender = dgvCtrl;

                // Selected cell collection
                selectEventArgs.SelectedCellCollection = dgvCtrl.dgv.SelectedCells;

                if (DebugAll || DebugSelnChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - DgvSelectionChanged EventArgs built");
            }

            private void Raise_DgvSelectionChanged_Intermittent_Event()
            {
                if (DebugAll || DebugSelnChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Intermittent_Event");

                // Fire the event to any subscriber
                DgvSelectionChanged_Intermittent?.Invoke(this, selectEventArgs);
            }
            private void Raise_DgvSelectionChanged_Debounced_Event()
            {
                if (DebugAll || DebugSelnChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Debounced_Event");

                // Fire the event to any subscriber
                DgvSelectionChanged_Debounced?.Invoke(this, selectEventArgs);
            }
            private void Raise_DgvSelectionChanged_Event()
            {
                if (DebugAll || DebugSelnChanged)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raised DgvSelectionChanged_Event");

                // Fire the event to any subscriber
                DgvSelectionChanged_Immediate?.Invoke(this, selectEventArgs);

                // Graoh3d subscribes to this event
                if (!PauseSelnEventsToGraph3d)
                    DgvSelectionChanged_ToGraph3d_Immediate?.Invoke(this, selectEventArgs);
            }
            #endregion
            #endregion

            public class SizeEventArgs : EventArgs
            #region
            {
                public object Sender { get; set; }
                public Size DgvSize { get; set; }
                public Rectangle DgvDisplayRectangle { get; set; }
                public Rectangle DgvClientRectangle { get; set; }
                public Point DgvLocation { get; set; }
                public int DgvRowHeaderWidth { get; set; }
                public int DgvColumnHeaderWidth { get; set; }
                public int DgvColumnWidth { get; set; }
                public int DgvRowHeight { get; set; }
                public int DgvColumnHeaderHeight { get; set; }
                public string RowHeaderFormat { get; set; } = "N0";
                public string ColHeaderFormat { get; set; } = "N0";

                public SizeEventArgs()
                {
                }

                public static new SizeEventArgs Empty;
            }
            #endregion

            public class SelectEventArgs : EventArgs
            #region
            {
                public object Sender { get; set; }
                public DataGridViewSelectedCellCollection SelectedCellCollection { get; set; }

                public SelectEventArgs()
                {
                }

                public static new SelectEventArgs Empty;
            }
            #endregion
        }
        #endregion
        
        // Input - External class events
        private void Paste_Completed_NDR(object sender, DgvData e)
        {
            if (UndoEnabled && undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Call Undo_Set() from Paste_Completed_NDR()");

            if (EventDebug && UndoEnabled && !undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Paste_Completed_NDR()");

            // Updates headers
            myEvents.Req_DgvDataChanged_ToHeaders_Event(e);

            // Set the dgv widths
            SetCellWidths();

            // Triggers a capture of the dgv image. At this point this class's properties (although somewhat scattered)
            // will reflect the paste data change event ags
            Undo_Set(myEvents.BuildEventArgs_DgvDataChanged_Event());
        }
        private void Undo_Completed_NDR(object sender, DgvData e)
        {
            if (undo.Debug || EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Completed_NDR()");

            // Included are the event args from the undo action
            myEvents.Req_DgvDataChanged_Event(e);     
            
            // Updates headers
            myEvents.Req_DgvDataChanged_ToHeaders_Event(e);

            // Set the dgv widths
            SetCellWidths();
        }
        private void IncDec_Incremental_NDR(object sender, EventArgs e)
        {
            if (EventDebug || incDecTask.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - IncDec_Incremental_NDR()");

            myEvents.Req_DgvDataChanged_Event();
        }
        private void IncDec_Completed_NDR(object sender, EventArgs e)
        {
            if (undo.Debug || incDecTask.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Call Undo_Set() from IncDec_Completed_NDR()");

            if (EventDebug && !undo.Debug && !incDecTask.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - IncDec_Completed_NDR()");

            myEvents.Req_DgvDataChanged_Event();
        }

        // Input - Dgv events
        private void Dgv_MyEvents_CellValueOrDtRowChanged()
        {
            if (UndoEnabled && !undo.InProgress && !redo.InProgress && CopyPasteEnabled && !paste.InProgress && !incDecTask.Mode.Enabled && !Z_RemoteValuesChanging)
            {
                if (EventDebug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MyEvents_CellValueOrDtRowChanged()");

                myEvents.Req_DgvDataChanged_Event();
            }
        }
        private void Dgv_MyEvents_SelectionChanged()
        {
            myEvents.Req_DgvSelectionChanged_Event();
        }

        // Output (partial) - MyEvents subscribed to by this (dgvCtrl) class
        private void MyEvents_Dgv_NDR_Debounced(object sender, DgvData e)
        {
            if (UndoEnabled && undo.Debug && !undo.InProgress)
                Console.WriteLine($"{InstanceName} - {ClassName} - Undo_Set() from MyEvents_Raise_Dgv_NDR_Debounced_Event()");

            if (EventDebug && UndoEnabled && !undo.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_NDR_Debounced()");

            // Request to take a snapshot of the current dgv contents
            Undo_Set(e);
        }
        private void MyEvents_Dgv_NDR_Immediate(object sender, DgvData e)
        {
            if (UndoEnabled && !undo.InProgress)
            {
                //redo.ClearStack();

                // Disables redo button
                //FormButton_RedoEnabled?.Invoke(this, redo.CanDo);
            }
        }
        private void MyEvents_Dgv_NewSize_Intermittent(object sender, MyEvents.SizeEventArgs e)
        {

        }
        #endregion

        //------------------------- Events --------------------------------------------------------------------------------------------
        #region
        public void Events_CellDataAndSelectionChanged_Pause()
        {
            if (EventDebug || DataChangedDebug || SelectionChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Events_CellDataAndSelectionChanged_Pause()");

            dgv.CellValueChanged -= Dgv_CellValueChanged;
            dt.RowChanged -= Dt_RowChanged;
            dgv.SelectionChanged -= Dgv_SelectionChanged;
        }

        public void Events_CellDataAndSelectionChanged_Resume(bool bypass = false)
        {
            if (EventDebug || DataChangedDebug || SelectionChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Events_CellDataAndSelectionChanged_Resume()");

            dgv.CellValueChanged -= Dgv_CellValueChanged; // has multiple subs. Ensures only 1 is sub'd at a time
            dgv.CellValueChanged += Dgv_CellValueChanged; 
            dt.RowChanged += Dt_RowChanged;
            dgv.SelectionChanged += Dgv_SelectionChanged;

            // Raise events to catch everyone up
            myEvents.Req_DgvDataChanged_Event();
            myEvents.Req_DgvSizeChanged_Event();
            //myEvents.Req_DgvSelectionChanged_Event();
            if (UseMyScrollBars) myEvents.Req_DgvDataChanged_ToHeaders_Event(myEvents.BuildEventArgs_DgvDataChanged_Event());
        }

        public void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (EventDebug || SelectionChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_SelectionChanged() Current cell X{dgv.CurrentCell.ColumnIndex} Y{dgv.CurrentCell.RowIndex}");

            // This code snippet runs once at the completion of a user edit to override the default behaviour where the
            // selection is cleared and the selected cell is moved down 1 position. Instead it restores the current
            // selection
            if (keepCellsSelectedAfterEdit) // Set in the Dgv_CellValidating event
            {
                // Unload the event to stop a race condition
                dgv.SelectionChanged -= Dgv_SelectionChanged;
                myEvents.Pause_SelectionFromDgvCtrl();

                // Clear all cell selections first
                dgv.ClearSelection();

                // Restore the current cell
                dgv.CurrentCell = currentCell_Copy;

                // Restore the selected cells
                foreach (DataGridViewCell cell in selectedCellsCollection_Copy)
                {
                    dgv.Rows[cell.RowIndex].Cells[cell.ColumnIndex].Selected = true;
                }

                // All done, reload the event
                dgv.SelectionChanged += Dgv_SelectionChanged;
                myEvents.Resume_SelectionFromDgvCtrl();

                // Reset the copy flag
                keepCellsSelectedAfterEdit = false;
            }

            // Set the selected cell property to the top left cell of the selection
            SelectedCellAddress = GetTopLeftCellAddress(dgv.SelectedCells);

            // Hand this event over to the myEvent selection change function
            Dgv_MyEvents_SelectionChanged();
        }

        private void Dgv_SizeChanged(object sender, EventArgs e)
        {
            if (!DgvHasData)
                return;

            if (SizeChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_SizeChanged()");

            myEvents.Req_DgvSizeChanged_Event();
        }

        private void Dt_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            if (EventDebug || DataChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dt_RowChanged()");

            Dgv_MyEvents_CellValueOrDtRowChanged();
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (EventDebug || DataChangedDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_CellValueChanged()");

            Dgv_MyEvents_CellValueOrDtRowChanged();
        }

        private void Dgv_MouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (EventDebug || DebugMouse)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MouseDown() Button {e.Button}");

            // If right mouse button is down unload the selection changed event until the mouse is released and the
            // context menu is open. This is to address the cell losing selection when using the context menu and
            // looking to paste to a cell
            //if (e.Button == MouseButtons.Right)
            //    dgv.SelectionChanged -= Dgv_SelectionChanged;

            // This section is for when there are none or 1 cells selected and the user right clicks. It will set the current
            // cell to the cell under the mouse pointer. Handy for pasting without having to activate a cell first
            if (e.Button == MouseButtons.Right)
            {
                // Check no cells are selected
                if (dgv.SelectedCells.Count <= 1)
                {
                    // Check if the clicked cell is valid
                    if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        // Deselect the other cell
                        foreach (DataGridViewCell cell in dgv.SelectedCells)
                            cell.Selected = false;

                        // Set the clicked cell as the current cell. This also sets the selected property to true
                        //dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                        dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;

                        if (DebugMouse)
                            Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MouseDown() Right click cell selected X{dgv.CurrentCell.ColumnIndex} Y{dgv.CurrentCell.RowIndex}");
                    }
                }
            }

            // Reset the user edit flag
            userCellEditPending = false;
        }

        private void Dgv_MouseUp(object sender, MouseEventArgs e)
        {
            if (EventDebug || DebugMouse)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MouseUp() Button {e.Button}");

            dgv.ReadOnly = false;
        }

        private void Dgv_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_EditingControlShowing()");

            // Initiate increment decrement mode
            if (DgvHasData)
            {
                if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.Subtract))
                {
                    dgv.ReadOnly = true;

                    if (!incDecTask.Mode.Enabled)
                        Dgv_IncDecKeyDown();

                    return;
                }
            }

            // If inc dec mode not running then continue on with the cell edit. The next event will be the editing
            // keypress event.
            // Note; we unload then reload the event as per MS documentation
            dgv.EditingControl.KeyPress -= Dgv_EditingControl_KeyPress;
            dgv.EditingControl.KeyPress += Dgv_EditingControl_KeyPress;
        }

        private void Dgv_EditingControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_EditingControl_KeyPress()");

            // Pass in sender as an editor
            Control editingControl = (Control)sender;

            // Regex pattern for decimal values
            string pattern = @"^-?(0|[1-9]\d*)(\.\d+)?$";

            // Parse edited cell text to a double
            double.TryParse(editingControl.Text, out double result);

            // Check editor text against the pattern
            if (!Regex.IsMatch(result.ToString() + e.KeyChar, pattern))
            {
                e.Handled = true; // Not a pattern match, mark event handled which cancels the key press
            }

            // Valid text entered
            userCellEditPending = true;
        }

        private void Dgv_CellValidating(object sender, CancelEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_CellValidating()");

            // User cell edit pending is set when valid text has been entered into a cell. Because the cell validation
            // event fires even when selecting cells we need to prevent the below code snippet from running which would
            // block us from selecting more than 2 cells due to the keep cells selected after edit logic running which
            // will keep the same 2 cells selected even as we try to select more
            if (userCellEditPending)
            {
                // Pausing events whilst we're changing these cells
                myEvents.Pause_All();

                // If 2 or more cells are selected, the user can type a new value and
                // after committing the value, the new value is written to all the
                // selected cells.
                if (dgv.SelectedCells.Count >= 2 && dgv.CurrentCell.EditedFormattedValue != dgv.CurrentCell.Value)
                {
                    // Get the value of the current cell
                    double value = double.Parse(dgv.CurrentCell.EditedFormattedValue.ToString());

                    // Write the value to all selected cells
                    foreach (DataGridViewCell cell in dgv.SelectedCells)
                    {
                        WriteDt(cell.RowIndex, cell.ColumnIndex, value);
                    }

                    // Flag that we are needing to reload the selected cell collection after editing. Get a copy of the
                    // current selected cells collection and the address of the current cell. After the edit is complete
                    // these cell selections are restored. The default behaviour is to clear the selection and move the
                    // selected cell down 1 position similar to ms excel
                    keepCellsSelectedAfterEdit = true;
                    selectedCellsCollection_Copy = dgv.SelectedCells;
                    currentCell_Copy = dgv.CurrentCell;
                }
            }
        }

        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_CellEndEdit()");

            // Re adjust the cell widths if editing
            if (userCellEditPending)
                SetCellWidths();

            // After cell validation or if the edit was cancelled with escape we land here. The user cell edit pending
            // bit is reset
            userCellEditPending = false;

            // We can resume the events now. This will fire off one hit of the myEvents events
            myEvents.Resume_All();
        }

        private void Dgv_KeyUp(object sender, KeyEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_KeyUp()");

            if (incDecTask.Mode.Enabled)
                Dgv_IncDecKeyUp();

            dgv.ReadOnly = false;
        }

        private void Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_KeyDown()");

            // Initiate increment decrement mode
            if (DgvHasData)
            {
                if (Keyboard.IsKeyDown(Key.Add) || Keyboard.IsKeyDown(Key.Subtract))
                {
                    dgv.ReadOnly = true;

                    if (!incDecTask.Mode.Enabled)
                        Dgv_IncDecKeyDown();
                }
            }
        }

        // Cell clicks
        private void Dgv_Table_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_Table_CellClick()");

            // Temporarily mark the cell as read only. This prevents a cell click from putting the cell into edit mode
            // which really annoyed me. In the mouse up event the read only property is set back to false which allows
            // key presses etc to edit the cell
            dgv.ReadOnly = true;

            // Clear all selections if the control key is not pressed and then select this cell
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && e.RowIndex != -1 && e.ColumnIndex != -1)
            {
                dgv.SelectionChanged -= Dgv_SelectionChanged;

                dgv.ClearSelection();
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;

                dgv.SelectionChanged += Dgv_SelectionChanged;
            }
        }

        private void Dgv_Table_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Temporarily mark the cell as read only. This prevents a cell click from
            // putting the cell into edit mode which really annoyed me. In the timer task
            // the read only property is set back to false which allows key presses etc to
            // edit the cell
            dgv.ReadOnly = true;
        }

        private void RowHeader_CellClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine($"{InstanceName} - {ClassName} - RowHeader_CellClick()");

            // If the control keys are not down, clear all selections
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                dgv.ClearSelection();
                selectedRows.Clear();
            }

            // Is row already selected? If yes, de-select it
            if (selectedRows.Contains(e.RowIndex))
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    dgv.Rows[e.RowIndex].Cells[i].Selected = false;
                }
                while (selectedRows.Remove(e.RowIndex)) ;
                return;
            }

            // Iterate through each cell in the row and set its selected property to true
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                dgv.Rows[e.RowIndex].Cells[i].Selected = true;
            }

            selectedRows.Add(e.RowIndex);
        }

        private void ColHeader_CellClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (EventDebug)
                Console.WriteLine("${InstanceName} - {ClassName} - ColHeader_CellClick()");

            // If the control keys are not down, clear all selections
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                dgv.ClearSelection();
                selectedCols.Clear();
            }
            // Is column already selected? If yes, de-select it
            if (selectedCols.Contains(e.ColumnIndex))
            {
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    dgv.Rows[i].Cells[e.ColumnIndex].Selected = false;
                }
                while (selectedCols.Remove(e.ColumnIndex)) ;
                return;
            }

            // Iterate through each cell in the column and set its selected property to true
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                dgv.Rows[i].Cells[e.ColumnIndex].Selected = true;
            }

            selectedCols.Add(e.ColumnIndex);
        }

        private void ScrollBarCtrl_ResetRowHeaderPosition(object sender, EventArgs e)
        {
            dgvHeaders.ResetRowHeaderPosition();
        }

        private void ScrollBarCtrl_ResetColHeaderPosition(object sender, EventArgs e)
        {
            dgvHeaders.ResetColHeaderPosition();
        }

        private void ScrollBarCtrl_PushNewHeaderPosition(object sender, (bool, int) e)
        {
            dgvHeaders.PushNewHeaderLocation(e.Item1, e.Item2);
        }
        #endregion

        //------------------------- Inc Dec -------------------------------------------------------------------------------------------
        #region 
        private void Dgv_IncDecKeyDown()
        {
            if (incDecTask.Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_IncDecKeyDown()");

            // Data change events are paused. Handling is done by the IncDec_NDR event
            Events_CellDataAndSelectionChanged_Pause();

            // Called from the Dgv_EditingControlShowing or key down event, whichever occurs first and so long as the +
            // or - key is down
            incDecTask.Start();
        }

        private void Dgv_IncDecKeyUp()
        {
            if (incDecTask.Debug)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_IncDecKeyUp()");
                Console.WriteLine($"{InstanceName} - {ClassName} - Stop requested");
            }

            // All done, request stop
            incDecTask.StopRequest = true;

            // Restore the data change events
            Events_CellDataAndSelectionChanged_Resume();
        }

        public class IncDec
        #region
        {
            #region Properties & Variables
            public string ClassName { get; set; } = "IncDec";
            public string InstanceName { get { return instanceName; } set { instanceName = value; tmr_IncDec.DebugInstanceName = value; } }
            public bool Debug { get { return debug; } set { debug = value; tmr_IncDec.Debug = value; } }
            public bool StopRequest { get; set; } = false;

            DgvCtrl dgvCtrl;
            public TimerOnDelay tmr_IncDec;
            double initialIncrement;
            bool newSpeedMode;
            double increment = 0;
            bool debug;
            string instanceName;

            public event EventHandler IncDec_Incremental_NDR;
            public event EventHandler IncDec_Completed_NDR;

            public struct KeyIncDec
            {
                public bool Add;
                public bool Subtract;
                public int Counter;
                public bool Enabled;
            }
            public KeyIncDec Mode;

            public enum IncDecSpeedMode
            {
                LowLow,
                Low,
                Med,
                High,
                HighHigh
            }
            IncDecSpeedMode SpeedMode;
            #endregion

            public IncDec(DgvCtrl dgvCtrl)
            {
                this.dgvCtrl = dgvCtrl;

                // Timer setup
                tmr_IncDec = new TimerOnDelay
                {
                    Preset = 125,
                    AutoRestart = true,
                    OnTimingDone = IncDecTimer_Tick,
                    DebugTimerName = "tmr_IncDec"
                };
            }

            public void Start()
            {
                // This method is only called on the initial key press. Following on, with the
                // key held down the timer tick function is what runs the incrementing and
                // decrementing.  

                // We don't get the class instance name on application load so we'll grab it now
                tmr_IncDec.DebugInstanceName = InstanceName;

                // Accumulating counter to compare against to gradually increase the increment speed mode
                Mode.Counter = 0;

                // Calculate the initial increment based on the first value in the selected cell collection. The value
                // passed in is the formatted value as displayed in decimal format                
                string formattedValue = dgvCtrl.dgv.SelectedCells[0].FormattedValue.ToString();

                // Parsing the string to a decimal
                if (decimal.TryParse(formattedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    initialIncrement = InitialIncrement(result);
                }
                else
                {
                    initialIncrement = 1; // Default
                }

                if (Debug)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Initial increment = {initialIncrement}");
                }

                // Set enum properties
                SpeedMode = IncDecSpeedMode.LowLow;
                Mode.Enabled = true;
                newSpeedMode = true;

                // Latch in the key pressed state
                if (Keyboard.IsKeyDown(Key.Add))
                {
                    Mode.Add = true;
                    Mode.Subtract = false;
                }
                else if (Keyboard.IsKeyDown(Key.Subtract))
                {
                    Mode.Add = false;
                    Mode.Subtract = true;
                }

                // Start the timer to run the inc dec on the tick of the timer
                tmr_IncDec.Start();
            }

            private void IncDecTimer_Tick()
            {
                Mode.Counter++;

                if (Mode.Counter >= 30)
                {
                    SpeedMode = IncDecSpeedMode.HighHigh;
                    newSpeedMode = true;
                }
                else if (Mode.Counter >= 20)
                {
                    SpeedMode = IncDecSpeedMode.High;
                    newSpeedMode = true;
                }
                else if (Mode.Counter >= 10)
                {
                    SpeedMode = IncDecSpeedMode.Med;
                    newSpeedMode = true;
                }
                else if (Mode.Counter >= 5)
                {
                    SpeedMode = IncDecSpeedMode.Low;
                    newSpeedMode = true;
                }
                else if (Mode.Counter >= 0)
                {
                    SpeedMode = IncDecSpeedMode.LowLow;
                    newSpeedMode = true;
                }

                // Increment decrement the cell(s) value
                IncDecCellValue();
            }

            private void IncDecCellValue()
            {
                if (newSpeedMode)
                {
                    switch (SpeedMode)
                    {
                        case IncDecSpeedMode.LowLow:
                            increment = initialIncrement;
                            break;

                        case IncDecSpeedMode.Low:
                            increment = initialIncrement * 2;
                            break;

                        case IncDecSpeedMode.Med:
                            increment = initialIncrement * 4;
                            break;

                        case IncDecSpeedMode.High:
                            increment = initialIncrement * 8;
                            break;

                        case IncDecSpeedMode.HighHigh:
                            increment = initialIncrement * 16;
                            break;

                        default:
                            increment = 1;
                            break;
                    }

                    newSpeedMode = false;
                }

                // Increment or decrement every selected cell
                foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
                {
                    // Retrieve the respective element from the data table
                    double value = dgvCtrl.ReadDt(cell.RowIndex, cell.ColumnIndex);

                    // Increment or decrement its value
                    if (Mode.Add)
                    {
                        value += increment;
                    }
                    else if (Mode.Subtract)
                    {
                        value -= increment;
                    }

                    // Insert the updated value back onto the data table
                    dgvCtrl.WriteDt(cell.RowIndex, cell.ColumnIndex, value);
                }

                // Signal update
                IncDec_Incremental_NDR?.Invoke(null, EventArgs.Empty);

                if (Debug)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Increment  = {increment} ");
                    Console.WriteLine($"{InstanceName} - {ClassName} - Speed mode = {SpeedMode}");
                    Console.WriteLine($"{InstanceName} - {ClassName} - Raise IncDec_Incremental_NDR");
                }

                // If the + or - key is released the stop requested bit will be true, call stop function
                if (StopRequest)
                {
                    if (Debug)
                    {
                        Console.WriteLine($"{InstanceName} - {ClassName} - Stop request executing");
                    }
                    Stop();
                }
            }

            public void Stop()
            {
                // Stops the timers when the '+' or '-' key is released
                tmr_IncDec.Stop();

                // Resets
                Mode.Add = false;
                Mode.Subtract = false;
                Mode.Counter = 0;
                Mode.Enabled = false;
                StopRequest = false;

                if (Debug)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Stop executed");
                    Console.WriteLine($"{InstanceName} - {ClassName} - Fired IncDec_Completed_NDR()");
                }

                // Completed event
                IncDec_Completed_NDR?.Invoke(null, EventArgs.Empty);
            }

            public double InitialIncrement(decimal myValue)
            {
                // The initial increment is simply the formatted value of the first cell in the collection / 10. In the
                // edge case where the formatted value is 0.0x the increment amount is the last displayed decimal place 

                int intDigits, decDigits;

                // Gets the number of digit places each side of the decimal point. Includes 0's
                (intDigits, decDigits) = GetSignificantIntegerAndDecimalDigitPlaces(myValue);

                if (decDigits > 0)
                {
                    // The digit to increment is the last decimal place
                    initialIncrement = 1 * Math.Pow(10, -decDigits);

                    return initialIncrement;
                }

                // Initial increment is simply x order of magnitude less that the scientific notation of the integer
                // part Select here how aggressive you want the int part to be
                int modExponent = intDigits - 4;

                if (modExponent > 0)
                {
                    initialIncrement = 1 * Math.Pow(10, (double)modExponent);

                    return initialIncrement;
                }
                else
                {
                    // Exponent was + or -1
                    initialIncrement = 1;

                    return initialIncrement;
                }
            }

            private (int, int) GetSignificantIntegerAndDecimalDigitPlaces(decimal myValue)
            {
                // Returns the location of the most significant integer and decimal part of a number
                // 0.0    returns 0, 0
                // 10.001 returns 2, 3
                // 10     returns 2, 0
                // 0.001  returns 0, 3

                int sigWholeDigit;
                int sigDecimalDigit;

                // --------------------
                // Integer Part
                // --------------------
                //
                // Convert the value to int
                int iValue = (int)myValue;

                // Converting to string then counting the length gets the number of digits
                if (iValue > 0)
                {
                    sigWholeDigit = iValue.ToString().Length;
                }
                else
                {
                    sigWholeDigit = 0;
                }

                // --------------------
                // Decimal Part
                // --------------------
                //
                // Retrieve the current displayed number format. Note, this is seperate to the underlying datatable format
                string s = myValue.ToString();

                // Check the string contains a decimal place
                if (s.Contains("."))
                {
                    // Split the string using the decimal point as the separator and count current decimal places in the
                    // fractional part
                    string decimalPart = myValue.ToString().Split('.')[1];

                    // Converting to string then counting the length gets the number of decimal place digits
                    sigDecimalDigit = decimalPart.Length;
                }
                else
                {
                    sigDecimalDigit = 0;
                }

                // Return result
                return (sigWholeDigit, sigDecimalDigit);
            }
        }
        #endregion
        #endregion
    }
    #endregion

    public class DgvToGraph3d
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region
        public string ClassName { get; set; } = "DgvGraph3dInterface";
        public string InstanceName { get; set; }
        public bool MirrorPoints { get; set; } // Enabled in user settings
        public bool TransposeXY { get; set; }
        public double[] X_AxisLabels { get; set; } // e.g. 0, 500, 1000rpm ... Referenced to the dgv axes
        public double[] Y_AxisLabels { get; set; } // e.g. 0, 50, 100mg ... Referenced to the dgv axes
        public double[,] Z_Values { get; set; }
        public bool PointSelectMode { get; set; }
        public bool PointMoveMode { get; set; }
        public bool PointMoveInProgress { get { return pointMoveInProgress; } set { pointMoveInProgress = value; dgvCtrl.Z_RemoteValuesChanging = value; } }
        public MouseEventArgs MouseArgs { get; set; }

        // Debug
        public bool DebugAll { get; set; }
        public bool DebugData { get; set; }
        public bool DebugHoverPoint { get; set; }
        public bool DebugSelection { get; set; }
        public bool DebugPointMoveMode { get; set; }
        public bool DebugTimers { get; set; }
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region
        private DgvCtrl dgvCtrl;
        private Graph3dCtrl graph3dCtrl;
        private DgvData dgvDataPrev;

        // MyPoints classes
        MyPoints myPoints = new MyPoints();

        // Timer off delays (debounce timers)
        public TimerOnDelay tmr_zValuesToDgv_Intermittent;

        // Hover myPoints
        MyPoint dgv_HoverPoint;
        MyPoint graph3d_HoverPoint;
        MyPoint dgv_HoverPoint_Prev;
        MyPoint graph3d_HoverPoint_Prev;

        // Selection points
        public MyPoints dgv_Selections;
        private MyPoints graph3d_Selections;

        // Var to hold the new z values from a graph3d point move op
        List<cPoint3D> zValuesToDgv = new List<cPoint3D>();

        // Backing fields
        bool pointMoveInProgress;
        #endregion

        //------------------------- Constructor ---------------------------------------------------------------------------------------
        #region
        public DgvToGraph3d(DgvCtrl dgvCtrl, Graph3dCtrl graph3dCtrl, string instanceName)
        {
            // Debug
            InstanceName = instanceName;

            // Bring the instances of these classes in that were created on the form
            this.dgvCtrl = dgvCtrl;
            this.graph3dCtrl = graph3dCtrl;

            // Hover classes
            dgv_HoverPoint = new MyPoint();
            graph3d_HoverPoint = new MyPoint();
            dgv_HoverPoint_Prev = new MyPoint();
            graph3d_HoverPoint_Prev = new MyPoint();

            // Selection classes
            dgv_Selections = new MyPoints();
            graph3d_Selections = new MyPoints();

            // New dgv table data
            dgvCtrl.myEvents.DgvDataChanged_Debounced += MyEvents_Dgv_NDR_Debounced;

            // Debounce the selection changed event
            //dgvCtrl.dgv.SelectionChanged += Dgv_SelectionChanged;
            dgvCtrl.myEvents.DgvSelectionChanged_ToGraph3d_Immediate += MyEvents_Dgv_SelectionChanged;

            // New graph3d point drag data, plus mouse up to detect that we're finished dragging points
            graph3dCtrl.Graph3d_NDR += Graph3dCtrl_NDR;
            graph3dCtrl.graph3d.MouseUp += Graph3d_MouseUp;

            // Kicks off hover point mirror from dgv to graph3d
            dgvCtrl.dgv.CellMouseEnter += Dgv_CellMouseEnter;

            // Graph3d alt click selection
            graph3dCtrl.AltSelection += Graph3dCtrl_AltClickSelection;

            // Mouse click on the graph3d for 'target mode' point selection
            graph3dCtrl.graph3d.MouseClick += Graph3d_MouseClick;

            // Mouse move event for nearest point detection
            graph3dCtrl.graph3d.MouseMove += Graph3d_MouseMove;
            graph3dCtrl.graph3d.MouseHover += Graph3d_MouseHover;

            // Z values to dgv timer
            tmr_zValuesToDgv_Intermittent = new TimerOnDelay
            {
                Preset = 125,
                AutoRestart = true,
                OnTimingDone = Timer_zValuesToDgv_Tick,
                DebugInstanceName = InstanceName,
                DebugTimerName = "tmr_zValuesToDgv",
                Debug = DebugTimers
            };
        }
        #endregion

        //------------------------- Values --------------------------------------------------------------------------------------------
        #region
        // Draws plot when dgv signals to this it has new data
        private void MyEvents_Dgv_NDR_Debounced(object sender, DgvData e)
        {
            if (DebugAll || DebugData)
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_NDR_Debounced()");

            // Null checks
            if (e.RowHeaders == null || e.ColHeaders == null || e.TableData == null)
            {
                if (DebugAll || DebugData)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Returned from Dgv_NDR_Debounced(). Null checks failed");

                return;
            }

            // Update this class property
            X_AxisLabels = (double[])e.ColHeaders.Clone();
            Y_AxisLabels = (double[])e.RowHeaders.Clone();

            // MyPoints
            dgv_Selections.X_AxisLabels = X_AxisLabels;
            dgv_Selections.Y_AxisLabels = Y_AxisLabels;

            // MyPoints
            graph3d_Selections.X_AxisLabels = X_AxisLabels;
            graph3d_Selections.Y_AxisLabels = Y_AxisLabels;

            // Graph3d
            graph3dCtrl.X_AxisLabels = X_AxisLabels;
            graph3dCtrl.Y_AxisLabels = Y_AxisLabels;

            // Draw the plot if the headers have changed
            if (!e.HeadersEqual(dgvDataPrev))
            {
                // Blow away the hover points
                dgv_HoverPoint.Invalidate(); graph3d_HoverPoint.Invalidate();
                dgv_HoverPoint_Prev.Invalidate(); graph3d_HoverPoint_Prev.Invalidate();

                if (DebugAll || DebugHoverPoint)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_NDR_Debounced() Hover point invalidated");

                // And get rid of the selection points
                dgv_Selections.Clear();
                graph3d_Selections.Clear();

                // Redraws the plot with new axis labels
                graph3dCtrl.SetPlotData(e.ColHeaders, e.RowHeaders, e.TableData);
            }
            else // Update z values only
            {
                // Updates the plot using existing axis labels
                graph3dCtrl.UpdatePlot(e.ColHeaders, e.RowHeaders, e.TableData);
            }

            // Saves a copy of the event data for comparison next scan
            dgvDataPrev = e.Copy();
        }

        // From point drag selection callback
        private void Graph3dCtrl_NDR(object sender, List<cPoint3D> e)
        {
            if (DebugAll || DebugPointMoveMode)
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dCtrl_NDR");

            // Pause the firing of the dgv data changed events
            dgvCtrl.myEvents.Pause_DataFromGraph3d();

            // Set the in progress property, it is reset when the debounce timer completes. This also sets
            // dgvCtrl.Z_RemoteValuesChanging
            PointMoveInProgress = true;

            // Grab the updated z values from the graph3d event args
            zValuesToDgv = e;

            // Start the z value update timer. This timer intermittently fires to send the z values across to dgvCtrl.
            // The timer will run until the user stops dragging points I.e. when the left mouse button is released. 
            tmr_zValuesToDgv_Intermittent.Start();
        }

        // Point drag send z values to dgvCtrl intermittent update tick
        private void Timer_zValuesToDgv_Tick()
        {
            if (DebugAll || DebugPointMoveMode)
                Console.WriteLine($"{InstanceName} - {ClassName} - Timer_zValuesToDgv_Tick()");

            // Loop through all entries in the point list and break them out to the dgv cells
            foreach (cPoint3D p in zValuesToDgv)
            {
                int X, Y;

                // Convert axis tags to indexes
                if (TransposeXY)
                {
                    X = Array.FindIndex(X_AxisLabels, x => x == p.Y);
                    Y = Array.FindIndex(Y_AxisLabels, y => y == p.X);
                    dgvCtrl.WriteDt(Y, X, p.Z);
                }
                else
                {
                    X = Array.FindIndex(X_AxisLabels, x => x == p.X);
                    Y = Array.FindIndex(Y_AxisLabels, y => y == p.Y);
                    dgvCtrl.WriteDt(Y, X, p.Z);
                }
            }

            // Sets the cell width and cell colours
            dgvCtrl.Refresh(RefreshMode.Partial);
        }

        // When left mouse button raises the point drag is finished
        private void Graph3d_MouseUp(object sender, MouseEventArgs e)
        {
            if (!dgvCtrl.DgvHasData)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (DebugAll || DebugPointMoveMode)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Graph3d_MouseUp()");

                // Stop the z value update timer
                tmr_zValuesToDgv_Intermittent.Stop();

                // Reset the flag, the user has finished dragging points, also resets dgvCtrl.Z_RemoteValuesChanging
                PointMoveInProgress = false;

                // Call an undo capture
                dgvCtrl.Undo_Set(dgvCtrl.myEvents.BuildEventArgs_DgvDataChanged_Event());

                // Resume the dgv to this data event
                dgvCtrl.myEvents.Resume_DataFromGraph3d();
            }
        }
        #endregion

        //------------------------- Hover point ---------------------------------------------------------------------------------------
        #region
        // Dgv -> Graph3d, selects hover point
        private void Dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            // Check for valid conditions before continuing
            if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn || dgvCtrl.undo.InProgress || dgvCtrl.paste.InProgress || e.RowIndex == -1 || e.ColumnIndex == -1)
            {
                graph3d_HoverPoint.Invalidate();
                graph3d_HoverPoint_Prev.Invalidate();

                if (DebugAll || DebugHoverPoint)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_CellMouseEnter() Hover point conditions not valid");

                return;
            }

            // Builds a new fully defined hover from the event args. This could also include -1 x y values
            MyPoint myPoint = BuildPointFromDgvCellEventArgs(e);

            // If the point is valid, clone it as the current myPoint
            if (myPoint.IsValid())
                dgv_HoverPoint = (MyPoint)myPoint.Clone();

            // If this event fires with the left mouse button down we're dragging across the cells multi-selecting.
            bool multiCellSelectionInProgress = false;
            if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
            {
                multiCellSelectionInProgress = true;
            }

            // If the user is multiselecting cells and if the last point was classed as a hover selection, deselect the
            // hover point on the graph and return as we're no longer hovering.
            if (multiCellSelectionInProgress)
            {
                // Deselect the point over on the graph 
                if (dgv_HoverPoint_Prev.IsValid() && dgv_HoverPoint_Prev.HoverSelected)
                {
                    graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint_Prev.X_Index, dgv_HoverPoint_Prev.Y_Index).Selected = false;

                    // Clear the hover selection property.
                    dgv_HoverPoint_Prev.HoverSelected = false;

                    // Bug fix, mark the current cell as user selected. This stops an issue where the top left cell in a
                    // multiselect operation remains unselected
                    dgv_HoverPoint.UserSelected = true;
                }

                ClearHoverPoints();
                return;
            }

            // Deselect the previous point ONLY if it was a hover selection by us. This check is important as we don't
            // want to deselect a point that was selected by the user
            if (dgv_HoverPoint_Prev.IsValid() && dgv_HoverPoint_Prev.HoverSelected)
            {
                if (TransposeXY)
                    graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint_Prev.Y_Index, dgv_HoverPoint_Prev.X_Index).Selected = false;
                else
                    graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint_Prev.X_Index, dgv_HoverPoint_Prev.Y_Index).Selected = false;

                dgv_HoverPoint_Prev.HoverSelected = false;
            }

            // Select the current dgv cell hovered over on the graph only if this dgv cell is not selected.
            // dgv_HoverPoint.IsValid() is also checked to block a bare myPoint I.e. X=-1, Y=-1 which can occur on
            // startup
            if (dgv_HoverPoint.IsValid())
            {
                if (!dgvCtrl.dgv.Rows[dgv_HoverPoint.Y_Index].Cells[dgv_HoverPoint.X_Index].Selected)
                {
                    // Dgv cell is not selected which means we're hovering over this dgv cell. Select the corresponding
                    // point on the graph
                    if (TransposeXY)
                        graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint.Y_Index, dgv_HoverPoint.X_Index).Selected = true;
                    else
                        graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint.X_Index, dgv_HoverPoint.Y_Index).Selected = true;

                    // Mark this point as 'hover' selected
                    dgv_HoverPoint.HoverSelected = true;
                }
                else
                {
                    // Mark this point as 'user' selected. We don't bother selecting the coresponding point on the graph as
                    // the dgv selection changed event handles that for us
                    dgv_HoverPoint.UserSelected = true;
                }
            }

            // Copy this my point to the previous my point for use next time
            dgv_HoverPoint_Prev = (MyPoint)dgv_HoverPoint.Clone();

            // Redraws the plot to show / hide the hover point
            graph3dCtrl.Invalidate();

            if (DebugAll || DebugHoverPoint)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - dgv_HoverPoint" + dgv_HoverPoint.ToString());
            }
        }

        // Graph3d -> Dgv, selects hover point. Triggered from graph3d mouse move event
        private void Graph3dToDgv_SelectHoverPoint()
        {
            if (DebugAll)
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() start");

            // Check for valid conditions before continuing
            if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn || PointMoveMode)
            {
                graph3d_HoverPoint.Invalidate();
                graph3d_HoverPoint_Prev.Invalidate();

                if (DebugAll || DebugHoverPoint)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() Hover point invalidated");
                    Console.WriteLine($"{InstanceName} - {ClassName} - Exit from Graph3dToDgv_SelectHoverPoint {MirrorPoints} {dgvCtrl.DgvHasData} {graph3dCtrl.IsDrawn} {PointMoveMode}");
                }

                return;
            }

            // New hover point found flag
            bool newPointFound = false;

            // Get the point that is nearest to the mouse cursor. If not found an in-valid myPoint is returned
            MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

            // Add the hover point if a valid point on the graph is found and is unique. 
            if (hoverPoint.IsValid() && !graph3d_HoverPoint.Equals(hoverPoint))
            {
                newPointFound = true;

                // Shift the last point to the previous point
                graph3d_HoverPoint_Prev = (MyPoint)graph3d_HoverPoint.Clone();

                // Create the hover point from the newly found nearest point 
                graph3d_HoverPoint = (MyPoint)hoverPoint.Clone();

                if (DebugAll || DebugHoverPoint)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - graph3d_HoverPoint      " + graph3d_HoverPoint.ToString());
                    Console.WriteLine($"{InstanceName} - {ClassName} - graph3d_HoverPoint_Prev " + graph3d_HoverPoint_Prev.ToString());
                }
            }

            // New point found
            if (newPointFound)
            {
                // Select new graph point point only if it is not already selected over on the dgv. If the cell is
                // already selected on the dgv, then the user must have selected it
                if (TransposeXY)
                {
                    if (!dgvCtrl.dgv.Rows[graph3d_HoverPoint.X_Index].Cells[graph3d_HoverPoint.Y_Index].Selected)
                    {
                        // Temporarily selects the cell over on the dgv to mirror the current hover point
                        dgvCtrl.dgv.Rows[graph3d_HoverPoint.X_Index].Cells[graph3d_HoverPoint.Y_Index].Selected = true;

                        // Selects the point on the graph under the mouse
                        graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint.X_Index, graph3d_HoverPoint.Y_Index).Selected = true;

                        // Mark this hover point as hover selected
                        graph3d_HoverPoint.HoverSelected = true;
                    }
                    else
                    {
                        // Mark hover point as user selected
                        graph3d_HoverPoint.UserSelected = true;
                    }
                }
                else
                {
                    if (!dgvCtrl.dgv.Rows[graph3d_HoverPoint.Y_Index].Cells[graph3d_HoverPoint.X_Index].Selected)
                    {
                        // Temporarily selects the cell over on the dgv to mirror the current hover point
                        dgvCtrl.dgv.Rows[graph3d_HoverPoint.Y_Index].Cells[graph3d_HoverPoint.X_Index].Selected = true;

                        // Selects the point on the graph under the mouse
                        graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint.X_Index, graph3d_HoverPoint.Y_Index).Selected = true;

                        // Mark this hover point as hover selected
                        graph3d_HoverPoint.HoverSelected = true;
                    }
                    else
                    {
                        // Mark hover point as user selected
                        graph3d_HoverPoint.UserSelected = true;
                    }
                }

                // Deselect the previous point ONLY if it was selected by us. This check is important as we don't
                // want to deselect a point that was selected by the user 
                if (TransposeXY)
                {
                    if (graph3d_HoverPoint_Prev.HoverSelected)
                    {
                        // Deselect the cell on the dgv
                        dgvCtrl.dgv.Rows[graph3d_HoverPoint_Prev.X_Index].Cells[graph3d_HoverPoint_Prev.Y_Index].Selected = false;

                        // And deselect the previous point on the graph that was under the mouse
                        graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint_Prev.X_Index, graph3d_HoverPoint_Prev.Y_Index).Selected = false;

                        // Update previous point property
                        graph3d_HoverPoint_Prev.HoverSelected = false;
                    }
                }
                else
                {
                    if (graph3d_HoverPoint_Prev.HoverSelected)
                    {
                        // Deselect the cell on the dgv
                        dgvCtrl.dgv.Rows[graph3d_HoverPoint_Prev.Y_Index].Cells[graph3d_HoverPoint_Prev.X_Index].Selected = false;

                        // And deselect the previous point on the graph that was under the mouse
                        graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint_Prev.X_Index, graph3d_HoverPoint_Prev.Y_Index).Selected = false;

                        // Update previous point property
                        graph3d_HoverPoint_Prev.HoverSelected = false;
                    }
                }
            }

            // Invalidate the graph so it draws our new updated points
            graph3dCtrl.Invalidate();

            if (DebugAll)
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dToDgv_SelectHoverPoint() end");
        }

        // Clears all the hover point selections and invalidates the current and previous points
        public void ClearHoverPoints()
        {
            // Dgv hover point. Deselect the currently selected point
            if (dgv_HoverPoint.IsValid() && dgv_HoverPoint.HoverSelected)
            {
                if (TransposeXY)
                {
                    if (dgvCtrl.dgv.RowCount >= dgv_HoverPoint.Y_Index && dgvCtrl.dgv.ColumnCount >= dgv_HoverPoint.X_Index)
                        dgvCtrl.dgv.Rows[dgv_HoverPoint.Y_Index].Cells[dgv_HoverPoint.X_Index].Selected = false;
                    graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint.Y_Index, dgv_HoverPoint.X_Index).Selected = false;
                }
                else
                {
                    if (dgvCtrl.dgv.RowCount >= dgv_HoverPoint.Y_Index && dgvCtrl.dgv.ColumnCount >= dgv_HoverPoint.X_Index)
                        dgvCtrl.dgv.Rows[dgv_HoverPoint.Y_Index].Cells[dgv_HoverPoint.X_Index].Selected = false;
                    graph3dCtrl.i_Data.GetPointAt(dgv_HoverPoint.X_Index, dgv_HoverPoint.Y_Index).Selected = false;
                }
            }

            // Graph3d hover point. Deselect the currently selected point
            if (graph3d_HoverPoint.IsValid() && graph3d_HoverPoint.HoverSelected)
            {
                if (TransposeXY)
                {
                    dgvCtrl.dgv.Rows[graph3d_HoverPoint.X_Index].Cells[graph3d_HoverPoint.Y_Index].Selected = false;
                    graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint.X_Index, graph3d_HoverPoint.Y_Index).Selected = false;
                }
                else
                {
                    dgvCtrl.dgv.Rows[graph3d_HoverPoint.Y_Index].Cells[graph3d_HoverPoint.X_Index].Selected = false;
                    graph3dCtrl.i_Data.GetPointAt(graph3d_HoverPoint.X_Index, graph3d_HoverPoint.Y_Index).Selected = false;
                }
            }

            // Invalidate all hover points
            dgv_HoverPoint.Invalidate();
            graph3d_HoverPoint.Invalidate();
            dgv_HoverPoint_Prev.Invalidate();
            graph3d_HoverPoint_Prev.Invalidate();

            if (DebugAll || DebugHoverPoint)
                Console.WriteLine($"{InstanceName} - {ClassName} - Hover point invalidated");

            // Redraws the plot to show / hide the hover point
            graph3dCtrl.Invalidate();
        }
        #endregion

        //------------------------- Selection -----------------------------------------------------------------------------------------
        #region
        private void MyEvents_Dgv_SelectionChanged(object sender, DgvCtrl.MyEvents.SelectEventArgs e)
        {
            // Check for valid conditions before continuing
            if (!MirrorPoints || !dgvCtrl.DgvHasData || !graph3dCtrl.IsDrawn)
            {
                return;
            }

            if (DebugAll || DebugSelection)
                Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_Dgv_SelectionChanged");

            // Clear all selections out of both MyPoints selection classes. Cut throat but effective, we will then build
            // them up again fresh below
            dgv_Selections.Clear(); graph3d_Selections.Clear();

            // Deselect all graph points before re-selecting
            graph3dCtrl.ClearGraphSelection();

            // Add the selected cells
            dgv_Selections.Add(e.SelectedCellCollection);

            // Select the new points on the graph
            if (dgv_Selections.Count() > 0)
            {
                foreach (MyPoint pt in dgv_Selections)
                {
                    if (TransposeXY)
                        graph3dCtrl.i_Data.GetPointAt(pt.Y_Index, pt.X_Index).Selected = true;
                    else
                        graph3dCtrl.i_Data.GetPointAt(pt.X_Index, pt.Y_Index).Selected = true;
                }
            }

            // Mark the previous hover point as user selected if it exists within this selected collection
            if (dgv_HoverPoint.IsValid())
                if (dgv_Selections.Contains(dgv_HoverPoint_Prev))
                    dgv_HoverPoint_Prev.UserSelected = true;

            // Redraws the plot to show / hide the new selections
            graph3dCtrl.Invalidate();
        }

        private void Graph3dCtrl_AltClickSelection(object sender, cObject3D e)
        {
            // Toggling the previous hover point user selection var gives the desired action of selecting / deselecting
            // the graph point 
            graph3d_HoverPoint.UserSelected = !graph3d_HoverPoint.UserSelected;

            if (graph3d_HoverPoint.UserSelected)
                graph3d_HoverPoint.HoverSelected = false;

            if (DebugAll || DebugHoverPoint)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Graph3dCtrl_AltClickSelection()");
                Console.WriteLine($"{InstanceName} - {ClassName} - graph3d_HoverPoint " + graph3d_HoverPoint.ToString());
            }
        }

        private void Graph3d_MouseClick(object sender, MouseEventArgs e)
        {
            if (!PointSelectMode)
                return;

            // Update to current mouse state
            MouseArgs = e;

            // Selection of a point in target mode
            MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(e);

            if (hoverPoint == null)
                return;

            if (!hoverPoint.Found)
                return;

            // Toggling the previous hover point user selection var gives the desired action of selecting / deselecting
            // the graph point 
            graph3d_HoverPoint_Prev.UserSelected = !graph3d_HoverPoint_Prev.UserSelected;
        }

        private void Graph3d_MouseMove(object sender, MouseEventArgs e)
        {
            // Load mouse state into MouseArgs property. This is used by others
            MouseArgs = e;

            // Return if point dragging
            if (PointMoveInProgress)
                return;

            // Pause the dgv selection change events whilst we create a hover point
            dgvCtrl.myEvents.Pause_SelectionFromGraph3d();

            // Hover point selection
            Graph3dToDgv_SelectHoverPoint();

            // Resume the dgv selection change events after the hover point is selected
            dgvCtrl.myEvents.Resume_SelectionFromGraph3d();

            // Get the point that is nearest to the mouse cursor. If not found an in-valid myPoint is returned
            MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

            // If alt key is pressed or point select mode is active and we are over a valid point change the mouse cursor icon to crosshairs
            if (((Control.ModifierKeys & Keys.Alt) == Keys.Alt || PointSelectMode) && hoverPoint.IsValid())
            {
                graph3dCtrl.graph3d.Cursor = Cursors.Cross;
            }
            else
            {
                graph3dCtrl.graph3d.Cursor = Cursors.Default;
            }
        }

        private void Graph3d_MouseHover(object sender, EventArgs e)
        {
            // Get the point that is nearest to the mouse cursor. If not found an in-valid myPoint is returned
            MyPoint hoverPoint = graph3dCtrl.GetNearestPoint(MouseArgs);

            // If alt key is pressed or point select mode is active and we are over a valid point change the mouse cursor icon to crosshairs
            if (((Control.ModifierKeys & Keys.Alt) == Keys.Alt || PointSelectMode) && hoverPoint.IsValid())
            {
                graph3dCtrl.graph3d.Cursor = Cursors.Cross;
            }
            else
            {
                graph3dCtrl.graph3d.Cursor = Cursors.Default;
            }
        }
        #endregion

        //------------------------- General -------------------------------------------------------------------------------------------
        #region
        private MyPoint BuildPointFromDgvCellEventArgs(DataGridViewCellEventArgs e)
        {
            MyPoint pt = new MyPoint();

            // Get row and column indexes
            pt.X_Index = e.ColumnIndex;
            pt.Y_Index = e.RowIndex;

            // Check for valid indexes
            if (pt.X_Index == -1 || pt.Y_Index == -1)
                return new MyPoint(); // returns an empty myPoint

            // Get the z value which is used in the hashcode calculation
            pt.Z = (double)dgvCtrl.dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            // Get the axis tags 
            pt.X_AxisTag = X_AxisLabels[e.ColumnIndex];
            pt.Y_AxisTag = Y_AxisLabels[e.RowIndex];

            // Hash code calc
            pt.HashCode = pt.GetHashCode();

            return pt;
        }
        #endregion

        //------------------------- Classes -------------------------------------------------------------------------------------------
        #region
        public class MyPoints : List<MyPoint>, ICloneable
        #region
        {
            // Axis label arrays
            public double[] X_AxisLabels { get; set; }
            public double[] Y_AxisLabels { get; set; }

            internal bool Exists(MyPoint myPoint)
            {
                if (Contains(myPoint))
                    return true;
                else
                    return false;
            }

            public new bool Add(MyPoint newPoint)
            {
                // Only unique points can be added
                if (!Contains(newPoint))
                {
                    base.Add(newPoint);
                    return true; // Added
                }
                else
                {
                    return false; // Rejected
                }
            }

            public bool Add(cObject3D obj)
            {
                MyPoint newPoint = ConvertToMyPoint(obj);

                // Only unique points can be added
                if (!Contains(newPoint))
                {
                    base.Add(newPoint);
                    return true; // Added
                }
                else
                {
                    return false; // Rejected
                }
            }

            public bool Add(DataGridViewCell obj)
            {
                MyPoint newPoint = ConvertToMyPoint(obj);

                // Only unique points can be added
                if (!Contains(newPoint))
                {
                    base.Add(newPoint);
                    return true; // Added
                }
                else
                {
                    return false; // Rejected
                }
            }

            public void Add(DataGridViewSelectedCellCollection obj)
            {
                MyPoint newPoint;

                foreach (DataGridViewCell c in obj)
                {
                    newPoint = ConvertToMyPoint(c);

                    if (!Contains(newPoint))
                        base.Add(newPoint);
                }
            }

            public override string ToString()
            {
                string[] pointStringArray = new string[this.Count];
                int i = 0;
                string result = "";

                foreach (MyPoint myPoint in this)
                {
                    pointStringArray[i] = myPoint.ToString();
                    i++;
                }

                if (pointStringArray.Count() > 1)
                {
                    foreach (string s in pointStringArray)
                    {
                        result += s + "\r"; // Multiple entries
                    }
                }
                else
                {
                    foreach (string s in pointStringArray)
                    {
                        result += s; // Single entry
                    }
                }

                return result;
            }

            public object Clone()
            {
                MyPoints clone = new MyPoints();

                foreach (var point in this)
                {
                    if (point == null)
                        break;

                    // Clone each MyPoint object and add it to the cloned list
                    clone.Add((MyPoint)point.Clone());
                }

                return clone;
            }

            //public int Count()
            //{
            //    return base.Count;
            //}

            public MyPoint ConvertToMyPoint(DataGridViewCell obj)
            {
                MyPoint newPoint = new MyPoint();

                newPoint.X_Index = obj.ColumnIndex;
                newPoint.Y_Index = obj.RowIndex;
                newPoint.Z = double.Parse(obj.Value.ToString());

                newPoint.UserSelected = obj.Selected;

                newPoint.X_AxisTag = X_AxisLabels[newPoint.X_Index];
                newPoint.Y_AxisTag = Y_AxisLabels[newPoint.Y_Index];

                newPoint.HashCode = newPoint.GetHashCode();

                // Returns a fully defined myPoint
                return newPoint;
            }

            public MyPoint ConvertToMyPoint(DataGridViewSelectedCellCollection obj)
            {
                MyPoint newPoint = new MyPoint();

                foreach (DataGridViewCell cell in obj)
                {
                    newPoint.X_Index = cell.ColumnIndex;
                    newPoint.Y_Index = cell.RowIndex;
                    newPoint.Z = double.Parse(cell.Value.ToString());

                    newPoint.HoverSelected = cell.Selected;

                    newPoint.X_AxisTag = X_AxisLabels[newPoint.X_Index];
                    newPoint.Y_AxisTag = Y_AxisLabels[newPoint.Y_Index];

                    //newPoint.SelectionSource = MyPoint.SelectSource.Dgv;

                    newPoint.HashCode = newPoint.GetHashCode();
                }

                // Returns a fully defined myPoint
                return newPoint;
            }

            public MyPoint ConvertToMyPoint(cObject3D obj)
            {
                MyPoint newPoint = new MyPoint();

                newPoint.X_AxisTag = obj.Points[0].X;
                newPoint.Y_AxisTag = obj.Points[0].Y;
                newPoint.Z = obj.Points[0].Z;

                newPoint.HoverSelected = obj.Points[0].Selected;

                newPoint.X_Index = Array.FindIndex(X_AxisLabels, x => x == newPoint.X_AxisTag);
                newPoint.Y_Index = Array.FindIndex(Y_AxisLabels, y => y == newPoint.Y_AxisTag);

                //newPoint.SelectionSource = MyPoint.SelectSource.Graph;

                newPoint.HashCode = newPoint.GetHashCode();

                // Returns a fully defined myPoint
                return newPoint;
            }

            public MyPoint ConvertToMyPoint(cPoint3D obj)
            {
                MyPoint newPoint = new MyPoint();

                newPoint.X_AxisTag = obj.X;
                newPoint.Y_AxisTag = obj.Y;
                newPoint.Z = obj.Z;

                newPoint.HoverSelected = obj.Selected;

                newPoint.X_Index = Array.FindIndex(X_AxisLabels, x => x == newPoint.X_AxisTag);
                newPoint.Y_Index = Array.FindIndex(Y_AxisLabels, y => y == newPoint.Y_AxisTag);

                //newPoint.SelectionSource = MyPoint.SelectSource.Graph;

                newPoint.HashCode = newPoint.GetHashCode();

                // Returns a fully defined myPoint
                return newPoint;
            }

            public MyPoints() { }

            public MyPoints(DataGridViewSelectedCellCollection selectedCells)
            {
                foreach (DataGridViewCell cell in selectedCells)
                {
                    base.Add(ConvertToMyPoint(cell));
                }
            }
        }
        #endregion

        public class MyPoint : IEquatable<MyPoint>, ICloneable
        #region
        {
            // ------------------------ Properties ------------------------------------------------------------------------------------
            #region
            // Bools
            public bool Found { get; set; }

            // Axis array indexes
            public int X_Index { get; set; }
            public int Y_Index { get; set; }
            public double Z { get; set; }

            // Axis individual values
            public double X_AxisTag { get; set; }
            public double Y_AxisTag { get; set; }

            // Axis label arrays, on first load the AxisLabels are stored in a temp var. The transpose flag is read to
            // determine which way we store the axis tags.
            public double[] X_AxisLabels { get; set; }
            public double[] Y_AxisLabels { get; set; }

            // Selection
            public bool UserSelected { get { return userSelected; } set { if (HoverSelected) HoverSelected = false; userSelected = value; } }
            public bool HoverSelected { get { return hoverSelected; } set { if (UserSelected) UserSelected = false; hoverSelected = value; } }
            public int HashCode { get; set; }

            bool userSelected = false;
            bool hoverSelected = false;
            #endregion

            // Methods -------------------------------------------------------------------------------------------
            public override bool Equals(object obj)
            {
                return Equals(obj as MyPoint);
            }

            public bool Equals(MyPoint myPointToAdd)
            {
                // Returns true if match found
                if (!(myPointToAdd is null) && HashCode == myPointToAdd.HashCode)
                {
                    // Match found, further checks required. Compare x and y indexes
                    if (X_Index == myPointToAdd.X_Index && Y_Index == myPointToAdd.Y_Index)
                    {
                        return true; // Match confirmed
                    }
                    else
                    {
                        return false; // myPointToAdd is unique
                    }
                }
                return false; // myPointToAdd is unique
            }

            public override int GetHashCode()
            {
                int hashCode = -2;
                hashCode = hashCode * 3 + X_Index.GetHashCode();
                hashCode = hashCode * 5 + Y_Index.GetHashCode();
                hashCode = hashCode * 7 + Z.GetHashCode();
                return hashCode;
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return String.Format("(X={0}, Y={1},  Z={2}, UsrSlctn={3}, HvrSlctn={4})",
                    X_Index.ToString(), Y_Index.ToString(), Z.ToString(Utils.FormatDouble(Z)), UserSelected, HoverSelected);
            }

            public object Clone()
            {
                MyPoint clone = new MyPoint();

                clone.Found = Found;

                clone.X_Index = X_Index;
                clone.Y_Index = Y_Index;
                clone.Z = Z;

                clone.X_AxisTag = X_AxisTag;
                clone.Y_AxisTag = Y_AxisTag;

                clone.X_AxisLabels = X_AxisLabels;
                clone.Y_AxisLabels = Y_AxisLabels;

                clone.UserSelected = UserSelected;
                clone.HoverSelected = HoverSelected;

                clone.HashCode = HashCode;

                return clone;
            }

            public bool IsValid()
            {
                if (X_Index != -1 && Y_Index != -1)// && HashCode != 0) // && Z != -1)
                    return true;
                else
                    return false;
            }

            public void Invalidate()
            {
                Found = false;

                X_Index = -1;
                Y_Index = -1;
                Z = -1;

                X_AxisTag = -1;
                Y_AxisTag = -1;

                X_AxisLabels = new double[0];
                Y_AxisLabels = new double[0];

                UserSelected = false;
                HoverSelected = false;

                HashCode = 0;
            }

            public MyPoint ConvertToMyPoint(DataGridViewCell obj)
            {
                MyPoint newPoint = new MyPoint();

                newPoint.X_Index = obj.ColumnIndex;
                newPoint.Y_Index = obj.RowIndex;
                newPoint.Z = double.Parse(obj.Value.ToString());

                newPoint.X_AxisTag = X_AxisLabels[newPoint.X_Index];
                newPoint.Y_AxisTag = Y_AxisLabels[newPoint.Y_Index];

                newPoint.HashCode = newPoint.GetHashCode();

                // Returns a fully defined myPoint
                return newPoint;
            }

            // Constructor - Empty myPoint
            public MyPoint()
            {
                Invalidate();
            }

            // Constructor - Fully defined, from dgv cell
            public MyPoint(DataGridViewCell obj)
            {
                ConvertToMyPoint(obj);
            }

            // Constructor - Fully defined, gets the index from the axis labels
            public MyPoint(double X_AxisLabel, double Y_AxisLabel, double[] X_AxisLabels, double[] Y_AxisLabels, double Z, bool isUserSelected = false, bool isHoverSelected = false)
            {
                // Add the point data to the point properties
                this.X_Index = Array.FindIndex(X_AxisLabels, x => x == X_AxisLabel);
                this.Y_Index = Array.FindIndex(Y_AxisLabels, y => y == Y_AxisLabel);
                this.Z = Z;

                // Point selection states
                HoverSelected = isHoverSelected;
                UserSelected = isUserSelected;

                // Generates a unique hashcode derived from the x, y and z values
                HashCode = GetHashCode();
            }
        }
        #endregion
    }
    #endregion
    #endregion

    public class Graph3dCtrl
    #region
    {
        //------------------------- Default settings ----------------------------------------------------------------------------------
        #region

        // Home rotation defaults
        const int DEFAULT_ROTATION = 210;
        const int DEFAULT_ROTATION_TRANSPOSED = 240;
        const int DEFAULT_ZOOM = 1350;
        const int DEFAULT_ELEVATION = 80;

        // Display setting defaults
        const bool SHOW_AXIS = true;
        const bool SHOW_AXIS_LABELS = true;
        const bool SHOW_TOOL_TIP = false;
        const bool SHOW_HOVER_POINT = false;
        const bool SHOW_GRAPH_POSITION = true;
        static Color SEL_POINT_COLOUR = Color.Black;
        const float SEL_POINT_SIZE = 1.0F; // 0.8 - 1.2 is good
        const bool SHOW_LEGEND = false;
        const bool MIRROR_X_AXIS = true;
        const bool MIRROR_Y_AXIS = true;

        // Select defaults
        const int TOOLTIP_RADIUS = 6;
        const int SELECT_RADIUS = 32;

        #endregion

        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region
        // Debug
        public string ClassName { get; set; } = "Graph3dCtrl";
        public string InstanceName { get; set; }

        // User settings
        public int Zoom
        {
            get { return zoom; }
            set
            {
                if (zoom != value)
                {
                    zoom = value;

                    updatePositionFlag = true;

                    if (IsDrawn)
                        DrawPlot();
                }
            }
        }
        public int Elevation
        {
            get { return elevation; }
            set
            {
                if (elevation != value)
                {
                    elevation = value;

                    updatePositionFlag = true;

                    if (IsDrawn)
                        DrawPlot();
                }
            }
        }
        public int Rotation
        {
            get { return rotation; }
            set
            {
                if (rotation != value)
                {
                    rotation = value;

                    updatePositionFlag = true;

                    if (IsDrawn)
                        DrawPlot();
                }
            }
        }
        public int RotationTransposed
        {
            get { return rotation_Transposed; }
            set
            {
                if (rotation_Transposed != value)
                {
                    rotation_Transposed = value;

                    updatePositionFlag = true;

                    if (IsDrawn)
                        DrawPlot();
                }
            }
        }
        public bool ShowAxis
        {
            get { return showAxis; }
            set
            {
                showAxis = value;
            }
        }
        public bool ShowAxisLabels
        {
            get { return showAxisLabels; }
            set
            {
                showAxisLabels = value;
            }
        }
        public bool MirrorPoints { get; set; }
        public bool ShowToolTip
        {
            get { return showToolTip; }
            set { showToolTip = value; }
        }
        public bool ShowGraphPosition
        {
            get { return showGraphPosition; }
            set
            {
                showGraphPosition = value;
            }
        }
        public Color SelectPointColour
        {
            get { return selPointColour; }
            set
            {
                selPointColour = value;

                if (IsDrawn)
                    DrawPlot();
            }
        }
        public float SelectPointSize
        {
            get { return selPointSize; }
            set
            {
                selPointSize = value;

                if (IsDrawn)
                    DrawPlot();
            }
        }
        public bool ShowLegend
        {
            set { showLegend = value; }
        }
        public bool MirrorXAxis
        {
            get { return mirrorXAxis; }
            set { mirrorXAxis = value; }
        }
        public bool MirrorYAxis
        {
            get { return mirrorYAxis; }
            set { mirrorYAxis = value; }
        }
        public bool Focused { get { return graph3d.Focused; } set { graph3d.Focus(); } }

        // Data set
        public bool TransposeXY { get { return transposeXY; } set { transposeXY = value; Transpose(); } }
        public bool IsDrawn { get; private set; }
        public cPoint3D[] SelectedPoints { get { return graph3d.Selection.GetSelectedPoints(eSelType.All); } }
        public double[] X_AxisLabels { get { return x_ValuesFromDgv; } set { x_ValuesFromDgv = value; x_ValuesToGraph = value; } } // X axis tags e.g. 0, 500, 1000rpm ... 
        public double[] Y_AxisLabels { get { return y_ValuesFromDgv; } set { y_ValuesFromDgv = value; y_ValuesToGraph = value; } } // X axis tags e.g. 0, 50, 100mg ... 
        public double[,] Z_Values { get { return z_ValuesFromDgv; } set { z_ValuesFromDgv = value; z_ValuesToGraph = value; } }


        // Tool tip (enable bit is located in user settings. 15/05/24 deleted)
        public string ToolTipTextTitle
        {
            set { toolTipTextTitle = value; }
        }
        public string X_Unit
        {
            set { x_Unit = " " + value; }
        }
        public string Y_Unit
        {
            set { y_Unit = " " + value; }
        }
        public string Z_Unit
        {
            set { z_Unit = " " + value; }
        }
        public int Tooltip_Radius
        {
            get { return toolTip_Radius; }
            set { toolTip_Radius = value; }
        }
        public int SelectRadius
        {
            get { return select_Radius; }
            set { select_Radius = value; }
        }

        // Legend text (enable bit is located in settings)
        public string Legend_X_Axis
        {
            set { legend_X_Axis = value; }
        }
        public string Legend_Y_Axis
        {
            set { legend_Y_Axis = value; }
        }
        public string Legend_Z_Axis
        {
            set { legend_Z_Axis = value; }
        }

        // Physical
        public Size ClientSize { get { return graph3d.ClientSize; } }

        // Debug
        public bool DebugPointSelectMode { get; set; }
        public bool DebugPointMoveMode { get; set; }
        public bool DebugData { get; set; }
        public bool DebugData_WithPrint { get; set; }

        // Myinterface
        public DgvToGraph3d.MyPoints UserSelectedMyPoints { get; set; }
        #endregion

        //------------------------- Backing Fields ------------------------------------------------------------------------------------
        #region
        // Tooltip
        string toolTipTextTitle = "";
        string toolTipText = "";
        string x_Unit = "";
        string y_Unit = "";
        string z_Unit = "";

        // Settings
        int zoom = DEFAULT_ZOOM;
        int elevation = DEFAULT_ELEVATION;
        int rotation = DEFAULT_ROTATION;
        int rotation_Transposed = DEFAULT_ROTATION_TRANSPOSED;
        bool showAxis = SHOW_AXIS;
        bool showAxisLabels = SHOW_AXIS_LABELS;
        //bool    MirrorPoints        =   SHOW_HOVER_POINT;
        bool showToolTip = SHOW_TOOL_TIP;
        Color selPointColour = SEL_POINT_COLOUR;
        float selPointSize = SEL_POINT_SIZE;
        bool showGraphPosition = SHOW_GRAPH_POSITION;
        bool showLegend = SHOW_LEGEND;
        bool mirrorXAxis = MIRROR_X_AXIS;
        bool mirrorYAxis = MIRROR_Y_AXIS;
        int toolTip_Radius = TOOLTIP_RADIUS;
        int select_Radius = SELECT_RADIUS;

        // Axis text
        string legend_X_Axis = "";
        string legend_Y_Axis = "";
        string legend_Z_Axis = "";
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region
        // Classes
        public Editor3D graph3d;

        // Graph values, local copies that can be transposed
        double[] x_ValuesToGraph;
        double[] y_ValuesToGraph;
        double[,] z_ValuesToGraph;

        // Graph values, these are the tags sent from the table editor. These are not transposed and are used for the
        // reverse lookup to convert point selection
        double[] x_ValuesFromDgv;
        double[] y_ValuesFromDgv;
        double[,] z_ValuesFromDgv;

        // Plot 3D
        bool resetViewPosition;
        bool transposeXY;

        // Update zoom
        bool updatePositionFlag = false;

        // Transpose special rotation flag
        bool transposeReq;

        // Interface
        public MyPoints local_Selection;
        public cSurfaceData i_Data;
        public List<cPoint3D> PointsMoved;
        public event EventHandler<cObject3D> AltSelection;
        public event EventHandler<List<cPoint3D>> Graph3d_NDR;
        #endregion

        //------------------------- Constructor ---------------------------------------------------------------------------------------
        #region
        public Graph3dCtrl(Editor3D Graph3d_UserControl, string instanceName)
        {
            graph3d = Graph3d_UserControl; // Name of winform user control passed in here

            // Options
            LoadOptions();

            // Debug
            InstanceName = instanceName;
        }
        #endregion

        //------------------------- Graph3D -------------------------------------------------------------------------------------------
        #region
        public void SetPlotData(double[] x, double[] y, double[,] z) // Master plot data, call during paste ops, new dgv etc.
        {
            if (!ValidValuesToGraph(x, y, z))
            {
                if (DebugData)
                    Console.WriteLine($"{InstanceName} - {ClassName} - SetPlotData() returned, graph values invalid");

                if (DebugData_WithPrint)
                {
                    // Prints all the recieved data in space delimited format
                    PrintGraphValues(x, y, z);
                }

                graph3d.Clear(); // Erases the plot from the screen
                return;
            }

            if (DebugData)
                Console.WriteLine($"{InstanceName} - {ClassName} - SetPlotData(x, y, z)");

            if (DebugData_WithPrint)
            {
                // Prints all the recieved data in space delimited format
                PrintGraphValues(x, y, z);
            }

            ClearGraphSelection(); // Removes selection points from the graph

            // Save a copy of the values
            x_ValuesFromDgv = x;
            y_ValuesFromDgv = y;
            z_ValuesFromDgv = z;

            if (!transposeReq)
                ResetViewPosition(); // Set view back to home only if not transposing

            UpdatePlot(x, y, z); // Sets up the graph points, handles transposing and draws the plot

            IsDrawn = true;
        }

        public void DrawPlot()
        {
            if (!ValidValuesToGraph(x_ValuesToGraph, y_ValuesToGraph, z_ValuesToGraph))
            {
                if (DebugData)
                    Console.WriteLine($"{InstanceName} - {ClassName} - DrawPlot() returned, graph values invalid");

                if (DebugData_WithPrint)
                {
                    // Prints all the recieved data in space delimited format
                    PrintGraphValues(x_ValuesToGraph, y_ValuesToGraph, z_ValuesToGraph);
                }

                graph3d.Clear(); // Erases the plot from the screen
                return;
            }

            if (DebugData)
                Console.WriteLine($"{InstanceName} - {ClassName} - DrawPlot()");

            int cols = x_ValuesToGraph.Length;
            int rows = y_ValuesToGraph.Length;

            // In Fill mode the pen is used to draw the thin separator lines (always 1 pixel, black)
            ePolygonMode e_Mode   = ePolygonMode.Fill; // default
            Pen          i_Pen    = (e_Mode == ePolygonMode.Lines) ? new Pen(Color.Yellow, 2) : Pens.Black;
            cColorScheme i_Scheme = new cColorScheme(eColorScheme.HP_Tuners);
                         i_Data   = new cSurfaceData(cols, rows, e_Mode, i_Pen, i_Scheme);

            // Loop through the z values to add those points to the graph. Also create the
            // tool tip text for hovering over a data point
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // Coordinates
                    double X = x_ValuesToGraph[x];
                    double Y = y_ValuesToGraph[y];
                    double Z = z_ValuesToGraph[y, x];

                    // Create graph point
                    cPoint3D i_Point = new cPoint3D(X, Y, Z, toolTipText, Z);

                    // Set the graph point
                    i_Data.SetPointAt(x, y, i_Point);
                }
            }

            // Get a copy of all the selected points
            cPoint3D[] selPoints = graph3d.Selection.GetSelectedPoints(eSelType.All);

            // Clear plot
            graph3d.Clear();

            LoadOptions();

            // Engine
            graph3d.AddRenderData(i_Data);

            // Recalculate the plot
            graph3d.Invalidate();

            // Load back the selected points after drawing otherwise they will be lost
            ReloadSelectedPoints(selPoints);
        }

        private void ReloadSelectedPoints(cPoint3D[] selPoints)
        {
            // Load back the selections after drawing
            if (selPoints.Length > 0)
            {
                foreach (cPoint3D pt in selPoints)
                {
                    int xIndex = Array.FindIndex(x_ValuesToGraph, x => x == pt.X);
                    int yIndex = Array.FindIndex(y_ValuesToGraph, y => y == pt.Y);

                    i_Data.GetPointAt(xIndex, yIndex).Selected = true;
                }
            }
        }

        private void LoadOptions()
        {
            // Modes
            graph3d.Normalize = eNormalize.Separate;
            graph3d.Selection.SinglePoints = true;
            Editor3D.SelSizeK = selPointSize; // Set to 1 or lower for large tables I.e. VE table
            Editor3D.SelectRadius = select_Radius;
            Editor3D.ToolTipRadius = toolTip_Radius;

            // User input
            graph3d.SetUserInputs(eMouseCtrl.L_Theta_L_Phi);

            // Tool tips & hover point
            if (showToolTip && !MirrorPoints) graph3d.TooltipMode = eTooltip.Coord;
            else if (!showToolTip && MirrorPoints) graph3d.TooltipMode = eTooltip.Hover;
            else if (!showToolTip && !MirrorPoints) graph3d.TooltipMode = eTooltip.Off;

            // Appearance            
            graph3d.BackColor = SystemColors.Control;
            graph3d.BorderColorFocus = SystemColors.Control;
            graph3d.BorderColorNormal = SystemColors.Control;
            graph3d.Selection.HighlightColor = selPointColour; // Color.FromArgb(255, 0, 0); // Red

            // Axis
            graph3d.AxisX.Mirror = mirrorXAxis;
            graph3d.AxisY.Mirror = mirrorYAxis;
            graph3d.AxisX.LegendText = legend_X_Axis;
            graph3d.AxisY.LegendText = legend_Y_Axis;
            graph3d.AxisZ.LegendText = legend_Z_Axis;
            graph3d.LegendPos = eLegendPos.BottomLeft;

            // Show axis lines & show labels
            if (showAxis && !showAxisLabels) graph3d.Raster = eRaster.MainAxes;
            else if (showAxis && showAxisLabels) graph3d.Raster = eRaster.Labels;
            else if (!showAxis) graph3d.Raster = eRaster.Off;

            // Display            
            if (!IsDrawn || resetViewPosition || updatePositionFlag || transposeReq)
            {
                if (transposeReq)
                    if (TransposeXY)
                        rotation -= 30;
                    else
                        rotation += 30;
                else if (!TransposeXY)
                    graph3d.SetCoefficients(zoom, elevation, rotation);
                else
                    graph3d.SetCoefficients(zoom, elevation, rotation_Transposed);
            }
            updatePositionFlag = false;
            transposeReq = false;

            // Graph position
            if (showGraphPosition) graph3d.TopLegendColor = Color.Black;
            else graph3d.TopLegendColor = Color.Empty;

            // Select options
            graph3d.Selection.Callback = OnSelectEvent;
            graph3d.Selection.MultiSelect = false;
            graph3d.Selection.Enabled = true;
            graph3d.Selection.SinglePoints = true;
            graph3d.UndoBuffer.Enabled = false;

            // Reset view
            resetViewPosition = false;
        }

        public void Invalidate()
        {
            graph3d.Invalidate();
        }

        public void Reset()
        {
            graph3d.Clear();

            graph3d.Invalidate();
        }

        public void ClearGraphSelection()
        {
            graph3d.Selection.DeSelectAll();

            Invalidate();
        }

        public bool ValidValuesToGraph(double[] x, double[] y, double[,] z)
        {
            // Rwturn if each values array fails checks. Minimum graph size is 3 x 3
            if (x == null || x.Length < 3)
                return false;

            if (y == null || y.Length < 3)
                return false;

            if (z == null || z.GetLength(0) < 3 || z.GetLength(1) < 3)
                return false;

            // Values ok
            return true;
        }

        public MyPoint GetNearestPoint(MouseEventArgs e)
        {
            // If we have a nearest point, build up the information on the point. Or if none, reset values to default
            MyPoint myPoint = new MyPoint();

            if (!IsDrawn)
            {
                myPoint.Invalidate();
                return myPoint; // Returns an empty myPoint
            }

            // If the mouse cursor is within the select radius of a point, the point object is returned. Otherwise null
            // is returned.
            cObject3D i_Object = graph3d.FindObjectAt(e.X, e.Y, true);

            // Point not found...
            if (i_Object == null)
            {
                myPoint.Invalidate();
                return myPoint; // Returns an empty myPoint
            }

            // Point found...
            // Copy the coordinates of the found point
            if (TransposeXY)
            {
                myPoint.X_AxisTag = i_Object.Points[0].Y;
                myPoint.Y_AxisTag = i_Object.Points[0].X;
                myPoint.Z = i_Object.Points[0].Z;
            }
            else
            {
                myPoint.X_AxisTag = i_Object.Points[0].X;
                myPoint.Y_AxisTag = i_Object.Points[0].Y;
                myPoint.Z = i_Object.Points[0].Z;
            }

            // Get array indexes from the axis tags including the z value (above) as that is used in the hash code calculation
            if (TransposeXY)
            {
                myPoint.X_Index = Array.FindIndex(y_ValuesFromDgv, x => x == myPoint.Y_AxisTag);
                myPoint.Y_Index = Array.FindIndex(x_ValuesFromDgv, y => y == myPoint.X_AxisTag);
            }
            else
            {
                myPoint.X_Index = Array.FindIndex(x_ValuesFromDgv, x => x == myPoint.X_AxisTag);
                myPoint.Y_Index = Array.FindIndex(y_ValuesFromDgv, y => y == myPoint.Y_AxisTag);
            }

            // Set point found
            myPoint.Found = true;

            return myPoint;
        }

        public void UpdatePlot(double[] x, double[] y, double[,] z)
        {
            if (DebugData)
                Console.WriteLine($"{InstanceName} - {ClassName} - UpdatePlot(x, y, z)");

            if (DebugData_WithPrint)
            {
                // Prints all the recieved data in space delimited format
                PrintGraphValues(x, y, z);
            }

            // Transpose axis tags & z values
            if (TransposeXY)
            {
                x_ValuesToGraph = y;
                y_ValuesToGraph = x;
                z_ValuesToGraph = Transpose_Z(z);
            }
            else
            {
                x_ValuesToGraph = x;
                y_ValuesToGraph = y;
                z_ValuesToGraph = z;
            }

            DrawPlot();
        }

        public void Transpose()
        {
            // Set new plot when the transpose button is clicked
            SetPlotData(x_ValuesFromDgv, y_ValuesFromDgv, z_ValuesFromDgv);
        }

        public double[,] Transpose_Z(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            double[,] transposedArray = new double[cols, rows];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    transposedArray[j, i] = array[i, j];
                }
            }

            return transposedArray;
        }

        public void ResetViewPosition() // Will rotate the graph home when LoadOptions is called from the DrawPlot function
        {
            resetViewPosition = true;
        }

        private void PrintGraphValues(double[] x, double[] y, double[,] z)
        {
            // First space to shift column header over 1 spot
            Console.Write($" ");

            // Print the column header on the same line
            for (int i = 0; i < x.Length; i++)
            {
                Console.Write($"{x[i].ToString()} ");
            }

            // Finish this line
            Console.Write('\r');

            // Each line prints a row header then the table data for that row
            for (int i = 0; i < y.Length; i++)
            {
                Console.Write($"{y[i].ToString()} ");

                for (int j = 0; j < x.Length; j++)
                {
                    Console.Write($"{z[i, j].ToString()} ");
                }

                // Finish this line
                Console.Write('\r');
            }
        }

        private eInvalidate OnSelectEvent(eSelEvent e_Event, Keys e_Modifiers, int s32_DeltaX, int s32_DeltaY, cObject3D i_Object)
        {
            bool b_CTRL = (e_Modifiers & Keys.Control) > 0;

            eInvalidate e_Invalidate = eInvalidate.NoChange;

            if (DebugPointSelectMode || DebugPointMoveMode)
            {
                //Console.WriteLine($"{InstanceName} - {ClassName} - Selection call back active");
                //Console.WriteLine($"b_CTRL {b_CTRL}, e_Event {e_Event}, e_Modifiers {e_Modifiers}");
            }

            // The left mouse button went down with ALT key down and CTRL key up
            if (e_Event == eSelEvent.MouseDown && !b_CTRL && i_Object != null)
            {
                // Toggle point object selected status
                //i_Object.Selected = !i_Object.Selected;

                if (DebugPointSelectMode)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Point select AltSelection event fired");
                }

                // Fire event to the myinterface class. The point selection is handled there
                AltSelection?.Invoke(null, i_Object);

                // After changing the selection status the object must be redrawn.
                e_Invalidate = eInvalidate.Invalidate;
            }
            else if (e_Event == eSelEvent.MouseDrag && b_CTRL)
            {
                // The user is dragging the mouse with ALT + CTRL keys down.
                // Convert the mouse movement in the 2D space into a movement in the 3D space.
                cPoint3D i_Project = graph3d.ReverseProject(s32_DeltaX, s32_DeltaY);

                if (DebugPointMoveMode)
                {
                    //Console.WriteLine($"{InstanceName} - {ClassName} - Point move code section active");
                    //Console.WriteLine($"dX {s32_DeltaX} dY {s32_DeltaY}");
                }

                // New empty points moved list
                PointsMoved = new List<cPoint3D>();

                // The returned array contains only unique points.
                foreach (cPoint3D i_Selected in graph3d.Selection.GetSelectedPoints(eSelType.All))
                {
                    // The points in the Surface grid have a fixed X,Y position, only Z can be modified.
                    i_Selected.Move(0, 0, i_Project.Z);

                    // Add the individual point to the point collection
                    PointsMoved.Add(i_Selected);
                }

                if (DebugPointMoveMode)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - Point move Graph3d_NDR event fired");
                }

                // Useful to inhibit user dgv undo buffer writes whilst points are dragged and stop hover cells being
                // selected. Also signals the dgv to take the point values from our list
                Graph3d_NDR?.Invoke(null, PointsMoved);

                // Set flag to recalculate the coordinate system, then Invalidate()
                e_Invalidate = eInvalidate.CoordSystem;
            }

            return e_Invalidate;
        }
        #endregion
    }
    #endregion

    public class ScrollBarCtrl
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region 
        // Debug
        public string ClassName { get; set; } = "ScrlBrCtrl";
        public string InstanceName { get; private set; } // For debugging, the name of the TableEditor3D class instance

        // Required initial settings
        public bool UseMyScrollBars { get; set; } // If false, hides the scroll bars, blanking plate and the header dgvs
        public bool DebugPosition { get; set; }
        public bool DebugScrollBars { get; set; }
        public bool DebugExternalEvents { get; set; }
        public bool DebugMouseWheel { get; set; }

        //
        public Point _OffCanvasPixelCount { get { return OffCanvasPixelCount(); } }//private set { OffCanvasPixelCount() = value; } }
        public bool HScrollShown { get { return hScroll.Visible; } }
        public bool VScrollShown { get { return vScroll.Visible; } }
        public int HScrollValue { get { return hScroll.Value; } }
        public int VScrollValue { get { return vScroll.Value; } }
        public int VScrollWidth { get { return vScroll.Width; } }
        public int HScrollHeight { get { return hScroll.Height; } }
        public int HScrollPreferredValue { get { return hScrollPreferredValue; } set { hScrollPreferredValue = value; } } // Small increment value to match row dimensions
        public int VScrollPreferredValue { get { return vScrollPreferredValue; } set { vScrollPreferredValue = value; } }
        public bool ScrollBarsReadyForStart { get; private set; }
        public bool Initiated { get; private set; }

        // Private
        private Rectangle cRect { get { return new Rectangle(child.Location, child.Size); } }
        private Point cLoc { get { return child.Location; } set { child.Location = value; } }
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region 
        // Classes
        DgvCtrl dgvCtrl;
        Control parent;
        DataGridView child;
        VScrollBar vScroll;
        HScrollBar hScroll;
        SplitContainer splitContainer;

        // Scroll values
        int hScrollPreferredValue = int.MinValue;
        int vScrollPreferredValue = int.MinValue;
        int vPixels_Top, vPixels_Bottom;

        // Save values to restore on external event load
        Rectangle pRectLast = Rectangle.Empty;
        Rectangle cRectLast = Rectangle.Empty;
        Point offCanvasPixelsLast = Point.Empty;

        // Timer
        public TimerOffDelay scrlBrDgvMvTmr;

        // Events
        //public event EventHandler ResetRowHeaderPosition;
        //public event EventHandler ResetColHeaderPosition;
        //public event EventHandler<(bool, int)> PushNewHeaderPosition;
        public event EventHandler<ScrollEventArgs> hScroll_Scrolled;
        public event EventHandler<ScrollEventArgs> vScroll_Scrolled;
        #endregion

        //------------------------- Constructor ---------------------------------------------------------------------------------------
        #region
        public ScrollBarCtrl(DgvCtrl _dgvCtrl, string instanceName)
        {
            // When creating this class the form, parent and dgv types can be changed to suit the design requirements.
            // Attention is needed if re-mapping of events is required

            // Class members
            this.dgvCtrl = _dgvCtrl;
            this.parent  = dgvCtrl.dgv.Parent; // Enclosing dgv controls container e.g panel, split container etc.
            this.child   = dgvCtrl.dgv;
            this.vScroll = dgvCtrl.ScrollBarCntrls.vScrollBar;
            this.hScroll = dgvCtrl.ScrollBarCntrls.hScrollBar;
            this.splitContainer = dgvCtrl.ScrollBarCntrls.splitContainer;

            // Debug name
            InstanceName = instanceName;

            // Scroll bar dgv move timer
            scrlBrDgvMvTmr = new TimerOffDelay
            {
                Preset = 50,
                OnTimingDone = DgvMove_TimerOff_Tick,
                DebugTimerName = "scrlBrDgvMvTmr",
                Debug = DebugPosition
            };
        }
        #endregion

        //------------------------- Functions -----------------------------------------------------------------------------------------
        #region
        // Call at program start-up
        public void Initiate()
        {
            // Note: This function is designed to be run once at the start of runtime. It does reset properties and
            // events that were previously active if it is called again during runtime with different options set!

            // Debug message
            if (DebugPosition)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Initiate()");
            }

            // Check the dock property for none if using my scroll bars. If docked my scroll bars don't work
            if (UseMyScrollBars && child.Dock != DockStyle.None)
                throw new Exception("Cannot use MyScrollBars with a docked child (dgv), please undock");

            // Same deal with autosize
            if (UseMyScrollBars && child.AutoSize)
                throw new Exception("Cannot use MyScrollBars with (dgv) AutoSize == true");

            // Events
            LoadEvents();

            // Initial update if dgv has data
            if (dgvCtrl.DgvHasData)
                UpdateScrollBarVisibilityAndValues();

            Initiated = true;
        }

        // Call to update
        public void ExternalUpdateReq()
        {
            // This function is called from the table editor (or from this) class when the split container resize event
            // fires. This event is debounced so it should only occur once at the end of the resize. A resize end event
            // is not available

            if (DebugPosition)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - ExternalUpdateReq()");
                //Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                //Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
            }

            // 1. Parent size > child size --> Set location to 0
            // 2. White space at top (left). Child XY value > 0 --> Set location to 0
            // 3. White space at bottom. Child Y value < 0 --> Line up bottom edge
            // 4. White space at right. Child X value < 0 --> Line up right edge

            #region 1: Parent size > child size --> Set header location to 0
            // If the parent size is larger than the child size set the location to 0
            if (pRect().Height >= cRect.Height || pRect().Width >= cRect.Width)
            {
                cRectLast = cRect;
                if (pRect().Height >= cRect.Height)
                {
                    // Sets the new child location
                    cLoc = new Point(cLoc.X, 0);
                }
                if (pRect().Width >= cRect.Width)
                {
                    // Sets the new child location
                    cLoc = new Point(0, cLoc.Y);
                    dgvCtrl.dgvHeaders.ResetColHeaderPosition();
                }
                if (DebugPosition)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - 1");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
                }
                if (!cRect.Equals(cRectLast))
                {
                    //Console.WriteLine($"{InstanceName} - {ClassName} - Child shifted");
                }
                goto CheckWhiteSpace;
            }
            #endregion 

            #region 2: White space at top or left. Child XY value > 0 --> Set header location to 0
            if (cLoc.Y > 0 || cLoc.X > 0)
            {
                cRectLast = cRect;
                if (cLoc.Y > 0)
                {
                    // Sets the new child location
                    cLoc = new Point(cLoc.X, 0);
                    dgvCtrl.dgvHeaders.ResetRowHeaderPosition();
                }
                if (cLoc.X > 0)
                {
                    // Sets the new child location
                    cLoc = new Point(0, cLoc.Y);
                    dgvCtrl.dgvHeaders.ResetColHeaderPosition();
                }
                if (DebugPosition)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - 2");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
                }
                if (!cRect.Equals(cRectLast))
                {
                    //Console.WriteLine($"{InstanceName} - {ClassName} - Child shifted");
                }
                goto CheckWhiteSpace;
            }
        #endregion

        CheckWhiteSpace:
            #region 3: White space at bottom. Child Y value < 0 --> Line up bottom edge
            // Vertical
            int whiteSpaceDistance = 0;
            int shiftAmount = 0;
            if (cLoc.Y != 0)
            {
                cRectLast = cRect;
                // White space distance
                whiteSpaceDistance = pRect().Height - (cRect.Height + cLoc.Y);

                // Positive if white space showing at the bottom, negative is at the top
                if (whiteSpaceDistance > 0)
                {
                    // We need to shift the dgv, the aim is to meet up the bottom edge of the dgv with the bottom of the
                    // window. The maximum shift amount is the lesser of the off screen pixels or the white space amount
                    shiftAmount = Math.Min(-cLoc.Y, whiteSpaceDistance);
                }

                // If the shift amount is not 0, move the dgv by that amount
                if (shiftAmount != 0)
                {
                    // New Y location
                    cLoc = new Point(cLoc.X, cLoc.Y + shiftAmount);

                    // Shifts the row header 
                    dgvCtrl.dgvHeaders.PushNewHeaderLocation(true, shiftAmount);
                }
                if (DebugPosition)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - 3");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
                }
                if (!cRect.Equals(cRectLast))
                {
                    //Console.WriteLine($"{InstanceName} - {ClassName} - Child shifted");
                }
            }
            #endregion

            #region 4: White space at right. Child X value < 0 --> Line up right edge
            // Horizontal
            whiteSpaceDistance = 0;
            shiftAmount = 0;
            if (cLoc.X != 0)
            {
                cRectLast = cRect;
                // White space distance
                whiteSpaceDistance = pRect().Width - (cRect.Width + cLoc.X);

                // Positive if white space showing
                if (whiteSpaceDistance > 0)
                {
                    // We need to shift the dgv, the aim is to meet up the bottom edge of the dgv with the bottom of the
                    // window. The maximum shift amount is the lesser of the off screen pixels or the white space amount
                    shiftAmount = Math.Min(Math.Abs(cLoc.X), whiteSpaceDistance);
                }

                // If the shift amount is not 0, move the dgv by that amount
                if (shiftAmount > 0)
                {
                    // Sets the new child location
                    cLoc = new Point(cLoc.X + shiftAmount, cLoc.Y);

                    // Shifts the column header
                    dgvCtrl.dgvHeaders.PushNewHeaderLocation(false, shiftAmount);
                }
                if (DebugPosition)
                {
                    Console.WriteLine($"{InstanceName} - {ClassName} - 4");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                    //Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
                }
                if (!cRect.Equals(cRectLast))
                {
                    //Console.WriteLine($"{InstanceName} - {ClassName} - Child shifted");
                }
            }
            #endregion

            UpdateScrollBarVisibilityAndValues();
        }

        // Timer tick event
        private void DgvMove_TimerOff_Tick()
        {
            // Debug message
            //if (Debug)
            //{
            //    Console.WriteLine($"{InstanceName} - {ClassName} - {scrlBrDgvMvTmr.DebugName} TimerOff_Tick()");
            //}

            // Run the resize end function if its running
            //if (dgvResizeStartFlag)
            //{
            //    scrlBrDgvMvTmr.Stop();
            //    dgvResizeStartFlag = false;
            //}
        }

        // Events
        private void Dgv_NDR_Debounced(object sender, DgvData e)
        {
            // Debug message
            if (DebugExternalEvents)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_NDR_Debounced()");
            }

            UpdateScrollBarVisibilityAndValues();
        }

        private void Parent_SizeChanged(object sender, EventArgs e)
        {
            ExternalUpdateReq();
        }

        private void Child_Move(object sender, EventArgs e)
        {
            // Debug message. N.B. High speed message
            if (DebugPosition || DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_Move()");
            }
        }

        private void Child_Move_Backup(object sender, EventArgs e)
        {
            // If re-enabling, event load and DgvMove_TimerOff have been commented out

            // When the form moves, a dgv will automatically set the first displayed cell to 0, 0. Fucken cunt of a
            // thing. This code keeps the dgv still by continually restoring the initial location

            // Debug message. N.B. High speed message
            //if (Debug)
            //{
            //    Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_Move()");
            //}

            //if (!holdDgvStillDuringResize)
            //    return;

            //// Captures the dgv location on the first move event
            //if (!dgvResizeStartFlag)
            //{
            //    childLocationPrev = dgv.Location;
            //    dgvResizeStartFlag = true;
            //}

            //// Timer off delay
            //scrlBrDgvMvTmr.Start();

            //// Sets the dgv location back to its initial captured location
            //dgv.Location = childLocationPrev;
        }

        private void Scroll_MouseEnter(object sender, EventArgs e)
        {
            // Debug message
            if (DebugScrollBars)
            {
                //Console.WriteLine($"{InstanceName} - {ClassName} - Scroll_MouseEnter()");
            }

            // Check to see if the scroll bars still need displaying
            //UpdateScrollBars();
        }

        private void Scroll_MouseLeave(object sender, EventArgs e)
        {
            // Debug message
            if (DebugScrollBars)
            {
                //Console.WriteLine($"{InstanceName} - {ClassName} - Scroll_MouseLeave()");
            }

            // Check to see if the scroll bars still need displaying
            //UpdateScrollBars();
        }

        private void hScroll_Scroll(object sender, ScrollEventArgs e)
        {
            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - hScroll_Scroll()");
            }

            // Unload the child move event which would have kept the child at it's original location thereby preventing
            // the ability to scroll
            //child.Move -= Child_Move;

            Point location = child.Location;

            // Move the child control by the same amount the scroll bar value moved. 
            location.X -= e.NewValue - e.OldValue;

            // Move to new location. 
            child.Location = location;

            // Raise the scroll event
            hScroll_Scrolled?.Invoke(this, e);

            // Signal the dgv headers class
            if (dgvCtrl.dgvHeaders != null)
            {
                dgvCtrl.dgvHeaders.ScrollEventArgs = e;
                dgvCtrl.dgvHeaders.hScrollInProgress = true;
            }

            // Reinstate the child move event
            //child.Move += Child_Move;
        }

        private void vScroll_Scroll(object sender, ScrollEventArgs e)
        {
            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - vScroll_Scroll()");
            }

            // Unload the child move event which would have kept the child at it's original location thereby preventing
            // the ability to scroll
            //child.Move -= Child_Move;

            Point location = child.Location;

            // Move the child control by the same amount the scroll bar value moved
            location.Y -= e.NewValue - e.OldValue;

            // Move to new location
            child.Location = location;

            // Raise the scroll event
            vScroll_Scrolled?.Invoke(this, e);

            // Signal the dgv headers class
            if (dgvCtrl.dgvHeaders != null)
            {
                dgvCtrl.dgvHeaders.ScrollEventArgs = e;
                dgvCtrl.dgvHeaders.vScrollInProgress = true;
            }

            // Reinstate the child move event
            //child.Move += Child_Move;
        }

        private void Dgv_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Debug message
            if (DebugMouseWheel)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Dgv_MouseWheel()");
            }

            //// Touchpad scrolling is only supported in the vertical direction. The delta values can be large and are
            //// scaled down by a factor

            //// Check delta for scroll direction. + is scroll up (towards the top)
            //int delta = -e.Delta;

            //// Scale scrolling
            //double deltaScaled = (double)delta / 25;
            //double ratio;
            //double offScreenPixelsY = OffScreenPixelsCount().Y;

            //// Get the current scroll value, convert it to a ratio and populate the ratio variable
            //ratio = (double)vScroll.Value / (vScroll.Maximum - vScroll.LargeChange);

            //// Add the delta scaled ratio to the current ratio
            //if (offScreenPixelsY > 0)
            //    ratio -= deltaScaled / offScreenPixelsY;

            //// Clamp to the maximum allowed ratio of 0-1
            //ratio = Math.Max(ratio, 0);
            //ratio = Math.Min(ratio, 1);

            //// Transfer the ratio to the scroll value
            ////vScroll.Value = (int)((vScroll.Maximum - vScroll.LargeChange) * ratio);

            //// Move the child
            //Point location = child.Location;
            //location.Y = (int)((vScroll.Maximum - vScroll.LargeChange) * ratio);
            //child.Location = location;
        }

        private void Parent_Scroll(object sender, ScrollEventArgs e)
        {
            // Debug message
            if (DebugMouseWheel)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Parent_Scroll()");
            }

            throw new Exception("Ah huh, you did need this!");

            //// Unload the child move event which will keep the child at it's original location
            //dgv.Move -= Child_Move;

            //// Raise the scroll event
            //if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            //    hScroll_Scrolled?.Invoke(this, e);

            //if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            //    vScroll_Scrolled?.Invoke(this, e);

            //// Signal the dgv headers class
            //if (dgvHeaders != null)
            //{
            //    dgvHeaders.ScrollEventArgs = e;

            //    if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            //        dgvHeaders.hScrollInProgress = true;

            //    if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            //        dgvHeaders.vScrollInProgress = true;
            //}

            //// Reinstate the child move event
            //dgv.Move += Child_Move;
        }

        private void Parent_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Debug message
            if (DebugMouseWheel)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - Parent_MouseWheel()");
            }

            // The current position ranges from 0 (home) to more negative values (fully scrolled down).
            // Delta
            // Positive = scrolling up
            // Negative = scrolling down

            //// Unload the child move event which will keep the child at it's original location
            //dgv.Move -= Child_Move;

            //int oldValue = -parent.AutoScrollPosition.Y;
            //int delta = -e.Delta;

            //int endPosition = -(parent.ClientRectangle.Height - parent.DisplayRectangle.Height);

            //int newValue = delta + oldValue;

            //// Clamp the new value to the end position
            //newValue = Math.Min(newValue, endPosition);

            //// Clamp the new value to 0
            //newValue = Math.Max(newValue, 0);

            //ScrollEventArgs scrollEventArgs = new ScrollEventArgs(ScrollEventType.ThumbTrack, oldValue, newValue, ScrollOrientation.VerticalScroll);

            //// Raise the scroll event
            //vScroll_Scrolled?.Invoke(this, scrollEventArgs);

            //// Signal the dgv headers class
            //if (dgvHeaders != null)
            //{
            //    dgvHeaders.ScrollEventArgs = scrollEventArgs;
            //    dgvHeaders.vScrollInProgress = true;
            //}

            //// Reinstate the child move event
            //dgv.Move += Child_Move;
        }

        private void SplitContainer_Resize(object sender, EventArgs e)
        {
            ExternalUpdateReq();
        }

        private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            ExternalUpdateReq();
        }


        // Core functions
        private Rectangle pRect()
        {
            return new Rectangle(parent.Location.X, parent.Location.Y, parent.DisplayRectangle.Width - (vScroll.Visible ? vScroll.Width : 0), parent.DisplayRectangle.Height - (hScroll.Visible ? hScroll.Height : 0));
        }

        private void UpdateScrollBarVisibilityAndValues()
        {
            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - UpdateScrollBars()");
            }

            // Calc the abs value of off canvas pixels
            OffCanvasPixelCount();// = CalcOffCanvasPixelCount();

            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - pRect() {pRect()}");
                Console.WriteLine($"{InstanceName} - {ClassName} - cRect {cRect}");
            }

            SetScrollBarVisibility();

            AssignScrollValues();
        }

        private void SetScrollBarVisibility()
        {
            Point offCanvasPixelCount = OffCanvasPixelCount();

            // Basic implementation where if there are any child pixels outside the parent panel area then the
            // respective scroll bar(s) are shown. 
            if (offCanvasPixelCount.X > 0)
                hScroll.Visible = true;
            else
                hScroll.Visible = false;

            if (offCanvasPixelCount.Y > 0)
                vScroll.Visible = true;
            else
                vScroll.Visible = false;

            // Edge case scenario where both the parent and child controls are located at (0, 0), and without the
            // presence of scroll bars, the parent rectangle would have been larger than the child rectangle.
            if (parent.Location.Equals(child.Location))
            {
                if (pRect().Width >= cRect.Width && pRect().Height >= cRect.Height)
                {
                    hScroll.Visible = false;
                    vScroll.Visible = false;
                }
            }

            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - hScroll.Visible {hScroll.Visible}");
                Console.WriteLine($"{InstanceName} - {ClassName} - vScroll.Visible {vScroll.Visible}");
            }
        }

        private void AssignScrollValues()
        {
            // If child is home, reset scroll bar value to 0
            if (cRect.X == 0)
                hScroll.Value = 0;
            if (cRect.Y == 0)
                vScroll.Value = 0;

            Point offCanvasPixelCount = OffCanvasPixelCount();

            // Horizontal scroll
            if (offCanvasPixelCount.X > 0)
            {
                hScroll.Minimum = 0;
                hScroll.SmallChange = hScrollPreferredValue != int.MinValue ? hScrollPreferredValue : offCanvasPixelCount.X / 10;
                hScroll.LargeChange = offCanvasPixelCount.X; //Math.Min(offScreenPixelsCount.X, offScreenPixelsCount.X / 5);

                // Disable / enable to get rid of flicker on value update
                hScroll.Enabled = false;
                hScroll.Maximum = offCanvasPixelCount.X;
                hScroll.Maximum += hScroll.LargeChange;
                hScroll.Enabled = true;
            }

            // Vertical scroll
            if (offCanvasPixelCount.Y > 0)
            {
                vScroll.Minimum = 0;
                vScroll.SmallChange = vScrollPreferredValue != int.MinValue ? vScrollPreferredValue : offCanvasPixelCount.Y / 10;
                vScroll.LargeChange = offCanvasPixelCount.Y; //Math.Min(offScreenPixelsCount.Y, offScreenPixelsCount.Y / 5);

                // Disable / enable to get rid of flicker on value update
                vScroll.Enabled = false;
                vScroll.Maximum = offCanvasPixelCount.Y;
                vScroll.Maximum += vScroll.LargeChange;
                vScroll.Enabled = true;
            }

            // Debug message
            if (DebugScrollBars)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - hScroll.Value = {hScroll.Value}");
                Console.WriteLine($"{InstanceName} - {ClassName} - vScroll.Value = {vScroll.Value}");
            }
        }

        private Point OffCanvasPixelCount()
        {
            // If offset > 0 the child control is not fully visible.
            // If child panel location is < 0 then there are off screen pixels
            int hPixels = 0, vPixels = 0;

            // Any part of the child rectangle is outside of the parent client area (left)
            if (cRect.Left < pRect().Left)
            {
                hPixels = Math.Abs(cRect.Left);
            }

            // Any part of the child rectangle is outside of the parent client area (top)
            if (cRect.Top < pRect().Top)
            {
                vPixels = Math.Abs(cRect.Top);
                vPixels_Top = Math.Abs(cRect.Top);
            }

            // If rectangle is outside then scroll bar(s) are visible which we take into account
            if (cRect.Right > pRect().Right)
            {
                hPixels += Math.Abs(cRect.Right - pRect().Right);
            }

            if (cRect.Bottom > pRect().Bottom)
            {
                vPixels += Math.Abs(cRect.Bottom - pRect().Bottom);
                vPixels_Bottom = Math.Abs(cRect.Bottom - pRect().Bottom);
            }

            return new Point(hPixels, vPixels);
        }

        private void LoadEvents()
        {
            // Data change event
            dgvCtrl.myEvents.DgvDataChanged_Debounced += Dgv_NDR_Debounced;

            // Controls events
            vScroll.MouseEnter += Scroll_MouseEnter;
            hScroll.MouseEnter += Scroll_MouseEnter;
            vScroll.MouseLeave += Scroll_MouseLeave;
            hScroll.MouseLeave += Scroll_MouseLeave;
            vScroll.Scroll += vScroll_Scroll;
            hScroll.Scroll += hScroll_Scroll;

            // Resize events
            parent.SizeChanged    += Parent_SizeChanged;
            splitContainer.Resize += SplitContainer_Resize;
            splitContainer.SplitterMoved += SplitContainer_SplitterMoved;
        }
        #endregion
    }
    #endregion

    public class DgvHeadersCtrl
    #region
    {
        #region Properties
        public string ClassName { get; set; } = "DgvHdrs";
        public string InstanceName { get; private set; } // For debugging, the name of the TableEditor3D class instance
        public bool DebugHeaders { get; set; }
        public ScrollEventArgs ScrollEventArgs { get; set; }
        public bool hScrollInProgress { set { hScroll_Scrolled(dgv_ColHeader, ScrollEventArgs); } }
        public bool vScrollInProgress { set { vScroll_Scrolled(dgv_RowHeader, ScrollEventArgs); } }
        public int RowHeight { get { return dgvCtrl.dgv.Rows[0].Height + 1; } }
        public int ColWidth { get { return dgvCtrl.dgv.RowHeadersWidth - 1; } }
        #endregion

        #region Variables

        DgvCtrl dgvCtrl;
        public DataGridView dgv_RowHeader;
        public DataGridView dgv_ColHeader;
        Panel blankingPanel;
        HScrollBar hScroll;
        VScrollBar vScroll;
        SplitContainer splitContainer;

        double[] rowHeaderValues_Prev = new double[0];
        double[] colHeaderValues_Prev = new double[0];

        // Foreground / background colour
        Color foreColour = Color.Black;
        Color backColour = Color.Beige;

        // Selection
        List<int> selectedRows = new List<int>();
        List<int> selectedCols = new List<int>();

        // General purpose
        enum Target
        {
            Both,
            Rows,
            Columns
        };

        #endregion

        #region Constructor
        public DgvHeadersCtrl(DgvCtrl _dgvCtrl, string instanceName)
        {
            this.dgvCtrl       = _dgvCtrl;
            this.dgv_RowHeader = dgvCtrl.DgvHeaderCntrls.RowHeader;
            this.dgv_ColHeader = dgvCtrl.DgvHeaderCntrls.ColHeader;
            this.blankingPanel = dgvCtrl.DgvHeaderCntrls.BlankingPanel;
            this.hScroll = dgvCtrl.DgvHeaderCntrls.hScrollBar;
            this.vScroll = dgvCtrl.DgvHeaderCntrls.vScrollBar;
            this.splitContainer = dgvCtrl.DgvHeaderCntrls.splitContainer;
            // Debug name
            InstanceName = instanceName;

            // Hide row / column dgvs and blank panel to start with. When update is run they will be shown
            HideHeaders();

            // Monitor for a change in dgv table values, this will trigger the headers to be re-read
            dgvCtrl.myEvents.DgvSizeChanged_Intermittent += MyEvents_SizeChanged_Intermittent;
            dgvCtrl.myEvents.DgvDataChangedToHeaders     += MyEvents_DgvDataChanged_ToHeaders_Event;
            blankingPanel.Click += BlankingPanel_Click;
            dgvCtrl.dgv.Move    += Dgv_Move;
            dgv_RowHeader.CellClick += RowHeader_CellClick;
            dgv_ColHeader.CellClick += ColHeader_CellClick;
            splitContainer.Resize += SplitContainer_Resize;
        }
        #endregion

        #region Functions

        private void Dgv_Move(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        public void PushNewHeaderLocation(bool vert, int delta)
        {
            if (vert)
            {
                Point loc = dgv_RowHeader.Location;
                loc.Y += delta;
                dgv_RowHeader.Location = loc;
            }
            else
            {
                Point loc = dgv_ColHeader.Location;
                loc.X += delta;
                dgv_ColHeader.Location = loc;
            }
        }

        public void ResetRowHeaderPosition()
        {
            Point loc = dgvCtrl.dgv.Location;
            loc.Y = 0;
            dgvCtrl.dgv.Location = loc;
            dgv_RowHeader.Location = new Point(0, RowHeight); // row
            vScroll.Value = 0;
        }

        public void ResetColHeaderPosition()
        {
            Point loc = dgvCtrl.dgv.Location;
            loc.X = 0;
            dgvCtrl.dgv.Location = loc;
            dgv_ColHeader.Location = new Point(ColWidth, 0); // col
            hScroll.Value = 0;
        }

        private void MyEvents_DgvDataChanged_ToHeaders_Event(object sender, DgvData e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_ScrollBarDataChanged_Event()");
            }

            // Check the event args to see if new row or column headers are present. If yes, load the new headers,
            // reset the header & table positions and set scroll bar values to their origin
            LoadHeaders(e);
        }

        private void MyEvents_SizeChanged_Intermittent(object sender, DgvCtrl.MyEvents.SizeEventArgs e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - MyEvents_SizeChanged_Intermittent()");
            }

            // Hide and return when table is empty
            if (dgvCtrl.dgv.Rows.Count == 1 && dgvCtrl.dgv.Columns.Count == 1)
            {
                HideHeaders();
                return;
            }

            LoadHeaders(e);
        }

        private void RowHeader_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - RowHeader_CellClick()");
            }

            // If the control keys are not down, clear all selections
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                dgvCtrl.dgv.ClearSelection();

            // Is row already selected? If yes, de-select it
            if (selectedRows.Contains(e.RowIndex))
            {
                for (int i = 0; i < dgvCtrl.dgv.Columns.Count; i++)
                {
                    dgvCtrl.dgv.Rows[e.RowIndex].Cells[i].Selected = false;
                }
                selectedRows.Remove(e.RowIndex);
                return;
            }

            // Iterate through each cell in the row and set its selected property to true
            for (int i = 0; i < dgvCtrl.dgv.Columns.Count; i++)
            {
                dgvCtrl.dgv.Rows[e.RowIndex].Cells[i].Selected = true;
            }

            selectedRows.Add(e.RowIndex);
        }

        private void ColHeader_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - ColHeader_CellClick()");
            }

            // If the control keys are not down, clear all selections
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                dgvCtrl.dgv.ClearSelection();

            // Is column already selected? If yes, remove it from the selection collection
            if (selectedCols.Contains(e.ColumnIndex))
            {
                selectedCols.Remove(e.ColumnIndex);
                return;
            }

            // Iterate through each cell in the column and set its selected property to true
            for (int i = 0; i < dgvCtrl.dgv.Rows.Count; i++)
            {
                dgvCtrl.dgv.Rows[i].Cells[e.ColumnIndex].Selected = true;
            }

            selectedCols.Add(e.ColumnIndex);
        }

        private void BlankingPanel_Click(object sender, EventArgs e)
        {
            // Flip - flop table selection / deselection
            if (dgvCtrl.dgv.AreAllCellsSelected(true))
                dgvCtrl.dgv.ClearSelection();
            else
                dgvCtrl.dgv.SelectAll();
        }

        private void HideHeaders()
        {
            dgv_RowHeader.Hide();
            dgv_ColHeader.Hide();
            blankingPanel.Hide();

            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - HideHeaders()");
            }
        }

        private void UnhideHeaders()
        {
            dgv_RowHeader.Show();
            dgv_ColHeader.Show();
            blankingPanel.Show();

            dgv_RowHeader.BringToFront();
            dgv_ColHeader.BringToFront();
            hScroll.BringToFront();
            vScroll.BringToFront();
            blankingPanel.BringToFront();

            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - UnhideHeaders()");
            }
        }

        private void LoadHeaders(DgvData e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - LoadNewHeaders(PasteEventArgs)");
            }

            // Null checks
            if (e.RowHeaders == null || e.ColHeaders == null || e.RowHeadersText == null || e.ColHeadersText == null)
            {
                if (DebugHeaders)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Returned from LoadHeaders(). Null checks failed");

                return;
            }

            // Sets all objects back to their original locations
            ResetRowHeaderPosition();
            ResetColHeaderPosition();

            // Writes the headers
            WriteScrollBarRowHeaders(e.RowHeadersText);
            WriteScrollBarColHeaders(e.ColHeadersText);

            // Style format
            DgvStyleOverrides(dgv_RowHeader, Target.Rows);
            DgvStyleOverrides(dgv_ColHeader, Target.Columns);
            PanelStyleOverrides(blankingPanel);

            // Show the headers
            UnhideHeaders();
        }

        private void LoadHeaders(DgvCtrl.MyEvents.SizeEventArgs e)
        {
            // Debug message
            if (DebugHeaders)
            {
                Console.WriteLine($"{InstanceName} - {ClassName} - LoadNewHeaders(MyEvent SizeEventArgs)");
            }

            // Sets all objects back to their original locations
            ResetRowHeaderPosition();
            ResetColHeaderPosition();

            // Style format
            DgvStyleOverrides(dgv_RowHeader, Target.Rows);
            DgvStyleOverrides(dgv_ColHeader, Target.Columns);
            PanelStyleOverrides(blankingPanel);

            // Show the headers
            UnhideHeaders();
        }

        private void hScroll_Scrolled(DataGridView hdrDgv, ScrollEventArgs e)
        {
            Point location = hdrDgv.Location;

            location.X -= e.NewValue - e.OldValue;

            hdrDgv.Location = location;
        }

        private void vScroll_Scrolled(DataGridView hdrDgv, ScrollEventArgs e)
        {
            Point location = hdrDgv.Location;

            location.Y -= e.NewValue - e.OldValue;

            hdrDgv.Location = location;
        }

        private void SplitContainer_Resize(object sender, EventArgs e)
        {
            dgv_RowHeader.Location = new Point(0, dgvCtrl.dgv.Rows[0].Height + 1 + dgvCtrl.dgv.Location.Y);
            dgv_ColHeader.Location = new Point(dgvCtrl.dgv.RowHeadersWidth - 1 + dgvCtrl.dgv.Location.X, 0);
        }

        private void DgvStyleOverrides(DataGridView hdrDgv, Target target)
        {
            // These settings override the designer settings. They are changes from the default settings that are common
            // across all the data grid views in this project
            //
            // This is a pretty crazy list but it seems to work, it was based on the dgv style override function I
            // made. Where appropriate settings are inherited from the main dgv

            // Properties I need from the main dgv:
            // Font
            // Rows[0].Height
            // RowHeadersWidth;
            // Columns[0].Width
            // GridColor
            // BorderStyle

            // Exit if blank dgv
            if (hdrDgv.Rows.Count == 0 || hdrDgv.Columns.Count == 0)
                return;

            // Rows                 
            foreach (DataGridViewRow row in hdrDgv.Rows)
            {
                row.Height = dgvCtrl.dgv.Rows[0].Height;
            }

            // Cells                 
            hdrDgv.DefaultCellStyle.Font = dgvCtrl.dgv.DefaultCellStyle.Font;
            hdrDgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            foreach (DataGridViewRow row in hdrDgv.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Style.ForeColor = foreColour;
                    cell.Style.BackColor = backColour;
                    cell.Style.SelectionForeColor = foreColour;
                    cell.Style.SelectionBackColor = backColour;
                }
            }

            // Control                 
            hdrDgv.BackgroundColor = backColour;
            hdrDgv.GridColor = dgvCtrl.dgv.GridColor;
            hdrDgv.BorderStyle = dgvCtrl.dgv.BorderStyle;
            hdrDgv.ScrollBars = ScrollBars.None;
            hdrDgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            hdrDgv.ColumnHeadersVisible = false;
            hdrDgv.RowHeadersVisible = false;

            // Behaviour                 
            hdrDgv.AllowUserToAddRows = false;
            hdrDgv.AllowUserToDeleteRows = false;
            hdrDgv.AllowUserToOrderColumns = false;
            hdrDgv.AllowUserToResizeColumns = false;
            hdrDgv.AllowUserToResizeRows = false;
            hdrDgv.ReadOnly = true;

            // Detect row or column orientation to set respective dimensions and number format
            // Rows
            if (target == Target.Rows || target == Target.Both)
            {
                hdrDgv.Columns[0].Width = dgvCtrl.dgv.RowHeadersWidth;
                hdrDgv.Width = dgvCtrl.dgv.RowHeadersWidth + 1;
                hdrDgv.Height = dgvCtrl.dgv.Height - dgvCtrl.dgv.Rows[0].Height - 1;
                hdrDgv.Location = new Point(0, dgvCtrl.dgv.Rows[0].Height + 1);

                //dgv.DefaultCellStyle.Format = rowHeaderFormat;
                hdrDgv.DefaultCellStyle.Format = dgvCtrl.dgv.RowHeadersDefaultCellStyle.Format;
            }

            // Columns
            if (target == Target.Columns || target == Target.Both)
            {
                hdrDgv.Width = dgvCtrl.dgv.Width - dgvCtrl.dgv.RowHeadersWidth;
                hdrDgv.Height = dgvCtrl.dgv.Rows[0].Height + 3;
                hdrDgv.Location = new Point(dgvCtrl.dgv.RowHeadersWidth - 1, 0);

                //dgv.DefaultCellStyle.Format = colHeaderFormat;
                hdrDgv.DefaultCellStyle.Format = dgvCtrl.dgv.ColumnHeadersDefaultCellStyle.Format;

                // Cell width
                foreach (DataGridViewColumn column in hdrDgv.Columns)
                    column.Width = dgvCtrl.dgv.Columns[0].Width;
            }

            //
            hdrDgv.ClearSelection();
        }

        private void PanelStyleOverrides(Panel blankingPlate)
        {
            // Blank plate setup
            blankingPlate.Location = new Point(0, 0);
            blankingPlate.Size = new Size(dgvCtrl.dgv.RowHeadersWidth + 1, dgvCtrl.dgv.ColumnHeadersHeight + 1);
            blankingPlate.BackColor = backColour;
            blankingPlate.BorderStyle = BorderStyle.FixedSingle;
        }

        public void WriteScrollBarRowHeaders<T>(T[] values)
        {
            if (values.Length == 0)
                return;

            // Set AutoSizeColumnsMode to None to avoid FillWeight issue
            dgv_RowHeader.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Null check, if null values attempt to read the datatable to get a length
            if (values == null || values.Length == 0)
            {
                if (dgvCtrl.dgv.Rows.Count == 0)
                    throw new Exception("You fucked this up!");

                // Redim the values to the dt length
                values = new T[dgvCtrl.dgv.Rows.Count];

                // Add dummy values
                for (int i = 0; i < values.Length; i++)
                    values[i] = (T)Convert.ChangeType(i, typeof(T));
            }

            // Remove all existing columns and rows
            dgv_RowHeader.Columns.Clear();
            dgv_RowHeader.Rows.Clear();

            // Create a new DataGridViewColumn
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.HeaderText = "Values";

            // Add the column to the DataGridView
            dgv_RowHeader.Columns.Add(column);

            // Add rows to the DataGridView
            for (int i = 0; i < values.Length; i++)
            {
                dgv_RowHeader.Rows.Add(values[i]);
                dgv_RowHeader.Rows[i].Cells[0].Value = values[i];
            }
        }

        public void WriteScrollBarColHeaders<T>(T[] values)
        {
            if (values.Length == 0)
                return;

            // Set AutoSizeColumnsMode to None to avoid FillWeight issue
            dgv_ColHeader.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Null check, if null values attempt to read the datatable to get a length
            if (values == null)
            {
                if (dgvCtrl.dgv.Columns.Count == 0)
                    throw new Exception("You fucked this up!");

                // Redim the values to the dt length
                values = new T[dgvCtrl.dgv.Columns.Count];

                // Add dummy values
                for (int i = 0; i < values.Length; i++)
                    values[i] = (T)Convert.ChangeType(i, typeof(T));
            }

            // Remove all existing columns and rows
            dgv_ColHeader.Columns.Clear();
            dgv_ColHeader.Rows.Clear();

            // Remove all existing rows, there should only ever be 1
            //while (dgv_ColHeader.Rows.Count > 0)
            //    dgv_ColHeader.Rows.RemoveAt(dgv_ColHeader.Rows.Count - 1);

            // Add columns based on the length of the double array
            for (int i = 0; i < values.Length; i++)
            {
                dgv_ColHeader.Columns.Add($"Column{i + 1}", $"Column{i + 1}"); // Column name, Header text
            }

            // Add a new row
            dgv_ColHeader.Rows.Add();

            // Populate the row with the values from the array. These are our visible column headers 
            for (int i = 0; i < values.Length; i++)
            {
                dgv_ColHeader.Rows[0].Cells[i].Value = values[i];
            }
        }

        public string[] ReadRowHeaders()
        {
            if (DebugHeaders)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadRowHeaders()");

            int i = 0;

            if (!dgvCtrl.DgvHasData) return null;

            string[] rowLabels = new string[dgv_RowHeader.RowCount];

            // Return entire row
            for (i = 0; i < dgv_RowHeader.RowCount; i++)
            {
                rowLabels[i] = (string)dgv_RowHeader.Rows[i].Cells[0].Value;
            }

            return rowLabels;
        }

        public string[] ReadColHeaders()
        {
            if (DebugHeaders)
                Console.WriteLine($"{InstanceName} - {ClassName} - ReadColHeaders()");

            int i = 0;

            if (!dgvCtrl.DgvHasData) return null;

            string[] colLabels = new string[dgv_ColHeader.ColumnCount];

            // Return entire row
            for (i = 0; i < dgv_ColHeader.ColumnCount; i++)
            {
                colLabels[i] = (string)dgv_ColHeader.Rows[0].Cells[i].Value;
            }

            return colLabels;
        }
    }
    #endregion

    #endregion

    public class DgvData
    #region
    {
        // General purpose data class that covers off everything needed to communicate to the main and headers dgv's
        
        public string[] RowHeadersText { get; set; }
        public string[] ColHeadersText { get; set; }
        public double[] RowHeaders { get; set; }
        public double[] ColHeaders { get; set; }
        public double[,] TableData { get; set; }
        public string RowHeaderFormat { get; set; }
        public string ColHeaderFormat { get; set; }
        public string TableDataFormat { get; set; }
        public DgvData Empty { get { return new DgvData(); } }

        public DgvData() // Empty class if needed
        { }

        public DgvData(DgvData dgvData)
        {
            this.RowHeadersText = dgvData.RowHeadersText;
            this.ColHeadersText = dgvData.ColHeadersText;
            this.RowHeaders = dgvData.RowHeaders;
            this.ColHeaders = dgvData.ColHeaders;
            this.TableData = dgvData.TableData;
            this.RowHeaderFormat = dgvData.RowHeaderFormat;
            this.ColHeaderFormat = dgvData.ColHeaderFormat;
            this.TableDataFormat = dgvData.TableDataFormat;
        }

        public bool Equals(DgvData dgvData)
        {
            // Returns true if equal

            // Row and column length comparison
            if (RowHeaders.Length != dgvData.RowHeaders.Length || ColHeaders.Length != dgvData.ColHeaders.Length)
                return false;

            // Comparison on the row header values
            if (!RowHeaders.SequenceEqual(dgvData.RowHeaders))
                return false;

            // Comparison on the column header values
            if (!ColHeaders.SequenceEqual(dgvData.ColHeaders))
                return false;

            // Table data comparison
            if (TableData.Length != dgvData.TableData.Length)
                return false;

            for (int i = 0; i < TableData.GetLength(0); i++)
                for (int j = 0; j < TableData.GetLength(1); j++)
                    if (TableData[i, j] != dgvData.TableData[i, j])
                        return false;

            // Dt comparison
            //if (Dt != null && dgvData.Dt != null)
            //{
            //    if (Dt.Rows.Count != dgvData.Dt.Rows.Count)
            //        return false;

            //    if (Dt.Columns.Count != dgvData.Dt.Columns.Count)
            //        return false;

            //    if (!Utils.AreDataTablesEqual(Dt, dgvData.Dt))
            //        return false;
            //}

            // We're equal
            return true;
        }

        public bool HeadersEqual(DgvData dgvData)
        {
            // Returns true if equal

            if (dgvData == null)
                return false;

            // Row and column length comparison
            if (RowHeaders.Length != dgvData.RowHeaders.Length || ColHeaders.Length != dgvData.ColHeaders.Length)
                return false;

            // Comparison on the row header values
            if (!RowHeaders.SequenceEqual(dgvData.RowHeaders))
                return false;

            // Comparison on the column header values
            if (!ColHeaders.SequenceEqual(dgvData.ColHeaders))
                return false;

            return true;
        }

        public DgvData Copy()
        {
            DgvData dataOut = new DgvData();

            // Using clone and ImportRow to create clean copies

            // Copy row header text
            if (RowHeadersText != null)
                dataOut.RowHeadersText = (string[])RowHeadersText.Clone();

            // Copy column header text
            if (ColHeadersText != null) 
                dataOut.ColHeadersText = (string[])ColHeadersText.Clone();

            // Copy row headers
            if (RowHeaders != null)
                dataOut.RowHeaders = (double[])RowHeaders.Clone();

            // Copy column headers
            if (ColHeaders != null)
                dataOut.ColHeaders = (double[])ColHeaders.Clone();

            // Copy table data
            if (TableData != null)
                dataOut.TableData = (double[,])TableData.Clone();

            // Copy schema and data values
            //if (Dt != null)
            //{
            //    dataOut.Dt = Dt.Clone();

            //    foreach (DataRow row in Dt.Rows)
            //        dataOut.Dt.ImportRow(row); // Imports rows from the data table
            //}

            // Copy header format
            dataOut.RowHeaderFormat = RowHeaderFormat;
            dataOut.ColHeaderFormat = ColHeaderFormat;
            dataOut.TableDataFormat = TableDataFormat;

            return dataOut;
        }

        public string[] FormatHeaderText(string[] s, string format)
        {
            if (s == null)
                return new string[0];

            for (int i = 0; i < s.Length; i++)
            {
                if (double.TryParse(s[i], out double number))
                {
                    s[i] = number.ToString(format);
                }
            }

            return s;
        }

        public string[] FormatHeaderText(double[] d, string format)
        {
            if (d == null)
                return new string[0];

            string[] s = new string[d.Length];

            for (int i = 0; i < d.Length; i++)
            {
                s[i] = d[i].ToString(format);
            }

            return s;
        }

        public void FormatHeaderText()
        {
            RowHeadersText = FormatHeaderText(RowHeadersText, RowHeaderFormat);
            ColHeadersText = FormatHeaderText(ColHeadersText, ColHeaderFormat);
        }

        public static string[] ConvertNumericHeadersToText(double[] d)
        {
            string[] s = new string[d.Length];

            for (int i = 0; i < d.GetLength(0); i++)
                s[i] = d[i].ToString();

            return s;
        }
    }
    #endregion

    public static class Utils
    #region
    {
        public static void xyz()
        { }

        public static bool IsNumber(string input)
        {
            // Use regular expression to check if the input consists of digits
            return Regex.IsMatch(input, @"^[-+]?\d*\.?\d+$");
        }

        public static bool AreDataTablesEqual(DataTable dt1, DataTable dt2)
        {
            if (dt1.Rows.Count != dt2.Rows.Count || dt1.Columns.Count != dt2.Columns.Count)
            {
                return false;
            }

            // Check each cell in both DataTables
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                for (int j = 0; j < dt1.Columns.Count; j++)
                {
                    if (!Equals(dt1.Rows[i][j], dt2.Rows[i][j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static DataTable CopyDataTable(DataTable dt_In)
        {
            // Source data dimensions must match dt dimensions!
            DataTable dt_Out = new DataTable();

            // Copy schema and data values
            if (dt_In != null)
            {
                dt_Out = dt_In.Clone();

                foreach (DataRow row in dt_In.Rows)
                    dt_Out.ImportRow(row); // Imports rows from the data table
            }
            else
            {
                return null;
            }

            return dt_Out;
        }

        public static double[,] Transpose(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            double[,] transposedArray = new double[cols, rows];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    transposedArray[j, i] = array[i, j];
                }
            }

            return transposedArray;
        }

        public static double[,] CopyDataTableToArray(DataTable dt)
        {
            if (dt == null)
                return new double[0, 0];

            Double[,] dt_Copy = new double[dt.Rows.Count, dt.Columns.Count];

            // Store the data values
            for (int i = 0; i < dt.Rows.Count; i++)
                for (int j = 0; j < dt.Columns.Count; j++)
                    dt_Copy[i, j] = (double)dt.Rows[i][j];

            return dt_Copy;
        }

        public static bool CompareDataTableToArray(double[,] dt1, double[,] dt2)
        {
            if (dt1.GetLength(0) != dt2.GetLength(0) || dt1.GetLength(1) != dt2.GetLength(1))
                return false;

            // Check each row and column
            for (int i = 0; i < dt1.GetLength(0); i++)
            {
                for (int j = 0; j < dt1.GetLength(1); j++)
                {
                    try
                    {
                        if (dt1[i, j] != dt2[i, j])
                            return false;
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }

            return true; // Are equal
        }

        public static bool CompareDataTableToArray(DataTable dt1, double[,] dt2)
        {
            if (dt1.Rows.Count != dt2.GetLength(0) || dt1.Columns.Count != dt2.GetLength(1))
                return false;

            // Check each row and column
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                for (int j = 0; j < dt1.Columns.Count; j++)
                {
                    if ((double)dt1.Rows[i][j] != dt2[i, j])
                        return false;
                }
            }

            return true; // Are equal
        }

        public static string FormatDouble(double d)
        {
            // Get data
            int minDP = int.MaxValue;
            int maxDP = int.MinValue;
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            string numberFormat = "N0";
            string[] parts;

            // Min max decimal places
            if (d == double.NaN)
                return string.Empty;

            double value = d;

            // Find the min / max number of decimal places
            int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];

            minDP = Math.Min(minDP, decimalPlaces);
            maxDP = Math.Max(maxDP, decimalPlaces);

            // Find the largest & smallest value in the data set. Exclude 0
            if (value != 0)
            {
                minValue = Math.Min(minValue, Math.Abs(value));
                maxValue = Math.Max(maxValue, Math.Abs(value));
            }

            // Determine best fit number format
            if (minValue > 100 || (maxDP == 0 && minDP == 0))
            {
                numberFormat = "N0";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 2) // 10.00 => 99.99  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 1) // 10.0 => 99.9  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 0.01) // 0.01 => 9.99 or 0.1 => 9.9 or 1 => 9
            {
                if (Math.Abs(maxValue) < 1 && maxDP >= 3)
                {
                    numberFormat = "N3";
                    goto End;
                }
                else if (maxDP >= 2)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP >= 1)
                {
                    numberFormat = "N1";
                    goto End;
                }
                else
                {
                    numberFormat = "N0";
                    goto End;
                }
            }
            else if (minValue < 0.01) // 0.000001 => 0.009
            {
                if (maxValue > 1)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP <= 3)
                {
                    numberFormat = "N" + maxDP.ToString();
                    goto End;
                }

                int maxLeadingZeros = 0;

                // Finds min and max number of leading 0's after the decimal point
                if (d != 0) // this stops 20 max leading 0's being reported
                {
                    string stringValue = d.ToString("F20");
                    // Split the string using decimal point as separator and count leading zeros in the fractional part
                    parts = stringValue.Split('.');
                    double d1 = 0;
                    if (parts.Length >= 2)
                    {
                        d1 = Convert.ToDouble(parts[0]);
                    }
                    if (d1 < 1)
                    {
                        int leadingZeros = parts[1].TakeWhile(c => c == '0').Count();
                        maxLeadingZeros = Math.Max(maxLeadingZeros, leadingZeros);
                    }
                }
                // Limit floating point values with crazy amounts of decimal places to leading zeros
                // + 3 more. Else just display enough so that trailing 0's are avoided on the smallest value
                if ((maxLeadingZeros + 3) < maxDP)
                {
                    maxLeadingZeros += 3;
                }
                else
                {
                    maxLeadingZeros = maxDP;
                }

                numberFormat = "N" + maxLeadingZeros.ToString();
                goto End;
            }

        End:
            // Final check. Sometimes the formatted value has the last decimal place all 0's. We loop through here and
            // if it is the case we will go to the next format and check again

            // Split number format to get the integer part
            parts = numberFormat.Split('N');

            // Gets the integer portion of the number format
            int numberFormatIntegerPart = int.Parse(parts[1]);

            bool result = true;

            // Can only potentially improve if format is "N1" or higher...
            if (numberFormatIntegerPart >= 0)
            {
                while (result)
                {
                    // Check the last digit of each element. If all 0 the number format decrements and the while
                    // loop will run through this again
                        string s = d.ToString($"N{numberFormatIntegerPart}");

                        // Reset the flag if last digit is not 0
                        if (!s.EndsWith("0"))
                        {
                            result = false;
                            break;
                        }

                        // When at the end and the flag is still true we can decrement the number format, yah!
                        if (result == true && numberFormatIntegerPart > 0)
                            numberFormatIntegerPart--;

                    // Exit if the result went false, no improvement
                    if (!result)
                        break;

                    // Exit if we cant decrement any further. "N0" is as good as it gets
                    if (numberFormatIntegerPart == 0)
                    {
                        break;
                    }
                }

                // Rebuild the number format. If nothing changed it will still be the same
                numberFormat = $"N{numberFormatIntegerPart}";
            }

            return numberFormat;
        }

        public static string FormatDouble(double[] d)
        {
            if (d == null)
                return String.Empty;

            // Get data
            int length = d.Length;
            int minDP = int.MaxValue;
            int maxDP = int.MinValue;
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            string numberFormat = "N0";
            string[] parts;

            // Min max decimal places
            for (int i = 0; i < length; i++)
            {
                if (d[i] == double.NaN)
                    break;

                double value = d[i];

                // Find the min / max number of decimal places
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];

                minDP = Math.Min(minDP, decimalPlaces);
                maxDP = Math.Max(maxDP, decimalPlaces);

                // Find the largest & smallest value in the data set. Exclude 0
                if (value != 0)
                {
                    minValue = Math.Min(minValue, Math.Abs(value));
                    maxValue = Math.Max(maxValue, Math.Abs(value));
                }
            }

            // Determine best fit number format
            if (minValue > 100 || (maxDP == 0 && minDP == 0))
            {
                numberFormat = "N0";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 2) // 10.00 => 99.99  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 1) // 10.0 => 99.9  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 0.01) // 0.01 => 9.99 or 0.1 => 9.9 or 1 => 9
            {
                if (Math.Abs(maxValue) < 1 && maxDP >= 3)
                {
                    numberFormat = "N3";
                    goto End;
                }
                else if (maxDP >= 2)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP >= 1)
                {
                    numberFormat = "N1";
                    goto End;
                }
                else
                {
                    numberFormat = "N0";
                    goto End;
                }
            }
            else if (minValue < 0.01) // 0.000001 => 0.009
            {
                if (maxValue > 1)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP <= 3)
                {
                    numberFormat = "N" + maxDP.ToString();
                    goto End;
                }

                int maxLeadingZeros = 0;

                // Finds min and max number of leading 0's after the decimal point
                for (int i = 0; i < length; i++)
                {
                    if (d[i] != 0) // this stops 20 max leading 0's being reported
                    {
                        string stringValue = d[i].ToString("F20");
                        // Split the string using decimal point as separator and count leading zeros in the fractional part
                        parts = stringValue.Split('.');
                        double d1 = 0;
                        if (parts.Length >= 2)
                        {
                            d1 = Convert.ToDouble(parts[0]);
                        }
                        if (d1 < 1)
                        {
                            int leadingZeros = parts[1].TakeWhile(c => c == '0').Count();
                            maxLeadingZeros = Math.Max(maxLeadingZeros, leadingZeros);
                        }
                    }
                }
                // Limit floating point values with crazy amounts of decimal places to leading zeros
                // + 3 more. Else just display enough so that trailing 0's are avoided on the smallest value
                if ((maxLeadingZeros + 3) < maxDP)
                {
                    maxLeadingZeros += 3;
                }
                else
                {
                    maxLeadingZeros = maxDP;
                }

                numberFormat = "N" + maxLeadingZeros.ToString();
                goto End;
            }

        End:
            // Final check. Sometimes the formatted value has the last decimal place all 0's. We loop through here and
            // if it is the case we will go to the next format and check again

            // Split number format to get the integer part
            parts = numberFormat.Split('N');

            // Gets the integer portion of the number format
            int numberFormatIntegerPart = int.Parse(parts[1]);

            bool result = true;

            // Can only potentially improve if format is "N1" or higher...
            if (numberFormatIntegerPart >= 0)
            {
                while (result)
                {
                    // Check the last digit of each array element. If all 0 the number format decrements and the while
                    // loop will run through this again
                    for (int i = 0; i < length; i++)
                    {
                        string s = d[i].ToString($"N{numberFormatIntegerPart}");

                        // Reset the flag if last digit is not 0
                        if (!s.EndsWith("0"))
                        {
                            result = false;
                            break;
                        }

                        // When at the end and the flag is still true we can decrement the number format, yah!
                        if (i == length - 1 && result == true && numberFormatIntegerPart > 0)
                            numberFormatIntegerPart--;
                    }

                    // Exit if the result went false, no improvement
                    if (!result)
                        break;

                    // Exit if we cant decrement any further. "N0" is as good as it gets
                    if (numberFormatIntegerPart == 0)
                    {
                        break;
                    }
                }

                // Rebuild the number format. If nothing changed it will still be the same
                numberFormat = $"N{numberFormatIntegerPart}";
            }

            return numberFormat;
        }

        public static string FormatDouble(double[,] d)
        {
            if (d == null)
                return String.Empty;

            // Get data
            int rows = d.GetLength(0);
            int columns = d.GetLength(1);
            int minDP = int.MaxValue;
            int maxDP = int.MinValue;
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            string numberFormat = "N0";
            string[] parts;

            // Min max decimal places
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (d[i, j] == double.NaN)
                        break;

                    double value = d[i, j];

                    // Find the min / max number of decimal places
                    int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];

                    minDP = Math.Min(minDP, decimalPlaces);
                    maxDP = Math.Max(maxDP, decimalPlaces);

                    // Find the largest & smallest value in the data set. Exclude 0
                    if (value != 0)
                    {
                        minValue = Math.Min(minValue, Math.Abs(value));
                        maxValue = Math.Max(maxValue, Math.Abs(value));
                    }
                }
            }

            // Determine best fit number format
            if (minValue > 100 || (maxDP == 0 && minDP == 0))
            {
                numberFormat = "N0";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 2) // 10.00 => 99.99  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 1) // 10.0 => 99.9  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 0.01) // 0.01 => 9.99 or 0.1 => 9.9 or 1 => 9
            {
                if (Math.Abs(maxValue) < 1 && maxDP >= 3)
                {
                    numberFormat = "N3";
                    goto End;
                }
                else if (maxDP >= 2)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP >= 1)
                {
                    numberFormat = "N1";
                    goto End;
                }
                else
                {
                    numberFormat = "N0";
                    goto End;
                }
            }
            else if (minValue < 0.01) // 0.000001 => 0.009
            {
                if (maxValue > 1)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP <= 3)
                {
                    numberFormat = "N" + maxDP.ToString();
                    goto End;
                }

                int maxLeadingZeros = 0;

                // Finds min and max number of leading 0's after the decimal point
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        if (d[i, j] != 0) // this stops 20 max leading 0's being reported
                        {
                            string stringValue = d[i, j].ToString("F20");
                            // Split the string using decimal point as separator and count leading zeros in the fractional part
                            parts = stringValue.Split('.');
                            double d1 = 0;
                            if (parts.Length >= 2)
                            {
                                d1 = Convert.ToDouble(parts[0]);
                            }
                            if (d1 < 1)
                            {
                                int leadingZeros = parts[1].TakeWhile(c => c == '0').Count();
                                maxLeadingZeros = Math.Max(maxLeadingZeros, leadingZeros);
                            }
                        }
                    }
                }
                // Limit floating point values with crazy amounts of decimal places to leading zeros
                // + 3 more. Else just display enough so that trailing 0's are avoided on the smallest value
                if ((maxLeadingZeros + 3) < maxDP)
                {
                    maxLeadingZeros += 3;
                }
                else
                {
                    maxLeadingZeros = maxDP;
                }

                numberFormat = "N" + maxLeadingZeros.ToString();
                goto End;
            }

        End:
            // Final check. Sometimes the formatted value has the last decimal place all 0's. We loop through here and
            // if it is the case we will go to the next format and check again

            // Split number format to get the integer part
            parts = numberFormat.Split('N');

            // Gets the integer portion of the number format
            int numberFormatIntegerPart = int.Parse(parts[1]);

            bool result = true;

            // Can only potentially improve if format is "N1" or higher...
            if (numberFormatIntegerPart >= 0)
            {
                while (result)
                {
                    // Check the last digit of each array element. If all 0 the number format decrements and the while
                    // loop will run through this again
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            string s = d[i, j].ToString($"N{numberFormatIntegerPart}");

                            // Reset the flag if last digit is not 0
                            if (!s.EndsWith("0"))
                            {
                                result = false;
                                break;
                            }
                        }

                        // Exit if the result went false, no improvement
                        if (!result)
                            break;

                        // When at the end and the flag is still true we can decrement the number format, yah!
                        if (i == rows - 1 && result == true && numberFormatIntegerPart > 0)
                            numberFormatIntegerPart--;
                    }

                    // Exit if the result went false, no improvement
                    if (!result)
                        break;

                    // Exit if we cant decrement any further. "N0" is as good as it gets
                    if (numberFormatIntegerPart == 0)
                    {
                        break;
                    }
                }

                // Rebuild the number format. If nothing changed it will still be the same
                numberFormat = $"N{numberFormatIntegerPart}";
            }

            return numberFormat;
        }

        public static string FormatDouble(DataTable dt)
        {
            if (dt == null)
                return String.Empty;

            // Get data
            int rows = dt.Rows.Count;
            int columns = dt.Columns.Count;
            int minDP = int.MaxValue;
            int maxDP = int.MinValue;
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            string numberFormat = "N0";
            string[] parts;

            // Min max decimal places
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (dt.Rows[i][j] == DBNull.Value)
                        continue; // skip this entry

                    double value = Convert.ToDouble(dt.Rows[i][j]);

                    if (value.Equals(double.NaN))
                        continue; // skip this entry

                    // Find the min / max number of decimal places
                    int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];

                    minDP = Math.Min(minDP, decimalPlaces);
                    maxDP = Math.Max(maxDP, decimalPlaces);

                    // Find the largest & smallest value in the data set. Exclude 0
                    if (value != 0)
                    {
                        minValue = Math.Min(minValue, Math.Abs(value));
                        maxValue = Math.Max(maxValue, Math.Abs(value));
                    }
                }
            }

            // Determine best fit number format
            if (minValue > 100 || (maxDP == 0 && minDP == 0))
            {
                numberFormat = "N0";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 2) // 10.00 => 99.99  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 10 && maxDP >= 1) // 10.0 => 99.9  
            {
                numberFormat = "N1";
                goto End;
            }
            else if (minValue >= 0.01) // 0.01 => 9.99 or 0.1 => 9.9 or 1 => 9
            {
                if (Math.Abs(maxValue) < 1 && maxDP >= 3)
                {
                    numberFormat = "N3";
                    goto End;
                }
                else if (maxDP >= 2)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP >= 1)
                {
                    numberFormat = "N1";
                    goto End;
                }
                else
                {
                    numberFormat = "N0";
                    goto End;
                }
            }
            else if (minValue < 0.01) // 0.000001 => 0.009
            {
                if (maxValue > 1)
                {
                    numberFormat = "N2";
                    goto End;
                }
                else if (maxDP <= 3)
                {
                    numberFormat = "N" + maxDP.ToString();
                    goto End;
                }

                int maxLeadingZeros = 0;

                // Finds min and max number of leading 0's after the decimal point
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        if (Convert.ToDouble(dt.Rows[i][j]) != 0) // this stops 20 max leading 0's being reported
                        {
                            string stringValue = Convert.ToDouble(dt.Rows[i][j]).ToString("F20");
                            // Split the string using decimal point as separator and count leading zeros in the fractional part
                            parts = stringValue.Split('.');
                            double d = 0;
                            if (parts.Length >= 2)
                            {
                                d = Convert.ToDouble(parts[0]);
                            }
                            if (d < 1)
                            {
                                int leadingZeros = parts[1].TakeWhile(c => c == '0').Count();
                                maxLeadingZeros = Math.Max(maxLeadingZeros, leadingZeros);
                            }
                        }
                    }
                }
                // Limit floating point values with crazy amounts of decimal places to leading zeros
                // + 3 more. Else just display enough so that trailing 0's are avoided on the smallest value
                if ((maxLeadingZeros + 3) < maxDP)
                {
                    maxLeadingZeros += 3;
                }
                else
                {
                    maxLeadingZeros = maxDP;
                }

                numberFormat = "N" + maxLeadingZeros.ToString();
                goto End;
            }

        End:
            // Final check. Sometimes the formatted value has the last decimal place all 0's. We loop through here and
            // if it is the case we will go to the next format and check again

            // Split number format to get the integer part
            parts = numberFormat.Split('N');

            // Gets the integer portion of the number format
            int numberFormatIntegerPart = int.Parse(parts[1]);

            bool result = true;

            // Can only potentially improve if format is "N1" or higher...
            if (numberFormatIntegerPart >= 0)
            {
                while (result)
                {
                    // Check the last digit of each array element. If all 0 the number format decrements and the while
                    // loop will run through this again
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            if (dt.Rows[i][j] == DBNull.Value)
                            {
                                result = false;
                                break;
                            }

                            double value = Convert.ToDouble(dt.Rows[i][j]);

                            string s = value.ToString($"N{numberFormatIntegerPart}");

                            // Reset the flag if last digit is not 0
                            if (!s.EndsWith("0"))
                            {
                                result = false;
                                break;
                            }
                        }

                        // Exit if the result went false, no improvement
                        if (!result)
                            break;

                        // When at the end and the flag is still true we can decrement the number format, yah!
                        if (i == rows - 1 && result == true && numberFormatIntegerPart > 0)
                            numberFormatIntegerPart--;
                    }

                    // Exit if the result went false, no improvement
                    if (!result)
                        break;

                    // Exit if we cant decrement any further. "N0" is as good as it gets
                    if (numberFormatIntegerPart == 0)
                    {
                        break;
                    }
                }

                // Rebuild the number format. If nothing changed it will still be the same
                numberFormat = $"N{numberFormatIntegerPart}";
            }

            return numberFormat;
        }

        public static string GetNumberFormat(DataGridViewCell cell)
        {
            // Example usage:
            // DataGridViewCell cell = dgv.Rows[i].Cells[j];
            // string displayedFormat = GetDisplayedNumberFormat(cell);

            // Check if the cell value is numeric
            if (cell.Value != null && cell.Value is IFormattable)
            {
                Type type = cell.FormattedValueType;

                // Retrieve the format provider from the cell style or column style
                IFormatProvider formatProvider = cell.InheritedStyle.FormatProvider ?? cell.OwningColumn.InheritedStyle.FormatProvider;

                // Get the NumberFormatInfo from the format provider
                NumberFormatInfo numberFormat = (NumberFormatInfo)formatProvider.GetFormat(typeof(NumberFormatInfo));

                // Retrieve the format string from the cell style or column style
                string formatString = cell.InheritedStyle.Format ?? cell.OwningColumn.InheritedStyle.Format;

                if (!string.IsNullOrEmpty(formatString))
                {
                    // If a format string is found, return it
                    return formatString;
                }
                else if (formatProvider != null)
                {
                    // If no format string is found but a format provider is available,
                    // use the default format string for the format provider
                    return $"N{numberFormat.NumberDecimalDigits}";
                }
            }

            // Return default format value of N0 if no format is found
            return "N0";
        }

        public static string GetNumberFormat(string s)
        {
            // Split the string by decimal point to determine the number of decimal places
            string[] parts = s.Split('.');

            // If there is no decimal point, return N0
            if (parts.Length == 1)
                return "N0";

            // If there is a decimal point, return N followed by the number of decimal places
            return "N" + parts[1].Length.ToString();
        }
    }
    #endregion

    public class Undo
    #region
    {
        //------------------------- Properties ----------------------------------------------------------------------------------------
        #region 
        public string InstanceName { get; set; }
        public virtual string ClassName { get; set; } = "Undo";
        public bool Debug { get; set; }
        public bool CanDo { get; set; }
        public bool CanSet { get; set; } = true;
        //public bool ReqImage { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public bool Completed { get; set; }
        public bool InProgress { get; set; }
        public int StackCount { get { return stack.Count; } }
        #endregion

        //------------------------- Variables -----------------------------------------------------------------------------------------
        #region 
        protected readonly int MAX_STACK_DEPTH = 100;
        protected readonly int MIN_STACK_DEPTH = 1;

        protected int stackPointer = 0;

        public event EventHandler<DgvData> NDR;

        // Create a list of the Dgvstack data class. Each list entry represents an undo / redo state
        public List<DgvData> stack = new List<DgvData>();
        #endregion

        //------------------------- Functions -----------------------------------------------------------------------------------------
        #region 
        public Undo()
        {
        }

        public Undo (string className, int minStackDepth) // Used by the derived redo class
        {
            ClassName = className;
            MIN_STACK_DEPTH = minStackDepth;
        }

        public void Set(DgvData dgvData)
        {
            // Adds the new dgv image to the bottom of the stack. dgv[index] is the current state. dgv[index - 1] is what will be
            // restored

            if (stackPointer >= MAX_STACK_DEPTH)
            {
                return; // No more room left
            }

            // Check last entry against this new entry to make sure its different
            if (stack.Count > 0 && stack.Last().Equals(dgvData))
            {
                if (Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - Duplicate entry rejected");

                return; // Entry is a double up with the last entry
            }

            // Add current state to the stack
            stack.Add(dgvData);

            // Decrement the stack index
            stackPointer = stack.Count - 1;

            // Make sure our undoDepth variable does not exceed the length of the stack
            if (stackPointer >= MAX_STACK_DEPTH)
            {
                stackPointer = MAX_STACK_DEPTH;
            }

            // Set the can undo property
            if (stackPointer >= MIN_STACK_DEPTH)
            {
                CanDo = true;
            }
            else
            {
                CanDo = false;
            }
        }

        public DgvData Get()
        {
            // The current state is at [index - 1]. The state we want to restore is [index - 2]

            // Check there is something to return I.e. [index] >= 2
            if (stackPointer < MIN_STACK_DEPTH)
            {
                return null; // Nothing to get from the stack
            }

            // When the undo data is written back to the dgv it will cause the firing of the new data event. We don't
            // want this event being written back to the undo stack so this flag is set which is used in the new data
            // event function to prevent a write to the undo stack. This flag should be reset in the dgv new data event
            // function. Also, other classes can use this flag to inhibit there operations. E.g. hover point
            InProgress = true;

            // For the required index, parse out the buffer data to the dgv undo struct where the user program can retrieve
            // the undo values
            DgvData dgvData = GetDgvInstanceAt(stackPointer - 1);

            // Remove the current state
            stack.RemoveAt(stackPointer);

            // Update the stack pointer
            stackPointer = stack.Count - 1;

            // Set the canUndo property
            if (stackPointer >= MIN_STACK_DEPTH)
            {
                CanDo = true;
            }
            else
            {
                CanDo = false;
            }

            // Set the operation completed bit for anyone who wants it. If this flag is used the user code must reset it
            Completed = true;

            // Raises the new data ready event with this instance and the retrieved stack data as event args
            Raise_NDR_Event(dgvData);

            // Or the user can use this data on the function return
            return dgvData;
        }

        protected virtual DgvData GetDgvInstanceAt(int index)
        {
            // Retrieve dgvStack list element at the requested index 
            DgvData item = stack.ElementAt(index);

            return item;
        }

        protected void Raise_NDR_Event(DgvData dgvData)
        {            
            NDR?.Invoke(this, dgvData);
        }

        public void ClearStack()
        {
            stack.Clear();

            stackPointer = 0;

            CanDo = false;
        }
        #endregion
    }
    #endregion

    public class Redo : Undo
    #region
    {
        // Just enough to repurpose the undo class

        public override string ClassName { get { return base.ClassName; } set { base.ClassName = value; } }

        public Redo() : base("Redo", 0) // Sets a default value for the class name, 0 = min stack depth
        { }

        protected override DgvData GetDgvInstanceAt(int index)
        {
            // The index pointer needs to be offset by 1 as the redo stack doesn't have an initial base image capture
            // that the undo stack does
            index += 1; 

            return base.GetDgvInstanceAt(index);
        }
    }
    #endregion

    public class Copy
    #region
    {
        public string InstanceName { get; set; }
        public string ClassName { get; set; } = "Copy";

        public enum SelectMode
        {
            SelectAll,
            SelectedCells
        }

        public enum Headers
        {
            Include,
            Exclude
        }

        public Copy()
        {
        }

        // Copies selected cells or whole table, with or without row & column headers
        public static void CopyClipboard(DgvCtrl dgvCtrl, SelectMode selectMode, Headers headerMode, bool useMyScrollBars)
        {
            StringBuilder sb = new StringBuilder();
            Point currentCellAddress = new Point();
            HashSet<int> selectedRows = new HashSet<int>(); // Collect selected row indexes
            HashSet<int> selectedColumns = new HashSet<int>(); // Collect selected column indexes
            int firstRowIndex;
            int firstColumnIndex;

            // If select all cells mode
            if (selectMode == SelectMode.SelectAll)
            {
                // Remember first selected cell
                currentCellAddress = dgvCtrl.dgv.CurrentCellAddress;

                // Select all cells
                dgvCtrl.dgv.SelectAll();
            }

            // Collect selected column indexes
            foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
            {
                selectedColumns.Add(cell.ColumnIndex);
            }

            // Sort selected column indexes
            var sortedColumns = selectedColumns.OrderBy(index => index).ToList();
            firstColumnIndex = sortedColumns.First();

            // Collect selected row indexes
            foreach (DataGridViewCell cell in dgvCtrl.dgv.SelectedCells)
            {
                selectedRows.Add(cell.RowIndex);
            }

            // Sort selected column indexes
            var sortedRows = selectedRows.OrderBy(index => index).ToList();
            firstRowIndex = sortedRows.First();

            // Include column headers for selected cells
            if (headerMode == Headers.Include)
            {
                // Add a tab char first up
                sb.Append('\t');

                // Include column headers
                foreach (int columnIndex in sortedColumns)
                {
                    if (useMyScrollBars)
                        sb.Append(dgvCtrl.dgvHeaders.dgv_ColHeader.Rows[0].Cells[columnIndex].Value);
                    else
                        sb.Append(dgvCtrl.dgv.Columns[columnIndex].HeaderCell.FormattedValue);

                    // Don't add tab on last column
                    if (columnIndex - firstColumnIndex < sortedColumns.Count - 1)
                        sb.Append('\t');
                }

                sb.AppendLine();
            }

            // Build table data
            foreach (DataGridViewRow row in dgvCtrl.dgv.Rows)
            {
                // Check if any cell in the row is selected
                bool rowSelected = row.Cells.Cast<DataGridViewCell>().Any(cell => cell.Selected);

                if (rowSelected)
                {
                    if (headerMode == Headers.Include)
                    {
                        if (useMyScrollBars)
                            sb.Append(dgvCtrl.dgvHeaders.dgv_RowHeader.Rows[row.Index].Cells[0].Value);
                        else
                            sb.Append(dgvCtrl.dgv.Rows[row.Index].HeaderCell.FormattedValue);
                        sb.Append('\t');
                    }

                    foreach (int columnIndex in sortedColumns)
                    {
                        DataGridViewCell cell = row.Cells[columnIndex];
                        if (cell.Selected)
                        {
                            sb.Append(cell.Value);

                            // Don't add tab on last column
                            if (columnIndex - firstColumnIndex < sortedColumns.Count - 1)
                                sb.Append('\t');
                        }
                    }

                    // If not last row append a new line
                    if (row.Index - firstRowIndex < selectedRows.Count - 1)
                        sb.AppendLine();
                }
            }

            // Restore selected cell if in select all mode
            if (selectMode == SelectMode.SelectAll)
            {
                // Clear selection
                dgvCtrl.dgv.ClearSelection();

                // Restore currently selected cell
                dgvCtrl.dgv.CurrentCell = dgvCtrl.dgv[currentCellAddress.X, currentCellAddress.Y];
            }

            // Copy the string to the clipboard
            try
            {
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} at line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}");
            }
        }
    }
    #endregion

    public class Paste
    #region
    {
        public string InstanceName { get; set; }
        public string ClassName { get; set; } = "Paste";
        public bool Debug { get; set; }
        public bool InProgress { get; private set; }

        public event EventHandler<DgvData> Paste_NDR;

        public Paste()
        {
            clipboard = new MyClipboard();
        }

        #region Enums
        public enum Mode
        {
            None,
            CopyWithAxis,
            Copy,
            PasteTableWithXYAxis,
            PasteTableWithXAxis,
            PasteTableWithYAxis,
            PasteTableWithNoAxis,
            PasteXAxis,
            PasteYAxis,
            PasteToCurrentCell,
            PasteSpecial_MultiplyByPercent,
            PasteSpecial_MultiplyByPercentHalf,
            PasteSpecial_Add,
            PasteSpecial_Subtract,
            Default
        }

        public enum DataSource
        {
            ClipBoard,
            TextFile
        }

        public class MyClipboard
        {
            public string RawClipboardText { get; set; }
            public string[,] RawClipboardTextArray { get; set; }
            public int RowLength { get; set; }
            public int ColLength { get; set; }
            public string[] RawRows { get; set; }
            public string[] RawColumns { get; set; }
            public bool RowHeaderPresent { get; set; }
            public bool ColHeaderPresent { get; set; }
            public bool TableDataPresent { get; set; }
            public bool RowHeaderIsText { get; set; }
            public bool ColHeaderIsText { get; set; }
            public bool HeadersChanged { get; set; }
            public string[] RowHeadersText { get; set; }
            public string[] ColHeadersText { get; set; }
            public double[] RowHeaders { get; set; }
            public double[] ColHeaders { get; set; }
            public double[,] TableData { get; set; }
            public string ErrorText { get; set; }
            public Mode PasteMode { get; set; }

            public void Reset()
            {
                RawClipboardText = "";
                RawClipboardTextArray = new string[0, 0];
                RowLength = 0;
                ColLength = 0;
                RawRows = new string[0];
                RawColumns = new string[0];
                RowHeaderPresent = false;
                ColHeaderPresent = false;
                TableDataPresent = false;
                RowHeaderIsText = false;
                ColHeaderIsText = false;
                RowHeadersText = new string[0];
                ColHeadersText = new string[0];
                RowHeaders = new double[0];
                ColHeaders = new double[0];
                TableData = new double[0, 0];
                ErrorText = "";
                PasteMode = Mode.None;
            }
        }
        public static MyClipboard clipboard;


        #endregion

        public void ParseClipboardToDgv(DgvCtrl dgvCtrl, Mode copyPasteMode, DataSource dataSource = DataSource.ClipBoard, string fileName = null)
        {
            //----------------------
            // Variables 
            //----------------------
            #region
            // Locals
            int i, j;
            DataTable dt = GetBoundDataTableFromDgv(dgvCtrl.dgv);
            bool error = false;
            #endregion

            try
            {
                // Status
                InProgress = true;
                clipboard.Reset();
                clipboard.PasteMode = copyPasteMode;

                if (Debug)
                    Console.WriteLine($"{InstanceName} - {ClassName} - ParseClipboardToDgv.InProgress {InProgress}");

                //------------------------------
                // Set up the clipboard modes 
                //------------------------------
                #region
                switch (copyPasteMode)
                {
                    case Mode.PasteTableWithXYAxis:
                        clipboard.RowHeaderPresent = true;
                        clipboard.ColHeaderPresent = true;
                        clipboard.TableDataPresent = true;
                        break;

                    case Mode.PasteTableWithXAxis:
                        clipboard.RowHeaderPresent = false;
                        clipboard.ColHeaderPresent = true;
                        clipboard.TableDataPresent = true;
                        break;

                    case Mode.PasteTableWithYAxis:
                        clipboard.RowHeaderPresent = true;
                        clipboard.ColHeaderPresent = false;
                        clipboard.TableDataPresent = true;
                        break;

                    case Mode.PasteTableWithNoAxis:
                        clipboard.RowHeaderPresent = false;
                        clipboard.ColHeaderPresent = false;
                        clipboard.TableDataPresent = true;
                        break;

                    case Mode.PasteToCurrentCell:
                        clipboard.RowHeaderPresent = false;
                        clipboard.ColHeaderPresent = false;
                        clipboard.TableDataPresent = true;
                        break;

                    case Mode.PasteXAxis:
                        clipboard.RowHeaderPresent = false;
                        clipboard.ColHeaderPresent = true;
                        clipboard.TableDataPresent = false;
                        break;

                    case Mode.PasteYAxis:
                        clipboard.RowHeaderPresent = true;
                        clipboard.ColHeaderPresent = false;
                        clipboard.TableDataPresent = false;
                        break;

                    case Mode.PasteSpecial_MultiplyByPercent:
                    case Mode.PasteSpecial_MultiplyByPercentHalf:
                    case Mode.PasteSpecial_Add:
                    case Mode.PasteSpecial_Subtract:
                        clipboard.RowHeaderPresent = false;
                        clipboard.ColHeaderPresent = false;
                        clipboard.TableDataPresent = true;
                        break;
                }
                #endregion

                //----------------------
                // Read the input data 
                //----------------------
                #region
                // It will come from either the clipboard or a text file
                if (dataSource == DataSource.TextFile && fileName != null)
                {
                    clipboard.RawClipboardText = File.ReadAllText(fileName);
                }
                else if (dataSource == DataSource.ClipBoard && System.Windows.Forms.Clipboard.ContainsData(DataFormats.Text))
                {
                    clipboard.RawClipboardText = System.Windows.Forms.Clipboard.GetText();
                }
                else
                {
                    clipboard.RawClipboardText = null;
                }

                // Check if we got something
                if (clipboard.RawClipboardText == null)
                {
                    clipboard.ErrorText = $"No data on clipboard";
                    error = true;
                    goto end;
                }

                // Remove all commas from the text
                clipboard.RawClipboardText = clipboard.RawClipboardText.Replace(",", "");
                #endregion

                //---------------------------------
                // Convert into rows and columns 
                //---------------------------------
                #region
                // Split clipboard text into rows
                clipboard.RawRows = clipboard.RawClipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                // If the user has valid data and regardless of the copyPasteMode the first row will be longer or the
                // same length as our final data set. Use the first row to get the initial column length
                clipboard.RawColumns = clipboard.RawRows[0].Split(new char[] { '\t' });

                // Set up our input text array
                clipboard.RawClipboardTextArray = new string[clipboard.RawRows.Length, clipboard.RawColumns.Length];

                // Convert the clipboard data into a 2d array
                for (i = 0; i < clipboard.RawRows.Length; i++)
                {
                    // Extract columns from each row
                    clipboard.RawColumns = clipboard.RawRows[i].Split(new char[] { '\t' });

                    // Writing out the data table
                    for (j = 0; j < clipboard.RawColumns.Length; j++)
                    {
                        clipboard.RawClipboardTextArray[i, j] = clipboard.RawColumns[j];
                    }
                }

                // Initial size of arrays
                clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0);
                clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1);
                #endregion

                //---------------------------------
                // Paste with axis - Process raw clipboard text array
                //---------------------------------
                #region 
                // This section aims to remove all the junk out that the various paste sources might include. After
                // processing the array contains a neat version of the paste data that we can parse out further on.
                switch (copyPasteMode)
                {
                    case Mode.PasteTableWithXYAxis:
                        // If the last row contains only empty or null for column length - 1, then remove the last row.
                        // Similarily, we check the last column also with the same conditions
                        clipboard.RawClipboardTextArray = ProcessLastRow(clipboard.RawClipboardTextArray);
                        clipboard.RawClipboardTextArray = ProcessLastColumn(clipboard.RawClipboardTextArray);
                        break;

                    case Mode.PasteTableWithXAxis:
                        clipboard.RawClipboardTextArray = ProcessLastColumn(clipboard.RawClipboardTextArray);
                        break;

                    case Mode.PasteTableWithYAxis:
                        clipboard.RawClipboardTextArray = ProcessLastRow(clipboard.RawClipboardTextArray);
                        break;
                }
                #endregion

                //----------------------
                // Set lengths 
                //----------------------
                #region
                // Lengths are set based on the paste mode. Software like HP tuners append the row and column header
                // units to the headers. Where we can we always want to use the 2nd row and column to get our table
                // length as this avoids having to detect text and infer if we should trim the array lengths or not.
                // Additionally, having the minimum array lengths check above provides this code section with certainty
                // over the differing dimensions that can accompany the choosen paste mode.

                switch (copyPasteMode)
                #region
                {
                    case Mode.PasteTableWithXYAxis:
                        clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0) - 1;
                        clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1) - 1;
                        break;
                    case Mode.PasteTableWithXAxis:
                        clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0) - 1;
                        clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1);
                        break;
                    case Mode.PasteTableWithYAxis:
                        clipboard.RowLength = clipboard.RawClipboardTextArray.GetLength(0);
                        clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1) - 1;
                        break;
                    case Mode.PasteTableWithNoAxis:
                    case Mode.PasteToCurrentCell:
                    case Mode.PasteSpecial_MultiplyByPercent:
                    case Mode.PasteSpecial_MultiplyByPercentHalf:
                    case Mode.PasteSpecial_Add:
                    case Mode.PasteSpecial_Subtract:
                        clipboard.RowLength = GetRowCountFromRawInputText(clipboard.RawClipboardText);
                        clipboard.ColLength = GetColCountFromRawInputText(clipboard.RawClipboardText);
                        break;

                    case Mode.PasteXAxis:
                        // Do nothing, this was handled in the 'Convert into rows and columns' section earlier
                        break;
                    case Mode.PasteYAxis:
                        // Do nothing, this was handled in the 'Convert into rows and columns' section earlier
                        break;
                }

                // The row and column lengths must be greater than 0
                if (clipboard.RowLength == 0 || clipboard.ColLength == 0)
                {
                    clipboard.ErrorText = $"Set lengths failed. Row or column length is 0";
                    error = true;
                    goto end;
                }
                #endregion
                #endregion

                //-----------------------------------
                // Verify array dimensions 
                //-----------------------------------
                #region
                // --------------------------------|
                // Minimum dimensions              |
                // ---------------------|Row|--|Col|
                // Paste with xy axis:  | 2 |  | 2 |
                // Paste with x axis:   | 2 |  | 1 |
                // Paste with y axis:   | 1 |  | 2 |
                // Paste with no axis:  | 1 |  | 1 |
                // Paste:               | 1 |  | 1 |
                // Paste special:       | 1 |  | 1 |
                // --------------------------------|
                // Maximum dimensions              |
                // ---------------------|Row|--|Col|
                // Paste x axis:        | * |  | * |
                // Paste y axis:        | * |  | * |
                // --------------------------------|
                // * Must be same length as existing dgv table

                switch (copyPasteMode)
                {
                    case Mode.PasteTableWithXYAxis:
                    case Mode.PasteTableWithXAxis:
                    case Mode.PasteTableWithYAxis:
                        // We must have at least 2 rows and 2 columns to continue processing the clipboard
                        if (clipboard.RowLength < 1 || clipboard.ColLength < 1)
                        {
                            clipboard.ErrorText = $"Row or column length is <1";
                            error = true;
                            goto end;
                        }
                        break;

                    case Mode.PasteTableWithNoAxis:
                    case Mode.PasteToCurrentCell:
                    case Mode.PasteSpecial_MultiplyByPercent:
                    case Mode.PasteSpecial_MultiplyByPercentHalf:
                    case Mode.PasteSpecial_Add:
                    case Mode.PasteSpecial_Subtract:
                        // We must have at least 1 row and 1 column to continue processing the clipboard
                        if (clipboard.RowLength < 1 || clipboard.ColLength < 1)
                        {
                            clipboard.ErrorText = $"Row or column length is <1";
                            error = true;
                            goto end;
                        }
                        break;

                    case Mode.PasteXAxis:
                        // If pasting axis, they must be the same length as the dgv
                        if (clipboard.ColLength != dt.Columns.Count)
                        {
                            clipboard.ErrorText = $"Paste single axis failed, the axis length is not equal to the data table length";
                            error = true;
                            goto end;
                        }

                        break;

                    case Mode.PasteYAxis:
                        // If pasting axis, they must be the same length as the dgv
                        if (clipboard.RowLength != dt.Rows.Count)
                        {
                            clipboard.ErrorText = $"Paste single axis failed, the axis length is not equal to the data table length";
                            error = true;
                            goto end;
                        }
                        break;
                }
                #endregion

                //----------------------
                // Parse row headers 
                //----------------------
                #region
                // If row and or column headers are present then we parse them out here. If row headers are not present
                // I.e. paste with column axis mode, we still need to do some processing as further on this function
                // will set up the correct table dimensions

                // Get row headers
                if (clipboard.RowHeaderPresent)
                {
                    clipboard.RowHeaders = new double[clipboard.RowLength];
                    clipboard.RowHeadersText = new string[clipboard.RowLength];

                    int rowLength = clipboard.RowLength;

                    // Write out the text version of the clipboard data
                    // If column header is present, skip the first element
                    if (clipboard.ColHeaderPresent)
                    {
                        for (i = 1; i < rowLength + 1; i++)
                        {
                            clipboard.RowHeadersText[i - 1] = clipboard.RawClipboardTextArray[i, 0];
                        }
                    }
                    else
                    {
                        for (i = 0; i < rowLength; i++)
                        {
                            clipboard.RowHeadersText[i] = clipboard.RawClipboardTextArray[i, 0];
                        }
                    }

                    // Write out the numeric version of the clipboard data
                    for (i = 0; i < rowLength; i++)
                    {
                        if (double.TryParse(clipboard.RowHeadersText[i], out double d_Result))
                        {
                            clipboard.RowHeaders[i] = d_Result;
                        }
                        else // double parse failed, give it the number of the index instead and mark that the axis is text
                        {
                            clipboard.RowHeaders[i] = i;
                            clipboard.RowHeaderIsText = true;
                        }
                    }
                }
                #endregion

                //------------------------
                // Parse column headers 
                //------------------------
                #region
                // Get column headers
                if (clipboard.ColHeaderPresent)
                {
                    clipboard.ColHeaders = new double[clipboard.ColLength];
                    clipboard.ColHeadersText = new string[clipboard.ColLength];

                    int colLength = clipboard.ColLength;

                    // Write out the text version of the clipboard data
                    // If column header is present, skip the first element
                    if (clipboard.RowHeaderPresent)
                    {
                        for (i = 1; i < colLength + 1; i++)
                        {
                            clipboard.ColHeadersText[i - 1] = clipboard.RawClipboardTextArray[0, i];
                        }
                    }
                    else
                    {
                        for (i = 0; i < colLength; i++)
                        {
                            clipboard.ColHeadersText[i] = clipboard.RawClipboardTextArray[0, i];
                        }
                    }

                    // Write out the numeric version of the clipboard data
                    for (i = 0; i < colLength; i++)
                    {
                        if (double.TryParse(clipboard.ColHeadersText[i], out double d_Result))
                        {
                            clipboard.ColHeaders[i] = d_Result;
                        }
                        else // double parse failed, give it the number of the index instead
                        {
                            clipboard.ColHeaders[i] = i;
                            clipboard.ColHeaderIsText = true;
                        }
                    }
                }

                // If pasting a y axis from HP tuners the data comes in a single row with multiple columns. When pasting
                // from pcmtec the data comes in 1 column with multiple rows. For pcmtec, the data is passed on as is.
                // For HP we need to re-dimension the row array and transpose the columns to the rows. For each case the
                // incoming array dimension are checked against the existing data table row length as they must match
                if (copyPasteMode == Mode.PasteYAxis)
                {
                    bool sourceIsHP = false;

                    // Check for correct dimensions. First line is hp, second line is
                    // pcmtec
                    if ((clipboard.RawClipboardTextArray.GetLength(0) == 1 && clipboard.RawClipboardTextArray.GetLength(1) != dt.Rows.Count) ||
                        (clipboard.RawClipboardTextArray.GetLength(1) == 1 && clipboard.RawClipboardTextArray.GetLength(0) != dt.Rows.Count))
                    {
                        clipboard.ErrorText = $"Paste Y axis failed, the attempted paste axis length is not equal to the data table length";
                        goto end;
                    }

                    // Check for which software the paste is coming from
                    if (clipboard.RawClipboardTextArray.GetLength(0) == 1 && clipboard.RawClipboardTextArray.GetLength(1) == dt.Rows.Count)
                    {
                        sourceIsHP = true;
                    }

                    // If coming from hp, redimension the raw text array
                    if (sourceIsHP)
                    {
                        // First we need to make a copy of the array
                        string[,] copyOfRawInputTextArray = new string[clipboard.RawClipboardTextArray.GetLength(0), clipboard.RawClipboardTextArray.GetLength(1)];

                        Array.Copy(clipboard.RawClipboardTextArray, copyOfRawInputTextArray, dt.Rows.Count);

                        // Redimension the raw text array to transposed dimensions
                        clipboard.RawClipboardTextArray = new string[dt.Rows.Count, 1];

                        // Copy the elements
                        for (i = 0; i < dt.Rows.Count; i++)
                        {
                            clipboard.RawClipboardTextArray[i, 0] = copyOfRawInputTextArray[0, i];
                        }
                    }
                    if (sourceIsHP)
                    {
                        clipboard.RowLength = 1;
                        clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(0); // HP
                    }
                    else
                    {
                        clipboard.RowLength = 1;
                        clipboard.ColLength = clipboard.RawClipboardTextArray.GetLength(1); // PCMTEC
                    }
                }
                #endregion

                //--------------------------------------------
                // Set header & data table array dimensions 
                //--------------------------------------------
                #region
                switch (copyPasteMode)
                {
                    case Mode.PasteTableWithXYAxis:
                        clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                        break;

                    case Mode.PasteTableWithXAxis:
                        clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                        break;

                    case Mode.PasteTableWithYAxis:
                        clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                        break;

                    case Mode.PasteTableWithNoAxis:
                        clipboard.RowHeadersText = new string[clipboard.RowLength];
                        clipboard.ColHeadersText = new string[clipboard.ColLength];
                        clipboard.RowHeaders = new double[clipboard.RowLength];
                        clipboard.ColHeaders = new double[clipboard.ColLength];
                        clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                        break;

                    case Mode.PasteXAxis:
                    case Mode.PasteYAxis:
                    case Mode.PasteToCurrentCell:
                    case Mode.PasteSpecial_MultiplyByPercent:
                    case Mode.PasteSpecial_MultiplyByPercentHalf:
                    case Mode.PasteSpecial_Add:
                    case Mode.PasteSpecial_Subtract:
                        clipboard.TableData = new double[clipboard.RowLength, clipboard.ColLength];
                        break;
                }
                #endregion

                //--------------------------------------------
                // Parse table data
                //--------------------------------------------
                #region
                switch (copyPasteMode)
                {
                    case Mode.PasteTableWithXYAxis:
                        #region
                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            for (j = 0; j < clipboard.ColLength; j++)
                            {
                                if (double.TryParse(clipboard.RawClipboardTextArray[i + 1, j + 1], out double d_Result))
                                {
                                    clipboard.TableData[i, j] = d_Result;
                                }
                                else
                                {
                                    clipboard.TableData[i, j] = 0.0; // Write 0 for entries that cannot be parsed to double
                                }
                            }
                        }
                        break;
                    #endregion

                    case Mode.PasteTableWithXAxis:
                        #region
                        // Create token column headers
                        clipboard.RowHeaders = new double[clipboard.RowLength];
                        clipboard.RowHeadersText = new string[clipboard.RowLength];

                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            for (j = 0; j < clipboard.ColLength; j++)
                            {
                                if (double.TryParse(clipboard.RawClipboardTextArray[i + 1, j], out double d_Result))
                                {
                                    clipboard.TableData[i, j] = d_Result;
                                }
                                else
                                {
                                    clipboard.TableData[i, j] = 0.0; // Write 0 for entries that cannot be parsed to double
                                }
                            }
                        }

                        // Create token row headers
                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            clipboard.RowHeaders[i] = i + 1;
                            clipboard.RowHeadersText[i] = (i + 1).ToString();
                        }

                        break;
                    #endregion

                    case Mode.PasteTableWithYAxis:
                        #region
                        clipboard.ColHeaders = new double[clipboard.ColLength];
                        clipboard.ColHeadersText = new string[clipboard.ColLength];

                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            for (j = 1; j < clipboard.ColLength + 1; j++)
                            {
                                if (double.TryParse(clipboard.RawClipboardTextArray[i, j], out double d_Result))
                                {
                                    clipboard.TableData[i, j - 1] = d_Result;
                                }
                                else
                                {
                                    clipboard.TableData[i, j - 1] = 0.0; // Write 0 for entries that cannot be parsed to double
                                }
                            }
                        }

                        // Create token column headers
                        for (i = 0; i < clipboard.ColLength; i++)
                        {
                            clipboard.ColHeaders[i] = i + 1;
                            clipboard.ColHeadersText[i] = (i + 1).ToString();
                        }

                        break;
                    #endregion

                    case Mode.PasteTableWithNoAxis:
                        #region
                        // Create new token labels
                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            clipboard.RowHeadersText[i] = i.ToString();
                            clipboard.RowHeaders[i] = i;
                        }

                        for (i = 0; i < clipboard.ColLength; i++)
                        {
                            clipboard.ColHeadersText[i] = i.ToString();
                            clipboard.ColHeaders[i] = i;
                        }

                        // Write to clipboard data table array
                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            for (j = 0; j < clipboard.ColLength; j++)
                            {
                                if (double.TryParse(clipboard.RawClipboardTextArray[i, j], out double d_Result))
                                {
                                    clipboard.TableData[i, j] = d_Result;
                                }
                                else
                                {
                                    clipboard.TableData[i, j] = 0.0; // Write 0 for entries that cannot be parsed to double
                                }
                            }
                        }
                        break;
                        #endregion
                }
                #endregion

                //--------------------------
                // Write to dgv
                //--------------------------
                #region
                // Write to the axis and or data table depending on mode
                switch (copyPasteMode)
                {
                    // Creates a new table
                    case Mode.PasteTableWithXYAxis:
                    case Mode.PasteTableWithXAxis:
                    case Mode.PasteTableWithYAxis:
                    case Mode.PasteTableWithNoAxis:
                        #region
                        // Write to data grid view then exit
                        dgvCtrl.WriteToDataGridView(clipboard.RowHeaders, clipboard.ColHeaders, clipboard.TableData);
                        dgvCtrl.ClearSelection();
                        clipboard.HeadersChanged = true;

                        if (Debug)
                            Console.WriteLine($"{InstanceName} - {ClassName} - {Enum.GetName(typeof(Mode), copyPasteMode)}");

                        break;
                    #endregion

                    // Works with existing table and redimensions if required
                    case Mode.PasteXAxis:
                        #region
                        dgvCtrl.ReDimensionDataTable_v2(dt.Rows.Count, clipboard.ColHeaders.Length);
                        dgvCtrl.WriteColHeaderLabels(clipboard.ColHeaders);
                        dgvCtrl.ClearSelection();
                        clipboard.HeadersChanged = true;

                        if (Debug)
                            Console.WriteLine($"{InstanceName} - {ClassName} - {Enum.GetName(typeof(Mode), copyPasteMode)}");

                        break;
                    #endregion

                    case Mode.PasteYAxis:
                        #region
                        dgvCtrl.ReDimensionDataTable_v2(clipboard.RowHeaders.Length, dt.Columns.Count);
                        dgvCtrl.WriteRowHeaderLabels(clipboard.RowHeaders);
                        dgvCtrl.ClearSelection();
                        clipboard.HeadersChanged = true;

                        if (Debug)
                            Console.WriteLine($"{InstanceName} - {ClassName} - {Enum.GetName(typeof(Mode), copyPasteMode)}");

                        break;
                    #endregion

                    // Works with existing table
                    case Mode.PasteToCurrentCell:
                    case Mode.PasteSpecial_MultiplyByPercent:
                    case Mode.PasteSpecial_MultiplyByPercentHalf:
                    case Mode.PasteSpecial_Add:
                    case Mode.PasteSpecial_Subtract:
                        #region

                        // Copy the dt 
                        int rowLength = dt.Rows.Count;
                        int columnLength = dt.Columns.Count;
                        double[,] dgvDataTable = new double[rowLength, columnLength];

                        for (i = 0; i < rowLength; i++)
                        {
                            for (j = 0; j < columnLength; j++)
                            {
                                if (dt.Rows[i][j] != DBNull.Value)
                                {
                                    dgvDataTable[i, j] = double.Parse(dt.Rows[i][j].ToString());
                                }
                            }
                        }

                        // Indexes
                        int m = 0, n = 0;

                        // Get the top left address of the current selected cell(s)
                        Point topLeftCallAddress = dgvCtrl.TopLeftCellAddress;

                        // If -1 is returned nothing was selected. In that case set the top left cell to 0, 0
                        if (topLeftCallAddress.X == -1 && topLeftCallAddress.Y == -1)
                        {
                            topLeftCallAddress.X = 0; topLeftCallAddress.Y = 0;
                        }

                        // Parse the clipboard raw text (string) to the clipboard data table (double)
                        for (i = 0; i < clipboard.RowLength; i++)
                        {
                            for (j = 0; j < clipboard.ColLength; j++)
                            {
                                if (double.TryParse(clipboard.RawClipboardTextArray[i, j], out double d_Result))
                                {
                                    clipboard.TableData[i, j] = d_Result;
                                }
                                else
                                {
                                    clipboard.TableData[i, j] = 0.0; // Write 0 for entries that cannot be parsed to double
                                }
                            }
                        }

                        // Using the selected cell as the start address, write the parsed values. Any values that would
                        // overflow the data table are ignored. If paste special call the paste special function instead
                        for (i = topLeftCallAddress.Y; i < dgvDataTable.GetLength(0); i++)
                        {
                            for (j = topLeftCallAddress.X; j < dgvDataTable.GetLength(1); j++)
                            {
                                if (dgvCtrl.DgvHasData)
                                {
                                    switch (copyPasteMode)
                                    {
                                        case Mode.PasteToCurrentCell:
                                            dgvDataTable[i, j] = clipboard.TableData[m, n];
                                            break;

                                        case Mode.PasteSpecial_MultiplyByPercent:
                                            dgvDataTable[i, j] = MultiplyByPercent(1.0, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                            break;

                                        case Mode.PasteSpecial_MultiplyByPercentHalf:
                                            dgvDataTable[i, j] = MultiplyByPercent(0.5, dgvDataTable[i, j], clipboard.TableData[m, n]);
                                            break;

                                        case Mode.PasteSpecial_Add:
                                            dgvDataTable[i, j] = Add(dgvDataTable[i, j], clipboard.TableData[m, n]);
                                            break;

                                        case Mode.PasteSpecial_Subtract:
                                            dgvDataTable[i, j] = Subtract(dgvDataTable[i, j], clipboard.TableData[m, n]);
                                            break;
                                    }
                                }
                                else
                                {
                                    clipboard.ErrorText = $"parse to selected cell at row {i} column {j} failed";
                                    error = true;
                                    goto end;
                                }
                                n++;
                                if (n >= clipboard.TableData.GetLength(1))
                                {
                                    break;
                                }
                            }
                            n = 0;
                            m++;
                            if (m >= clipboard.TableData.GetLength(0))
                            {
                                break;
                            }
                        }

                        // Write the datatable to the clipboard data table. This step is solely to maintain consistancy
                        clipboard.TableData = new double[dgvDataTable.GetLength(0), dgvDataTable.GetLength(1)];
                        for (i = 0; i < dgvDataTable.GetLength(0); i++)
                        {
                            for (j = 0; j < dgvDataTable.GetLength(1); j++)
                            {
                                clipboard.TableData[i, j] = dgvDataTable[i, j];
                            }
                        }

                        // Write to data grid view
                        dgvCtrl.WriteToDataTable(clipboard.TableData);
                        clipboard.HeadersChanged = false;
                        break;
                        #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\r\nAt line {ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(":line"))}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

        end:
            // Paste data NDR notification if no error, else print the error to the console
            if (!error)
            {
                DgvData e = new DgvData
                {
                    RowHeadersText = clipboard.RowHeadersText,
                    ColHeadersText = clipboard.ColHeadersText,
                    RowHeaders = clipboard.RowHeaders,
                    ColHeaders = clipboard.ColHeaders,
                    TableData = clipboard.TableData,
                    RowHeaderFormat = dgvCtrl.RowHeaderFormat,
                    ColHeaderFormat = dgvCtrl.ColHeaderFormat
                };
                e.FormatHeaderText();

                Paste_NDR?.Invoke(this, e);
            }
            else
            {
#if DEBUG
                Console.WriteLine($"{clipboard.ErrorText}");
#endif
            }

            // Status
            InProgress = false;

            // Debug
            if (Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - ParseClipboardToDgv.InProgress {InProgress}");

            return;
        }

        private string[,] ProcessLastRow(string[,] text)
        {
            // If the last row contains only empty or null for column length - 1, then remove the last row.
            // Comparing against column length - 1 allows 1 entry to not meet the criteria
            if (text.GetLength(0) == 1)
                return text;

            int cnt = 0;
            for (int i = 0; i < clipboard.ColLength; i++)
            {
                string s = text[text.GetLength(0) - 1, i];
                if (string.IsNullOrEmpty(s))
                    cnt++;
            }
            // If the cnt >= column length - 1 then the last row can be removed
            if (cnt >= text.GetLength(1) - 1)
            {
                text = RemoveLastRow(clipboard.RawClipboardTextArray);
            }

            return text;
        }

        private string[,] ProcessLastColumn(string[,] text)
        {
            // Looking at the last column in each row, if row length - 1 have nothing then the last column is removed
            if (text.GetLength(1) == 1)
                return text;

            int cnt = 0;
            for (int i = 0; i < text.GetLength(0); i++)
            {
                string s = clipboard.RawClipboardTextArray[i, text.GetLength(1) - 1];
                if (string.IsNullOrEmpty(s))
                    cnt++;
            }
            // If the cnt >= row length - 1 then the last row can be removed
            if (cnt >= text.GetLength(0) - 1)
            {
                text = RemoveLastColumn(clipboard.RawClipboardTextArray);
            }

            return text;
        }

        private int GetRowCountFromRawInputText(string input)
        {
            // Split the input by lines
            //string[] lines = input.Trim().Split('\n');
            string[] lines = input.Split('\n');

            // Initialize a list to store the second entry of each row
            List<string> list = new List<string>();

            // Iterate over each line
            foreach (string line in lines)
            {
                // Split the line by spaces or tabs
                string[] parts = line.Split('\t', '\r');

                string text = parts[0];

                if (Utils.IsNumber(text))
                {
                    list.Add(text);
                }
            }

            return list.Count;
        }

        private int GetColCountFromRawInputText(string input)
        {
            // Split the raw clipboard text into an array of rows, removing any empty entries
            string[] rows = input.Split('\n');

            // Split the second row (index 1) into an array of columns using space and tab characters as delimiters, removing any empty entries
            string[] columns = rows[0].Trim().Split('\t');

            // If column header is present skip the first element as it will be blank
            if (!Utils.IsNumber(columns[0]) || columns[0] == String.Empty)
            {
                columns = columns.Skip(1).ToArray();
            }

            // Look at the last cell to determine if we have text, a number or is empty. Text or an empty value
            // likely means the clipboard data was from HPT, or a number value could mean the clipboard data
            // might be coming from a spreadsheet. If text or empty, then strip it out
            if (columns[columns.Length - 1] == string.Empty || !Utils.IsNumber(columns[columns.Length - 1]))
            {
                columns = columns.Take(columns.Length - 1).ToArray();
            }

            return columns.Length;
        }

        private string[,] RemoveLastRow(string[,] array2D)
        {
            int rows = array2D.GetLength(0);
            int cols = array2D.GetLength(1);

            // Create a new 2D array with one less row
            string[,] newArray2D = new string[rows - 1, cols];

            // Copy all rows except the last one
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newArray2D[i, j] = array2D[i, j];
                }
            }

            return newArray2D;
        }

        private string[,] RemoveLastColumn(string[,] array2D)
        {
            int rows = array2D.GetLength(0);
            int cols = array2D.GetLength(1);

            // Create a new 2D array with one less column
            string[,] newArray2D = new string[rows, cols - 1];

            // Copy all columns except the last one
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols - 1; j++)
                {
                    newArray2D[i, j] = array2D[i, j];
                }
            }

            return newArray2D;
        }

        private DataTable GetBoundDataTableFromDgv(DataGridView dataGridView)
        {
            if (dataGridView.DataSource is DataTable)
            {
                // If DataSource is directly a DataTable
                return (DataTable)dataGridView.DataSource;
            }
            else if (dataGridView.DataSource is BindingSource)
            {
                // If DataSource is a BindingSource
                BindingSource bindingSource = (BindingSource)dataGridView.DataSource;
                if (bindingSource.DataSource is DataTable)
                {
                    return (DataTable)bindingSource.DataSource;
                }
            }

            return null;
        }

        private double MultiplyByPercent(double percentScalar, double dataValue, double modifierValue)
        {
            return dataValue * (1 + modifierValue * percentScalar / 100);
        }

        private double Add(double dataValue, double modifierValue)
        {
            return dataValue + modifierValue;
        }

        private double Subtract(double dataValue, double modifierValue)
        {
            // Apply the multiply by percent calc
            return dataValue - modifierValue;
        }
    }
    #endregion

    public static class LinearInterpolation
    #region
    {
        public enum Mode
        {
            Vertical,
            Horizontal,
            All
        }

        // ------------------------ 1D & 2D table lookups -----------------------------------------------------------------------------
        public static double[,] LookUp_2D(double[] x, double[] y, double[] x_Axis, double[] y_Axis, double[,] z_Data)
        {
            double[,] result = new double[y.Length, x.Length];

            for (int i = 0; i < y.Length; i++)
            {
                for (int j = 0; j < x.Length; j++)
                {
                    result[i, j] = LookUp_1D(x[j], y[i], x_Axis, y_Axis, z_Data);
                }
            }

            return result;
        }

        public static double LookUp_1D(double x, double y, double[] x_Axis, double[] y_Axis, double[,] z_Data)
        {
            // Discrete points on source array that bound our target data points
            double x_1, x_2, y_1, y_2, z_11, z_12, z_21, z_22; // Values
            int x__1, x__2, y__1, y__2; // Indexes

            // Handle edge cases Clamp the target data points to the min axis boundaries, we are not
            // extrapolating
            if (x < x_Axis[0]) x = x_Axis[0];
            if (y < y_Axis[0]) y = y_Axis[0];

            // Clamp the target data points to the max axis boundaries, we are not extrapolating
            if (x > x_Axis[x_Axis.Length - 1]) x = x_Axis[x_Axis.Length - 1];
            if (y > y_Axis[y_Axis.Length - 1]) y = y_Axis[y_Axis.Length - 1];

            // Return the upper & lower x & y axis points
            x__1 = LowerBoundIndex(x, x_Axis);
            x__2 = UpperBoundIndex(x, x_Axis);

            y__1 = LowerBoundIndex(y, y_Axis);
            y__2 = UpperBoundIndex(y, y_Axis);

            x_1 = LowerBoundValue(x, x_Axis);
            x_2 = UpperBoundValue(x, x_Axis);

            y_1 = LowerBoundValue(y, y_Axis);
            y_2 = UpperBoundValue(y, y_Axis);

            // Get the 4 Z points at the x & y co-ordinates
            z_11 = z_Data[y__1, x__1];
            z_12 = z_Data[y__2, x__1];
            z_21 = z_Data[y__1, x__2];
            z_22 = z_Data[y__2, x__2];

            // Interpolate
            try
            {
                return 1 / ((x_2 - x_1) * (y_2 - y_1)) * (z_11 * (x_2 - x) * (y_2 - y) + z_21 * (x - x_1) * (y_2 - y) + z_12 * (x_2 - x) * (y - y_1) + z_22 * (x - x_1) * (y - y_1));
            }
            catch
            {
                return double.NaN;
            }
        }

        private static double LowerBoundValue(double x_Target, double[] x_SearchArray)
        {
            // Find where in the search array x resides
            int index = Array.BinarySearch(x_SearchArray, x_Target);

            // Get lower bound
            if (index >= 0)
            {
                return x_SearchArray[index]; // match, return x array point
            }
            else if (~index > 0)
            {
                return x_SearchArray[~index - 1]; // inbetween, return next lowest x array point 
            }
            else
            {
                return x_SearchArray[~index]; // can't find a lower x array value, clamp to lowest x array value
            }
        }

        private static double UpperBoundValue(double x_Target, double[] x_SearchArray)
        {
            // Find where in the search array x resides
            int index = Array.BinarySearch(x_SearchArray, x_Target);

            // Get upper bound
            if (index >= 0)
            {
                if (index < x_SearchArray.Length - 1)
                {
                    return x_SearchArray[index + 1]; // match, return next highest x array point
                }
                else
                {
                    //return x_SearchArray[index]; // match but at the end of the x array, clamp to
                    //highest x array value
                    return 0;
                }
            }
            else if (~index < x_SearchArray.Length)
            {
                return x_SearchArray[~index]; // inbetween, return next highest x array point
            }
            else
            {
                //return x_SearchArray[~index - 1]; // inbetween but at the end of the x array,
                //clamp to highest x array value
                return 0;
            }
        }

        private static int LowerBoundIndex(double x_Target, double[] x_SearchArray)
        {
            // Find where in the search array x resides
            int index = Array.BinarySearch(x_SearchArray, x_Target);

            // Get lower bound
            if (index >= 0)
            {
                return index; // match, return x array point
            }
            else if (~index > 0)
            {
                return ~index - 1; // inbetween, return next lowest x array point 
            }
            else
            {
                return ~index; // can't find a lower x array value, clamp to lowest x array value
            }
        }

        private static int UpperBoundIndex(double x_Target, double[] x_SearchArray)
        {
            // Find where in the search array x resides
            int index = Array.BinarySearch(x_SearchArray, x_Target);

            // Get upper bound
            if (index >= 0)
            {
                if (index < x_SearchArray.Length - 1)
                {
                    return index + 1; // match, return next highest x array point
                }
                else
                {
                    //return index; // match but at the end of the x array, clamp to highest x array
                    //value
                    return 0;
                }
            }
            else if (~index < x_SearchArray.Length)
            {
                return ~index; // inbetween, return next highest x array point
            }
            else
            {
                //return ~index - 1; // inbetween but at the end of the x array, clamp to highest x
                //array value
                return 0;
            }
        }

        // ------------------------ Nearest neighbour linear interpolation ------------------------------------------------------------
        public static double[,] MissingNeighbour(double[,] in_data)
        {
            // Prep data
            int numRows = in_data.GetLength(0);
            int numCols = in_data.GetLength(1);
            double[,] out_Data = new double[numRows, numCols];

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    if (in_data[i, j] == 0) // If data is missing
                    {
                        // Perform basic nearest neighbour linear interpolation to estimate missing values
                        out_Data[i, j] = NearestNeighour_LinearInterpolation(in_data, i, j);

                        // Set NaN back to 0
                        if (double.IsNaN(out_Data[i, j]))
                        {
                            out_Data[i, j] = 0;
                        }
                    }
                    else
                    {
                        out_Data[i, j] = in_data[i, j]; // Keep original value
                    }
                }
            }

            return out_Data;
        }

        private static double NearestNeighour_LinearInterpolation(double[,] data, int row, int col)
        {
            int numRows = data.GetLength(0);
            int numCols = data.GetLength(1);

            double sum = 0;
            int count = 0;

            // Sum neighboring valid values
            for (int i = Math.Max(0, row - 1); i <= Math.Min(numRows - 1, row + 1); i++)
            {
                for (int j = Math.Max(0, col - 1); j <= Math.Min(numCols - 1, col + 1); j++)
                {
                    if (data[i, j] != 0)
                    {
                        sum += data[i, j];
                        count++;
                    }
                }
            }

            // Compute average of valid neighbors
            return sum / count;
        }


        // ------------------------- Linear interpolation within data range -----------------------------------------------------------
        public static double[,] AutoInterpolate(double[,] data)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[,] interpolatedData = new double[rows, cols];
            bool dataChanged = false;

            // Go row by row
            for (int i = 0; i < rows; i++)
            {
                // Each column in the row
                for (int j = 0; j < cols; j++)
                {
                    if (data[i, j] == 0)
                    {
                        // The initial preference is to vertical interpolate. Once we
                        // can't make that any better, we run over the data set with
                        // horizontal interpolation
                        //
                        // Vertical & horizontal interpolation
                        double verticalInterpolation = 0;
                        double horizontalInterpolation = 0;
                        verticalInterpolation = InterpolateVertically(data, i, j);
                        horizontalInterpolation = InterpolateHorizontally(data, i, j);

                        if (verticalInterpolation != 0 || horizontalInterpolation != 0)
                        {
                            dataChanged = true;

                            // If there is a change in value from vertical interpolation we
                            // keep that value. If there is no change then if there is a
                            // different value from horizontal interpolation we'll take that
                            if (verticalInterpolation != 0)
                            {
                                interpolatedData[i, j] = verticalInterpolation;

                            }
                            else if (horizontalInterpolation != 0)
                            {
                                interpolatedData[i, j] = horizontalInterpolation;
                            }
                        }
                    }
                    else
                    {
                        // Keep the original non-zero values
                        interpolatedData[i, j] = data[i, j];
                    }
                }
            }

            if (dataChanged)
            {
                return AutoInterpolate(interpolatedData); // Run function again
            }
            else
            {
                return interpolatedData;
            }
        }

        private static double InterpolateHorizontally(double[,] data, int row, int col)
        {
            double leftValue = 0;
            double rightValue = 0;
            double interpolatedValue = 0;
            int leftIndex = -1;
            int rightIndex = -1;

            for (int k = col - 1; k >= 0; k--)
            {
                if (data[row, k] != 0)
                {
                    leftValue = data[row, k];
                    leftIndex = k;
                    break;
                }
            }

            for (int k = col + 1; k < data.GetLength(1); k++)
            {
                if (data[row, k] != 0)
                {
                    rightValue = data[row, k];
                    rightIndex = k;
                    break;
                }
            }

            // Linear interpolation formula
            if (leftIndex != -1 && rightIndex != -1)
            {
                interpolatedValue = leftValue + (rightValue - leftValue) * (col - leftIndex) / (rightIndex - leftIndex);
            }
            else
            {
                interpolatedValue = 0;
            }

            return interpolatedValue;
        }

        private static double InterpolateVertically(double[,] data, int row, int col)
        {
            double topValue = 0;
            double bottomValue = 0;
            int topIndex = -1;
            int bottomIndex = -1;

            for (int k = row - 1; k >= 0; k--)
            {
                if (data[k, col] != 0 && !data[k, col].Equals(double.NaN))
                {
                    topValue = data[k, col];
                    topIndex = k;
                    break;
                }
            }

            for (int k = row + 1; k < data.GetLength(0); k++)
            {
                if (data[k, col] != 0 && !data[k, col].Equals(double.NaN))
                {
                    bottomValue = data[k, col];
                    bottomIndex = k;
                    break;
                }
            }

            // Linear interpolation formula
            if (topIndex != -1 && bottomIndex != -1)
            {
                return topValue + (bottomValue - topValue) * (row - topIndex) / (bottomIndex - topIndex);
            }
            else
            {
                return 0;
            }
        }

        public static void Interpolate(DataGridView dgv, DataGridViewSelectedCellCollection selectedCellCollection, Mode interpolateMode)
        {
            // The bare minimum we need to proceed with interpolation are 2 row indexes
            // within the same column index or vica versa for horizontal interpolation. If
            // we find 2 matching column indexes then we know that particular column index
            // has at least 2 rows selected and again vica versa for horizontal
            // interpolation 

            // Get the bound data table
            DataTable dt = (DataTable)dgv.DataSource;

            // Return if nothing selected
            if (selectedCellCollection.Count == 0)
            {
                return;
            }

            // List for storing the user selected row and column indexes
            List<(int a_Index, int b_Index)> rawSelectedIndexes = new List<(int, int)>();

            // Get the user selected cell indexes in column -> row for vertical
            // interpolation and row -> column for horizontal interpolation
            switch (interpolateMode)
            {
                case Mode.Vertical:
                    // Get the selected column indexes with their respective row indexes
                    foreach (DataGridViewCell cell in selectedCellCollection)
                    {
                        rawSelectedIndexes.Add((cell.ColumnIndex, cell.RowIndex)); // column then row
                    }
                    break;

                case Mode.Horizontal:
                    // Get the selected row indexes with their respective column indexes
                    foreach (DataGridViewCell cell in selectedCellCollection)
                    {
                        rawSelectedIndexes.Add((cell.RowIndex, cell.ColumnIndex)); // row then column
                    }
                    break;

                case Mode.All:
                    // Recursively call this function using both interpolation modes
                    Interpolate(dgv, selectedCellCollection, Mode.Vertical);
                    Interpolate(dgv, selectedCellCollection, Mode.Horizontal);
                    return;
            }

            // Sort the selected indexes
            rawSelectedIndexes.Sort();

            // Sort the list based on the a_Index values
            // rawSelectedIndexes.Sort((x, y) => x.a_Index.CompareTo(y.a_Index));

            // Create a new list to store unique primary index number elements
            HashSet<int> temp_ValidIndexes = new HashSet<int>(); // helper list
            HashSet<int> validIndexes = new HashSet<int>();

            // Flag to indicate if duplicates are found
            bool validIndexesFound = false;

            // Iterate through the list of primary indexes
            foreach (var index in rawSelectedIndexes)
            {
                // If the element is already in the set, it's a duplicate
                if (!temp_ValidIndexes.Add(index.a_Index))
                {
                    validIndexes.Add(index.a_Index);
                    validIndexesFound = true;
                }
            }

            // Exit if no valid indexes were found
            if (!validIndexesFound)
            {
                return;
            }

            // There will be 3 values for each entry; the primary index followed by the
            // min and max secondary indexes I.e for vertical interpolation we will have
            // column index, low row index, high row index. Note these are the index
            // numbers, the cell values themselves are not checked at this point
            List<(int a_Index, int Min_b_Index, int Max_b_Index)> validSelectedIndexes = new List<(int, int, int)>();

            // For each valid primary index get the respective minimum and maximum
            // selected secondary indexes
            foreach (var index in validIndexes)
            {
                // Filter the list to get secondary indexes corresponding to the current primary index
                var b_Indexes = rawSelectedIndexes
                    .Where(cell => cell.a_Index == index)
                    .Select(cell => cell.b_Index);

                int min_b_Index = int.MaxValue;
                int max_b_Index = int.MinValue;

                // Iterate over each secondary index for the current primary index
                foreach (var b_Index in b_Indexes)
                {
                    min_b_Index = Math.Min(min_b_Index, b_Index);
                    max_b_Index = Math.Max(max_b_Index, b_Index);
                }

                // Write the entry to the list
                validSelectedIndexes.Add((index, min_b_Index, max_b_Index));
            }

            // For each valid primary index, interpolate the cells
            foreach (var pair in validSelectedIndexes)
            {
                double minRowIndex_CellValue = double.NaN;
                double maxRowIndex_CellValue = double.NaN;

                switch (interpolateMode)
                {
                    case Mode.Vertical:
                        // Check dimensions are valid
                        if (pair.Min_b_Index > dgv.Rows.Count || pair.Max_b_Index > dgv.Rows.Count || pair.a_Index > dgv.Columns.Count)
                        {
                            return;
                        }

                        // Retrieve the cell values in row -> column
                        minRowIndex_CellValue = (double)dt.Rows[pair.Min_b_Index][pair.a_Index];
                        maxRowIndex_CellValue = (double)dt.Rows[pair.Max_b_Index][pair.a_Index];
                        break;

                    case Mode.Horizontal:
                        // Check dimensions are valid
                        if (pair.Min_b_Index > dgv.Columns.Count || pair.Max_b_Index > dgv.Columns.Count || pair.a_Index > dgv.Rows.Count)
                        {
                            return;
                        }

                        // Retrieve the cell values in row -> column
                        minRowIndex_CellValue = (double)dt.Rows[pair.a_Index][pair.Min_b_Index];
                        maxRowIndex_CellValue = (double)dt.Rows[pair.a_Index][pair.Max_b_Index];
                        break;

                    case Mode.All:

                        break;
                }

                // Get the cell index distances between the cell pairs
                int indexDistance = pair.Max_b_Index - pair.Min_b_Index;

                // To create a true linear interpolation we cant just divide the difference between min and max by the
                // number of cells, we have to look at the axis header(s) to determine the proportionate increment value
                // to apply.

                // Retrieve the axis headers bound by the min and max cell indexes
                double[] axisList = new double[pair.Max_b_Index + 1];

                switch (interpolateMode)
                {
                    case Mode.Vertical:
                        for (int i = 0; i < pair.Max_b_Index + 1; i++)
                        {
                            if (!double.TryParse(dgv.Rows[i].HeaderCell.Value.ToString(), out axisList[i]))
                                return; // error
                        }

                        break;

                    case Mode.Horizontal:
                        for (int i = 0; i < pair.Max_b_Index + 1; i++)
                        {
                            if (!double.TryParse(dgv.Columns[i].HeaderCell.Value.ToString(), out axisList[i]))
                                return; // error
                        }

                        break;

                    case Mode.All:

                        break;
                }

                // New array of cell values. The first and last value are the first and last value from the current
                // selection. The remaining values will be interpolated
                double[] cellValues = new double[indexDistance + 1];
                cellValues[0] = minRowIndex_CellValue;
                cellValues[cellValues.Length - 1] = maxRowIndex_CellValue;

                // The difference between the cell selection respective min and max axis header values
                double overallAxisDistance = axisList[pair.Max_b_Index] - axisList[pair.Min_b_Index];

                for (int i = 1; i < cellValues.Length - 1; i++)
                {
                    // Difference between this axis header and 1 previous. Offset i with the min index of the current
                    // selection to align the respective axisList value with the axis pair we are working with
                    double thisAxisDistance = axisList[pair.Min_b_Index + i] - axisList[pair.Min_b_Index + i - 1];

                    // % of the overall axis distance 
                    double proportionalIncrement = thisAxisDistance / overallAxisDistance;

                    // Get the difference between the start and end cell values
                    double cellAbsoluteDifference = Math.Max(maxRowIndex_CellValue, minRowIndex_CellValue) - Math.Min(maxRowIndex_CellValue, minRowIndex_CellValue);

                    // Increment direction positive or negative
                    double increment = 0;
                    if (maxRowIndex_CellValue > minRowIndex_CellValue)
                        increment = cellAbsoluteDifference * proportionalIncrement;
                    else
                        increment = cellAbsoluteDifference * -proportionalIncrement;

                    // Just to keep things tidy, round the increment to the preset number of decimal places. There are
                    // occasions where the output data will not be perfectly linear
                    if (Math.Abs(increment) > 1)
                    {
                        increment = Math.Round(increment, 2);
                    }
                    else if (Math.Abs(increment) > 0.1)
                    {
                        increment = Math.Round(increment, 3);
                    }
                    else if (Math.Abs(increment) > 0.01)
                    {
                        increment = Math.Round(increment, 4);
                    }
                    else if (Math.Abs(increment) > 0.001)
                    {
                        increment = Math.Round(increment, 5);
                    }
                    else if (Math.Abs(increment) > 0.0001)
                    {
                        increment = Math.Round(increment, 6);
                    }
                    else if (Math.Abs(increment) > 0.00001)
                    {
                        increment = Math.Round(increment, 7);
                    }
                    else
                    {
                        increment = Math.Round(increment, 8);
                    }

                    cellValues[i] = cellValues[i - 1] + increment;
                }

                for (int i = 0; i < cellValues.Length; i++)
                {
                    switch (interpolateMode)
                    {
                        case Mode.Vertical:
                            // Check dimensions are valid
                            if (pair.Min_b_Index > dgv.Rows.Count || pair.Max_b_Index > dgv.Rows.Count || pair.a_Index > dgv.Columns.Count)
                            {
                                return;
                            }

                            // Write the cell value in row -> column
                            dt.Rows[pair.Min_b_Index + i][pair.a_Index] = cellValues[i];

                            break;

                        case Mode.Horizontal:
                            // Check dimensions are valid
                            if (pair.Min_b_Index > dgv.Columns.Count || pair.Max_b_Index > dgv.Columns.Count || pair.a_Index > dgv.Rows.Count)
                            {
                                return;
                            }

                            // Write the cell value in row -> column
                            dt.Rows[pair.a_Index][pair.Min_b_Index + i] = cellValues[i];
                            break;

                        case Mode.All:

                            break;
                    }
                }
            }
        }

        private static int CountZeros(double value)
        {
            return value == 0 ? 1 : 0;
        }
    }
    #endregion

    public static class MovingAverage
    #region
    {
        public static double Weight { get { return weight; } set { weight = value; } }

        public enum Mode
        {
            Vertical,
            Horizontal,
            All
        }

        // Flag for alternating who goes first in smooth all mode
        static bool goFirst = false;

        static double weight = 0.5;

        public static void Smooth(DataGridView dgv, DataGridViewSelectedCellCollection selectedCellCollection, Mode smoothingMode)
        {
            // The bare minimum we need to proceed with smoothing are 2 row indexes within the same column index or vica
            // versa for horizontal interpolation. If we find 2 matching column indexes then we know that particular
            // column index has at least 2 rows selected and again vica versa for horizontal interpolation 

            // Get the bound data table
            DataTable dt = (DataTable)dgv.DataSource;

            // Return if nothing selected
            if (selectedCellCollection.Count == 0)
            {
                return;
            }

            // List for storing the user selected row and column indexes
            List<(int a_Index, int b_Index)> rawSelectedIndexes = new List<(int, int)>();

            // Get the user selected cell indexes in column -> row for vertical interpolation and row -> column for
            // horizontal interpolation
            switch (smoothingMode)
            {
                case Mode.Vertical:
                    // Get the selected column indexes with their respective row indexes
                    foreach (DataGridViewCell cell in selectedCellCollection)
                    {
                        rawSelectedIndexes.Add((cell.ColumnIndex, cell.RowIndex)); // column then row
                    }
                    break;

                case Mode.Horizontal:
                    // Get the selected row indexes with their respective column indexes
                    foreach (DataGridViewCell cell in selectedCellCollection)
                    {
                        rawSelectedIndexes.Add((cell.RowIndex, cell.ColumnIndex)); // row then column
                    }
                    break;

                case Mode.All:
                    // Recursively call this function using both interpolation modes. Alternate who goes first
                    if (goFirst)
                    {
                        Smooth(dgv, selectedCellCollection, Mode.Vertical);
                        Smooth(dgv, selectedCellCollection, Mode.Horizontal);
                    }
                    else
                    {
                        Smooth(dgv, selectedCellCollection, Mode.Horizontal);
                        Smooth(dgv, selectedCellCollection, Mode.Vertical);
                    }
                    goFirst = !goFirst;
                    return;
            }

            // Sort the selected indexes
            rawSelectedIndexes.Sort();

            // Create a new list to store unique primary index number elements
            HashSet<int> temp_ValidIndexes = new HashSet<int>(); // helper list
            HashSet<int> validIndexes = new HashSet<int>();

            // Flag to indicate if duplicates are found
            bool validIndexesFound = false;

            // Iterate through the list of primary indexes
            foreach (var index in rawSelectedIndexes)
            {
                // If the element is already in the set, it's a duplicate
                if (!temp_ValidIndexes.Add(index.a_Index))
                {
                    validIndexes.Add(index.a_Index);
                    validIndexesFound = true;
                }
            }

            // Exit if no valid indexes were found
            if (!validIndexesFound)
            {
                return;
            }

            // There will be 3 values for each entry; the primary index followed by the min and max secondary indexes
            // I.e for vertical interpolation we will have column index, low row index, high row index. Note these are
            // the index numbers, the cell values themselves are not checked at this point
            List<(int a_Index, int Min_b_Index, int Max_b_Index)> validSelectedIndexes = new List<(int, int, int)>();

            // For each valid primary index get the respective minimum and maximum selected secondary indexes
            foreach (var index in validIndexes)
            {
                // Filter the list to get secondary indexes corresponding to the current primary index
                var b_Indexes = rawSelectedIndexes
                    .Where(cell => cell.a_Index == index)
                    .Select(cell => cell.b_Index);

                int min_b_Index = int.MaxValue;
                int max_b_Index = int.MinValue;

                // Iterate over each secondary index for the current primary index
                foreach (var b_Index in b_Indexes)
                {
                    min_b_Index = Math.Min(min_b_Index, b_Index);
                    max_b_Index = Math.Max(max_b_Index, b_Index);
                }

                // Write the entry to the list
                validSelectedIndexes.Add((index, min_b_Index, max_b_Index));
            }

            // For each valid primary index, interpolate the cells
            foreach (var pair in validSelectedIndexes)
            {
                // Get the cell index distances between the cell pairs
                int distance = pair.Max_b_Index - pair.Min_b_Index;

                // Smoothing testing
                double[] values = new double[distance + 1];

                switch (smoothingMode)
                {
                    case Mode.Vertical:
                        // Retrieve the cell values
                        for (int i = pair.Min_b_Index; i < pair.Max_b_Index + 1; i++)
                        {
                            values[i - pair.Min_b_Index] = (double)dt.Rows[i][pair.a_Index];
                        }

                        // Apply the average 
                        values = MovingAvg(values);

                        // Write back to the cells
                        for (int i = pair.Min_b_Index; i < pair.Max_b_Index + 1; i++)
                        {
                            dt.Rows[i][pair.a_Index] = values[i - pair.Min_b_Index];
                        }

                        break;

                    case Mode.Horizontal:
                        // Retrieve the cell values
                        for (int i = pair.Min_b_Index; i < pair.Max_b_Index + 1; i++)
                        {
                            values[i - pair.Min_b_Index] = (double)dt.Rows[pair.a_Index][i];
                        }

                        // Apply the average 
                        values = MovingAvg(values);

                        // Write back to the cells
                        for (int i = pair.Min_b_Index; i < pair.Max_b_Index + 1; i++)
                        {
                            dt.Rows[pair.a_Index][i] = values[i - pair.Min_b_Index];
                        }

                        break;

                    case Mode.All:

                        break;
                }
            }
        }

        private static List<double> MovingAvg(List<double> data, int windowSize = 2)
        {
            List<double> smoothedData = new List<double>();

            // First point remains unchanged
            smoothedData.Add(data[0]);

            // Calculate moving averages for middle points
            for (int i = 1; i < data.Count - 1; i++)
            {
                int startIndex = Math.Max(0, i - windowSize / 2);
                int endIndex = Math.Min(data.Count - 1, i + windowSize / 2);

                double sum = 0;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    sum += data[j];
                }
                double average = sum / (endIndex - startIndex + 1);

                // Difference between average and current value
                double delta = data[i] - average;

                // Only apply a weighted percentage of the delta between the current data value and the average
                smoothedData.Add(data[i] - delta * weight);
            }

            // Last point remains unchanged
            smoothedData.Add(data[data.Count - 1]);

            return smoothedData;
        }

        private static double[] MovingAvg(double[] data, int windowSize = 2)
        {
            double[] smoothedData = new double[data.Length];

            // First point remains unchanged
            smoothedData[0] = data[0];

            // Calculate moving averages for middle points
            for (int i = 1; i < data.Length - 1; i++)
            {
                int startIndex = Math.Max(0, i - windowSize / 2);
                int endIndex = Math.Min(data.Length - 1, i + windowSize / 2);

                double sum = 0;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    sum += data[j];
                }
                double average = sum / (endIndex - startIndex + 1);

                // Difference between average and current value
                double delta = data[i] - average;

                // Only apply a weighted percentage of the delta between the current data value and the average
                smoothedData[i] = data[i] - delta * weight;
            }

            // Last point remains unchanged
            smoothedData[data.Length - 1] = data[data.Length - 1];

            return smoothedData;
        }

        private static void MovingAvgIterations_Example()
        {
            List<double> originalData = new List<double>
            {
                0.638984025, 0.8475, 0.8725, 0.7775, 0.56, 0.486637264, 0.5025, 0.395,
                0.355, 0.5175, 0.71, 0.78, 0.7525, 0.7375, 0.8175, 0.7875, 0.617592096
            };

            List<List<double>> iterationsData = new List<List<double>>();
            iterationsData.Add(originalData);

            for (int i = 0; i < 5; i++)
            {
                iterationsData.Add(MovingAvg(iterationsData.Last(), 2));
            }
        }
    }
    #endregion

    public static class StopWatchManager
    #region
    {
        // Global stopwatch available for debugging
        public static MyStopWatch stopWatch = new MyStopWatch();
    }
    #endregion

    public static class Init
    #region
    {
        public static Size Graph3dMinimumSize { get { return new Size(350, 350); } }
        public static Size Graph3dInitialSize { get { return new Size(400, 400); } } // fudged to get zoom @2100 right
        public static Size FormOpeningSize { get { return new Size(880, 600); } }
        public static Size FormMinimumSize { get { return new Size(376, 496); } } // Modify as needed to get the user control dgv / graph area to 350, 350
        public static int ToolBarHeight { get { return 76; } }
        public static int SplitContainerSplitterWidth { get { return 8; } }
        public static int SplitContainerPanel1MinSize { get { return 125; } }
        public static int SplitContainerPanel2MinSize { get { return Graph3dMinimumSize.Width; } }
        public static int SplitContainerSplitterDistance { get { return FormOpeningSize.Width - Graph3dMinimumSize.Width - SplitContainerSplitterWidth; } }
    }
    #endregion

    public class MyDebug
    #region
    {
        public bool TableEditor_Form { set { tableEditor.DebugForm = value; } }
        public bool TableEditor_Mouse { set { tableEditor.DebugMouse = value; } }
        public bool TableEditor_SplitContainer { set { tableEditor.DebugSplitContainer = value; } }
        public bool DgvCtrl_DataChangedDebug { set { tableEditor.dgvCtrl.DataChangedDebug = value; } }
        public bool DgvCtrl_SelectionChangedDebug { set { tableEditor.dgvCtrl.SelectionChangedDebug = value; } }
        public bool DgvCtrl_SizeChangedDebug { set { tableEditor.dgvCtrl.SizeChangedDebug = value; } }
        public bool DgvCtrl_EventDebug { set { tableEditor.dgvCtrl.EventDebug = value; } }
        public bool DgvCtrl_incDecTask_Debug { set { tableEditor.dgvCtrl.incDecTask.Debug = value; } }
        public bool DgvCtrl_paste_Debug { set { tableEditor.dgvCtrl.paste.Debug = value; } }
        public bool DgvCtrl_undo_Debug { set { if (tableEditor.dgvCtrl.undo != null) tableEditor.dgvCtrl.undo.Debug = value; } }
        public bool DgvCtrl_DgvData_Debug { set { tableEditor.dgvCtrl.DgvData_Debug = value; } }
        public bool DgvCtrl_MouseDebug { set { tableEditor.dgvCtrl.DebugMouse = value; } }
        public bool DgvCtrl_myEvents_DebugAll { set { tableEditor.dgvCtrl.myEvents.DebugAll = value; } }
        public bool DgvCtrl_myEvents_DebugDataChngd { set { tableEditor.dgvCtrl.myEvents.DebugDataChanged = value; } }
        public bool DgvCtrl_myEvents_DebugSizeChngd { set { tableEditor.dgvCtrl.myEvents.DebugSizeChanged = value; } }
        public bool DgvCtrl_myEvents_DebugSelnChngd { set { tableEditor.dgvCtrl.myEvents.DebugSelnChanged = value; } }
        public bool DgvCtrl_myEvents_MuteHghSpd { set { tableEditor.dgvCtrl.myEvents.DebugMuteHighSpeed = value; } }
        public bool DgvCtrl_myEvents_DebugDbncTmr { set { tableEditor.dgvCtrl.myEvents.DebugDbncTmr = value; } }
        public bool DgvCtrl_myEvents_DebugIntTmr { set { tableEditor.dgvCtrl.myEvents.DebugIntTmr = value; } }
        public bool ScrollBarCtrl_DebugPosition { set { if (tableEditor.dgvCtrl.scrollBarCtrl != null) tableEditor.dgvCtrl.scrollBarCtrl.DebugPosition = value; } }
        public bool ScrollBarCtrl_DebugExternalEvents { set { if (tableEditor.dgvCtrl.scrollBarCtrl != null) tableEditor.dgvCtrl.scrollBarCtrl.DebugExternalEvents = value; } }
        public bool ScrollBarCtrl_DebugMouseWheel { set { if (tableEditor.dgvCtrl.scrollBarCtrl != null) tableEditor.dgvCtrl.scrollBarCtrl.DebugMouseWheel = value; } }
        public bool ScrollBarCtrl_DebugValues { set { if (tableEditor.dgvCtrl.scrollBarCtrl != null) tableEditor.dgvCtrl.scrollBarCtrl.DebugScrollBars = value; } }
        public bool DgvHeaders_DebugHeaders { set { if (tableEditor.dgvCtrl.dgvHeaders != null) tableEditor.dgvCtrl.dgvHeaders.DebugHeaders = value; } }
        public bool DgvGrph3dIntfc_DebugAll { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugAll = value; } }
        public bool DgvGrph3dIntfc_DebugData { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugData = value; } }
        public bool DgvGrph3dIntfc_DebugTimers { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugTimers = value; } }
        public bool DgvGrph3dIntfc_DebugHoverPoint { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugHoverPoint = value; } }
        public bool DgvGrph3dIntfc_DebugPointMoveMode { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugPointMoveMode = value; } }
        public bool DgvGrph3dIntfc_DebugSelectionPoints { set { if (tableEditor.dgvGrph3dIntfc != null) tableEditor.dgvGrph3dIntfc.DebugSelection = value; } }
        public bool Graph3dCtrl_DebugPointMoveMode { set { if (tableEditor.graph3dCtrl != null) tableEditor.graph3dCtrl.DebugPointMoveMode = value; } }
        public bool Graph3dCtrl_DebugData { set { if (tableEditor.graph3dCtrl != null) tableEditor.graph3dCtrl.DebugData = value; } }
        public bool Graph3dCtrl_DebugData_WithPrint { set { if (tableEditor.graph3dCtrl != null) tableEditor.graph3dCtrl.DebugData_WithPrint = value; } }
        public bool Graph3dCtrl_DebugPointSelectMode { set { if (tableEditor.graph3dCtrl != null) tableEditor.graph3dCtrl.DebugPointSelectMode = value; } }

        public MyDebug(TableEditor3D tableEditor)
        {
            this.tableEditor = tableEditor;
        }

        TableEditor3D tableEditor;
    }
    #endregion

    public static class Logger
    #region
    {
        /*
         * Usage example with output using the default logMessage string:
         * 
         * public void MyTest()
         * {
         *      Logger.Log(this, "my awesome message");
         * 
         *      // Carry on with your code below...
         * }
         * 
         * Output: [18/05/2024 1:01:05 PM] Form1 (Form1).myTest (Line 50): my awesome message
         */

        /*
         * Logging to file is supported. Set the default value of the property IsFileLoggingEnabled to true
         */

        /*
         * To use the logger method template I made (Logger.snippet) when wanting to create a new log entry, start typing
         * logger. When the snippet name pops up in the code browser, tab twice to insert it. If it can't be found or you
         * want to load an update you made to the .snippet file; navigate to Tools --> Code Snippets Manager. Add a desired
         * folder location if not present, then re-import the snippet.
         */

        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        public static bool IsFileLoggingEnabled { get; set; } = false; // Default to false, change to true to enable logging to file

        static Logger()
        {
            // Ensures the log file is created if file logging is enabled
            if (IsFileLoggingEnabled && !File.Exists(logFilePath))
            {
                using (var stream = File.Create(logFilePath)) { }
            }
        }

        [Conditional("DEBUG")]
        public static void Log(
            object instance,
            string message = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            string instanceName = instance.GetType().Name;
            //string logMessage = $"[{DateTime.Now}] {className} ({instanceName}).{memberName} (Line {lineNumber}): {message}";
            string logMessage = $"({instanceName}).{memberName} {message}";

            // Write to the debug output
            Debug.WriteLine(logMessage);

            // Write to the log file if enabled
            if (IsFileLoggingEnabled)
            {
                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine(logMessage);
                }
            }
        }
    }
    #endregion
}

namespace Timers
#region
{
    // Utilises the MicroLibrary library to create timer on and off delays that run parallel to the gui thread

    public class MyStopWatch
    #region
    {
        readonly Stopwatch stopwatch;

        public MyStopWatch()
        {
            stopwatch = new Stopwatch();
        }

        public MyStopWatch(bool startNow)
        {
            stopwatch = new Stopwatch();

            if (startNow)
                stopwatch.Start();
        }

        public string Get(bool autoReset = false, bool stop = false)
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();

            // Use timespan to enable the reformatting of stopwatch with high resolution
            TimeSpan timeSpan = stopwatch.Elapsed;

            // Format to display seconds.milliseconds:nanoseconds
            //string formattedElapsed = $"{timeSpan.Seconds}.{timeSpan.Milliseconds:000}:{timeSpan.Ticks / 100:000}";

            // Format to display seconds.milliseconds
            string formattedElapsed = $"{timeSpan.Seconds}.{timeSpan.Milliseconds:000}s";

            if (autoReset)
                stopwatch.Restart();

            if (stop)
                stopwatch.Stop();

            return formattedElapsed;
        }

        public void Start()
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();
            else
                stopwatch.Restart();
        }

        public void Restart()
        {
            if (!stopwatch.IsRunning)
                stopwatch.Start();
            else
                stopwatch.Restart();
        }

        public void Stop()
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
                stopwatch.Reset();
            }
        }
    }
    #endregion

    public class TimerOnDelay
    #region
    {
        #region Properties
        /// <summary>
        /// Timer preset value in milliseconds
        /// </summary>
        public int Preset { get; set; }

        /// <summary>
        /// Timer accumulator value in ms. Decrements as timer is timing down
        /// </summary>
        public int Accumulator { get; private set; }

        /// <summary>
        /// True when timer is timing down
        /// </summary>
        public bool TimerTiming { get; private set; }

        /// <summary>
        /// Stays high until timer is next enabled
        /// </summary>
        public bool TimerDone { get; private set; }

        /// <summary>
        /// Enables 'endless timing' firing the timer done event each timing period. Call Stop() to end
        /// </summary>
        public bool AutoRestart { get; set; }

        /// <summary>
        /// If using AutoRestart timing mode and this property is set true, the timer will automatically stop if a call
        /// to Start() has not been recieved within the number of timer done events as configured in the
        /// AutoRestartOffCounts property
        /// </summary>
        public bool AutoStop { get; set; }

        /// <summary>
        /// If in AutoRestart timing mode and if AutoStop is true and a call to Start() hasn't been recieved for this
        /// amount of timer done events, the timer will stop
        /// </summary>
        public int AutoStop_CountsPreset { get; set; }

        /// <summary>
        /// If auto restart is used, this property is the counts to go until the timer stops
        /// </summary>
        public int AutoStop_CountsToGo { get; private set; }

        /// <summary>
        /// Debug mode. Writes status to console
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// The name of the class the owns this timer instance
        /// </summary>
        public string DebugInstanceName { get; set; }

        /// <summary>
        /// Meanigful name for the debug console output
        /// </summary>
        public string DebugTimerName { get; set; }

        /// <summary>
        /// Form control reference. Used as a reference for the timer done event to be invoked back to the ui thread
        /// </summary>       
        public static Control uiControl { get; set; }
        #endregion

        #region Variables

        private readonly MyStopWatch stopWatch;

        private readonly MicroTimer microTimer;

        public delegate void TimingDoneCallback();

        public TimingDoneCallback OnTimingDone; // Assign your other class callback method to this variable

        bool restartLatch = false;
        #endregion

        #region Constructor
        public TimerOnDelay()
        {
            // New MicroTimer and add event handler
            microTimer = new MicroTimer();
            microTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(Tick);

            // Stopwatch for debug mode
            stopWatch = new MyStopWatch();

            // Set tick interval. I've tested various values and settled on 2ms resolution gives consistent ticks
            microTimer.Interval = 2000;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Starts the timer task. The timer runs to completion each call. Repeated calls whilst the timer is running
        /// are ignored. If AutoStop mode is active, each call resets the number of timer period done events counted. 
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Start()
        {
            // Preset must be greater than the task timer interval
            if (Preset < microTimer.Interval / 1000) // us to ms
                throw new Exception($"Preset must be greater or equal to {microTimer.Interval / 1000}");

            // Cannot have auto restart and auto stop on at the same time
            if (AutoStop && AutoRestart)
                throw new Exception($"Cannot have AutoStop and AutoRestart set true at the same time");

            // If no off counts are set by the user, raise this error
            if (AutoStop && AutoStop_CountsPreset <= 0)
                throw new Exception($"AutoStopOffCounts must be > 0");

            // If timer is not restarting and auto stop mode is enabled, load / reload the number of autostop counts to
            // go then return.
            if (AutoStop)
            {
                if (!restartLatch)
                {
                    if (Debug && (AutoStop_CountsToGo != AutoStop_CountsPreset))
                    {
                        Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() AutoStop_OffCountsToGo Loaded");
                    }

                    AutoStop_CountsToGo = AutoStop_CountsPreset;
                }

                //if (restartLatch)
                //    return;
            }

            // Return if we are already timing. This bit will be false if we have arrived here from a call to Restart()
            if (TimerTiming)
            {
                if (Debug)
                {
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start() call ignored. Timer already running");
                }

                return;
            }

            // Anticipated setting of the timer timing flag. Bridges the delay between the first start call and the
            // first microTimer tick
            TimerTiming = true;

            // Timer started debug
            if (Debug && !restartLatch)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer started");
            }

            // Clear the restart flag
            restartLatch = false;

            // For debug and ToString override
            stopWatch.Start();

            // Presetting of timer accumulator
            Accumulator = Preset / (int)(microTimer.Interval / 1000); // Preset loads into accumulator. PRE / interval => ms / ticks

            // Start the task timer, make sure this is the last code line in the start function
            if (!microTimer.Enabled)
                microTimer.Start();
        }

        private void Restart()
        {
            // Restart flag to keep taskTimer running and allows for selective conditions in the start function that are
            // required to auto restart the timer
            restartLatch = true;

            Start();

            // Reset the restart latch
            restartLatch = false;

            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer restarted");
            }
        }

        public void Stop()
        {
            if (Debug)
            {
                if (!restartLatch && microTimer.Enabled)
                {

                }
            }

            // If not set to auto restart, nuke the microtimer thread
            if (!restartLatch && microTimer.Enabled)
            {
                if (Debug)
                {
                    if (!AutoStop)
                        Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer stopped");
                    else
                        Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer auto stopped");
                }

                // For debug and ToString override
                stopWatch.Stop();

                // Resets
                TimerDone = false;
                TimerTiming = false;
                Accumulator = 0;
                AutoStop_CountsToGo = 0;

                // This must be the last line in the stop function as it kills the microtimer timer thread which happens
                // to be this thread also!!!
                microTimer.Stop();
                microTimer.Abort();
            }
        }

        // Task timer tick event call back
        private void Tick(object sender, MicroTimerEventArgs timerEventArgs)
        {
            if (Debug) // Selectively uncomment as needed. Generates shit loads of high speed entries
            {
                // Console.WriteLine($"{InstanceName} {DebugName} RunTimerTask()");
            }

            RunTimerTask();
        }

        private void RunTimerTask()
        {
            Accumulator--;      // Decrement accumulator
            TimerTiming = true;

            if (Accumulator <= 0) // Timed out
            {
                Accumulator = 0;
                TimerTiming = false;
                TimerDone = true;

                RaiseEventReq();
            }
        }

        private void RaiseEventReq()
        {
            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer done, event fired");
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}");
            }

            // Raise the timer done event
            if (uiControl != null && uiControl.IsHandleCreated)
            {
                RaiseOnTimingDoneEvent();
            }
            else
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Event fire failed! Control Handle not created");
            }

            // If auto stop mode is enabled, increase the off counts by 1. If the off counts exceeds the set threshold,
            // stop the timer
            if (AutoStop)
            {
                AutoStop_CountsToGo--;

                if (Debug)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - AutoStopOffCountsToGo = {AutoStop_CountsToGo}");

                if (AutoStop_CountsToGo == 0)
                {
                    Stop();
                }
                else
                    Restart();
            }

            // If autorestart is enabled, start the timer again
            if (AutoRestart)
            {
                Restart();
            }
        }

        protected virtual void RaiseOnTimingDoneEvent()
        {
            uiControl.BeginInvoke((MethodInvoker)delegate
            {
                OnTimingDone?.Invoke();
            });
        }

        public override string ToString()
        {
            return $"Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}";
        }
        #endregion
    }
    #endregion

    public class TimerOffDelay
    #region
    {
        #region Properties
        /// <summary>
        /// Timer preset value in milliseconds
        /// </summary>
        public int Preset { get; set; }

        /// <summary>
        /// Timer accumulator value in ms. Decrements as timer is timing down
        /// </summary>
        public int Accumulator { get; private set; }

        /// <summary>
        /// Timer timing. On when timer is timing down                     
        /// </summary>
        public bool TimerTiming { get; private set; }

        /// <summary>
        /// Timer period done. Stays high until start or stop is next called
        /// </summary>
        public bool TimerDone { get; set; }

        /// <summary>
        /// Debug mode. Writes status to console with timer information
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// The name of the class the owns this timer instance
        /// </summary>
        public string DebugInstanceName { get; set; }

        /// <summary>
        /// Meanigful name for the debug console output
        /// </summary>
        public string DebugTimerName { get; set; }

        /// <summary>
        /// Form control reference. Used as a reference for the timer done event to be invoked back to the ui thread
        /// </summary>
        public static Control uiControl { get; set; }
        #endregion

        #region Variables

        private readonly MyStopWatch stopWatch; // for debugging

        private readonly MicroTimer taskTimer;

        public delegate void TimingDoneCallback();

        public TimingDoneCallback OnTimingDone; 
        #endregion

        #region Constructor
        public TimerOffDelay()
        {
            // New MicroTimer and add event handler
            taskTimer = new MicroTimer();
            taskTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(Tick);

            // Stopwatch for debug mode
            stopWatch = new MyStopWatch();

            // Set tick interval. I've tested various values and settled on 2ms resolution gives consistent ticks
            taskTimer.Interval = 2000;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Call to start the timer off delay. Repeated calls whilst the timer is running resets the accumulator to the
        /// preset value. Replicates setting of the Enabled property true then false
        /// </summary>
        public void Start()
        {
            if (Debug && !TimerTiming)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Start()");
            }

            // If timer is timing, reload the accumulator and return
            if (TimerTiming)
            {
                Accumulator = Preset / (int)(taskTimer.Interval / 1000); // Convert preset ms into interval ticks. Interval is in us
                if (Debug)
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Returned. Timer already timing");
                return;
            }

            Initialise();
            Start_Internal();
        }

        // Timer parameters are initialised
        private void Initialise()
        {
            // Initial timer state
            Accumulator = Preset / (int)(taskTimer.Interval / 1000); // Convert preset ms into interval ticks. Interval is in us
            TimerTiming = true; // Timer timing down
            TimerDone = false;

            // Pre must be greater than the task timer interval
            //if (Preset < taskTimer.Interval / 1000) // us to ms
            //    throw new Exception($"Preset must be greater or equal to {taskTimer.Interval / 1000}");

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Initialised");
        }

        // Starts the timer task
        private void Start_Internal()
        {
            // Starts the internal microTimer
            if (!taskTimer.Enabled)
            {
                taskTimer.Start();

                if (Debug)
                {
                    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Start()");
                }

                // For debug and ToString override
                stopWatch.Restart();
            }
        }

        // Aborts the timer task
        public void Stop()
        {
            taskTimer.Abort();

            stopWatch.Stop();

            Accumulator = 0;
            TimerTiming = false;
            TimerDone = false;

            if (Debug)
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Stop()");
        }

        // Task timer tick event call back
        private void Tick(object sender, MicroTimerEventArgs timerEventArgs)
        {
            // Writes the tick interval to the console, generates shit loads of messages
            //if (Debug)
            //    Console.WriteLine($"{InstanceName} {DebugName} Timer tick {stopWatch.Get(true)}");

            RunTimerTask();
        }

        // Main timer task
        private void RunTimerTask()
        {
            // Start timing down from the PRE value by decrementing ACC once each timer tick. 
            //if (Debug)
            //    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Accumulator {Accumulator}");

            Accumulator--;     // Decrement accumulator

            // Timer timed down, reset and raise the timer timed down event
            if (Accumulator <= 0)
            {
                Accumulator = 0;
                TimerTiming = false;
                // TimerDone = true; // Relocated to RaiseEvent()

                //if (Debug)
                //    Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Accumulator {Accumulator}");

                RaiseEventReq();
            }
        }

        // Timer done event invoked back to the ui thread
        private void RaiseEventReq()
        {
            // Stop the task timer
            taskTimer.Stop();

            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - taskTimer.Stop()");
            }

            // Status
            TimerDone = true; // Stays true until Start() or Stop() is called

            if (Debug)
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Timer done, event fired");
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}");
            }

            // Raise the timer done event
            if (uiControl != null && uiControl.IsHandleCreated)
            {
                RaiseOnTimingDoneEvent();
            }
            else
            {
                Console.WriteLine($"{DebugInstanceName} - {DebugTimerName} - Event fire failed! Control Handle not created");
            }
        }

        protected virtual void RaiseOnTimingDoneEvent()
        {
            uiControl.BeginInvoke((MethodInvoker)delegate
            {
                OnTimingDone?.Invoke();
            });
        }

        public override string ToString()
        {
            return $"Preset = {((double)Preset / 1000).ToString("0.000")}s, Actual = {stopWatch.Get()}";
        }
        #endregion
    }
    #endregion
}
#endregion

namespace MicroLibrary
#region
{
    // https://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer
    // Written by: https://www.codeproject.com/Members/ken-loveday
    // MicroTimer: A microsecond and millisecond timer in C# that is used in a similar way to the .NET System.Timers.Timer.

    /// <summary>
    /// MicroStopwatch class
    /// </summary>
    public class MicroStopwatch : System.Diagnostics.Stopwatch
    {
        readonly double _microSecPerTick =
            1000000D / System.Diagnostics.Stopwatch.Frequency;

        public MicroStopwatch()
        {
            if (!System.Diagnostics.Stopwatch.IsHighResolution)
            {
                throw new Exception("On this system the high-resolution " +
                                    "performance counter is not available");
            }
        }

        public long ElapsedMicroseconds
        {
            get
            {
                return (long)(ElapsedTicks * _microSecPerTick);
            }
        }
    }

    /// <summary>
    /// MicroTimer class
    /// </summary>
    public class MicroTimer
    {
        public delegate void MicroTimerElapsedEventHandler(
                             object sender,
                             MicroTimerEventArgs timerEventArgs);
        public event MicroTimerElapsedEventHandler MicroTimerElapsed;

        System.Threading.Thread _threadTimer = null;
        long _ignoreEventIfLateBy = long.MaxValue;
        long _timerIntervalInMicroSec = 0;
        bool _stopTimer = true;

        public MicroTimer()
        {
        }

        public MicroTimer(long timerIntervalInMicroseconds)
        {
            Interval = timerIntervalInMicroseconds;
        }

        public long Interval
        {
            get
            {
                return System.Threading.Interlocked.Read(
                    ref _timerIntervalInMicroSec);
            }
            set
            {
                System.Threading.Interlocked.Exchange(
                    ref _timerIntervalInMicroSec, value);
            }
        }

        public long IgnoreEventIfLateBy
        {
            get
            {
                return System.Threading.Interlocked.Read(
                    ref _ignoreEventIfLateBy);
            }
            set
            {
                System.Threading.Interlocked.Exchange(
                    ref _ignoreEventIfLateBy, value <= 0 ? long.MaxValue : value);
            }
        }

        public bool Enabled
        {
            set
            {
                if (value)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
            get
            {
                return (_threadTimer != null && _threadTimer.IsAlive);
            }
        }

        public void Start()
        {
            if (Enabled || Interval <= 0)
            {
                return;
            }

            _stopTimer = false;

            System.Threading.ThreadStart threadStart = delegate ()
            {
                NotificationTimer(ref _timerIntervalInMicroSec,
                                  ref _ignoreEventIfLateBy,
                                  ref _stopTimer);
            };

            _threadTimer = new System.Threading.Thread(threadStart);
            _threadTimer.Priority = System.Threading.ThreadPriority.Highest;
            _threadTimer.Start();
        }

        public void Stop()
        {
            _stopTimer = true;
        }

        public void StopAndWait()
        {
            StopAndWait(System.Threading.Timeout.Infinite);
        }

        public bool StopAndWait(int timeoutInMilliSec)
        {
            _stopTimer = true;

            if (!Enabled || _threadTimer.ManagedThreadId ==
                System.Threading.Thread.CurrentThread.ManagedThreadId)
            {
                return true;
            }

            return _threadTimer.Join(timeoutInMilliSec);
        }

        public void Abort()
        {
            _stopTimer = true;

            if (Enabled)
            {
                _threadTimer.Abort();
            }
        }

        void NotificationTimer(ref long timerIntervalInMicroSec,
                               ref long ignoreEventIfLateBy,
                               ref bool stopTimer)
        {
            int timerCount = 0;
            long nextNotification = 0;

            MicroStopwatch microStopwatch = new MicroStopwatch();
            microStopwatch.Start();

            while (!stopTimer)
            {
                long callbackFunctionExecutionTime =
                    microStopwatch.ElapsedMicroseconds - nextNotification;

                long timerIntervalInMicroSecCurrent =
                    System.Threading.Interlocked.Read(ref timerIntervalInMicroSec);
                long ignoreEventIfLateByCurrent =
                    System.Threading.Interlocked.Read(ref ignoreEventIfLateBy);

                nextNotification += timerIntervalInMicroSecCurrent;
                timerCount++;
                long elapsedMicroseconds = 0;

                while ((elapsedMicroseconds = microStopwatch.ElapsedMicroseconds)
                        < nextNotification)
                {
                    System.Threading.Thread.SpinWait(10);
                }

                long timerLateBy = elapsedMicroseconds - nextNotification;

                if (timerLateBy >= ignoreEventIfLateByCurrent)
                {
                    continue;
                }

                MicroTimerEventArgs microTimerEventArgs =
                     new MicroTimerEventArgs(timerCount,
                                             elapsedMicroseconds,
                                             timerLateBy,
                                             callbackFunctionExecutionTime);
                MicroTimerElapsed(this, microTimerEventArgs);
            }

            microStopwatch.Stop();
        }
    }

    /// <summary>
    /// MicroTimer Event Argument class
    /// </summary>
    public class MicroTimerEventArgs : EventArgs
    {
        // Simple counter, number times timed event (callback function) executed
        public int TimerCount { get; private set; }

        // Time when timed event was called since timer started
        public long ElapsedMicroseconds { get; private set; }

        // How late the timer was compared to when it should have been called
        public long TimerLateBy { get; private set; }

        // Time it took to execute previous call to callback function (OnTimedEvent)
        public long CallbackFunctionExecutionTime { get; private set; }

        public MicroTimerEventArgs(int timerCount,
                                   long elapsedMicroseconds,
                                   long timerLateBy,
                                   long callbackFunctionExecutionTime)
        {
            TimerCount = timerCount;
            ElapsedMicroseconds = elapsedMicroseconds;
            TimerLateBy = timerLateBy;
            CallbackFunctionExecutionTime = callbackFunctionExecutionTime;
        }
    }
}
#endregion