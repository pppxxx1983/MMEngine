# PlayableFramework EditorUI Context

## Current Result

- Design history is stored separately in `EditorUI_History.md`.
- `EditorUI_Context.md` stores deduction results only.
- After every EditorUI chat, keep this file updated with the latest stable result.

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
  Supports window-local `Ctrl+S` to save node positions.
  Supports window-local `Delete` to remove selected nodes.
  When no node is selected, `Delete` unlinks selected curves by moving the `Enter` side node object back under `Graph`.
  Shortcut uses Unity `Shortcut` with `EditorUIWindow` context.
  Shortcut uses the same save flow as the top `Save` button.
  New node first build uses mouse position directly, without default-position flash.
  Middle mouse drag pans the canvas view without changing node saved positions.
  Mouse wheel zooms the canvas view without changing node saved positions.
  Canvas zoom should keep the content point under the mouse visually fixed.
  Pointer and wheel input should be received on a full-window viewport layer, not on the transformed canvas itself.

- `UIManager`
  Singleton window entry.
  Use `UIManager.Instance`.
  Depends on `UnityEditor.EditorWindow`.
  Owns global single-instance UI objects.
  Owns `root`
  Owns `canvas`
  Owns `curve`
  Owns `varLine`
  Owns `line`
  Owns `selectionBox`
  Owns preview-line state.
  Starting a line clears current node and curve selection.
  Global mouse move updates the line.
  Global mouse up stops the line.

- `EditorUIDefaults`
  Shared default values are centralized here.

- `NodeTitle`
  Dedicated title control for `UINode`.
  Owns title label.
  Owns mirror button.
  Title-related UI and callbacks should be handled here.

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
  ensures `Graph` also has a `SceneRefObject` id
  if a service type implements `IGroupNode`, its node object is created under `Graph/Group`
  if a service type implements `ISpecialNode`, its node object is created under `Graph/Special`
  creates the scene node object and adds the selected service component

- `GameObjectOperator`
  Centralized `GameObject` operations for EditorUI.
  Can find `Root`.
  Can find `Graph`.
  Can delete node objects by node id.
  Can move a node object back under `Graph`.

- `ServiceRule`
  Singleton for Service-based EditorUI rules.
  Future interface and attribute checks on Service should be handled here.
  Can get the actual `Service` from node id.
  Can get flow-port visibility from `IFlowPort`.
  Can get and toggle mirror state from `IMirrorNode`.
  Can get `[Input]` fields.

- `ISpecialNode`
  Stores:
  `EnterId`
  `NextId`

- `IGroupNode`
  Marker interface for grouped scene-node creation.
  Service types with this interface are created under `Graph/Group`.

- `IFlowPort`
  Short flow-port config interface.
  Stores:
  `HasEnterPort`
  `HasNextPort`
  If both are `true`, node shows `EnterNextPoint`.
  If both are `false`, node does not create `EnterNextPoint`.
  If only one is `true`, the other side is hidden but still keeps its layout space.

- `IMirrorNode`
  Mirror interface for Service nodes.
  Stores:
  `IsMirror`

- `IServiceNode.cs`
  Centralized file for Service node-extension interfaces.
  New Service creation interfaces should continue to be added here.

- `NodeManager`
  Manages `UINode`.
  No default sample node is created.
  Use `UINodes`
  Use `GetUINode`
  Use `GetSelectedUINodes`
  Use `SelectedUINode`

- `NodeData`
  Pure node data.
  Stores:
  `ParentId`
  `Title`
  `Position`
  `IsSelected`
  `NodeBorderState`
  Position changes must be saved to the node pos asset file.

- `NodePosAsset`
  Stores `graphId`.
  Node positions are only loaded when asset `graphId` matches current `Graph` id.
  Saving node positions also saves current `Graph` id.

- `NodeBorderState`
  `Default`
  `Selected`
  `Running`
  `Completed`

- `UINode : VisualElement`
  Node view control.
  Uses `UINode.uss`.
  Owns `NodeData`.
  Uses `NodeTitle` for title UI.
  Point controls are added through a dedicated setup function, not a single-object return.
  `ParamPoint` controls are added through `ServiceRule`.
  Point layout is decided through `ServiceRule`.
  `EnterNextPoint` is created only when `ServiceRule` says at least one flow port is enabled.
  Saves node position after a real drag move finishes.

- `MirrorBtn`
  Small square mirror button in the title area.
  Draws two arrows.
  Clicking it toggles `IMirrorNode.IsMirror`.

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
  Draws above nodes.
  Uses a thin stroke.
  Draws as a straight line.

- `VarLine`
  Dedicated layer for var drawing.
  Separate from parent-child `Curve`.
  Refreshes with node changes and position changes.
  Lives under `Lines`.
  Should not mutate point visual state during `generateVisualContent`.

