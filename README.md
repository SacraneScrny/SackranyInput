# SackranyInput

A wrapper around the Unity Input System: type-safe action caches, a pointer
helper (`Pointer`), and binding overrides through `ConfigSystem`.

## Code generation

1. Create/configure an `InputActionAsset` (see `Schemes/SackranyInputScheme.inputactions`).
2. `Sackrany/Generate Input Managers` → select the asset → **Generate**.
3. Generated into `Assets/_Generated/GameInput/`:
   - action caches (`*ActionsCache`),
   - a `GameControls : IInputBinding` class,
   - `SackranyInput.Generated.asmdef` (references `SackranyInput`).

`GameControls` registers itself in `InputManager` at startup, so there are no
`partial` classes shared between assemblies. The hand-written `InputManager`
compiles even without the generated code.

```csharp
GameControls.EnablePlayer();

if (GameControls.PlayerCache.Jump) { /* ... */ }

var look = InputManager.ApplyLookSettings(GameControls.PlayerCache.Look);
var aim = InputManager.CurrentPointer.WorldRay;
```

## Action access

Each button action exposes three members on its cache:

- `Jump` — current state (hold or toggle, depending on `JumpMode.IsHold`),
- `JumpJustPressed` — true only on the frame the action started,
- `JumpMode` — the underlying `ToggleableBool` (switch hold/toggle behaviour).

Value actions (e.g. `Move`, `Look`) are exposed as strongly typed properties
read every frame.

Caches can be resolved by type:

```csharp
var player = InputManager.Get<PlayerActionsCache>();

if (InputManager.TryGet<PlayerActionsCache>(out var cache))
    cache.Jump.ToString();
```

## Pointer

`InputManager.CurrentPointer` provides screen position, delta, a world ray,
and `IsPointerOverUI`. Cursor visibility/lock is toggled together:

```csharp
InputManager.CurrentPointer.SwitchCursorVisibility(false); // hidden + locked

if (InputManager.CurrentPointer.TryGetWorldHit(out var hit))
    Debug.Log(hit.collider.name);
```

## Configs

- `GameInputConfigs` — `MouseSensitivity` and `InvertY`, applied to look input
  through `InputManager.ApplyLookSettings(Vector2)`.
- `GameInputBindingsConfig` — JSON binding overrides.

Persist rebindings with `InputManager.SaveBindingOverrides(asset)`; they are
re-applied automatically on startup.

## Dependencies

`Sackrany.Config`, Input System, UniTask, UnityEngine.UI.

**Editor:** the `SackranyInput.Editor` generator.
