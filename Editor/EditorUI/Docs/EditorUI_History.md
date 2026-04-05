# PlayableFramework EditorUI History

## Purpose

This file keeps deduction process, design evolution, and semantic changes.
It is not the main background file.
The main background file stays focused on the final current result.
After every EditorUI chat, keep this file updated with the latest reasoning process.

## Architecture Direction

- EditorUI moved away from old Graph IMGUI style.
- EditorUI uses `UI Toolkit`.
- The preferred structure is Qt-like:
  controls own their own UI and events.
- Do not split by global `Render/Event/Style` responsibilities.
- Prefer splitting by control responsibility.
- Current direction keeps `NodeData` inside `UINode`.
- `NodeManager` manages `UINode` objects, and data is read back through `UINode.Data`.
- `NodeManager` API names should also say `UINode` directly to avoid mixing them with pure data access.
- `UINode` point creation should go through a dedicated function so later point-type/order expansion does not have to reopen the constructor structure.
- That point setup function should not be shaped as a single-object return, because later it may create multiple point controls.
- Interface-derived UI flags should not be cached into `NodeData`.
- Current direction is to resolve the actual `Service` object from node id when UI needs interface-based decisions.
- Future Service-based checks should not stay inside `UINode` or other controls.
- Current direction centralizes them into a singleton `ServiceRule`.
- Title UI should also not stay spread across `UINode`.
- Current direction wraps title label and mirror button into a dedicated `NodeTitle` control.
- Mirror support for Service nodes should follow the same rule.
- Current direction uses `IMirrorNode` in the shared interface file and routes mirror state checks/toggles through `ServiceRule`.
- Services implementing `IMirrorNode` need a real `IsMirror` property implementation, preferably backed by a serialized field so editor toggles persist.

## Layout Direction

- `HLayout` and `VLayout` were tested in both wrap-content mode and stretch mode.
- Current direction keeps them as shared base layouts with strong defaults.
- Special layout behavior should be handled by dedicated controls instead of adding more global layout rules.

## LinkPoint Evolution

- `LinkPoint` originally included both point and text.
- This made mirror behavior harder to control.
- Final direction changed `LinkPoint` to only keep the point itself.
- Labels such as `Enter` and `Next` were moved out into `EnterNextPoint`.
- Unity `Toggle` was later replaced by custom `Point : VisualElement`.
- Point drawing follows the old `ConnectionPoint` visual style direction.
- Point interaction uses `PointerDownEvent` for more reliable trigger behavior in current UI Toolkit flow.

## EnterNextPoint Evolution

- `EnterNextPoint` was added as a composite control to manage:
  left group
  right group
  labels
  mirror behavior
- Mirror behavior is handled at the composite control level instead of inside `LinkPoint`.
- A separate `ParamPoint` was later added for the single-parameter case.
- `ParamPoint` keeps the same composite-control direction:
  based on `LinkPoint`
  no label
  mirror handled by the wrapper on top of `LinkPoint`
  mirror now also flips the parameter field area, not only the point side
- For `ParamPoint`, calling `SetMirror(...)` must reorder both the inherited point side and the added field root.
- Nodes with mirror capability should expose it through a small title-right button instead of requiring code-only toggles.
- Avoid naming private fields `layout` inside `VisualElement` controls because it hides inherited `VisualElement.layout`.
- Current direction uses the Service type's `[Input]` fields to decide whether `ParamPoint` should be added to `UINode`.
- Input should not be treated as only one boolean flag.
- Current direction creates one `ParamPoint` for each `[Input]` field.
- `FieldInfo.FieldType` should also be passed into `ParamPoint` so future var matching and drawing rules can use the real parameter type.
- Inspector-style parameter UI should not be hand-built field by field when Unity already has serialized drawing rules.
- Current direction moves that Inspector-style parameter UI into `ParamPoint` itself instead of keeping a separate `ParamField` wrapper.
- `ParamPoint` now owns:
  parameter point
  Unity `PropertyField`
  drawing through `SerializedObject` and `SerializedProperty`
- For `MMVar/MMListVar`, `ParamPoint` should not display the whole wrapper object.
- Current direction displays only the actual needed child properties such as `type`, `obj`, `objs`, `service`, or `global`.
- Custom-drawn EditorUI controls should prefer basic `Painter2D` APIs when version support for helpers like rounded-rect drawing is uncertain.

## ReverseOrder Semantic Changes

- Earlier versions used:
  `false = left`
  `true = right`
- Later this was flipped to match actual usage in the current code:
  `true = left`
  `false = right`
- Current valid meaning is recorded only in `EditorUI_Context.md`.

## Text Alignment Notes

- Vertical alignment was adjusted multiple times.
- Bottom-edge alignment is not the goal.
- Visual center-line alignment is the goal.
- Small position offsets may still be used when font baseline behavior makes the visual center look wrong.