- `Curve`
  Parent-child curve control.
  Draws bezier curves in canvas local coordinates.
  Refreshes by `NodeData.ParentId`.
  Draws above nodes.
  Uses `NodeManager.GetUINode`.
  Uses a thin stroke.
  Supports mouse click selection by curve hit test.
  Supports box selection by selection rect hit test.
  A curve is also selected when either related node is selected.
  Exposes selected child ids for delete-unlink flow.
  Selected curve changes color and becomes thicker.
  Hit building must skip nodes that currently have no valid `EnterPoint` or `NextPoint`.

- `LinkPoint`
  Only contains the point itself.
  No label text inside.
  `SetReverseOrder(true)` = left side
  `SetReverseOrder(false)` = right side
  Uses custom `Point : VisualElement` instead of Unity `Toggle`.
  `point` pointer down only starts line mode.
  Can be used as the base class for specialized point controls.

- `Point : VisualElement`
  Custom point control for link UI.
  Draw style follows old `ConnectionPoint` visual direction.

- `ParamPoint`
  Composite control for a single parameter point.
  Inherits `LinkPoint`.
  Supports `SetMirror(bool)` for true left/right mirror layout of both point and parameter field area.
  Supports `SetValueType(Type)`.
  Supports `Setup(Service, FieldInfo)`.
  Stores the parameter value type.
  Has no label.
  Also draws Inspector-style parameter UI through `SerializedObject`, `SerializedProperty`, and `PropertyField`.
  For `MMVar/MMListVar`, only the actual needed child properties are shown instead of the whole wrapper object.
  For non-generic value-type vars such as `Vector2Var`, value type should be resolved from the actual `Get()` return type.
  For `MMVar/MMListVar`, unsupported `InputType` values must be normalized to the Service-defined fallback before drawing the actual value field.
  The `InputType` popup should only show the options actually supported by the current var type.

- `EnterNextPoint`
  Composite control for `Enter` and `Next`.
  Labels live here.
  `SetMirror(bool)` swaps left/right arrangement.
  `SetPortVisible(bool, bool)` hides a side while keeping its layout space.

- `LinkRule`
  Manages link-point connection rules and connection execution.
  On connect, `Enter` node object becomes child of `Next` node object.

- `ControlDebugHelper`
  Controls debug borders.

## Coding Rules

- Do not add meaningless wrapper functions.
- Do not wrap an existing validated getter with another getter that only repeats null checks.
- Do not use `if (...) { ... return; }` when the real structure is `if / else`.
- Prefer direct `if / else`.
- Do not name private fields `layout` inside `VisualElement` controls.
- For custom classes, do not put too many parameters into constructors.
- Create the object first, then set extra fields after `new` when needed.
- Global single-instance UI objects must live in `UIManager`.
- Future Service interface and attribute checks should be centralized in `ServiceRule.Instance`.
- Service mirror behavior should also be checked and toggled through `ServiceRule.Instance`.
- Do not pass global single-instance UI objects through parameters.
- Use `UIManager.Instance.xxx` directly.
- Text stays vertically centered by default.
- Only change horizontal alignment when needed.
- Prefer short names for classes and files.
- Keep only the core responsibility words in names.
- Avoid overly long names such as combined `Point/Connection/Manager` style names unless really necessary.
- Deduction process and deduction result must be written into Docs separately.
- Write deduction result into `EditorUI_Context.md`.
- Keep `EditorUI_Context.md` updated after every EditorUI chat.
- Node position changes must save the node pos asset file.

## Keywords

`EditorUI`
`UIManager.Instance`
`EditorUIWindow`
`EditorUITypeMenu`
`NodeTitle`
`SceneNodeFactory`
`NodeManager.Instance`
`NodeData`
`NodeBorderState`
`UINode`
`UINode.uss`
`HLayout`
`VLayout`
`NodeLayout`
`Curve`
`VarLine`
`Line`
`LinkPoint`
`LinkRule`
`ParamPoint`
`EnterNextPoint`
`SetMirror`
`IFlowPort`
`IGroupNode`
`IMirrorNode`
`ISpecialNode`

## Short Background For Next Chat

