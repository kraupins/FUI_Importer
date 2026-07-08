#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MTK.FigmaUIImport
{

    public sealed class AiFuiImporterWindow : EditorWindow
    {
        private string _fuiPath = string.Empty;
        private Vector2 _scroll;
        private FuiImportReport _lastReport;
        private string _lastError = string.Empty;

        [MenuItem("Инструменты/MTK/Figma UI Import")]
        public static void OpenWindow()
        {
            var window = GetWindow<AiFuiImporterWindow>("MTK | Figma UI Import");
            window.minSize = new Vector2(460, 420);
            window.Show();
        }

        [MenuItem("Assets/MTK/Import selected .fui", true)]
        private static bool ValidateImportSelectedFui()
        {
            var path = GetSelectedProjectPath();
            return !string.IsNullOrEmpty(path) && path.EndsWith(".fui", StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/MTK/Import selected .fui")]
        private static void ImportSelectedFui()
        {
            var path = GetSelectedProjectPath();
            if (string.IsNullOrEmpty(path)) return;
            var absolute = Path.GetFullPath(Path.Combine(AiFuiImporterUtility.ProjectRoot, path));
            try
            {
                var report = AiFuiImporter.Import(absolute, new FuiImportOptions());
                EditorUtility.DisplayDialog("MTK | Figma UI Import", report.ToHumanString(), "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Ошибка импорта", ex.Message, "OK");
            }
        }

        private static string GetSelectedProjectPath()
        {
            if (Selection.activeObject == null) return string.Empty;
            return AssetDatabase.GetAssetPath(Selection.activeObject);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fixedHeight = 34
            };
            EditorGUILayout.LabelField("MTK | Figma UI Import", titleStyle);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("FUI-файл", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _fuiPath = EditorGUILayout.TextField(".fui файл", _fuiPath);
                if (GUILayout.Button("Выбрать", GUILayout.Width(86)))
                {
                    var picked = EditorUtility.OpenFilePanel("Выбрать .fui пакет", string.Empty, "fui,zip");
                    if (!string.IsNullOrEmpty(picked)) _fuiPath = picked;
                }
            }

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_fuiPath)))
            {
                if (GUILayout.Button("Импортировать", GUILayout.Height(34)))
                {
                    RunImport();
                }
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Что создаётся", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Папка Assets/<PROJECT_NAME>/ с UXML, USS, Textures, Fonts, Resources/Color Gradient Presets, PanelSettings, Scenes и Info.\n" +
                "Тексты остаются редактируемыми Label/Button. Градиенты текста импортируются как TextCore Color Gradient presets и применяются через Unity rich text.",
                MessageType.None);

            DrawImportedProjectsCleanup();

            EditorGUILayout.EndScrollView();
        }


        private void DrawImportedProjectsCleanup()
        {
            var projects = AiFuiImporter.FindImportedProjects("Assets");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Импортированные проекты", EditorStyles.boldLabel);
            if (projects.Count == 0)
            {
                EditorGUILayout.HelpBox("Пока нет импортированных проектов.", MessageType.None);
                return;
            }
            foreach (var project in projects)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(project.DisplayName, GUILayout.MinWidth(180));
                    if (GUILayout.Button("Показать", GUILayout.Width(80)))
                    {
                        var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(project.AssetPath);
                        if (folder != null) EditorGUIUtility.PingObject(folder);
                    }
                    if (GUILayout.Button("Удалить", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("Удалить импортированный проект", "Полностью удалить папку проекта?\n" + project.AssetPath, "Удалить", "Отмена"))
                        {
                            AiFuiImporter.DeleteImportedProject(project.AssetPath);
                            if (_lastReport != null && string.Equals(AiFuiImporterUtility.NormalizeAssetPath(_lastReport.OutputRootAssetPath), AiFuiImporterUtility.NormalizeAssetPath(project.AssetPath), StringComparison.OrdinalIgnoreCase)) _lastReport = null;
                            _lastError = string.Empty;
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        private void RunImport()
        {
            _lastError = string.Empty;
            _lastReport = null;

            try
            {
                var options = new FuiImportOptions
                {
                    OutputRootAssetPath = "Assets",
                    OverwriteExisting = true,
                    CreatePackageSubfolder = true,
                    ApplyTextureSettings = true,
                    CreatePanelSettings = true,
                    CreateScreenPrefabs = false,
                    CreateSceneAssets = true,
                    AddScreensToOpenScene = false,
                    OpenFirstUxmlAfterImport = true
                };

                _lastReport = AiFuiImporter.Import(_fuiPath, options);
                Debug.Log("[MTK | Figma UI Import] " + _lastReport.ToHumanString());
                EditorUtility.DisplayDialog("MTK | Figma UI Import", _lastReport.ToHumanString(), "OK");
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Debug.LogException(ex);
            }
        }
    }

    [Serializable]
    public sealed class FuiImportOptions
    {
        public string OutputRootAssetPath = "Assets";
        public bool OverwriteExisting = true;
        public bool CreatePackageSubfolder = true;
        public bool ApplyTextureSettings = true;
        public bool CreatePanelSettings = true;
        public bool CreateScreenPrefabs = false;
        public bool CreateSceneAssets = true;
        public bool AddScreensToOpenScene = false;
        public bool OpenFirstUxmlAfterImport = true;
    }

    public sealed class FuiImportReport
    {
        public string PackageName;
        public string OutputRootAssetPath;
        public readonly List<string> GeneratedUxml = new List<string>();
        public readonly List<string> GeneratedUss = new List<string>();
        public readonly List<string> GeneratedPrefabs = new List<string>();
        public readonly List<string> GeneratedScenes = new List<string>();
        public string PanelSettingsAssetPath;
        public string SceneRootObjectName;
        public int CopiedTextureCount;
        public int CopiedFontCount;
        public int ScreenCount;
        public int WarningCount;
        public readonly List<string> Warnings = new List<string>();

        public string ToHumanString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Пакет: " + (PackageName ?? "FUI"));
            sb.AppendLine("Папка: " + (OutputRootAssetPath ?? ""));
            sb.AppendLine("Экраны: " + ScreenCount);
            sb.AppendLine("UXML: " + GeneratedUxml.Count);
            sb.AppendLine("USS: " + GeneratedUss.Count);
            sb.AppendLine("Префабы: " + GeneratedPrefabs.Count);
            sb.AppendLine("Сцены: " + GeneratedScenes.Count);
            if (!string.IsNullOrEmpty(PanelSettingsAssetPath)) sb.AppendLine("PanelSettings: " + PanelSettingsAssetPath);
            if (!string.IsNullOrEmpty(SceneRootObjectName)) sb.AppendLine("Объект в сцене: " + SceneRootObjectName);
            sb.AppendLine("Текстуры: " + CopiedTextureCount);
            sb.AppendLine("Шрифты скопированы: " + CopiedFontCount);
            if (WarningCount > 0)
            {
                sb.AppendLine("Предупреждения: " + WarningCount);
                for (var i = 0; i < Math.Min(Warnings.Count, 8); i++) sb.AppendLine("- " + Warnings[i]);
            }
            return sb.ToString().TrimEnd();
        }
    }

    public sealed class FuiImportedProjectInfo
    {
        public string DisplayName;
        public string AssetPath;
    }

    public static class AiFuiImporter
    {
        public static List<FuiImportedProjectInfo> FindImportedProjects(string rootAssetPath)
        {
            var result = new List<FuiImportedProjectInfo>();
            rootAssetPath = AiFuiImporterUtility.NormalizeAssetPath(string.IsNullOrEmpty(rootAssetPath) ? "Assets" : rootAssetPath);
            var absRoot = AiFuiImporterUtility.ToAbsolutePath(rootAssetPath);
            if (!Directory.Exists(absRoot)) return result;
            foreach (var dir in Directory.GetDirectories(absRoot))
            {
                var name = Path.GetFileName(dir);
                var assetPath = AiFuiImporterUtility.CombineAssetPath(rootAssetPath, name);
                var infoPath = AiFuiImporterUtility.CombineAssetPath(AiFuiImporterUtility.CombineAssetPath(assetPath, "Info"), "fui-import-report.json");
                if (!File.Exists(AiFuiImporterUtility.ToAbsolutePath(infoPath))) continue;
                result.Add(new FuiImportedProjectInfo { DisplayName = name, AssetPath = assetPath });
            }
            return result.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void DeleteImportedProject(string assetPath)
        {
            assetPath = AiFuiImporterUtility.NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(assetPath) || assetPath == "Assets" || !assetPath.StartsWith("Assets/", StringComparison.Ordinal)) return;
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }


        public static FuiImportReport Import(string fuiFilePath, FuiImportOptions options)
        {
            options = options ?? new FuiImportOptions();
            if (string.IsNullOrEmpty(fuiFilePath)) throw new ArgumentException("Путь к FUI пустой.");
            if (!File.Exists(fuiFilePath)) throw new FileNotFoundException("FUI файл не найден.", fuiFilePath);

            var normalizedRoot = AiFuiImporterUtility.NormalizeAssetPath(options.OutputRootAssetPath);
            if (!normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal) && normalizedRoot != "Assets")
                throw new ArgumentException("Папка вывода должна быть внутри Assets/. Пример: Assets");

            var package = FuiPackage.Read(fuiFilePath);
            var projectName = AiFuiImporterUtility.SanitizeFileName(package.ProjectName, Path.GetFileNameWithoutExtension(fuiFilePath));
            // FUI projects are always imported to Assets/<ProjectName>. The output folder is not user-configurable.
            var outputRoot = AiFuiImporterUtility.CombineAssetPath("Assets", projectName);
            outputRoot = PrepareOutputRoot(outputRoot, options.OverwriteExisting);

            var report = new FuiImportReport
            {
                PackageName = projectName,
                OutputRootAssetPath = outputRoot
            };
            var generatedScreens = new List<FuiGeneratedScreenAsset>();

            var infoRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Info");
            var uxmlRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "UXML");
            var ussRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "USS");
            var textureRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Textures");
            var fontRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Fonts");
            AiFuiImporterUtility.EnsureAssetFolder(infoRoot);
            AiFuiImporterUtility.EnsureAssetFolder(uxmlRoot);
            AiFuiImporterUtility.EnsureAssetFolder(ussRoot);
            AiFuiImporterUtility.EnsureAssetFolder(textureRoot);
            AiFuiImporterUtility.EnsureAssetFolder(fontRoot);

            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(infoRoot, "manifest.json"), package.ManifestJson ?? "{}");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(infoRoot, "metadata.json"), package.MetadataJson ?? "{}");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(infoRoot, "assets.json"), package.AssetsJson ?? "[]");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(infoRoot, "fonts.json"), package.FontsJson ?? "{}");

            var assetPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fontPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in package.AssetFiles)
            {
                var relative = AiFuiImporterUtility.TrimPrefix(file.Path, "assets/");
                var target = AiFuiImporterUtility.CombineAssetPath(textureRoot, AiFuiImporterUtility.SanitizeRelativePath(relative, "asset.png"));
                WriteBinaryAsset(target, file.Bytes);
                assetPathMap[AiFuiImporterUtility.NormalizePackagePath(file.Path)] = target;
                report.CopiedTextureCount++;
            }

            var textureMetaByAssetPath = new Dictionary<string, FuiAssetMeta>(StringComparer.OrdinalIgnoreCase);
            foreach (var meta in package.AssetMetadata)
            {
                if (!string.IsNullOrEmpty(meta.Id) && !string.IsNullOrEmpty(meta.Path))
                {
                    var key = AiFuiImporterUtility.NormalizePackagePath(meta.Path);
                    string localTexturePath;
                    if (assetPathMap.TryGetValue(key, out localTexturePath))
                    {
                        assetPathMap[meta.Id] = localTexturePath;
                        textureMetaByAssetPath[localTexturePath] = meta;
                    }
                }
            }

            foreach (var file in package.FontFiles)
            {
                var relative = AiFuiImporterUtility.TrimPrefix(file.Path, "fonts/");
                if (string.Equals(relative, "fonts.json", StringComparison.OrdinalIgnoreCase)) continue;
                var target = AiFuiImporterUtility.CombineAssetPath(fontRoot, AiFuiImporterUtility.SanitizeRelativePath(relative, "font.ttf"));
                WriteBinaryAsset(target, file.Bytes);
                fontPathMap[AiFuiImporterUtility.NormalizePackagePath(file.Path)] = target;
                fontPathMap[AiFuiImporterUtility.NormalizePackagePath(relative)] = target;
                report.CopiedFontCount++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (options.ApplyTextureSettings)
            {
                foreach (var path in assetPathMap.Values.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    FuiAssetMeta meta;
                    textureMetaByAssetPath.TryGetValue(path, out meta);
                    ApplyUiTextureImportSettings(path, meta);
                }
            }
            foreach (var path in fontPathMap.Values.Distinct(StringComparer.OrdinalIgnoreCase)) ApplyUiFontImportSettings(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Важно: не создаём TextCore FontAsset через reflection.
            // В Unity 6.5 такой FontAsset может создаться без m_Material и начать спамить
            // UnassignedReferenceException на любой сцене. Используем реальные .ttf/.otf Font assets,
            // которые Unity импортирует корректно и которые можно безопасно подключать через -unity-font.

            var usedScreenNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var screen in package.Screens)
            {
                var baseScreenName = AiFuiImporterUtility.SanitizeFileName(FuiJson.GetString(screen, "name", "Screen"), "Screen");
                var screenName = AiFuiImporterUtility.MakeUniqueName(baseScreenName, usedScreenNames);
                var ussPath = AiFuiImporterUtility.CombineAssetPath(ussRoot, screenName + ".uss");
                var uxmlPath = AiFuiImporterUtility.CombineAssetPath(uxmlRoot, screenName + ".uxml");
                var sourcePath = AiFuiImporterUtility.CombineAssetPath(infoRoot, "screen_" + screenName + ".json");

                var textGradientFolder = AiFuiImporterUtility.CombineAssetPath(AiFuiImporterUtility.CombineAssetPath(outputRoot, "Resources"), "Color Gradient Presets");
                AiFuiImporterUtility.EnsureAssetFolder(textGradientFolder);
                var generator = new FuiUiToolkitGenerator(screen, ussPath, assetPathMap, fontPathMap, package.FontMetadata, textGradientFolder, report);
                var generated = generator.Generate();

                WriteTextAsset(uxmlPath, generated.Uxml);
                WriteTextAsset(ussPath, generated.Uss);
                WriteTextAsset(sourcePath, FuiJson.Serialize(screen));

                report.GeneratedUxml.Add(uxmlPath);
                report.GeneratedUss.Add(ussPath);
                report.ScreenCount++;
                generatedScreens.Add(new FuiGeneratedScreenAsset
                {
                    Name = screenName,
                    UxmlPath = uxmlPath,
                    UssPath = ussPath,
                    Width = FuiJson.GetDouble(screen, "width", 0),
                    Height = FuiJson.GetDouble(screen, "height", 0)
                });
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var textSettings = CreateTextSettingsAsset(outputRoot, projectName, report);
            var runtimeTheme = CreateRuntimeThemeStyleSheet(outputRoot, projectName, generatedScreens, report);

            PanelSettings panelSettings = null;
            if (options.CreatePanelSettings || options.CreateScreenPrefabs || options.CreateSceneAssets || options.AddScreensToOpenScene)
            {
                panelSettings = CreatePanelSettingsAsset(outputRoot, projectName, generatedScreens, runtimeTheme, textSettings, report);
            }

            if (options.CreateScreenPrefabs)
            {
                CreateScreenPrefabs(outputRoot, generatedScreens, panelSettings, report);
            }

            if (options.CreateSceneAssets)
            {
                CreateSceneAssets(outputRoot, projectName, generatedScreens, panelSettings, report);
            }

            if (options.AddScreensToOpenScene)
            {
                CreateSceneObjects(projectName, generatedScreens, panelSettings, report);
            }

            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(infoRoot, "fui-import-report.json"), BuildReportJson(report));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (options.OpenFirstUxmlAfterImport && report.GeneratedUxml.Count > 0)
            {
                var firstUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(report.GeneratedUxml[0]);
                if (firstUxml != null)
                {
                    var w = generatedScreens.Count > 0 ? generatedScreens[0].Width : 0;
                    var h = generatedScreens.Count > 0 ? generatedScreens[0].Height : 0;
                    EditorApplication.delayCall += () => { AssetDatabase.OpenAsset(firstUxml); TryEnableUiBuilderMatchGameView(w, h); };
                }
            }

            return report;
        }


        private static void TryEnableUiBuilderMatchGameView(double width, double height)
        {
            TryEnableUiBuilderMatchGameView((float)Math.Max(1, width), (float)Math.Max(1, height));
        }

        private static void TryEnableUiBuilderMatchGameView(float width, float height)
        {
            // UI Builder stores Canvas options outside UXML. The public manual says Match Game View
            // is a Canvas checkbox, but Unity doesn't expose a stable public API for it, so this pass
            // uses guarded reflection and retries while UI Builder finishes opening the document.
            for (var i = 1; i <= 12; i++)
            {
                var delay = i;
                EditorApplication.delayCall += () =>
                {
                    try { ConfigureAllOpenUiBuilderWindows(width, height); }
                    catch { }
                };
            }
        }

        private static void ConfigureAllOpenUiBuilderWindows(float width, float height)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                var type = window.GetType();
                var typeName = type.FullName ?? type.Name;
                if (typeName.IndexOf("Builder", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (typeName.IndexOf("UI", StringComparison.OrdinalIgnoreCase) < 0 && typeName.IndexOf("Ui", StringComparison.OrdinalIgnoreCase) < 0) continue;

                var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                TryConfigureUiBuilderObject(window, width, height, 0, visited);
                TryInvokeUiBuilderCanvasMethods(window, type, width, height);
                window.Repaint();
            }
        }

        private static void TryConfigureUiBuilderObject(object target, float width, float height, int depth, HashSet<object> visited)
        {
            if (target == null || depth > 5) return;
            var targetType = target.GetType();
            if (targetType.IsPrimitive || target is string || target is UnityEngine.Object && !(target is EditorWindow)) return;
            if (!visited.Add(target)) return;

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            TrySetUiBuilderMembers(target, targetType, width, height);

            foreach (var field in targetType.GetFields(flags))
            {
                try
                {
                    if (field.FieldType.IsPrimitive || field.FieldType == typeof(string)) continue;
                    var value = field.GetValue(target);
                    if (value == null) continue;
                    var name = (field.Name + " " + field.FieldType.FullName).ToLowerInvariant();
                    if (name.Contains("canvas") || name.Contains("viewport") || name.Contains("builder") || name.Contains("document"))
                        TryConfigureUiBuilderObject(value, width, height, depth + 1, visited);
                }
                catch { }
            }

            foreach (var prop in targetType.GetProperties(flags))
            {
                try
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length != 0) continue;
                    if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)) continue;
                    var name = (prop.Name + " " + prop.PropertyType.FullName).ToLowerInvariant();
                    if (!name.Contains("canvas") && !name.Contains("viewport") && !name.Contains("builder") && !name.Contains("document")) continue;
                    var value = prop.GetValue(target, null);
                    TryConfigureUiBuilderObject(value, width, height, depth + 1, visited);
                }
                catch { }
            }
        }

        private static void TrySetUiBuilderMembers(object target, Type type, float width, float height)
        {
            if (target == null || type == null) return;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var prop in type.GetProperties(flags))
            {
                try
                {
                    if (!prop.CanWrite || prop.GetIndexParameters().Length != 0) continue;
                    var key = (prop.Name ?? string.Empty).ToLowerInvariant();
                    if (prop.PropertyType == typeof(bool) && key.Contains("match") && key.Contains("game") && key.Contains("view")) prop.SetValue(target, true, null);
                    else if ((prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double) || prop.PropertyType == typeof(int)) && key.Contains("canvas"))
                    {
                        if (key.Contains("width")) SetNumericProperty(prop, target, width);
                        else if (key.Contains("height")) SetNumericProperty(prop, target, height);
                    }
                }
                catch { }
            }
            foreach (var field in type.GetFields(flags))
            {
                try
                {
                    var key = (field.Name ?? string.Empty).ToLowerInvariant();
                    if (field.FieldType == typeof(bool) && key.Contains("match") && key.Contains("game") && key.Contains("view")) field.SetValue(target, true);
                    else if ((field.FieldType == typeof(float) || field.FieldType == typeof(double) || field.FieldType == typeof(int)) && key.Contains("canvas"))
                    {
                        if (key.Contains("width")) SetNumericField(field, target, width);
                        else if (key.Contains("height")) SetNumericField(field, target, height);
                    }
                }
                catch { }
            }
        }

        private static void TryInvokeUiBuilderCanvasMethods(object target, Type type, float width, float height)
        {
            if (target == null || type == null) return;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var method in type.GetMethods(flags))
            {
                try
                {
                    var name = (method.Name ?? string.Empty).ToLowerInvariant();
                    var ps = method.GetParameters();
                    if (name.Contains("match") && name.Contains("game") && name.Contains("view") && ps.Length == 0) method.Invoke(target, null);
                    else if (name.Contains("match") && name.Contains("game") && name.Contains("view") && ps.Length == 1 && ps[0].ParameterType == typeof(bool)) method.Invoke(target, new object[] { true });
                    else if (name.Contains("canvas") && name.Contains("size") && ps.Length == 2) method.Invoke(target, new object[] { Convert.ChangeType(width, ps[0].ParameterType), Convert.ChangeType(height, ps[1].ParameterType) });
                }
                catch { }
            }
        }

        private static void SetNumericProperty(System.Reflection.PropertyInfo prop, object target, float value)
        {
            if (prop.PropertyType == typeof(float)) prop.SetValue(target, value, null);
            else if (prop.PropertyType == typeof(double)) prop.SetValue(target, (double)value, null);
            else if (prop.PropertyType == typeof(int)) prop.SetValue(target, Mathf.RoundToInt(value), null);
        }

        private static void SetNumericField(System.Reflection.FieldInfo field, object target, float value)
        {
            if (field.FieldType == typeof(float)) field.SetValue(target, value);
            else if (field.FieldType == typeof(double)) field.SetValue(target, (double)value);
            else if (field.FieldType == typeof(int)) field.SetValue(target, Mathf.RoundToInt(value));
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }

        private static string PrepareOutputRoot(string outputRoot, bool overwrite)
        {
            outputRoot = AiFuiImporterUtility.NormalizeAssetPath(outputRoot);
            if (overwrite)
            {
                CleanGeneratedOutputRoot(outputRoot);
                AiFuiImporterUtility.EnsureAssetFolder(outputRoot);
                return outputRoot;
            }

            if (!AssetDatabase.IsValidFolder(outputRoot))
            {
                AiFuiImporterUtility.EnsureAssetFolder(outputRoot);
                return outputRoot;
            }

            var basePath = outputRoot;
            for (var i = 2; i < 1000; i++)
            {
                var candidate = basePath + "_" + i.ToString(CultureInfo.InvariantCulture);
                if (!AssetDatabase.IsValidFolder(candidate))
                {
                    AiFuiImporterUtility.EnsureAssetFolder(candidate);
                    return candidate;
                }
            }
            throw new IOException("Не удалось создать уникальную папку вывода для " + outputRoot);
        }

        private static void CleanGeneratedOutputRoot(string outputRoot)
        {
            if (string.IsNullOrEmpty(outputRoot)) return;
            outputRoot = AiFuiImporterUtility.NormalizeAssetPath(outputRoot);
            if (outputRoot == "Assets") return;
            if (!outputRoot.StartsWith("Assets/", StringComparison.Ordinal)) return;
            if (!AssetDatabase.IsValidFolder(outputRoot)) return;

            // Полная очистка важна: старые импорты 1.0.8 могли оставить битые SDF FontAsset
            // без m_Material. Если их не удалить, Unity продолжает спамить ошибками даже после нового импорта.
            AssetDatabase.DeleteAsset(outputRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteTextAsset(string assetPath, string text)
        {
            var absolute = AiFuiImporterUtility.ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, text ?? string.Empty, new UTF8Encoding(false));
        }

        private static void WriteBinaryAsset(string assetPath, byte[] bytes)
        {
            var absolute = AiFuiImporterUtility.ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, bytes ?? new byte[0]);
        }

        private static void ApplyUiTextureImportSettings(string assetPath, FuiAssetMeta meta)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            // Все FUI-картинки должны быть Sprite (2D and UI), чтобы их можно было
            // использовать как UI sprites и чтобы 9-slice Border был доступен в Sprite Editor.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (meta != null && meta.HasSpriteBorder)
            {
                // Unity TextureImporter.spriteBorder хранит Vector4(left, bottom, right, top).
                importer.spriteBorder = new Vector4(
                    Mathf.Max(0, meta.BorderLeft),
                    Mathf.Max(0, meta.BorderBottom),
                    Mathf.Max(0, meta.BorderRight),
                    Mathf.Max(0, meta.BorderTop));
            }
            else
            {
                importer.spriteBorder = Vector4.zero;
            }

            importer.SaveAndReimport();
        }

        private static void ApplyUiFontImportSettings(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TrueTypeFontImporter;
            if (importer == null) return;
            importer.includeFontData = true;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.SaveAndReimport();
        }

        private static void ReplaceFontFilesWithSdfFontAssets(Dictionary<string, string> fontPathMap, string outputRoot, FuiImportReport report)
        {
            if (fontPathMap == null || fontPathMap.Count == 0) return;
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in fontPathMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList())
            {
                var sdf = CreateSdfFontAsset(path, outputRoot, report);
                if (!string.IsNullOrEmpty(sdf)) replacements[path] = sdf;
            }
            if (replacements.Count == 0) return;
            var keys = fontPathMap.Keys.ToList();
            foreach (var key in keys)
            {
                var value = fontPathMap[key];
                string sdf;
                if (replacements.TryGetValue(value, out sdf)) fontPathMap[key] = sdf;
            }
        }

        private static string CreateSdfFontAsset(string fontPath, string outputRoot, FuiImportReport report)
        {
            try
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                if (font == null) return string.Empty;
                var fontAssetType = Type.GetType("UnityEngine.TextCore.Text.FontAsset, UnityEngine.TextCoreTextEngineModule")
                    ?? Type.GetType("UnityEngine.TextCore.Text.FontAsset, UnityEngine.TextCoreFontEngineModule")
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.TextCore.Text.FontAsset")).FirstOrDefault(t => t != null);
                if (fontAssetType == null) return string.Empty;
                var method = fontAssetType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "CreateFontAsset" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(Font));
                if (method == null) return string.Empty;

                var folder = AiFuiImporterUtility.CombineAssetPath(AiFuiImporterUtility.CombineAssetPath(outputRoot, "Resources"), "Fonts & Materials");
                AiFuiImporterUtility.EnsureAssetFolder(folder);
                var assetName = AiFuiImporterUtility.SanitizeFileName(Path.GetFileNameWithoutExtension(fontPath) + " SDF", "FUI_Font_SDF");
                var assetPath = AiFuiImporterUtility.CombineAssetPath(folder, assetName + ".asset");
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (existing != null) return assetPath;

                object created;
                var parameters = method.GetParameters();
                if (parameters.Length == 1) created = method.Invoke(null, new object[] { font });
                else
                {
                    var args = new object[parameters.Length];
                    args[0] = font;
                    for (var i = 1; i < args.Length; i++) args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : GetDefaultValue(parameters[i].ParameterType);
                    created = method.Invoke(null, args);
                }
                var obj = created as UnityEngine.Object;
                if (obj == null) return string.Empty;
                AssetDatabase.CreateAsset(obj, assetPath);
                EditorUtility.SetDirty(obj);
                return assetPath;
            }
            catch (Exception ex)
            {
                if (report != null)
                {
                    report.WarningCount++;
                    report.Warnings.Add("Не удалось создать SDF Font Asset для " + fontPath + ": " + ex.Message);
                }
                return string.Empty;
            }
        }

        private static object GetDefaultValue(Type type)
        {
            return type != null && type.IsValueType ? Activator.CreateInstance(type) : null;
        }




        private static UnityEngine.Object CreateTextSettingsAsset(string outputRoot, string projectName, FuiImportReport report)
        {
            try
            {
                var textSettingsType = Type.GetType("UnityEngine.UIElements.TextSettings, UnityEngine.UIElementsModule")
                    ?? Type.GetType("UnityEngine.UIElements.PanelTextSettings, UnityEngine.UIElementsModule")
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.UIElements.TextSettings") ?? a.GetType("UnityEngine.UIElements.PanelTextSettings")).FirstOrDefault(t => t != null);
                if (textSettingsType == null)
                {
                    AddReportWarning(report, "Unity Panel Text Settings asset не создан: тип UnityEngine.UIElements.TextSettings не найден в этой версии Unity.");
                    return null;
                }

                var folder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "PanelSettings");
                AiFuiImporterUtility.EnsureAssetFolder(folder);
                var assetPath = AiFuiImporterUtility.CombineAssetPath(folder, AiFuiImporterUtility.SanitizeFileName(projectName + "_TextSettings", "FUI_TextSettings") + ".asset");
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                var textSettings = existing != null ? existing : ScriptableObject.CreateInstance(textSettingsType);
                if (textSettings == null) return null;

                var so = new SerializedObject(textSettings);
                // Unity 6.5 resolves <gradient="name"> through Panel Text Settings and Resources.Load.
                // The preset assets are stored in Assets/<Project>/Resources/Color Gradient Presets,
                // but the Text Settings path itself must be the Resources-relative folder name.
                var resourcesFolder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Resources");
                AiFuiImporterUtility.EnsureAssetFolder(resourcesFolder);
                AiFuiImporterUtility.EnsureAssetFolder(AiFuiImporterUtility.CombineAssetPath(resourcesFolder, "Color Gradient Presets"));
                AiFuiImporterUtility.EnsureAssetFolder(AiFuiImporterUtility.CombineAssetPath(resourcesFolder, "Fonts & Materials"));
                AiFuiImporterUtility.EnsureAssetFolder(AiFuiImporterUtility.CombineAssetPath(resourcesFolder, "Sprite Assets"));
                AiFuiImporterUtility.EnsureAssetFolder(AiFuiImporterUtility.CombineAssetPath(resourcesFolder, "Text Style Sheets"));

                var colorGradientPresetPath = "Color Gradient Presets";
                var fontAssetPath = "Fonts & Materials";
                var spriteAssetPath = "Sprite Assets";
                var styleSheetPath = "Text Style Sheets";

                TrySetSerializedStringAny(so, new[]
                {
                    "m_ColorGradientPresetsPath",
                    "m_ColorGradientPresetPath",
                    "m_ColorGradientPath",
                    "m_TextColorGradientPresetsPath",
                    "m_ColorGradientPresetAssetsPath",
                    "m_ColorGradientPresetAssetPath",
                    "m_DefaultColorGradientPresetsPath",
                    "m_DefaultColorGradientPresetPath",
                    "colorGradientPresetsPath",
                    "colorGradientPresetPath",
                    "colorGradientPath",
                    "textColorGradientPresetsPath",
                    "defaultColorGradientPresetsPath",
                    "defaultColorGradientPresetPath"
                }, colorGradientPresetPath);
                TrySetSerializedStringPropertiesContaining(so, new[] { "gradient" }, new[] { "path" }, colorGradientPresetPath);

                TrySetSerializedStringAny(so, new[]
                {
                    "m_FontAssetPath",
                    "m_FontAssetsPath",
                    "m_DefaultFontAssetPath",
                    "fontAssetPath",
                    "fontAssetsPath",
                    "defaultFontAssetPath"
                }, fontAssetPath);
                TrySetSerializedStringAny(so, new[]
                {
                    "m_SpriteAssetPath",
                    "m_SpriteAssetsPath",
                    "m_DefaultSpriteAssetPath",
                    "spriteAssetPath",
                    "spriteAssetsPath",
                    "defaultSpriteAssetPath"
                }, spriteAssetPath);
                TrySetSerializedStringAny(so, new[]
                {
                    "m_StyleSheetPath",
                    "m_StyleSheetsPath",
                    "m_DefaultStyleSheetPath",
                    "styleSheetPath",
                    "styleSheetsPath",
                    "defaultStyleSheetPath"
                }, styleSheetPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                if (existing == null) AssetDatabase.CreateAsset(textSettings, assetPath);
                else EditorUtility.SetDirty(textSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) ?? textSettings;
            }
            catch (Exception ex)
            {
                AddReportWarning(report, "Не удалось создать Unity Panel Text Settings asset: " + ex.Message);
                return null;
            }
        }

        private static ThemeStyleSheet CreateRuntimeThemeStyleSheet(string outputRoot, string projectName, List<FuiGeneratedScreenAsset> screens, FuiImportReport report)
        {
            try
            {
                if (screens == null || screens.Count == 0) return null;
                var folder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "PanelSettings");
                AiFuiImporterUtility.EnsureAssetFolder(folder);
                var themePath = AiFuiImporterUtility.CombineAssetPath(folder, AiFuiImporterUtility.SanitizeFileName(projectName + "_RuntimeTheme", "FUI_RuntimeTheme") + ".tss");
                var sb = new StringBuilder();
                sb.AppendLine("/* Auto-generated MTK Figma UI Import runtime theme. */");
                sb.AppendLine("/* ВАЖНО: RuntimeTheme должен быть базовой Unity-темой, а не контейнером всех экранных USS. */");
                sb.AppendLine("/* Экранные USS уже подключаются напрямую внутри каждого UXML через <Style>. */");
                sb.AppendLine("/* Если импортировать все USS сюда, одинаковые fallback-классы разных экранов начинают конфликтовать: */");
                sb.AppendLine("/* например .fui_element_0001 из loading может переопределить .fui_element_0001 из main_menu. */");
                sb.AppendLine("@import url(\"unity-theme://default\");");
                WriteTextAsset(themePath, sb.ToString());
                AssetDatabase.ImportAsset(themePath, ImportAssetOptions.ForceSynchronousImport);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                if (theme == null) AddReportWarning(report, "RuntimeTheme .tss создан, но Unity не смогла загрузить его как ThemeStyleSheet: " + themePath);
                return theme;
            }
            catch (Exception ex)
            {
                AddReportWarning(report, "Не удалось создать RuntimeTheme.tss: " + ex.Message);
                return null;
            }
        }

        private static PanelSettings CreatePanelSettingsAsset(string outputRoot, string projectName, List<FuiGeneratedScreenAsset> screens, ThemeStyleSheet runtimeTheme, UnityEngine.Object textSettings, FuiImportReport report)
        {
            var folder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "PanelSettings");
            AiFuiImporterUtility.EnsureAssetFolder(folder);
            var assetName = AiFuiImporterUtility.SanitizeFileName(projectName + "_PanelSettings", "FUI_PanelSettings");
            var assetPath = AiFuiImporterUtility.CombineAssetPath(folder, assetName + ".asset");

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, assetPath);
            }

            var reference = GetReferenceResolution(screens);
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = reference;
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            if (runtimeTheme != null)
            {
                TrySetMemberObject(panelSettings, "themeStyleSheet", runtimeTheme);
                var soTheme = new SerializedObject(panelSettings);
                TrySetSerializedObjectReference(soTheme, "m_ThemeStyleSheet", runtimeTheme);
                soTheme.ApplyModifiedPropertiesWithoutUndo();
            }
            if (textSettings != null)
            {
                TrySetMemberObject(panelSettings, "textSettings", textSettings);
                var soText = new SerializedObject(panelSettings);
                TrySetSerializedObjectReference(soText, "m_TextSettings", textSettings);
                TrySetSerializedObjectReference(soText, "textSettings", textSettings);
                soText.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();

            if (report != null) report.PanelSettingsAssetPath = assetPath;
            return panelSettings;
        }

        private static Vector2Int GetReferenceResolution(List<FuiGeneratedScreenAsset> screens)
        {
            if (screens != null)
            {
                foreach (var screen in screens)
                {
                    if (screen != null && screen.Width > 0 && screen.Height > 0)
                        return new Vector2Int(Mathf.RoundToInt((float)screen.Width), Mathf.RoundToInt((float)screen.Height));
                }
            }
            return new Vector2Int(1920, 1080);
        }

        private static void CreateScreenPrefabs(string outputRoot, List<FuiGeneratedScreenAsset> screens, PanelSettings panelSettings, FuiImportReport report)
        {
            // В Unity 6.5 PanelRenderer внутри prefab asset может ломать AssetPreviewUpdater:
            // Invalid worldAABB / localAABB / Preview Scene Camera NaN.
            // Поэтому importer создаёт готовые сцены с PanelRenderer, а prefab assets не генерирует.
            // Сгенерированные UXML/USS/PanelSettings полностью рабочие и не зависят от этого пакета.
            AddReportWarning(report, "Префабы с PanelRenderer не созданы: в Unity 6.5 preview префабов может выдавать Invalid AABB/NaN. Используйте созданные сцены или добавьте PanelRenderer вручную к нужному экрану.");
        }



        private static void CreateSceneAssets(string outputRoot, string projectName, List<FuiGeneratedScreenAsset> screens, PanelSettings panelSettings, FuiImportReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AddReportWarning(report, "Сцены не созданы, потому что Unity переходит в Play Mode.");
                return;
            }
            if (screens == null || screens.Count == 0) return;

            var folder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Scenes");
            AiFuiImporterUtility.EnsureAssetFolder(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Общая сцена со всеми экранами больше не создаётся: она мешала симуляции и могла
            // накладывать экраны друг на друга. Генерируем только отдельную сцену на каждый экран.
            foreach (var screen in screens)
            {
                if (screen == null) continue;
                var scenePath = AiFuiImporterUtility.CombineAssetPath(folder, screen.Name + ".unity");
                CreateSingleScreenScene(scenePath, screen, panelSettings, report);
            }
        }

        private static int GetDefaultActiveScreenIndex(List<FuiGeneratedScreenAsset> screens)
        {
            if (screens == null || screens.Count == 0) return 0;
            for (var i = 0; i < screens.Count; i++)
            {
                var n = (screens[i] != null ? screens[i].Name : string.Empty).ToLowerInvariant();
                if ((n.Contains("main") || n.Contains("menu")) && !n.Contains("loading") && !n.Contains("background")) return i;
            }
            for (var i = 0; i < screens.Count; i++)
            {
                var n = (screens[i] != null ? screens[i].Name : string.Empty).ToLowerInvariant();
                if (!n.Contains("loading") && !n.Contains("background")) return i;
            }
            return 0;
        }

        private static void CreateAllScreensScene(string sceneAssetPath, string projectName, List<FuiGeneratedScreenAsset> screens, PanelSettings panelSettings, FuiImportReport report)
        {
            Scene scene = default(Scene);
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                scene.name = Path.GetFileNameWithoutExtension(sceneAssetPath);

                var rootName = "FUI_" + AiFuiImporterUtility.SanitizeIdentifier(projectName, "Imported") + "_Screens";
                var root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);

                var defaultActiveIndex = GetDefaultActiveScreenIndex(screens);
                for (var i = 0; i < screens.Count; i++)
                {
                    var go = CreateUiToolkitPanelObject(screens[i], panelSettings, i, i == defaultActiveIndex, report);
                    if (go == null) continue;
                    SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.SetParent(root.transform, false);
                }

                EditorSceneManager.SaveScene(scene, sceneAssetPath);
                if (report != null) report.GeneratedScenes.Add(sceneAssetPath);
            }
            catch (Exception ex)
            {
                AddReportWarning(report, "Не удалось создать общую сцену " + sceneAssetPath + ": " + ex.Message);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateSingleScreenScene(string sceneAssetPath, FuiGeneratedScreenAsset screen, PanelSettings panelSettings, FuiImportReport report)
        {
            Scene scene = default(Scene);
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                scene.name = Path.GetFileNameWithoutExtension(sceneAssetPath);

                var go = CreateUiToolkitPanelObject(screen, panelSettings, 0, true, report);
                if (go != null) SceneManager.MoveGameObjectToScene(go, scene);

                EditorSceneManager.SaveScene(scene, sceneAssetPath);
                if (report != null) report.GeneratedScenes.Add(sceneAssetPath);
            }
            catch (Exception ex)
            {
                AddReportWarning(report, "Не удалось создать сцену " + sceneAssetPath + ": " + ex.Message);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject CreateUiToolkitPanelObject(FuiGeneratedScreenAsset screen, PanelSettings panelSettings, int sortingOrder, bool active, FuiImportReport report)
        {
            if (screen == null) return null;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(screen.UxmlPath);
            if (visualTree == null)
            {
                AddReportWarning(report, "Не удалось создать UI-объект. Не найден UXML: " + screen.UxmlPath);
                return null;
            }

            var go = new GameObject(screen.Name);
            go.SetActive(false);

            // Unity 6.5: создаём актуальный Panel Renderer, чтобы не было warning миграции runtime UI.
            var renderer = go.AddComponent<PanelRenderer>();
            renderer.panelSettings = panelSettings;
            renderer.visualTreeAsset = visualTree;
            renderer.sortingOrder = sortingOrder;

            go.SetActive(active);
            return go;
        }

        private static Type FindPanelRendererType()
        {
            var direct = Type.GetType("UnityEngine.UIElements.PanelRenderer, UnityEngine.UIElementsModule");
            if (direct != null) return direct;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    var type = asm.GetType("UnityEngine.UIElements.PanelRenderer");
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }

        private static bool AssignPanelRenderer(Component renderer, VisualTreeAsset visualTree, PanelSettings panelSettings, int sortingOrder)
        {
            if (renderer == null) return false;
            var assignedSource = false;
            assignedSource |= TrySetMember(renderer, "visualTreeAsset", visualTree);
            assignedSource |= TrySetMember(renderer, "sourceAsset", visualTree);
            assignedSource |= TrySetMember(renderer, "source", visualTree);
            assignedSource |= TrySetMember(renderer, "uxml", visualTree);
            TrySetMember(renderer, "panelSettings", panelSettings);
            TrySetMember(renderer, "sortingOrder", sortingOrder);
            TrySetMember(renderer, "sortOrder", sortingOrder);

            var so = new SerializedObject(renderer);
            assignedSource |= TrySetSerializedObjectReference(so, "m_VisualTreeAsset", visualTree);
            assignedSource |= TrySetSerializedObjectReference(so, "m_SourceAsset", visualTree);
            assignedSource |= TrySetSerializedObjectReference(so, "m_Source", visualTree);
            assignedSource |= TrySetSerializedObjectReference(so, "m_Uxml", visualTree);
            TrySetSerializedObjectReference(so, "m_PanelSettings", panelSettings);
            TrySetSerializedInt(so, "m_SortingOrder", sortingOrder);
            TrySetSerializedInt(so, "m_SortOrder", sortingOrder);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            return assignedSource;
        }


        private static bool TrySetMemberObject(object target, string memberName, object value)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return false;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var type = target.GetType();
            try
            {
                var prop = type.GetProperty(memberName, flags);
                if (prop != null && prop.CanWrite && (value == null || prop.PropertyType.IsAssignableFrom(value.GetType())))
                {
                    prop.SetValue(target, value, null);
                    return true;
                }
            }
            catch { }
            try
            {
                var field = type.GetField(memberName, flags);
                if (field != null && (value == null || field.FieldType.IsAssignableFrom(value.GetType())))
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TrySetMember(Component target, string memberName, object value)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return false;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var type = target.GetType();
            try
            {
                var prop = type.GetProperty(memberName, flags);
                if (prop != null && prop.CanWrite && value != null && prop.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    prop.SetValue(target, value, null);
                    return true;
                }
            }
            catch { }
            try
            {
                var field = type.GetField(memberName, flags);
                if (field != null && value != null && field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TrySetSerializedObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            if (so == null || string.IsNullOrEmpty(propertyName)) return false;
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference) return false;
            prop.objectReferenceValue = value;
            return true;
        }

        private static bool TrySetSerializedInt(SerializedObject so, string propertyName, int value)
        {
            if (so == null || string.IsNullOrEmpty(propertyName)) return false;
            var prop = so.FindProperty(propertyName);
            if (prop == null) return false;
            if (prop.propertyType == SerializedPropertyType.Integer) { prop.intValue = value; return true; }
            return false;
        }


        private static bool TrySetSerializedStringAny(SerializedObject so, IEnumerable<string> propertyNames, string value)
        {
            if (so == null || propertyNames == null) return false;
            var changed = false;
            foreach (var propertyName in propertyNames)
            {
                if (string.IsNullOrEmpty(propertyName)) continue;
                var prop = so.FindProperty(propertyName);
                if (prop == null || prop.propertyType != SerializedPropertyType.String) continue;
                prop.stringValue = value ?? string.Empty;
                changed = true;
            }
            return changed;
        }

        private static bool TrySetSerializedStringPropertiesContaining(SerializedObject so, IEnumerable<string> requiredNameParts, IEnumerable<string> requiredPathParts, string value)
        {
            if (so == null) return false;
            var changed = false;
            var iterator = so.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.String) continue;
                var path = (iterator.propertyPath ?? string.Empty).ToLowerInvariant();
                var displayName = (iterator.displayName ?? string.Empty).ToLowerInvariant();
                if (!ContainsAll(path + " " + displayName, requiredNameParts)) continue;
                if (!ContainsAny(path + " " + displayName, requiredPathParts)) continue;
                iterator.stringValue = value ?? string.Empty;
                changed = true;
            }
            return changed;
        }

        private static bool ContainsAll(string text, IEnumerable<string> parts)
        {
            if (parts == null) return true;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (text == null || text.IndexOf(part.ToLowerInvariant(), StringComparison.Ordinal) < 0) return false;
            }
            return true;
        }

        private static bool ContainsAny(string text, IEnumerable<string> parts)
        {
            if (parts == null) return true;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (text != null && text.IndexOf(part.ToLowerInvariant(), StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        private static void CreateSceneObjects(string projectName, List<FuiGeneratedScreenAsset> screens, PanelSettings panelSettings, FuiImportReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AddReportWarning(report, "Объекты сцены не созданы, потому что Unity переходит в Play Mode.");
                return;
            }
            if (screens == null || screens.Count == 0) return;

            var rootName = "FUI_" + AiFuiImporterUtility.SanitizeIdentifier(projectName, "Imported") + "_Screens";
            var existing = GameObject.Find(rootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            var root = new GameObject(rootName);
            var index = 0;
            foreach (var screen in screens)
            {
                var go = CreateUiToolkitPanelObject(screen, panelSettings, index, index == 0, report);
                if (go == null) continue;
                go.transform.SetParent(root.transform, false);
                index++;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            if (report != null) report.SceneRootObjectName = rootName;
        }

        private static void AddReportWarning(FuiImportReport report, string message)
        {
            if (report == null || string.IsNullOrEmpty(message)) return;
            report.WarningCount++;
            report.Warnings.Add(message);
        }

        private static string BuildReportJson(FuiImportReport report)
        {
            var dict = new Dictionary<string, object>();
            dict["packageName"] = report.PackageName;
            dict["outputRoot"] = report.OutputRootAssetPath;
            dict["screenCount"] = report.ScreenCount;
            dict["textureCount"] = report.CopiedTextureCount;
            dict["fontCount"] = report.CopiedFontCount;
            dict["warnings"] = report.Warnings;
            dict["uxml"] = report.GeneratedUxml;
            dict["uss"] = report.GeneratedUss;
            dict["prefabs"] = report.GeneratedPrefabs;
            dict["scenes"] = report.GeneratedScenes;
            dict["panelSettings"] = report.PanelSettingsAssetPath;
            dict["sceneRoot"] = report.SceneRootObjectName;
            return FuiJson.Serialize(dict);
        }
    }

    internal sealed class FuiPackage
    {
        public string ProjectName = "FUI";
        public string ManifestJson = "{}";
        public string MetadataJson = "{}";
        public string AssetsJson = "[]";
        public string FontsJson = "{}";
        public readonly List<Dictionary<string, object>> Screens = new List<Dictionary<string, object>>();
        public readonly List<FuiZipFile> AssetFiles = new List<FuiZipFile>();
        public readonly List<FuiZipFile> FontFiles = new List<FuiZipFile>();
        public readonly List<FuiAssetMeta> AssetMetadata = new List<FuiAssetMeta>();
        public readonly List<FuiFontMeta> FontMetadata = new List<FuiFontMeta>();

        public static FuiPackage Read(string fuiFilePath)
        {
            try
            {
                var entries = new List<FuiArchiveFile>();
                using (var stream = File.OpenRead(fuiFilePath))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var path = AiFuiImporterUtility.NormalizePackagePath(entry.FullName);
                        if (string.IsNullOrEmpty(path) || path.EndsWith("/", StringComparison.Ordinal)) continue;
                        entries.Add(new FuiArchiveFile(path, ReadEntryBytes(entry)));
                    }
                }
                return FromArchiveFiles(fuiFilePath, entries);
            }
            catch (Exception zipException)
            {
                Debug.LogWarning("[MTK | Figma UI Import] Standard ZipArchive read failed. Trying tolerant FUI zip reader. Reason: " + zipException.Message);
                var entries = FuiTolerantZipReader.ReadStoredZip(fuiFilePath);
                return FromArchiveFiles(fuiFilePath, entries);
            }
        }

        private static FuiPackage FromArchiveFiles(string fuiFilePath, List<FuiArchiveFile> files)
        {
            var package = new FuiPackage();
            foreach (var file in files)
            {
                var path = AiFuiImporterUtility.NormalizePackagePath(file.Path);
                if (string.IsNullOrEmpty(path) || path.EndsWith("/", StringComparison.Ordinal)) continue;

                if (string.Equals(path, "manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    package.ManifestJson = Encoding.UTF8.GetString(file.Bytes ?? new byte[0]);
                    var manifest = FuiJson.Deserialize(package.ManifestJson) as Dictionary<string, object>;
                    package.ProjectName = FuiJson.GetString(manifest, "projectName", FuiJson.GetString(manifest, "name", Path.GetFileNameWithoutExtension(fuiFilePath)));
                }
                else if (string.Equals(path, "metadata.json", StringComparison.OrdinalIgnoreCase))
                {
                    package.MetadataJson = Encoding.UTF8.GetString(file.Bytes ?? new byte[0]);
                }
                else if (string.Equals(path, "assets/assets.json", StringComparison.OrdinalIgnoreCase))
                {
                    package.AssetsJson = Encoding.UTF8.GetString(file.Bytes ?? new byte[0]);
                    package.AssetMetadata.AddRange(ParseAssetMetadata(package.AssetsJson));
                }
                else if (string.Equals(path, "fonts/fonts.json", StringComparison.OrdinalIgnoreCase))
                {
                    package.FontsJson = Encoding.UTF8.GetString(file.Bytes ?? new byte[0]);
                }
                else if (path.StartsWith("screens/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var json = Encoding.UTF8.GetString(file.Bytes ?? new byte[0]);
                    var screen = FuiJson.Deserialize(json) as Dictionary<string, object>;
                    if (screen != null) package.Screens.Add(screen);
                }
                else if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) && IsImagePath(path))
                {
                    package.AssetFiles.Add(new FuiZipFile(path, file.Bytes));
                }
                else if (path.StartsWith("fonts/", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("/", StringComparison.Ordinal))
                {
                    package.FontFiles.Add(new FuiZipFile(path, file.Bytes));
                }
            }

            package.FontMetadata.AddRange(ParseFontMetadata(package.FontsJson));
            if (package.Screens.Count == 0) throw new InvalidDataException("В FUI пакете не найдены screens/*.json.");
            return package;
        }

        private static bool IsImagePath(string path)
        {
            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadEntryText(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }

        private static IEnumerable<FuiAssetMeta> ParseAssetMetadata(string json)
        {
            var list = FuiJson.Deserialize(json) as List<object>;
            if (list == null) yield break;
            foreach (var item in list)
            {
                var dict = item as Dictionary<string, object>;
                if (dict == null) continue;
                var meta = new FuiAssetMeta
                {
                    Id = FuiJson.GetString(dict, "id", string.Empty),
                    Path = FuiJson.GetString(dict, "path", string.Empty)
                };
                var border = FuiJson.GetObject(dict, "nineSliceBorder") ?? FuiJson.GetObject(FuiJson.GetObject(dict, "sprite"), "border");
                if (border != null)
                {
                    meta.BorderLeft = (float)FuiJson.GetDouble(border, "left", 0);
                    meta.BorderRight = (float)FuiJson.GetDouble(border, "right", 0);
                    meta.BorderTop = (float)FuiJson.GetDouble(border, "top", 0);
                    meta.BorderBottom = (float)FuiJson.GetDouble(border, "bottom", 0);
                    meta.HasSpriteBorder = meta.BorderLeft > 0 || meta.BorderRight > 0 || meta.BorderTop > 0 || meta.BorderBottom > 0;
                }
                yield return meta;
            }
        }

        private static IEnumerable<FuiFontMeta> ParseFontMetadata(string json)
        {
            var root = FuiJson.Deserialize(string.IsNullOrEmpty(json) ? "{}" : json) as Dictionary<string, object>;
            if (root == null) yield break;

            var uploaded = FuiJson.GetArray(root, "uploadedFonts");
            var figmaFonts = FuiJson.GetArray(root, "figmaFonts");
            var uploadedList = new List<FuiFontMeta>();
            if (uploaded != null)
            {
                foreach (var item in uploaded)
                {
                    var dict = item as Dictionary<string, object>;
                    if (dict == null) continue;
                    var meta = new FuiFontMeta
                    {
                        FileName = FuiJson.GetString(dict, "fileName", string.Empty),
                        Path = FuiJson.GetString(dict, "path", string.Empty)
                    };
                    uploadedList.Add(meta);
                    yield return meta;
                }
            }

            if (figmaFonts != null)
            {
                foreach (var item in figmaFonts)
                {
                    var dict = item as Dictionary<string, object>;
                    if (dict == null) continue;
                    var family = FuiJson.GetString(dict, "family", string.Empty);
                    var style = FuiJson.GetString(dict, "style", string.Empty);
                    if (string.IsNullOrEmpty(family)) continue;
                    var bestUpload = PickBestUploadedFont(uploadedList, family, style);
                    yield return new FuiFontMeta
                    {
                        Family = family,
                        Style = string.IsNullOrEmpty(style) ? "Regular" : style,
                        FileName = bestUpload != null ? bestUpload.FileName : string.Empty,
                        Path = bestUpload != null ? bestUpload.Path : string.Empty
                    };
                }
            }
        }

        private static FuiFontMeta PickBestUploadedFont(List<FuiFontMeta> uploaded, string family, string style)
        {
            if (uploaded == null || uploaded.Count == 0) return null;
            var familyKey = AiFuiImporterUtility.Slug(family);
            var styleKey = AiFuiImporterUtility.Slug(style);
            FuiFontMeta fallback = uploaded[0];
            foreach (var f in uploaded)
            {
                var nameKey = AiFuiImporterUtility.Slug((f.FileName ?? string.Empty) + " " + (f.Path ?? string.Empty));
                if (!string.IsNullOrEmpty(familyKey) && nameKey.Contains(familyKey))
                {
                    if (string.IsNullOrEmpty(styleKey) || nameKey.Contains(styleKey)) return f;
                    fallback = f;
                }
            }
            return fallback;
        }
    }


    internal sealed class FuiArchiveFile
    {
        public readonly string Path;
        public readonly byte[] Bytes;
        public FuiArchiveFile(string path, byte[] bytes)
        {
            Path = path;
            Bytes = bytes ?? new byte[0];
        }
    }

    internal static class FuiTolerantZipReader
    {
        public static List<FuiArchiveFile> ReadStoredZip(string path)
        {
            var data = File.ReadAllBytes(path);
            var eocd = FindEndOfCentralDirectory(data);
            if (eocd < 0) throw new InvalidDataException("Некорректный FUI zip: не найден central directory.");

            var totalEntries = ReadUInt16(data, eocd + 10);
            var centralOffset = (int)ReadUInt32(data, eocd + 16);
            var result = new List<FuiArchiveFile>();
            var cursor = centralOffset;

            for (var i = 0; i < totalEntries; i++)
            {
                if (cursor + 46 > data.Length || ReadUInt32(data, cursor) != 0x02014b50)
                    throw new InvalidDataException("Некорректный FUI zip: повреждена запись central directory.");

                var method = ReadUInt16(data, cursor + 10);
                var compressedSize = (int)ReadUInt32(data, cursor + 20);
                var nameLength = ReadUInt16(data, cursor + 28);
                var centralExtraLength = ReadUInt16(data, cursor + 30);
                var commentLength = ReadUInt16(data, cursor + 32);
                var localOffset = (int)ReadUInt32(data, cursor + 42);
                var fileName = Encoding.UTF8.GetString(data, cursor + 46, nameLength);

                if (method != 0)
                    throw new InvalidDataException("Tolerant FUI reader поддерживает только stored zip entries. Для сжатых файлов должен использоваться стандартный ZipArchive.");
                if (localOffset + 30 > data.Length || ReadUInt32(data, localOffset) != 0x04034b50)
                    throw new InvalidDataException("Некорректный FUI zip: повреждён local file header для " + fileName);

                var localNameLength = ReadUInt16(data, localOffset + 26);
                var localExtraLength = ReadUInt16(data, localOffset + 28);
                var dataStart = localOffset + 30 + localNameLength + localExtraLength;

                // Multi-Tool Kit writes a browser-side stored zip. Older builds may have an incorrect
                // local extra length equal to file-name length while the central directory says there is no extra field.
                // In that case we trust the central directory and keep reading the package instead of failing import.
                if (centralExtraLength == 0 && localExtraLength == localNameLength)
                    dataStart = localOffset + 30 + localNameLength;

                if (dataStart < 0 || dataStart + compressedSize > data.Length)
                    throw new InvalidDataException("Некорректный FUI zip: данные entry вне диапазона для " + fileName);

                var bytes = new byte[compressedSize];
                Buffer.BlockCopy(data, dataStart, bytes, 0, compressedSize);
                result.Add(new FuiArchiveFile(fileName, bytes));

                cursor += 46 + nameLength + centralExtraLength + commentLength;
            }

            return result;
        }

        private static int FindEndOfCentralDirectory(byte[] data)
        {
            for (var i = data.Length - 22; i >= 0 && i >= data.Length - 66000; i--)
            {
                if (ReadUInt32(data, i) == 0x06054b50) return i;
            }
            return -1;
        }

        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }
    }

    internal sealed class FuiAssetMeta
    {
        public string Id;
        public string Path;
        public bool HasSpriteBorder;
        public float BorderLeft;
        public float BorderRight;
        public float BorderTop;
        public float BorderBottom;
    }

    internal sealed class FuiFontMeta
    {
        public string Family;
        public string Style;
        public string FileName;
        public string Path;
    }

    internal sealed class FuiZipFile
    {
        public readonly string Path;
        public readonly byte[] Bytes;
        public FuiZipFile(string path, byte[] bytes)
        {
            Path = path;
            Bytes = bytes;
        }
    }

    internal sealed class FuiGeneratedScreenAsset
    {
        public string Name;
        public string UxmlPath;
        public string UssPath;
        public double Width;
        public double Height;
    }

    internal sealed class FuiGeneratedScreen
    {
        public string Uxml;
        public string Uss;
    }

    internal sealed class FuiUiToolkitGenerator
    {
        private readonly Dictionary<string, object> _screen;
        private readonly string _ussAssetPath;
        private readonly Dictionary<string, string> _assetPathMap;
        private readonly Dictionary<string, string> _fontPathMap;
        private readonly List<FuiFontMeta> _fontMetadata;
        private readonly string _textGradientFolderPath;
        private readonly FuiImportReport _report;
        private readonly StringBuilder _uss = new StringBuilder();
        private int _elementSeq;
        private readonly Dictionary<string, int> _classNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private string _screenClass;
        private double _screenWidth;
        private double _screenHeight;

        public FuiUiToolkitGenerator(Dictionary<string, object> screen, string ussAssetPath, Dictionary<string, string> assetPathMap, Dictionary<string, string> fontPathMap, List<FuiFontMeta> fontMetadata, string textGradientFolderPath, FuiImportReport report)
        {
            _screen = screen;
            _ussAssetPath = ussAssetPath;
            _assetPathMap = assetPathMap ?? new Dictionary<string, string>();
            _fontPathMap = fontPathMap ?? new Dictionary<string, string>();
            _fontMetadata = fontMetadata ?? new List<FuiFontMeta>();
            _textGradientFolderPath = textGradientFolderPath ?? string.Empty;
            _report = report;
        }

        public FuiGeneratedScreen Generate()
        {
            var screenName = AiFuiImporterUtility.SanitizeIdentifier(FuiJson.GetString(_screen, "name", "Screen"), "Screen");
            _screenClass = "fui_screen_" + screenName;
            var root = FuiJson.GetObject(_screen, "root");
            if (root == null) root = _screen;

            _uss.AppendLine("/* Auto-generated by MTK | Figma UI Import. */");
            _uss.AppendLine("/* Safe to keep after deleting the importer. Uses standard Unity UI Toolkit USS only. */");
            var rootBounds = FuiJson.GetObject(root, "bounds");
            var screenWidth = FuiJson.GetDouble(_screen, "width", 0);
            var screenHeight = FuiJson.GetDouble(_screen, "height", 0);
            if (screenWidth <= 0) screenWidth = FuiJson.GetDouble(rootBounds, "width", 0);
            if (screenHeight <= 0) screenHeight = FuiJson.GetDouble(rootBounds, "height", 0);
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            if (rootBounds != null)
            {
                rootBounds["width"] = screenWidth;
                rootBounds["height"] = screenHeight;
                if (!rootBounds.ContainsKey("x")) rootBounds["x"] = 0;
                if (!rootBounds.ContainsKey("y")) rootBounds["y"] = 0;
            }

            _uss.AppendLine("." + _screenClass + " {");
            _uss.AppendLine("  width: 100%;");
            _uss.AppendLine("  height: 100%;");
            _uss.AppendLine("  min-width: 0;");
            _uss.AppendLine("  min-height: 0;");
            _uss.AppendLine("  flex-grow: 1;");
            _uss.AppendLine("  flex-shrink: 1;");
            _uss.AppendLine("  position: relative;");
            _uss.AppendLine("  overflow: hidden;");
            _uss.AppendLine("}");
            _uss.AppendLine();
            _uss.AppendLine(".fui_element {");
            _uss.AppendLine("  overflow: hidden;");
            _uss.AppendLine("}");
            _uss.AppendLine();
            _uss.AppendLine(".fui_type_label {");
            _uss.AppendLine("  white-space: normal;");
            _uss.AppendLine("  overflow: visible;");
            _uss.AppendLine("  flex-shrink: 0;");
            _uss.AppendLine("}");
            _uss.AppendLine();
            _uss.AppendLine(".fui_button {");
            _uss.AppendLine("  padding-left: 0;");
            _uss.AppendLine("  padding-right: 0;");
            _uss.AppendLine("  padding-top: 0;");
            _uss.AppendLine("  padding-bottom: 0;");
            _uss.AppendLine("  margin-left: 0;");
            _uss.AppendLine("  margin-right: 0;");
            _uss.AppendLine("  margin-top: 0;");
            _uss.AppendLine("  margin-bottom: 0;");
            _uss.AppendLine("  border-left-width: 0;");
            _uss.AppendLine("  border-right-width: 0;");
            _uss.AppendLine("  border-top-width: 0;");
            _uss.AppendLine("  border-bottom-width: 0;");
            _uss.AppendLine("  background-color: rgba(0, 0, 0, 0);");
            _uss.AppendLine("}");
            _uss.AppendLine();

            var rootXml = BuildElementXml(root, 1, true, "root", null, null);
            var uxml = new StringBuilder();
            uxml.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" editor-extension-mode=\"False\">");
            uxml.AppendLine("    <Style src=\"project://database/" + XmlEscape(AiFuiImporterUtility.NormalizeAssetPath(_ussAssetPath)) + "\" />");
            uxml.Append(rootXml);
            uxml.AppendLine("</ui:UXML>");

            return new FuiGeneratedScreen { Uxml = uxml.ToString(), Uss = _uss.ToString() };
        }


        private string BuildSemanticUssClassName(string rawName, string type, int index)
        {
            var token = AiFuiImporterUtility.SanitizeIdentifier(rawName, string.Empty);
            token = (token ?? string.Empty).Trim('_');
            var lower = token.ToLowerInvariant();
            var typeLower = AiFuiImporterUtility.SanitizeIdentifier(type, "element").ToLowerInvariant();
            var semantic = false;
            if (!string.IsNullOrEmpty(lower))
            {
                semantic = lower.StartsWith("button_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("panel_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("progressbar_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("progress_bar_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("popup_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("image_", StringComparison.OrdinalIgnoreCase)
                    || lower.StartsWith("label_", StringComparison.OrdinalIgnoreCase);
            }
            var baseName = semantic ? ("fui_" + lower) : ("fui_element_" + index.ToString("0000", CultureInfo.InvariantCulture));
            if (string.IsNullOrEmpty(baseName) || baseName == "fui_") baseName = "fui_" + typeLower + "_" + index.ToString("0000", CultureInfo.InvariantCulture);
            if (!_classNameCounts.ContainsKey(baseName))
            {
                _classNameCounts[baseName] = 1;
                return baseName;
            }
            _classNameCounts[baseName] += 1;
            return baseName + "_" + _classNameCounts[baseName].ToString("0000", CultureInfo.InvariantCulture);
        }


        private string BuildElementXml(Dictionary<string, object> element, int indent, bool isRoot, string parentLayoutMode, Dictionary<string, object> parentBounds, Dictionary<string, object> parentElement)
        {
            var rawType = NormalizeType(FuiJson.GetString(element, "elementType", "Panel"));
            var childList = FuiJson.GetArray(element, "children");
            var ownText = GetOwnText(element);
            var type = rawType;
            var style = FuiJson.GetObject(element, "style");
            var textAsImage = IsTextRasterized(element);

            // Figma groups that contain several text nodes can be marked as Label by the exporter.
            // UI Toolkit Label cannot hold children, so group-labels become VisualElement containers.
            // True Figma TEXT nodes stay editable ui:Label; gradients/shadows/outlines are mapped to TextCore/USS.
            if (rawType == "Label" && textAsImage) type = "Image";
            else if (rawType == "Label" && childList != null && childList.Count > 0 && string.IsNullOrWhiteSpace(ownText))
                type = "Panel";
            else if (rawType == "ProgressBar" && childList != null && childList.Count > 0)
                type = "Panel"; // custom progress bar from Figma: back/fill images must stay editable in UI Builder

            var tag = MapUxmlTag(type);
            var rawName = FuiJson.GetString(element, "name", type);
            var name = AiFuiImporterUtility.SanitizeIdentifier(rawName, type + "_" + _elementSeq.ToString(CultureInfo.InvariantCulture));
            ++_elementSeq;
            var className = BuildSemanticUssClassName(rawName, type, _elementSeq);
            var classes = new List<string> { "fui_element", className, "fui_type_" + rawType.ToLowerInvariant() };
            if (type != rawType) classes.Add("fui_as_" + type.ToLowerInvariant());
            if (type == "Button") classes.Add("fui_button");
            if (rawType == "ProgressBar") classes.Add("fui_progressbar");
            if (textAsImage) classes.Add("fui_text_image");
            if (IsAtomicLayered(element)) classes.Add("fui_layered");
            if (isRoot) classes.Add(_screenClass);

            AppendStyle(element, className, type, isRoot, parentLayoutMode, parentBounds, parentElement);

            var canHaveChildren = CanHaveChildren(type);
            var pad = new string(' ', indent * 4);
            var attrs = new StringBuilder();
            attrs.Append(" name=\"").Append(XmlEscape(name)).Append("\"");
            attrs.Append(" class=\"").Append(XmlEscape(string.Join(" ", classes.ToArray()))).Append("\"");
            if (isRoot)
            {
                attrs.Append(" style=\"")
                    .Append("width: 100%; height: 100%; min-width: 0; min-height: 0; ")
                    .Append("flex-grow: 1; flex-shrink: 1; position: relative; overflow: hidden;\"");
            }
            else
            {
                var inlineFallback = BuildCriticalInlineStyle(element, type);
                if (!string.IsNullOrEmpty(inlineFallback)) attrs.Append(" style=\"").Append(XmlEscape(inlineFallback)).Append("\"");
            }

            var text = CleanImportedText(!string.IsNullOrWhiteSpace(ownText) ? ownText : FindVisibleText(element));
            var hasTextGradient = FuiJson.GetObject(style, "textGradient") != null;
            var editableText = BuildEditableLabelText(text, style, name);
            if (type == "Label" && !textAsImage)
            {
                attrs.Append(" text=\"").Append(XmlEscape(editableText)).Append("\"");
                if (hasTextGradient) attrs.Append(" enable-rich-text=\"true\"");
            }
            else if (type == "Button" && (childList == null || childList.Count == 0) && !string.IsNullOrEmpty(text))
            {
                attrs.Append(" text=\"").Append(XmlEscape(editableText)).Append("\"");
                if (hasTextGradient) attrs.Append(" enable-rich-text=\"true\"");
            }
            else if (type == "Input" || type == "Входные данные")
            {
                attrs.Append(" label=\"").Append(XmlEscape(text)).Append("\"");
                attrs.Append(" value=\"\"");
            }
            else if (type == "ProgressBar")
            {
                attrs.Append(" value=\"0\"");
                if (!string.IsNullOrEmpty(text)) attrs.Append(" title=\"").Append(XmlEscape(text)).Append("\"");
            }
            else if (type == "Slider")
            {
                attrs.Append(" low-value=\"0\" high-value=\"1\" value=\"0\"");
            }

            if (!canHaveChildren || childList == null || childList.Count == 0)
            {
                return pad + "<" + tag + attrs + " />\n";
            }

            var layout = FuiJson.GetObject(element, "layout");
            var ownLayoutMode = FuiJson.GetString(layout, "mode", "flex");
            var sb = new StringBuilder();
            sb.Append(pad).Append("<").Append(tag).Append(attrs).AppendLine(">");
            foreach (var child in OrderChildrenForUxml(element, childList, ownLayoutMode))
            {
                var childDict = child as Dictionary<string, object>;
                if (childDict != null) sb.Append(BuildElementXml(childDict, indent + 1, false, ownLayoutMode, FuiJson.GetObject(element, "bounds"), element));
            }
            sb.Append(pad).Append("</").Append(tag).AppendLine(">");
            return sb.ToString();
        }


        private string BuildCriticalInlineStyle(Dictionary<string, object> element, string type)
        {
            if (element == null) return string.Empty;
            var parts = new List<string>();
            var assetRef = FuiJson.GetObject(element, "assetRef");
            var assetPath = ResolveAssetPath(assetRef);
            if (!string.IsNullOrEmpty(assetPath))
            {
                parts.Add("background-image: url(\"project://database/" + CssUrl(AiFuiImporterUtility.NormalizeAssetPath(assetPath)) + "\")");
                parts.Add("-unity-background-scale-mode: stretch-to-fill");
            }

            var style = FuiJson.GetObject(element, "style");
            if (style != null && string.Equals(type, "Label", StringComparison.OrdinalIgnoreCase))
            {
                var family = FuiJson.GetString(style, "fontFamily", string.Empty);
                var fontStyle = FuiJson.GetString(style, "fontStyle", string.Empty);
                var fontAssetPath = ResolveFontAssetPath(family, fontStyle);
                if (!string.IsNullOrEmpty(fontAssetPath))
                {
                    var normalizedFontAssetPath = AiFuiImporterUtility.NormalizeAssetPath(fontAssetPath);
                    if (normalizedFontAssetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        parts.Add("-unity-font-definition: url(\"project://database/" + CssUrl(normalizedFontAssetPath) + "\")");
                    else
                        parts.Add("-unity-font: url(\"project://database/" + CssUrl(normalizedFontAssetPath) + "\")");
                }

                if (FuiJson.GetObject(style, "textGradient") != null) parts.Add("color: rgb(255, 255, 255)");
                else
                {
                    var color = FuiJson.GetObject(style, "color");
                    if (color != null) parts.Add("color: " + ColorToCss(color));
                }
            }
            return string.Join("; ", parts) + (parts.Count > 0 ? ";" : string.Empty);
        }

        private void AppendStyle(Dictionary<string, object> element, string className, string type, bool isRoot, string parentLayoutMode, Dictionary<string, object> parentBounds, Dictionary<string, object> parentElement)
        {
            var bounds = FuiJson.GetObject(element, "bounds");
            var layout = FuiJson.GetObject(element, "layout");
            var anchor = FuiJson.GetObject(element, "anchor");
            var style = FuiJson.GetObject(element, "style");
            var assetRef = FuiJson.GetObject(element, "assetRef");
            var width = FuiJson.GetDouble(bounds, "width", 0);
            var height = FuiJson.GetDouble(bounds, "height", 0);
            var useAbsolute = ShouldUseAbsoluteLayout(element, isRoot, parentLayoutMode);

            if (isRoot)
                _uss.AppendLine("." + className + " {");
            else
                _uss.AppendLine("." + _screenClass + " ." + className + " {");

            if (isRoot)
            {
                _uss.AppendLine("  width: 100%;");
                _uss.AppendLine("  height: 100%;");
                _uss.AppendLine("  min-width: 0;");
                _uss.AppendLine("  min-height: 0;");
                _uss.AppendLine("  flex-grow: 1;");
                _uss.AppendLine("  flex-shrink: 1;");
                _uss.AppendLine("  position: relative;");
                _uss.AppendLine("  overflow: hidden;");
            }
            else if (useAbsolute)
            {
                _uss.AppendLine("  position: absolute;");
                AppendAbsoluteAnchor(anchor, bounds, parentBounds, width, height);
            }
            else
            {
                AppendRelativeAdaptiveLayout(element, parentElement, parentLayoutMode, width, height, type);
            }

            AppendLayout(layout);
            AppendVisualStyle(style);
            if (type == "Label")
            {
                _uss.AppendLine("  overflow: visible;");
                _uss.AppendLine("  white-space: normal;");
                _uss.AppendLine("  -unity-text-generator: advanced;");
                _uss.AppendLine("  -unity-text-overflow-position: end;");
            }
            if (type != "Image" || !IsTextRasterized(element)) AppendTextStyle(style);
            AppendAssetBackground(assetRef);

            if (IsTextRasterized(element))
            {
                _uss.AppendLine("  overflow: visible;");
                _uss.AppendLine("  background-repeat: no-repeat;");
            }

            if (type == "Button")
            {
                _uss.AppendLine("  -unity-background-scale-mode: stretch-to-fill;");
            }

            _uss.AppendLine("}");
            _uss.AppendLine();
        }


        private bool ShouldUseAbsoluteLayout(Dictionary<string, object> element, bool isRoot, string parentLayoutMode)
        {
            if (isRoot || element == null) return false;
            if (string.Equals(parentLayoutMode, "absolute", StringComparison.OrdinalIgnoreCase)) return true;
            if (IsAtomicLayered(element)) return true;

            var layout = FuiJson.GetObject(element, "layout");
            var ownMode = FuiJson.GetString(layout, "mode", string.Empty);
            if (ownMode.Equals("absolute", StringComparison.OrdinalIgnoreCase)) return true;

            var anchor = FuiJson.GetObject(element, "anchor");
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            if (scenario == "A_FULL_STRETCH_BACKGROUND" || scenario == "B_BOTTOM_RIGHT") return true;
            if (scenario.StartsWith("MODAL_", StringComparison.OrdinalIgnoreCase)) return true;

            // A regular child can have an ABSOLUTE_FALLBACK anchor because its distance to
            // the parent edges is not meaningful. If the parent was inferred as Row/Column,
            // keep the child in flex flow and preserve the local gap through margins.
            return false;
        }

        private void AppendRelativeAdaptiveLayout(Dictionary<string, object> element, Dictionary<string, object> parentElement, string parentLayoutMode, double width, double height, string type)
        {
            var parentLayout = parentElement != null ? FuiJson.GetObject(parentElement, "layout") : null;
            var direction = CssKeyword(FuiJson.GetString(parentLayout, "flexDirection", "column"), "column");
            var anchor = FuiJson.GetObject(element, "anchor");
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            var sizing = FuiJson.GetObject(element, "sizing") ?? FuiJson.GetObject(FuiJson.GetObject(element, "adaptive"), "sizing");
            var horizontal = FuiJson.GetString(sizing, "horizontal", string.Empty);
            var vertical = FuiJson.GetString(sizing, "vertical", string.Empty);

            if (string.IsNullOrEmpty(horizontal))
            {
                if (FuiJson.GetBool(anchor, "stretchX", false) || FuiJson.GetBool(anchor, "stretchHorizontal", false) || scenario == "C_FULL_WIDTH_FIXED_HEIGHT") horizontal = "Stretch";
                else if (type == "Label" && !HasAssetRef(element)) horizontal = "Hug";
                else horizontal = "Fixed";
            }
            if (string.IsNullOrEmpty(vertical))
            {
                if (FuiJson.GetBool(anchor, "stretchY", false) || FuiJson.GetBool(anchor, "stretchVertical", false)) vertical = "Stretch";
                else if (type == "Label") vertical = "Hug";
                else vertical = "Fixed";
            }

            horizontal = NormalizeSizingMode(horizontal);
            vertical = NormalizeSizingMode(vertical);

            _uss.AppendLine("  position: relative;");
            AppendFlowMargins(element, parentElement, direction);

            var parentIsRow = direction == "row";
            var mainSizing = parentIsRow ? horizontal : vertical;
            var crossSizing = parentIsRow ? vertical : horizontal;
            var mainProperty = parentIsRow ? "width" : "height";
            var crossProperty = parentIsRow ? "height" : "width";
            var mainValue = parentIsRow ? width : height;
            var crossValue = parentIsRow ? height : width;

            if (mainSizing == "Fill" || mainSizing == "Stretch")
            {
                _uss.AppendLine("  flex-grow: 1;");
                _uss.AppendLine("  flex-shrink: 1;");
                _uss.AppendLine("  flex-basis: 0;");
                _uss.AppendLine("  min-" + mainProperty + ": 0;");
            }
            else if (mainSizing == "Hug")
            {
                _uss.AppendLine("  flex-grow: 0;");
                _uss.AppendLine("  flex-shrink: 0;");
                if (type == "Image" || HasAssetRef(element)) AppendPx(mainProperty, mainValue);
            }
            else
            {
                AppendPx(mainProperty, mainValue);
                _uss.AppendLine("  flex-grow: 0;");
                _uss.AppendLine("  flex-shrink: 0;");
            }

            if (crossSizing == "Stretch" || crossSizing == "Fill")
            {
                _uss.AppendLine("  align-self: stretch;");
                _uss.AppendLine("  min-" + crossProperty + ": 0;");
            }
            else if (crossSizing == "Hug")
            {
                if (type == "Image" || HasAssetRef(element)) AppendPx(crossProperty, crossValue);
            }
            else
            {
                AppendPx(crossProperty, crossValue);
            }

            if (scenario == "D_CENTER_CONTENT") _uss.AppendLine("  align-self: center;");
        }

        private static string NormalizeSizingMode(string mode)
        {
            var s = (mode ?? string.Empty).Trim().ToLowerInvariant();
            if (s == "fill" || s == "grow" || s == "layoutgrow") return "Fill";
            if (s == "hug" || s == "content" || s == "auto") return "Hug";
            if (s == "stretch" || s == "stretched") return "Stretch";
            return "Fixed";
        }

        private static bool HasAssetRef(Dictionary<string, object> element)
        {
            return FuiJson.GetObject(element, "assetRef") != null;
        }

        private void AppendFlowMargins(Dictionary<string, object> element, Dictionary<string, object> parentElement, string direction)
        {
            var margin = FuiJson.GetObject(element, "margin") ?? FuiJson.GetObject(FuiJson.GetObject(element, "adaptive"), "margin");
            if (margin != null)
            {
                AppendOptionalMargin("margin-left", FuiJson.GetNullableDouble(margin, "left"));
                AppendOptionalMargin("margin-right", FuiJson.GetNullableDouble(margin, "right"));
                AppendOptionalMargin("margin-top", FuiJson.GetNullableDouble(margin, "top"));
                AppendOptionalMargin("margin-bottom", FuiJson.GetNullableDouble(margin, "bottom"));
                return;
            }

            if (parentElement == null) return;
            var parentLayout = FuiJson.GetObject(parentElement, "layout");
            var parentMode = FuiJson.GetString(parentLayout, "mode", "flex");
            if (!parentMode.Equals("flex", StringComparison.OrdinalIgnoreCase)) return;

            var children = FuiJson.GetArray(parentElement, "children");
            if (children == null || children.Count <= 1) return;
            var parentBounds = FuiJson.GetObject(parentElement, "bounds");
            var ordered = OrderChildrenForUxml(parentElement, children, parentMode)
                .Select(item => item as Dictionary<string, object>)
                .Where(child => child != null && !ShouldUseAbsoluteLayout(child, false, parentMode))
                .ToList();
            var index = ordered.IndexOf(element);
            if (index <= 0) return;

            var currentBounds = FuiJson.GetObject(element, "bounds");
            var previousBounds = FuiJson.GetObject(ordered[index - 1], "bounds");
            if (currentBounds == null || previousBounds == null) return;

            if (direction == "row")
            {
                var gap = FuiJson.GetDouble(currentBounds, "x", 0) - (FuiJson.GetDouble(previousBounds, "x", 0) + FuiJson.GetDouble(previousBounds, "width", 0));
                if (gap > 0.5) AppendPx("margin-left", Math.Round(gap));
            }
            else
            {
                var gap = FuiJson.GetDouble(currentBounds, "y", 0) - (FuiJson.GetDouble(previousBounds, "y", 0) + FuiJson.GetDouble(previousBounds, "height", 0));
                if (gap > 0.5) AppendPx("margin-top", Math.Round(gap));
            }
        }

        private void AppendOptionalMargin(string property, double? value)
        {
            if (value.HasValue && Math.Abs(value.Value) > 0.001) AppendPx(property, value.Value);
        }

        private void AppendAbsoluteAnchor(Dictionary<string, object> anchor, Dictionary<string, object> bounds, Dictionary<string, object> parentBounds, double width, double height)
        {
            bounds = bounds ?? new Dictionary<string, object>();
            parentBounds = parentBounds ?? new Dictionary<string, object>();
            var localX = FuiJson.GetDouble(bounds, "x", 0) - FuiJson.GetDouble(parentBounds, "x", 0);
            var localY = FuiJson.GetDouble(bounds, "y", 0) - FuiJson.GetDouble(parentBounds, "y", 0);
            var parentWidth = FuiJson.GetDouble(parentBounds, "width", 0);
            var parentHeight = FuiJson.GetDouble(parentBounds, "height", 0);
            var computedRight = parentWidth > 0 ? Math.Max(0, parentWidth - localX - width) : 0;
            var computedBottom = parentHeight > 0 ? Math.Max(0, parentHeight - localY - height) : 0;

            if (ShouldClampToParentBackground(anchor, bounds, parentBounds))
            {
                _uss.AppendLine("  left: 0;");
                _uss.AppendLine("  right: 0;");
                _uss.AppendLine("  top: 0;");
                _uss.AppendLine("  bottom: 0;");
                _uss.AppendLine("  overflow: hidden;");
                return;
            }

            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            if (scenario == "MODAL_FULL_SCREEN_OVERLAY" || scenario == "A_FULL_STRETCH_BACKGROUND")
            {
                _uss.AppendLine("  left: 0;");
                _uss.AppendLine("  right: 0;");
                _uss.AppendLine("  top: 0;");
                _uss.AppendLine("  bottom: 0;");
                _uss.AppendLine("  overflow: visible;");
                return;
            }

            if (scenario == "MODAL_CENTER_OVERLAY")
            {
                AppendCenteredAbsoluteAxis("horizontal", localX, width, parentWidth, true);
                AppendCenteredAbsoluteAxis("vertical", localY, height, parentHeight, true);
                _uss.AppendLine("  overflow: visible;");
                return;
            }

            var left = FuiJson.GetNullableDouble(anchor, "left");
            var right = FuiJson.GetNullableDouble(anchor, "right");
            var top = FuiJson.GetNullableDouble(anchor, "top");
            var bottom = FuiJson.GetNullableDouble(anchor, "bottom");
            var centerX = FuiJson.GetBool(anchor, "centerX", false) || FuiJson.GetBool(anchor, "center", false) || FuiJson.GetString(anchor, "horizontal", string.Empty).Equals("center", StringComparison.OrdinalIgnoreCase);
            var centerY = FuiJson.GetBool(anchor, "centerY", false) || FuiJson.GetBool(anchor, "center", false) || FuiJson.GetString(anchor, "vertical", string.Empty).Equals("center", StringComparison.OrdinalIgnoreCase);
            var stretchX = FuiJson.GetBool(anchor, "stretchX", false) || FuiJson.GetBool(anchor, "stretchHorizontal", false) || FuiJson.GetString(anchor, "horizontal", string.Empty).Equals("stretch", StringComparison.OrdinalIgnoreCase);
            var stretchY = FuiJson.GetBool(anchor, "stretchY", false) || FuiJson.GetBool(anchor, "stretchVertical", false) || FuiJson.GetString(anchor, "vertical", string.Empty).Equals("stretch", StringComparison.OrdinalIgnoreCase);

            if (scenario == "B_BOTTOM_RIGHT")
            {
                AppendPx("right", right.HasValue ? Math.Max(0, right.Value) : computedRight);
                AppendPx("bottom", bottom.HasValue ? Math.Max(0, bottom.Value) : computedBottom);
                AppendPx("width", width);
                AppendPx("height", height);
                return;
            }

            if (stretchX || (left.HasValue && right.HasValue))
            {
                AppendPx("left", left.HasValue ? Math.Max(0, left.Value) : Math.Max(0, localX));
                AppendPx("right", right.HasValue ? Math.Max(0, right.Value) : computedRight);
            }
            else if (centerX && parentWidth > 0)
            {
                AppendCenteredAbsoluteAxis("horizontal", localX, width, parentWidth, false);
            }
            else if (right.HasValue && !left.HasValue)
            {
                AppendPx("right", Math.Max(0, right.Value));
                AppendPx("width", width);
            }
            else
            {
                AppendPx("left", left.HasValue ? left.Value : localX);
                AppendPx("width", width);
            }

            if (stretchY || (top.HasValue && bottom.HasValue))
            {
                AppendPx("top", top.HasValue ? Math.Max(0, top.Value) : Math.Max(0, localY));
                AppendPx("bottom", bottom.HasValue ? Math.Max(0, bottom.Value) : computedBottom);
            }
            else if (centerY && parentHeight > 0)
            {
                AppendCenteredAbsoluteAxis("vertical", localY, height, parentHeight, false);
            }
            else if (bottom.HasValue && !top.HasValue)
            {
                AppendPx("bottom", Math.Max(0, bottom.Value));
                AppendPx("height", height);
            }
            else
            {
                AppendPx("top", top.HasValue ? top.Value : localY);
                AppendPx("height", height);
            }
        }

        private void AppendCenteredAbsoluteAxis(string axis, double localStart, double size, double parentSize, bool preferExactLocalFallback)
        {
            if (parentSize <= 0 || preferExactLocalFallback)
            {
                if (axis == "horizontal")
                {
                    AppendPx("left", localStart);
                    AppendPx("width", size);
                }
                else
                {
                    AppendPx("top", localStart);
                    AppendPx("height", size);
                }
                return;
            }

            if (axis == "horizontal")
            {
                _uss.AppendLine("  left: 50%;");
                AppendPx("margin-left", -size / 2.0);
                AppendPx("width", size);
            }
            else
            {
                _uss.AppendLine("  top: 50%;");
                AppendPx("margin-top", -size / 2.0);
                AppendPx("height", size);
            }
        }

        private static bool ShouldClampToParentBackground(Dictionary<string, object> anchor, Dictionary<string, object> bounds, Dictionary<string, object> parentBounds)
        {
            if (bounds == null || parentBounds == null) return false;
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            if (scenario == "A_FULL_STRETCH_BACKGROUND") return true;
            var pw = FuiJson.GetDouble(parentBounds, "width", 0);
            var ph = FuiJson.GetDouble(parentBounds, "height", 0);
            var w = FuiJson.GetDouble(bounds, "width", 0);
            var h = FuiJson.GetDouble(bounds, "height", 0);
            if (pw <= 0 || ph <= 0 || w <= 0 || h <= 0) return false;
            return w >= pw - 1 && h >= ph - 1;
        }

        private void AppendLayout(Dictionary<string, object> layout)
        {
            if (layout == null) return;
            var mode = FuiJson.GetString(layout, "mode", "flex");
            if (mode == "absolute")
            {
                _uss.AppendLine("  flex-direction: column;");
                return;
            }

            var direction = FuiJson.GetString(layout, "flexDirection", "column");
            var justify = FuiJson.GetString(layout, "justifyContent", "flex-start");
            var align = FuiJson.GetString(layout, "alignItems", "flex-start");
            _uss.AppendLine("  flex-direction: " + CssKeyword(direction, "column") + ";");
            _uss.AppendLine("  justify-content: " + CssKeyword(justify, "flex-start") + ";");
            _uss.AppendLine("  align-items: " + CssKeyword(align, "flex-start") + ";");
            var wrap = FuiJson.GetString(layout, "wrap", FuiJson.GetString(layout, "layoutWrap", string.Empty));
            if (!string.IsNullOrEmpty(wrap) && !wrap.Equals("NO_WRAP", StringComparison.OrdinalIgnoreCase)) _uss.AppendLine("  flex-wrap: wrap;");

            // UI Toolkit USS has no reliable row-gap/column-gap in all target Unity versions.
            // The generator therefore emits per-child margins based on measured sibling distances.
            var padding = FuiJson.GetObject(layout, "padding");
            if (padding != null)
            {
                AppendPx("padding-left", FuiJson.GetDouble(padding, "left", 0));
                AppendPx("padding-right", FuiJson.GetDouble(padding, "right", 0));
                AppendPx("padding-top", FuiJson.GetDouble(padding, "top", 0));
                AppendPx("padding-bottom", FuiJson.GetDouble(padding, "bottom", 0));
            }
        }

        private void AppendVisualStyle(Dictionary<string, object> style)
        {
            if (style == null) return;
            var bg = FuiJson.GetObject(style, "backgroundColor");
            if (bg != null) _uss.AppendLine("  background-color: " + ColorToCss(bg) + ";");

            var opacity = FuiJson.GetNullableDouble(style, "opacity");
            if (opacity.HasValue && opacity.Value < 0.999) _uss.AppendLine("  opacity: " + Num(opacity.Value) + ";");

            var radius = FuiJson.GetNullableDouble(style, "borderRadius");
            if (radius.HasValue && radius.Value > 0) AppendPx("border-radius", radius.Value);

            var borderColor = FuiJson.GetObject(style, "borderColor");
            var borderWidth = FuiJson.GetNullableDouble(style, "borderWidth");
            if (borderColor != null) _uss.AppendLine("  border-color: " + ColorToCss(borderColor) + ";");
            if (borderWidth.HasValue && borderWidth.Value > 0)
            {
                AppendPx("border-left-width", borderWidth.Value);
                AppendPx("border-right-width", borderWidth.Value);
                AppendPx("border-top-width", borderWidth.Value);
                AppendPx("border-bottom-width", borderWidth.Value);
            }
        }

        private void AppendTextStyle(Dictionary<string, object> style)
        {
            if (style == null) return;
            var color = FuiJson.GetObject(style, "color");
            if (FuiJson.GetObject(style, "textGradient") != null)
            {
                _uss.AppendLine("  color: rgb(255, 255, 255);");
            }
            else if (color != null) _uss.AppendLine("  color: " + ColorToCss(color) + ";");

            var fontSize = FuiJson.GetNullableDouble(style, "fontSize");
            if (fontSize.HasValue && fontSize.Value > 0) AppendPx("font-size", fontSize.Value);

            var lineHeight = FuiJson.GetObject(style, "lineHeight");
            var lineHeightPx = ResolveLineHeightPx(lineHeight, fontSize.HasValue ? fontSize.Value : 0);
            if (lineHeightPx > 0) AppendPx("line-height", lineHeightPx);

            var letterSpacing = FuiJson.GetObject(style, "letterSpacing");
            var letterSpacingPx = ResolveLetterSpacingPx(letterSpacing, fontSize.HasValue ? fontSize.Value : 0);
            if (Math.Abs(letterSpacingPx) > 0.001) AppendPx("letter-spacing", letterSpacingPx);

            var textAlign = FuiJson.GetString(style, "textAlign", string.Empty);
            var verticalAlign = FuiJson.GetString(style, "verticalAlign", string.Empty);
            var unityTextAlign = ToUnityTextAlign(textAlign, verticalAlign);
            if (!string.IsNullOrEmpty(unityTextAlign)) _uss.AppendLine("  -unity-text-align: " + unityTextAlign + ";");

            var outlineColor = FuiJson.GetObject(style, "textStrokeColor");
            var outlineWidth = FuiJson.GetNullableDouble(style, "textStrokeWidth");
            if (outlineColor != null && outlineWidth.HasValue && outlineWidth.Value > 0)
            {
                _uss.AppendLine("  -unity-text-outline-color: " + ColorToCss(outlineColor) + ";");
                AppendPx("-unity-text-outline-width", outlineWidth.Value);
            }

            AppendTextEffects(style);

            var fontStyle = FuiJson.GetString(style, "fontStyle", string.Empty);
            var mapped = ToUnityFontStyle(fontStyle);
            if (!string.IsNullOrEmpty(mapped)) _uss.AppendLine("  -unity-font-style: " + mapped + ";");

            var family = FuiJson.GetString(style, "fontFamily", string.Empty);
            if (!string.IsNullOrEmpty(family))
            {
                var fontAssetPath = ResolveFontAssetPath(family, fontStyle);
                if (!string.IsNullOrEmpty(fontAssetPath))
                {
                    var normalizedFontAssetPath = AiFuiImporterUtility.NormalizeAssetPath(fontAssetPath);
                    if (normalizedFontAssetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        _uss.AppendLine("  -unity-font-definition: url(\"project://database/" + CssUrl(normalizedFontAssetPath) + "\");");
                    else
                        _uss.AppendLine("  -unity-font: url(\"project://database/" + CssUrl(normalizedFontAssetPath) + "\");");
                }
                else
                {
                    _uss.AppendLine("  /* Figma font family: " + CssComment(family) + ". Font file was not found in this FUI package. */");
                }
            }
        }


        private static string CleanImportedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var cleaned = text;

            // Figma/TextCore internal style ids can occasionally be exported as visible text
            // (for example "<style F0000...>"). They are not part of the player's text and can
            // break UI Toolkit rich text parsing, so strip only known rich-text-like tags.
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"</?(style|gradient|color|font|font-weight|size|alpha|mark|align|line-height|line-indent|indent|margin|margin-left|margin-right|voffset|cspace|mspace|space|pos|width|link|a|b|i|u|s|sup|sub|smallcaps|uppercase|lowercase|allcaps|nobr|noparse|sprite|br)(\s+[^<>]*)?>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            // Broken/unclosed style ids from exporters often look like "<style F0000..." with no closing >.
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"<\s*style\s+F[0-9A-Fa-f_\-]+[^\r\n<>]*",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            return cleaned.Trim();
        }

        private string BuildEditableLabelText(string text, Dictionary<string, object> style, string elementName)
        {
            var raw = CleanImportedText(text ?? string.Empty);
            var gradient = FuiJson.GetObject(style, "textGradient");
            if (gradient == null) return raw;
            var presetName = EnsureTextGradientPreset(gradient, elementName);
            if (string.IsNullOrEmpty(presetName)) return raw;

            // Unity 6.5 UI Toolkit/TextCore applies gradients only through rich text tags.
            // The returned value is still XML-escaped by the UXML writer, so the Label/Button stays editable.
            // <color=#FFFFFFFF> prevents the element vertex color from multiplying the preset to transparent.
            return "<color=#FFFFFFFF><gradient=\"" + presetName + "\">" + raw + "</gradient></color>";
        }

        private string EnsureTextGradientPreset(Dictionary<string, object> gradient, string elementName)
        {
            try
            {
                if (gradient == null || string.IsNullOrEmpty(_textGradientFolderPath)) return string.Empty;
                AiFuiImporterUtility.EnsureAssetFolder(_textGradientFolderPath);

                // The preset name is part of the rich text tag. Do not reuse a plain element name only:
                // different screens often have labels named Title/Button, and a later import can overwrite
                // the first gradient asset, making older texts lose or change their gradient.
                var safeName = AiFuiImporterUtility.SanitizeFileName(
                    "fui_gradient_" + elementName + "_" + ShortStableHash(FuiJson.Serialize(gradient)),
                    "fui_gradient");
                var assetPath = AiFuiImporterUtility.CombineAssetPath(_textGradientFolderPath, safeName + ".asset");
                var gradientType = Type.GetType("UnityEngine.TextCore.Text.TextColorGradient, UnityEngine.TextCoreTextEngineModule")
                    ?? Type.GetType("UnityEngine.TextCore.Text.TextColorGradient, UnityEngine.TextCoreFontEngineModule")
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.TextCore.Text.TextColorGradient")).FirstOrDefault(t => t != null);
                if (gradientType == null) return string.Empty;
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                var obj = existing != null ? existing : ScriptableObject.CreateInstance(gradientType);
                ApplyTextGradientColors(obj, gradient);
                if (existing == null) AssetDatabase.CreateAsset(obj, assetPath);
                else EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                return safeName;
            }
            catch (Exception ex)
            {
                AddWarning("Не удалось создать TextCore gradient preset: " + ex.Message);
                return string.Empty;
            }
        }

        private static void ApplyTextGradientColors(UnityEngine.Object obj, Dictionary<string, object> gradient)
        {
            if (obj == null || gradient == null) return;
            var stops = FuiJson.GetArray(gradient, "stops");

            var c0 = GetGradientStopColor(stops, 0, null);
            var c1 = GetGradientStopColor(stops, 1, GetGradientStopColor(stops, stops != null ? stops.Count - 1 : 0, c0));
            var c2 = GetGradientStopColor(stops, 2, c1);
            var c3 = GetGradientStopColor(stops, 3, c2);

            var first = ForceOpaque(ToUnityColor(c0, Color.white));
            var second = ForceOpaque(ToUnityColor(c1, first));
            var third = ForceOpaque(ToUnityColor(c2, second));
            var fourth = ForceOpaque(ToUnityColor(c3, third));

            var type = FuiJson.GetString(gradient, "type", "linear").ToLowerInvariant();
            var angle = FuiJson.GetDouble(gradient, "angle", double.NaN);
            var horizontal = type.Contains("horizontal") || (!double.IsNaN(angle) && Math.Abs(Math.Sin(angle * Math.PI / 180.0)) < 0.35);
            var fourCorners = type.Contains("radial") || type.Contains("diamond") || type.Contains("corner") || (stops != null && stops.Count >= 3);
            var mode = fourCorners ? 3 : horizontal ? 1 : 2;

            var topLeft = first;
            var topRight = fourCorners ? second : horizontal ? second : first;
            var bottomLeft = fourCorners ? third : horizontal ? first : second;
            var bottomRight = fourCorners ? fourth : second;

            var so = new SerializedObject(obj);
            TrySetSerializedEnum(so, "m_ColorMode", mode);
            TrySetSerializedEnum(so, "colorMode", mode);
            TrySetSerializedColor(so, "m_TopLeft", topLeft);
            TrySetSerializedColor(so, "m_TopRight", topRight);
            TrySetSerializedColor(so, "m_BottomLeft", bottomLeft);
            TrySetSerializedColor(so, "m_BottomRight", bottomRight);
            TrySetSerializedColor(so, "topLeft", topLeft);
            TrySetSerializedColor(so, "topRight", topRight);
            TrySetSerializedColor(so, "bottomLeft", bottomLeft);
            TrySetSerializedColor(so, "bottomRight", bottomRight);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Some Unity 6.5 builds expose TextColorGradient members as public fields/properties rather than
            // the serialized names above. Set both paths, but keep the old reflection-free flow intact.
            TrySetMemberEnum(obj, "colorMode", mode);
            TrySetMemberEnum(obj, "m_ColorMode", mode);
            TrySetMemberColor(obj, "topLeft", topLeft);
            TrySetMemberColor(obj, "topRight", topRight);
            TrySetMemberColor(obj, "bottomLeft", bottomLeft);
            TrySetMemberColor(obj, "bottomRight", bottomRight);
            TrySetMemberColor(obj, "m_TopLeft", topLeft);
            TrySetMemberColor(obj, "m_TopRight", topRight);
            TrySetMemberColor(obj, "m_BottomLeft", bottomLeft);
            TrySetMemberColor(obj, "m_BottomRight", bottomRight);
            EditorUtility.SetDirty(obj);
        }

        private static Dictionary<string, object> GetGradientStopColor(List<object> stops, int index, Dictionary<string, object> fallback)
        {
            if (stops == null || index < 0 || index >= stops.Count) return fallback;
            var stop = stops[index] as Dictionary<string, object>;
            if (stop == null) return fallback;

            var nested = FuiJson.GetObject(stop, "color")
                ?? FuiJson.GetObject(stop, "rgba")
                ?? FuiJson.GetObject(stop, "fill")
                ?? FuiJson.GetObject(stop, "value");
            if (nested != null) return nested;
            if (stop.ContainsKey("r") && stop.ContainsKey("g") && stop.ContainsKey("b")) return stop;
            return fallback;
        }

        private static Color ForceOpaque(Color color)
        {
            // Text gradients exported from design tools sometimes carry zero alpha in the preset colors.
            // In UI Toolkit that makes the whole glyph transparent even though the Figma text is visible.
            color.a = 1f;
            return color;
        }

        private static void TrySetMemberColor(UnityEngine.Object obj, string memberName, Color value)
        {
            TrySetMemberValue(obj, memberName, value);
        }

        private static void TrySetMemberEnum(UnityEngine.Object obj, string memberName, int value)
        {
            if (obj == null || string.IsNullOrEmpty(memberName)) return;
            var type = obj.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                try
                {
                    if (field.FieldType.IsEnum) field.SetValue(obj, Enum.ToObject(field.FieldType, value));
                    else if (field.FieldType == typeof(int)) field.SetValue(obj, value);
                }
                catch { }
                return;
            }

            var property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                try
                {
                    if (property.PropertyType.IsEnum) property.SetValue(obj, Enum.ToObject(property.PropertyType, value), null);
                    else if (property.PropertyType == typeof(int)) property.SetValue(obj, value, null);
                }
                catch { }
            }
        }

        private static void TrySetMemberValue(UnityEngine.Object obj, string memberName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(memberName)) return;
            var type = obj.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var field = type.GetField(memberName, flags);
            if (field != null && field.FieldType.IsInstanceOfType(value))
            {
                try { field.SetValue(obj, value); } catch { }
                return;
            }
            var property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite && property.PropertyType.IsInstanceOfType(value))
            {
                try { property.SetValue(obj, value, null); } catch { }
            }
        }

        private static string ShortStableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                var text = value ?? string.Empty;
                for (var i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }
                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        private static void TrySetSerializedEnum(SerializedObject so, string propertyName, int value)
        {
            var p = so.FindProperty(propertyName);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = Mathf.Clamp(value, 0, Math.Max(0, p.enumDisplayNames.Length - 1));
            else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
        }

        private static void TrySetSerializedColor(SerializedObject so, string propertyName, Color color)
        {
            var p = so.FindProperty(propertyName);
            if (p != null && p.propertyType == SerializedPropertyType.Color) p.colorValue = color;
        }

        private static Color ToUnityColor(Dictionary<string, object> c, Color fallback)
        {
            if (c == null) return fallback;
            return new Color(
                (float)(FuiJson.GetDouble(c, "r", fallback.r * 255.0) / 255.0),
                (float)(FuiJson.GetDouble(c, "g", fallback.g * 255.0) / 255.0),
                (float)(FuiJson.GetDouble(c, "b", fallback.b * 255.0) / 255.0),
                (float)FuiJson.GetDouble(c, "a", fallback.a)
            );
        }

        private void AppendTextEffects(Dictionary<string, object> style)
        {
            var effects = FuiJson.GetArray(style, "effects");
            if (effects == null || effects.Count == 0) return;
            foreach (var item in effects)
            {
                var effect = item as Dictionary<string, object>;
                if (effect == null) continue;
                var type = FuiJson.GetString(effect, "type", string.Empty);
                if (!type.Contains("shadow")) continue;
                var color = FuiJson.GetObject(effect, "color");
                var x = FuiJson.GetDouble(effect, "offsetX", 0);
                var y = FuiJson.GetDouble(effect, "offsetY", 0);
                var blur = FuiJson.GetDouble(effect, "blur", 0);
                _uss.AppendLine("  text-shadow: " + Num(x) + "px " + Num(y) + "px " + Num(blur) + "px " + ColorToCss(color) + ";");
                break;
            }
        }

        private string ResolveFontAssetPath(string family, string style)
        {
            if (_fontPathMap == null || _fontPathMap.Count == 0) return string.Empty;
            var familyKey = AiFuiImporterUtility.Slug(family);
            var styleKey = AiFuiImporterUtility.Slug(style);
            string bestPath = string.Empty;
            var bestScore = -1;

            if (_fontMetadata != null)
            {
                foreach (var meta in _fontMetadata)
                {
                    var resolved = ResolveFontMetaPath(meta);
                    if (string.IsNullOrEmpty(resolved)) continue;
                    var score = ScoreFontCandidate(resolved, familyKey, styleKey, meta != null ? meta.Family : string.Empty, meta != null ? meta.Style : string.Empty);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPath = resolved;
                    }
                }
            }

            foreach (var path in _fontPathMap.Values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var score = ScoreFontCandidate(path, familyKey, styleKey, string.Empty, string.Empty);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }

            if (!string.IsNullOrEmpty(bestPath) && bestScore > 0) return bestPath;
            return _fontPathMap.Values.FirstOrDefault() ?? string.Empty;
        }

        private int ScoreFontCandidate(string assetPath, string familyKey, string styleKey, string metaFamily, string metaStyle)
        {
            if (string.IsNullOrEmpty(assetPath)) return 0;
            var nameBlob = AiFuiImporterUtility.Slug(Path.GetFileNameWithoutExtension(assetPath) + " " + assetPath + " " + metaFamily + " " + metaStyle);
            try
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
                if (font != null)
                {
                    nameBlob += " " + AiFuiImporterUtility.Slug(font.name);
                    try
                    {
                        var fontNamesProperty = typeof(Font).GetProperty("fontNames");
                        var fontNamesValue = fontNamesProperty != null ? fontNamesProperty.GetValue(font, null) as string[] : null;
                        if (fontNamesValue != null) nameBlob += " " + AiFuiImporterUtility.Slug(string.Join(" ", fontNamesValue));
                    }
                    catch {}
                }
            }
            catch {}

            var score = 0;
            if (!string.IsNullOrEmpty(familyKey))
            {
                if (nameBlob.Contains(familyKey)) score += 100;
                else
                {
                    var parts = familyKey.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts) if (part.Length >= 3 && nameBlob.Contains(part)) score += 15;
                }
            }
            if (!string.IsNullOrEmpty(styleKey))
            {
                if (nameBlob.Contains(styleKey)) score += 50;
                if (styleKey.Contains("bold") && nameBlob.Contains("bold")) score += 20;
                if ((styleKey.Contains("italic") || styleKey.Contains("oblique")) && (nameBlob.Contains("italic") || nameBlob.Contains("oblique"))) score += 20;
                if ((styleKey.Contains("regular") || styleKey.Contains("normal")) && (nameBlob.Contains("regular") || nameBlob.Contains("normal"))) score += 10;
            }
            return score;
        }

        private string ResolveFontMetaPath(FuiFontMeta meta)
        {
            if (meta == null) return string.Empty;
            string resolved;
            if (!string.IsNullOrEmpty(meta.Path) && _fontPathMap.TryGetValue(AiFuiImporterUtility.NormalizePackagePath(meta.Path), out resolved)) return resolved;
            if (!string.IsNullOrEmpty(meta.FileName) && _fontPathMap.TryGetValue(AiFuiImporterUtility.NormalizePackagePath(meta.FileName), out resolved)) return resolved;
            if (!string.IsNullOrEmpty(meta.FileName))
            {
                foreach (var kv in _fontPathMap)
                {
                    if (kv.Key.EndsWith("/" + meta.FileName, StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetFileName(kv.Key), meta.FileName, StringComparison.OrdinalIgnoreCase)) return kv.Value;
                }
            }
            return string.Empty;
        }

        private static double ResolveLineHeightPx(Dictionary<string, object> lineHeight, double fontSize)
        {
            if (lineHeight == null) return 0;
            var unit = FuiJson.GetString(lineHeight, "unit", string.Empty).ToUpperInvariant();
            var value = FuiJson.GetDouble(lineHeight, "value", 0);
            if (unit == "PIXELS") return value;
            if (unit == "PERCENT" && fontSize > 0) return value / 100.0 * fontSize;
            return 0;
        }

        private static double ResolveLetterSpacingPx(Dictionary<string, object> letterSpacing, double fontSize)
        {
            if (letterSpacing == null) return 0;
            var unit = FuiJson.GetString(letterSpacing, "unit", string.Empty).ToUpperInvariant();
            var value = FuiJson.GetDouble(letterSpacing, "value", 0);
            if (unit == "PIXELS") return value;
            if (unit == "PERCENT" && fontSize > 0) return value / 100.0 * fontSize;
            return 0;
        }

        private void AppendAssetBackground(Dictionary<string, object> assetRef)
        {
            if (assetRef == null) return;
            var resolved = ResolveAssetPath(assetRef);
            if (string.IsNullOrEmpty(resolved))
            {
                var path = FuiJson.GetString(assetRef, "path", string.Empty);
                var id = FuiJson.GetString(assetRef, "id", string.Empty);
                AddWarning("Не найдена текстура для assetRef " + (path == string.Empty ? id : path));
                return;
            }

            _uss.AppendLine("  background-image: url(\"project://database/" + CssUrl(AiFuiImporterUtility.NormalizeAssetPath(resolved)) + "\");");
            _uss.AppendLine("  -unity-background-scale-mode: stretch-to-fill;");
        }

        private string ResolveAssetPath(Dictionary<string, object> assetRef)
        {
            if (assetRef == null) return string.Empty;
            var path = FuiJson.GetString(assetRef, "path", string.Empty);
            var id = FuiJson.GetString(assetRef, "id", string.Empty);
            var resolved = string.Empty;
            if (!string.IsNullOrEmpty(path)) _assetPathMap.TryGetValue(AiFuiImporterUtility.NormalizePackagePath(path), out resolved);
            if (string.IsNullOrEmpty(resolved) && !string.IsNullOrEmpty(id)) _assetPathMap.TryGetValue(id, out resolved);
            return resolved ?? string.Empty;
        }

        private static bool IsTextRasterized(Dictionary<string, object> element)
        {
            // Text must remain editable. Figma gradients, shadows and outlines are mapped to
            // UI Toolkit rich text / USS text effects instead of baking PNG text.
            return false;
        }

        private static bool IsAtomicLayered(Dictionary<string, object> element)
        {
            if (element == null) return false;
            if (FuiJson.GetString(element, "atomicVisualComponent", string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (FuiJson.GetString(element, "preserveInternalLayers", string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            var layout = FuiJson.GetObject(element, "layout");
            return FuiJson.GetString(layout, "source", string.Empty).Equals("atomic-layered-component", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<object> OrderChildrenForUxml(Dictionary<string, object> parent, List<object> children, string parentLayoutMode)
        {
            if (children == null) yield break;
            var list = children.Select((item, index) => new { Item = item, Dict = item as Dictionary<string, object>, Index = index }).ToList();
            var layout = FuiJson.GetObject(parent, "layout");
            var mode = FuiJson.GetString(layout, "mode", parentLayoutMode ?? "flex");
            var direction = FuiJson.GetString(layout, "flexDirection", "column").ToLowerInvariant();
            var parentBounds = FuiJson.GetObject(parent, "bounds");
            var parentIsLayered = IsAtomicLayered(parent) || mode.Equals("absolute", StringComparison.OrdinalIgnoreCase);

            var ordered = list.AsEnumerable();
            if (mode.Equals("flex", StringComparison.OrdinalIgnoreCase) && !parentIsLayered)
            {
                ordered = direction == "row"
                    ? list.OrderBy(x => IsPopupElement(x.Dict) ? 1 : 0).ThenBy(x => GetBoundsDouble(x.Dict, "x")).ThenBy(x => GetBoundsDouble(x.Dict, "y")).ThenBy(x => x.Index)
                    : list.OrderBy(x => IsPopupElement(x.Dict) ? 1 : 0).ThenBy(x => GetBoundsDouble(x.Dict, "y")).ThenBy(x => GetBoundsDouble(x.Dict, "x")).ThenBy(x => x.Index);
            }
            else
            {
                // Absolute/layered containers must keep visual depth, not flow order.
                // Full-screen/oversized backgrounds go first; Popup/modal overlays go last so they render above the screen.
                ordered = list.OrderBy(x => LayerRoleWeight(x.Dict, parentBounds)).ThenBy(x => x.Index);
            }

            foreach (var item in ordered) yield return item.Item;
        }

        private static double GetBoundsDouble(Dictionary<string, object> element, string key)
        {
            var bounds = FuiJson.GetObject(element, "bounds");
            return FuiJson.GetDouble(bounds, key, 0);
        }

        private static bool IsPopupElement(Dictionary<string, object> element)
        {
            if (element == null) return false;
            var type = NormalizeType(FuiJson.GetString(element, "elementType", "Panel"));
            var name = (FuiJson.GetString(element, "name", string.Empty) + " " + FuiJson.GetString(element, "originalName", string.Empty)).ToLowerInvariant();
            var anchor = FuiJson.GetObject(element, "anchor");
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            return type == "Popup" || scenario.StartsWith("MODAL", StringComparison.OrdinalIgnoreCase) || name.Contains("modal") || name.Contains("popup");
        }

        private static int LayerRoleWeight(Dictionary<string, object> element, Dictionary<string, object> parentBounds)
        {
            if (element == null) return 50;
            var type = NormalizeType(FuiJson.GetString(element, "elementType", "Panel"));
            var name = (FuiJson.GetString(element, "name", string.Empty) + " " + FuiJson.GetString(element, "originalName", string.Empty)).ToLowerInvariant();
            var bounds = FuiJson.GetObject(element, "bounds");
            var anchor = FuiJson.GetObject(element, "anchor");
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            if (IsPopupElement(element)) return 200;
            if (scenario == "A_FULL_STRETCH_BACKGROUND" || CoversParent(bounds, parentBounds, 0.72)) return 0;
            if (name.Contains("background") || name.Contains("bg") || name.Contains("fon") || name.Contains("фон") || name.Contains("back")) return 5;
            if (name.Contains("track") || name.Contains("bar_bg") || name.Contains("bar bg") || name.Contains("plate") || name.Contains("плаш")) return 10;
            if (name.Contains("mask") || name.Contains("underlay")) return 12;
            if (name.Contains("fill") || name.Contains("progress") || name.Contains("loading")) return 25;
            if (name.Contains("tint") || name.Contains("overlay") || name.Contains("shade")) return 30;
            if (type == "Image") return 40;
            if (type == "Panel") return 50;
            if (type == "Button" || type == "CurrencyPanel" || type == "InventorySlot" || type == "ProgressBar") return 60;
            if (type == "Label" || IsTextRasterized(element)) return 90;
            return 70;
        }

        private static bool CoversParent(Dictionary<string, object> bounds, Dictionary<string, object> parentBounds, double minRatio)
        {
            if (bounds == null || parentBounds == null) return false;
            var pw = FuiJson.GetDouble(parentBounds, "width", 0);
            var ph = FuiJson.GetDouble(parentBounds, "height", 0);
            var w = FuiJson.GetDouble(bounds, "width", 0);
            var h = FuiJson.GetDouble(bounds, "height", 0);
            if (pw <= 0 || ph <= 0 || w <= 0 || h <= 0) return false;
            var parentArea = pw * ph;
            var ownArea = w * h;
            if ((ownArea / parentArea) >= minRatio) return true;
            var px = FuiJson.GetDouble(parentBounds, "x", 0);
            var py = FuiJson.GetDouble(parentBounds, "y", 0);
            var x = FuiJson.GetDouble(bounds, "x", 0);
            var y = FuiJson.GetDouble(bounds, "y", 0);
            return x <= px + 2 && y <= py + 2 && x + w >= px + pw - 2 && y + h >= py + ph - 2;
        }

        private static string MapUxmlTag(string type)
        {
            switch (type)
            {
                case "Button": return "ui:Button";
                case "Text": return "ui:Label";
                case "Label": return "ui:Label";
                case "Input": return "ui:TextField";
                case "Входные данные": return "ui:TextField";
                case "ProgressBar": return "ui:ProgressBar";
                case "ScrollView": return "ui:ScrollView";
                case "Slider": return "ui:Slider";
                default: return "ui:VisualElement";
            }
        }

        private static bool CanHaveChildren(string type)
        {
            return type == "Screen" || type == "Panel" || type == "CurrencyPanel" || type == "InventorySlot" || type == "Popup" || type == "Button" || type == "ScrollView" || type == "Image";
        }

        private static string NormalizeType(string type)
        {
            if (string.IsNullOrEmpty(type)) return "Panel";
            switch (type.Trim())
            {
                case "Screen": return "Screen";
                case "Button": return "Button";
                case "Text": return "Label";
                case "Label": return "Label";
                case "Input": return "Input";
                case "Входные данные": return "Input";
                case "ProgressBar": return "ProgressBar";
                case "ScrollView": return "ScrollView";
                case "CurrencyPanel": return "CurrencyPanel";
                case "InventorySlot": return "InventorySlot";
                case "Popup": return "Popup";
                case "Slider": return "Slider";
                case "Image": return "Image";
                case "Panel": return "Panel";
                default: return "Panel";
            }
        }

        private static string GetOwnText(Dictionary<string, object> element)
        {
            var own = FuiJson.GetString(element, "text", string.Empty);
            if (!string.IsNullOrWhiteSpace(own)) return own.Trim();
            var style = FuiJson.GetObject(element, "style");
            own = FuiJson.GetString(style, "text", string.Empty);
            if (!string.IsNullOrWhiteSpace(own)) return own.Trim();
            return string.Empty;
        }

        private static string FindVisibleText(Dictionary<string, object> element)
        {
            var own = FuiJson.GetString(element, "text", string.Empty);
            if (!string.IsNullOrWhiteSpace(own)) return own.Trim();
            var style = FuiJson.GetObject(element, "style");
            own = FuiJson.GetString(style, "text", string.Empty);
            if (!string.IsNullOrWhiteSpace(own)) return own.Trim();
            var children = FuiJson.GetArray(element, "children");
            if (children == null) return string.Empty;
            foreach (var child in children)
            {
                var dict = child as Dictionary<string, object>;
                if (dict == null) continue;
                var t = FindVisibleText(dict);
                if (!string.IsNullOrWhiteSpace(t)) return t.Trim();
            }
            return string.Empty;
        }

        private void AddWarning(string message)
        {
            if (_report == null || string.IsNullOrEmpty(message)) return;
            _report.WarningCount++;
            _report.Warnings.Add(message);
        }

        private void AppendPx(string property, double value)
        {
            _uss.AppendLine("  " + property + ": " + Num(value) + "px;");
        }

        private static string ToUnityTextAlign(string horizontal, string vertical)
        {
            var h = (horizontal ?? string.Empty).ToLowerInvariant();
            var v = (vertical ?? string.Empty).ToLowerInvariant();
            var hh = h.Contains("center") ? "center" : h.Contains("right") ? "right" : h.Contains("just") ? "left" : h.Contains("left") ? "left" : string.Empty;
            var vv = v.Contains("center") || v.Contains("middle") ? "middle" : v.Contains("bottom") || v.Contains("lower") ? "lower" : v.Contains("top") || v.Contains("upper") ? "upper" : string.Empty;
            if (string.IsNullOrEmpty(hh) && string.IsNullOrEmpty(vv)) return string.Empty;
            if (string.IsNullOrEmpty(hh)) hh = "left";
            if (string.IsNullOrEmpty(vv)) vv = "middle";
            return vv + "-" + hh;
        }

        private static string ToUnityFontStyle(string figmaStyle)
        {
            var s = (figmaStyle ?? string.Empty).ToLowerInvariant();
            var bold = s.Contains("bold") || s.Contains("black") || s.Contains("heavy") || s.Contains("semibold");
            var italic = s.Contains("italic") || s.Contains("oblique");
            if (bold && italic) return "bold-and-italic";
            if (bold) return "bold";
            if (italic) return "italic";
            return string.Empty;
        }

        private static string CssKeyword(string value, string fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return value.Trim().ToLowerInvariant();
        }

        private static string ColorToCss(Dictionary<string, object> color)
        {
            var r = Mathf.Clamp((int)Math.Round(FuiJson.GetDouble(color, "r", 0)), 0, 255);
            var g = Mathf.Clamp((int)Math.Round(FuiJson.GetDouble(color, "g", 0)), 0, 255);
            var b = Mathf.Clamp((int)Math.Round(FuiJson.GetDouble(color, "b", 0)), 0, 255);
            var a = Math.Max(0, Math.Min(1, FuiJson.GetDouble(color, "a", 1)));
            return "rgba(" + r + ", " + g + ", " + b + ", " + Num(a) + ")";
        }

        private static string Num(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) value = 0;
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string CssUrl(string value)
        {
            return (value ?? string.Empty).Replace("\\", "/").Replace("\"", "\\\"");
        }

        private static string CssComment(string value)
        {
            return (value ?? string.Empty).Replace("*/", "* /");
        }

        private static string CssString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    internal static class AiFuiImporterUtility
    {
        public static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/"); }
        }

        public static string ToAbsolutePath(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            return Path.GetFullPath(Path.Combine(ProjectRoot, assetPath)).Replace("\\", "/");
        }

        public static string NormalizeAssetPath(string path)
        {
            path = (path ?? string.Empty).Replace("\\", "/").Trim();
            while (path.StartsWith("/", StringComparison.Ordinal)) path = path.Substring(1);
            return path;
        }

        public static string NormalizePackagePath(string path)
        {
            path = (path ?? string.Empty).Replace("\\", "/").Trim();
            while (path.StartsWith("/", StringComparison.Ordinal)) path = path.Substring(1);
            return path;
        }

        public static string CombineAssetPath(string left, string right)
        {
            left = NormalizeAssetPath(left).TrimEnd('/');
            right = NormalizeAssetPath(right).TrimStart('/');
            if (string.IsNullOrEmpty(left)) return right;
            if (string.IsNullOrEmpty(right)) return left;
            return left + "/" + right;
        }

        public static string MakeRelativeAssetUrl(string fromAssetPath, string toAssetPath)
        {
            fromAssetPath = NormalizeAssetPath(fromAssetPath);
            toAssetPath = NormalizeAssetPath(toAssetPath);
            try
            {
                var fromDir = Path.GetDirectoryName(fromAssetPath).Replace("\\", "/");
                var uriFrom = new Uri((fromDir.EndsWith("/") ? fromDir : fromDir + "/"), UriKind.Relative);
                var fromAbs = new Uri(Path.GetFullPath(Path.Combine(ProjectRoot, fromDir)).Replace("\\", "/") + "/");
                var toAbs = new Uri(Path.GetFullPath(Path.Combine(ProjectRoot, toAssetPath)).Replace("\\", "/"));
                return Uri.UnescapeDataString(fromAbs.MakeRelativeUri(toAbs).ToString()).Replace("\\", "/");
            }
            catch
            {
                return NormalizeAssetPath(toAssetPath);
            }
        }

        public static string TrimPrefix(string value, string prefix)
        {
            value = NormalizePackagePath(value);
            prefix = NormalizePackagePath(prefix);
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return value.Substring(prefix.Length);
            return value;
        }

        public static void EnsureAssetFolder(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath).TrimEnd('/');
            if (string.IsNullOrEmpty(assetPath)) return;
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parts = assetPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets") throw new ArgumentException("Folder must be inside Assets: " + assetPath);
            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static string MakeUniqueName(string baseName, Dictionary<string, int> used)
        {
            baseName = string.IsNullOrEmpty(baseName) ? "Item" : baseName;
            if (used == null) return baseName;
            int count;
            if (!used.TryGetValue(baseName, out count))
            {
                used[baseName] = 1;
                return baseName;
            }
            count++;
            used[baseName] = count;
            return baseName + "_" + count.ToString(CultureInfo.InvariantCulture);
        }

        public static string Slug(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.ToString();
        }

        public static string SanitizeFileName(string value, string fallback)
        {
            value = string.IsNullOrEmpty(value) ? fallback : value;
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                if (Array.IndexOf(invalid, ch) >= 0 || ch == '/' || ch == '\\' || ch == ':' || ch == '*') sb.Append('_');
                else if (char.IsWhiteSpace(ch)) sb.Append('_');
                else sb.Append(ch);
            }
            var result = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? fallback : result;
        }

        public static string SanitizeIdentifier(string value, string fallback)
        {
            value = string.IsNullOrEmpty(value) ? fallback : value;
            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-') sb.Append(ch);
                else if (char.IsWhiteSpace(ch)) sb.Append('_');
                else sb.Append('_');
            }
            var result = sb.ToString().Trim('_');
            if (string.IsNullOrEmpty(result)) result = fallback;
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }

        public static string SanitizeRelativePath(string relativePath, string fallback)
        {
            relativePath = NormalizePackagePath(relativePath);
            if (string.IsNullOrEmpty(relativePath)) relativePath = fallback;
            var parts = relativePath.Split('/');
            for (var i = 0; i < parts.Length; i++) parts[i] = SanitizeFileName(parts[i], i == parts.Length - 1 ? fallback : "Folder");
            return string.Join("/", parts);
        }
    }

    internal static class FuiJson
    {
        public static object Deserialize(string json)
        {
            return new Parser(json).ParseValue();
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            WriteJsonValue(sb, value, 0);
            return sb.ToString();
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> dict, string key)
        {
            if (dict == null || key == null || !dict.ContainsKey(key)) return null;
            return dict[key] as Dictionary<string, object>;
        }

        public static List<object> GetArray(Dictionary<string, object> dict, string key)
        {
            if (dict == null || key == null || !dict.ContainsKey(key)) return null;
            return dict[key] as List<object>;
        }

        public static string GetString(Dictionary<string, object> dict, string key, string fallback)
        {
            if (dict == null || key == null || !dict.ContainsKey(key) || dict[key] == null) return fallback;
            return Convert.ToString(dict[key], CultureInfo.InvariantCulture) ?? fallback;
        }

        public static double GetDouble(Dictionary<string, object> dict, string key, double fallback)
        {
            if (dict == null || key == null || !dict.ContainsKey(key) || dict[key] == null) return fallback;
            try { return Convert.ToDouble(dict[key], CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static bool GetBool(Dictionary<string, object> dict, string key, bool fallback)
        {
            if (dict == null || key == null || !dict.ContainsKey(key) || dict[key] == null) return fallback;
            var value = dict[key];
            if (value is bool) return (bool)value;
            try
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(text)) return fallback;
                if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1") return true;
                if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) || text == "0") return false;
                return fallback;
            }
            catch { return fallback; }
        }

        public static double? GetNullableDouble(Dictionary<string, object> dict, string key)
        {
            if (dict == null || key == null || !dict.ContainsKey(key) || dict[key] == null) return null;
            try { return Convert.ToDouble(dict[key], CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        private static void WriteJsonValue(StringBuilder sb, object value, int indent)
        {
            if (value == null) { sb.Append("null"); return; }
            if (value is string) { WriteJsonString(sb, (string)value); return; }
            if (value is bool) { sb.Append((bool)value ? "true" : "false"); return; }
            if (value is int || value is long || value is float || value is double || value is decimal)
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }
            var dict = value as IDictionary;
            if (dict != null)
            {
                sb.Append("{\n");
                var first = true;
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first) sb.Append(",\n");
                    first = false;
                    sb.Append(new string(' ', (indent + 1) * 2));
                    WriteJsonString(sb, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                    sb.Append(": ");
                    WriteJsonValue(sb, entry.Value, indent + 1);
                }
                sb.Append("\n").Append(new string(' ', indent * 2)).Append("}");
                return;
            }
            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                sb.Append("[");
                var first = true;
                foreach (var item in enumerable)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    WriteJsonValue(sb, item, indent + 1);
                }
                sb.Append("]");
                return;
            }
            WriteJsonString(sb, value.ToString());
        }

        private static void WriteJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(ch)) sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json ?? string.Empty;
            }

            public object ParseValue()
            {
                SkipWhite();
                if (_index >= _json.Length) return null;
                var ch = _json[_index];
                if (ch == '{') return ParseObject();
                if (ch == '[') return ParseArray();
                if (ch == '"') return ParseString();
                if (ch == '-' || char.IsDigit(ch)) return ParseNumber();
                if (Match("true")) return true;
                if (Match("false")) return false;
                if (Match("null")) return null;
                throw new FormatException("Invalid JSON at position " + _index.ToString(CultureInfo.InvariantCulture));
            }

            private Dictionary<string, object> ParseObject()
            {
                var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                Expect('{');
                SkipWhite();
                if (Peek('}')) { _index++; return obj; }
                while (_index < _json.Length)
                {
                    SkipWhite();
                    var key = ParseString();
                    SkipWhite();
                    Expect(':');
                    var value = ParseValue();
                    obj[key] = value;
                    SkipWhite();
                    if (Peek('}')) { _index++; break; }
                    Expect(',');
                }
                return obj;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                Expect('[');
                SkipWhite();
                if (Peek(']')) { _index++; return list; }
                while (_index < _json.Length)
                {
                    list.Add(ParseValue());
                    SkipWhite();
                    if (Peek(']')) { _index++; break; }
                    Expect(',');
                }
                return list;
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_index < _json.Length)
                {
                    var ch = _json[_index++];
                    if (ch == '"') break;
                    if (ch == '\\')
                    {
                        if (_index >= _json.Length) break;
                        var esc = _json[_index++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_index + 4 <= _json.Length)
                                {
                                    var hex = _json.Substring(_index, 4);
                                    sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                    _index += 4;
                                }
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else sb.Append(ch);
                }
                return sb.ToString();
            }

            private object ParseNumber()
            {
                var start = _index;
                if (Peek('-')) _index++;
                while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                if (Peek('.'))
                {
                    _index++;
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }
                if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
                {
                    _index++;
                    if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-')) _index++;
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }
                var raw = _json.Substring(start, _index - start);
                double value;
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return value;
                return 0d;
            }

            private bool Match(string word)
            {
                SkipWhite();
                if (_index + word.Length > _json.Length) return false;
                if (string.Compare(_json, _index, word, 0, word.Length, StringComparison.Ordinal) != 0) return false;
                _index += word.Length;
                return true;
            }

            private bool Peek(char ch)
            {
                return _index < _json.Length && _json[_index] == ch;
            }

            private void Expect(char ch)
            {
                SkipWhite();
                if (_index >= _json.Length || _json[_index] != ch)
                    throw new FormatException("Expected '" + ch + "' at JSON position " + _index.ToString(CultureInfo.InvariantCulture));
                _index++;
            }

            private void SkipWhite()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }
        }
    }
}
#endif
