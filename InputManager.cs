using System;
using System.Collections.Generic;
using System.Threading;

using SackranyConfig;

using SackranyInput.Caches;
using SackranyInput.Configurations;

using UnityEngine;
using UnityEngine.InputSystem;

namespace SackranyInput
{
    public static class InputManager
    {
        static CancellationTokenSource _cancellation;
        static readonly Dictionary<Type, IDisposable> _caches = new();
        static readonly List<IInputBinding> _bindings = new();

        public static GameInputConfigs InputConfigs { get; private set; }
        public static string InputBindings { get; private set; }
        public static Pointer CurrentPointer { get; private set; }

        public static CancellationToken Token => _cancellation?.Token ?? CancellationToken.None;

        public static void RegisterBinding(IInputBinding binding)
        {
            if (binding == null || _bindings.Contains(binding)) return;
            _bindings.Add(binding);

            if (_cancellation != null)
            {
                binding.Init(_cancellation.Token);
                binding.ApplySettings();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _cancellation = new CancellationTokenSource();
            CurrentPointer = new Pointer(_cancellation.Token);
            InputConfigs = ConfigGet<GameInputConfigs>.Value;

            foreach (var binding in _bindings)
                binding.Init(_cancellation.Token);

            ApplySettings();

            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        public static T Register<T>(T cache) where T : InputActionsCache
        {
            _caches[typeof(T)] = cache;
            return cache;
        }

        public static T Get<T>() where T : InputActionsCache
            => (T)_caches[typeof(T)];

        public static bool TryGet<T>(out T cache) where T : InputActionsCache
        {
            if (_caches.TryGetValue(typeof(T), out var disposable))
            {
                cache = (T)disposable;
                return true;
            }

            cache = null;
            return false;
        }

        public static Vector2 ApplyLookSettings(Vector2 look)
        {
            var cfg = InputConfigs;
            if (cfg == null) return look;

            look *= cfg.MouseSensitivity;
            if (cfg.InvertY) look.y = -look.y;
            return look;
        }

        public static void ApplySettings()
        {
            foreach (var binding in _bindings)
                binding.ApplySettings();
        }

        public static void ApplyBindingOverrides(InputActionAsset asset)
        {
            var cfg = ConfigGet<GameInputBindingsConfig>.Value;
            InputConfigs = ConfigGet<GameInputConfigs>.Value;
            InputBindings = cfg.BindingOverridesJson;

            asset.LoadBindingOverridesFromJson(cfg.BindingOverridesJson);
        }

        public static void SaveBindingOverrides(InputActionAsset asset)
        {
            ConfigSet<GameInputBindingsConfig>.Do(c => c.BindingOverridesJson = asset.SaveBindingOverridesAsJson());
            DynamicConfigLoader.Save<GameInputBindingsConfig>();
        }

        static void OnQuitting()
        {
            foreach (var binding in _bindings)
                binding.Dispose();

            foreach (var cache in _caches.Values)
                cache.Dispose();
            _caches.Clear();

            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }
}