```text
PlayableFramework EditorUI code is under PlayableFramework/Editor/EditorUI.
EditorUI_Context.md stores deduction results only and must be maintained after every EditorUI chat.
EditorUIWindow is the main window, opened from Tools/PlayableFramework/Editor UI.
EditorUIWindow handles window-local Ctrl+S save through Unity Shortcut window context.
EditorUIWindow Delete removes selected nodes first, or unlinks selected curves by moving Enter-side node objects back under Graph.
UIManager is the singleton window entry.
Global single-instance UI objects live in UIManager.
EditorUITypeMenu is the standalone Service/Data create menu.
SceneNodeFactory creates scene nodes and ensures Root, ResourceCenter, GlobalContext, GlobalAudioManager, then Graph last, and assigns root.resourceCenter/root.audioManager.
Service node-extension interfaces are centralized in IServiceNode.cs.
IFlowPort is the short flow-port interface and now also lives in IServiceNode.cs.
IFlowPort and other future interfaces should be checked from the actual Service object through `ServiceRule.Instance`, not stored as bool results in NodeData.
Service types with `[Input]` fields add one `ParamPoint` per field in EditorUI through `ServiceRule.Instance`.
ParamPoint reuses Unity inspector drawing through SerializedObject and PropertyField.
For MMVar/MMListVar, ParamPoint shows only the actual needed child properties.
Mirror-capable services use IMirrorNode and show a title-right mirror button that toggles node mirror state.
Node title UI is wrapped in NodeTitle, and title-related things should stay there.
IGroupNode makes a Service create under Graph/Group instead of directly under Graph.
GameObjectOperator centralizes scene object lookup, delete, and move-to-Graph operations.
Graph also keeps a SceneRefObject id, and NodePosAsset stores graphId to match the current Graph.
NodeManager manages UINode and does not create a default sample node.
NodeData is pure data and includes ParentId and NodeBorderState.
UINode is the node view control, owns NodeData, and uses UINode.uss.
HLayout and VLayout are shared base layouts with expand/stretch/center/debug-border defaults.
Line draws the preview line in canvas local coordinates.
Curve draws parent-child bezier lines from parent NextPoint to child EnterPoint.
Var drawing is separated into VarLine.
Curve supports both direct click selection and box selection.
Selected nodes also make their related curves selected.
UIManager owns the preview-line state.
Do not pass global single-instance UI objects through parameters.
LinkPoint toggle mouse down starts line mode.
LinkPoint only keeps the point itself.
ParamPoint is a single-point composite control for parameter input with mirror support.
LinkRule manages point-link rules and execution with a short name style.
EnterNextPoint owns the Enter/Next labels and supports SetMirror(bool).
Do not add meaningless wrapper functions.
Prefer explicit if/else.
Prefer short class and file names.
Text stays vertically centered by default.
```
Curve tangent direction must follow the actual left/right side of the mirrored point instead of assuming Next is always right and Enter is always left.
Mirror switching should prefer layout-direction changes over Clear/remove-and-add child rebuilding to avoid one-frame position flicker.
Mirror toggle should run on completed click and only refresh the current node plus line layers, not all node views.
MirrorBtn interaction should use left-button PointerUpEvent instead of ClickEvent for stable custom VisualElement handling.
Buttons inside UINode that should not start node dragging must intercept PointerDown and stop it before UINode capture logic runs.
Curve tangent length should be dynamic from point distance; do not keep a fixed long tangent when the end point is left of the start point.
When two linked points are close enough, Curve should draw a straight segment instead of a bezier, and hit/box-selection logic should follow the same straight-segment rule.
When a UINode geometry changes, line layers must repaint because point world positions may shift after the final layout width settles.
Mirror changes should batch line refresh: suppress geometry-triggered line repaints during the mirror update and repaint once after the layout settles.
When a LinkPoint swaps left/right slots, a temporary invisible placeholder can be used before Clear() to stabilize layout width and height during the mirror transition.
During mirror switching, UINode can temporarily lock its current width and height so the node does not collapse to an intermediate narrow layout before mirrored content settles.
Mirror property toggles should not trigger a full hierarchy-sync rebuild of EditorUI nodes; suppress the next OnHierarchyChange-driven SyncNodes during mirror switching.
For MMVar/MMListVar in Global mode, ParamPoint should not draw the raw string field; it should show a global-key popup based on GlobalContext.GetKeys(valueType, expectsList).
NodeLayout should keep a small bottom padding so single-row parameter nodes do not visually stick to the node bottom board.
Do not hide row spacing inside base HLayout defaults. Keep HLayout margin neutral and set explicit spacing on NodeTitle, EnterNextPoint, ParamPoint, and NodeLayout so node row gaps stay consistent.
ParamPoint is shared by both `[Input]` and `[Output]` fields. Output uses the same control class, but its default point side is the opposite of Input, and mirror flips both consistently.
Output fields should not reuse ParamPoint field-edit UI. Use OutputPoint: a lightweight row made of type text plus one output LinkPoint, with default side opposite to input and mirror support.
Dragging from Input/Output LinkPoint should use the same BeginLine flow as other points, but the preview line color should be white for var-style links.
Value links use Output->Input. During drag, connectable points turn green and non-connectable points turn red. Input/Output drag preview uses a white line.
For value links, EditorUI does not maintain a separate connection table. VarLine should derive connections from actual service data: MMVar/MMListVar with InputType.Service bindings, or direct assigned input values that match some node output value.
EditorUIWindow should repaint VarLine on inspector updates so external GameObject/Input/Output value changes can refresh parameter connections without waiting for a node rebuild.
