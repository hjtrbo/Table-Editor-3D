
/*****************************************************************************

This class has been written by Elmü (elmue@gmx.de)

Check if you have the latest version on:
https://www.codeproject.com/Articles/5293980/Graph3D-A-Windows-Forms-Render-Control-in-Csharp

=======================================
 
IMPORTANT:
This class has been written by a software developer with 45 years programming exprience.
This class is optimized for the highest possible speed in every line of it's code.
If you found a fork of this code on Github or elsewhere you do NOT have the original high quality code!
This code with 5000 lines is extremely complex and there is a very high risk that a beginner has 
broken this code by modifying it without properly understanding it.

=======================================
 
NAMING CONVENTIONS which allow to see the type of a variable immediately without having to jump to the variable definition:
 
     cName  for class    definitions
     tName  for type     definitions
     eName  for enum     definitions
     kName  for "konstruct" (struct) definitions (letter 's' already used for string)
   delName  for delegate definitions

    b_Name  for bool
    c_Name  for Char, also Color
    d_Name  for double
    e_Name  for enum variables
    f_Name  for function delegates, also float
    i_Name  for instances of classes
    k_Name  for "konstructs" (struct) (letter 's' already used for string)
	r_Name  for Rectangle
    s_Name  for strings
    o_Name  for objects
 
   s8_Name  for   signed  8 Bit (sbyte)
  s16_Name  for   signed 16 Bit (short)
  s32_Name  for   signed 32 Bit (int)
  s64_Name  for   signed 64 Bit (long)
   u8_Name  for unsigned  8 Bit (byte)
  u16_Name  for unsigned 16 bit (ushort)
  u32_Name  for unsigned 32 Bit (uint)
  u64_Name  for unsigned 64 Bit (ulong)

  An additional "m" is prefixed for all member variables (e.g. ms_String)


*****************************************************************************/

// Print render speed to Trace output
// ATTENTION: The times in the very first trace output are always wrong because of JIT compilation delays.
#if DEBUG
//    #define DEBUG_SPEED
#endif

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Diagnostics;
using TableEditor;

namespace Plot3D
{
    /// <summary>
    /// ATTENTION: This class is not thread safe.
    /// Call all functions only from the GUI thread or use Control.Invoke()
    /// </summary>
    public class Editor3D : UserControl
    {
        #region Added by NP
        public static float SelSizeK    { get { return mf_SelSizeK; } set { mf_SelSizeK = value; } }
        public static int ToolTipRadius { get { return mi_ToolTipRadius; } set { mi_ToolTipRadius = value; } }
        public static int SelectRadius  { get { return mi_SelectRadius; } set { mi_SelectRadius = value; } }
        public static Size InitialSize  { set { initialSize = value; } }
        public bool PointSelectMode { get; set; }
        public bool PointMoveMode   { get; set; }

        // Backing fields with default values
        static float mf_SelSizeK      = 1.0F; // A selection point size factor to allow the user to adjust selection point size. The automatic zoom factor scaling by the graph logic is retained.
        static int   mi_ToolTipRadius = 4;    // Previously was an internal constant. Now exposed to the user
        static int   mi_SelectRadius  = 4;    // Previously was an internal constant. Now exposed to the user
        static Size  initialSize      = Init.Graph3dInitialSize; // For proper implementation write to this property before the control is displayed

        // Returns the nearest point on the graph. Originates from the cToolTip class OnMouseEvent method
        public (double, double, double) NearestPoint()
        {
            cPoint nearestPoint = mi_Tooltip.NearestPoint;

            if (nearestPoint == null)
                return (-1, -1, -1);

            double x = nearestPoint.mi_P3D.X;
            double y = nearestPoint.mi_P3D.Y;
            double z = nearestPoint.mi_P3D.Z;

            return (x, y, z);
        }
        #endregion

        #region enums

        public enum eColorScheme
        {
            HP_Tuners = 0, // np
            Cool,
            Copper,
            Hot,
            Hsv,
            Monochrome,
            Pink,
            Rainbow_Sweep,  // This creates a 100% cyclic rainbow with 1536 colors. The end color is the same as the start color.
            Rainbow_Bright, // This creates a rainbow without magenta with 1024 colors. It goes from blue to red.
            Rainbow_Dark,   // This is similar to RainbowBright, but darker and only with 64 colors.
            Spring,
            Summer,
            Winter,
        }

        public enum eRaster
        {
            Off,      // turn off coordinate system
            MainAxes, // draw only solid main axes for X,Y,Z
            Raster,   // draw additional thin raster lines
            Labels,   // draw additional labels in quadrant 3
        }

        /// <summary>
        /// If a function has an asymetric range for X and Y as demo "Callback" a separate normalization 
        /// would always lead to a square X,Y pane which would be a distortion for the relation between X and Y values.
        /// MaintainXY  guarantees that the relation between X and Y values is maintained.
        /// MaintainXYZ additionally guarantees that the relation between X, Y and Z values is maintained.
        /// </summary>
        public enum eNormalize
        {
            Separate,    // Normalize X,Y,Z separately (use this for discrete values)
            MaintainXY,  // Normalize X,Y   without changing their relation (use this for math functions)
            MaintainXYZ, // Normalize X,Y,Z without changing their relation (use this for math functions)
        }

        public enum eScatterShape
        {
            // 0 is invalid
            Circle   = 1,
            Square   = 2,
            Triangle = 3,
         // Star     = 4,  you can implement your own shapes here
        }

        /// <summary>
        /// Used internally for coordinate system
        /// </summary>
        public enum eCoord
        {
            // for axis in coordinate system
            X = 0,
            Y = 1,
            Z = 2,
            Invalid,
        }

        /// <summary>
        /// These flags define which axes are allowed to be mirrored.
        /// For user objects option "All" is used.
        /// Coordinate system axes and raster lines are mirrored individually.
        /// </summary>
        [FlagsAttribute]
        public enum eMirror
        {
            None = 0,
            X    = 1,
            Y    = 2,
            Z    = 4,
            XY   = X | Y,
            XZ   = X | Z,
            YZ   = Y | Z,
            All  = X | Y | Z,
        }

        /// <summary>
        /// Mouse operations
        /// </summary>
        public enum eMouseAction
        {
            None = 0,
            Move,        // Move             the coordinate system with the mouse
            Theta,       // Elevate          the coordinate system with the mouse
            Phi,         // Rotate           the coordinate system with the mouse
            ThetaAndPhi, // Elevate + Rotate the coordinate system with the mouse
            Rho,         // Zoom in / out
            SelectObj,   // Select a 3D object or call the selection callback function if defined
            Callback,    // Call the selection callback function if defined
        }

        public enum eSelEvent
        {
            MouseDown, // Inform selection callback function that the pre-defined mouse button goes down
            MouseDrag, // Inform selection callback function that the mouse is moved while the pre-defined mouse button is down
            MouseUp,   // Inform selection callback function that the pre-defined mouse button goes up
        }

        public enum ePolygonMode
        {
            Fill,  // Fill polygons with Brush
            Lines, // Draw only polygon border lines
        }

        /// <summary>
        /// This enum defines of which type is a cObject3D
        /// </summary>
        public enum eObjType
        {
            Point,
            Line,
            Shape,
            Polygon,
        }

        /// <summary>
        /// These flags are used to filter the 3D objects that are selected.
        /// </summary>
        [FlagsAttribute]
        public enum eSelType
        {
            Line    = 0x1,
            Shape   = 0x2,
            Polygon = 0x4,
            All     = 0x7,
        }

        [FlagsAttribute]
        public enum eTooltip
        {
            Off      = 0x0, // Tooltip is disabled
            UserText = 0x1, // Show user defined tooltip text that has been set in cPoint3D.Tooltip
            Coord    = 0x2, // Show coordinates X,Y,Z of cPoint3D
            Hover    = 0x4, // np hover point mode
            All      = 0x7, // Show all
        }

        // This enum is to get the maximum speed out of your CPU.
        // Re-calculation is done only if required.
        [FlagsAttribute]
        private enum eRecalculate
        {
            Nothing     = 0x0, // repaint objects after changed selection      --> recalculate nothing
            Objects     = 0x1, // Projection, Brush, LineWidth,... has changed --> recalculate 3D objects
            CoordSystem = 0x2, // The coordinate system must be recalculated   --> recalculate Min/Max and Coord System
            AddRemove   = 0x4, // Draw Objects have been added or removed      --> refresh lists and recalculate all
        }

        /// <summary>
        /// These are the possible return values of the Selection callback. 
        /// See description of SelectionCallback() at the end of this class.
        /// </summary>
        public enum eInvalidate
        {
            NoChange,    // The callback has not modified anything --> do nothing.
            Invalidate,  // Calls Invalidate() to redraw what is required depending on the flags in me_Recalculate.
            CoordSystem, // The coordinate system will be recalculated, then Invalidate() is called.
                         // Use this option after moving a 3D object with the mouse.
        }

        /// <summary>
        /// This defines with which mouse button the user controls rotation and elevation
        /// </summary>
        public enum eMouseCtrl
        {
            L_Theta_R_Phi, // Left   mouse button vertical: Theta, Right  mouse button horizontal: Phi
            L_Theta_L_Phi, // Left   mouse button vertical: Theta, Left   mouse button horizontal: Phi
            M_Theta_M_Phi, // Middle mouse button vertical: Theta, Middle mouse button horizontal: Phi
        }

        /// <summary>
        /// 3D object properties that the user may change are stored in the Undo Buffer
        /// </summary>
        public enum eUndoProp
        {
            // cObject3D
            Tag,
            Selected,
            CanSelect,

            // cPoint3D
            X,
            Y,
            Z,
            Tooltip,

            // cLine3D
            Pen,
            Width,

            // cShape3D
            Shape,
            Radius,

            // cShape3D + cPolygon3D
            Brush,
        }

        public enum eLegendPos
        {
            BottomLeft, // The main axis legends are drawn in the bottom left corner of the graph pane. (recommended)
            AxisEnd,    // The main axis legends are drawn at the end of the axis and rotate with the graph. (not recommended)
        }

        #endregion

        // ================== PUBLIC =================

        #region cObject3D

        /// <summary>
        /// Base class for cPoint3D, cShape3D, cLine3D, cPloygon3D
        /// </summary>
        public abstract class cObject3D
        {
            public    Editor3D   mi_Inst;
            protected Object     mo_Tag;
            protected bool       mb_Selected;
            protected bool       mb_CanSelect = true;
            protected cPoint3D[] mi_Points; // This instance must never be replaced by a new array!

            public virtual void Restore(eUndoProp e_Property, Object o_Value)
            {
                switch (e_Property)
                {
                    case eUndoProp.Tag:       mo_Tag       =       o_Value; break;
                    case eUndoProp.Selected:  mb_Selected  = (bool)o_Value; break;
                    case eUndoProp.CanSelect: mb_CanSelect = (bool)o_Value; break;
                    default: throw new ArgumentException();
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Gets the type of this cObject3D
            /// </summary>
            public virtual eObjType ObjType
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// 1 point  for cPoint3D
            /// 1 point  for cShape3D
            /// 2 points for cLine3D
            /// n points for cPolygon3D
            /// </summary>
            public cPoint3D[] Points
            {
                get { return mi_Points; } 
             // set must NOT be allowed here !!!!
            }

            /// <summary>
            /// Here you can store your private data which is passed in Selection.Callback to your code.
            /// </summary>
            public Object Tag
            {
                get { return mo_Tag;  }
                set 
                { 
                    if (mo_Tag == value) 
                        return;
                    
                    if (mi_Inst != null)
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Tag, mo_Tag, value);
                    
                    mo_Tag = value; 
                }
            }

            /// <summary>
            /// The draw object has been selected by ALT + click
            /// </summary>
            public virtual bool Selected
            {
                get { return mb_Selected;  }
                set 
                { 
                    if (mb_Selected == value)
                        return;

                    if (mi_Inst != null)
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Selected, mb_Selected, value);
                    
                    mb_Selected = value; 
                    // me_Recalculate needs no change here
                }
            }

            /// <summary>
            /// Defines if the user is allowed to select this object
            /// </summary>
            public virtual bool CanSelect
            {
                get { return mb_CanSelect;  }
                set 
                { 
                    if (mb_CanSelect == value)
                        return;

                    if (mi_Inst != null)
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.CanSelect, mb_CanSelect, value);
                    
                    mb_CanSelect = value; 
                }
            }

            /// <summary>
            /// Move the object in the 3D space
            /// </summary>
            public void Move(double d_DeltaX, double d_DeltaY, double d_DeltaZ)
            {
                foreach (cPoint3D i_Point in mi_Points)
                {
                    i_Point.X += d_DeltaX;
                    i_Point.Y += d_DeltaY;
                    i_Point.Z += d_DeltaZ;
                }
            }
        }

        #endregion

        #region cPoint3D

        public class cPoint3D : cObject3D
        {
            private double md_X;
            private double md_Y;
            private double md_Z;
            private String ms_Tooltip; 

            public override void Restore(eUndoProp e_Property, Object o_Value)
            {
                switch (e_Property)
                {
                    case eUndoProp.X:       md_X       = (double)o_Value; break;
                    case eUndoProp.Y:       md_Y       = (double)o_Value; break;
                    case eUndoProp.Z:       md_Z       = (double)o_Value; break;
                    case eUndoProp.Tooltip: ms_Tooltip = (String)o_Value; break;
                    default: base.Restore(e_Property, o_Value); break;
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Gets the type of this cObject3D
            /// </summary>
            public override eObjType ObjType
            {
                get { return eObjType.Point; }
            }

            /// <summary>
            /// 3D coordinate
            /// </summary>
            public double X
            {
                get { return md_X;  }
                set
                { 
                    if (md_X == value)
                        return;

                    if (mi_Inst != null) 
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.X, md_X, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects; 

                        if (value < mi_Inst.mi_Bounds.X.Min || value > mi_Inst.mi_Bounds.X.Max)
                            mi_Inst.me_Recalculate |= eRecalculate.CoordSystem;
                    }
                    md_X = value; 
                }
            }

            /// <summary>
            /// 3D coordinate
            /// </summary>
            public double Y
            {
                get { return md_Y;  }
                set 
                { 
                    if (md_Y == value)
                        return;

                    if (mi_Inst != null) 
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Y, md_Y, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;

                        if (value < mi_Inst.mi_Bounds.Y.Min || value > mi_Inst.mi_Bounds.Y.Max)
                            mi_Inst.me_Recalculate |= eRecalculate.CoordSystem;
                    }
                    md_Y = value; 
                }
            }

            /// <summary>
            /// 3D coordinate
            /// </summary>
            public double Z
            {
                get { return md_Z;  }
                set 
                { 
                    if (md_Z == value)
                        return;

                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Z, md_Z, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;

                        if (value < mi_Inst.mi_Bounds.Z.Min || value > mi_Inst.mi_Bounds.Z.Max)
                            mi_Inst.me_Recalculate |= eRecalculate.CoordSystem;
                    }
                    md_Z = value; 
                }
            }

            /// <summary>
            /// Optional tooltip text to be displayed when the mouse is over this point
            /// </summary>
            public String Tooltip
            {
                get { return ms_Tooltip;  }
                set 
                { 
                    if (ms_Tooltip == value)
                        return;

                    if (mi_Inst != null)
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Tooltip, ms_Tooltip, value);

                    ms_Tooltip = value; 
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Constructor
            /// s_ToolTip is displayed when eTooltip.UserText is enabled
            /// In o_Tag you can store any data that you need when the Selection callback is called after the user has selected a point. 
            /// </summary>
            public cPoint3D(double d_X, double d_Y, double d_Z, String s_ToolTip = null, Object o_Tag = null)
            {
                md_X      = d_X;
                md_Y      = d_Y;
                md_Z      = d_Z;
                mo_Tag    = o_Tag;
                mi_Points = new cPoint3D[] { this };

                if (s_ToolTip != null)
                    ms_Tooltip = s_ToolTip.Trim();
            }

            // =================== used for coordinate system ===================

            public cPoint3D Clone()
            {
                return new cPoint3D(md_X, md_Y, md_Z, ms_Tooltip, Tag);
            }

            public bool CoordEquals(cPoint3D i_Point)
            {
                return md_X == i_Point.md_X && md_Y == i_Point.md_Y && md_Z == i_Point.md_Z;
            }

            public double GetValue(eCoord e_Coord)
            {
                switch (e_Coord)
                {
                    case eCoord.X: return md_X;
                    case eCoord.Y: return md_Y;
                    case eCoord.Z: return md_Z;
                    default:       return 0;
                }
            }

            public void SetValue(eCoord e_Coord, double d_Value)
            {
                switch (e_Coord)
                {
                    case eCoord.X: X = d_Value; break;
                    case eCoord.Y: Y = d_Value; break;
                    case eCoord.Z: Z = d_Value; break;
                }
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return String.Format("cPoint3D (X={0}, Y={1}, Z={2})", FormatDouble(md_X), FormatDouble(md_Y), FormatDouble(md_Z));
            }
        }

        #endregion

        #region cLine3D

        public class cLine3D : cObject3D
        {
            private Pen mi_Pen;
            private int ms32_Width;
            private int ms32_Parts;

