# AI Multi-Tool Kit FUI Importer

Editor-only Unity Package Manager package for importing `.fui` files exported from the AI Multi-Tool Kit Figma plugin.

## Install from Git

The repository root must contain `package.json`.

Use Unity Package Manager:

```text
Window → Package Manager → + → Install package from git URL
```

Then paste:

```text
https://github.com/kraupins/FUI_Importer.git
```

If you keep the package inside a subfolder instead of the repository root, install with:

```text
https://github.com/kraupins/FUI_Importer.git?path=/FUI_Importer
```

## Import

Open:

```text
Tools → AI Multi-Tool Kit → FUI Importer
```

Select a `.fui` file and click **Import .fui to UI Toolkit**.

The importer creates:

```text
Assets/FUI_Imported/<ProjectName>/
  UI/*.uxml
  UI/*.uss
  Textures/*
  Fonts/*
  PanelSettings/<ProjectName>_PanelSettings.asset
  Prefabs/*.prefab
  Source/*.json
  fui-import-report.json
```

It also creates scene objects in the open scene by default:

```text
FUI_<ProjectName>_Screens
  <FirstScreen>      active
  <OtherScreens>     inactive
```

All generated objects use Unity's built-in `UIDocument`, `PanelSettings`, `VisualTreeAsset`, UXML, USS, and regular textures/fonts. After import, you can remove this package and the generated UI will keep working.

## Context menu

You can also select a `.fui` asset in the Project window and run:

```text
Assets → AI Multi-Tool Kit → Import selected .fui
```

