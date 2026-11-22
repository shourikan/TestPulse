using UnityEngine;
using UnityEngine.InputSystem;

namespace Pulse
{
    public sealed class PulseGame : MonoBehaviour
    {
        public PulseSimulation Model { get; private set; }
        public PulseInput Input { get; private set; }
        public PulseAudio Audio { get; private set; }
        public PulseWorldView World { get; private set; }
        public PulseUI UI { get; private set; }
        public PulseOnline Online { get; private set; }
        public bool NewRecord { get; private set; }
        Camera mainCamera;
        int lastWidth, lastHeight;
        float hapticTime;
        float worldRoll;
        bool twistView;

        void Awake()
        {
            PulseSettings.Load();
            Online = new PulseOnline();
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.Portrait;
            mainCamera = Camera.main;
            Input = gameObject.AddComponent<PulseInput>(); Audio = gameObject.AddComponent<PulseAudio>();
            Model = new PulseSimulation();
            Model.Feedback += Feedback;
            World = new GameObject("World").AddComponent<PulseWorldView>(); World.Initialize();
            UI = new GameObject("Interface", typeof(RectTransform)).AddComponent<PulseUI>(); UI.Initialize(this, mainCamera);
            Input.CanBeginDrag = UI.CanBeginDrag;
            Input.CanActivatePulse = () => Model.PulseReady && !UI.SettingsOpen;
            FitCamera(); RefreshAudio();
            Online.SyncScore();
            Debug.Log("[Pulse] Ready: title, 64 recycled actors, touch/motion input, local saves.");
        }

        void FitCamera()
        {
            if (mainCamera == null) return;
            mainCamera.orthographicSize = 5.4f / Mathf.Max(.3f, mainCamera.aspect);
            FitPlayfield();
            lastWidth = Screen.width; lastHeight = Screen.height;
        }

        void SetTwistView(bool enabled)
        {
            twistView = enabled;
            worldRoll = 0;
            FitPlayfield();
        }

        void FitPlayfield()
        {
            if (mainCamera == null) return;
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            // A rotated corner can reach beyond either edge of the upright view.
            float radius = new Vector2(halfWidth, halfHeight).magnitude;
            Model.HalfHeight = twistView ? radius : halfHeight;
            Model.HalfWidth = twistView ? radius : halfWidth;
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) FitCamera();
            float dt = Time.unscaledDeltaTime;
            Online.Update();
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.escapeKey.wasPressedThisFrame)
                {
                    if (UI.LeaderboardOpen) UI.CloseLeaderboard();
                    else if (UI.SettingsOpen) UI.CloseSettings();
                    else if (Model.State == RunState.Playing || Model.State == RunState.Paused) TogglePause();
                    else if (Model.State == RunState.Ended) Home();
                }
                if (keys.spaceKey.wasPressedThisFrame && !UI.LeaderboardOpen)
                {
                    if (!UI.SettingsOpen && (Model.State == RunState.Title || Model.State == RunState.Ended)) StartRun();
                    else ActivatePulse();
                }
            }
            if (Model.State == RunState.Playing)
            {
                if (Input.TakePulseGesture()) ActivatePulse();
                float steering = Input.Read(dt);
                worldRoll = twistView ? Input.TwistDegrees : 0;
                Model.Step(dt, steering);
                if (Model.State == RunState.Playing) Online.RecordPlayTime(Model.Elapsed);
            }
            else if (Model.State == RunState.Ended && !UI.SettingsOpen && !PulseSettings.ReducedMotion)
                Model.DriftAfterDeath(dt);
            World.Render(Model, dt, UI.SettingsOpen, worldRoll, twistView); UI.Render(dt);
        }
        public void StartRun()
        {
            AbandonRun();
            NewRecord = false;
            Input.Calibrate(keepManualPose: true); World.ResetRun();
            SetTwistView(Input.EffectiveMode == SteeringMode.Twist);
            bool gentleOpening = !PulseSettings.HasLearned(Input.EffectiveMode);
            Model.Start(System.Environment.TickCount, PulseSettings.Best, gentleOpening);
            Online.RunStarted(Input.EffectiveMode);
            Time.timeScale = 1; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            UI.ResetRun(); Audio.Refresh(Model.State, newRun: true);
            Debug.Log("[Pulse] Run started: steering=" + Input.EffectiveMode + ", gentle opening=" + gentleOpening + ", max steering angle=" + PulseSimulation.MaxSteeringAngle);
        }
        public void TogglePause()
        {
            if (Model.State == RunState.Playing) Model.Pause();
            else if (Model.State == RunState.Paused) Model.Resume();
            else return;
            Time.timeScale = Model.State == RunState.Paused ? 0 : 1;
            Screen.sleepTimeout = Model.State == RunState.Playing ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            if (Model.State == RunState.Playing)
            {
                Input.ResumeGesture();
                SetTwistView(Input.EffectiveMode == SteeringMode.Twist);
            }
            else
            {
                Online.RecordPlayTime(Model.Elapsed, flush: true);
                Input.ResetGesture();
            }
            Online.Track(Model.State == RunState.Playing ? "run_resumed" : "run_paused");
            UI.ShowState(); RefreshAudio();
            Debug.Log("[Pulse] State=" + Model.State);
        }
        public void Home()
        {
            AbandonRun();
            Model.Home(); Time.timeScale = 1; Input.ResetGesture();
            SetTwistView(false);
            World.ResetRun();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            UI.ShowState(); RefreshAudio();
        }

        void AbandonRun()
        {
            if (Model.State != RunState.Playing && Model.State != RunState.Paused) return;
            Online.RecordPlayTime(Model.Elapsed, flush: true);
            Online.Track("run_abandoned");
        }

        public void ActivatePulse() { if (Model.ActivatePulse()) Debug.Log("[Pulse] Pulse activated for 10 seconds."); }
        public void RefreshAudio() { Audio.Refresh(Model.State); }
        void Feedback(FeedbackKind kind, Vector2 position, long value)
        {
            World.Feedback(Model, kind, position, value); Audio.Feedback(kind);
            if (kind == FeedbackKind.Death)
            {
                Input.ResetGesture();
                NewRecord = PulseSettings.SaveRun(value);
                Online.RunCompleted(value, Model.Elapsed, NewRecord);
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                UI.ShowState(); RefreshAudio();
                Debug.Log("[Pulse] Run ended. score=" + value + " best=" + PulseSettings.Best);
            }
            if (kind == FeedbackKind.Pulse) Online.Track("pulse_activated");
            bool strongFeedback = kind == FeedbackKind.Death || kind == FeedbackKind.Pulse || kind == FeedbackKind.ShieldBreak;
            if (strongFeedback && PulseSettings.Haptics && Application.isMobilePlatform && Time.unscaledTime > hapticTime)
            {
                Handheld.Vibrate(); hapticTime = Time.unscaledTime + .4f;
            }
        }
        void OnApplicationPause(bool paused)
        {
            if (Model == null) return;
            if (paused)
            {
                if (Model.State == RunState.Playing) TogglePause();
                PulseSettings.Save();
            }
            else Online.SyncScore();
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && Application.isMobilePlatform && Model != null && Model.State == RunState.Playing) TogglePause();
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (Model == null) return;
            if (Model.State == RunState.Playing)
                Online.RecordPlayTime(Model.Elapsed, flush: true);
            Model.Feedback -= Feedback;
        }
    }
}
