using System;
using System.Collections.Generic;
using System.Threading;

using Sackrany.ConfigSystem.SackranyConfig;
using Sackrany.GameInput.SackranyInput.Caches;

using SackranyInput.Configurations;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Sackrany.GameInput.SackranyInput
{
    /// <summary>
    /// Ручной рантайм ввода: жизненный цикл, кэши, указатель, оверрайды бинда.
    /// Не зависит от сгенерированного кода — сгенерированная схема подключается
    /// через <see cref="IInputBinding"/> и <see cref="RegisterBinding"/>.
    /// </summary>
    public static class InputManager
    {
        static CancellationTokenSource _cancellation;
        static readonly Dictionary<Type, IDisposable> _caches = new();
        static readonly List<IInputBinding> _bindings = new();

        public static GameInputConfigs InputConfigs { get; private set; }
        public static string InputBindings { get; private set; }
        public static Pointer CurrentPointer { get; private set; }

        public static CancellationToken Token => _cancellation?.Token ?? CancellationToken.None;

        /// <summary>
        /// Регистрирует сгенерированную схему ввода. Вызывается из генерёнки в
        /// [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] — то есть до <see cref="Init"/>.
        /// </summary>
        public static void RegisterBinding(IInputBinding binding)
        {
            if (binding == null || _bindings.Contains(binding)) return;
            _bindings.Add(binding);

            // Если Init уже прошёл (горячая регистрация) — догоняем биндинг.
            if (_cancellation != null)
            {
                binding.Init(_cancellation.Token);
                binding.ApplySettings();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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

        public static void ApplySettings()
        {
            foreach (var binding in _bindings)
                binding.ApplySettings();
        }

        /// <summary>Грузит оверрайды бинда из конфига в asset схемы. Зовётся биндингом.</summary>
        public static void ApplyBindingOverrides(InputActionAsset asset)
        {
            var cfg = ConfigGet<GameInputBindingsConfig>.Value;
            InputConfigs = ConfigGet<GameInputConfigs>.Value;
            InputBindings = cfg.BindingOverridesJson;

            asset.LoadBindingOverridesFromJson(cfg.BindingOverridesJson);
        }

        /// <summary>Сохраняет текущие оверрайды бинда из asset в конфиг (для UI настроек).</summary>
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