## Toggle Styling Notes

- The point uses Unity `Toggle`, not a custom-drawn control.
- Styling was changed toward:
  round outline
  round inner dot
  state color changes
- The visual result depends on actual Unity built-in toggle structure, so further refinement may still be needed by screenshot iteration.

## Line Preview Direction

- Preview line logic was simplified.
- `LinkPoint` no longer owns move/up line logic.
- `LinkPoint` only starts line mode from toggle mouse down.
- `UIManager` owns global mouse move and mouse up handling.
- Entering line mode should clear current node selection and curve selection first.
- `Line` draws in canvas local coordinates.

## Node Position Save Direction

- Node position persistence should happen only after a real move, not after a simple click.
- Pointer capture loss should still save the moved node position.
- Current direction keeps one save at drag finish instead of saving on every move step.
- Current direction keeps save shortcut local to `EditorUIWindow`.
- `OnGUI` and plain UI Toolkit key handling were not reliable enough here.
- Current direction uses Unity `Shortcut` with `EditorUIWindow` context for window-local `Ctrl+S`.
- Shortcut path still writes a debug log when triggered.
- Shortcut should not call bare `SavePos()` only, because `SavePos()` returns early when no asset exists.
- Current direction makes shortcut use the same flow as the top `Save` button.
- Shortcut entry and instance save flow should use different names to avoid static/instance member name collisions.

## Delete Direction

- Delete shortcut should stay local to `EditorUIWindow`.
- Current direction uses window-context `Delete` shortcut.
- Delete removes all selected `UINode` objects and destroys their matching `GameObject` objects.
- Delete should also work for selected curves when no node is selected.
- Current direction handles delete in one entry:
  delete selected nodes first
  otherwise move selected curve child nodes back under `Graph`
- For curve delete, the `Enter` side node object is the one moved under `Graph`.
- New node creation should not first appear at fallback default position and then jump to mouse position.
- Current direction applies a one-shot pending position before the first sync rebuild finishes.

## Graph Id Save Direction

- Node position asset should not be shared blindly across different Graph objects.
- Current direction adds a SceneRefObject id to Graph too.
- NodePosAsset now stores `graphId`.
- Node positions are loaded only when the saved `graphId` matches the current Graph id.

## Parent Sync Direction

- Parent-child relationship should also exist in pure node data instead of being read only from scene objects.
- Current direction stores parent relation as `ParentId` inside `NodeData`.
- `SyncNodes()` writes `ParentId` from the current scene hierarchy when rebuilding node data.
- Flow link direction uses `Next` as parent side and `Enter` as child side.
- On connect, the `Enter` side node object is parented under the `Next` side node object.

## Curve Direction

- Parent-child relation should have a visual result in EditorUI instead of staying only in data.
- Current direction adds a dedicated curve layer.
- Curve drawing follows `NodeData.ParentId`.
- Curves go from parent `NextPoint` to child `EnterPoint`.
- Curve layer order was moved above nodes so lines stay visible.
- Curve node lookup should reuse `NodeManager.GetNode` instead of duplicating raw id matching first.
- Do not add a second helper getter when `NodeManager.Instance.GetUINode(...)` already does the lookup work.
- Current visual direction uses thinner strokes for both preview line and parent-child curve.
- Curve also has a selection state in rendering.
- Curve now also supports direct mouse selection by hit testing against the bezier path.
- Canvas click flow checks curve hit first, then falls back to normal box selection.
- Box selection should also affect curves, not only nodes.
- Current direction makes `ApplyBoxSelection()` pass the same selection rect into `Curve.SelectInRect(...)`.
- Current curve selection feedback uses both color change and thicker stroke.
- Curve selection should also follow node selection so related lines are highlighted without separate manual line picks.
- Curve now exposes selected child ids so delete-unlink logic can reuse current curve selection instead of duplicating hit lookup.
- Variable drawing should not be mixed into the parent-child curve layer.
- Current direction adds a separate `VarLine` class as the dedicated var drawing layer.
- Var drawing files should live under `Lines`, not under `Points`.

## Naming Direction

- Long helper names reduce readability during rapid EditorUI iteration.
- Current direction prefers short file and class names with only the core responsibility words.
- `LinkPointConnectionManager` was renamed to `LinkRule`.
- When shortening names, keep required namespace imports intact.
- `UIManager` still needs `using UnityEditor;` because it calls `EditorWindow.GetWindow`.
- `UINode` still needs `using UnityEngine;` because it uses `Vector2`.

## Border State Direction

- Node border state was separated from plain selection color fill.
- Current state set is:
  `Default`
  `Selected`
  `Running`
  `Completed`

## Scene Creation Direction

