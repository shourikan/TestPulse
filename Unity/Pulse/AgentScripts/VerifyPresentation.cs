using System;
using System.Linq;
using Pulse;
using TMPro;
using UnityEditor;
using UnityEngine;

// Editor-only review scenarios. These helpers never enter a player build.
public static class VerifyPresentation
{
    public static string Busy()
    {
        var g = UnityEngine.Object.FindAnyObjectByType<PulseGame>();
        g.StartRun();
        foreach (var a in g.Model.Actors) a.Position = new Vector2(12, 100);
        // Explicitly arranged review field: compare readability, not random-seed difficulty.
        for (int i = 0; i < 24; i++)
            g.Model.Actors[i].Position = new Vector2((i % 6 - 2.5f) * 1.65f + (i % 2 == 0 ? .25f : 0), (i / 6) * 2.5f - 3.8f);
        for (int i = 51; i < 57; i++)
            g.Model.Actors[i].Position = new Vector2((i % 3 - 1) * 2, (i - 50) * 1.2f - 2);
        for (int i = 0; i < 3; i++) { g.Model.Actors[60+i].Position = Vector2.zero; g.Model.Step(.01f, 0); }
        g.ActivatePulse(); g.Model.Actors[50].Position = Vector2.zero; g.Model.Step(.01f, 0);
        g.World.Render(g.Model, .18f); g.UI.Render(.4f); Canvas.ForceUpdateCanvases();
        if (g.World.ActiveParticles <= 0) throw new Exception("No feedback particles");
        EditorApplication.isPaused = true;
        return "Busy review: " + g.World.ActiveParticles + " pooled particles, separated floating labels, pulse active.";
    }

    public static string Audit()
    {
        var g = UnityEngine.Object.FindAnyObjectByType<PulseGame>();
        var sprites = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        if (sprites.Any(r => r.sharedMaterial == null || r.sharedMaterial.name == "Default-Material")) throw new Exception("Default/missing sprite material");
        if (UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length != 1) throw new Exception("EventSystem count");
        if (UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include).Any(t => t.font == null)) throw new Exception("Missing font");
        int spriteCount = sprites.Length;
        for (int i = 0; i < 10; i++) g.World.Feedback(g.Model, FeedbackKind.Gold, Vector2.zero, 100);
        if (UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Length != spriteCount) throw new Exception("Effect pool grew");
        g.World.ResetRun();
        float original = PulseSettings.Music; PulseSettings.Music = 0; g.RefreshAudio();
        if (g.Audio.EffectiveMusicVolume != 0) throw new Exception("Music gain");
        PulseSettings.Music = original; g.RefreshAudio();
        return "Material/font/event-system/gain/pooling audit passed; " + sprites.Length + " recycled sprites after ten bursts.";
    }

    public static string ResetReviewScores()
    {
        // This new project's first title capture had zero runs and best zero, before review runs.
        PlayerPrefs.DeleteKey("pulse.v1.best64");
        PlayerPrefs.SetInt("pulse.v1.best", 0); PlayerPrefs.SetInt("pulse.v1.runs", 0); PlayerPrefs.Save();
        PulseSettings.Load();
        var g = UnityEngine.Object.FindAnyObjectByType<PulseGame>();
        if (g != null) { g.Home(); g.UI.Render(.5f); }
        return "Restored the pre-review zero score and run count.";
    }
}
