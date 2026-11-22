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
        public static bool TiltAvailable => AttitudeSensor.current != null || GravitySensor.current != null || Accelerometer.current != null;
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
        AttitudeSensor tiltAttitude;
        double tiltSampleAfter;
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
            InputSystem.onDeviceChange += OnDeviceChange;
            Calibrate();
        }
        public void Calibrate(bool keepManualPose = false)
        {
            SteeringMode mode = EffectiveMode;
            AttitudeSensor attitude = mode == SteeringMode.Tilt ? FindTiltAttitude() : null;
            bool gravityFallback = mode == SteeringMode.Tilt && attitude == null;
            ConfigureSensor(GravitySensor.current, gravityFallback);
            ConfigureSensor(Accelerometer.current, gravityFallback && GravitySensor.current == null);
            ConfigureSensor(Gyroscope.current, mode == SteeringMode.Twist);
            bool attitudeChanged = attitude != tiltAttitude;
            if (attitudeChanged)
            {
                ConfigureSensor(tiltAttitude, false);
                tiltAttitude = attitude;
                if (attitude != null) tiltSampleAfter = attitude.lastUpdateTime;
            }
            ConfigureSensor(tiltAttitude, true);
            if (keepManualPose && !attitudeChanged) ResumeGesture();
            else ResetGesture();
            if (MotionAvailable)
            {
                string sensor = mode == SteeringMode.Twist ? "gyroscope" : tiltAttitude != null ? "attitude" :
                    GravitySensor.current != null ? "gravity" : "accelerometer";
                Debug.Log("[Pulse] Motion input: " + mode + ", sensor=" + sensor + ", requested sampling=60Hz");
            }
        }

        static AttitudeSensor FindTiltAttitude()
        {
#if UNITY_ANDROID
            // Game rotation avoids magnetic heading corrections during steering.
            return (AttitudeSensor)InputSystem.GetDevice<UnityEngine.InputSystem.Android.AndroidGameRotationVector>()
                ?? (AttitudeSensor)InputSystem.GetDevice<UnityEngine.InputSystem.Android.AndroidRotationVector>()
                ?? AttitudeSensor.current;
#else
            return AttitudeSensor.current;
#endif
        }

        static void ConfigureSensor(Sensor sensor, bool useSensor)
        {
            if (sensor == null || !sensor.added) return;
            if (useSensor)
            {
                InputSystem.EnableDevice(sensor);
                sensor.samplingFrequency = 60;
            }
            else InputSystem.DisableDevice(sensor);
        }

        void OnDisable()
        {
            TrackedTouch.onFingerDown -= BeginTap;
            TrackedTouch.onFingerMove -= MoveTap;
            TrackedTouch.onFingerUp -= EndTap;
            InputSystem.onDeviceChange -= OnDeviceChange;
            EnhancedTouchSupport.Disable();
            ClearTaps();
            ConfigureSensor(tiltAttitude, false);
            tiltAttitude = null;
            ConfigureSensor(Accelerometer.current, false);
            ConfigureSensor(GravitySensor.current, false);
            ConfigureSensor(Gyroscope.current, false);
        }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device != tiltAttitude || change is not (InputDeviceChange.SoftReset or InputDeviceChange.HardReset)) return;
            tiltSampleAfter = device.lastUpdateTime;
            ResetGesture();
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
                ReadTilt();
                manualPose = motionControl.Calibrated;
            }
            else manualPose = EffectiveMode == SteeringMode.Twist;
            return manualPose;
        }

        float ReadTilt()
        {
            if (tiltAttitude != null)
            {
                // The default value can look like a real pose until a sample arrives.
                return tiltAttitude.lastUpdateTime > tiltSampleAfter
                    ? motionControl.ReadTilt(tiltAttitude.attitude.ReadValue(), PulseSettings.Sensitivity)
                    : 0;
            }

            Vector3 gravity = GravitySensor.current != null
                ? GravitySensor.current.gravity.ReadValue()
                : Accelerometer.current?.acceleration.ReadValue() ?? Vector3.zero;
            return motionControl.ReadTilt(gravity, PulseSettings.Sensitivity);
        }

        public float Read(float dt)
        {
            float target;
            switch (EffectiveMode)
            {
                case SteeringMode.Twist:
                    target = motionControl.ReadTwist(Gyroscope.current.angularVelocity.ReadValue().z, dt, PulseSettings.Sensitivity);
                    break;
                case SteeringMode.Tilt:
                    target = ReadTilt();
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