- EditorUI create flow was separated from old `GraphManager` menu flow.
- `EditorUITypeMenu` was created as an independent create menu.
- `EditorUISceneNodeFactory` was created to ensure required scene structure before creating node objects.
- Service node-extension interfaces should be centralized instead of scattered across many files.
- Current direction adds `IServiceNode.cs` as the shared file for these interfaces.
- The first interface is `IGroupNode`.
- The old long flow-port interface name was also shortened and moved into the same shared file.
- `IServiceFlowPortConfig` was renamed to `IFlowPort`.
- `IFlowPort` should also drive EditorUI point rendering instead of only runtime/editor-node logic.
- Current direction is:
  both `HasEnterPort` and `HasNextPort` true -> show `EnterNextPoint`
  both false -> do not create `EnterNextPoint`
  only one true -> keep `EnterNextPoint` layout and hide the other side content only
- The creation condition should be written as a direct positive check:
  create `EnterNextPoint` when `HasEnterPort || HasNextPort`
- `IFlowPort` should be read from the actual `Service` object at UI build time, not prewritten as bool fields into `NodeData`.
- `IFlowPort` and `[Input]` checks are now routed through `ServiceRule` instead of being implemented directly inside `UINode`.
- `IGroupNode` is a marker interface:
  matching Service types should not be created directly under `Graph`
  they should be created under a shared empty parent `Graph/Group`

## Scene Hierarchy Decisions

- Keep auto-create:
  `Root`
  `ResourceCenter`
  `GlobalContext`
  `GlobalAudioManager`
  `Graph`
- Do not auto-create:
  `SceneRefManager` object

## Coding Rules That Came From Iteration

- Do not add meaningless wrapper functions.
- Do not use `if (...) { ... return; }` when the real final structure is `if / else`.
- Global single-instance UI objects should live in `UIManager`.
- Do not pass global single-instance UI objects through parameters.
- Use `UIManager.Instance.xxx` directly.
- Prefer short class and file names.
- Keep only the core responsibility words in names.
- Keep rule documents clean:
  final result in `EditorUI_Context.md`
  iteration history in `EditorUI_History.md`
