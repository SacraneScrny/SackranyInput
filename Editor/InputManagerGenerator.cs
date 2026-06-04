using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Sackrany.GameInput.Editor.SackranyInput.Editor
{
    public class InputManagerGeneratorWindow : EditorWindow
    {
        InputActionAsset _asset;
        const string PrefKey = "Sackrany.InputManagerGenerator.AssetGuid";

        [MenuItem("Sackrany/Generate Input Managers")]
        static void Open()
        {
            var window = GetWindow<InputManagerGeneratorWindow>("Input Manager Generator");
            window.minSize = new Vector2(360, 120);
            window.Show();
            window.TryRestoreAsset();
        }

        void TryRestoreAsset()
        {
            var guid = EditorPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(guid)) return;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                _asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Input Action Asset", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _asset = (InputActionAsset)EditorGUILayout.ObjectField(
                _asset, typeof(InputActionAsset), false);
            if (EditorGUI.EndChangeCheck() && _asset != null)
            {
                var path = AssetDatabase.GetAssetPath(_asset);
                var guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(PrefKey, guid);
            }

            EditorGUILayout.Space(8);

            if (_asset == null)
            {
                EditorGUILayout.HelpBox("Выбери InputActionAsset для генерации.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Action Maps:", EditorStyles.miniLabel);
            foreach (var map in _asset.actionMaps)
                EditorGUILayout.LabelField($"  • {map.name} ({map.actions.Count} actions)", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Generate", GUILayout.Height(32)))
                InputManagerGenerator.Generate(_asset);
        }
    }

    public static class InputManagerGenerator
    {
        const string OutputDir = "Assets/_Generated/GameInput";

        public static void Generate(InputActionAsset asset)
        {
            Directory.CreateDirectory(OutputDir);
            EnsureAsmdef();

            var schemeName = asset.name;
            var mapInfos = new List<MapInfo>();

            foreach (var map in asset.actionMaps)
            {
                var info = BuildMapInfo(map);
                mapInfos.Add(info);
                GenerateCacheFile(info, schemeName);
            }

            GenerateBindingFile(mapInfos, schemeName);

            AssetDatabase.Refresh();
            Debug.Log($"[InputManagerGenerator] Generated {mapInfos.Count} caches + GameControls binding. Scheme: {schemeName}");
        }

        // Генерёнка живёт в отдельной сборке, ссылающейся на руками написанный
        // Sackrany.Input. Так asmdef фичи не ломается, а partial-классов между
        // сборками не возникает.
        static void EnsureAsmdef()
        {
            var path = Path.Combine(OutputDir, "Sackrany.Input.Generated.asmdef");
            if (File.Exists(path)) return;

            const string json =
@"{
    ""name"": ""Sackrany.Input.Generated"",
    ""rootNamespace"": """",
    ""references"": [
        ""Sackrany.Input"",
        ""Unity.InputSystem"",
        ""UniTask""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";
            File.WriteAllText(path, json);
        }

        // ── Cache file ──────────────────────────────────────────────────────

        static void GenerateCacheFile(MapInfo map, string schemeName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System.Threading;");
            sb.AppendLine();
            sb.AppendLine("using Cysharp.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace Sackrany.GameInput.Caches");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {map.CacheName} : InputActionsCache");
            sb.AppendLine("    {");
            sb.AppendLine($"        {schemeName}.{map.MapName}Actions _actions;");
            sb.AppendLine();

            foreach (var a in map.ButtonActions)
                sb.AppendLine($"        readonly ToggleableBool _{a.FieldName};");

            if (map.ButtonActions.Count > 0)
                sb.AppendLine();

            foreach (var a in map.ButtonActions)
            {
                sb.AppendLine($"        public bool {a.PropName} => _{a.FieldName}.Value;");
                sb.AppendLine($"        public bool {a.PropName}JustPressed => _{a.FieldName}.JustPressed;");
                sb.AppendLine($"        public ToggleableBool {a.PropName}Mode => _{a.FieldName};");
                sb.AppendLine();
            }

            foreach (var a in map.ValueActions)
                sb.AppendLine($"        public {a.TypeName} {a.PropName} {{ get; private set; }}");

            if (map.ValueActions.Count > 0)
                sb.AppendLine();

            sb.AppendLine($"        public {map.CacheName}({schemeName} input, CancellationToken token)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _actions = input.{map.MapName};");
            sb.AppendLine();

            foreach (var a in map.ButtonActions)
                sb.AppendLine($"            _{a.FieldName} = Register(_actions.{a.ActionName});");

            sb.AppendLine();
            sb.AppendLine("            Update(token).Forget();");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        async UniTaskVoid Update(CancellationToken token)");
            sb.AppendLine("        {");
            sb.AppendLine("            while (!token.IsCancellationRequested)");
            sb.AppendLine("            {");
            sb.AppendLine("                await UniTask.Yield(PlayerLoopTiming.LastUpdate, token);");
            sb.AppendLine();
            sb.AppendLine("                ResetAll();");

            if (map.ValueActions.Count > 0)
            {
                sb.AppendLine();
                foreach (var a in map.ValueActions)
                    sb.AppendLine($"                {a.PropName} = _actions.{a.ActionName}.ReadValue<{a.TypeName}>();");
            }

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(OutputDir, $"{map.CacheName}.cs"), sb.ToString());
        }

        // ── Binding (реестр вместо partial) ─────────────────────────────────

        static string MapField(MapInfo map) => char.ToLower(map.MapName[0]) + map.MapName.Substring(1);

        static void GenerateBindingFile(List<MapInfo> maps, string schemeName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine();
            sb.AppendLine("using Sackrany.GameInput;");
            sb.AppendLine("using Sackrany.GameInput.Caches;");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Сгенерированная схема ввода. Сама регистрируется в InputManager на старте.");
            sb.AppendLine("/// Доступ: GameControls.PlayerCache.Jump, GameControls.EnablePlayer() и т.д.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine("public sealed class GameControls : IInputBinding");
            sb.AppendLine("{");
            sb.AppendLine("    public static GameControls Instance { get; private set; }");
            sb.AppendLine();
            sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
            sb.AppendLine("    static void Register()");
            sb.AppendLine("    {");
            sb.AppendLine("        Instance ??= new GameControls();");
            sb.AppendLine("        InputManager.RegisterBinding(Instance);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    {schemeName} _inputScheme;");
            sb.AppendLine();

            // Action map fields + static shortcuts
            foreach (var map in maps)
            {
                var field = MapField(map);
                sb.AppendLine($"    {schemeName}.{map.MapName}Actions _{field};");
                sb.AppendLine($"    public static {schemeName}.{map.MapName}Actions {map.MapName} => Instance._{field};");
                sb.AppendLine();
            }

            // Cache fields + static shortcuts
            foreach (var map in maps)
            {
                sb.AppendLine($"    {map.CacheName} _{map.CacheFieldName};");
                sb.AppendLine($"    public static {map.CacheName} {map.CacheShortcut} => Instance._{map.CacheFieldName};");
                sb.AppendLine();
            }

            // Enable helpers
            foreach (var map in maps)
            {
                sb.AppendLine($"    public static void Enable{map.MapName}()");
                sb.AppendLine("    {");
                foreach (var other in maps)
                {
                    var otherField = MapField(other);
                    if (other.MapName == map.MapName)
                        sb.AppendLine($"        Instance._{otherField}.Enable();");
                    else
                        sb.AppendLine($"        Instance._{otherField}.Disable();");
                }
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Init
            sb.AppendLine("    public void Init(CancellationToken token)");
            sb.AppendLine("    {");
            sb.AppendLine($"        _inputScheme = new {schemeName}();");
            sb.AppendLine("        _inputScheme.Enable();");
            sb.AppendLine();
            foreach (var map in maps)
                sb.AppendLine($"        _{MapField(map)} = _inputScheme.{map.MapName};");
            sb.AppendLine();
            foreach (var map in maps)
            {
                sb.AppendLine($"        _{map.CacheFieldName} = new {map.CacheName}(_inputScheme, token);");
                sb.AppendLine($"        InputManager.Register(_{map.CacheFieldName});");
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // Dispose
            sb.AppendLine("    public void Dispose()");
            sb.AppendLine("    {");
            foreach (var map in maps)
                sb.AppendLine($"        _{MapField(map)}.Disable();");
            sb.AppendLine();
            sb.AppendLine("        _inputScheme?.Disable();");
            sb.AppendLine("        _inputScheme?.Dispose();");
            sb.AppendLine("    }");
            sb.AppendLine();

            // ApplySettings
            sb.AppendLine("    public void ApplySettings()");
            sb.AppendLine("    {");
            sb.AppendLine("        if (_inputScheme != null)");
            sb.AppendLine("            InputManager.ApplyBindingOverrides(_inputScheme.asset);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(OutputDir, "GameControls.cs"), sb.ToString());
        }

        // ── Data ────────────────────────────────────────────────────────────

        static MapInfo BuildMapInfo(InputActionMap map)
        {
            var name = map.name;
            var info = new MapInfo
            {
                MapName = name,
                CacheName = $"{name}ActionsCache",
                CacheShortcut = $"{name}Cache",
                CacheFieldName = char.ToLower(name[0]) + name.Substring(1) + "Cache"
            };

            foreach (var action in map.actions)
            {
                var controlType = action.expectedControlType;

                if (IsButtonType(controlType))
                {
                    var propName = action.name;
                    info.ButtonActions.Add(new ActionInfo
                    {
                        ActionName = action.name,
                        PropName = propName,
                        FieldName = char.ToLower(propName[0]) + propName.Substring(1)
                    });
                }
                else
                {
                    info.ValueActions.Add(new ValueActionInfo
                    {
                        ActionName = action.name,
                        PropName = action.name,
                        TypeName = ControlTypeToCSType(controlType)
                    });
                }
            }

            return info;
        }

        static bool IsButtonType(string controlType)
            => string.IsNullOrEmpty(controlType) || controlType == "Button";

        static string ControlTypeToCSType(string controlType) => controlType switch
        {
            "Vector2" => "UnityEngine.Vector2",
            "Stick" => "UnityEngine.Vector2",
            "Dpad" => "UnityEngine.Vector2",
            "Axis" => "float",
            "Vector3" => "UnityEngine.Vector3",
            "Quaternion" => "UnityEngine.Quaternion",
            _ => "float"
        };

        // ── Models ──────────────────────────────────────────────────────────

        class MapInfo
        {
            public string MapName;
            public string CacheName;
            public string CacheShortcut;
            public string CacheFieldName;
            public List<ActionInfo> ButtonActions = new();
            public List<ValueActionInfo> ValueActions = new();
        }

        class ActionInfo
        {
            public string ActionName;
            public string PropName;
            public string FieldName;
        }

        class ValueActionInfo
        {
            public string ActionName;
            public string PropName;
            public string TypeName;
        }
    }
}