using System;
using Pulse;
using UnityEditor;
using UnityEngine;

// Deterministic visual checks only; excluded from the Android player.
public static class VerifySteering
{
    public static string Show(int direction = 1, bool reverse = false)
    {
        var game = UnityEngine.Object.FindAnyObjectByType<PulseGame>();
        if (game == null) throw new Exception("PulseGame is missing");
        game.enabled = false;
        game.StartRun();
        foreach (var actor in game.Model.Actors) actor.Position = new Vector2(12, 100);
        for (int i = 0; i < 90; i++)
        {
            game.Model.Step(1f / 60, direction);
            game.World.Render(game.Model, 1f / 60);
        }
        if (reverse)
            for (int i = 0; i < 12; i++)
            {
                game.Model.Step(1f / 60, -direction);
                game.World.Render(game.Model, 1f / 60);
            }
        for (int i = 0; i < 12; i++)
            game.Model.Actors[i].Position = new Vector2((i % 4 - 1.5f) * 2, 2 + (i / 4) * 2.2f);
        game.World.Render(game.Model, 0);
        game.UI.Render(.4f);
        Canvas.ForceUpdateCanvases();
        float angle = Vector2.Angle(Vector2.down, game.Model.ScrollDirection);
        if (angle > PulseSimulation.MaxSteeringAngle + .01f) throw new Exception("Steering exceeded its angle limit");
        Debug.Log("[Pulse steering review] angle=" + angle + ", scroll=" + game.Model.ScrollDirection + ", reversal=" + reverse);
        return "Steering review: " + angle + " degrees; reversal=" + reverse;
    }
}
