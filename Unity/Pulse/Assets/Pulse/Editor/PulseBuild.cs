using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pulse.Editor
{
    public static class PulseBuild
    {
        public const string Scene = "Assets/Pulse/Scenes/Pulse.unity";
        [MenuItem("Pulse/Configure Android")]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "Ondrej Spetko";
            PlayerSettings.productName = "Breakthrough";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "spetko.ondrej.TestPulse");
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.stripEngineCode = true;
            QualitySettings.vSyncCount = 0; QualitySettings.antiAliasing = 0;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            ConfigureAndroidIcons();
            AssetDatabase.SaveAssets();
            Debug.Log("[Pulse] Android configured: portrait, ARM64 IL2CPP, GLES3, API26 minimum, target API36.");
        }

        public static void ConfigureAndroidIcons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pulse/Art/Icon.png");
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pulse/Art/AdaptiveBackground.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Pulse/Art/AdaptiveForeground.png");
            if (icon == null || background == null || foreground == null)
                throw new InvalidOperationException("Breakthrough launcher artwork is missing from Assets/Pulse/Art.");

            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Any);
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            foreach (var slot in icons) slot.SetTextures(new[] { background, foreground });
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, icons);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pulse/Build Android APK")]
        public static void AndroidApk() { ConfigureAndroid(); Build("Builds/Android/Pulse.apk"); }

        [MenuItem("Pulse/Build signed Android App Bundle")]
        public static void AndroidBundle()
        {
            string keystore = Environment.GetEnvironmentVariable("PULSE_KEYSTORE_PATH");
            string password = Environment.GetEnvironmentVariable("PULSE_KEYSTORE_PASSWORD");
            string alias = Environment.GetEnvironmentVariable("PULSE_KEY_ALIAS");
            string aliasPassword = Environment.GetEnvironmentVariable("PULSE_KEY_ALIAS_PASSWORD");
            if (!File.Exists(keystore) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPassword))
                throw new InvalidOperationException("Set PULSE_KEYSTORE_PATH, PULSE_KEYSTORE_PASSWORD, PULSE_KEY_ALIAS and PULSE_KEY_ALIAS_PASSWORD for a signed store bundle.");
            ConfigureAndroid();
            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore; PlayerSettings.Android.keystorePass = password;
                PlayerSettings.Android.keyaliasName = alias; PlayerSettings.Android.keyaliasPass = aliasPassword;
                EditorUserBuildSettings.buildAppBundle = true; Build("Builds/Android/Pulse.aab");
            }
            finally
            {
                PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.useCustomKeystore = false; EditorUserBuildSettings.buildAppBundle = false;
            }
        }
        static void Build(string output)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Pulse build failed: " + report.summary.result);
            Debug.Log("[Pulse] Built " + output + " (" + report.summary.totalSize + " bytes)");
        }
    }
}