- Deduction process and deduction result must be written into Docs separately.
- Write deduction process into `EditorUI_History.md`.
- Keep `EditorUI_History.md` updated after every EditorUI chat.
- Mirror curve bug cause: bezier tangents were hardcoded as start +X and end -X, so after node mirror the curve shape became wrong even though the points themselves were correct.
- Fix: compute tangent sign from each LinkPoint world position relative to its owner UINode center, then use that sign when building Curve tangents.
- Mirror toggle flicker cause: ParamPoint and EnterNextPoint rebuilt child order by Clear()/RemoveFromHierarchy()+Add(), so layout briefly jumped during the same interaction.
- Fix: keep the child tree stable and switch mirror by changing flexDirection/justifyContent/text alignment instead of rebuilding children.
- Mirror flicker still remained after layout-only flipping because the action was triggered on PointerDown and then refreshed all node views, causing extra layout work during the click.
- Follow-up fix: trigger mirror on ClickEvent and refresh only the current UINode plus Curve/VarLine repaint layers.
- Mirror button ClickEvent was unreliable on the custom-drawn MirrorBtn; switch to left-button PointerUpEvent for stable window-local click handling.
- Mirror button PointerUp did not fire because UINode captured the pointer on PointerDown for drag handling; the button must intercept PointerDown first and capture/stop the event itself.
- Abnormal curve in mirrored/backward layouts was also caused by a fixed 60f tangent length; when the end point moved to the left of the start point, the bezier overshot and bent too far.
- Fix: make Curve tangent length depend on horizontal point distance, and use a shorter tangent range for backward connections where end.x < start.x.
- Added near-distance straight-line rule for Curve: if linked points are close, draw a direct segment instead of a bezier to avoid cramped bends.
- Keep Curve behavior consistent by applying the same straight/curve rule to drawing, click hit-testing, and box-selection intersection checks.
- Fix: repaint Curve and VarLine on UINode GeometryChangedEvent so lines follow the final point positions after mirror-driven layout resizing.
- Mirror width-change issue: the node first laid out at a narrower width before mirrored children fully affected layout, so point positions moved again after the first line repaint.
- Mirror refresh is now batched: while a node is updating its mirrored layout, geometry-change callbacks do not repaint lines until the scheduled end of the mirror update.
- Added temporary invisible placeholders in LinkPoint before left/right slot Clear() so mirror-time slot rebuilding keeps layout size more stable until the real point is reattached.
- Mirror flicker root cause observed in practice: the node first collapsed to a temporary narrow layout before mirrored parameter content finished participating in layout, then expanded to its final width.
- Fix attempt: lock the current UINode width/height during mirror switching, then release back to Auto after the mirror update finishes and repaint lines once.
- Log result: the main mirror flicker came from OnHierarchyChange calling SyncNodes after a mirror toggle, which rebuilt UINodes and recreated the temporary narrow state before the final layout width settled.
- Fix: suppress the next hierarchy-driven SyncNodes when toggling mirror, so mirror stays as an in-place layout update instead of a full node rebuild.
- Fix: when ParamPoint rebuilds a MMVar/MMListVar value field and the active child property is `global`, build a popup from `GlobalContext.GetKeys(resolvedValueType, expectsList)` instead of using a raw string PropertyField.
- UI issue for `[Input] TransformListVar globalData`: drawing the `global` child as a plain string PropertyField lost the existing MMVarDrawer global-key behavior, so the node only showed the input-type popup and an unusable value area.
- Single-row parameter nodes looked too close to the bottom board because NodeLayout had no extra bottom inset; adding a small bottom padding makes spacing more consistent with multi-row nodes.
- Fix: neutralize default HLayout margins and move spacing responsibility to explicit per-row margins plus NodeLayout top/bottom padding.
- Spacing inconsistency came from HLayout carrying a hidden default top margin for every row, while NodeLayout and child rows had no explicit unified bottom spacing.
- Output default point direction should be the opposite of Input; ParamPoint now decides the visible point side from both mirror state and whether it is an output field.
- Output fields are now routed through the same ParamPoint class as Input fields.
- Direction changed: output should not use ParamPoint editor content. Output now uses a separate OutputPoint row with only type text and one LinkPoint.
- OutputPoint label displays the `[Output]` field type name, while its default side is opposite to input and still follows node mirror state.
- Input and Output points now both use the shared LinkPoint drag flow, and preview lines started from var-style points use a white stroke instead of the blue flow-line color.
- LinkPoint now carries field metadata (node id, field name, value type, expects-list) so link validation, apply-on-connect, and VarLine repaint can all derive behavior from the actual Service fields instead of an extra plugin-side connection store.
- VarLine now derives parameter connections from real data bindings: first MMVar/MMListVar service references, then direct input/output value equality fallback when no explicit var-service binding exists.
- Added Output->Input variable-link flow for EditorUI. Old graph rules were reused as the reference: MMVar/MMListVar inputs bind by setting InputType.Service and assigning the source service; non-var inputs connect only when the output value can be assigned directly.
- Added an OnInspectorUpdate VarLine repaint as a lightweight fallback so parameter connections also respond to external inspector-side value changes on the underlying GameObjects.
- ParamPoint originally inferred var value type only from generic MMVar/MMListVar bases, which failed for non-generic value wrappers such as `Vector2Var`.
- Fix: resolve the actual value type from the var class `Get()` return type first, then fall back to generic-base inspection.
- ParamPoint also missed the Inspector drawer's input-type normalization, so vars like `Vector2Var` that do not support `Default` incorrectly drew the `obj/GameObject` field.
- Fix: normalize unsupported `InputType` values to the var instance fallback before choosing which child property to draw.
- ParamPoint originally used a raw enum `PropertyField` for `InputType`, which still exposed unsupported options such as `Default` for `Vector2Var`.
- Fix: replace the raw enum field with a constrained popup that only shows the input types supported by the current var class.
- Added middle-mouse background drag by panning the whole canvas view instead of changing node data positions.
- Right-click create position must use `canvas.WorldToLocal(evt.mousePosition)` so new-node placement stays correct after canvas panning.
- Middle-mouse canvas pan hit a compile issue because `PointerMoveEvent.position` is not the same type as the stored `Vector2`; convert the current pointer position explicitly before subtracting.
- VarLine originally called `LinkPoint.SetState(...)` during `generateVisualContent`, which triggered `MarkDirtyRepaint()` inside the render callback and caused a UI Toolkit invalid-operation exception.
- Fix: VarLine drawing should stay read-only and must not change point visual state inside `generateVisualContent`.
- Curve hit-testing also threw null references when a node had no current flow point; hit building now has to bail out cleanly when `EnterPoint` or `NextPoint` is missing.
- Added wheel zoom on the canvas view by changing the canvas transform scale instead of node data positions.
- Zoom keeps the current mouse position visually stable by recomputing canvas offset from the pre-zoom local mouse position.
- Follow-up fix for zoom center: compute the pre-zoom content point explicitly from `(mousePanelPosition - canvasOffset) / previousScale`, and keep transform origin at top-left so wheel zoom really anchors around the mouse.
- Pan/zoom exposed another issue: after the transformed canvas moved away from the window bounds, pointer and wheel events were no longer reliable because input was registered on the canvas itself.
- Fix: keep input callbacks on a full-size viewport layer, and use the transformed canvas only for content rendering and local-coordinate conversion.
