using System;
using System.Collections.Generic;

using UnityEngine.InputSystem;

namespace Sackrany.GameInput.SackranyInput.Caches
{
    public abstract class InputActionsCache : IDisposable
    {
        protected readonly ActionBind _bindings = new();
        readonly List<ToggleableBool> _toggleables = new();

        protected ToggleableBool Register(InputAction action)
        {
            var tb = new ToggleableBool();
            _bindings.Bind(action, _ => tb.OnStarted(), _ => tb.OnCanceled());
            _toggleables.Add(tb);
            return tb;
        }

        protected void ResetAll()
        {
            foreach (var tb in _toggleables)
                tb.ResetOneShot();
        }

        public virtual void Dispose() => _bindings.UnbindAll();
    }
}
