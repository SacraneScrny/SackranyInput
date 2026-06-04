using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SackranyInput
{
    public class Pointer
    {
        public bool IsPointerOverUI { get; private set; }
        public Vector2 ScreenPosition { get; private set; }
        public Vector2 Delta { get; private set; }
        public Ray WorldRay { get; private set; }

        Camera _mainCamera;

        public Pointer(CancellationToken token)
        {
            PointerUpdate(token).Forget();
        }

        public void UpdateCamera(Camera camera)
        {
            _mainCamera = camera;
        }

        public bool TryGetWorldHit(out RaycastHit hit, float distance = 100f, int mask = 0)
            => Physics.Raycast(WorldRay, out hit, distance, mask);

        async UniTaskVoid PointerUpdate(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Yield(token);
                _mainCamera ??= Camera.main;

                var mouse = Mouse.current;
                if (mouse != null)
                {
                    ScreenPosition = mouse.position.ReadValue();
                    Delta = mouse.delta.ReadValue();
                }

                if (_mainCamera != null)
                    WorldRay = _mainCamera.ScreenPointToRay(ScreenPosition);

                IsPointerOverUI = EventSystem.current != null
                    && (EventSystem.current.IsPointerOverGameObject()
                        || EventSystem.current.IsPointerOverGameObject(0));
            }
        }
        
        public void SwitchCursorVisibility(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
