using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// WebGL 빌드(itch.io용). 설정: Gzip, Decompression Fallback 끔, 960x600, Exception None, 프로젝트 템플릿(ShiftMagicSpell).
    /// bundleVersion은 빌드하는 동안만 빌드 이름으로 바꾸고 끝나면 되돌린다(버전 변경은 커밋하지 않음).
    /// 배치 실행: -executeMethod ShiftMagicSpell.Editor.WebGLBuildUtils.BuildFromCommandLine -buildVersion 이름 -buildOutput 폴더
    /// </summary>
    public static class WebGLBuildUtils
    {
        private const int WEB_WIDTH = 960;
        private const int WEB_HEIGHT = 600;

        // Assets/WebGLTemplates/ShiftMagicSpell: 하단 막대 없이 캔버스만 960x600(itch.io 임베드 크기와 같게)
        private const string WEB_TEMPLATE = "PROJECT:ShiftMagicSpell";

        [MenuItem("Tools/Shift Magic Spell/Build WebGL")]
        public static void BuildFromMenu()
        {
            string version = PlayerSettings.bundleVersion;
            Build(version, Path.Combine("Builds", "WebGL", "shift-magic-spell-" + version));
        }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string version = GetArgOrNull(args, "-buildVersion");
            string output = GetArgOrNull(args, "-buildOutput");
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(output))
            {
                Debug.LogError("[ERROR] WebGLBuildUtils::BuildFromCommandLine() - Missing arguments. Details: [Need=-buildVersion, -buildOutput]");
                EditorApplication.Exit(2);
                return;
            }

            BuildResult result = Build(version, output);
            EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildResult Build(string version, string output)
        {
            ApplyWebSettings();
            string previousVersion = PlayerSettings.bundleVersion;
            PlayerSettings.bundleVersion = version;
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                Debug.Log($"[INFO] WebGLBuildUtils::Build() - Build finished. Details: [Result={summary.result}, Version={version}, Output={output}, Size={summary.totalSize}, Errors={summary.totalErrors}, Seconds={summary.totalTime.TotalSeconds:0}]");
                return summary.result;
            }
            finally
            {
                PlayerSettings.bundleVersion = previousVersion;
                AssetDatabase.SaveAssets();
            }
        }

        private static void ApplyWebSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.template = WEB_TEMPLATE;
            PlayerSettings.defaultWebScreenWidth = WEB_WIDTH;
            PlayerSettings.defaultWebScreenHeight = WEB_HEIGHT;

            // 스플래시가 떠 있는 동안 한 첫 클릭은 버려진다: 로딩이 끝나면 바로 "CLICK TO START"가 뜨게 끈다
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            Debug.Log($"[INFO] WebGLBuildUtils::ApplyWebSettings() - Web settings. Details: [Compression={PlayerSettings.WebGL.compressionFormat}, DecompressionFallback={PlayerSettings.WebGL.decompressionFallback}, Exceptions={PlayerSettings.WebGL.exceptionSupport}, Size={PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}, Template={PlayerSettings.WebGL.template}]");
        }

        private static string GetArgOrNull(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
