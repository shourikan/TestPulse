using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using TrackedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Pulse
{
    public sealed class PulseInput : MonoBehaviour
    {
        public float Steering { get; private set; }
        public Func<Vector2, bool> CanBeginDrag { private get; set; }
        public Func<bool> CanActivatePulse { private get; set; }
        public static bool TiltAvailable => GravitySensor.current != null || Accelerometer.current != null;
        public static bool TwistAvailable => Gyroscope.current != null;
        public SteeringMode EffectiveMode
        {
            get
            {
                if (PulseSettings.Controls == SteeringMode.Touch) return SteeringMode.Touch;
                if (PulseSettings.Controls == SteeringMode.Twist && TwistAvailable) return SteeringMode.Twist;
                return TiltAvailable ? SteeringMode.Tilt : SteeringMode.Touch;
            }
        }
        public bool MotionAvailable => EffectiveMode != SteeringMode.Touch;
        readonly PulseMotionControl motionControl = new PulseMotionControl();
        float origin, drag;
        bool dragging, manualPose;
        int touchId = -1;
        const double MaxTapHold = .22, MaxTapGap = .35;
        Finger tapFinger;
        Vector2 tapOrigin, firstTap;
        double tapStarted, firstTapEnded = double.NegativeInfinity;
        bool pulseGesture;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            TrackedTouch.onFingerDown += BeginTap;
            TrackedTouch.onFingerMove += MoveTap;
            TrackedTouch.onFingerUp += EndTap;
            Calibrate();
        }
        public void Calibrate(bool keepManualPose = false)
        {
            bool motion = PulseSettings.Controls != SteeringMode.Touch;
            if (GravitySensor.current != null)
            {
                if (motion)
                {
                    InputSystem.EnableDevice(GravitySensor.current);
                    GravitySensor.current.samplingFrequency = 60;
                }
                else InputSystem.DisableDevice(GravitySensor.current);
            }
            if (Accelerometer.current != null)
            {
                if (motion && GravitySensor.current == null)
                {
                    InputSystem.EnableDevice(Accelerometer.current);
                    Accelerometer.current.samplingFrequency = 60;
                }
                else InputSystem.DisableDevice(Accelerometer.current);
            }
            if (Gyroscope.current != null)
            {
                if (PulseSettings.Controls == SteeringMode.Twist)
                {
                    InputSystem.EnableDevice(Gyroscope.current);
                    Gyroscope.current.samplingFrequency = 60;
                }
                else InputSystem.DisableDevice(Gyroscope.current);
            }
            if (keepManualPose) ResumeGesture();
            else ResetGesture();
            if (motion && MotionAvailable)
                Debug.Log("[Pulse] Motion input: " + PulseSettings.Controls + ", sensor=" + (GravitySensor.current != null ? "gravity" : "accelerometer/gyro") + ", requested sampling=60Hz");
        }
        void OnDisable()
        {
            TrackedTouch.onFingerDown -= BeginTap;
            TrackedTouch.onFingerMove -= MoveTap;
            TrackedTouch.onFingerUp -= EndTap;
            EnhancedTouchSupport.Disable();
            ClearTaps();
            if (Accelerometer.current != null) InputSystem.DisableDevice(Accelerometer.current);
            if (GravitySensor.current != null) InputSystem.DisableDevice(GravitySensor.current);
            if (Gyroscope.current != null) InputSystem.DisableDevice(Gyroscope.current);
        }
        public void ResetGesture()
        {
            ClearTaps();
            dragging = manualPose = false;
            touchId = -1;
            drag = Steering = 0;
            motionControl.Reset();
        }

        public void ResumeGesture()
        {
            if (manualPose) manualPose = false;
            else ResetGesture();
        }

        public bool RecenterMotion()
        {
            ResetGesture();
            if (EffectiveMode == SteeringMode.Tilt)
            {
                motionControl.ReadTilt(ReadGravity(), PulseSettings.Sensitivity);
                manualPose = motionControl.Calibrated;
            }
            else manualPose = EffectiveMode == SteeringMode.Twist;
            return manualPose;
        }

        static Vector3 ReadGravity() => GravitySensor.current != null
            ? GravitySensor.current.gravity.ReadValue()
            : Accelerometer.current.acceleration.ReadValue();

        public float Read(float dt)
        {
            float target;
            switch (EffectiveMode)
            {
                case SteeringMode.Twist:
                    target = motionControl.ReadTwist(Gyroscope.current.angularVelocity.ReadValue().z, dt, PulseSettings.Sensitivity);
                    break;
                case SteeringMode.Tilt:
                    target = motionControl.ReadTilt(ReadGravity(), PulseSettings.Sensitivity);
                    break;
                default:
                    target = ReadDrag();
                    break;
            }

            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.leftArrowKey.isPressed || keys.aKey.isPressed) target = -1;
                if (keys.rightArrowKey.isPressed || keys.dKey.isPressed) target = 1;
            }
            Steering = PulseMotionControl.Smooth(Steering, target, dt);
            return Steering;
        }

        void BeginTap(Finger finger)
        {
            if (pulseGesture || tapFinger != null || CanActivatePulse?.Invoke() != true) return;
            var touch = finger.currentTouch;
            if (!AllowsDragStart(touch.screenPosition)) return;
            tapFinger = finger;
            tapOrigin = touch.screenPosition;
            tapStarted = touch.time;
        }

        void MoveTap(Finger finger)
        {
            if (finger == tapFinger && MovedTooFar(finger.currentTouch.screenPosition)) ClearTaps();
        }

        void EndTap(Finger finger)
        {
            if (finger != tapFinger) return;
            var touch = finger.currentTouch;
            tapFinger = null;
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended || touch.time - tapStarted > MaxTapHold
                || MovedTooFar(touch.screenPosition) || !AllowsDragStart(touch.screenPosition) || CanActivatePulse?.Invoke() != true)
            {
                ClearTaps();
                return;
            }

            float pairSlop = Screen.width * .08f;
            if (tapStarted - firstTapEnded <= MaxTapGap && (tapOrigin - firstTap).sqrMagnitude <= pairSlop * pairSlop)
            {
                ClearTaps();
                pulseGesture = true;
                return;
            }
            firstTap = touch.screenPosition;
            firstTapEnded = touch.time;
        }

        bool MovedTooFar(Vector2 position)
        {
            float tapSlop = Screen.width * .02f;
            return (position - tapOrigin).sqrMagnitude > tapSlop * tapSlop;
        }

        void ClearTaps()
        {
            tapFinger = null;
            firstTapEnded = double.NegativeInfinity;
            pulseGesture = false;
        }

        public bool TakePulseGesture()
        {
            bool recognized = pulseGesture;
            pulseGesture = false;
            return recognized;
        }

        float ReadDrag()
        {
            var screen = Touchscreen.current;
            if (screen != null)
            {
                // Keep the first steering touch while another finger taps for Pulse or pause.
                if (dragging)
                {
                    foreach (var touch in screen.touches)
                    {
                        if (touch.press.isPressed && touch.touchId.ReadValue() == touchId)
                            return UpdateDrag(touch.position.ReadValue().x);
                    }
                }
                dragging = false;
                touchId = -1;
                foreach (var touch in screen.touches)
                {
                    if (!touch.press.isPressed || !touch.press.wasPressedThisFrame) continue;
                    Vector2 position = touch.position.ReadValue();
                    if (!AllowsDragStart(position)) continue;
                    touchId = touch.touchId.ReadValue();
                    return UpdateDrag(position.x);
                }
            }
            else if (Mouse.current is { } mouse && mouse.leftButton.isPressed)
            {
                Vector2 position = mouse.position.ReadValue();
                if (dragging || mouse.leftButton.wasPressedThisFrame && AllowsDragStart(position))
                    return UpdateDrag(position.x);
            }
            dragging = false;
            return 0;
        }

        bool AllowsDragStart(Vector2 position) =>
            CanBeginDrag?.Invoke(position) ?? (position.y >= Screen.height * .18f && position.y <= Screen.height * .85f);

        float UpdateDrag(float x)
        {
            if (!dragging)
            {
                origin = x;
                dragging = true;
            }
            drag = Mathf.Clamp((x - origin) / (Screen.width * .2f), -1, 1);
            return Mathf.Clamp(drag * PulseSettings.Sensitivity, -1, 1);
        }
    }
}
