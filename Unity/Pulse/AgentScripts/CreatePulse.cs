using System;
using System.IO;
using Pulse;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CreatePulse
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory("Assets/Pulse/Resources/Visuals");
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
        Visuals();
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) throw new Exception("URP 2D unlit shader missing");
        const string matPath = "Assets/Pulse/Resources/Visuals/Unlit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
        ConfigureAudio();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 9.6f;
        camera.transform.position = new Vector3(0, 0, -10); camera.backgroundColor = PulsePalette.Void;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.allowHDR = false; camera.allowMSAA = false;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        new GameObject("Pulse").AddComponent<PulseGame>();
        RenderSettings.skybox = null;
        Directory.CreateDirectory("Assets/Pulse/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Pulse/Scenes/Pulse.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Pulse/Scenes/Pulse.unity", true) };
        AssetDatabase.SaveAssets();
        return "Created Pulse scene, six antialiased sprites, unlit material, TMP essentials and original audio import settings.";
    }

    public static string Visuals()
    {
        Sprite("Circle", (x, y) => 1 - Smooth(.95f, 1, Mathf.Sqrt(x*x+y*y)));
        Sprite("Ring", (x, y) => { float r = Mathf.Sqrt(x*x+y*y); return Smooth(.86f, .9f, r) * (1 - Smooth(.94f, .99f, r)); });
        Sprite("Glow", (x, y) => Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(x*x+y*y)), 3));
        Sprite("Diamond", (x, y) => 1 - Smooth(.9f, 1, Mathf.Abs(x) + Mathf.Abs(y)));
        RoleVisuals();
        AssetDatabase.SaveAssets();
        return "Updated six crisp antialiased sprites";
    }

    public static void RoleVisuals()
    {
        Sprite("Spike", (x, y) =>
        {
            x = Mathf.Abs(x);
            y = Mathf.Abs(y);
            return 1 - Smooth(.91f, 1, Mathf.Max(x, y) + 2.2f * Mathf.Min(x, y));
        });
        Sprite("Shield", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float circle = Smooth(.72f, .79f, r) * (1 - Smooth(.94f, 1, r));
            x = Mathf.Abs(x);
            y = Mathf.Abs(y);
            float cross = (1 - Smooth(.09f, .15f, Mathf.Min(x, y)))
                * (1 - Smooth(.38f, .46f, Mathf.Max(x, y)));
            return Mathf.Max(circle, cross);
        });
    }

    static float Smooth(float a, float b, float value) { float t = Mathf.InverseLerp(a, b, value); return t*t*(3-2*t); }

    static void Sprite(string name, Func<float, float, float> alpha)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            pixels[y*size+x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha((x+.5f)/size*2-1, (y+.5f)/size*2-1))*255));
        texture.SetPixels32(pixels); texture.Apply();
        string path = "Assets/Pulse/Resources/Visuals/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
    }
    public static string ConfigureAudio(bool musicOnly = false)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Pulse/Resources/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            bool music = Path.GetFileNameWithoutExtension(path).StartsWith("GameBg-", StringComparison.Ordinal);
            if (musicOnly && !music) continue;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = music ? .50f : .65f;
            settings.preloadAudioData = !music;
            importer.defaultSampleSettings = settings; importer.forceToMono = !music;
            importer.SaveAndReimport();
        }
        return musicOnly ? "Configured stereo streaming for the background themes." : "Configured music and sound effect imports.";
    }
}
