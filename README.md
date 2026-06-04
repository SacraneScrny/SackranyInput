# SackranyInput

Обёртка над Unity Input System: типобезопасные кэши действий, указатель (`Pointer`),
оверрайды бинда через `ConfigSystem`.

## Кодген (реестр вместо partial)

1. Создай/настрой `InputActionAsset` (есть `Schemes/SackranyInputScheme.inputactions`).
2. `Sackrany/Generate Input Managers` → выбери asset → **Generate**.
3. Генерится в `Assets/_Generated/GameInput/`:
   - кэши действий (`*ActionsCache`),
   - класс `GameControls : IInputBinding`,
   - `SackranyInput.Generated.asmdef` (ссылается на `SackranyInput`).

`GameControls` сам регистрируется в `InputManager` на старте — никаких `partial`-классов
между сборками. Ручной `InputManager` компилируется даже без генерёнки.

```csharp
GameControls.EnablePlayer();
if (GameControls.PlayerCache.Jump) { ... }
var aim = InputManager.CurrentPointer.WorldRay;
```

**Конфиги:** `GameInputConfigs` (чувствительность/инверсия), `GameInputBindingsConfig`
(JSON оверрайдов). Сохранение бинда — `InputManager.SaveBindingOverrides(asset)`.

**Зависимости:** `Sackrany.Config`, Input System, UniTask, UnityEngine.UI.
**Editor:** генератор `SackranyInput.Editor`.
