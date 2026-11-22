using UnityEngine;

namespace Pulse
{
    // A bounded motion command, independent of its visual smoothing. At an end
    // stop, excess phone movement shifts the reference instead of storing travel
    // that the player would have to unwind before steering back.
    public sealed class PulseMotionControl
    {
        public const float TiltRangeDegrees = 30;
        public bool Calibrated { get; private set; }
        float neutralDegrees, command;

        public void Reset() { Calibrated = false; neutralDegrees = command = 0; }

        public float ReadTilt(Vector3 gravity, float sensitivity)
        {
            if (gravity.sqrMagnitude < .01f) return Shape(command);
            float angle = Mathf.Atan2(gravity.x, Mathf.Sqrt(gravity.y * gravity.y + gravity.z * gravity.z)) * Mathf.Rad2Deg;
            if (!Calibrated) { neutralDegrees = angle; Calibrated = true; return 0; }
            float range = TiltRangeDegrees / Mathf.Clamp(sensitivity, .5f, 2);
            float tilt = Mathf.Clamp(angle - neutralDegrees, -range, range);
            neutralDegrees = angle - tilt;
            command = tilt / range;
            return Shape(command);
        }

        public float ReadTwist(float radiansPerSecond, float dt, float sensitivity)
        {
            float range = TiltRangeDegrees / Mathf.Clamp(sensitivity, .5f, 2);
            command = Mathf.Clamp(command - radiansPerSecond * Mathf.Rad2Deg * Mathf.Clamp(dt, 0, .1f) / range, -1, 1);
            return Shape(command);
        }

        static float Shape(float value)
        {
            const float deadzone = .015f;
            float amount = Mathf.Clamp01((Mathf.Abs(value) - deadzone) / (1 - deadzone));
            return Mathf.Sign(value) * amount * (.7f + .3f * amount);
        }

        public static float Smooth(float current, float target, float dt)
        {
            target = Mathf.Clamp(target, -1, 1);
            // Light filtering outward; faster release and reversals. This is the
            // only smoothing stage, so direction changes do not stack up delays.
            bool returning = current * target < 0 || Mathf.Abs(target) < Mathf.Abs(current);
            return Mathf.Lerp(current, target, 1 - Mathf.Exp(-(returning ? 36 : 24) * Mathf.Clamp(dt, 0, .1f)));
        }
    }
}
