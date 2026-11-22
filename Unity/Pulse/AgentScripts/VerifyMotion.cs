using System;
using Pulse;
using UnityEngine;
using UnityEngine.InputSystem;

// Exercises the real input -> simulation path with deterministic sensor samples.
// This file lives outside Assets and is excluded from player builds.
public static class VerifyMotion
{
    static void Sample(PulseGame game, GravitySensor sensor, float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        InputSystem.QueueDeltaStateEvent(sensor.gravity, new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0));
        InputSystem.Update();
        game.Model.Step(1f / 60, game.Input.Read(1f / 60));
        game.World.Render(game.Model, 1f / 60);
    }

    public static string Run()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<PulseGame>();
        if (game == null) throw new Exception("PulseGame is missing");
        var oldMode = PulseSettings.Controls;
        float oldSensitivity = PulseSettings.Sensitivity;
        var sensor = InputSystem.AddDevice<GravitySensor>();
        try
        {
            game.enabled = false;
            PulseSettings.Controls = SteeringMode.Tilt; PulseSettings.Sensitivity = 2;
            game.StartRun();
            foreach (var actor in game.Model.Actors) actor.Position = new Vector2(12, 100);
            Sample(game, sensor, 0);
            for (int i = 0; i < 60; i++) Sample(game, sensor, 70);
            float atLimit = game.Model.Steering * PulseSimulation.MaxSteeringAngle;
            Sample(game, sensor, 69);
            float afterReturn = game.Model.Steering * PulseSimulation.MaxSteeringAngle;
            if (atLimit < 59.9f || afterReturn >= atLimit - .1f)
                throw new Exception("Over-tilt return did not respond immediately");
            for (int i = 0; i < 3; i++) Sample(game, sensor, -69);
            float reversal = game.Model.Steering * PulseSimulation.MaxSteeringAngle;
            if (reversal >= 0) throw new Exception("Reversal did not cross center within 50ms");
            Debug.Log("[Pulse motion review] full=" + atLimit + ", first return=" + afterReturn + ", reversal at 50ms=" + reversal);
            return "Full=" + atLimit + "; first returning sample=" + afterReturn + "; reversal at 50ms=" + reversal;
        }
        finally
        {
            PulseSettings.Controls = oldMode; PulseSettings.Sensitivity = oldSensitivity;
            InputSystem.RemoveDevice(sensor);
            game.Input.Calibrate();
        }
    }
}