            public override void Restore(eUndoProp e_Property, Object o_Value)
            {
                switch (e_Property)
                {
                    case eUndoProp.Pen:   mi_Pen     = (Pen)o_Value; break;
                    case eUndoProp.Width: ms32_Width = (int)o_Value; break;
                    default: base.Restore(e_Property, o_Value); break;
                    // ms32_Parts is constant
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Gets the type of this cObject3D
            /// </summary>
            public override eObjType ObjType
            {
                get { return eObjType.Line; }
            }

            /// <summary>
            /// The line width in pixels
            /// </summary>
            public int Width
            {
                get { return ms32_Width;  }
                set 
                { 
                    if (ms32_Width == value)
                        return;

                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Width, ms32_Width, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    ms32_Width = value; 
                }
            }

            /// <summary>
            /// If Pen is null, a Pen from the ColorScheme will be used.
            /// The width of the Pen does not matter. It will be set to the property Width.
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Pen assigned here.
            /// If you externally change for example Pen.Color the Undo will not work correctly.
            /// Always assign a different instance of Pen to avoid this.
            /// </summary>
            public Pen Pen
            {
                get { return mi_Pen;  }
                set 
                { 
                    if (mi_Pen == value)
                        return;

                    if (mi_Inst != null) 
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Pen, mi_Pen, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    mi_Pen = value; 
                }
            }

            /// <summary>
            /// Parts of different color in multi-color lines
            /// </summary>
            public int ColorParts
            {
                get { return ms32_Parts; }
            }

            /// <summary>
            /// Constructor
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Pen assigned here.
            /// If you externally change for example Pen.Color the Undo will not work correctly.
            /// Always assign a different instance of Pen to cLine3D.Pen to avoid this.
            /// </summary>
            public cLine3D(cPoint3D i_Start, cPoint3D i_End, int s32_Width, Pen i_Pen, int s32_Parts, Object o_Tag = null)
            {
                mi_Points  = new cPoint3D[] { i_Start, i_End };
                ms32_Width = s32_Width;
                mi_Pen     = i_Pen;
                ms32_Parts = s32_Parts;
                mo_Tag     = o_Tag;
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return "cLine3D from " + mi_Points[0] + " to " + mi_Points[1];
            }
        }

        #endregion

        #region cShape3D

        public class cShape3D : cObject3D
        {
            private eScatterShape me_Shape;
            private int           ms32_Radius;
            private Brush         mi_Brush;

            public override void Restore(eUndoProp e_Property, Object o_Value)
            {
                switch (e_Property)
                {
                    case eUndoProp.Shape:  me_Shape    = (eScatterShape)o_Value; break;
                    case eUndoProp.Radius: ms32_Radius = (int)          o_Value; break;
                    case eUndoProp.Brush:  mi_Brush    = (Brush)        o_Value; break;
                    default: base.Restore(e_Property, o_Value); break;
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Gets the type of this cObject3D
            /// </summary>
            public override eObjType ObjType
            {
                get { return eObjType.Shape; }
            }

            /// <summary>
            /// The type of shape to be painted
            /// </summary>
            public eScatterShape Shape
            {
                get { return me_Shape;  }
                set 
                { 
                    if (me_Shape == value)
                        return;

                    // Circle and rectangle are drawn by the framework --> no recalculation required.
                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Shape, me_Shape, value);

                        if (value != eScatterShape.Circle && value != eScatterShape.Square) 
                            mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    me_Shape = value; 
                }
            }

            /// <summary>
            /// The radius of the circle, square or triangle
            /// </summary>
            public int Radius
            {
                get { return ms32_Radius;  }
                set 
                { 
                    if (ms32_Radius == value)
                        return;

                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Radius, ms32_Radius, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    ms32_Radius = value; 
                }
            }

            /// <summary>
            /// The color of the Shape or null to use a color from the ColorScheme
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Brush assigned here.
            /// If you externally change for example SolidBrush.Color the Undo will not work correctly.
            /// Always assign a different instance of Brush to avoid this.
            /// </summary>
            public Brush Brush
            {
                get { return mi_Brush;  }
                set 
                { 
                    if (mi_Brush == value)
                        return;

                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Brush, mi_Brush, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    mi_Brush = value; 
                }
            }

            /// <summary>
            /// The shape is selected
            /// </summary>
            public override bool Selected
            {
                get { return mi_Points[0].Selected;  }
                set { mi_Points[0].Selected = value; } // me_Recalculate needs no change
            }

            /// <summary>
            /// Defines if the user is allowed to select this shape
            /// </summary>
            public override bool CanSelect
            {
                get { return mi_Points[0].CanSelect;  }
                set { mi_Points[0].CanSelect = value; }
            }

            /// <summary>
            /// Constructor
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Brush assigned here.
            /// If you externally change for example SolidBrush.Color the Undo will not work correctly.
            /// Always assign a different instance of Brush to cShape3D.Brush to avoid this.
            /// </summary>
            public cShape3D(cPoint3D i_Point, eScatterShape e_Shape, int s32_Radius, Brush i_Brush, Object o_Tag = null)
            {
                i_Point.Tag  = o_Tag;
                mi_Points    = new cPoint3D[] { i_Point };
                me_Shape     = e_Shape;
                ms32_Radius  = s32_Radius;
                mi_Brush     = i_Brush;
                mo_Tag       = o_Tag;
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return "cShape3D " + me_Shape + " at " + mi_Points[0];
            }
        }

        #endregion

        #region cPolygon3D

        public class cPolygon3D : cObject3D
        {
            private Brush mi_Brush;
            private int   ms32_Col;
            private int   ms32_Row;

            public override void Restore(eUndoProp e_Property, Object o_Value)
            {
                switch (e_Property)
                {
                    case eUndoProp.Brush: mi_Brush = (Brush)o_Value; break;
                    default: base.Restore(e_Property, o_Value);      break;
                    // ms32_Col is constant
                    // ms32_Row is constant
                }
            }

            // --------------------------------------------------

            /// <summary>
            /// Gets the type of this cObject3D
            /// </summary>
            public override eObjType ObjType
            {
                get { return eObjType.Polygon; }
            }

            /// <summary>
            /// The color of the Polygon or null to use a color from the ColorScheme
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Brush assigned here.
            /// If you externally change for example SolidBrush.Color the Undo will not work correctly.
            /// Always assign a different instance of Brush to avoid this.
            /// </summary>
            public Brush Brush
            {
                get { return mi_Brush;  }
                set 
                { 
                    if (mi_Brush == value)
                        return;

                    if (mi_Inst != null)
                    {
                        mi_Inst.mi_UndoBuffer.Backup(this, eUndoProp.Brush, mi_Brush, value);
                        mi_Inst.me_Recalculate |= eRecalculate.Objects;
                    }
                    mi_Brush = value; 
                }
            }

            /// <summary>
            /// The column in the grid.
            /// This is only valid for Polygons created by cSurfaceData
            /// </summary>
            public int Column
            {
                get { return ms32_Col; } // the grid column cannot be changed
            }

            /// <summary>
            /// The row in the grid.
            /// This is only valid for Polygons created by cSurfaceData
            /// </summary>
            public int Row
            {
                get { return ms32_Row; } // the grid row cannot be changed
            }

            /// <summary>
            /// Constructor
            /// IMPORTANT:
            /// If you use the UndoBuffer you must never modify any property of the Brush assigned here.
            /// If you externally change for example SolidBrush.Color the Undo will not work correctly.
            /// Always assign a different instance of Brush to cPolygon3D.Brush to avoid this.
            /// </summary>
            public cPolygon3D(int s32_Col, int s32_Row, Brush i_Brush, params cPoint3D[] i_Points)
            {
                if (i_Points.Length < 3)
                    throw new ArgumentException("At least 3 points are required to draw a polygon.");

                mi_Points = i_Points;
                mi_Brush  = i_Brush;
                ms32_Col  = s32_Col;
                ms32_Row  = s32_Row;
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return "cPolygon3D (" + mi_Points.Length + " points)";
            }
        }

        #endregion

        // ---------------------

        #region cAxis

        /// <summary>
        /// Used for the main axes and raster lines of the coordinate system
        /// </summary>
        [Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class cAxis
        {
            private Editor3D   mi_Inst;
            private eCoord     me_Coord;
            private Pen        mi_RasterPen;   // Raster lines (brigther)
            private Pen        mi_AxisPen;     // Main coordinate axis (darker)
            private SolidBrush mi_LegendBrush; // Label text
            private String     ms_LegendText;
            private bool       mb_Mirror;
            private bool       mb_IncludeZero;

            public cAxis(Editor3D i_Inst, eCoord e_Coord, Color c_Color)
            {
                mi_Inst  = i_Inst;
                me_Coord = e_Coord;
                Color    = c_Color;
                Reset();
            }

            public void Reset()
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread
                ms_LegendText  = null;
                mb_Mirror      = false;
                mb_IncludeZero = me_Coord == eCoord.Z;
            }

            /// <summary>
            /// The color of the axis lines and the legend. 
            /// This change will become visible the next time you call Invalidate()
            /// Not modified by Reset()
            /// </summary>
            public Color Color
            {
                get { return mi_AxisPen.Color; }
                set
                {
                    Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread
                    mi_LegendBrush = new SolidBrush(value);
                    mi_AxisPen     = new Pen(value, 3);
                    mi_RasterPen   = new Pen(BrightenColor(value), 1);
                    mi_Inst.me_Recalculate |= eRecalculate.CoordSystem;
                }
            }

            /// <summary>
            /// Internally used to draw raster lines (brighter)
            /// </summary>
            [Browsable(false)]
            public Pen RasterPen
            {
                get { return mi_RasterPen; }
            }

            /// <summary>
            /// Internally used to draw main coordinates (darker)
            /// </summary>
            [Browsable(false)]
            public Pen AxisPen
            {
                get { return mi_AxisPen; }
            }

            /// <summary>
            /// Internally used for label text
            /// </summary>
            [Browsable(false)]
            public SolidBrush LegendBrush
            {
                get { return mi_LegendBrush; }
            }

            /// <summary>
            /// Here you can add an optional legend which is displayed for the axis.
            /// With the property Editor3D.LegendPos you can define where the legend is drawn.
            /// If the string is null or empty, no legend will be displayed for this axis.
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public String LegendText
            {
                get { return ms_LegendText;  }
                set { ms_LegendText = value; }
            }

            /// <summary>
            /// In the default rotation angle the axis values are normally increasing 
            /// from right to left (X), back to front (Y) and bottom to top (Z).
            /// If Mirror = true they will decrease instead of increase.
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public bool Mirror
            {
                get { return mb_Mirror;  }
                set
                {
                    if (mb_Mirror == value)
                        return;

                    if (me_Coord == eCoord.Z)
                        throw new NotImplementedException("Mirroring the Z axis is not implemented because there are multiple issues and it does not make sense to draw the Z values reverse.");

                    Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread
                    mb_Mirror = value; 
                    mi_Inst.me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }

            /// <summary>
            /// Example: If the axis has values from 4 to 10 and
            /// IncludeZero = false --> the coordinate axis has a range from 4 to 10
            /// IncludeZero = true  --> the coordinate axis has a range from 0 to 10 (default)
            /// ATTENTION: This setting is ignored if the coordinate system is not drawn (eRaster.Off)
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public bool IncludeZero
            {
                get { return mb_IncludeZero;  }
                set
                {
                    if (mb_IncludeZero == value)
                        return;

                    Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread
                    mb_IncludeZero = value; 
                    mi_Inst.me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }
        }

        #endregion

        #region cSelection

        /// <summary>
        /// This class controls the user selection of points, lines, shapes and polygons
        /// </summary>
        [Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class cSelection
        {
            private Editor3D          mi_Inst;
            private bool              mb_Enabled;
            private bool              mb_MultiSel;
            private bool              mb_SinglePoints;
            private delSelectHandler  mf_Callback;
            private Color             mc_HighlightColor = Color.Empty;
            private Brush             mi_HighlightBrush;
            private Pen               mi_HighlightPen;

            public cSelection(Editor3D i_Inst)
            {
                mi_Inst = i_Inst;
            }

            /// <summary>
            /// This property defines if the user is allowed to select 3D objects.
            /// The selection will only be visible if HighlightColor has also been set.
            /// The callback  will only be called  if Callback has also been assigned.
            /// </summary>
            public bool Enabled
            {
                get { return mb_Enabled;  }
                set { mb_Enabled = value; }
            }

            /// <summary>
            /// This defines the color with which selected draw objects / points are painted.
            /// If you pass Color.Empty draw objects will not be highlighted although they are selected!
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public Color HighlightColor
            {
                set
                {
                    if (value.A > 0)
                    {
                        mi_HighlightBrush = new SolidBrush(value);
                        mi_HighlightPen   = new Pen       (value);
                        mi_HighlightPen.StartCap = LineCap.Round;
                        mi_HighlightPen.EndCap   = LineCap.Round;
                    }
                    else
                    {
                        mi_HighlightBrush = null;
                        mi_HighlightPen   = null;
                    }
                    mc_HighlightColor = value;
                }
                get
                {
                    return mc_HighlightColor;
                }
            }

            [Browsable(false)]
            public Pen HighlightPen
            {
                get 
                { 
                    if (mb_Enabled) return mi_HighlightPen;
                    else            return null;
                }
            }

            [Browsable(false)]
            public Brush HighlightBrush
            {
                get 
                { 
                    if (mb_Enabled) return mi_HighlightBrush; 
                    else            return null;
                }
            }

            /// <summary>
            /// true  --> allow selection of multiple 3D objects at once
            /// false --> allow only selection of a single 3D object at a time
            /// This setting will be ignored when a Callback is assigned.
            /// The callback is responsible for any selection changes!
            /// </summary>
            public bool MultiSelect
            {
                get { return mb_MultiSel;  }
                set { mb_MultiSel = value; }
            }

            /// <summary>
            /// Enables selection of single points.
            /// true  --> the user can only select single points of a polygon or the end points of a line.
            /// false --> the user can only select an entire polygon or an entire line.
            /// For scatter shapes this setting does not matter because a scatter shape corresponds to one point.
            /// IMPORTANT: Selecting entire polygons makes only sense if Fill mode is used. Otherwise polygons are transparent 
            /// and a click would hit the background behind the polygon, so this setting is ignored for polygons in Line mode.
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public bool SinglePoints
            {
                get { return mb_SinglePoints;  }
                set 
                { 
                    mb_SinglePoints = value; 

                    // A mix of selected lines or polygons and selected points is possible but the user will be confused.
                    DeSelectAll();
                }
            }

            /// <summary>
            /// IMPORTANT: Read the detailed comment of function SelectionCallback() at the end of this class.
            /// You can set null here to turn off the callback.
            /// </summary>
            [Browsable(false)]
            public delSelectHandler Callback
            {
                set { mf_Callback = value; }
                get { return mf_Callback;  }
            }

            /// <summary>
            /// Returns selected cLine3D, cShape3D or cPoygon3D objects.
            /// Multiple enums can be combined. Example: GetSelectedObjects(eSelType.Line | eSelType.Shape)
            /// NOTE: For Shape3D the selection of the shape itself and it's point is always the same.
            /// </summary>
            public cObject3D[] GetSelectedObjects(eSelType e_SelType)
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread

                List<cObject3D> i_List = new List<cObject3D>();
                foreach (cDrawObj i_Object in mi_Inst.mi_UserObjects)
                {
                    if (!i_Object.Selected || (i_Object.SelType & e_SelType) == 0)
                        continue;
                    
                    i_List.Add(i_Object.mi_Object3D);
                }
                return i_List.ToArray();
            }

            /// <summary>
            /// Gets the points that the user may select individually if Selection.SinglePoints = true
            /// and gets all points of a Line3D or Polygon3D if it is selected.
            /// Multiple enums can be combined. Example: GetSelectedPoints(eSelType.Line | eSelType.Shape)
            /// NOTE: The returned array contains only unique points.
            /// NOTE: For Shape3D the selection of the shape itself and it's point is always the same.
            /// </summary>
            public cPoint3D[] GetSelectedPoints(eSelType e_SelType)
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread

                List<cPoint3D> i_Unique = new List<cPoint3D>();

                foreach (cDrawObj i_Object in mi_Inst.mi_UserObjects)
                {
                    if ((i_Object.SelType & e_SelType) == 0)
                        continue;

                    // If the object itself is selected return all it's points, no matter if the points are selected or not.
                    if (i_Object.Selected)
                    {
                        foreach (cPoint i_Point in i_Object.mi_Points)
                        {
                            if (!i_Unique.Contains(i_Point.mi_P3D))
                                 i_Unique.Add     (i_Point.mi_P3D);
                        }
                    }
                    else // Object not selected --> check if single points of the object are selected
                    {
                        foreach (cPoint i_Point in i_Object.mi_Points)
                        {
                            if (i_Point.mi_P3D.Selected && 
                               !i_Unique.Contains(i_Point.mi_P3D))
                                i_Unique.Add     (i_Point.mi_P3D);
                        }
                    }
                }
                return i_Unique.ToArray();
            }

            /// <summary>
            /// Remove the selection from all draw objects
            /// This change will become visible the next time you call Invalidate()
            /// </summary>
            public void DeSelectAll()
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread

                foreach (cDrawObj i_Obj in mi_Inst.mi_UserObjects)
                {
                    i_Obj.Selected = false;

                    foreach (cPoint i_Point in i_Obj.mi_Points)
                    {
                        i_Point.mi_P3D.Selected = false;
                    }
                }

                mi_Inst.mi_UndoBuffer.Store();
            }
        }

        #endregion

        #region cUndoBuffer

        /// <summary>
        /// This public base class is exposed to the user.
        /// The implementation is in cUndoImpl.
        /// </summary>
        [Serializable]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public abstract class cUndoBuffer
        {
            /// <summary>
            /// Enable the Undo Buffer only if you need the Undo functionality.
            /// By default Undo / Redo is disabled.
            /// </summary>
            public virtual bool Enabled
            {
                get;
                set;
            }

            /// <summary>
            /// Clears the Undo Buffer.
            /// </summary>
            public virtual void Clear()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Undo the last user operation.
            /// return false if Undo is not available.
            /// This is also called when hitting CTRL + Z while the 3D Editor has the keyboard focus.
            /// </summary>
            public virtual bool Undo()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Redo the last user operation.
            /// return false if Redo is not available.
            /// This is also called when hitting CTRL + Y while the 3D Editor has the keyboard focus.
            /// </summary>
            public virtual bool Redo()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region cColorScheme

        /// <summary>
        /// Pens and Brushes are GDI+ objects which must not be created on the fly in Draw()
        /// For speed optimization these are created only once and stored in this class.
        /// </summary>
        public class cColorScheme
        {
            // ========================= STATIC ===========================

            public static Color[] GetSchema(eColorScheme e_Scheme)
            {
                Byte[,] u8_RGB;            
                switch (e_Scheme)
                {
                    case eColorScheme.Rainbow_Sweep:  return CalcRainbow(6); // all colors, also magenta
                    case eColorScheme.Rainbow_Bright: return CalcRainbow(4); // from red to blue, no magenta
                    case eColorScheme.Monochrome:     return new Color[] { Color.Goldenrod };
                    case eColorScheme.HP_Tuners:    u8_RGB = new byte[,] { { 0, 255, 0 }, { 6, 255, 0 }, { 12, 255, 0 }, { 18, 255, 0 }, { 24, 255, 0 }, { 30, 255, 0 }, { 36, 255, 0 }, { 42, 255, 0 }, { 48, 255, 0 }, { 54, 255, 0 }, { 60, 255, 0 }, { 66, 255, 0 }, { 72, 255, 0 }, { 78, 255, 0 }, { 84, 255, 0 }, { 91, 255, 0 }, { 97, 255, 0 }, { 103, 255, 0 }, { 109, 255, 0 }, { 115, 255, 0 }, { 121, 255, 0 }, { 127, 255, 0 }, { 133, 255, 0 }, { 139, 255, 0 }, { 145, 255, 0 }, { 151, 255, 0 }, { 157, 255, 0 }, { 163, 255, 0 }, { 169, 255, 0 }, { 176, 255, 0 }, { 182, 255, 0 }, { 188, 255, 0 }, { 194, 255, 0 }, { 200, 255, 0 }, { 206, 255, 0 }, { 212, 255, 0 }, { 218, 255, 0 }, { 224, 255, 0 }, { 230, 255, 0 }, { 236, 255, 0 }, { 242, 255, 0 }, { 248, 255, 0 }, { 255, 255, 0 }, { 255, 250, 0 }, { 255, 246, 0 }, { 255, 242, 0 }, { 255, 237, 0 }, { 255, 233, 0 }, { 255, 229, 0 }, { 255, 225, 0 }, { 255, 220, 0 }, { 255, 216, 0 }, { 255, 212, 0 }, { 255, 207, 0 }, { 255, 203, 0 }, { 255, 199, 0 }, { 255, 195, 0 }, { 255, 190, 0 }, { 255, 186, 0 }, { 255, 182, 0 }, { 255, 177, 0 }, { 255, 173, 0 }, { 255, 169, 0 }, { 255, 165, 0 } }; break; // np
                    case eColorScheme.Cool:         u8_RGB = new byte[,] { { 0, 255, 255 }, { 4, 251, 255 }, { 8, 247, 255 }, { 12, 243, 255 }, { 16, 239, 255 }, { 20, 235, 255 }, { 24, 231, 255 }, { 28, 227, 255 }, { 32, 223, 255 }, { 36, 219, 255 }, { 40, 215, 255 }, { 45, 210, 255 }, { 49, 206, 255 }, { 53, 202, 255 }, { 57, 198, 255 }, { 61, 194, 255 }, { 65, 190, 255 }, { 69, 186, 255 }, { 73, 182, 255 }, { 77, 178, 255 }, { 81, 174, 255 }, { 85, 170, 255 }, { 89, 166, 255 }, { 93, 162, 255 }, { 97, 158, 255 }, { 101, 154, 255 }, { 105, 150, 255 }, { 109, 146, 255 }, { 113, 142, 255 }, { 117, 138, 255 }, { 121, 134, 255 }, { 125, 130, 255 }, { 130, 125, 255 }, { 134, 121, 255 }, { 138, 117, 255 }, { 142, 113, 255 }, { 146, 109, 255 }, { 150, 105, 255 }, { 154, 101, 255 }, { 158, 97, 255 }, { 162, 93, 255 }, { 166, 89, 255 }, { 170, 85, 255 }, { 174, 81, 255 }, { 178, 77, 255 }, { 182, 73, 255 }, { 186, 69, 255 }, { 190, 65, 255 }, { 194, 61, 255 }, { 198, 57, 255 }, { 202, 53, 255 }, { 206, 49, 255 }, { 210, 45, 255 }, { 215, 40, 255 }, { 219, 36, 255 }, { 223, 32, 255 }, { 227, 28, 255 }, { 231, 24, 255 }, { 235, 20, 255 }, { 239, 16, 255 }, { 243, 12, 255 }, { 247, 8, 255 }, { 251, 4, 255 }, { 255, 0, 255 } }; break;
                    case eColorScheme.Copper:       u8_RGB = new byte[,] { { 0, 0, 0 }, { 5, 3, 2 }, { 10, 6, 4 }, { 15, 9, 6 }, { 20, 13, 8 }, { 25, 16, 10 }, { 30, 19, 12 }, { 35, 22, 14 }, { 40, 25, 16 }, { 46, 28, 18 }, { 51, 32, 20 }, { 56, 35, 22 }, { 61, 38, 24 }, { 66, 41, 26 }, { 71, 44, 28 }, { 76, 47, 30 }, { 81, 51, 32 }, { 86, 54, 34 }, { 91, 57, 36 }, { 96, 60, 38 }, { 101, 63, 40 }, { 106, 66, 42 }, { 111, 70, 44 }, { 116, 73, 46 }, { 121, 76, 48 }, { 126, 79, 50 }, { 132, 82, 52 }, { 137, 85, 54 }, { 142, 89, 56 }, { 147, 92, 58 }, { 152, 95, 60 }, { 157, 98, 62 }, { 162, 101, 64 }, { 167, 104, 66 }, { 172, 108, 68 }, { 177, 111, 70 }, { 182, 114, 72 }, { 187, 117, 75 }, { 192, 120, 77 }, { 197, 123, 79 }, { 202, 126, 81 }, { 207, 130, 83 }, { 212, 133, 85 }, { 218, 136, 87 }, { 223, 139, 89 }, { 228, 142, 91 }, { 233, 145, 93 }, { 238, 149, 95 }, { 243, 152, 97 }, { 248, 155, 99 }, { 253, 158, 101 }, { 255, 161, 103 }, { 255, 164, 105 }, { 255, 168, 107 }, { 255, 171, 109 }, { 255, 174, 111 }, { 255, 177, 113 }, { 255, 180, 115 }, { 255, 183, 117 }, { 255, 187, 119 }, { 255, 190, 121 }, { 255, 193, 123 }, { 255, 196, 125 }, { 255, 199, 127 } }; break;
                    case eColorScheme.Hot:          u8_RGB = new byte[,] { { 11, 0, 0 }, { 21, 0, 0 }, { 32, 0, 0 }, { 43, 0, 0 }, { 53, 0, 0 }, { 64, 0, 0 }, { 74, 0, 0 }, { 85, 0, 0 }, { 96, 0, 0 }, { 106, 0, 0 }, { 117, 0, 0 }, { 128, 0, 0 }, { 138, 0, 0 }, { 149, 0, 0 }, { 159, 0, 0 }, { 170, 0, 0 }, { 181, 0, 0 }, { 191, 0, 0 }, { 202, 0, 0 }, { 213, 0, 0 }, { 223, 0, 0 }, { 234, 0, 0 }, { 244, 0, 0 }, { 255, 0, 0 }, { 255, 11, 0 }, { 255, 21, 0 }, { 255, 32, 0 }, { 255, 43, 0 }, { 255, 53, 0 }, { 255, 64, 0 }, { 255, 74, 0 }, { 255, 85, 0 }, { 255, 96, 0 }, { 255, 106, 0 }, { 255, 117, 0 }, { 255, 128, 0 }, { 255, 138, 0 }, { 255, 149, 0 }, { 255, 159, 0 }, { 255, 170, 0 }, { 255, 181, 0 }, { 255, 191, 0 }, { 255, 202, 0 }, { 255, 213, 0 }, { 255, 223, 0 }, { 255, 234, 0 }, { 255, 244, 0 }, { 255, 255, 0 }, { 255, 255, 16 }, { 255, 255, 32 }, { 255, 255, 48 }, { 255, 255, 64 }, { 255, 255, 80 }, { 255, 255, 96 }, { 255, 255, 112 }, { 255, 255, 128 }, { 255, 255, 143 }, { 255, 255, 159 }, { 255, 255, 175 }, { 255, 255, 191 }, { 255, 255, 207 }, { 255, 255, 223 }, { 255, 255, 239 }, { 255, 255, 255 } }; break;
                    case eColorScheme.Hsv:          u8_RGB = new byte[,] { { 255, 0, 0 }, { 255, 24, 0 }, { 255, 48, 0 }, { 255, 72, 0 }, { 255, 96, 0 }, { 255, 120, 0 }, { 255, 143, 0 }, { 255, 167, 0 }, { 255, 191, 0 }, { 255, 215, 0 }, { 255, 239, 0 }, { 247, 255, 0 }, { 223, 255, 0 }, { 199, 255, 0 }, { 175, 255, 0 }, { 151, 255, 0 }, { 128, 255, 0 }, { 104, 255, 0 }, { 80, 255, 0 }, { 56, 255, 0 }, { 32, 255, 0 }, { 8, 255, 0 }, { 0, 255, 16 }, { 0, 255, 40 }, { 0, 255, 64 }, { 0, 255, 88 }, { 0, 255, 112 }, { 0, 255, 135 }, { 0, 255, 159 }, { 0, 255, 183 }, { 0, 255, 207 }, { 0, 255, 231 }, { 0, 255, 255 }, { 0, 231, 255 }, { 0, 207, 255 }, { 0, 183, 255 }, { 0, 159, 255 }, { 0, 135, 255 }, { 0, 112, 255 }, { 0, 88, 255 }, { 0, 64, 255 }, { 0, 40, 255 }, { 0, 16, 255 }, { 8, 0, 255 }, { 32, 0, 255 }, { 56, 0, 255 }, { 80, 0, 255 }, { 104, 0, 255 }, { 128, 0, 255 }, { 151, 0, 255 }, { 175, 0, 255 }, { 199, 0, 255 }, { 223, 0, 255 }, { 247, 0, 255 }, { 255, 0, 239 }, { 255, 0, 215 }, { 255, 0, 191 }, { 255, 0, 167 }, { 255, 0, 143 }, { 255, 0, 120 }, { 255, 0, 96 }, { 255, 0, 72 }, { 255, 0, 48 }, { 255, 0, 24 } }; break;
                    case eColorScheme.Rainbow_Dark: u8_RGB = new byte[,] { { 0, 0, 143 }, { 0, 0, 159 }, { 0, 0, 175 }, { 0, 0, 191 }, { 0, 0, 207 }, { 0, 0, 223 }, { 0, 0, 239 }, { 0, 0, 255 }, { 0, 16, 255 }, { 0, 32, 255 }, { 0, 48, 255 }, { 0, 64, 255 }, { 0, 80, 255 }, { 0, 96, 255 }, { 0, 112, 255 }, { 0, 128, 255 }, { 0, 143, 255 }, { 0, 159, 255 }, { 0, 175, 255 }, { 0, 191, 255 }, { 0, 207, 255 }, { 0, 223, 255 }, { 0, 239, 255 }, { 0, 255, 255 }, { 16, 255, 239 }, { 32, 255, 223 }, { 48, 255, 207 }, { 64, 255, 191 }, { 80, 255, 175 }, { 96, 255, 159 }, { 112, 255, 143 }, { 128, 255, 128 }, { 143, 255, 112 }, { 159, 255, 96 }, { 175, 255, 80 }, { 191, 255, 64 }, { 207, 255, 48 }, { 223, 255, 32 }, { 239, 255, 16 }, { 255, 255, 0 }, { 255, 239, 0 }, { 255, 223, 0 }, { 255, 207, 0 }, { 255, 191, 0 }, { 255, 175, 0 }, { 255, 159, 0 }, { 255, 143, 0 }, { 255, 128, 0 }, { 255, 112, 0 }, { 255, 96, 0 }, { 255, 80, 0 }, { 255, 64, 0 }, { 255, 48, 0 }, { 255, 32, 0 }, { 255, 16, 0 }, { 255, 0, 0 }, { 239, 0, 0 }, { 223, 0, 0 }, { 207, 0, 0 }, { 191, 0, 0 }, { 175, 0, 0 }, { 159, 0, 0 }, { 143, 0, 0 }, { 128, 0, 0 } }; break;
                    case eColorScheme.Pink:         u8_RGB = new byte[,] { { 30, 0, 0 }, { 50, 26, 26 }, { 64, 37, 37 }, { 75, 45, 45 }, { 85, 52, 52 }, { 94, 59, 59 }, { 102, 64, 64 }, { 110, 69, 69 }, { 117, 74, 74 }, { 123, 79, 79 }, { 130, 83, 83 }, { 136, 87, 87 }, { 141, 91, 91 }, { 147, 95, 95 }, { 152, 98, 98 }, { 157, 102, 102 }, { 162, 105, 105 }, { 167, 108, 108 }, { 172, 111, 111 }, { 176, 114, 114 }, { 181, 117, 117 }, { 185, 120, 120 }, { 189, 123, 123 }, { 194, 126, 126 }, { 195, 132, 129 }, { 197, 138, 131 }, { 199, 144, 134 }, { 201, 149, 136 }, { 202, 154, 139 }, { 204, 159, 141 }, { 206, 164, 144 }, { 207, 169, 146 }, { 209, 174, 148 }, { 211, 178, 151 }, { 212, 183, 153 }, { 214, 187, 155 }, { 216, 191, 157 }, { 217, 195, 160 }, { 219, 199, 162 }, { 220, 203, 164 }, { 222, 207, 166 }, { 223, 211, 168 }, { 225, 215, 170 }, { 226, 218, 172 }, { 228, 222, 174 }, { 229, 225, 176 }, { 231, 229, 178 }, { 232, 232, 180 }, { 234, 234, 185 }, { 235, 235, 191 }, { 237, 237, 196 }, { 238, 238, 201 }, { 240, 240, 206 }, { 241, 241, 211 }, { 243, 243, 216 }, { 244, 244, 221 }, { 245, 245, 225 }, { 247, 247, 230 }, { 248, 248, 234 }, { 250, 250, 238 }, { 251, 251, 243 }, { 252, 252, 247 }, { 254, 254, 251 }, { 255, 255, 255 } }; break;
                    case eColorScheme.Spring:       u8_RGB = new byte[,] { { 255, 0, 255 }, { 255, 4, 251 }, { 255, 8, 247 }, { 255, 12, 243 }, { 255, 16, 239 }, { 255, 20, 235 }, { 255, 24, 231 }, { 255, 28, 227 }, { 255, 32, 223 }, { 255, 36, 219 }, { 255, 40, 215 }, { 255, 45, 210 }, { 255, 49, 206 }, { 255, 53, 202 }, { 255, 57, 198 }, { 255, 61, 194 }, { 255, 65, 190 }, { 255, 69, 186 }, { 255, 73, 182 }, { 255, 77, 178 }, { 255, 81, 174 }, { 255, 85, 170 }, { 255, 89, 166 }, { 255, 93, 162 }, { 255, 97, 158 }, { 255, 101, 154 }, { 255, 105, 150 }, { 255, 109, 146 }, { 255, 113, 142 }, { 255, 117, 138 }, { 255, 121, 134 }, { 255, 125, 130 }, { 255, 130, 125 }, { 255, 134, 121 }, { 255, 138, 117 }, { 255, 142, 113 }, { 255, 146, 109 }, { 255, 150, 105 }, { 255, 154, 101 }, { 255, 158, 97 }, { 255, 162, 93 }, { 255, 166, 89 }, { 255, 170, 85 }, { 255, 174, 81 }, { 255, 178, 77 }, { 255, 182, 73 }, { 255, 186, 69 }, { 255, 190, 65 }, { 255, 194, 61 }, { 255, 198, 57 }, { 255, 202, 53 }, { 255, 206, 49 }, { 255, 210, 45 }, { 255, 215, 40 }, { 255, 219, 36 }, { 255, 223, 32 }, { 255, 227, 28 }, { 255, 231, 24 }, { 255, 235, 20 }, { 255, 239, 16 }, { 255, 243, 12 }, { 255, 247, 8 }, { 255, 251, 4 }, { 255, 255, 0 } }; break;
                    case eColorScheme.Summer:       u8_RGB = new byte[,] { { 0, 128, 102 }, { 4, 130, 102 }, { 8, 132, 102 }, { 12, 134, 102 }, { 16, 136, 102 }, { 20, 138, 102 }, { 24, 140, 102 }, { 28, 142, 102 }, { 32, 144, 102 }, { 36, 146, 102 }, { 40, 148, 102 }, { 45, 150, 102 }, { 49, 152, 102 }, { 53, 154, 102 }, { 57, 156, 102 }, { 61, 158, 102 }, { 65, 160, 102 }, { 69, 162, 102 }, { 73, 164, 102 }, { 77, 166, 102 }, { 81, 168, 102 }, { 85, 170, 102 }, { 89, 172, 102 }, { 93, 174, 102 }, { 97, 176, 102 }, { 101, 178, 102 }, { 105, 180, 102 }, { 109, 182, 102 }, { 113, 184, 102 }, { 117, 186, 102 }, { 121, 188, 102 }, { 125, 190, 102 }, { 130, 192, 102 }, { 134, 194, 102 }, { 138, 196, 102 }, { 142, 198, 102 }, { 146, 200, 102 }, { 150, 202, 102 }, { 154, 204, 102 }, { 158, 206, 102 }, { 162, 208, 102 }, { 166, 210, 102 }, { 170, 212, 102 }, { 174, 215, 102 }, { 178, 217, 102 }, { 182, 219, 102 }, { 186, 221, 102 }, { 190, 223, 102 }, { 194, 225, 102 }, { 198, 227, 102 }, { 202, 229, 102 }, { 206, 231, 102 }, { 210, 233, 102 }, { 215, 235, 102 }, { 219, 237, 102 }, { 223, 239, 102 }, { 227, 241, 102 }, { 231, 243, 102 }, { 235, 245, 102 }, { 239, 247, 102 }, { 243, 249, 102 }, { 247, 251, 102 }, { 251, 253, 102 }, { 255, 255, 102 } }; break;
                    case eColorScheme.Winter:       u8_RGB = new byte[,] { { 0, 0, 255 }, { 0, 4, 253 }, { 0, 8, 251 }, { 0, 12, 249 }, { 0, 16, 247 }, { 0, 20, 245 }, { 0, 24, 243 }, { 0, 28, 241 }, { 0, 32, 239 }, { 0, 36, 237 }, { 0, 40, 235 }, { 0, 45, 233 }, { 0, 49, 231 }, { 0, 53, 229 }, { 0, 57, 227 }, { 0, 61, 225 }, { 0, 65, 223 }, { 0, 69, 221 }, { 0, 73, 219 }, { 0, 77, 217 }, { 0, 81, 215 }, { 0, 85, 213 }, { 0, 89, 210 }, { 0, 93, 208 }, { 0, 97, 206 }, { 0, 101, 204 }, { 0, 105, 202 }, { 0, 109, 200 }, { 0, 113, 198 }, { 0, 117, 196 }, { 0, 121, 194 }, { 0, 125, 192 }, { 0, 130, 190 }, { 0, 134, 188 }, { 0, 138, 186 }, { 0, 142, 184 }, { 0, 146, 182 }, { 0, 150, 180 }, { 0, 154, 178 }, { 0, 158, 176 }, { 0, 162, 174 }, { 0, 166, 172 }, { 0, 170, 170 }, { 0, 174, 168 }, { 0, 178, 166 }, { 0, 182, 164 }, { 0, 186, 162 }, { 0, 190, 160 }, { 0, 194, 158 }, { 0, 198, 156 }, { 0, 202, 154 }, { 0, 206, 152 }, { 0, 210, 150 }, { 0, 215, 148 }, { 0, 219, 146 }, { 0, 223, 144 }, { 0, 227, 142 }, { 0, 231, 140 }, { 0, 235, 138 }, { 0, 239, 136 }, { 0, 243, 134 }, { 0, 247, 132 }, { 0, 251, 130 }, { 0, 255, 128 } }; break;
                    default:
                        throw new ArgumentException("Invalid enum");
                }

                Color[] c_Schema = new Color[u8_RGB.GetLength(0)];
                for (int i=0; i<c_Schema.Length; i++)
                {
                    c_Schema[i] = Color.FromArgb(u8_RGB[i, 0], u8_RGB[i, 1], u8_RGB[i, 2]);
                }
                return c_Schema;
            }

            /// <summary>
            /// s32_Sweeps = 4 --> Colors from blue to red, not including magenta       (1024 colors)
            /// s32_Sweeps = 6 --> Complete sweep with all rainbow colors, also magenta (1536 colors)
            /// </summary>
            private static Color[] CalcRainbow(int s32_Sweeps)
            {
                Color[] c_Colors = new Color[s32_Sweeps * 256];
                int R,G,B,P=0;
                for (int L=0; L<s32_Sweeps; L++)
                {
                    for (int E=0; E<256; E++)
                    {
                        switch (L)
                        {
                            case 0: R = 0;       G = E;       B = 255;     break; // Blue...Cyan
                            case 1: R = 0;       G = 255;     B = 255 - E; break; // Cyan...Green
                            case 2: R = E;       G = 255;     B = 0;       break; // Green...Yellow
                            case 3: R = 255;     G = 255 - E; B = 0;       break; // Yellow...Red
                            case 4: R = 255;     G = 0;       B = E;       break; // Red...Magenta
                            case 5: R = 255 - E; G = 0;       B = 255;     break; // Magenta...Blue
                            default: throw new ArgumentException();
                        }
                        c_Colors[P++] = Color.FromArgb(255, R, G, B);
                    }
                }
                return c_Colors;
            }

            // ========================= MEMBER ===========================

            private Brush[] mi_Brushes;
            private Pen[]   mi_Pens;

            public int ColorCount
            {
                get { return mi_Brushes.Length; }
            }

            /// <summary>
            /// Constructor 1
            /// </summary>
            public cColorScheme(eColorScheme e_Scheme) 
                : this (GetSchema(e_Scheme))
            {
            }

            /// <summary>
            /// Constructor 2
            /// If you want to draw the entire graph with only one color, you can pass a single Color here.
            /// </summary>
            public cColorScheme(params Color[] c_Colors)
            {
                if (c_Colors.Length == 0)
                    throw new ArgumentException("A ColorScheme needs at least one color.");

                mi_Brushes = new SolidBrush[c_Colors.Length];
                mi_Pens    = new Pen       [c_Colors.Length];

                for (int i=0; i < c_Colors.Length; i++)
                {
                    mi_Brushes[i] = new SolidBrush(c_Colors[i]);
                    mi_Pens   [i] = new Pen       (c_Colors[i], 1); // the width of the Pen will be modified later.
                }
            }

            public Brush GetBrush(int s32_Index)
            {
                s32_Index = Math.Max(0, s32_Index);
                return mi_Brushes[s32_Index % mi_Brushes.Length];
            }

            public Pen GetPen(int s32_Index)
            {
                s32_Index = Math.Max(0, s32_Index);
                return mi_Pens[s32_Index % mi_Pens.Length];
            }

            public int CalcIndex(double d_FactorZ)
            {
                if (double.IsNaN(d_FactorZ))
                    return -1;

                d_FactorZ = Math.Min(1.0, d_FactorZ);
                d_FactorZ = Math.Max(0.0, d_FactorZ);

                // d_FactorZ is a value between 0.0 and 1.0
                return (int)(d_FactorZ * (mi_Brushes.Length - 1));
            }
        }

        #endregion

        #region cUserInput

        public class cUserInput
        {
            private MouseButtons me_MouseButton;
            private Keys         me_Modifiers;
            private eMouseAction me_Action;
            private Cursor       mi_Cursor;

            /// <summary>
            /// A unique identifier for the combination of mouse button and modifier(s)
            /// Keys.Shift            = 0x00010000
            /// Keys.Control          = 0x00020000
            /// Keys.Alt              = 0x00040000
            /// MouseButtons.Left     = 0x00100000
            /// MouseButtons.Right    = 0x00200000
            /// MouseButtons.Middle   = 0x00400000
            /// MouseButtons.XButton1 = 0x00800000
            /// MouseButtons.XButton2 = 0x01000000
            /// </summary>
            public int UID
            {
                get { return (int)me_MouseButton | (int)me_Modifiers; }
            }

            public MouseButtons MouseButton
            {
                get { return me_MouseButton; }
            }
            public Keys Modifiers
            {
                get { return me_Modifiers; }
            }
            public eMouseAction Action
            {
                get { return me_Action; }
            }
            public Cursor Cursor
            {
                get { return mi_Cursor;  }
            }

            public cUserInput(MouseButtons e_MouseButton, Keys e_Modifiers, eMouseAction e_Action, Cursor i_Cursor = null)
            {
                Debug.Assert((e_Modifiers & Keys.KeyCode) == 0,  "Invalid parameter e_Modifiers");
                Debug.Assert(e_MouseButton != MouseButtons.None, "Invalid parameter e_MouseButton");
                Debug.Assert(e_Action      != eMouseAction.None, "Invalid parameter e_Action");

                me_MouseButton = e_MouseButton;
                me_Modifiers   = e_Modifiers;
                me_Action      = e_Action;
                mi_Cursor      = i_Cursor;

                if (mi_Cursor == null)
                {
                    switch (e_Action)
                    {
                        case eMouseAction.ThetaAndPhi: mi_Cursor = Cursors.SizeAll;     break;
                        case eMouseAction.Theta:       mi_Cursor = Cursors.NoMoveVert;  break;
                        case eMouseAction.Phi:         mi_Cursor = Cursors.NoMoveHoriz; break;
                        case eMouseAction.Rho:         mi_Cursor = Cursors.SizeNS;      break;
                        case eMouseAction.Move:        mi_Cursor = Cursors.NoMove2D;    break;
                        default:                       mi_Cursor = Cursors.Arrow;       break;
                    }
                }
            }

            /// <summary>
            /// For debugging in Visual Studio
            /// </summary>
            public override string ToString()
            {
                return String.Format("MouseButton: {0}, Modifiers: {1} --> Action: {2}", me_MouseButton, me_Modifiers, me_Action);
            }
        }

        #endregion

        // ---------------------

        #region cRenderData

        /// <summary>
        /// Base class for cSurfaceData, cScatterData, cLineData, cPolygonData
        /// </summary>
        public abstract class cRenderData
        {
            public virtual void AddDrawObjects(Editor3D i_Inst)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// The width and color of the Pen may be modified later.
            /// So the immutable framework collection like Pens.Black,... cannot be used here.
            /// </summary>
            protected static void CheckPenMutable(Pen i_Pen, cColorScheme i_ColorScheme)
            {
                if (i_Pen != null && i_ColorScheme != null)
                {
                    try   { i_Pen.Color = Color.BlanchedAlmond; }
                    catch { throw new ArgumentException("To use a color scheme create a new Pen. Do not use the immutable Pens.XYZ collection."); }
                }
            }
        }

        #endregion

        #region cSurfaceData

        public class cSurfaceData : cRenderData
        {
            private bool          mb_Fill;
            private bool          mb_Missing;
            private int           ms32_Cols;
            private int           ms32_Rows;
            private int           ms32_Radius;
            private Pen           mi_Pen;
            private cPoint3D[,]   mi_PointArray;
            private cPolygon3D[,] mi_PolygonArray;
            private cColorScheme  mi_ColorScheme;

            public cColorScheme ColorScheme
            {
                get { return mi_ColorScheme; }
            }

            /// <summary>
            /// The count of points in one column of the surface grid
            /// </summary>
            public int Cols
            {
                get { return ms32_Cols; }
            }

            /// <summary>
            /// The count of points in one row of the surface grid
            /// </summary>
            public int Rows
            {
                get { return ms32_Rows; }
            }

            /// <summary>
            /// Fill Mode:
            /// ------------
            /// Polygons are filled with a color from the ColorScheme.
            /// If you want only one color, set a ColorScheme which contains only one color.
            /// The Pen is used to draw the thin lines between the polygons (mostly black, 1 pixel)
            /// If Pen is null, no lines are drawn.
            /// 
            /// Line Mode:
            /// ------------
            /// Only the border lines of the polygons are drawn.
            /// The Pen is used to draw these lines. The Pen's color and width will be modified.
            /// 
            /// Missing Mode:
            /// --------------
            /// s32_Radius > 0 allows missing points.
            /// s32_Radius defines the radius of cicles that represent points which have not enough neigbours to draw a polygon.
            /// </summary>
            public cSurfaceData(int s32_Cols, int s32_Rows, ePolygonMode e_Mode, Pen i_Pen, cColorScheme i_ColorScheme, 
                                int s32_Radius = 0)
            {
                if (s32_Cols < 3 || s32_Rows < 3)
                    throw new ArgumentException("cSurfaceData needs at least 3 columns and 3 rows");

                if (e_Mode == ePolygonMode.Fill)
                {
                    if (i_ColorScheme == null)
                        throw new ArgumentException("In Fill mode you must specify a ColorScheme");

                    // The border pen is allowed to be immutable. It will not be changed.
                }
                else // Lines
                {
                    if (i_Pen == null)
                        throw new ArgumentException("In Line mode you must specify a Line Pen");

                    CheckPenMutable(i_Pen, i_ColorScheme);
                }

                mb_Fill        = e_Mode == ePolygonMode.Fill;
                mb_Missing     = s32_Radius > 0;
                ms32_Radius    = s32_Radius;
                ms32_Cols      = s32_Cols;
                ms32_Rows      = s32_Rows;
                mi_Pen         = i_Pen;
                mi_ColorScheme = i_ColorScheme;
                mi_PointArray  = new cPoint3D[s32_Cols, s32_Rows];
            }

            /// <summary>
            /// Here you can set a callback function which will be called with X,Y to calculate the Z values of the points.
            /// </summary>
            public void ExecuteFunction(delRendererFunction f_Function, PointF k_Start, PointF k_End)
            {
                mb_Missing = false;

                double d_StepX = (k_End.X - k_Start.X) / (ms32_Cols - 1);
                double d_StepY = (k_End.Y - k_Start.Y) / (ms32_Rows - 1);

                for (int C = 0; C < ms32_Cols; C++)
                {
                    double d_X = k_Start.X + d_StepX * C;

                    for (int R = 0; R < ms32_Rows; R++)
                    {
                        double d_Y = k_Start.Y + d_StepY * R;
                        double d_Z = f_Function(d_X, d_Y);

                        SetPointAt(C, R, new cPoint3D(d_X, d_Y, d_Z));
                    }
                }
            }

            /// <summary>
            /// IMPORTANT: 
            /// The X coordinate of the point must be related to the column.
            /// The Y coordinate of the point must be related to the row.
            /// </summary>
            public void SetPointAt(int s32_Column, int s32_Row, cPoint3D i_Point3D)
            {
                if (mi_PolygonArray != null)
                    throw new Exception("You cannot call cSurfaceData.SetPointAt() or ExecuteFunction() anymore after calling GetPolygonAt() "
                                      + "or Editor3D.AddRenderData(). To modify a point after the polygons have been created call "
                                      + "GetPointAt() and modify the X,Y,Z values of the returned point.");

                mi_PointArray[s32_Column, s32_Row] = i_Point3D;
            }

            /// <summary>
            /// ATTENTION: 
            /// In mode 'Missing' null may be returned or polygons with only 3 corners!
            /// </summary>
            public cPoint3D GetPointAt(int s32_Column, int s32_Row)
            {
                // 15/08/2024 having trouble with null been returned. Need to sort this out
                cPoint3D point = mi_PointArray[s32_Column, s32_Row];

                return point;
            }

            /// <summary>
            /// ATTENTION: 
            /// The polygons have one row less than cSurfaceData.Rows and one column less than cSurfaceData.Cols
            /// </summary>
            public cPolygon3D GetPolygonAt(int s32_Column, int s32_Row)
            {
                CreatePolygons();
                return mi_PolygonArray[s32_Column, s32_Row];
            }

            private void CreatePolygons()
            {
                if (mi_PolygonArray != null)
                    return;

                cPolygon3D[,] i_TempArr = new cPolygon3D[ms32_Cols -1, ms32_Rows -1];

                List<cPoint3D> i_Valid = new List<cPoint3D>();
                for (int C=0; C < ms32_Cols -1; C++)
                {
                    for (int R=0; R < ms32_Rows -1; R++)
                    {
                        i_Valid.Clear();
                        if (mi_PointArray[C,   R]   != null) i_Valid.Add(mi_PointArray[C,   R]);
                        if (mi_PointArray[C,   R+1] != null) i_Valid.Add(mi_PointArray[C,   R+1]);
                        if (mi_PointArray[C+1, R+1] != null) i_Valid.Add(mi_PointArray[C+1, R+1]);
                        if (mi_PointArray[C+1, R]   != null) i_Valid.Add(mi_PointArray[C+1, R]);

                        if (i_Valid.Count < 4 && !mb_Missing)
                            throw new Exception("cSurfaceData: You must call cSurfaceData.SetPointAt() for all points!");

                        if (i_Valid.Count < 3)
                            continue; // A polygon needs at least 3 points
                                
                        i_TempArr[C, R] = new cPolygon3D(C, R, null, i_Valid.ToArray());
                    }
                }

                mi_PolygonArray = i_TempArr;
            }

            // =============================================================================

            /// <summary>
            /// Called from AddRenderData()
            /// </summary>
            public override void AddDrawObjects(Editor3D i_Inst)
            {
                CreatePolygons();

                bool b_Added = false;
                List<cPoint3D> i_Used = new List<cPoint3D>();

                foreach (cPolygon3D i_Poly3D in mi_PolygonArray)
                {
                    if (i_Poly3D == null)
                        continue;

                    i_Inst.AddDrawObject(new cPolygon(mb_Fill, i_Poly3D, mi_Pen, mi_ColorScheme));
                    b_Added = true;

                    foreach (cPoint3D i_Point3D in i_Poly3D.Points)
                    {
                        if (!i_Used.Contains(i_Point3D))
                             i_Used.Add(i_Point3D);
                    }
                }

                // Add all the remaining points that are not part of any polygon as Scatter circles.
                foreach (cPoint3D i_Point3D in mi_PointArray)
                {
                    if (i_Point3D == null || i_Used.Contains(i_Point3D))
                        continue;

                    cShape3D i_Shape3D = new cShape3D(i_Point3D, eScatterShape.Circle, ms32_Radius, null);
                    i_Inst.AddDrawObject(new cShape  (i_Shape3D, mi_ColorScheme));
                    b_Added = true;
                }

                if (!b_Added)
                    throw new Exception("You cannot draw a completely empty SurfaceData");
            }
        }

        #endregion

        #region cScatterData

        public class cScatterData : cRenderData
        {
            private List<cShape3D> mi_Shapes3D  = new List<cShape3D>();
            private cColorScheme   mi_ColorScheme;

            public cShape3D[] AllShapes
            {
                get { return mi_Shapes3D.ToArray(); }
            }

            public cColorScheme ColorScheme
            {
                get { return mi_ColorScheme; }
            }

            /// <summary>
            /// Constructor
            /// If all Scatter shapes contain a valid Brush, you can pass i_ColorScheme == null here
            /// </summary>
            public cScatterData(cColorScheme i_ColorScheme)
            {
                mi_ColorScheme = i_ColorScheme;
            }

            /// <summary>
            /// s32_Radius defines the size of the shape and i_Brush the color
            /// </summary>
            public cShape3D AddShape(cPoint3D i_Point, eScatterShape e_Shape, int s32_Radius, Brush i_Brush, Object o_Tag = null)
            {
                cShape3D i_Shape3D = new cShape3D(i_Point, e_Shape, s32_Radius, i_Brush, o_Tag);
                mi_Shapes3D.Add(i_Shape3D);
                return i_Shape3D;
            }

            // =============================================================================

            /// <summary>
            /// Called from AddRenderData()
            /// </summary>
            public override void AddDrawObjects(Editor3D i_Inst)
            {
                foreach (cShape3D i_Shape3D in mi_Shapes3D)
                {
                    i_Inst.AddDrawObject(new cShape(i_Shape3D, mi_ColorScheme));
                }
            }
        }

        #endregion

        #region cLineData

        public class cLineData : cRenderData
        {
            private List<cLine3D> mi_Lines3D = new List<cLine3D>();
            private cColorScheme  mi_ColorScheme;

            public cLine3D[] AllLines
            {
                get { return mi_Lines3D.ToArray(); }
            }

            public cColorScheme ColorScheme
            {
                get { return mi_ColorScheme; }
            }

            /// <summary>
            /// Constructor
            /// If you use only solid lines and specify a valid Pen, you can pass i_ColorScheme == null here
            /// </summary>
            public cLineData(cColorScheme i_ColorScheme)
            {
                mi_ColorScheme = i_ColorScheme;
            }

            /// <summary>
            /// Add a line which will be drawn entirely in one color.
            /// </summary>
            public cLine3D AddSolidLine(cPoint3D i_Start, cPoint3D i_End, int s32_Width, Pen i_Pen, Object o_Tag = null)
            {
                CheckPenMutable(i_Pen, mi_ColorScheme);

                cLine3D i_Line3D = new cLine3D(i_Start, i_End, s32_Width, i_Pen, 1, o_Tag);
                mi_Lines3D.Add(i_Line3D);
                return i_Line3D;
            }

            /// <summary>
            /// Add a line which will appear with multiple colors of the ColorScheme by drawing it in multiple parts.
            /// If s32_Parts = 50, the line is rendered in 50 parts where each part has it's own color depending on the Z coordinate.
            /// </summary>
            public cLine3D AddMultiColorLine(int s32_Parts, cPoint3D i_Start, cPoint3D i_End, int s32_Width, Pen i_Pen, Object o_Tag = null)
            {
                if (s32_Parts < 3)
                    throw new ArgumentException("Multi color lines require at least 3 parts");

                if (mi_ColorScheme == null)
                    throw new Exception("To create a multi-color line you must specify a ColorScheme");

                CheckPenMutable(i_Pen, mi_ColorScheme);

                cLine3D i_Line3D = new cLine3D(i_Start, i_End, s32_Width, i_Pen, s32_Parts, o_Tag);
                mi_Lines3D.Add(i_Line3D);
                return i_Line3D;
            }

            /// <summary>
            /// Creates connected lines from the points in the given order
            /// </summary>
            public cLine3D[] AddConnectedLines(List<cPoint3D> i_Points, int s32_Width, Pen i_Pen)
            {
                CheckPenMutable(i_Pen, mi_ColorScheme);

                List<cLine3D> i_NewLines = new List<cLine3D>();

                cPoint3D i_Prev = null;
                for (int i=0; i<i_Points.Count; i++)
                {
                    cPoint3D i_Point = i_Points[i];
                    if (i_Prev != null)
                    {
                        cLine3D i_Line3D = new cLine3D(i_Prev, i_Point, s32_Width, i_Pen, 1);
                        i_NewLines.Add(i_Line3D);
                        mi_Lines3D.Add(i_Line3D);
                    }
                    i_Prev = i_Point;
                }
                return i_NewLines.ToArray();
            }

            // =============================================================================

            /// <summary>
            /// Called from AddRenderData()
            /// </summary>
            public override void AddDrawObjects(Editor3D i_Inst)
            {
                foreach (cLine3D i_Line3D in mi_Lines3D)
                {
                    i_Inst.AddDrawObject(new cLine(i_Line3D, mi_ColorScheme));
                }
            }
        }

        #endregion

        #region cPolygonData

        public class cPolygonData : cRenderData
        {
            private bool             mb_Fill;
            private Pen              mi_Pen;
            private cColorScheme     mi_ColorScheme;
            private List<cPolygon3D> mi_Polygons3D = new List<cPolygon3D>();

            public cPolygon3D[] AllPolygons
            {
                get { return mi_Polygons3D.ToArray(); }
            }

            public cColorScheme ColorScheme
            {
                get { return mi_ColorScheme; }
            }

            /// <summary>
            /// Fill Mode:
            /// ------------
            /// Polygons are filled with a color from the ColorScheme.
            /// If you want only one color, set a ColorScheme which contains only one color.
            /// The Pen is used to draw the thin lines between the polygons (mostly black, 1 pixel)
            /// If Pen is null, no lines are drawn.
            /// 
            /// Line Mode:
            /// ------------
            /// Only the border lines of the polygons are drawn.
            /// The Pen is used to draw these lines. The Pen's color and width will be modified.
            /// </summary>
            public cPolygonData(ePolygonMode e_Mode, Pen i_Pen, cColorScheme i_ColorScheme)
            {
                if (e_Mode == ePolygonMode.Lines)
                {
                    if (i_Pen == null)
                        throw new ArgumentException("In Line mode you must specify a Line Pen");

                    CheckPenMutable(i_Pen, i_ColorScheme);
                }

                mb_Fill        = e_Mode == ePolygonMode.Fill;
                mi_Pen         = i_Pen;
                mi_ColorScheme = i_ColorScheme;
            }

            /// <summary>
            /// In contrast to other drawing libraries (like WPF or Direct3D) you can add polygons of any dimension here.
            /// A polygon can have any amount of corners (minimum 3).
            /// The Brush can be specified in Fill mode. To use a Brush from the ColorScheme set i_Brush = null.
            /// </summary>
            public cPolygon3D AddPolygon(Brush i_Brush, params cPoint3D[] i_Points3D)
            {
                cPolygon3D i_Polygon3D = new cPolygon3D(-1, -1, i_Brush, i_Points3D);               
                mi_Polygons3D.Add(i_Polygon3D);
                return i_Polygon3D;
            }

            // =============================================================================

            /// <summary>
            /// Called from AddRenderData()
            /// </summary>
            public override void AddDrawObjects(Editor3D i_Inst)
            {
                foreach (cPolygon3D i_Polygon3D in mi_Polygons3D)
                {
                    i_Inst.AddDrawObject(new cPolygon(mb_Fill, i_Polygon3D, mi_Pen, mi_ColorScheme));
                }
            }
        }

        #endregion

        #region cMessgData

        public class cMessgData
        {
            private String  ms_Text;
            private Brush   mi_Brush;
            private int     ms32_PosX;
            private int     ms32_PosY;
            private Font    mi_Font;
            private SizeF   mk_Size;

            /// <summary>
            /// Here you can change the text without loading all the render objects again.
            /// The change will become visible the next time you call Invalidate()
            /// </summary>
            public String Text
            {
                set
                {
                    ms_Text = value;
                    mk_Size = SizeF.Empty;
                }
            }

            /// <summary>
            /// Here you can change the text color without loading all the render objects again.
            /// The change will become visible the next time you call Invalidate()
            /// </summary>
            public Color TextColor
            {
                set { mi_Brush = new SolidBrush(value); }
            }

            /// <summary>
            /// If X is negative, it is displayed right  aligned at X pixels from the right
            /// If Y is negative, it is displayed bottom aligned at Y pixels from the bottom
            /// </summary>
            public cMessgData(String s_Text, int X, int Y, Color c_Color,
                              FontStyle e_FontStyle = FontStyle.Bold, 
                              int     s32_FontSize  = 9, 
                              String    s_FontFace  = "Tahoma")
            {
                ms_Text   = s_Text;
                ms32_PosX = X;
                ms32_PosY = Y;
                mi_Brush  = new SolidBrush(c_Color);
                mi_Font   = new Font(s_FontFace, s32_FontSize, e_FontStyle);
            }

            public void Draw(Graphics i_Graph, Rectangle k_Client)
            {
                if (String.IsNullOrEmpty(ms_Text))
                    return;

                float X = ms32_PosX;
                float Y = ms32_PosY;

                if (X < 0 || Y < 0)
                {
                    // Speed optimization: Measure the size only once.
                    if (mk_Size.IsEmpty)
                        mk_Size = i_Graph.MeasureString(ms_Text, mi_Font);

                    if (X < 0) X += k_Client.Width  - mk_Size.Width;
                    if (Y < 0) Y += k_Client.Height - mk_Size.Height;
                }

                i_Graph.DrawString(ms_Text, mi_Font, mi_Brush, X, Y);
            }
        }

        #endregion

        // ================= PRIVATE =================

        #region cPoint2D

        /// <summary>
        /// This class represents a point in the 2D space, in pixels.
        /// </summary>
        private class cPoint2D
        {
            public double md_X;
            public double md_Y;

            public cPoint2D()
            {
            }

            public cPoint2D(double X, double Y)
            {
                md_X = X;
                md_Y = Y;
            }

            public cPoint2D Clone()
            {
                return new cPoint2D(md_X, md_Y);
            }

            public PointF Coord
            {
                get { return new PointF((float)md_X, (float)md_Y); }
            }

            public bool IsValid
            {
                get
                {
                    // The screen will always be smaller than 9999 pixels
                    return (!Double.IsNaN(md_X) && Math.Abs(md_X) < 9999.9 &&
                            !Double.IsNaN(md_Y) && Math.Abs(md_Y) < 9999.9);
                }
            }

            /// <summary>
            /// Use the good old Pythagoras to calculate the pixel distance between this point and X, Y.
            /// </summary>
            public int CalcDistanceTo(int X, int Y)
            {
                int s32_DiffX = (int)md_X - X;
                int s32_DiffY = (int)md_Y - Y;
                return (int)Math.Sqrt(s32_DiffX * s32_DiffX + s32_DiffY * s32_DiffY);
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                return String.Format("cPoint2D (X={0}, Y={1})", FormatDouble(md_X), FormatDouble(md_Y));
            }
        }

        #endregion

        #region cPoint

        /// <summary>
        /// This class contains the 3D point and it's projection into the 2D space.
        /// </summary>
        private class cPoint
        {
            public  cPoint3D mi_P3D;
            public  cPoint2D mi_P2D;
            public  int      ms32_RadiusTip; // if 0 --> no tooltip

            public cPoint(double d_X, double d_Y, double d_Z)
            {
                mi_P3D = new cPoint3D(d_X, d_Y, d_Z);
                mi_P2D = new cPoint2D();
            }

            /// <summary>
            /// The radius defines at which distance of the mouse from the 2D point the tooltip pops up.
            /// Radius = 0 --> no tooltip
            /// </summary>
            public cPoint(cPoint3D i_Point3D, int s32_RadiusTip)
            {
                mi_P3D = i_Point3D;
                mi_P2D = new cPoint2D();

                if (s32_RadiusTip > 0)
                    ms32_RadiusTip = s32_RadiusTip + ToolTipRadius; // np                
            }

            /// <summary>
            /// Projects the 3D coordinates into the 2D space (pixels on the screen).
            /// </summary>
            public void Project3D(Editor3D i_Inst, eMirror e_Mirror)
            {
                mi_P2D = i_Inst.mi_Transform.Project3D(mi_P3D, e_Mirror);
            }

            /// <summary>
            /// For Debugging in Visual Studio
            /// </summary>
            public override string ToString()
            {
                return String.Format("cPoint [{0} = {1}]", mi_P3D, mi_P2D);
            }
        }

        #endregion

        #region cTooltip

        private class cTooltip
        {
            private Editor3D     mi_Inst    = null;
            private eTooltip     me_Mode    = eTooltip.All;
            private ToolTip      mi_Tooltip = new ToolTip();
            private List<cPoint> mi_Points  = new List<cPoint>();
            private cPoint       mi_Last    = null;
            private cPoint       mi_Nearest = null; // np

            public eTooltip Mode
            {
                get { return me_Mode;  }
                set { me_Mode = value; }
            }

            public cPoint NearestPoint { get { return mi_Nearest; } } // np

            // Constructor
            public cTooltip(Editor3D i_Inst)
            {
                mi_Inst = i_Inst;

                mi_Tooltip.AutoPopDelay = 30000; // The maximum that Windows allows are 32.767 seconds (0x7FFF milliseconds)
                mi_Tooltip.InitialDelay = 50;
                mi_Tooltip.ReshowDelay  = 50;
            }

            public void Clear()
            {
                mi_Points.Clear();
                mi_Last = null;
                Hide();
            }

            public void AddPoint(cPoint i_Point)
            {
                // ATTENTION adding a 
                // && !mi_Points.Contains(i_Point)
                // here would make this function 40 times slower!
                // The more points are already in mi_Points, the slower it would become.
                // Several points will be added multiple times here, but this does not affect the functioning of the tooltip.
                if (i_Point != null && i_Point.ms32_RadiusTip > 0)
                    mi_Points.Add(i_Point);
            }

            public void Hide()
            {
                mi_Tooltip.Hide(mi_Inst);
            }

            public void OnMouseMove(MouseEventArgs e)
            {
                if (me_Mode == eTooltip.Off)
                {
                    Hide();
                    return;
                }

                int s32_MouseX = e.X - mi_Inst.mi_Mouse.mk_OffMove.X - mi_Inst.mi_Mouse.mk_OffCoord.X;
                int s32_MouseY = e.Y - mi_Inst.mi_Mouse.mk_OffMove.Y - mi_Inst.mi_Mouse.mk_OffCoord.Y;
                
                int  s32_MinDist = int.MaxValue;
                cPoint i_Nearest = null;
                foreach (cPoint i_Point in mi_Points)
                {
                    int s32_Dist = i_Point.mi_P2D.CalcDistanceTo(s32_MouseX, s32_MouseY);
                    if (s32_Dist < s32_MinDist)
                    {
                        s32_MinDist = s32_Dist;
                        i_Nearest   = i_Point;
                    }
                }

                if (i_Nearest != null && s32_MinDist < i_Nearest.ms32_RadiusTip)
                {
                    if (mi_Last == i_Nearest)
                        return; // The mouse is still over the same point
                    
                    mi_Last = i_Nearest;

                    String s_TT = "";
                    if ((me_Mode & eTooltip.Coord) > 0)
                        s_TT = String.Format("X = {0}\nY = {1}\nZ = {2}\n", FormatDouble(i_Nearest.mi_P3D.X), 
                                                                            FormatDouble(i_Nearest.mi_P3D.Y), 
                                                                            FormatDouble(i_Nearest.mi_P3D.Z));
                
                    if ((me_Mode & eTooltip.UserText) > 0 && i_Nearest.mi_P3D.Tooltip != null)
                        s_TT += i_Nearest.mi_P3D.Tooltip;

                    s_TT = s_TT.Trim();
                    if (s_TT.Length > 0)
                    {
                        mi_Tooltip.Show(s_TT, mi_Inst, e.X + 10, e.Y + 10);
                        return;
                    }
                }

                mi_Last = null;
                Hide();
            }
        }

        #endregion

        #region cBackup

        private class cBackup
        {
            public Dictionary<eUndoProp, Object[]> mi_Changes = new Dictionary<eUndoProp, Object[]>();

            public void StoreProperty(eUndoProp e_Property, Object o_OldValue, Object o_NewValue)
            {
                Object[] o_Values;
                if (mi_Changes.TryGetValue(e_Property, out o_Values))
                {
                    // The property has changed multiple times (dragging) --> store only the last value
                    o_Values[1] = o_NewValue;
                }
                else
                {
                    // The property has changed for the first time
                    mi_Changes.Add(e_Property, new Object[] { o_OldValue, o_NewValue });
                }
            }

            public void Restore(cObject3D i_Object3D, bool b_Undo)
            {
                foreach (KeyValuePair<eUndoProp, Object[]> i_Pair in mi_Changes)
                {
                    if (b_Undo) i_Object3D.Restore(i_Pair.Key, i_Pair.Value[0]); // Undo: old value
                    else        i_Object3D.Restore(i_Pair.Key, i_Pair.Value[1]); // Redo: new value
                }
            }

            // for debugging in Visual Studio
            public override string ToString()
            {
                StringBuilder i_Dbg = new StringBuilder();
                foreach (eUndoProp e_Property in mi_Changes.Keys)
                {
                    if (i_Dbg.Length > 0) i_Dbg.Append(", ");
                    i_Dbg.Append(e_Property);
                }
                return "cBackup Properties: " + i_Dbg.ToString();
            }
        }

        #endregion

        #region cUndoEntry

        private class cUndoEntry
        {
            public Dictionary<cObject3D, cBackup> mi_Backups = new Dictionary<cObject3D,cBackup>();
            public List<cDrawObj>                 mi_Added   = new List<cDrawObj>();
            public List<cDrawObj>                 mi_Removed = new List<cDrawObj>();

            public bool IsEmpty
            {
                get { return mi_Backups.Count == 0 && mi_Added.Count == 0 && mi_Removed.Count == 0; }
            }

            // for debugging in Visual Studio
            public override string ToString()
            {
                return String.Format("Backups: {0}, Added: {1}, Removed: {2}", (mi_Backups == null) ? 0 : mi_Backups.Count, 
                                                                               (mi_Added   == null) ? 0 : mi_Added  .Count, 
                                                                               (mi_Removed == null) ? 0 : mi_Removed.Count);
            }
        }

        #endregion

        #region cUndoImpl

        /// <summary>
        /// This private class stores the changes after each edit operation that the user has made.
        /// </summary>
        private class cUndoImpl : cUndoBuffer
        {
            private Editor3D         mi_Inst;
            private int              ms32_UndoIdx = 0;     // Points to the next Undo item in mi_UndoList
            private int              ms32_RedoIdx = 0;     // Points to the next Redo item in mi_UndoList
            private bool             mb_Enabled   = false; // The Undo Buffer is by default disabled
            private bool             mb_DoInit    = false; // After Clear() execute Init() once.
            private bool             mb_Pending   = false; // User actions are pending that have not yet been stored
            private cUndoEntry       mi_CurUndo   = new cUndoEntry();
            private List<cUndoEntry> mi_UndoList  = new List<cUndoEntry>();

            /// <summary>
            /// Do not enable the Undo buffer if you don't need the Undo functionality.
            /// By default it is disabled.
            /// </summary>
            public override bool Enabled
            {
                get { return mb_Enabled;  }
                set 
                { 
                    Clear();
                    mb_Enabled = value; 
                }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public cUndoImpl(Editor3D i_Inst)
            {
                mi_Inst = i_Inst;
            }

            public override void Clear()
            {
                ms32_UndoIdx = 0;
                ms32_RedoIdx = 0;
                mb_DoInit    = true;
                mb_Pending   = false;
                mi_CurUndo   = new cUndoEntry();
                mi_UndoList.Clear();
            }

            /// <summary>
            /// This must only be called from Editor3D.Render() !
            /// The first time calling Invalidate() after Clear() --> clear all draw objets that have been added.
            /// Otherwise all added objects would be stored. (This class stores only user changes.)
            /// </summary>
            public void Init()
            {
                if (mb_DoInit)
                {
                    Clear();
                    mb_DoInit = false;
                }
            }

            /// <summary>
            /// Called when any public property of a 3D object was changed (e.g. X,Y,Z coordinates of a cPoint3D).
            /// These changes will be stored when Store() is called.
            /// </summary>
            public void Backup(cObject3D i_Object3D, eUndoProp e_Property, Object o_OldValue, Object o_NewValue)
            {
                if (!mb_Enabled)
                    return;

                cBackup i_Backup;
                if (!mi_CurUndo.mi_Backups.TryGetValue(i_Object3D, out i_Backup))
                {
                    i_Backup = new cBackup();
                    mi_CurUndo.mi_Backups.Add(i_Object3D, i_Backup);
                }

                i_Backup.StoreProperty(e_Property, o_OldValue, o_NewValue);
                mb_Pending = true;
            }

            /// <summary>
            /// Called when the user removes a draw object.
            /// These changes will be stored when Store() is called.
            /// </summary>
            public void DrawObjectRemoved(cDrawObj i_DrawObject)
            {
                if (!mb_Enabled)
                    return;

                mi_CurUndo.mi_Removed.Add(i_DrawObject);
                mb_Pending = true;
            }

            /// <summary>
            /// Called when the user adds a draw object.
            /// These changes will be stored when Store() is called.
            /// </summary>
            public void DrawObjectAdded(cDrawObj i_DrawObject)
            {
                if (!mb_Enabled)
                    return;

                mi_CurUndo.mi_Added.Add(i_DrawObject);
                mb_Pending = true;
            }

            /// <summary>
            /// Store the changes on user objects in the Undo buffer on mouse-up.
            /// </summary>
            public void Store()
            {
                if (!mb_Enabled || !mb_Pending || mi_CurUndo.IsEmpty)
                    return;

                // Remove any entries in the Undo Buffer behind the current index
                int s32_Remove = mi_UndoList.Count - ms32_UndoIdx - 1;
                if (s32_Remove > 0)
                    mi_UndoList.RemoveRange(ms32_UndoIdx + 1, s32_Remove);

                mi_UndoList.Add(mi_CurUndo);
                mi_CurUndo = new cUndoEntry();

                ms32_UndoIdx = mi_UndoList.Count - 1;
                ms32_RedoIdx = mi_UndoList.Count - 1;
                mb_Pending   = false;
            }

            /// <summary>
            /// This is also called when hitting CTRL + Z while the 3D Editor has the keyboard focus
            /// </summary>
            public override bool Undo()
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread

                if (!mb_Enabled || mi_UndoList.Count == 0 || ms32_UndoIdx < 0)
                    return false;

                Restore(true, ms32_UndoIdx);

                ms32_RedoIdx = ms32_UndoIdx;
                ms32_UndoIdx --;
                return true;
            }

            /// <summary>
            /// This is also called when hitting CTRL + Y while the 3D Editor has the keyboard focus
            /// </summary>
            public override bool Redo()
            {
                Debug.Assert(!mi_Inst.InvokeRequired); // Call only from GUI thread

                if (!mb_Enabled || mi_UndoList.Count == 0 || ms32_RedoIdx >= mi_UndoList.Count)
                    return false;

                Restore(false, ms32_RedoIdx);

                ms32_UndoIdx = ms32_RedoIdx;
                ms32_RedoIdx ++;
                return true;
            }

            private void Restore(bool b_Undo, int s32_Index)
            {
                s32_Index = Math.Max(s32_Index, 0);
                s32_Index = Math.Min(s32_Index, mi_UndoList.Count - 1);

                cUndoEntry i_UndoItem = mi_UndoList[s32_Index];

                foreach (cDrawObj i_Removed in i_UndoItem.mi_Removed)
                {
                    if (b_Undo) mi_Inst.mi_UserObjects.Add   (i_Removed);
                    else        mi_Inst.mi_UserObjects.Remove(i_Removed);
                    
                    mi_Inst.me_Recalculate |= eRecalculate.AddRemove | eRecalculate.Objects | eRecalculate.CoordSystem;
                }

                foreach (cDrawObj i_Added in i_UndoItem.mi_Added)
                {
                    if (b_Undo) mi_Inst.mi_UserObjects.Remove(i_Added);
                    else        mi_Inst.mi_UserObjects.Add   (i_Added);
                    
                    mi_Inst.me_Recalculate |= eRecalculate.AddRemove | eRecalculate.Objects | eRecalculate.CoordSystem;
                }

                foreach (KeyValuePair<cObject3D, cBackup> i_Pair in i_UndoItem.mi_Backups)
                {
                    i_Pair.Value.Restore(i_Pair.Key, b_Undo);
                    
                    mi_Inst.me_Recalculate |= eRecalculate.Objects | eRecalculate.CoordSystem;
                }

                mi_Inst.Invalidate();
            }
        }

        #endregion

        // ----- DrawObjects -----

        #region cDrawObj

        /// <summary>
        /// Base class for cLine, cShape, cPolygon
        /// </summary>
        private abstract class cDrawObj : IComparable
        {
            public    Editor3D      mi_Inst;
            public    SmoothingMode me_SmoothMode;
            public    cPoint[]      mi_Points;
            public    cObject3D     mi_Object3D;   // This is a cLine3D, cShape3D or cPolygon3D
            public    double        md_Sort;       // sorting is important. Always draw first back, then front objects.
            public    bool          mb_IsAxis;     // This is a line from the coordinate system
            protected double        md_AvrgZ;      // 3D center of the Z coordinates of all points in this object
            protected bool          mb_IsValid;
            protected eMirror       me_Mirror;

            // --------------------------------------------------

            /// <summary>
            /// Set after conversion 3D --> 2D. If invalid coordinates are found (NaN or > 9999) this returns false.
            /// If projection results in lines of thousands of pixels length, the drawing will become extremely slow.
            /// Do not draw lines or polygons outside the screen area.
            /// </summary>
            public bool IsValid
            {
                get { return mb_IsValid; }
            }

            /// <summary>
            /// The object is selected
            /// </summary>
            public bool Selected
            {
                get { return mi_Object3D.Selected;  }
                set { mi_Object3D.Selected = value; }
            }

            /// <summary>
            /// The type of this object
            /// </summary>
            public virtual eSelType SelType
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// Calculates colors from color scheme
            /// </summary>
            public virtual void ProcessColors()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Calculates the 2D screen coordinates for each 3D point.
            /// </summary>
            public virtual void Project3D()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Draw into Graphics
            /// </summary>
            public virtual void Render(Graphics i_Graph)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Check if a user click at mouse point X, Y matches this draw object
            /// </summary>
            public virtual cObject3D MatchesPoint2D(int X, int Y)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Uses the center of a draw object (e.g. the middle of a line) to calculate in which order the draw objects are rendered.
            /// This is called for all user defined objects, but not for the coordinate system which has it's own logic.
            /// </summary>
            public void CalcSortOrder()
            {
                Debug.Assert(!mb_IsAxis); // axes have their own value for md_Sort

                double d_AvrgX = 0.0;
                double d_AvrgY = 0.0;
                      md_AvrgZ = 0.0;

                foreach (cPoint i_Point in mi_Points)
                {
                     d_AvrgX += i_Point.mi_P3D.X;
                     d_AvrgY += i_Point.mi_P3D.Y;
                    md_AvrgZ += i_Point.mi_P3D.Z;
                }

                 d_AvrgX /= mi_Points.Length;
                 d_AvrgY /= mi_Points.Length;
                md_AvrgZ /= mi_Points.Length;

                cPolygon3D i_Poly = mi_Object3D as cPolygon3D;
                if (i_Poly != null && i_Poly.Row > -1)
                {
                    int X = i_Poly.Column + 1;
                    int Y = i_Poly.Row    + 1;

                    // Mirror axis values increasing / decreasing
                    if (mi_Inst.AxisX.Mirror && (me_Mirror & eMirror.X) > 0) X = 5000 - i_Poly.Column;
                    if (mi_Inst.AxisY.Mirror && (me_Mirror & eMirror.Y) > 0) Y = 5000 - i_Poly.Row;

                    // In case of a surface grid the Z value must be ignored because sorting is ALWAYS based on the position in the grid.
                    // Using the Z value here may even result in wrong sort order.
                    md_Sort = mi_Inst.mi_Transform.ProjectXY(X, Y);
                }
                else
                {
                    double X =  d_AvrgX;
                    double Y =  d_AvrgY;
                    double Z = md_AvrgZ;

                    // Mirror axis values increasing / decreasing
                    if (mi_Inst.AxisX.Mirror && (me_Mirror & eMirror.X) > 0) X = mi_Inst.mi_Bounds.X.Max - (X - mi_Inst.mi_Bounds.X.Min);
                    if (mi_Inst.AxisY.Mirror && (me_Mirror & eMirror.Y) > 0) Y = mi_Inst.mi_Bounds.Y.Max - (Y - mi_Inst.mi_Bounds.Y.Min);
                    if (mi_Inst.AxisZ.Mirror && (me_Mirror & eMirror.Z) > 0) Z = mi_Inst.mi_Bounds.Z.Max - (Z - mi_Inst.mi_Bounds.Z.Min);

                    // In case of any other 3D object the Z value must also be included in the calculation to avoid artifacts.
                    // Demo Sphere shows that the Z value is required if you move Theta to an extreme.
                    X = (X - mi_Inst.mi_Transform.mi_Center3D.X) * mi_Inst.mi_Transform.md_NormalizeX;
                    Y = (Y - mi_Inst.mi_Transform.mi_Center3D.Y) * mi_Inst.mi_Transform.md_NormalizeY;
                    Z = (Z - mi_Inst.mi_Transform.mi_Center3D.Z) * mi_Inst.mi_Transform.md_NormalizeZ;

                    md_Sort = mi_Inst.mi_Transform.ProjectXY(X, Y, Z);
                }
            }

            /// <summary>
            /// Used for sorting all DrawObjects from back to front
            /// </summary>
            int IComparable.CompareTo(Object o_Comp)
            {
                return md_Sort.CompareTo(((cDrawObj)o_Comp).md_Sort);
            }
        }

        #endregion

        #region cLine

        private class cLine : cDrawObj
        {
            private cLine3D mi_Line3D;       // object passed to and from the user
            private Pen     mi_Pen;
            private Brush   mi_Brush;        // assigned to Pen
            private float   mf_LineWidth;    // Linewidth with zoom factor
            private float   mf_SelSize;      // size of selection points

            // -------- coordinate axes --------
            public  double  md_Angle;                    // needed to calculate current rotation quadrant of coordinate axis
            public  eCoord  me_Line   = eCoord.Invalid;  // main      coordinate in coordinate direction
            public  eCoord  me_Offset = eCoord.Invalid;  // secondary coordinate in coordinate direction
            public  String  ms_Label;                    // Label for axis
            
            // ---------- multicolor -----------
            private cPoint[]     mi_ColorPoints;         // all points on the line that are drawn separately
            private Brush[]      mi_ColorBrushes;        // all Brushes which are assigned to the Pen
            private cColorScheme mi_ColorScheme;

            public override eSelType SelType
            {
                get { return eSelType.Line; }
            }

            /// <summary>
            /// Constructor 1 for coordinate system.
            /// LineWidth is always 1.
            /// </summary>
            public cLine(Editor3D i_Inst, eCoord e_Line, eCoord e_Offset, eMirror e_Mirror)
            {
                if (e_Line == e_Offset) mi_Pen = i_Inst.mi_Axis[(int)e_Line]  .AxisPen;   // Main axis
                else                    mi_Pen = i_Inst.mi_Axis[(int)e_Offset].RasterPen; // Raster line

                mi_Inst       = i_Inst;
                me_Line       = e_Line;
                me_Offset     = e_Offset;
                mi_Brush      = mi_Pen.Brush;
                mi_Points     = new cPoint[2];
                mi_Points[0]  = new cPoint(0, 0, 0);
                mi_Points[1]  = new cPoint(0, 0, 0);
                mb_IsAxis     = true;
                me_Mirror     = e_Mirror;
                me_SmoothMode = SmoothingMode.AntiAlias;
            }

            /// <summary>
            /// Constructor 2 for user lines
            /// if i_Line3D.Pen == null --> Pen from ColorScheme is used
            /// if i_Line3D.Pen != null --> ColorScheme will be ignored, even if a ColorScheme is specified
            /// An indvidual LineWidth can be defined in each cLine3D.
            /// </summary>
            public cLine(cLine3D i_Line3D, cColorScheme i_ColorScheme)
            {
                if (i_Line3D.Pen == null && i_ColorScheme == null)
                    throw new ArgumentException("You must specify a Pen or a ColorScheme");

                mi_ColorScheme = i_ColorScheme;
                mi_Line3D      = i_Line3D;
                mi_Object3D    = i_Line3D;
                mi_Pen         = i_Line3D.Pen; // get user's Pen or null
                mi_Points      = new cPoint[2];
                mi_Points[0]   = new cPoint(i_Line3D.Points[0], 1);
                mi_Points[1]   = new cPoint(i_Line3D.Points[1], 1);
                mb_IsAxis      = false;
                me_Mirror      = eMirror.All;
                me_SmoothMode  = SmoothingMode.AntiAlias;

                if (mi_Pen != null)
                {
                    // The original Brush must be stored separately because the same Pen may be used for multiple instances of cLine
                    // Changing the color of one line would affect all the others.
                    mi_Brush = mi_Pen.Brush;
                }
                else
                {
                    mi_Pen   = new Pen(Brushes.Black); // color and width will be changed below
                    mi_Brush = null;                   // Brush will be taken from colorscheme
                }
                mi_Pen.StartCap = LineCap.Round;
                mi_Pen.EndCap   = LineCap.Round;

                // ---------- multi color ---------

                if (i_Line3D.ColorParts > 1)
                {
                    mi_ColorBrushes = new Brush [i_Line3D.ColorParts];
                    mi_ColorPoints  = new cPoint[i_Line3D.ColorParts];
                }
            }

            public override void ProcessColors()
            {
                Debug.Assert(!mb_IsAxis); // axes have their own color

                // If the user has changed the Pen --> use the new Pen and it's Brush
                if (mi_Line3D.Pen != null)
                {
                    mi_Pen   = mi_Line3D.Pen;
                    mi_Brush = mi_Pen.Brush;

                    mi_Pen.StartCap = LineCap.Round;
                    mi_Pen.EndCap   = LineCap.Round;
                }

                if (mi_ColorPoints != null) // multicolor line
                {
                    double d_X = mi_Points[0].mi_P3D.X;
                    double d_Y = mi_Points[0].mi_P3D.Y;
                    double d_Z = mi_Points[0].mi_P3D.Z;

                    double d_DeltaX = (mi_Points[1].mi_P3D.X - d_X) / mi_ColorPoints.Length;
                    double d_DeltaY = (mi_Points[1].mi_P3D.Y - d_Y) / mi_ColorPoints.Length;
                    double d_DeltaZ = (mi_Points[1].mi_P3D.Z - d_Z) / mi_ColorPoints.Length;

                    mi_ColorPoints[0] = mi_Points[0]; // Set Start Point

                    cPoint i_Prev = mi_Points[0];
                    for (int i=1; i<mi_ColorPoints.Length; i++)
                    {
                        d_X += d_DeltaX;
                        d_Y += d_DeltaY;
                        d_Z += d_DeltaZ;

                        cPoint i_Point = new cPoint(new cPoint3D(d_X, d_Y, d_Z), 0);

                        double d_AvrgZ     = (i_Prev.mi_P3D.Z + i_Point.mi_P3D.Z) / 2.0;
                        double d_FactorZ   = mi_Inst.mi_Bounds.CalcFactorZ(d_AvrgZ);
                        int    s32_Index   = mi_ColorScheme.CalcIndex(d_FactorZ);
                        mi_ColorBrushes[i] = mi_ColorScheme.GetBrush(s32_Index);
                        mi_ColorPoints [i] = i_Point;

                        i_Prev = i_Point;
                    }

                    mi_ColorPoints[mi_ColorPoints.Length -1] = mi_Points[1]; // Set End Point
                }
                else // solid line
                {
                    if (mi_ColorScheme != null)
                    {
                        // Load missing Pen from ColorScheme which the user has not specified.
                        double d_FactorZ = mi_Inst.mi_Bounds.CalcFactorZ(md_AvrgZ);
                        int    s32_Index = mi_ColorScheme.CalcIndex(d_FactorZ);
                        mi_Brush         = mi_ColorScheme.GetBrush(s32_Index);
                    }
                }
            }

            public override void Project3D()
            {
                cPoint[] i_PointArr = (mi_ColorPoints != null) ? mi_ColorPoints : mi_Points;
                foreach (cPoint i_Point in i_PointArr)
                {
                    i_Point.Project3D(mi_Inst, me_Mirror);
                }

                mb_IsValid = mi_Points[0].mi_P2D.IsValid && mi_Points[1].mi_P2D.IsValid;

                if (!mb_IsAxis)
                {
                    mf_LineWidth = (float)(mi_Line3D.Width * mi_Inst.mi_Transform.md_Zoom);
                    // Diameter of circle for selected points
                    mf_SelSize   = (float)(Math.Max(6, mi_Line3D.Width * 2) * mi_Inst.mi_Transform.md_Zoom);
                }
            }

            public override void Render(Graphics i_Graph)
            {
                // b_LineSel depends on the selection of the line only. It does not matter if an end point is selected or not.
                bool b_LineSel = !mb_IsAxis && Selected && mi_Inst.mi_Selection.HighlightPen != null;

                if (mi_ColorPoints == null || b_LineSel) // draw solid line
                {                  
                    Pen i_DrawPen;
                    if (b_LineSel)
                    {
                        i_DrawPen = mi_Inst.mi_Selection.HighlightPen;
                    }
                    else
                    {
                         i_DrawPen       = mi_Pen;
                         i_DrawPen.Brush = mi_Brush; // mi_Brush = null for multicolor lines!
                    }
                    
                    // Axis lines have always 1 pixel width
                    if (!mb_IsAxis)
                        i_DrawPen.Width = mf_LineWidth;
                    
                    i_Graph.DrawLine(i_DrawPen, mi_Points[0].mi_P2D.Coord, mi_Points[1].mi_P2D.Coord);
                }
                else // multi color
                {
                    mi_Pen.Width = mf_LineWidth;

                    cPoint i_Prev = mi_ColorPoints[0];
                    for (int i=1; i<mi_ColorPoints.Length; i++)
                    {
                        cPoint i_Point = mi_ColorPoints [i];
                        mi_Pen.Brush   = mi_ColorBrushes[i];
                        i_Graph.DrawLine(mi_Pen, i_Prev.mi_P2D.Coord, i_Point.mi_P2D.Coord);

                        i_Prev = i_Point;
                    }
                }

                // Draw circle for selected points
                foreach (cPoint i_Point in mi_Points)
                {
                    if (!i_Point.mi_P3D.Selected)
                        continue;
                    
                    float X = (float)i_Point.mi_P2D.md_X - mf_SelSize / 2.0f;
                    float Y = (float)i_Point.mi_P2D.md_Y - mf_SelSize / 2.0f;
                    i_Graph.FillEllipse(mi_Inst.mi_Selection.HighlightBrush, X, Y, mf_SelSize, mf_SelSize);
                }
            }

            /// <summary>
            /// Check if a user click at X, Y matches this draw object
            /// </summary>
            public override cObject3D MatchesPoint2D(int X, int Y)
            {
                if (mb_IsAxis)
                    return null; // do not allow to select axis lines

                int s32_MaxDist = Math.Max(1, (int)mi_Pen.Width / 2); // np ... + mi_SelectRadius

                if (mi_Inst.Selection.SinglePoints)
                {
                    foreach (cPoint i_Point in mi_Points)
                    {
                        if (i_Point.mi_P2D.CalcDistanceTo(X, Y) <= s32_MaxDist)
                            return i_Point.mi_P3D;
                    }
                }
                else // select entire line
                {
                    if (IsPointOnLine(mi_Points[0].mi_P2D, mi_Points[1].mi_P2D, X, Y, s32_MaxDist))
                        return mi_Line3D;
                }
                    
                return null;
            }

            // ---------------- Coord System ---------------

            /// <summary>
            /// Used while creating coordinate system
            /// Check if 2 lines have the same coordinates.
            /// </summary>
            public bool CoordEquals(cLine i_Line)
            {
                return mi_Points[0].mi_P3D.CoordEquals(i_Line.mi_Points[0].mi_P3D) &&
                       mi_Points[1].mi_P3D.CoordEquals(i_Line.mi_Points[1].mi_P3D);
            }

            /// <summary>
            /// Used while creating coordinate system
            /// Calculate the angle of the 3 main axes on the screen in a range from 0 to 360 degree.
            /// </summary>
            public void CalcAngle2D()
            {
                double d_DX = mi_Points[1].mi_P2D.md_X - mi_Points[0].mi_P2D.md_X;
                double d_DY = mi_Points[1].mi_P2D.md_Y - mi_Points[0].mi_P2D.md_Y;
                md_Angle = Math.Atan2(d_DY, d_DX) * 180.0 / Math.PI;
                if (md_Angle < 0.0) md_Angle += 360.0;
            }

            // For debugging in Visual Studio
            public override string ToString()
            {
                String s_Dbg = String.Format("cLine from {0} to {1}", mi_Points[0], mi_Points[1]);
                if (mb_IsAxis)
                    s_Dbg += String.Format(" (Axis {0}, {1})", me_Line, me_Offset);

                return s_Dbg;
            }
        }

        #endregion

        #region cShape

        private class cShape : cDrawObj
        {
            private float         mf_Radius;   // radius   of shape adapted with Zoom factor
            private float         mf_Diameter; // diameter of shape adapted with Zoom factor
            private PointF        mk_TopLeft;  // top left corner in screen coordinates for all types of shapes
            private PointF[]      mk_Polygon;  // used for triangles or any future user objects
            private Brush         mi_Brush;
            private cShape3D      mi_Shape3D;
            private cColorScheme  mi_ColorScheme;

            public override eSelType SelType
            {
                get { return eSelType.Shape; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public cShape(cShape3D i_Shape3D, cColorScheme i_ColorScheme)
            {
                if (i_Shape3D.Brush == null && i_ColorScheme == null)
                    throw new ArgumentException("You must specify a Brush or a ColorScheme");

                mi_Shape3D     = i_Shape3D;
                mi_Object3D    = i_Shape3D;
                mi_Points      = new cPoint[1];
                mi_Points[0]   = new cPoint(i_Shape3D.Points[0], i_Shape3D.Radius);
                mi_ColorScheme = i_ColorScheme;
                me_SmoothMode  = SmoothingMode.AntiAlias;
                me_Mirror      = eMirror.All;
            }

            public override void ProcessColors()
            {
                // If the user has specified an individual brush for this Shape --> always use it
                mi_Brush = mi_Shape3D.Brush;

                // Otherwise use Brush from ColorScheme
                if (mi_Brush == null)
                {
                    double d_FactorZ = mi_Inst.mi_Bounds.CalcFactorZ(md_AvrgZ);
                    int    s32_Index = mi_ColorScheme.CalcIndex(d_FactorZ);
                    mi_Brush         = mi_ColorScheme.GetBrush (s32_Index);
                }
            }

            public override void Project3D()
            {
                mi_Points[0].Project3D(mi_Inst, me_Mirror);

                mb_IsValid  = mi_Points[0].mi_P2D.IsValid;
                mf_Radius   = (float)(mi_Shape3D.Radius * mi_Inst.mi_Transform.md_Zoom);
                mf_Diameter = mf_Radius * 2.0f;

                // Move coordinate from center to upper left corner of circle
                mk_TopLeft    = mi_Points[0].mi_P2D.Coord;
                mk_TopLeft.X -= mf_Radius;
                mk_TopLeft.Y -= mf_Radius;

                switch (mi_Shape3D.Shape)
                {
                    case eScatterShape.Triangle:
                        mk_Polygon = new PointF[3];
                        // top center
                        mk_Polygon[0].X = mk_TopLeft.X + mf_Radius;
                        mk_Polygon[0].Y = mk_TopLeft.Y;
                        // bottom left
                        mk_Polygon[1].X = mk_TopLeft.X;
                        mk_Polygon[1].Y = mk_TopLeft.Y + mf_Diameter;
                        // bottom right
                        mk_Polygon[2].X = mk_TopLeft.X + mf_Diameter;
                        mk_Polygon[2].Y = mk_TopLeft.Y + mf_Diameter;
                        break;

                    // case eScatterShape.Star:
                        // Here you can implement your own shapes
                        // break;
                }
            }

            public override void Render(Graphics i_Graph)
            {
                bool  b_DrawSel   = Selected && mi_Inst.mi_Selection.HighlightBrush != null;
                Brush i_DrawBrush = b_DrawSel ? mi_Inst.mi_Selection.HighlightBrush : mi_Brush;

                switch (mi_Shape3D.Shape)
                {
                    case eScatterShape.Circle:
                        i_Graph.FillEllipse  (i_DrawBrush, mk_TopLeft.X, mk_TopLeft.Y, mf_Diameter, mf_Diameter);
                        break;
                    case eScatterShape.Square:
                        i_Graph.FillRectangle(i_DrawBrush, mk_TopLeft.X, mk_TopLeft.Y, mf_Diameter, mf_Diameter);
                        break;
                    default:
                        i_Graph.FillPolygon  (i_DrawBrush, mk_Polygon);
                        break;
                }
            }

            /// <summary>
            /// Check if a user click at X, Y matches this draw object
            /// </summary>
            public override cObject3D MatchesPoint2D(int X, int Y)
            {
                int s32_MaxDist = (int)mf_Radius + SelectRadius; // np ... + SELECT_RADIUS;


                if (mi_Points[0].mi_P2D.CalcDistanceTo(X, Y) <= s32_MaxDist)
                    return mi_Shape3D;

                return null;
            }

            /// <summary>
            /// For Debugging in Visual Studio
            /// </summary>
            public override string ToString()
            {
                return String.Format("cShape {0} at {1}, Diameter {2}", mi_Shape3D.Shape, mi_Points[0], FormatDouble(mf_Diameter));
            }
        }

        #endregion

        #region cPolygon

        private class cPolygon : cDrawObj
        {
            private bool          mb_Fill;        // Fill / Line mode
            private float         mf_SelSize;     // size of selection points
            private PointF[]      mk_Screen;      // the 2D polygon corner points in screen coordinates
            private int           ms32_OrgWidth;  // original line width for Line Pen
            private float         mf_LineWidth;   // zoomed   line width for Line Pen
            private Pen           mi_LinePen;     // used in Line mode
            private Pen           mi_BorderPen;   // used in Fill mode (not zoomed)
            private Brush         mi_Brush;       // used in Fill mode
            private cPolygon3D    mi_Polygon3D;
            private cColorScheme  mi_ColorScheme;

            public override eSelType SelType
            {
                get { return eSelType.Polygon; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public cPolygon(bool b_Fill, cPolygon3D i_Polygon3D, Pen i_Pen, cColorScheme i_ColorScheme)
            {
                mi_Points = new cPoint[i_Polygon3D.Points.Length];
                for (int i=0; i<mi_Points.Length; i++)
                {
                    mi_Points[i] = new cPoint(i_Polygon3D.Points[i], 1);
                }

                mb_Fill        = b_Fill;
                mi_Polygon3D   = i_Polygon3D;
                mi_Object3D    = i_Polygon3D;
                mi_ColorScheme = i_ColorScheme;
                mk_Screen      = new PointF[mi_Points.Length];
                me_Mirror      = eMirror.All;

                if (b_Fill) mi_BorderPen = i_Pen;
                else        mi_LinePen   = i_Pen;

                if (i_Pen != null)
                    ms32_OrgWidth = (int)i_Pen.Width;

                // Drawing polygon border lines with antialias makes them very thick and black.
                // Do not smooth the lines when the polygons are filled with color and the lines are only separators.
                // But use smooth mode if only the lines are drawn.
                me_SmoothMode = (b_Fill) ? SmoothingMode.None : SmoothingMode.AntiAlias;
            }

            public override void ProcessColors()
            {
                // If the user has specified an individual brush for this Polygon --> always use it
                if (mi_Polygon3D.Brush != null)
                {
                    mi_Brush = mi_Polygon3D.Brush;
                }
                else if (mi_ColorScheme != null) // ColorScheme is never null in Fill mode
                {
                    double  d_FactorZ = mi_Inst.mi_Bounds.CalcFactorZ(md_AvrgZ);
                    int     s32_Index = mi_ColorScheme.CalcIndex(d_FactorZ);
                    mi_Brush          = mi_ColorScheme.GetBrush (s32_Index); // used for Fill and assigned to LinePen
                }
            }

            public override void Project3D()
            {
                foreach (cPoint i_Point in mi_Points)
                {
                    i_Point.Project3D(mi_Inst, me_Mirror);
                }

                // Line width for Line mode 
                mf_LineWidth = (float)(ms32_OrgWidth * mi_Inst.mi_Transform.md_Zoom);

                // Diameter of circle for selected points
                mf_SelSize = (float)(Math.Max(6, ms32_OrgWidth * 2) * mi_Inst.mi_Transform.md_Zoom);

                mb_IsValid = true;
                for (int i=0; i<mi_Points.Length; i++)
                {
                    if (mi_Points[i].mi_P2D.IsValid)
                        mk_Screen[i] = mi_Points[i].mi_P2D.Coord;                       
                    else
                        mb_IsValid = false;
                }
            }

            public override void Render(Graphics i_Graph)
            {
                // Fill polygon with solid color
                if (mb_Fill)
                {
                    Brush i_FillBrush = mi_Brush;
                    if (Selected && mi_Inst.mi_Selection.HighlightBrush != null)
                        i_FillBrush = mi_Inst.mi_Selection.HighlightBrush;

                    i_Graph.FillPolygon(i_FillBrush, mk_Screen);
                }

                Pen i_DrawPen;
                if (mb_Fill)
                {
                    i_DrawPen = mi_BorderPen;
                }
                else // Line mode
                {
                    i_DrawPen       = mi_LinePen;
                    i_DrawPen.Width = mf_LineWidth;

                    if (mi_Brush != null)
                        i_DrawPen.Brush = mi_Brush;
                }

                // Fill mode --> draw thin black border lines around the polygons
                // Line mode --> draw thicker lines around transparent polygons
                if (i_DrawPen != null)
                {
                    // ATTENTION: Graphics.DrawPolygon() with a Pen > 1 pixel is buggy in the .NET framework (artifacts)!
                    // The lines must be drawn one by one manually here.
                    int T = mk_Screen.Length - 1;
                    for (int F=0; F<mk_Screen.Length; F++)
                    {
                        i_Graph.DrawLine(i_DrawPen, mk_Screen[F], mk_Screen[T]);
                        T = F;
                    }    
                }

                // Draw selected points
                foreach (cPoint i_Point in mi_Points)
                {
                    if (!i_Point.mi_P3D.Selected)
                        continue;
                    
                    float X = (float)i_Point.mi_P2D.md_X - mf_SelSize / 2.0f;
                    float Y = (float)i_Point.mi_P2D.md_Y - mf_SelSize / 2.0f;
                    i_Graph.FillEllipse(mi_Inst.mi_Selection.HighlightBrush, X, Y, mf_SelSize, mf_SelSize); // np, selected point drawn here
                }
            }

            /// <summary>
            /// Check if a user click at X, Y matches this draw object
            /// </summary>
            public override cObject3D MatchesPoint2D(int X, int Y)
            {
                // ATTENTION: Selecting entire polygons makes only sense with eSurfaceMode.Fill
                // In line mode polygons are transparent and a click into the polygon would go to the background.
                if (!mb_Fill || mi_Inst.mi_Selection.SinglePoints)
                {
                    int s32_MaxDist = (int)mf_SelSize / 2 + SelectRadius; // np ... + SELECT_RADIUS;
                    foreach (cPoint i_Point in mi_Points)
                    {
                        if (i_Point.mi_P2D.CalcDistanceTo(X, Y) <= s32_MaxDist)
                            return i_Point.mi_P3D;
                    }
                }
                else // select entire polygon
                {
                    // Detect if the point is inside the polygon. Here SELECT_RADIUS is ignored. 
                    // But the user must only click into the middle of the polygon, which is easier than clicking a thin line.
                    bool b_Result = false;
                    int  k = mi_Points.Length - 1;
                    for (int i = 0; i < mi_Points.Length; i++)
                    {
                        cPoint2D i_Point1 = mi_Points[i].mi_P2D;
                        cPoint2D i_Point2 = mi_Points[k].mi_P2D;

                        if (i_Point1.md_Y < Y && i_Point2.md_Y >= Y || 
                            i_Point2.md_Y < Y && i_Point1.md_Y >= Y)
                        {
                            if (i_Point1.md_X + (Y - i_Point1.md_Y) /
                                (i_Point2.md_Y - i_Point1.md_Y) *
                                (i_Point2.md_X - i_Point1.md_X) < X)
                            {
                                b_Result = !b_Result;
                            }
                        }
                        k = i;
                    }
                    if (b_Result) 
                        return mi_Polygon3D;
                }
                return null;
            }

            /// <summary>
            /// For debugging in Visual Studio
            /// </summary>
            public override string ToString()
            {
                return String.Format("cPolygon ({0} points)", mk_Screen.Length);
            }
        }

        #endregion

        // ----- Math Stuff ------

        #region cMouse 

        private class cMouse
        {
            public eMouseAction me_Action;     // left mouse button action
            public Point        mk_LastPos;    // last mouse location
            public Point        mk_OffMove;    // Mouse offset after moving the graph with the mouse
            public Point        mk_OffCoord;   // Offset caused by labels in coordinate system
            public TrackBar     mi_TrackRho;   // Rho trackbar (optional)
            public TrackBar     mi_TrackTheta; // Theta trackbar (optional)
            public TrackBar     mi_TrackPhi;   // Phi trackbar (optional)
            public double       md_Rho     = VALUES_RHO  .Default;
            public double       md_Theta   = VALUES_THETA.Default;
            public double       md_Phi     = VALUES_PHI  .Default;

            public void AssignTrackbar(eMouseAction e_Trackbar, TrackBar i_Trackbar, EventHandler i_OnScroll)
            {
                if (i_Trackbar == null)
                    return;

                cDefault i_Default = null;
                switch (e_Trackbar)
                {
                    case eMouseAction.Rho:
                        i_Default     = VALUES_RHO;
                        mi_TrackRho   = i_Trackbar;
                        break;
                    case eMouseAction.Theta:
                        i_Default     = VALUES_THETA;
                        mi_TrackTheta = i_Trackbar;
                        break;
                    case eMouseAction.Phi:
                        i_Default     = VALUES_PHI;
                        mi_TrackPhi   = i_Trackbar;
                        break;
                }

                i_Trackbar.Minimum = (int)i_Default.Min;
                i_Trackbar.Maximum = (int)i_Default.Max;
                i_Trackbar.Value   = (int)i_Default.Default;
                i_Trackbar.Scroll += i_OnScroll;
            }

            /// <summary>
            /// User has moved the TrackBar
            /// </summary>
            public void OnTrackBarScroll()
            {
                if (mi_TrackRho   != null) md_Rho   = mi_TrackRho  .Value;
                if (mi_TrackTheta != null) md_Theta = mi_TrackTheta.Value;
                if (mi_TrackPhi   != null) md_Phi   = mi_TrackPhi  .Value;
            }

            public bool OnMouseWheel(int s32_Delta)
            {
                if (me_Action != eMouseAction.None)
                    return false;

                me_Action = eMouseAction.Rho;
                OnMouseMove(0, s32_Delta / 10);
                me_Action = eMouseAction.None;
                return true;
            }

            /// <summary>
            /// User has dragged the mouse over the 3D control
            /// </summary>
            public void OnMouseMove(int s32_DiffX, int s32_DiffY)
            {
                if (me_Action == eMouseAction.Rho)
                {
                    md_Rho += s32_DiffY * VALUES_RHO.MouseFactor;
                    SetRho(md_Rho);
                }
                if (me_Action == eMouseAction.Theta || me_Action == eMouseAction.ThetaAndPhi)
                {
                    md_Theta -= s32_DiffY * VALUES_THETA.MouseFactor;
                    SetTheta(md_Theta);
                }
                if (me_Action == eMouseAction.Phi || me_Action == eMouseAction.ThetaAndPhi)
                {
                    md_Phi -= s32_DiffX * VALUES_PHI.MouseFactor;
                    SetPhi(md_Phi);
                }
            }

            public void SetRho(double d_Rho)
            {
                md_Rho = d_Rho;
                md_Rho = Math.Max(md_Rho, VALUES_RHO.Min);
                md_Rho = Math.Min(md_Rho, VALUES_RHO.Max);
                if (mi_TrackRho != null)
                    mi_TrackRho.Value = (int)md_Rho;
            }
            public void SetTheta(double d_Theta)
            {
                md_Theta = d_Theta;
                md_Theta = Math.Max(md_Theta, VALUES_THETA.Min);
                md_Theta = Math.Min(md_Theta, VALUES_THETA.Max);
                if (mi_TrackTheta != null)
                    mi_TrackTheta.Value = (int)md_Theta;
            }
            public void SetPhi(double d_Phi)
            {
                md_Phi = d_Phi;
                while (md_Phi > 360.0) md_Phi -= 360.0; // continuous rotation
                while (md_Phi <   0.0) md_Phi += 360.0; // continuous rotation
                if (mi_TrackPhi != null)
                    mi_TrackPhi.Value = (int)md_Phi;
            }
        }

        #endregion

        #region cRange

        private class cRange
        {
            private double md_Min, md_Max;

            public double Min
            {
                get { return md_Min; }
            }
            public double Max
            {
                get { return md_Max; }
            }
            public double Range
            {
                get { return md_Max - md_Min; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public cRange(double d_Min, double d_Max, bool b_IncludeZero, eRaster e_Raster)
            {
                md_Min = d_Min;
                md_Max = d_Max;

                if (md_Max == md_Min) 
                { 
                    md_Min -= 1.0; 
                    md_Max += 1.0; 
                }

                if (e_Raster == eRaster.Off)
                    return;

                if (b_IncludeZero)
                {
                    md_Min = Math.Min(0.0, md_Min); 
                    md_Max = Math.Max(0.0, md_Max); 
                }

                // Add 10 % excess to all axes
                if (md_Min < 0.0 || (md_Min > 0.0 && !b_IncludeZero)) md_Min -= Math.Abs(md_Min) * AXIS_EXCESS;
                if (md_Max > 0.0 || (md_Max < 0.0 && !b_IncludeZero)) md_Max += Math.Abs(md_Max) * AXIS_EXCESS;
            }
        }

        #endregion

        #region cBounds

        private class cBounds
        {
            private Editor3D mi_Inst;
            private cRange   mi_RangeX;
            private cRange   mi_RangeY;
            private cRange   mi_RangeZ;
            private double   md_MinX, md_MaxX, md_MinY, md_MaxY, md_MinZ, md_MaxZ;

            public cRange X 
            {
                get { return mi_RangeX; }
            }
            public cRange Y
            {
                get { return mi_RangeY; }
            }
            public cRange Z
            {
                get { return mi_RangeZ; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public cBounds(Editor3D i_Inst)
            {
                mi_Inst = i_Inst;
            }

            /// <summary>
            /// Also assigns mi_Inst to all draw objects
            /// </summary>
            public void Calculate()
            {
                md_MinX = double.PositiveInfinity;
                md_MaxX = double.NegativeInfinity;
                md_MinY = double.PositiveInfinity;
                md_MaxY = double.NegativeInfinity;
                md_MinZ = double.PositiveInfinity;
                md_MaxZ = double.NegativeInfinity;

                foreach (cDrawObj i_DrawObj in mi_Inst.mi_UserObjects)
                {
                    i_DrawObj.mi_Inst             = mi_Inst;
                    i_DrawObj.mi_Object3D.mi_Inst = mi_Inst;

                    foreach (cPoint i_Point in i_DrawObj.mi_Points)
                    {
                        cPoint3D i_Point3D = i_Point.mi_P3D;
                        i_Point3D.mi_Inst  = mi_Inst;

                        md_MinX = Math.Min(md_MinX, i_Point3D.X);
                        md_MaxX = Math.Max(md_MaxX, i_Point3D.X);
                        md_MinY = Math.Min(md_MinY, i_Point3D.Y);
                        md_MaxY = Math.Max(md_MaxY, i_Point3D.Y);
                        md_MinZ = Math.Min(md_MinZ, i_Point3D.Z);
                        md_MaxZ = Math.Max(md_MaxZ, i_Point3D.Z);
                    }
                }

                mi_RangeX = new cRange(md_MinX, md_MaxX, mi_Inst.AxisX.IncludeZero, mi_Inst.me_Raster);
                mi_RangeY = new cRange(md_MinY, md_MaxY, mi_Inst.AxisY.IncludeZero, mi_Inst.me_Raster);
                mi_RangeZ = new cRange(md_MinZ, md_MaxZ, mi_Inst.AxisZ.IncludeZero, mi_Inst.me_Raster);
            }

            /// <summary>
            /// Used to get the color from the ColorScheme
            /// </summary>
            public double CalcFactorZ(double d_Value)
            {
                return (d_Value - md_MinZ) / (md_MaxZ - md_MinZ);
            }
        }

        #endregion

        #region cQuadrant

        private class cQuadrant
        {
            public double md_SortXY;   // Sort order of raster in area XY  (red)
            public double md_SortXZ;   // Sort order of X axis and raster in area XZ (blue)
            public double md_SortYZ;   // Sort order of Y axis and raster in area YZ (green)
            public int    ms32_Quadrant;
            public bool   mb_BottomView;

            public void Calculate(double d_Phi, cLine i_AxisX, cLine i_AxisY, cLine i_AxisZ)
            {
                // Split rotation into 4 sections (0...3) which increment every 90° starting at 45°
                int s32_Section45 = (int)d_Phi + 45;
                if (s32_Section45 > 360) s32_Section45 -= 360;
                s32_Section45 = Math.Min(3, s32_Section45 / 90);

                // Theta elevation lets the camera watch the graph from the top or bottom
                switch (s32_Section45)
                {
                    case 0: mb_BottomView = i_AxisX.md_Angle < 180.0; break;
                    case 1: mb_BottomView = i_AxisY.md_Angle < 180.0; break;
                    case 2: mb_BottomView = i_AxisX.md_Angle > 180.0; break;
                    case 3: mb_BottomView = i_AxisY.md_Angle > 180.0; break;
                }

                // The quadrant changes when the 2D transformed Z axis is in line with the X or Y axis
                if (mb_BottomView)
                {
                    switch (s32_Section45)
                    {
                        case 0: ms32_Quadrant = i_AxisX.md_Angle + 180.0 < i_AxisZ.md_Angle ? 1 : 0; break;
                        case 1: ms32_Quadrant = i_AxisY.md_Angle + 180.0 < i_AxisZ.md_Angle ? 2 : 1; break;
                        case 2: ms32_Quadrant = i_AxisX.md_Angle         < i_AxisZ.md_Angle ? 3 : 2; break;
                        case 3: ms32_Quadrant = i_AxisY.md_Angle         < i_AxisZ.md_Angle ? 0 : 3; break;
                    }
                }
                else // Top View
                {
                    switch (s32_Section45)
                    {
                        case 0: ms32_Quadrant = i_AxisX.md_Angle         > i_AxisZ.md_Angle ? 1 : 0; break;
                        case 1: ms32_Quadrant = i_AxisY.md_Angle         > i_AxisZ.md_Angle ? 2 : 1; break;
                        case 2: ms32_Quadrant = i_AxisX.md_Angle + 180.0 > i_AxisZ.md_Angle ? 3 : 2; break;
                        case 3: ms32_Quadrant = i_AxisY.md_Angle + 180.0 > i_AxisZ.md_Angle ? 0 : 3; break;
                    }
                }

                md_SortXY = (mb_BottomView) ? 99999.9 : -99999.9;
                md_SortXZ = (ms32_Quadrant == 1 || ms32_Quadrant == 2) ? 99999.9 : -99999.9;
                md_SortYZ = (ms32_Quadrant == 0 || ms32_Quadrant == 1) ? 99999.9 : -99999.9;

                i_AxisX.md_Sort = md_SortXZ;
                i_AxisY.md_Sort = md_SortYZ;
                i_AxisZ.md_Sort = (ms32_Quadrant == 3) ? -99999.9 : 99999.9;

                // Debug.WriteLine(String.Format("Section: {0}  Quadrant: {1}", s32_Section45, ms32_Quadrant));
            }
        }

        #endregion

        #region cTransform

        private class cTransform
        {
            // Camera distance. Smaller values result in ugly stretched egdes when rotating.
            private const double DISTANCE = 0.55;

            private double  md_sf;   // sf = sinus fi
            private double  md_st;   // st = sinus theta
            private double  md_cf;   // cf = cosinus fi
            private double  md_ct;   // ct = cosinus theta
            private double  md_Rho;
            // ----------------
            private double  md_FactX;
            private double  md_OffsX;
            private double  md_FactY;
            private double  md_OffsY;
            private double  md_Resize = 1.0;
            // ----------------
            public cPoint3D mi_Center3D = new cPoint3D(0,0,0);
            public double   md_NormalizeX;
            public double   md_NormalizeY;
            public double   md_NormalizeZ;
            public double   md_Zoom;
            // ----------------
            Size     mk_InitialSize = Size.Empty;
            Editor3D mi_Inst;

            public cTransform(Editor3D i_Inst)
            {
                mi_Inst = i_Inst;
            }

            public void SetCoefficients(cMouse i_Mouse)
            {
                md_Rho         =  i_Mouse.md_Rho;                           // Distance of viewer (zoom)
                double d_Theta =  i_Mouse.md_Theta       * Math.PI / 180.0; // Height   of viewer (elevation)
                double d_Phi   = (i_Mouse.md_Phi -180.0) * Math.PI / 180.0; // Rotation around center (-Pi ... +Pi)

                // Speed optimization: precalculate factors
                md_sf = Math.Sin(d_Phi);  
                md_cf = Math.Cos(d_Phi);  
                md_st = Math.Sin(d_Theta); // Theta = 0...pi --> st = 0 .. 1 .. 0
                md_ct = Math.Cos(d_Theta); // Theta = 0...pi --> ct = 1 .. 0 .. -1

                CalcZoom();
                mi_Inst.me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
            }

            /// <summary>
            /// The initial size is needed to calculate the user resizing factor.
            /// To assure that it is correct it must be set when the control has already been created.
            /// Then it will be the size that was defined in Visual Studio Form Designer.
            /// </summary>
            public void SetInitialSize(Size k_Size)
            {
                mk_InitialSize = k_Size;
                SetSize(k_Size);
            }

            /// <summary>
            /// The control has been resized.
            /// This may be called with an invalid size before the control is created!
            /// </summary>
            public void SetSize(Size k_Size) // Control.ClientSize
            {
                if (mk_InitialSize == Size.Empty)
                    return;

                double d_Width  = k_Size.Width  * 0.0254 / 96.0; // 0.0254 meter = 1 inch. Screen has 96 DPI
                double d_Height = k_Size.Height * 0.0254 / 96.0;

                // linear transformation coefficients
                md_FactX =  k_Size.Width  / d_Width;
                md_FactY = -k_Size.Height / d_Height;
               
                md_OffsX =  md_FactX * d_Width  / 2.0;
                md_OffsY = -md_FactY * d_Height / 2.0;

                // -----------------------------------

                double d_ResizeX = (double)k_Size.Width  / mk_InitialSize.Width;
                double d_ResizeY = (double)k_Size.Height / mk_InitialSize.Height;
                md_Resize = Math.Min(d_ResizeX, d_ResizeY);

                md_FactX *= md_Resize;
                md_FactY *= md_Resize;

                CalcZoom();
                mi_Inst.me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
            }

            // Required for correct painting order of polygons (always from back to front)
            public double ProjectXY(double X, double Y, double Z = 0.0)
            {
                return X * md_cf + Y * md_sf + Z * md_ct;
            }

            // Used to convert mouse movements back into the 3D space depending on the current rotation angle
            public double ReverseProject(double X, double Y, double Z)
            {
                if (mi_Inst.AxisX.Mirror) X = -X;
                if (mi_Inst.AxisY.Mirror) Y = -Y;
                if (mi_Inst.AxisZ.Mirror) Z = -Z;

                // If Theta has the correct range from 10 to 170 degree --> Sinus(Theta) will never become zero.
                // This can only happen if VALUES_THETA has been manipulated to invalid Min/Max values.
                double d_Divide = Math.Max(0.1, md_st);
                return (-X * md_sf + Y * md_cf + Z / d_Divide) / md_Zoom;
            }

            /// <summary>
            /// This approximates a zoom factor that depends on Rho and the resize window factor.
            /// Used to adapt the size of lines, shapes and selected points.
            /// </summary>
            private void CalcZoom()
            {
                md_Zoom = md_Resize * (1800.0 / (md_Rho + 300));
            }

            // Performs projection. Calculates 2D screen coordinates from 3D point.
            public cPoint2D Project3D(cPoint3D i_Point3D, eMirror e_Mirror)
            {
                double X = i_Point3D.X;
                double Y = i_Point3D.Y;
                double Z = i_Point3D.Z;

                // Mirror axis values increasing / decreasing
                if (mi_Inst.AxisX.Mirror && (e_Mirror & eMirror.X) > 0) X = mi_Inst.mi_Bounds.X.Max - (X - mi_Inst.mi_Bounds.X.Min);
                if (mi_Inst.AxisY.Mirror && (e_Mirror & eMirror.Y) > 0) Y = mi_Inst.mi_Bounds.Y.Max - (Y - mi_Inst.mi_Bounds.Y.Min);
                if (mi_Inst.AxisZ.Mirror && (e_Mirror & eMirror.Z) > 0) Z = mi_Inst.mi_Bounds.Z.Max - (Z - mi_Inst.mi_Bounds.Z.Min);

                X = (X - mi_Center3D.X) * md_NormalizeX;
                Y = (Y - mi_Center3D.Y) * md_NormalizeY;
                Z = (Z - mi_Center3D.Z) * md_NormalizeZ;

                // 3D coordinates with center point in the middle of the screen
                // X positive to the right, X negative to the left
                // Y positive to the top,   Y negative to the bottom
                double xn = -md_sf *         X + md_cf         * Y;
                double yn = -md_cf * md_ct * X - md_sf * md_ct * Y + md_st * Z;
                double zn = -md_cf * md_st * X - md_sf * md_st * Y - md_ct * Z + md_Rho;

                zn = Math.Max(zn, 0.01); // avoid division by zero

                // Thales' theorem
                cPoint2D i_Point2D = new cPoint2D(xn * DISTANCE / zn,  yn * DISTANCE / zn);

                i_Point2D.md_X = i_Point2D.md_X * md_FactX + md_OffsX;
                i_Point2D.md_Y = i_Point2D.md_Y * md_FactY + md_OffsY;
                return i_Point2D;
            }
        }

        #endregion

        #region cDefault

        /// <summary>
        /// Stores defauls for Rho, Theta, Phi
        /// </summary>
        private class cDefault
        {
            public readonly double Min;
            public readonly double Max;
            public readonly double Default;
            public readonly double MouseFactor;

            public cDefault(double d_Min, double d_Max, double d_Default, double d_MouseFactor)
            {
                Min         = d_Min;
                Max         = d_Max;
                Default     = d_Default;
                MouseFactor = d_MouseFactor;
            }
        }

        #endregion

        // Limits and default values for mouse actions and trackbars.
        // ATTENTION: It is strongly recommended not to change the MIN, MAX values.
        // The mouse factor defines how much mouse movement you need for a change.
        // A movement of mouse by approx 1000 pixels on the screen results in getting from Min to Max or vice versa.
        static readonly cDefault VALUES_RHO   = new cDefault(300,   1800,  1350,    2   );
        static readonly cDefault VALUES_THETA = new cDefault( 10,    170,    70,    0.25); // degree
        static readonly cDefault VALUES_PHI   = new cDefault(  0,    360,   230,    0.4 ); // degree  (continuous rotation)

        // The coordinate axes are 10 % longer than the bounds of the X,Y,Z values
        const double AXIS_EXCESS = 0.1;

        // For any strange reason the graph is not centered vertically
        const int VERT_OFFSET = -30;

        // The maximum distance between mouse pointer and a 2D point to display the tooltip
        // const int TOOLTIP_RADIUS = 6; // np now replaced by myMods.mi_ToolTipRadius

        // The maximum distance between mouse pointer and a 2D point to allow a match when selecting a 3D object.
        // const int SELECT_RADIUS = 3; // np now replaced by myMods.mi_SelectRadius

        // Calculate 3-dimensional Z value from X,Y values
        public delegate double delRendererFunction(double X, double Y);

        // IMPORTANT: Read the detailed comment of function SelectionCallback() at the end of this class.
        public delegate eInvalidate delSelectHandler(eSelEvent e_Event, Keys e_Modifiers, int s32_DeltaX, int s32_DeltaY, cObject3D i_Object);

        Pen[]                        mi_BorderPens      = new Pen[2];
        SolidBrush                   mi_TopLegendBrush  = null;
        eRaster                      me_Raster          = eRaster.Labels;
        cAxis[]                      mi_Axis            = new cAxis[3];
        cMouse                       mi_Mouse           = new cMouse();
        List<cMessgData>             mi_MessageData     = new List<cMessgData>();
        eRecalculate                 me_Recalculate     = eRecalculate.Nothing;
        eNormalize                   me_Normalize       = eNormalize.Separate;
        eLegendPos                   me_LegendPos       = eLegendPos.BottomLeft;
        List<cLine>                  mi_AxisLines       = new List<cLine>();    // 0, 3, or 45 axis lines of coordinate system
        List<cDrawObj>               mi_UserObjects     = new List<cDrawObj>(); // Draw objects from the user (cLine, cShape, cPolygon)
        List<cDrawObj>               mi_AllObjects      = new List<cDrawObj>(); // mi_UserObjects + mi_AxisLines
        cQuadrant                    mi_Quadrant        = new cQuadrant();
        Dictionary<int, cUserInput>  mi_UserInputs      = new Dictionary<int, cUserInput>();
        cUndoImpl                    mi_UndoBuffer;
        cTransform                   mi_Transform;
        cBounds                      mi_Bounds;
        cTooltip                     mi_Tooltip;
        cSelection                   mi_Selection;
        cObject3D                    mi_DragObject;

        #region Properties 

        /// <summary>
        /// See comment of enum eTooltip.
        /// This property can also be set in the Visual Studio Designer
        /// </summary>
        public eTooltip TooltipMode
        {
            get { return mi_Tooltip.Mode;  }
            set { mi_Tooltip.Mode = value; }
        }

        /// <summary>
        /// See comment of enum eLegendPos.
        /// This property can also be set in the Visual Studio Designer
        /// </summary>
        public eLegendPos LegendPos
        {
            get { return me_LegendPos;  }
            set { me_LegendPos = value; }
        }

        /// <summary>
        /// See comment of enum eNormalize.
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public eNormalize Normalize
        {
            get { return me_Normalize;  }
            set 
            { 
                if (me_Normalize != value)
                {
                    me_Normalize = value; 
                    me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }
        }

        /// <summary>
        /// See comment of enum eRaster
        /// This property can also be set in the Visual Studio Designer
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public eRaster Raster
        {
            set
            {
                Debug.Assert(!InvokeRequired); // Call only from GUI thread

                if (me_Raster != value)
                {
                    me_Raster = value;
                    me_Recalculate |= eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }
            get
            {
                return me_Raster;
            }
        }

        /// <summary>
        /// Sets the border color when the 3D Editor does not have the keyboard focus
        /// Setting BorderColor = Color.Empty turns off the border
        /// This change will become visible the next time you call Invalidate()
        /// This property can also be set in the Visual Studio Designer
        /// </summary>
        public Color BorderColorNormal
        {
            set
            {
                Debug.Assert(!InvokeRequired); // Call only from GUI thread

                if (value.A > 0) mi_BorderPens[0] = new Pen(value, 1);
                else             mi_BorderPens[0] = null; // transparent color
            }
            get
            {
                if (mi_BorderPens[0] != null) return mi_BorderPens[0].Color;
                else                          return Color.Empty;
            }
        }

        /// <summary>
        /// Sets the border color when the 3D Editor has the keyboard focus
        /// Setting BorderColorFocus = Color.Empty turns off the highlighting on focus.
        /// This change will become visible the next time you call Invalidate()
        /// This property can also be set in the Visual Studio Designer
        /// </summary>
        public Color BorderColorFocus
        {
            set
            {
                Debug.Assert(!InvokeRequired); // Call only from GUI thread

                if (value.A > 0) mi_BorderPens[1] = new Pen(value, 1);
                else             mi_BorderPens[1] = mi_BorderPens[0];
            }
            get
            {
                if (mi_BorderPens[1] != null) return mi_BorderPens[1].Color;
                else                          return BorderColorNormal;
            }
        }

        /// <summary>
        /// Show a legend with Rotation, Elevation and Distance at the top left
        /// Setting LegendColor = Color.Empty turns off the top legend
        /// This property can also be set in the Visual Studio Designer
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public Color TopLegendColor
        {
            set
            {
                Debug.Assert(!InvokeRequired); // Call only from GUI thread

                mi_TopLegendBrush = new SolidBrush(value);
            }
            get
            {
                if (mi_TopLegendBrush != null) return mi_TopLegendBrush.Color;
                else                           return Color.Empty;
            }
        }

        /// <summary>
        /// returns the total count of loaded draw objects (lines, shapes and polygons)
        /// </summary>
        [Browsable(false)]
        public String ObjectStatistics
        {
            get 
            { 
                int s32_Lines    = 0;
                int s32_Shapes   = 0;
                int s32_Polygons = 0;
                foreach (cDrawObj i_Obj in mi_UserObjects)
                {
                    if (i_Obj is cLine)    s32_Lines ++;
                    if (i_Obj is cShape)   s32_Shapes ++;
                    if (i_Obj is cPolygon) s32_Polygons ++;
                }
                StringBuilder i_Out = new StringBuilder();
                if (s32_Lines    > 0) i_Out.Append(s32_Lines    + " Lines, ");
                if (s32_Shapes   > 0) i_Out.Append(s32_Shapes   + " Shapes, ");
                if (s32_Polygons > 0) i_Out.Append(s32_Polygons + " Polygons, ");
                return i_Out.ToString().TrimEnd(' ', ',');
            }
        }

        /// <summary>
        /// See comments for class cAxis
        /// The properties of the class cAxis can be expanded in the Visual Studio designer
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cAxis AxisX
        {
            get { return mi_Axis[(int)eCoord.X]; }
        }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cAxis AxisY
        {
            get { return mi_Axis[(int)eCoord.Y]; }
        }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cAxis AxisZ
        {
            get { return mi_Axis[(int)eCoord.Z]; }
        }

        /// <summary>
        /// This property controls if and how the user can select draw objects / points
        /// The properties of the class cSelection can be expanded in the Visual Studio designer
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cSelection Selection
        {
            get { return mi_Selection; }
        }

        /// <summary>
        /// This property contains the public methods for the Undo / Redo buffer
        /// The property 'Enabled' can be expanded in the Visual Studio designer
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cUndoBuffer UndoBuffer
        {
            get { return mi_UndoBuffer; }
        }

        #endregion

        /// <summary>
        /// b_ResetOffset = true --> reset the offset that the user has created with SHIFT + moving the 3D object
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public void SetCoefficients(double d_Rho, double d_Theta, double d_Phi, bool b_ResetOffset = true)
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            mi_Mouse.SetRho  (d_Rho);
            mi_Mouse.SetTheta(d_Theta);
            mi_Mouse.SetPhi  (d_Phi);

            if (b_ResetOffset)
            {
                mi_Mouse.mk_OffMove.X = 0;
                mi_Mouse.mk_OffMove.Y = 0;
            }

            mi_Transform.SetCoefficients(mi_Mouse);
        }

        /// <summary>
        /// Convert mouse movement in 2D space back into the 3D space depending on the current rotation angle and Min/Max values.
        /// </summary>
        public cPoint3D ReverseProject(int s32_MouseX, int s32_MouseY)
        {
            double d_FactX = mi_Transform.ReverseProject(mi_Bounds.X.Range, 0.0, 0.0);
            double d_FactY = mi_Transform.ReverseProject(0.0, mi_Bounds.Y.Range, 0.0);
            double d_FactZ = mi_Transform.ReverseProject(0.0, 0.0, mi_Bounds.Z.Range);

            return new cPoint3D(d_FactX * s32_MouseX / 300.0, 
                                d_FactY * s32_MouseX / 300.0, 
                                d_FactZ * s32_MouseY / 300.0);
        }

        /// <summary>
        /// Trackbars are optional for user interaction.
        /// If this function is never called thetrackbars are not used.
        /// </summary>
        public void AssignTrackBars(TrackBar i_Rho, TrackBar i_Theta, TrackBar i_Phi)
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            mi_Mouse.AssignTrackbar(eMouseAction.Rho,   i_Rho,   new EventHandler(OnTrackbarScroll));
            mi_Mouse.AssignTrackbar(eMouseAction.Theta, i_Theta, new EventHandler(OnTrackbarScroll));
            mi_Mouse.AssignTrackbar(eMouseAction.Phi,   i_Phi,   new EventHandler(OnTrackbarScroll));
        }

        /// <summary>
        /// Load one of the three pre-defined input control patterns
        /// </summary>
        public void SetUserInputs(eMouseCtrl e_MouseCtrl)
        {
            List<cUserInput> i_Inputs = new List<cUserInput>();

            switch (e_MouseCtrl)
            {
                case eMouseCtrl.L_Theta_R_Phi:
                    i_Inputs.Add(new cUserInput(MouseButtons.Left,   Keys.None, eMouseAction.Theta));
                    i_Inputs.Add(new cUserInput(MouseButtons.Right,  Keys.None, eMouseAction.Phi));
                    break;
                case eMouseCtrl.L_Theta_L_Phi:
                    i_Inputs.Add(new cUserInput(MouseButtons.Left,   Keys.None, eMouseAction.ThetaAndPhi));
                    break;
                case eMouseCtrl.M_Theta_M_Phi:
                    i_Inputs.Add(new cUserInput(MouseButtons.Middle, Keys.None, eMouseAction.ThetaAndPhi));
                    break;
            }

            i_Inputs.Add(new cUserInput(MouseButtons.Left, Keys.Control,            eMouseAction.Rho));
            i_Inputs.Add(new cUserInput(MouseButtons.Left, Keys.Shift,              eMouseAction.Move));
            i_Inputs.Add(new cUserInput(MouseButtons.Left, Keys.Alt,                eMouseAction.SelectObj));
            i_Inputs.Add(new cUserInput(MouseButtons.Left, Keys.Alt | Keys.Control, eMouseAction.Callback));
            i_Inputs.Add(new cUserInput(MouseButtons.Left, Keys.Alt | Keys.Shift,   eMouseAction.Callback));

            SetUserInputs(i_Inputs.ToArray());
        }

        /// <summary>
        /// Load fully user defined input control patterns.
        /// Each user input must define a unique combination of mouse button and modifier key(s).
        /// </summary>
        public void SetUserInputs(cUserInput[] i_Inputs)
        {
            mi_UserInputs.Clear();
            foreach (cUserInput i_Input in i_Inputs)
            {
                // throws if same UID has already been added
                mi_UserInputs.Add(i_Input.UID, i_Input); 
            }
        }

        // ==================================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public Editor3D()
        {
            // avoid flicker
            SetStyle(ControlStyles.AllPaintingInWmPaint,  true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            mi_Bounds     = new cBounds   (this);
            mi_Transform  = new cTransform(this);
            mi_Tooltip    = new cTooltip  (this);
            mi_Selection  = new cSelection(this);
            mi_UndoBuffer = new cUndoImpl (this);

            // Load the default colors
            BackColor = Color.White;

            mi_Axis[(int)eCoord.X] = new cAxis(this, eCoord.X, Color.DarkBlue);
            mi_Axis[(int)eCoord.Y] = new cAxis(this, eCoord.Y, Color.DarkGreen);
            mi_Axis[(int)eCoord.Z] = new cAxis(this, eCoord.Z, Color.DarkRed);

            mi_BorderPens[0]  = new Pen       (Color.FromArgb(255, 0xB4, 0xB4, 0xB4), 1); // normal  border:  bright gray
            mi_BorderPens[1]  = new Pen       (Color.FromArgb(255, 0x33, 0x99, 0xFF), 1); // focused border: bright cyan
            mi_TopLegendBrush = new SolidBrush(Color.FromArgb(255, 0xC8, 0xC8, 0x96));    // beige

            mi_Transform.SetCoefficients(mi_Mouse);
            SetUserInputs(eMouseCtrl.L_Theta_R_Phi);
        }

        // ==================================================================================

        /// <summary>
        /// Removes all content from the control.
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public void Clear()
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            mi_MessageData.Clear();
            mi_UserObjects.Clear();
            mi_AxisLines  .Clear();
            mi_AllObjects .Clear();
            mi_UndoBuffer .Clear();
            AxisX.Reset();
            AxisY.Reset();
            AxisZ.Reset();
            mi_Mouse.mk_OffMove  = Point.Empty;
            mi_Mouse.mk_OffCoord = Point.Empty;
            me_Recalculate = eRecalculate.Nothing;
        }

        /// <summary>
        /// Adds a message to be shown, even if no 3D data is loaded.
        /// Messages which are null are allowed, they will be skipped.
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public void AddMessageData(params cMessgData[] i_Messages)
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            foreach (cMessgData i_Mesg in i_Messages)
            {
                if (i_Mesg != null)
                    mi_MessageData.Add(i_Mesg);
            }
        }

        /// <summary>
        /// Here you can add cSurfaceData, cScatterData, cPolygonData
        /// RenderData which are null are allowed, they will be skipped.
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public void AddRenderData(params cRenderData[] i_Render)
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            foreach (cRenderData i_Data in i_Render)
            {
                if (i_Data != null)
                {
                    i_Data.AddDrawObjects(this);
                    me_Recalculate |= eRecalculate.AddRemove | eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }
        }

        /// <summary>
        /// This is called from cRenderData.AddDrawObjects()
        /// </summary>
        private void AddDrawObject(cDrawObj i_Obj)
        {
            mi_UserObjects.Add(i_Obj);
            mi_UndoBuffer.DrawObjectAdded(i_Obj);
        }

        /// <summary>
        /// Removes one or multiple 3D objects: cLine3D, cShape3D or cPolygon3D. 
        /// ATTENTION: It is not possible to remove a cPoint3D which is part of one or multiple Lines/Shapes/Polygons
        /// This change will become visible the next time you call Invalidate()
        /// </summary>
        public void RemoveObjects(params cObject3D[] i_Objects)
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            foreach (cObject3D i_Object3D in i_Objects)
            {
                if (i_Object3D is cPoint3D)
                    throw new ArgumentException("You cannot remove a single point. Remove the 3D object that contains the point instead.");

                int s32_Found = -1;
                for (int D=0; D<mi_UserObjects.Count; D++)
                {
                    if (mi_UserObjects[D].mi_Object3D == i_Object3D)
                    {
                        s32_Found = D;
                        break;
                    }
                }

                if (s32_Found < 0)
                {
                    Debug.Assert(false, "There is something wrong in your code. You are removing an object that does not exist.");
                }
                else
                {
                    cDrawObj i_DrawObj = mi_UserObjects[s32_Found];
                    mi_UserObjects.RemoveAt(s32_Found);
                    mi_UndoBuffer.DrawObjectRemoved(i_DrawObj);
                    me_Recalculate |= eRecalculate.AddRemove | eRecalculate.CoordSystem | eRecalculate.Objects;
                }
            }

            mi_UndoBuffer.Store();
        }

        /// <summary>
        /// Search the object at the coordinate X, Y relative to to the upper left corner of the control.
        /// b_OnlyCanSelect = true  --> return only objects that can be selected by the user
        /// b_OnlyCanSelect = false --> return any object at the given location
        /// </summary>
        public cObject3D FindObjectAt(int X, int Y, bool b_OnlyCanSelect)
        {
            X -= (mi_Mouse.mk_OffMove.X + mi_Mouse.mk_OffCoord.X);
            Y -= (mi_Mouse.mk_OffMove.Y + mi_Mouse.mk_OffCoord.Y);
            
            // Search in reverse order. Last drawn objects are in foreground.
            // ATTENTION: mi_UserObjects cannot be used here because it is not sorted --> background polygons would be found.
            for (int i=mi_AllObjects.Count -1; i>=0; i--)
            {
                cDrawObj i_Draw = mi_AllObjects[i];

                if (i_Draw.mi_Object3D == null) 
                    continue; // a coordinate system line

                if (b_OnlyCanSelect && !i_Draw.mi_Object3D.CanSelect) 
                    continue; // user selection disabled for this object

                // MatchesPoint2D() returns a cPoint3D, cLine3D, cShape3D or cPolygon3D.
                cObject3D i_Found = i_Draw.MatchesPoint2D(X, Y);
                if (i_Found != null)
                    return i_Found;
            }
            return null;
        }

        // ========================================== PRIVATE ============================================

        /// <summary>
        /// This function normalizes the 3D ranges for the X,Y,Z coordinates.
        /// Otherwise a 3D range of X,Y from -10 to +10 will appear much smaller than a range from -100 to +100.
        /// It adapts the values so that rotation (phi) goes through the center of the X, Y pane.
        /// </summary>
        private void NormalizeRanges()
        {
            double d_RangeX = mi_Bounds.X.Range;
            double d_RangeY = mi_Bounds.Y.Range;
            double d_RangeZ = mi_Bounds.Z.Range;

            switch (me_Normalize)
            {
                case eNormalize.MaintainXY:
                    double d_RangeXY = (d_RangeX + d_RangeY) / 2.0; // average
                    d_RangeX = d_RangeXY;
                    d_RangeY = d_RangeXY;
                    break;

                case eNormalize.MaintainXYZ:
                    double d_RangeXYZ = (d_RangeX + d_RangeY + d_RangeZ) / 3.0; // average
                    d_RangeX = d_RangeXYZ;
                    d_RangeY = d_RangeXYZ;
                    d_RangeZ = d_RangeXYZ;
                    break;
            }

            mi_Transform.md_NormalizeX = 250.0 / d_RangeX; // Ranges will never be zero.
            mi_Transform.md_NormalizeY = 250.0 / d_RangeY;
            mi_Transform.md_NormalizeZ = 250.0 / d_RangeZ;

            mi_Transform.mi_Center3D.X = (mi_Bounds.X.Max + mi_Bounds.X.Min) / 2.0; // average
            mi_Transform.mi_Center3D.Y = (mi_Bounds.Y.Max + mi_Bounds.Y.Min) / 2.0;
            mi_Transform.mi_Center3D.Z = (mi_Bounds.Z.Max + mi_Bounds.Z.Min) / 2.0;
        }

        /// <summary>
        /// Fills mi_AxisLines with 3 main axis and 42 raster lines
        /// </summary>
        private void CreateCoordinateSystem(Graphics i_Graph)
        {
            mi_Mouse.mk_OffCoord = new Point(0, VERT_OFFSET);
            mi_AxisLines.Clear();

            if (me_Raster == eRaster.Off)
                return;

            cLine i_MainAxisX = new cLine(this, eCoord.X, eCoord.X, eMirror.Z);
            mi_AxisLines.Add(i_MainAxisX);

            i_MainAxisX.mi_Points[0].mi_P3D.X = mi_Bounds.X.Min;
            i_MainAxisX.mi_Points[1].mi_P3D.X = mi_Bounds.X.Max;
            // ------------
            i_MainAxisX.mi_Points[0].mi_P3D.Y = mi_Bounds.Y.Min; // X axis at minimum Y position
            i_MainAxisX.mi_Points[1].mi_P3D.Y = mi_Bounds.Y.Min; // X axis at minimum Y position

            // ---------------------------------------------------

            cLine i_MainAxisY = new cLine(this, eCoord.Y, eCoord.Y, eMirror.Z);
            mi_AxisLines.Add(i_MainAxisY);

            i_MainAxisY.mi_Points[0].mi_P3D.Y = mi_Bounds.Y.Min;
            i_MainAxisY.mi_Points[1].mi_P3D.Y = mi_Bounds.Y.Max;
            // ------------
            i_MainAxisY.mi_Points[0].mi_P3D.X = mi_Bounds.X.Min; // Y axis at minimum X position
            i_MainAxisY.mi_Points[1].mi_P3D.X = mi_Bounds.X.Min; // Y axis at minimum X position
            // ------------
            if (mi_Bounds.Z.Min < 0.0 && mi_Bounds.Z.Max > 0.0) 
                i_MainAxisY.ms_Label = "0"; // label for Z value zero (red)

            // ---------------------------------------------------

            cLine i_MainAxisZ = new cLine(this, eCoord.Z, eCoord.Z, eMirror.None);
            mi_AxisLines.Add(i_MainAxisZ);

            i_MainAxisZ.mi_Points[0].mi_P3D.Z = mi_Bounds.Z.Min;
            i_MainAxisZ.mi_Points[1].mi_P3D.Z = mi_Bounds.Z.Max;
            // ------------
            i_MainAxisZ.mi_Points[0].mi_P3D.X = mi_Bounds.X.Min; // Z axis start at minimum X position
            i_MainAxisZ.mi_Points[1].mi_P3D.X = mi_Bounds.X.Min; // Z axis start at minimum X position
            i_MainAxisZ.mi_Points[0].mi_P3D.Y = mi_Bounds.Y.Min; // Z axis start at minimum Y position
            i_MainAxisZ.mi_Points[1].mi_P3D.Y = mi_Bounds.Y.Min; // Z axis start at minimum Y position

            // ---------------------------------------------------

            foreach (cLine i_Axis in mi_AxisLines)
            {
                i_Axis.Project3D();
                i_Axis.CalcAngle2D(); // required to calculate the quadrant
            }

            // Calculate currently visible quadrant
            mi_Quadrant.Calculate(mi_Mouse.md_Phi, i_MainAxisX, i_MainAxisY, i_MainAxisZ);

            // Add raster lines in 6 different directions
            if (me_Raster >= eRaster.Raster)
            {
                for (int A=0; A<6; A++) // iterate Axes X,Y,Z twice
                {
                    int F = A;
                    int S = A;

                    // Combine X+Y, Y+Z, Z+X, Y+X, Z+Y, X+Z
                    if (A >= 3) F ++;
                    else        S ++;
                    
                    eCoord e_First  = (eCoord)(F % 3);
                    eCoord e_Second = (eCoord)(S % 3);

                    // Define which mirror operations are allowed for this raster line
                    eMirror e_Mirror = eMirror.None;
                    if (e_Second == eCoord.X) e_Mirror = eMirror.X;
                    if (e_Second == eCoord.Y) e_Mirror = eMirror.Y;
                    if (e_Second == eCoord.Z) e_Mirror = eMirror.Z;

                    cLine i_FirstLine = mi_AxisLines[(int)e_First];  // Main axis
                    cLine i_SecndLine = mi_AxisLines[(int)e_Second]; // Main axis

                    double d_SecndStart = i_SecndLine.mi_Points[0].mi_P3D.GetValue(e_Second);
                    double d_SecndEnd   = i_SecndLine.mi_Points[1].mi_P3D.GetValue(e_Second);

                    // Distance between raster lines
                    double d_Interval = CalculateInterval(d_SecndEnd - d_SecndStart);

                    int s32_Start = (int)(d_SecndStart / d_Interval) - 1;
                    int s32_End   = (int)(d_SecndEnd   / d_Interval) + 1;

                    for (int L=s32_Start; L<s32_End; L++) // iterate raster lines
                    {
                        double d_Offset = d_Interval * L;

                        if (d_Offset < d_SecndStart || d_Offset > d_SecndEnd) 
                            continue;

                        cLine i_Raster = new cLine(this, e_First, e_Second, e_Mirror);
                            
                        i_Raster.mi_Points[0].mi_P3D = i_FirstLine.mi_Points[0].mi_P3D.Clone();
                        i_Raster.mi_Points[1].mi_P3D = i_FirstLine.mi_Points[1].mi_P3D.Clone();

                        i_Raster.mi_Points[0].mi_P3D.SetValue(e_Second, d_Offset);
                        i_Raster.mi_Points[1].mi_P3D.SetValue(e_Second, d_Offset);

                        i_Raster.ms_Label = FormatDouble(d_Offset);

                        // Do not draw the raster line at value zero if it equals the main axis and the axis is not mirrored
                        if (L == 0 && !mi_Axis[(int)e_Second].Mirror && i_Raster.CoordEquals(mi_AxisLines[(int)e_First]))
                            continue;

                        if ((e_First == eCoord.X && e_Second == eCoord.Z) || // Blue
                            (e_First == eCoord.Z && e_Second == eCoord.X))
                        {
                            i_Raster.md_Sort = mi_Quadrant.md_SortXZ;
                        }
                        else if ((e_First == eCoord.Z && e_Second == eCoord.Y) || // Green
                                 (e_First == eCoord.Y && e_Second == eCoord.Z))
                        {
                            i_Raster.md_Sort = mi_Quadrant.md_SortYZ;
                        }
                        else // X + Y Red
                        {
                            i_Raster.md_Sort = mi_Quadrant.md_SortXY;

                            // Special case: XY raster lines must be shifted down to negative end of Z axis
                            i_Raster.mi_Points[0].mi_P3D.Z = i_MainAxisZ.mi_Points[0].mi_P3D.Z;
                            i_Raster.mi_Points[1].mi_P3D.Z = i_MainAxisZ.mi_Points[0].mi_P3D.Z;
                        }

                        i_Raster.Project3D();
                        mi_AxisLines.Add(i_Raster);
                    } // for (L)
                } // for (A)
            } // if (Raster)

            // Remove the green and blue main axes if Z value zero is outside the visible range
            if (mi_Bounds.Z.Min > 0.0 || mi_Bounds.Z.Max < 0.0) 
            {
                mi_AxisLines.Remove(i_MainAxisX);
                mi_AxisLines.Remove(i_MainAxisY);
            }

            // Move the graph to the left when labels are enabled
            if (me_Raster == eRaster.Labels)
            {
                int s32_LabelWidth = 0;
                foreach (cLine i_Line in mi_AxisLines)
                {
                    if (i_Line.me_Line == eCoord.Y && i_Line.me_Offset == eCoord.Z)
                    {
                        SizeF  k_Size  = i_Graph.MeasureString(i_Line.ms_Label, Font);
                        s32_LabelWidth = Math.Max(s32_LabelWidth, (int)k_Size.Width);
                    }
                }
                mi_Mouse.mk_OffCoord.X -= s32_LabelWidth / 2;
            }
        }

        /// <summary>
        /// Makes a color brigther
        /// </summary>
        private static Color BrightenColor(Color c_Color)
        {
            int s32_Red   = c_Color.R + (255 - c_Color.R) / 2;
            int s32_Green = c_Color.G + (255 - c_Color.G) / 2;
            int s32_Blue  = c_Color.B + (255 - c_Color.B) / 2;

            return Color.FromArgb(255, s32_Red, s32_Green, s32_Blue);
        }

        /// <summary>
        /// returns intervals of  0.1, 0.2, 0.5,  1, 2, 5,  10, 20, 50,  etc...
        /// The count of intervals which fit into the range is always between 5 and 10
        /// </summary>
        private static double CalculateInterval(double d_Range)
        {
            double d_Factor = Math.Pow(10.0, Math.Floor(Math.Log10(d_Range)));
            if (d_Range / d_Factor >= 5.0)
                return d_Factor;
            else if (d_Range / (d_Factor / 2.0) >= 5.0)
                return d_Factor / 2.0;
            else
                return d_Factor / 5.0;
        }

        // md_Label = 123.000 --> display "123"
        // md_Label =  15.700 --> display "15.7"  
        // md_Label =   4.260 --> display "4.26"
        // md_Label =   0.834 --> display "0.834"
        public static String FormatDouble(double d_Label)
        {
            return d_Label.ToString("0.000", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }

        /// <summary>
        /// For debugging drawing speed.
        /// ATTENTION: The first time the delays are wrong because of JIT compilation delays.
        /// </summary>
        private static void FormatStopwatch(String s_Measure, Stopwatch i_Watch, StringBuilder i_Debug)
        {
            double d_Elapsed = (double)i_Watch.ElapsedTicks / TimeSpan.TicksPerMillisecond;
            i_Debug.AppendLine(s_Measure.PadRight(17) + d_Elapsed.ToString("0.000") + " ms");
            i_Watch.Restart();
        }

        /// <summary>
        /// Checks if the 2D point X,Y lies on the line between i_Start and i_End within s32_MaxDist
        /// All coordinates in pixels.
        /// </summary>
        private static bool IsPointOnLine(cPoint2D i_Start, cPoint2D i_End, int X, int Y, int s32_MaxDist)
        {
            double d_DeltaX = i_End.md_X - i_Start.md_X;
            double d_DeltaY = i_End.md_Y - i_Start.md_Y;

            int s32_StepsX = Math.Abs((int)d_DeltaX);
            int s32_StepsY = Math.Abs((int)d_DeltaY);
            int s32_Steps  = Math.Max(s32_StepsX, s32_StepsY);

            d_DeltaX /= s32_Steps;
            d_DeltaY /= s32_Steps;

            cPoint2D i_Point = i_Start.Clone();
            for (int S=0; S<=s32_Steps; S++)
            {
                if (i_Point.CalcDistanceTo(X, Y) <= s32_MaxDist)
                    return true;

                i_Point.md_X += d_DeltaX;
                i_Point.md_Y += d_DeltaY;
            }
            return false;
        }

        // =================================== DRAWING =====================================

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Background is painted flickerless in OnPaint()
        }

        /// <summary>
        /// This is invoked by Invalidate() when the GUI thread becomes idle.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Stupidly the .NET framework draws a red cross if any exception occurres in OnPaint()
            try
            {
                Render(e.Graphics);
            }
            catch (Exception Ex)
            {
                e.Graphics.ResetTransform();
                e.Graphics.Clear(Color.DarkRed);
                e.Graphics.DrawString(Ex.Message + "\n" + Ex.StackTrace, new Font("Verdana", 8, FontStyle.Bold), Brushes.White, 10, 10);
                return;
            }

            DrawBorder(e.Graphics);
        }

        public Bitmap GetScreenshot()
        {
            Debug.Assert(!InvokeRequired); // Call only from GUI thread

            Bitmap i_Bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (Graphics i_Graph = Graphics.FromImage(i_Bmp))
            {
                Render(i_Graph);
            }
            return i_Bmp;
        }

        private void Render(Graphics i_Graph)
        {
            i_Graph.Clear(BackColor);

            foreach (cMessgData i_Mesg in mi_MessageData)
            {
                i_Mesg.Draw(i_Graph, ClientRectangle);
            }

            // If there are no 3D objects --> only show the messages. Do not show an empty coordinate system.
            if (mi_UserObjects.Count == 0)
                return;

            mi_UndoBuffer.Init();

            #if DEBUG_SPEED
                StringBuilder i_Debug = new StringBuilder();
                Stopwatch     i_Watch = new Stopwatch();
                i_Debug.Append("--------------------------\n");
                i_Watch.Start();
            #endif

            // Speed optimization: Add points to tooltip only if required
            if ((me_Recalculate & eRecalculate.AddRemove) > 0)
            {
                mi_Tooltip.Clear();
                foreach (cDrawObj i_Object in mi_UserObjects)
                {
                    foreach (cPoint i_Point in i_Object.mi_Points)
                    {
                        mi_Tooltip.AddPoint(i_Point);
                    }
                }

                #if DEBUG_SPEED
                    FormatStopwatch("Add Tooltip: ", i_Watch, i_Debug);
                #endif
            }

            // Speed optimization: Calculate coordinate system only if required
            if ((me_Recalculate & eRecalculate.CoordSystem) > 0)
            {
                // Calculate Min/Max for all UserObjects, assign mi_Inst to all user objects.
                mi_Bounds.Calculate();

                // Calculate factors for transformation
                NormalizeRanges();

                // Fills mi_AxisLines with 3 main axis and 42 raster lines
                CreateCoordinateSystem(i_Graph);

                #if DEBUG_SPEED
                    FormatStopwatch("Coord System: ", i_Watch, i_Debug);
                #endif
            }

            // Speed optimization: Calculate 3D objects only if required
            if ((me_Recalculate & eRecalculate.Objects) > 0)
            {
                foreach (cDrawObj i_Object in mi_UserObjects)
                {
                    // This must not be called for axes
                    i_Object.CalcSortOrder();

                    // This must not be called for axes
                    // reload Pens, Brushes from User objects or from ColorScheme
                    i_Object.ProcessColors();

                    // Project 3D --> 2D, calculate line width, shape radius,...
                    i_Object.Project3D();
                }

                #if DEBUG_SPEED
                    FormatStopwatch("Prepare Objects: ", i_Watch, i_Debug);
                #endif
            }

            // Speed optimization: Merge lists only if at least one of them has changed
            if ((me_Recalculate & (eRecalculate.AddRemove | eRecalculate.CoordSystem)) > 0)
            {
                mi_AllObjects.Clear();
                mi_AllObjects.AddRange(mi_AxisLines);
                mi_AllObjects.AddRange(mi_UserObjects);

                #if DEBUG_SPEED
                    FormatStopwatch("Merge Lists: ", i_Watch, i_Debug);
                #endif
            }

            // Speed optimization: sort draw objects only if required
            if ((me_Recalculate & (eRecalculate.AddRemove | eRecalculate.CoordSystem | eRecalculate.Objects)) > 0)
            {
                // Sort draw order from background to foreground
                mi_AllObjects.Sort();

                #if DEBUG_SPEED
                    FormatStopwatch("Sort List: ", i_Watch, i_Debug);
                #endif
            }

            me_Recalculate = eRecalculate.Nothing;

            // ---------------------------------------------------

            // Draw axis legends in bottom left corner
            if (me_LegendPos == eLegendPos.BottomLeft)
            {
                int X = 4;
                int Y = ClientSize.Height - Font.Height - 4;
                for (int i=2; i>=0; i--)
                {
                    if (String.IsNullOrEmpty(mi_Axis[i].LegendText))
                        continue;

                    String s_Disp = String.Format("{0}: {1}", (eCoord)i, mi_Axis[i].LegendText);
                    i_Graph.DrawString(s_Disp, Font, mi_Axis[i].LegendBrush, X,  Y);
                    Y -= Font.Height;
                }
            }

            // Draw rotation legends at top
            if (mi_TopLegendBrush != null)
            {
                String[] s_Legend = new String[] { "Rotation:", "Elevation:", "Distance:" };
                String[] s_Value  = new String[] { String.Format("{0:+#;-#;0}°", (int)mi_Mouse.md_Phi),
                                                   String.Format("{0:+#;-#;0}°", (int)mi_Mouse.md_Theta),
                                                   String.Format("{0}",          (int)mi_Mouse.md_Rho) };

                SizeF k_Size = i_Graph.MeasureString(s_Legend[1], Font); // measure the widest string
                int X = 4;
                int Y = 3;
                for (int i=0; i<3; i++)
                {
                    i_Graph.DrawString(s_Legend[i], Font, mi_TopLegendBrush, X,  Y);
                    i_Graph.DrawString(s_Value [i], Font, mi_TopLegendBrush, X + k_Size.Width, Y);
                    Y += Font.Height;
                }
            }

            // ---------------------------------------------------

            // Set X, Y offset which user has set by mouse dragging with SHIFT key pressed
            i_Graph.TranslateTransform(mi_Mouse.mk_OffMove.X + mi_Mouse.mk_OffCoord.X, 
                                       mi_Mouse.mk_OffMove.Y + mi_Mouse.mk_OffCoord.Y);

            SmoothingMode e_Smooth = SmoothingMode.Invalid;

            foreach (cDrawObj i_DrawObj in mi_AllObjects)
            {
                if (!i_DrawObj.IsValid)
                    continue; // avoid overflow exception or hanging

                if (e_Smooth != i_DrawObj.me_SmoothMode) // avoid unneccessary calls into GDI+ (speed optimization)
                {
                    e_Smooth              = i_DrawObj.me_SmoothMode;
                    i_Graph.SmoothingMode = i_DrawObj.me_SmoothMode;
                }

                // Draw Line, Shape, Polygon
                i_DrawObj.Render(i_Graph);

                // Draw labels and legends
                cLine i_Line = i_DrawObj as cLine;
                if (i_Line         != null             &&
                    i_Line.me_Line != eCoord.Invalid   &&
                    mi_Quadrant.mb_BottomView == false && // no label in bottom view
                    mi_Quadrant.ms32_Quadrant == 3)       // showing labels makes sense only in quadrant 3 
                {
                    bool b_Legend = false;

                    // Draw axis legends at end of of main axis
                    if (me_LegendPos == eLegendPos.AxisEnd && i_Line.me_Line == i_Line.me_Offset)
                    {
                        cAxis i_Axis = mi_Axis[(int)i_Line.me_Line];
                        if (!String.IsNullOrEmpty(i_Axis.LegendText))
                        {
                            StringFormat i_Align = new StringFormat();
                            PointF       k_Pos   = i_Line.mi_Points[1].mi_P2D.Coord;
                            switch (i_Line.me_Line)
                            {
                                case eCoord.X:
                                    k_Pos.X += (float)mi_Transform.ProjectXY(5, -5);
                                    k_Pos.Y += (float)mi_Transform.ProjectXY(5, -Font.Height / 2 - 2);
                                    i_Align.Alignment = StringAlignment.Far;
                                    break;
                                case eCoord.Y:
                                    k_Pos.X += 5;
                                    k_Pos.Y -= Font.Height / 2;
                                    break;
                                case eCoord.Z:
                                    k_Pos.X -= 5;
                                    k_Pos.Y -= Font.Height + 5;
                                    break;
                            }

                            i_Graph.DrawString(i_Axis.LegendText, Font, i_Axis.LegendBrush, k_Pos, i_Align);
                            b_Legend = true; // do not draw a label if a legend has already been drawn (see Demo Sphere)
                        }
                    }
                    
                    // Draw labels of raster lines
                    if (me_Raster == eRaster.Labels && !b_Legend && !String.IsNullOrEmpty(i_Line.ms_Label))
                    {
                        Brush        i_Brush = null;
                        StringFormat i_Align = new StringFormat();
                        PointF       k_Pos   = i_Line.mi_Points[1].mi_P2D.Coord;

                        if (i_Line.me_Line == eCoord.Y)
                        {
                            if (i_Line.me_Offset == eCoord.X)
                            {
                                k_Pos.X += (float)mi_Transform.ProjectXY(5, -5);
                                k_Pos.Y += (float)mi_Transform.ProjectXY(-Font.Height / 2, 5);
                                i_Brush = AxisX.LegendBrush;
                            }
                            else // Y (Main axis) and Z (Raster)
                            {
                                k_Pos.X += 5;
                                k_Pos.Y -= Font.Height / 2;
                                i_Brush = AxisZ.LegendBrush;
                            }
                        }
                        else if (i_Line.me_Line == eCoord.X && i_Line.me_Offset == eCoord.Y)
                        {
                            k_Pos.X += (float)mi_Transform.ProjectXY(5, -5);
                            k_Pos.Y += (float)mi_Transform.ProjectXY(5, -Font.Height / 2);
                            i_Align.Alignment = StringAlignment.Far;
                            i_Brush = AxisY.LegendBrush;
                        }

                        if (i_Brush != null)
                            i_Graph.DrawString(i_Line.ms_Label, Font, i_Brush, k_Pos, i_Align);
                    }
                } // if (Line != null)
            } // foreach (cDrawObj)

            #if DEBUG_SPEED
                FormatStopwatch("Render Objects: ", i_Watch, i_Debug);
                Debug.Print(i_Debug.ToString().TrimEnd());
            #endif
        }

        // ============================================================================

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // This control draws it's own border. See DrawBorder()
            BorderStyle = BorderStyle.None;

            // This is the size of the control defined in Visual Studio Form Designer
            //mi_Transform.SetInitialSize(ClientSize); // np

            // np, its better for me this way
            mi_Transform.SetInitialSize(Init.Graph3dInitialSize);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            // This may be called with an invalid size before the control is created!
            mi_Transform.SetSize(ClientSize);
            Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
        }

        /// <summary>
        /// This is only called when the user moves the trackbar, not when TrackBar.Value is set programmatically.
        /// </summary>
        void OnTrackbarScroll(object sender, EventArgs e)
        {
            mi_Mouse.OnTrackBarScroll();
            mi_Transform.SetCoefficients(mi_Mouse);
            Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
        }

        // --------------------------------------------

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            DrawBorder(null);
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            DrawBorder(null);
        }
        /// <summary>
        /// Draw a one pixel border around the control which may change color when the control has the focus.
        /// </summary>
        private void DrawBorder(Graphics i_Graphics)
        {
            Pen i_Pen = mi_BorderPens[Focused ? 1 : 0];
            if (i_Pen != null)
            {
                BorderStyle = BorderStyle.None;

                if (i_Graphics == null)
                    i_Graphics = Graphics.FromHwnd(Handle);

                i_Graphics.ResetTransform();
                Rectangle r_Rect = ClientRectangle;
                i_Graphics.DrawRectangle(i_Pen, r_Rect.X, r_Rect.Y, r_Rect.Width - 1, r_Rect.Height - 1);
            }
        }

        // ============================== MOUSE =====================================

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            mi_Tooltip.Hide();
            mi_Mouse.mk_LastPos = e.Location;

            if (mi_AllObjects.Count == 0)
                return;

            // np, modified for point move mode
            //int s32_UID = (int)Control.ModifierKeys | (int)e.Button;

            int s32_UID;

            if (PointMoveMode)
            {
                s32_UID = (int)Keys.Control | (int)Keys.Alt | (int)e.Button;
            }
            else if (PointSelectMode)
            {
                s32_UID = (int)Keys.Alt | (int)e.Button;
            }
            else // default graph3d operation
            {
                s32_UID = (int)Control.ModifierKeys | (int)e.Button;
            }

            cUserInput i_Input;
            if (mi_UserInputs.TryGetValue(s32_UID, out i_Input))
            {
                switch (i_Input.Action)
                {
                    case eMouseAction.SelectObj:
                    case eMouseAction.Callback:
                        OnSelMouseDown(e.X, e.Y, i_Input);
                        break;

                    default:
                        mi_Mouse.me_Action = i_Input.Action;
                        Cursor             = i_Input.Cursor;
                        break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int s32_DeltaX = e.X - mi_Mouse.mk_LastPos.X;
            int s32_DeltaY = e.Y - mi_Mouse.mk_LastPos.Y;
            mi_Mouse.mk_LastPos = e.Location;

            switch (mi_Mouse.me_Action)
            {
                case eMouseAction.Move:
                    mi_Tooltip.Hide();
                    mi_Mouse.mk_OffMove.X += s32_DeltaX;
                    mi_Mouse.mk_OffMove.Y += s32_DeltaY;
                    Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
                    break;

                case eMouseAction.Rho:
                case eMouseAction.Theta:
                case eMouseAction.Phi:
                case eMouseAction.ThetaAndPhi:
                    mi_Tooltip.Hide();
                    mi_Mouse.OnMouseMove(s32_DeltaX, s32_DeltaY);
                    mi_Transform.SetCoefficients(mi_Mouse);
                    Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
                    break;

                case eMouseAction.SelectObj:
                case eMouseAction.Callback:
                    //int s32_UID = (int)Control.ModifierKeys | (int)e.Button; // np

                    int s32_UID;

                    if (PointMoveMode) // np
                    {
                        s32_UID = (int)Keys.Control | (int)Keys.Alt | (int)e.Button;
                    }
                    else if (PointSelectMode)
                    {
                        s32_UID = (int)Keys.Alt | (int)e.Button;
                    }
                    else // default graph3d operation
                    {
                        s32_UID = (int)Control.ModifierKeys | (int)e.Button;
                    }

                    cUserInput i_Input;
                    if (mi_UserInputs.TryGetValue(s32_UID, out i_Input))
                    {
                        if (i_Input.Action == mi_Mouse.me_Action)
                        {
                            // Mouse.Y  coordinates have the zero point at top left
                            // Editor3D coordinates have the zero point at bottom left --> negate Y 
                            SelectionCallback(eSelEvent.MouseDrag, i_Input.Modifiers, s32_DeltaX, -s32_DeltaY, mi_DragObject);
                        }
                        else 
                        {
                            // The modifier keys have changed --> abort sending events to the callback
                            OnMouseExit(); 
                        }
                    }
                    break;

                case eMouseAction.None:
                    mi_Tooltip.OnMouseMove(e);
                    break;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            OnMouseExit();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            OnMouseExit();
        }
        private void OnMouseExit()
        {
            mi_Tooltip.Hide();
            Cursor = Cursors.Arrow;

            switch (mi_Mouse.me_Action)
            {
                case eMouseAction.SelectObj:
                case eMouseAction.Callback:
                    SelectionCallback(eSelEvent.MouseUp, Keys.None, 0, 0, mi_DragObject);
                    break;
            }

            mi_DragObject = null;
            mi_Mouse.me_Action = eMouseAction.None;

            // Store any pending user changes on mouse-up
            mi_UndoBuffer.Store();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            mi_Tooltip.Hide();

            if (mi_Mouse.OnMouseWheel(e.Delta))
            {
                mi_Transform.SetCoefficients(mi_Mouse);
                Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
            }
        }

        /// <summary>
        /// Select 3D object or call selection callback function
        /// </summary>
        private void OnSelMouseDown(int X, int Y, cUserInput i_Input)
        {
            if (!mi_Selection.Enabled)
                return;

            cObject3D i_Found = FindObjectAt(X, Y, true);

            if (mi_Selection.Callback != null)
            {
                // Start dragging even if i_Found == null
                mi_DragObject      = i_Found;
                mi_Mouse.me_Action = i_Input.Action;
                Cursor             = i_Input.Cursor;

                SelectionCallback(eSelEvent.MouseDown, i_Input.Modifiers, 0, 0, mi_DragObject);
                return;
            }

            // No callback assigned --> toggle selection of 3D object.
            if (i_Found != null && i_Input.Action == eMouseAction.SelectObj)
            {
                // Not multiselect --> remove all current selections
                if (!mi_Selection.MultiSelect)
                     mi_Selection.DeSelectAll();

                i_Found.Selected = !i_Found.Selected; // toggle selection

                Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
            }
        }

        // ============================== KEYBOARD =====================================

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control && !e.Alt && !e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.Y: mi_UndoBuffer.Redo(); break; // CTRL + Y --> Redo
                    case Keys.Z: mi_UndoBuffer.Undo(); break; // CTRL + Z --> Undo
                }
            }
        }

        // ========================== SELECTION CALLBACK ===============================

        /// <summary>
        /// Selection.Callback is called on the mouse events Down, Move and Up if cUserInput.Action = Callback or SelectObj
        /// The callback must never throw an exception.
        /// i_Object may be cPoint3D                       if Selection.SinglePoints = true
        /// i_Object may be cShape3D, cLine3D, cPolygon3D  if Selection.SinglePoints = false
        /// i_Object may be null if the user has clicked a location without a 3D object.
        /// In this case the callback can call Selection.GetSelectedObjects() / GetSelectedPoints() to obtain the previous selections.
        /// The callback is responsible for selecting / deselecting the desired objects.
        /// If the callback does not change the selection status, the 3D object will never be selected / deselected.
        /// The callback is allowed to show a MessageBox to the user.
        /// </summary>
        private void SelectionCallback(eSelEvent e_Event, Keys e_Modifiers, int s32_DeltaX, int s32_DeltaY, cObject3D i_Object)
        {
            if (!mi_Selection.Enabled || mi_Selection.Callback == null)
                return;
            try
            {
                eInvalidate e_Invalidate = mi_Selection.Callback(e_Event, e_Modifiers, s32_DeltaX, s32_DeltaY, i_Object);

                if (e_Invalidate == eInvalidate.CoordSystem)
                    me_Recalculate |= eRecalculate.CoordSystem;

                if (e_Invalidate != eInvalidate.NoChange)
                    Invalidate(); // Windows will call OnPaint() when the GUI thread becomes idle.
            }
            catch (Exception Ex)
            {
                MessageBox.Show(TopLevelControl, "Your callback function has crashed:\n\n" + Ex.Message + "\n\n" + Ex.StackTrace, 
                                "Bug Alarm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}