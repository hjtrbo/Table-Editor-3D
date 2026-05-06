# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

Open `Table Editor 3D.sln` in Visual Studio, or use the .NET CLI:

```
dotnet build "Table Editor 3D.csproj"
dotnet run --project "Table Editor 3D.csproj"
```

- Target framework: **.NET Framework 4.8**, C# 10.0
- Output type: WinExe — runs the `Demo` form as the application host
- No test project; manual testing is done by running the Demo form with sample data (Book1.xlsx, Book2.xlsx)

## Architecture

This is a **Windows Forms desktop control library** for 3D data table visualization and editing, targeted at automotive ECU tuning software (HP Tuners / EFI Live style).

### Core components

**`TableEditor3D`** (`01_TableEditor3D.cs`) — the primary reusable `UserControl`. It is the only deliverable artifact; everything else supports it or demos it. It contains:
- A `DataGridView` for tabular editing (with undo/redo, copy/paste, smoothing, interpolation)
- An embedded `Editor3D` 3D graph pane (toggleable via `Graph3dEnabled`)
- A toolbar, custom scrollbars, and a color-coded heat map overlay
- A rich property surface for host-app configuration (rotation, elevation, zoom, colour scheme, selection radius, etc.)
- Public enums: `CopyPasteMode`, `RefreshMode`, `FormatTarget`, `DpDirection`, `ColourScheme`

**`Editor3D`** (`06_Graph3D.cs`) — the 3D rendering engine, adapted from Elmue's CodeProject library. Lives in the `TableEditor.Plot3D` namespace. Key enums: `eColorScheme`, `eRaster`, `eNormalize`. Not modified frequently — treat as a stable dependency.

**`Demo`** (`Demo.cs`) — sample host application. Creates four `TableEditor3D` instances (`nateDogg`, `snoopDogg`, `iceCube`, `eminem`) in a tabbed interface to exercise different configurations.

**Support forms** — `AverageTool` (04), `UserSettingsDialog` (03), `Graph3D_Instructions` (02), `PasteWithXAxisPcmtecDialog` (05) — launched by `TableEditor3D` internally.

### Key design patterns

- Files are numbered by dependency order (`00_Program` → `06_Graph3D`).
- Heavy use of `#region` blocks inside the large files.
- `01_TableEditor3D.cs` is ~500 KB and intentionally monolithic — the control surface is intentionally self-contained.
- User preferences (rotation, elevation, zoom, colors, etc.) are persisted via `Properties.Settings` (`Properties/Settings.settings`).
- The `Editor3D` source uses `c_` / `e_` / `m_` member prefixes per its original coding style — keep that convention inside `06_Graph3D.cs`.

### Excluded directories

Do **not** search or read files under `bin\` or `obj\` unless you are specifically fault-finding a build error or verifying whether stale/outdated build artifacts exist.

### Off-limits

**`06_Graph3D.cs`** is **off-limits** — treat it as a stable vendored dependency adapted from Elmue's CodeProject library. Do not rename identifiers, reformat, or restructure anything inside it. It uses the `c_` / `e_` / `m_` member-prefix style of its origin — that convention is intentional and must not be normalised to the rest of the project.

### Embedding the control in a host app

Add `TableEditor3D` to a form, then configure via properties:
```csharp
myControl.Graph3dEnabled = true;
myControl.AllowUndo = true;
myControl.AllowCopyPaste = true;
myControl.AllowAveraging = true;
```
Load data by setting the `DataSource` / axis label arrays as documented in the Demo form.
