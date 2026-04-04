# PlayableFramework EditorUI History

## Purpose

This file keeps design evolution and semantic changes.
It is not the main background file.
The main background file stays focused on the final current result.

## Architecture Direction

- EditorUI moved away from old Graph IMGUI style.
- EditorUI uses `UI Toolkit`.
- The preferred structure is Qt-like:
  controls own their own UI and events.
- Do not split by global `Render/Event/Style` responsibilities.
- Prefer splitting by control responsibility.

## Layout Direction

- `HLayout` and `VLayout` were tested in both wrap-content mode and stretch mode.
- Current direction keeps them as shared base layouts with strong defaults.
- Special layout behavior should be handled by dedicated controls instead of adding more global layout rules.

## LinkPoint Evolution

- `LinkPoint` originally included both point and text.
- This made mirror behavior harder to control.
- Final direction changed `LinkPoint` to only keep the point itself.
- Labels such as `Enter` and `Next` were moved out into `EnterNextPoint`.

## EnterNextPoint Evolution

- `EnterNextPoint` was added as a composite control to manage:
  left group
  right group
  labels
  mirror behavior
- Mirror behavior is handled at the composite control level instead of inside `LinkPoint`.

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
- `Line` draws in canvas local coordinates.

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
- Keep rule documents clean:
  final result in `EditorUI_Context.md`
  iteration history in `EditorUI_History.md`
