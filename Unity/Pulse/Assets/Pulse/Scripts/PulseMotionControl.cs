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
        public float TwistDegrees { get; private set; }
        float neutralDegrees, command;
        Quaternion previousAttitude;

        public void Reset() { Calibrated = false; neutralDegrees = command = TwistDegrees = 0; }

        public float ReadTilt(Quaternion attitude, float sensitivity)
        {
            Quaternion delta = ReadAttitudeDelta(attitude);
            command = Mathf.Clamp(command + RotationDegrees(delta.y, delta.w) / InputRange(sensitivity), -1, 1);
            return Shape(command);
        }

        public float ReadTwist(Quaternion attitude, float sensitivity)
        {
            Quaternion delta = ReadAttitudeDelta(attitude);
            return AddTwist(-RotationDegrees(delta.z, delta.w), sensitivity);
        }

        Quaternion ReadAttitudeDelta(Quaternion attitude)
        {
            if (Quaternion.Dot(attitude, attitude) < .01f) return Quaternion.identity;
            if (!Calibrated)
            {
                previousAttitude = attitude;
                Calibrated = true;
                return Quaternion.identity;
            }

            if (previousAttitude.Equals(attitude)) return Quaternion.identity;

            // Measure the change in the phone's local frame.
            Quaternion delta = Quaternion.Inverse(previousAttitude) * attitude;
            previousAttitude = attitude;
            return delta;
        }

        static float RotationDegrees(float axisComponent, float scalar)
        {
            // A half turn around a perpendicular axis has no measurable twist.
            if (axisComponent * axisComponent + scalar * scalar < .000001f) return 0;
            return Mathf.DeltaAngle(0, 2 * Mathf.Atan2(axisComponent, scalar) * Mathf.Rad2Deg);
        }

        public float ReadTilt(Vector3 gravity, float sensitivity)
        {
            if (gravity.sqrMagnitude < .01f) return Shape(command);
            float angle = Mathf.Atan2(gravity.x, Mathf.Sqrt(gravity.y * gravity.y + gravity.z * gravity.z)) * Mathf.Rad2Deg;
            if (!Calibrated) { neutralDegrees = angle; Calibrated = true; return 0; }
            float range = InputRange(sensitivity);
            float tilt = Mathf.Clamp(angle - neutralDegrees, -range, range);
            neutralDegrees = angle - tilt;
            command = tilt / range;
            return Shape(command);
        }

        public float ReadTwist(float radiansPerSecond, float dt, float sensitivity) =>
            AddTwist(-radiansPerSecond * Mathf.Rad2Deg * Mathf.Clamp(dt, 0, .1f), sensitivity);

        static float InputRange(float sensitivity) => TiltRangeDegrees / Mathf.Clamp(sensitivity, .5f, 2);

        float AddTwist(float degrees, float sensitivity)
        {
            float range = InputRange(sensitivity);
            // The view uses physical degrees while the simulation uses shaped steering.
            TwistDegrees = Mathf.Clamp(TwistDegrees + degrees, -range, range);
            return Shape(TwistDegrees / range);
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
