using System;
using System.Collections.Generic;

using UnityEngine.InputSystem;

namespace SackranyInput
{
    public class ActionBind
    {
        readonly List<(InputAction action, Action<InputAction.CallbackContext> started, Action<InputAction.CallbackContext> canceled)> _bindings = new();
        readonly List<Action> _oneShots = new();

        public void Bind(InputAction action, Action<bool> setter)
        {
            Action<InputAction.CallbackContext> started = _ => setter(true);
            Action<InputAction.CallbackContext> canceled = _ => setter(false);
            action.started += started;
            action.canceled += canceled;
            _bindings.Add((action, started, canceled));
        }

        public void Bind(InputAction action,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> canceled)
        {
            action.started += started;
            action.canceled += canceled;
            _bindings.Add((action, started, canceled));
        }

        public void BindOneShot(InputAction action, Action<bool> setter)
        {
            bool current = false;

            Action<InputAction.CallbackContext> started = _ => { setter(true); current = true; };
            Action<InputAction.CallbackContext> canceled = _ => setter(false);
            action.started += started;
            action.canceled += canceled;
            _bindings.Add((action, started, canceled));

            _oneShots.Add(() =>
            {
                if (current) { setter(false); current = false; }
            });
        }

        public void ResetOneShots()
        {
            foreach (var reset in _oneShots)
                reset();
        }

        public void UnbindAll()
        {
            foreach (var (action, started, canceled) in _bindings)
            {
                action.started -= started;
                action.canceled -= canceled;
            }
            _bindings.Clear();
            _oneShots.Clear();
        }
    }
}
