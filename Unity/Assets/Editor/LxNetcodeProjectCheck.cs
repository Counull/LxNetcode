using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.Rendering;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace LxNetcode.Development
{
    /// <summary>验证开发工程的包接入、编译边界及渲染管线配置，不随 UPM 包分发。</summary>
    public static class LxNetcodeProjectCheck
    {
        private const string PackageName = "com.counull.lxnetcode";
        private const string RuntimeAssemblyName = "LxNetcode.Sockets";

        [Serializable]
        private sealed class Report
        {
            public string unityVersion;
            public string buildTarget;
            public string apiCompatibilityLevel;
            public int apiCompatibilityLevelValue;
            public string scriptingBackend;
            public string editorRuntimeVersion;
            public string packageVersion;
            public string packageSource;
            public string resolvedPath;
            public string runtimeAssembly;
            public bool noEngineReferences;
            public bool localSourceRefresh;
            public string renderPipelineAsset;
            public string renderPipelineType;
        }

        /// <summary>供 -executeMethod 调用，失败时抛出异常令批处理返回非零退出码。</summary>
        public static void Run()
        {
            PackageInfo package = Array.Find(PackageInfo.GetAllRegisteredPackages(),
                item => item.name == PackageName);
            if (package == null)
                throw new InvalidOperationException("LxNetcode package is not registered.");

            string expectedPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", "Packages", PackageName));
            if (package.source != PackageSource.Embedded ||
                !string.Equals(Path.GetFullPath(package.resolvedPath), expectedPath,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Development project must use the embedded package in its Packages directory.");

            Assembly runtimeAssembly = null;
            foreach (Assembly loadedAssembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                if (loadedAssembly.GetName().Name == RuntimeAssemblyName)
                {
                    runtimeAssembly = loadedAssembly;
                    break;
                }
            }
            if (runtimeAssembly == null)
                throw new InvalidOperationException("Pure C# runtime assembly was not loaded.");

            foreach (AssemblyName reference in runtimeAssembly.GetReferencedAssemblies())
            {
                if (reference.Name.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                    reference.Name.StartsWith("UnityEditor", StringComparison.Ordinal))
                    throw new InvalidOperationException("Runtime references a Unity assembly: " + reference.Name);
            }

            RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
            if (renderPipeline == null)
                throw new InvalidOperationException("Development project has no render pipeline asset.");

            VerifyLocalSourceRefresh(package.resolvedPath);

            var apiLevel = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone);
            var report = new Report
            {
                unityVersion = Application.unityVersion,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                apiCompatibilityLevel = apiLevel.ToString(),
                apiCompatibilityLevelValue = (int)apiLevel,
                scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                editorRuntimeVersion = Environment.Version.ToString(),
                packageVersion = package.version,
                packageSource = package.source.ToString(),
                resolvedPath = Path.GetFullPath(package.resolvedPath),
                runtimeAssembly = runtimeAssembly.GetName().Name,
                noEngineReferences = true,
                localSourceRefresh = true,
                renderPipelineAsset = AssetDatabase.GetAssetPath(renderPipeline),
                renderPipelineType = renderPipeline.GetType().FullName
            };
            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", "Logs", "project-check.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(reportPath, json, new UTF8Encoding(false));
            Debug.Log("LXNETCODE_PROJECT_CHECK_PASS\n" + json);
        }

        /// <summary>用临时文本资源验证同一份本地源码在显式刷新后可见，并清理本次探针。</summary>
        private static void VerifyLocalSourceRefresh(string packagePath)
        {
            const string probeName = "LxNetcodeSyncProbe.txt";
            string fullPath = Path.Combine(packagePath, probeName);
            string metaPath = fullPath + ".meta";
            string assetPath = "Packages/" + PackageName + "/" + probeName;
            if (File.Exists(fullPath) || File.Exists(metaPath))
                throw new InvalidOperationException("A local sync probe already exists; refusing to overwrite it.");

            try
            {
                File.WriteAllText(fullPath, "lxnetcode-sync-first", new UTF8Encoding(false));
                AssertImportedText(assetPath, "lxnetcode-sync-first");
                File.WriteAllText(fullPath, "lxnetcode-sync-second", new UTF8Encoding(false));
                AssertImportedText(assetPath, "lxnetcode-sync-second");
            }
            finally
            {
                File.Delete(fullPath);
                File.Delete(metaPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        /// <summary>同步刷新资源并通过 Unity 的导入结果检查文本，避免仅验证文件系统。</summary>
        private static void AssertImportedText(string assetPath, string expected)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null || asset.text != expected)
                throw new InvalidOperationException("Unity did not import the expected local package content.");
        }
    }
}
