#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AiMultiToolKit.FuiImporter
{
    public sealed class AiFuiImporterWindow : EditorWindow
    {
        private string _fuiPath = string.Empty;
        private string _outputRoot = "Assets/FUI_Imported";
        private bool _overwriteExisting = true;
        private bool _createPackageSubfolder = true;
        private bool _importTexturesForUiToolkit = true;
        private bool _createPanelSettings = true;
        private bool _createScreenPrefabs = true;
        private bool _addScreensToOpenScene = true;
        private Vector2 _scroll;
        private FuiImportReport _lastReport;
        private string _lastError = string.Empty;

        [MenuItem("Tools/AI Multi-Tool Kit/FUI Importer")]
        public static void OpenWindow()
        {
            var window = GetWindow<AiFuiImporterWindow>("FUI Importer");
            window.minSize = new Vector2(460, 420);
            window.Show();
        }

        [MenuItem("Assets/AI Multi-Tool Kit/Import selected .fui", true)]
        private static bool ValidateImportSelectedFui()
        {
            var path = GetSelectedProjectPath();
            return !string.IsNullOrEmpty(path) && path.EndsWith(".fui", StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/AI Multi-Tool Kit/Import selected .fui")]
        private static void ImportSelectedFui()
        {
            var path = GetSelectedProjectPath();
            if (string.IsNullOrEmpty(path)) return;
            var absolute = Path.GetFullPath(Path.Combine(AiFuiImporterUtility.ProjectRoot, path));
            try
            {
                var report = AiFuiImporter.Import(absolute, new FuiImportOptions());
                EditorUtility.DisplayDialog("FUI Import", report.ToHumanString(), "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("FUI Import Error", ex.Message, "OK");
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

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("AI Multi-Tool Kit · FUI → Unity UI Toolkit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Импортирует .fui из Figma-плагина, раскладывает ассеты и генерирует стандартные UXML/USS. " +
                "После импорта папку этого импортёра можно удалить: созданный UI не зависит от кастомного runtime-кода.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _fuiPath = EditorGUILayout.TextField(".fui file", _fuiPath);
                if (GUILayout.Button("Browse", GUILayout.Width(86)))
                {
                    var picked = EditorUtility.OpenFilePanel("Select .fui package", string.Empty, "fui,zip");
                    if (!string.IsNullOrEmpty(picked)) _fuiPath = picked;
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _outputRoot = EditorGUILayout.TextField("Output root", _outputRoot);
            _createPackageSubfolder = EditorGUILayout.ToggleLeft("Create subfolder by package/project name", _createPackageSubfolder);
            _overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing generated files", _overwriteExisting);
            _importTexturesForUiToolkit = EditorGUILayout.ToggleLeft("Apply recommended Texture settings for UI Toolkit", _importTexturesForUiToolkit);
            _createPanelSettings = EditorGUILayout.ToggleLeft("Create PanelSettings asset", _createPanelSettings);
            _createScreenPrefabs = EditorGUILayout.ToggleLeft("Create ready UIDocument prefabs", _createScreenPrefabs);
            _addScreensToOpenScene = EditorGUILayout.ToggleLeft("Add generated screens to the open scene", _addScreensToOpenScene);

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_fuiPath)))
            {
                if (GUILayout.Button("Import .fui to UI Toolkit", GUILayout.Height(34)))
                {
                    RunImport();
                }
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }

            if (_lastReport != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Last import", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_lastReport.ToHumanString(), MessageType.None);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping output folder"))
                    {
                        var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_lastReport.OutputRootAssetPath);
                        if (folder != null) EditorGUIUtility.PingObject(folder);
                    }
                    if (GUILayout.Button("Open in Explorer/Finder"))
                    {
                        EditorUtility.RevealInFinder(AiFuiImporterUtility.ToAbsolutePath(_lastReport.OutputRootAssetPath));
                    }
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "Generated structure:\n" +
                "UI/*.uxml + UI/*.uss — ready for UI Document / UI Builder\n" +
                "Textures/* — images referenced by USS background-image\n" +
                "Fonts/* — copied font files, if they were packed into .fui\n" +
                "Source/* — manifest/metadata/screen JSON for debugging",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private void RunImport()
        {
            _lastError = string.Empty;
            _lastReport = null;

            try
            {
                var options = new FuiImportOptions
                {
                    OutputRootAssetPath = _outputRoot,
                    OverwriteExisting = _overwriteExisting,
                    CreatePackageSubfolder = _createPackageSubfolder,
                    ApplyTextureSettings = _importTexturesForUiToolkit,
                    CreatePanelSettings = _createPanelSettings,
                    CreateScreenPrefabs = _createScreenPrefabs,
                    AddScreensToOpenScene = _addScreensToOpenScene
                };

                _lastReport = AiFuiImporter.Import(_fuiPath, options);
                Debug.Log("[AI FUI Importer] " + _lastReport.ToHumanString());
                EditorUtility.DisplayDialog("FUI Import Complete", _lastReport.ToHumanString(), "OK");
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
        public string OutputRootAssetPath = "Assets/FUI_Imported";
        public bool OverwriteExisting = true;
        public bool CreatePackageSubfolder = true;
        public bool ApplyTextureSettings = true;
        public bool CreatePanelSettings = true;
        public bool CreateScreenPrefabs = true;
        public bool AddScreensToOpenScene = true;
    }

    public sealed class FuiImportReport
    {
        public string PackageName;
        public string OutputRootAssetPath;
        public readonly List<string> GeneratedUxml = new List<string>();
        public readonly List<string> GeneratedUss = new List<string>();
        public readonly List<string> GeneratedPrefabs = new List<string>();
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
            sb.AppendLine("Package: " + (PackageName ?? "FUI"));
            sb.AppendLine("Output: " + (OutputRootAssetPath ?? ""));
            sb.AppendLine("Screens: " + ScreenCount);
            sb.AppendLine("UXML: " + GeneratedUxml.Count);
            sb.AppendLine("USS: " + GeneratedUss.Count);
            sb.AppendLine("Prefabs: " + GeneratedPrefabs.Count);
            if (!string.IsNullOrEmpty(PanelSettingsAssetPath)) sb.AppendLine("PanelSettings: " + PanelSettingsAssetPath);
            if (!string.IsNullOrEmpty(SceneRootObjectName)) sb.AppendLine("Scene root: " + SceneRootObjectName);
            sb.AppendLine("Textures: " + CopiedTextureCount);
            sb.AppendLine("Fonts copied: " + CopiedFontCount);
            if (WarningCount > 0)
            {
                sb.AppendLine("Warnings: " + WarningCount);
                for (var i = 0; i < Math.Min(Warnings.Count, 8); i++) sb.AppendLine("- " + Warnings[i]);
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class AiFuiImporter
    {
        public static FuiImportReport Import(string fuiFilePath, FuiImportOptions options)
        {
            options = options ?? new FuiImportOptions();
            if (string.IsNullOrEmpty(fuiFilePath)) throw new ArgumentException("FUI path is empty.");
            if (!File.Exists(fuiFilePath)) throw new FileNotFoundException("FUI file not found.", fuiFilePath);

            var normalizedRoot = AiFuiImporterUtility.NormalizeAssetPath(options.OutputRootAssetPath);
            if (!normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal) && normalizedRoot != "Assets")
                throw new ArgumentException("Output root must be inside Assets/. Example: Assets/FUI_Imported");

            var package = FuiPackage.Read(fuiFilePath);
            var projectName = AiFuiImporterUtility.SanitizeFileName(package.ProjectName, Path.GetFileNameWithoutExtension(fuiFilePath));
            var outputRoot = options.CreatePackageSubfolder ? AiFuiImporterUtility.CombineAssetPath(normalizedRoot, projectName) : normalizedRoot;
            outputRoot = PrepareOutputRoot(outputRoot, options.OverwriteExisting);

            var report = new FuiImportReport
            {
                PackageName = projectName,
                OutputRootAssetPath = outputRoot
            };
            var generatedScreens = new List<FuiGeneratedScreenAsset>();

            var sourceRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Source");
            var uiRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "UI");
            var textureRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Textures");
            var fontRoot = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Fonts");
            AiFuiImporterUtility.EnsureAssetFolder(sourceRoot);
            AiFuiImporterUtility.EnsureAssetFolder(uiRoot);
            AiFuiImporterUtility.EnsureAssetFolder(textureRoot);
            AiFuiImporterUtility.EnsureAssetFolder(fontRoot);

            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(sourceRoot, "manifest.json"), package.ManifestJson ?? "{}");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(sourceRoot, "metadata.json"), package.MetadataJson ?? "{}");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(sourceRoot, "assets.json"), package.AssetsJson ?? "[]");
            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(sourceRoot, "fonts.json"), package.FontsJson ?? "{}");

            var assetPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in package.AssetFiles)
            {
                var relative = AiFuiImporterUtility.TrimPrefix(file.Path, "assets/");
                var target = AiFuiImporterUtility.CombineAssetPath(textureRoot, AiFuiImporterUtility.SanitizeRelativePath(relative, "asset.png"));
                WriteBinaryAsset(target, file.Bytes);
                assetPathMap[AiFuiImporterUtility.NormalizePackagePath(file.Path)] = target;
                report.CopiedTextureCount++;
            }

            foreach (var meta in package.AssetMetadata)
            {
                if (!string.IsNullOrEmpty(meta.Id) && !string.IsNullOrEmpty(meta.Path))
                {
                    var key = AiFuiImporterUtility.NormalizePackagePath(meta.Path);
                    if (assetPathMap.ContainsKey(key)) assetPathMap[meta.Id] = assetPathMap[key];
                }
            }

            foreach (var file in package.FontFiles)
            {
                var relative = AiFuiImporterUtility.TrimPrefix(file.Path, "fonts/");
                if (string.Equals(relative, "fonts.json", StringComparison.OrdinalIgnoreCase)) continue;
                var target = AiFuiImporterUtility.CombineAssetPath(fontRoot, AiFuiImporterUtility.SanitizeRelativePath(relative, "font.ttf"));
                WriteBinaryAsset(target, file.Bytes);
                report.CopiedFontCount++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (options.ApplyTextureSettings)
            {
                foreach (var path in assetPathMap.Values) ApplyUiTextureImportSettings(path);
            }

            var usedScreenNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var screen in package.Screens)
            {
                var baseScreenName = AiFuiImporterUtility.SanitizeFileName(FuiJson.GetString(screen, "name", "Screen"), "Screen");
                var screenName = AiFuiImporterUtility.MakeUniqueName(baseScreenName, usedScreenNames);
                var ussPath = AiFuiImporterUtility.CombineAssetPath(uiRoot, screenName + ".uss");
                var uxmlPath = AiFuiImporterUtility.CombineAssetPath(uiRoot, screenName + ".uxml");
                var sourcePath = AiFuiImporterUtility.CombineAssetPath(sourceRoot, "screen_" + screenName + ".json");

                var generator = new FuiUiToolkitGenerator(screen, ussPath, assetPathMap, report);
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

            PanelSettings panelSettings = null;
            if (options.CreatePanelSettings || options.CreateScreenPrefabs || options.AddScreensToOpenScene)
            {
                panelSettings = CreatePanelSettingsAsset(outputRoot, projectName, generatedScreens, report);
            }

            if (options.CreateScreenPrefabs)
            {
                CreateScreenPrefabs(outputRoot, generatedScreens, panelSettings, report);
            }

            if (options.AddScreensToOpenScene)
            {
                CreateSceneObjects(projectName, generatedScreens, panelSettings, report);
            }

            WriteTextAsset(AiFuiImporterUtility.CombineAssetPath(outputRoot, "fui-import-report.json"), BuildReportJson(report));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return report;
        }

        private static string PrepareOutputRoot(string outputRoot, bool overwrite)
        {
            if (overwrite)
            {
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
            throw new IOException("Could not create unique output folder for " + outputRoot);
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

        private static void ApplyUiTextureImportSettings(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }


        private static PanelSettings CreatePanelSettingsAsset(string outputRoot, string projectName, List<FuiGeneratedScreenAsset> screens, FuiImportReport report)
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
            var folder = AiFuiImporterUtility.CombineAssetPath(outputRoot, "Prefabs");
            AiFuiImporterUtility.EnsureAssetFolder(folder);
            if (screens == null) return;

            foreach (var screen in screens)
            {
                if (screen == null) continue;
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(screen.UxmlPath);
                if (visualTree == null)
                {
                    AddReportWarning(report, "Could not load generated UXML as VisualTreeAsset: " + screen.UxmlPath);
                    continue;
                }

                var prefabPath = AiFuiImporterUtility.CombineAssetPath(folder, screen.Name + ".prefab");
                var go = new GameObject(screen.Name);
                try
                {
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = panelSettings;
                    doc.visualTreeAsset = visualTree;
                    doc.sortingOrder = 0;
                    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    if (report != null) report.GeneratedPrefabs.Add(prefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private static void CreateSceneObjects(string projectName, List<FuiGeneratedScreenAsset> screens, PanelSettings panelSettings, FuiImportReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AddReportWarning(report, "Scene objects were not created because the Editor is entering Play Mode.");
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
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(screen.UxmlPath);
                if (visualTree == null)
                {
                    AddReportWarning(report, "Could not create scene UIDocument. Missing UXML asset: " + screen.UxmlPath);
                    continue;
                }

                var go = new GameObject(screen.Name);
                go.transform.SetParent(root.transform, false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = panelSettings;
                doc.visualTreeAsset = visualTree;
                doc.sortingOrder = index;
                go.SetActive(index == 0);
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
                Debug.LogWarning("[AI FUI Importer] Standard ZipArchive read failed. Trying tolerant FUI zip reader. Reason: " + zipException.Message);
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

            if (package.Screens.Count == 0) throw new InvalidDataException("No screens/*.json found in FUI package.");
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
                yield return new FuiAssetMeta
                {
                    Id = FuiJson.GetString(dict, "id", string.Empty),
                    Path = FuiJson.GetString(dict, "path", string.Empty)
                };
            }
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
            if (eocd < 0) throw new InvalidDataException("Invalid FUI zip: central directory not found.");

            var totalEntries = ReadUInt16(data, eocd + 10);
            var centralOffset = (int)ReadUInt32(data, eocd + 16);
            var result = new List<FuiArchiveFile>();
            var cursor = centralOffset;

            for (var i = 0; i < totalEntries; i++)
            {
                if (cursor + 46 > data.Length || ReadUInt32(data, cursor) != 0x02014b50)
                    throw new InvalidDataException("Invalid FUI zip: central directory entry is corrupted.");

                var method = ReadUInt16(data, cursor + 10);
                var compressedSize = (int)ReadUInt32(data, cursor + 20);
                var nameLength = ReadUInt16(data, cursor + 28);
                var centralExtraLength = ReadUInt16(data, cursor + 30);
                var commentLength = ReadUInt16(data, cursor + 32);
                var localOffset = (int)ReadUInt32(data, cursor + 42);
                var fileName = Encoding.UTF8.GetString(data, cursor + 46, nameLength);

                if (method != 0)
                    throw new InvalidDataException("Tolerant FUI reader supports only stored zip entries. Standard ZipArchive should be used for compressed files.");
                if (localOffset + 30 > data.Length || ReadUInt32(data, localOffset) != 0x04034b50)
                    throw new InvalidDataException("Invalid FUI zip: local file header is corrupted for " + fileName);

                var localNameLength = ReadUInt16(data, localOffset + 26);
                var localExtraLength = ReadUInt16(data, localOffset + 28);
                var dataStart = localOffset + 30 + localNameLength + localExtraLength;

                // AI Multi-Tool Kit 3.2.2 writes a browser-side stored zip. Older builds may have an incorrect
                // local extra length equal to file-name length while the central directory says there is no extra field.
                // In that case we trust the central directory and keep reading the package instead of failing import.
                if (centralExtraLength == 0 && localExtraLength == localNameLength)
                    dataStart = localOffset + 30 + localNameLength;

                if (dataStart < 0 || dataStart + compressedSize > data.Length)
                    throw new InvalidDataException("Invalid FUI zip: entry data is out of range for " + fileName);

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
        private readonly FuiImportReport _report;
        private readonly StringBuilder _uss = new StringBuilder();
        private int _elementSeq;
        private string _screenClass;

        public FuiUiToolkitGenerator(Dictionary<string, object> screen, string ussAssetPath, Dictionary<string, string> assetPathMap, FuiImportReport report)
        {
            _screen = screen;
            _ussAssetPath = ussAssetPath;
            _assetPathMap = assetPathMap ?? new Dictionary<string, string>();
            _report = report;
        }

        public FuiGeneratedScreen Generate()
        {
            var screenName = AiFuiImporterUtility.SanitizeIdentifier(FuiJson.GetString(_screen, "name", "Screen"), "Screen");
            _screenClass = "fui_screen_" + screenName;
            var root = FuiJson.GetObject(_screen, "root");
            if (root == null) root = _screen;

            _uss.AppendLine("/* Auto-generated by AI Multi-Tool Kit FUI Importer. */");
            _uss.AppendLine("/* Safe to keep after deleting the importer. Uses standard Unity UI Toolkit USS only. */");
            _uss.AppendLine("." + _screenClass + " {");
            _uss.AppendLine("  width: 100%;");
            _uss.AppendLine("  height: 100%;");
            _uss.AppendLine("  flex-grow: 1;");
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

            var rootXml = BuildElementXml(root, 1, true, "root");
            var uxml = new StringBuilder();
            uxml.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" editor-extension-mode=\"False\">");
            uxml.AppendLine("    <Style src=\"project://database/" + XmlEscape(AiFuiImporterUtility.NormalizeAssetPath(_ussAssetPath)) + "\" />");
            uxml.Append(rootXml);
            uxml.AppendLine("</ui:UXML>");

            return new FuiGeneratedScreen { Uxml = uxml.ToString(), Uss = _uss.ToString() };
        }

        private string BuildElementXml(Dictionary<string, object> element, int indent, bool isRoot, string parentLayoutMode)
        {
            var rawType = NormalizeType(FuiJson.GetString(element, "elementType", "Panel"));
            var childList = FuiJson.GetArray(element, "children");
            var ownText = GetOwnText(element);
            var type = rawType;

            // Figma groups that contain several text nodes can be marked as Label by the exporter.
            // UI Toolkit Label cannot hold children, so group-labels become VisualElement containers
            // while true Figma TEXT nodes still become ui:Label.
            if (rawType == "Label" && childList != null && childList.Count > 0 && string.IsNullOrWhiteSpace(ownText))
                type = "Panel";

            var tag = MapUxmlTag(type);
            var rawName = FuiJson.GetString(element, "name", type);
            var name = AiFuiImporterUtility.SanitizeIdentifier(rawName, type + "_" + _elementSeq.ToString(CultureInfo.InvariantCulture));
            var className = "fui_el_" + (++_elementSeq).ToString("0000", CultureInfo.InvariantCulture);
            var classes = new List<string> { "fui_element", className, "fui_type_" + rawType.ToLowerInvariant() };
            if (type != rawType) classes.Add("fui_as_" + type.ToLowerInvariant());
            if (type == "Button") classes.Add("fui_button");
            if (isRoot) classes.Add(_screenClass);

            AppendStyle(element, className, type, isRoot, parentLayoutMode);

            var canHaveChildren = CanHaveChildren(type);
            var pad = new string(' ', indent * 4);
            var attrs = new StringBuilder();
            attrs.Append(" name=\"").Append(XmlEscape(name)).Append("\"");
            attrs.Append(" class=\"").Append(XmlEscape(string.Join(" ", classes.ToArray()))).Append("\"");

            var text = !string.IsNullOrWhiteSpace(ownText) ? ownText : FindVisibleText(element);
            if (type == "Label") attrs.Append(" text=\"").Append(XmlEscape(text)).Append("\"");
            else if (type == "Button" && (childList == null || childList.Count == 0) && !string.IsNullOrEmpty(text)) attrs.Append(" text=\"").Append(XmlEscape(text)).Append("\"");
            else if (type == "Input")
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
            foreach (var child in childList)
            {
                var childDict = child as Dictionary<string, object>;
                if (childDict != null) sb.Append(BuildElementXml(childDict, indent + 1, false, ownLayoutMode));
            }
            sb.Append(pad).Append("</").Append(tag).AppendLine(">");
            return sb.ToString();
        }

        private void AppendStyle(Dictionary<string, object> element, string className, string type, bool isRoot, string parentLayoutMode)
        {
            var bounds = FuiJson.GetObject(element, "bounds");
            var layout = FuiJson.GetObject(element, "layout");
            var anchor = FuiJson.GetObject(element, "anchor");
            var style = FuiJson.GetObject(element, "style");
            var assetRef = FuiJson.GetObject(element, "assetRef");
            var width = FuiJson.GetDouble(bounds, "width", 0);
            var height = FuiJson.GetDouble(bounds, "height", 0);
            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            var parentAbsolute = string.Equals(parentLayoutMode, "absolute", StringComparison.OrdinalIgnoreCase);
            var forceAbsolute = parentAbsolute || scenario == "A_FULL_STRETCH_BACKGROUND" || scenario == "B_BOTTOM_RIGHT";

            _uss.AppendLine("." + className + " {");

            if (isRoot)
            {
                _uss.AppendLine("  width: 100%;");
                _uss.AppendLine("  height: 100%;");
                _uss.AppendLine("  position: relative;");
            }
            else if (forceAbsolute)
            {
                _uss.AppendLine("  position: absolute;");
                AppendAbsoluteAnchor(anchor, width, height);
            }
            else
            {
                _uss.AppendLine("  position: relative;");
                if (scenario == "C_FULL_WIDTH_FIXED_HEIGHT")
                {
                    _uss.AppendLine("  align-self: stretch;");
                    AppendPx("height", height);
                }
                else
                {
                    AppendPx("width", width);
                    AppendPx("height", height);
                    _uss.AppendLine("  flex-shrink: 0;");
                    if (scenario == "D_CENTER_CONTENT") _uss.AppendLine("  align-self: center;");
                }
            }

            AppendLayout(layout);
            AppendVisualStyle(style);
            AppendTextStyle(style);
            AppendAssetBackground(assetRef);

            if (type == "Button")
            {
                _uss.AppendLine("  -unity-background-scale-mode: stretch-to-fill;");
            }

            _uss.AppendLine("}");
            _uss.AppendLine();
        }

        private void AppendAbsoluteAnchor(Dictionary<string, object> anchor, double width, double height)
        {
            if (anchor == null)
            {
                AppendPx("left", 0);
                AppendPx("top", 0);
                AppendPx("width", width);
                AppendPx("height", height);
                return;
            }

            var scenario = FuiJson.GetString(anchor, "scenario", string.Empty);
            if (scenario == "A_FULL_STRETCH_BACKGROUND")
            {
                _uss.AppendLine("  left: 0;");
                _uss.AppendLine("  right: 0;");
                _uss.AppendLine("  top: 0;");
                _uss.AppendLine("  bottom: 0;");
                return;
            }

            if (anchor.ContainsKey("left")) AppendPx("left", FuiJson.GetDouble(anchor, "left", 0));
            if (anchor.ContainsKey("right")) AppendPx("right", FuiJson.GetDouble(anchor, "right", 0));
            if (anchor.ContainsKey("top")) AppendPx("top", FuiJson.GetDouble(anchor, "top", 0));
            if (anchor.ContainsKey("bottom")) AppendPx("bottom", FuiJson.GetDouble(anchor, "bottom", 0));
            AppendPx("width", FuiJson.GetDouble(anchor, "width", width));
            AppendPx("height", FuiJson.GetDouble(anchor, "height", height));
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

            var gap = FuiJson.GetDouble(layout, "gap", 0);
            if (gap > 0)
            {
                AppendPx("row-gap", gap);
                AppendPx("column-gap", gap);
            }

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
            if (color != null) _uss.AppendLine("  color: " + ColorToCss(color) + ";");

            var fontSize = FuiJson.GetNullableDouble(style, "fontSize");
            if (fontSize.HasValue && fontSize.Value > 0) AppendPx("font-size", fontSize.Value);

            var textAlign = FuiJson.GetString(style, "textAlign", string.Empty);
            var verticalAlign = FuiJson.GetString(style, "verticalAlign", string.Empty);
            var unityTextAlign = ToUnityTextAlign(textAlign, verticalAlign);
            if (!string.IsNullOrEmpty(unityTextAlign)) _uss.AppendLine("  -unity-text-align: " + unityTextAlign + ";");

            var fontStyle = FuiJson.GetString(style, "fontStyle", string.Empty);
            var mapped = ToUnityFontStyle(fontStyle);
            if (!string.IsNullOrEmpty(mapped)) _uss.AppendLine("  -unity-font-style: " + mapped + ";");

            var family = FuiJson.GetString(style, "fontFamily", string.Empty);
            if (!string.IsNullOrEmpty(family))
            {
                _uss.AppendLine("  /* Figma font family: " + CssComment(family) + ". Font files are copied to Fonts/ when present; create/assign Unity Font Assets if your project needs exact font binding. */");
            }
        }

        private void AppendAssetBackground(Dictionary<string, object> assetRef)
        {
            if (assetRef == null) return;
            var path = FuiJson.GetString(assetRef, "path", string.Empty);
            var id = FuiJson.GetString(assetRef, "id", string.Empty);
            var resolved = string.Empty;
            if (!string.IsNullOrEmpty(path)) _assetPathMap.TryGetValue(AiFuiImporterUtility.NormalizePackagePath(path), out resolved);
            if (string.IsNullOrEmpty(resolved) && !string.IsNullOrEmpty(id)) _assetPathMap.TryGetValue(id, out resolved);
            if (string.IsNullOrEmpty(resolved))
            {
                AddWarning("Missing texture for assetRef " + (path == string.Empty ? id : path));
                return;
            }

            _uss.AppendLine("  background-image: url(\"project://database/" + CssUrl(AiFuiImporterUtility.NormalizeAssetPath(resolved)) + "\");");
            _uss.AppendLine("  -unity-background-scale-mode: stretch-to-fill;");
        }

        private static string MapUxmlTag(string type)
        {
            switch (type)
            {
                case "Button": return "ui:Button";
                case "Text": return "ui:Label";
                case "Label": return "ui:Label";
                case "Input": return "ui:TextField";
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
                case "Screen":
                case "Button":
                case "Text": return "Label";
                case "Label":
                case "Input":
                case "ProgressBar":
                case "ScrollView":
                case "CurrencyPanel":
                case "InventorySlot":
                case "Popup":
                case "Slider":
                case "Image":
                case "Panel": return type.Trim();
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
