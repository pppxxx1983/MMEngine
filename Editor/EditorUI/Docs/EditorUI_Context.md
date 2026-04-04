# PlayableFramework EditorUI Context

## Current Result

- Design history is stored separately in `EditorUI_History.md`.

- `PlayableFramework/Editor/EditorUI`
  All EditorUI code is placed here.

- `Nodes`
  Node-related files.

- `Points`
  Point-related files.

- `Lines`
  Line-related files.

- `EditorUIWindow`
  Main window.
  Menu path: `Tools/PlayableFramework/Editor UI`

- `UIManager`
  Singleton window entry.
  Use `UIManager.Instance`.
  Owns global single-instance UI objects.
  Owns `root`
  Owns `canvas`
  Owns `line`
  Owns `selectionBox`
  Owns preview-line state.
  Global mouse move updates the line.
  Global mouse up stops the line.

- `EditorUIDefaults`
  Shared default values are centralized here.

- `EditorUITypeMenu`
  Standalone create menu for `Service` and `Data`.

- `SceneNodeFactory`
  On create:
  ensures `Root`
  ensures `ResourceCenter`
  ensures `GlobalContext`
  ensures `GlobalAudioManager`
  assigns `root.resourceCenter`
  assigns `root.audioManager`
  creates `Graph` last
  if a service type implements `ISpecialNode`, its node object is created under `Graph/Special`
  creates the scene node object and adds the selected service component

- `ISpecialNode`
  Stores:
  `EnterId`
  `NextId`

- `NodeManager`
  Manages `NodeData`.
  No default sample node is created.

- `NodeData`
  Pure node data.
  Stores:
  `Title`
  `Position`
  `IsSelected`
  `NodeBorderState`

- `NodeBorderState`
  `Default`
  `Selected`
  `Running`
  `Completed`

- `UINode : VisualElement`
  Node view control.
  Uses `UINode.uss`.

- `UINode.uss`
  Shared node style file under `Styles`.
  Node border color changes by state.

- `HLayout`
  Base horizontal layout.
  Default:
  expand
  stretch
  center align
  debug border on

- `VLayout`
  Base vertical layout.
  Default:
  expand
  stretch
  center align
  debug border on

- `NodeLayout`
  Node internal layout container.

- `Line`
  Preview line control.
  Draws in canvas local coordinates.

- `LinkPoint`
  Only contains the point itself.
  No label text inside.
  `SetReverseOrder(true)` = left side
  `SetReverseOrder(false)` = right side
  `toggle` mouse down only starts line mode.

- `EnterNextPoint`
  Composite control for `Enter` and `Next`.
  Labels live here.
  `SetMirror(bool)` swaps left/right arrangement.

- `ControlDebugHelper`
  Controls debug borders.

## Coding Rules

- Do not add meaningless wrapper functions.
- Do not use `if (...) { ... return; }` when the real structure is `if / else`.
- Prefer direct `if / else`.
- Global single-instance UI objects must live in `UIManager`.
- Do not pass global single-instance UI objects through parameters.
- Use `UIManager.Instance.xxx` directly.
- Text stays vertically centered by default.
- Only change horizontal alignment when needed.

## Keywords

`EditorUI`
`UIManager.Instance`
`EditorUIWindow`
`EditorUITypeMenu`
`SceneNodeFactory`
`NodeManager.Instance`
`NodeData`
`NodeBorderState`
`UINode`
`UINode.uss`
`HLayout`
`VLayout`
`NodeLayout`
`Line`
`LinkPoint`
`EnterNextPoint`
`SetMirror`
`ISpecialNode`

## Short Background For Next Chat

```text
PlayableFramework EditorUI code is under PlayableFramework/Editor/EditorUI.
EditorUIWindow is the main window, opened from Tools/PlayableFramework/Editor UI.
UIManager is the singleton window entry.
Global single-instance UI objects live in UIManager.
EditorUITypeMenu is the standalone Service/Data create menu.
SceneNodeFactory creates scene nodes and ensures Root, ResourceCenter, GlobalContext, GlobalAudioManager, then Graph last, and assigns root.resourceCenter/root.audioManager.
NodeManager manages NodeData and does not create a default sample node.
NodeData is pure data and includes NodeBorderState.
UINode is the node view control and uses UINode.uss.
HLayout and VLayout are shared base layouts with expand/stretch/center/debug-border defaults.
Line draws the preview line in canvas local coordinates.
UIManager owns the preview-line state.
Do not pass global single-instance UI objects through parameters.
LinkPoint toggle mouse down starts line mode.
LinkPoint only keeps the point itself.
EnterNextPoint owns the Enter/Next labels and supports SetMirror(bool).
Do not add meaningless wrapper functions.
Prefer explicit if/else.
Text stays vertically centered by default.
```
