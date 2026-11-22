using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Pulse
{
    public sealed class PulseUI : MonoBehaviour
    {
        const int LeaderboardTimeoutMs = 12000;
        const float FirstPlaceDuration = .8f;
        const int ConfettiPerSide = 5;

        PulseGame game;
        Camera uiCamera;
        RectTransform safe, hudHeader;
        GameObject hud, guide, calibrate;
        CanvasGroup titleGroup, pauseGroup, endGroup, settingsGroup, leaderboardGroup;
        TextMeshProUGUI scoreText, hudBest, titleBest;
        TextMeshProUGUI endScore, endRecord, endBest, endRank;
        RectTransform rankBadge;
        CanvasGroup rankConfetti;
        readonly RectTransform[] confettiPieces = new RectTransform[ConfettiPerSide * 2];
        TextMeshProUGUI guideTitle, modeDetail, hapticsValue, motionValue;
        TextMeshProUGUI leaderboardStatus;
        UnityEngine.UI.Button leaderboardRefresh;
        readonly RectTransform[] leaderboardRows = new RectTransform[PulseOnline.LeaderboardSize];
        readonly TextMeshProUGUI[] leaderboardNames = new TextMeshProUGUI[PulseOnline.LeaderboardSize];
        readonly TextMeshProUGUI[] leaderboardScores = new TextMeshProUGUI[PulseOnline.LeaderboardSize];
        UnityEngine.UI.Image hapticsTrack, motionTrack;
        RectTransform hapticsThumb, motionThumb;
        readonly UnityEngine.UI.Image[] controlButtons = new UnityEngine.UI.Image[3];
        readonly TextMeshProUGUI[] controlLabels = new TextMeshProUGUI[3];
        UnityEngine.UI.Image flash;
        Sprite circle, rounded, spike, diamond, shield;
        Texture2D roundedTexture;
        double shownScore;
        float flashAlpha;
        long lastScore;
        bool showingSettings;
        int leaderboardView, runRankRequest;
        float firstPlaceTime = FirstPlaceDuration;
        public bool SettingsOpen => showingSettings;
        public bool LeaderboardOpen { get; private set; }

        public void Initialize(PulseGame owner, Camera camera)
        {
            game = owner;
            uiCamera = camera;
            circle = Resources.Load<Sprite>("Visuals/Circle");
            spike = Resources.Load<Sprite>("Visuals/Spike");
            diamond = Resources.Load<Sprite>("Visuals/Diamond");
            shield = Resources.Load<Sprite>("Visuals/Shield");
            rounded = CreateRoundedSprite();
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 8;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
            safe = Rect(transform, "Safe area", Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<PulseSafeArea>();
            BuildTitle();
            BuildHud();
            BuildPause();
            BuildEnd();
            BuildSettings();
            BuildLeaderboard();
            flash = Fill(transform, "Transition", PulsePalette.Alpha(PulsePalette.Void, 0));
            ShowState();
        }

        void BuildTitle()
        {
            var title = Panel("Title", false, out titleGroup);
            var p = title.transform;
            var wordmark = Resources.Load<Sprite>("Visuals/BreakthroughTitle");
            if (wordmark != null)
            {
                var mark = Rect(p, "Breakthrough title", new Vector2(.073f, .776f), new Vector2(.927f, .94f));
                Icon(mark, wordmark, Color.white).preserveAspect = true;
            }
            else
            {
                Label(p, "BREAKTHROUGH", .5f, .858f, 104, PulsePalette.Ink, 970, 160, bold: true);
            }
            Label(p, $"Bank {PulseSimulation.ShieldsToActivate} shields. Double-tap for Pulse.", .5f, .751f, 36, PulsePalette.Muted, 1000, 84);
            var best = At(p, "Personal best card", .5f, .337f, 892, 126);
            Surface(best, PulsePalette.Panel);
            Label(best, "Personal best", .285f, .5f, 36, PulsePalette.Muted, 390, 84, TextAlignmentOptions.Left);
            titleBest = Label(best, "", .745f, .5f, 51, PulsePalette.Gold, 345, 95, TextAlignmentOptions.Right, true);
            FitNumber(titleBest, 20);
            Button(p, "Play", .5f, .235f, 892, 156, PulsePalette.Mint, game.StartRun, true);
            Button(p, "Settings", .286f, .139f, 430, 150, PulsePalette.Panel, OpenSettings);
            Button(p, "Leaderboard", .714f, .139f, 430, 150, PulsePalette.Panel, OpenLeaderboard);
            Legend(p);
        }

        void Legend(Transform parent)
        {
            LegendItem(.22f, diamond, PulsePalette.Gold, "Collect");
            LegendItem(.5f, spike, PulsePalette.Violet, "Avoid");
            LegendItem(.78f, shield, PulsePalette.Blue, "Shield");

            void LegendItem(float x, Sprite sprite, Color tint, string text)
            {
                Icon(At(parent, text + " icon", x, .067f, 38, 38), sprite, tint);
                Label(parent, text, x, .035f, 28, PulsePalette.Muted, 260, 52);
            }
        }

        void BuildHud()
        {
            hud = Panel("HUD", false, out _);
            var p = hud.transform;
            hudHeader = At(p, "Score and pause", .5f, 1, 974, 198);
            hudHeader.anchoredPosition = new Vector2(0, -124);
            Surface(hudHeader, PulsePalette.Alpha(PulsePalette.Panel, .93f));
            Label(hudHeader, "Score", .225f, .744f, 28, PulsePalette.Muted, 340, 52, TextAlignmentOptions.Left);
            scoreText = Label(hudHeader, "0", .23f, .366f, 81, PulsePalette.Gold, 348, 110, TextAlignmentOptions.Left, true);
            FitNumber(scoreText, 20);
            Label(hudHeader, "Best", .528f, .744f, 28, PulsePalette.Muted, 215, 52, TextAlignmentOptions.Left);
            hudBest = Label(hudHeader, "", .542f, .376f, 46, PulsePalette.Ink, 241, 80, TextAlignmentOptions.Left, true);
            FitNumber(hudBest, 16);
            var pauseButton = Button(hudHeader, "Pause", .889f, .5f, 154, 154, PulsePalette.Line, game.TogglePause, iconOnly: true);
            var pauseRect = pauseButton.targetGraphic.rectTransform;
            Icon(At(pauseRect, "Left bar", .434f, .5f, 9, 46), null, PulsePalette.Ink);
            Icon(At(pauseRect, "Right bar", .566f, .5f, 9, 46), null, PulsePalette.Ink);

            var hint = At(p, "Steering primer", .5f, .34f, 840, 220);
            guide = hint.gameObject;
            guide.SetActive(false);
            Surface(hint, PulsePalette.Alpha(PulsePalette.Panel, .96f));
            Label(hint, "<", .09f, .685f, 45, PulsePalette.Mint, 76, 85, bold: true);
            Label(hint, ">", .91f, .685f, 45, PulsePalette.Mint, 76, 85, bold: true);
            guideTitle = Label(hint, "", .5f, .685f, 47, PulsePalette.Ink, 678, 90, bold: true);
            Label(hint, "Steer the field past your signal.", .5f, .30f, 33, PulsePalette.Muted, 780, 68);
        }

        void BuildPause()
        {
            var pause = Panel("Paused", true, out pauseGroup);
            var p = pause.transform;
            Label(p, "Paused", .5f, .748f, 114, PulsePalette.Ink, 950, 166, bold: true);
            Button(p, "Resume", .5f, .533f, 892, 156, PulsePalette.Mint, game.TogglePause, true);
            Button(p, "Restart", .5f, .429f, 892, 150, PulsePalette.Panel, game.StartRun);
            Button(p, "Settings", .5f, .332f, 892, 150, PulsePalette.Panel, OpenSettings);
            Button(p, "Home", .5f, .235f, 892, 150, PulsePalette.Panel, game.Home);
        }

        void BuildEnd()
        {
            var ended = Panel("Results", true, out endGroup);
            var p = ended.transform;
            Label(p, "GAME OVER", .5f, .881f, 113, PulsePalette.Ink, 972, 157, bold: true, spacing: 7);
            endRecord = Label(p, "New best score", .5f, .786f, 76, PulsePalette.Gold, 972, 112, bold: true);
            endScore = Label(p, "", .5f, .692f, 170, PulsePalette.Gold, 972, 201, bold: true);
            FitNumber(endScore, 62);
            endBest = Label(p, "", .5f, .646f, 52, PulsePalette.Gold, 900, 78);
            FitNumber(endBest, 32);
            BuildRunRank(p);
            Button(p, "Leaderboard", .5f, .306f, 892, 150, PulsePalette.Panel, OpenLeaderboard);
            Button(p, "Retry", .5f, .211f, 892, 156, PulsePalette.Mint, game.StartRun, true);
            Button(p, "Home", .273f, .117f, 402, 150, PulsePalette.Panel, game.Home);
            Button(p, "Settings", .727f, .117f, 402, 150, PulsePalette.Panel, OpenSettings);
        }

        void BuildRunRank(Transform parent)
        {
            var burst = At(parent, "First place confetti", .5f, .515f, 0, 0);
            rankConfetti = burst.gameObject.AddComponent<CanvasGroup>();
            rankConfetti.gameObject.SetActive(false);
            for (int i = 0; i < confettiPieces.Length; i++)
            {
                var piece = At(burst, "Confetti", .5f, .5f, 16, 16);
                Icon(piece, diamond, i % 2 == 0 ? PulsePalette.Gold : PulsePalette.Mint);
                confettiPieces[i] = piece;
            }

            rankBadge = At(parent, "Run rank", .5f, .515f, 624, 88);
            Surface(rankBadge, PulsePalette.Panel);
            endRank = Label(rankBadge, "", .5f, .5f, 42, PulsePalette.Muted, 584, 70, bold: true);
            FitNumber(endRank, 28);
        }

        void BuildLeaderboard()
        {
            var p = Panel("Leaderboard", true, out leaderboardGroup).transform;
            Label(p, "Leaderboard", .5f, .906f, 84, PulsePalette.Ink, 946, 130, bold: true);
            leaderboardStatus = Label(p, "", .5f, .812f, 34, PulsePalette.Muted, 920, 104);
            leaderboardStatus.textWrappingMode = TextWrappingModes.Normal;
            for (int i = 0; i < leaderboardRows.Length; i++)
            {
                var row = At(p, $"Rank {i + 1}", .5f, .737f - i * .055f, 892, 77);
                leaderboardRows[i] = row;
                Surface(row, PulsePalette.Panel);
                Label(row, (i + 1).ToString(), .07f, .5f, 34, PulsePalette.Muted, 95, 66, bold: true);
                leaderboardNames[i] = Label(row, "", .352f, .5f, 38, PulsePalette.Ink, 383, 67, TextAlignmentOptions.Left);
                leaderboardNames[i].richText = false;
                leaderboardScores[i] = Label(row, "", .762f, .5f, 42, PulsePalette.Gold, 362, 67, TextAlignmentOptions.Right, true);
                FitNumber(leaderboardScores[i], 20);
                row.gameObject.SetActive(false);
            }
            Button(p, "Back", .286f, .092f, 430, 150, PulsePalette.Panel, CloseLeaderboard);
            leaderboardRefresh = Button(p, "Refresh", .714f, .092f, 430, 150, PulsePalette.Mint, RefreshLeaderboard, true);
        }

        void BuildSettings()
        {
            var settings = Panel("Settings", true, out settingsGroup);
            var p = settings.transform;
            Label(p, "Settings", .5f, .919f, 105, PulsePalette.Ink, 928, 145, TextAlignmentOptions.Left, true);
            Label(p, "Steering", .5f, .835f, 46, PulsePalette.Ink, 928, 90, TextAlignmentOptions.Left, true);
            Section(p, "Steering card", .531f, .805f);
            for (int i = 0; i < controlButtons.Length; i++)
            {
                var mode = (SteeringMode)i;
                var button = Button(p, mode.ToString(), .224f + i * .276f, .752f, 276, 150, PulsePalette.Void, () =>
                {
                    PulseSettings.Controls = mode;
                    game.Input.Calibrate();
                    RefreshSettings();
                    PulseSettings.Save();
                });
                controlButtons[i] = (UnityEngine.UI.Image)button.targetGraphic;
                controlLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            modeDetail = Label(p, "", .358f, .668f, 31, PulsePalette.Muted, 571, 120, TextAlignmentOptions.Left);
            modeDetail.textWrappingMode = TextWrappingModes.Normal;
            calibrate = Button(p, "Recenter", .777f, .664f, 290, 148, PulsePalette.Line, () =>
            {
                modeDetail.text = game.Input.RecenterMotion() ? "Steering centered." : "Hold the phone still and try again.";
            }).gameObject;
            Slider(p, "Sensitivity", .574f, PulseSettings.Sensitivity, .5f, 2, v => PulseSettings.Sensitivity = v, false);

            Label(p, "Audio", .5f, .485f, 46, PulsePalette.Ink, 928, 86, TextAlignmentOptions.Left, true);
            Section(p, "Audio card", .257f, .462f);
            Slider(p, "Music", .399f, PulseSettings.Music, 0, 1, v =>
            {
                PulseSettings.Music = v;
                game.RefreshAudio();
            });
            Slider(p, "Sound effects", .303f, PulseSettings.Sfx, 0, 1, v => PulseSettings.Sfx = v);

            Switch(p, "Vibration", .274f, () =>
            {
                PulseSettings.Haptics = !PulseSettings.Haptics;
                RefreshSettings();
            }, out hapticsValue, out hapticsTrack, out hapticsThumb);
            Switch(p, "Reduce motion", .726f, () =>
            {
                PulseSettings.ReducedMotion = !PulseSettings.ReducedMotion;
                RefreshSettings();
            }, out motionValue, out motionTrack, out motionThumb);
            Button(p, "Done", .5f, .076f, 946, 156, PulsePalette.Mint, CloseSettings, true);
            RefreshSettings();
        }

        void Section(Transform parent, string name, float bottom, float top)
        {
            var section = Rect(parent, name, new Vector2(.5f, bottom), new Vector2(.5f, top));
            section.sizeDelta = new Vector2(946, 0);
            Surface(section, PulsePalette.Panel);
        }

        void Switch(Transform parent, string caption, float x, UnityAction action,
            out TextMeshProUGUI value, out UnityEngine.UI.Image track, out RectTransform thumb)
        {
            var button = Button(parent, caption, x, .19f, 454, 154, PulsePalette.Panel, action, iconOnly: true);
            var p = button.targetGraphic.rectTransform;
            Label(p, caption, .5f, .711f, 32, PulsePalette.Ink, 394, 55, TextAlignmentOptions.Left, true);
            value = Label(p, "", .277f, .30f, 29, PulsePalette.Muted, 188, 52, TextAlignmentOptions.Left);
            var toggle = At(p, "Switch track", .815f, .347f, 75, 43);
            track = Surface(toggle, PulsePalette.Line);
            thumb = At(toggle, "Switch thumb", .25f, .5f, 31, 31);
            Icon(thumb, circle, PulsePalette.Ink);
        }

        void Slider(Transform p, string caption, float y, float value, float min, float max,
            System.Action<float> change, bool percentage = true)
        {
            Label(p, caption, .5f, y + .033f, 35, PulsePalette.Muted, 842, 68, TextAlignmentOptions.Left);
            var valueText = Label(p, "", .5f, y + .033f, 37, PulsePalette.Ink, 842, 72, TextAlignmentOptions.Right, true);
            var root = At(p, caption + " slider", .5f, y, 842, 148);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            var hit = Fill(root, "Hit area", PulsePalette.Alpha(PulsePalette.Panel, .01f));
            hit.raycastTarget = true;
            var bar = At(root, "Track", .5f, .45f, 842, 12);
            Surface(bar, PulsePalette.Line);
            var fillArea = Rect(bar, "Progress area", Vector2.zero, Vector2.one);
            var fillRect = Rect(fillArea, "Progress", Vector2.zero, Vector2.one);
            Surface(fillRect, PulsePalette.Mint);
            var handleArea = Rect(root, "Handle area", new Vector2(0, .45f), new Vector2(1, .45f));
            var handle = At(handleArea, "Handle", 0, .5f, 40, 40);
            var handleImage = Icon(handle, circle, PulsePalette.Mint);
            slider.fillRect = fillRect;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v =>
            {
                change(v);
                ShowValue(v);
            });
            var nav = slider.navigation;
            nav.mode = UnityEngine.UI.Navigation.Mode.None;
            slider.navigation = nav;
            ShowValue(value);

            void ShowValue(float v)
            {
                if (percentage) valueText.SetText("{0:0}%", v * 100);
                else valueText.SetText("{0:0.0}×", v);
            }
        }

        void RefreshSettings()
        {
            for (int i = 0; i < controlButtons.Length; i++)
            {
                bool selected = (int)PulseSettings.Controls == i;
                controlButtons[i].color = selected ? PulsePalette.Mint : PulsePalette.Void;
                controlLabels[i].color = selected ? PulsePalette.Void : PulsePalette.Ink;
            }
            SteeringMode effective = game.Input.EffectiveMode;
            modeDetail.text = PulseSettings.Controls != effective
                ? (effective == SteeringMode.Touch ? "Motion sensor unavailable. Using Touch." : "Gyroscope unavailable. Using Tilt.")
                : effective switch
                {
                    SteeringMode.Tilt => "Tilt the phone side to side.",
                    SteeringMode.Twist => "Turn the phone like a wheel.",
                    _ => "Drag sideways. Lift to go straight."
                };
            calibrate.SetActive(effective != SteeringMode.Touch);
            SetSwitch(PulseSettings.Haptics, hapticsValue, hapticsTrack, hapticsThumb);
            SetSwitch(PulseSettings.ReducedMotion, motionValue, motionTrack, motionThumb);
        }

        static void SetSwitch(bool on, TextMeshProUGUI value, UnityEngine.UI.Image track, RectTransform thumb)
        {
            value.text = on ? "On" : "Off";
            value.color = on ? PulsePalette.Mint : PulsePalette.Muted;
            track.color = on ? PulsePalette.Mint : PulsePalette.Line;
            thumb.anchorMin = thumb.anchorMax = new Vector2(on ? .71f : .29f, .5f);
            thumb.GetComponent<UnityEngine.UI.Image>().color = on ? PulsePalette.Void : PulsePalette.Ink;
        }

        public bool CanBeginDrag(Vector2 position)
        {
            var area = Screen.safeArea;
            float edge = area.width * .025f;
            if (!area.Contains(position) || position.x < area.xMin + edge || position.x > area.xMax - edge
                || position.y < area.yMin + edge)
                return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(hudHeader, position, uiCamera, out var upper)
                && upper.y < hudHeader.rect.yMin;
        }

        public void OpenSettings()
        {
            if (game.Model.State == RunState.Playing) game.TogglePause();
            showingSettings = true;
            game.Online.Track("settings_opened");
            RefreshSettings();
            ShowState();
        }

        public void CloseSettings()
        {
            PulseSettings.Save();
            showingSettings = false;
            ShowState();
        }

        void OpenLeaderboard()
        {
            LeaderboardOpen = true;
            ShowState();
            game.Online.Track("leaderboard_opened");
            RefreshLeaderboard();
        }

        public void CloseLeaderboard()
        {
            LeaderboardOpen = false;
            leaderboardView++;
            ShowState();
        }

        async void RefreshLeaderboard()
        {
            int view = ++leaderboardView;
            foreach (var row in leaderboardRows) row.gameObject.SetActive(false);
            if (!game.Online.LeaderboardAvailable)
            {
                leaderboardStatus.text = "Online scores are unavailable in this build.";
                leaderboardRefresh.gameObject.SetActive(false);
                return;
            }

            leaderboardRefresh.gameObject.SetActive(true);
            leaderboardRefresh.interactable = false;
            leaderboardStatus.text = "Loading scores…";
            var request = game.Online.GetLeaderboardAsync();
            var finished = await Task.WhenAny(request, Task.Delay(LeaderboardTimeoutMs));
            if (this == null || view != leaderboardView) return;
            leaderboardRefresh.interactable = true;
            var entries = finished == request ? await request : null;
            if (entries == null)
            {
                leaderboardStatus.text = "Couldn’t load scores. Try refreshing.";
                return;
            }

            if (game.Online.ScorePending)
                leaderboardStatus.text = "Your score is waiting to sync.";
            else
                leaderboardStatus.text = entries.Length == 0 ? "No scores yet. Finish a run to post yours." : "All-time top scores";

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                leaderboardNames[i].text = entry.IsYou ? "You" : entry.Name;
                leaderboardNames[i].color = entry.IsYou ? PulsePalette.Mint : PulsePalette.Ink;
                leaderboardScores[i].text = entry.Score.ToString("N0");
                leaderboardRows[i].gameObject.SetActive(true);
            }
        }

        public void ResetRun()
        {
            runRankRequest++;
            if (LeaderboardOpen) CloseLeaderboard();
            showingSettings = false;
            shownScore = 0;
            guide.SetActive(false);
            lastScore = -1;
            ShowState();
        }

        public async void ShowResults(long score)
        {
            int run = ++runRankRequest;
            firstPlaceTime = FirstPlaceDuration;
            rankBadge.localScale = Vector3.one;
            rankConfetti.gameObject.SetActive(false);
            SetRunRank("Leaderboard unavailable", PulsePalette.Muted);
            ShowState();
            if (!game.Online.LeaderboardAvailable || Application.internetReachability == NetworkReachability.NotReachable)
                return;

            SetRunRank("Checking run rank…", PulsePalette.Muted);
            var request = game.Online.GetRunRankAsync(score);
            var finished = await Task.WhenAny(request, Task.Delay(LeaderboardTimeoutMs));
            if (this == null || run != runRankRequest || game.Model.State != RunState.Ended) return;

            var rank = finished == request ? await request : null;
            if (!rank.HasValue)
            {
                SetRunRank("Leaderboard unavailable", PulsePalette.Muted);
                return;
            }

            ShowRunRank(rank.Value);
        }

        void ShowRunRank(int rank)
        {
            if (rank > PulseOnline.LeaderboardSize)
            {
                SetRunRank($"Outside the top {PulseOnline.LeaderboardSize}", PulsePalette.Muted);
                return;
            }

            bool firstPlace = rank == 1;
            SetRunRank($"Run rank #{rank}", firstPlace ? PulsePalette.Gold : PulsePalette.Mint);
            if (firstPlace && !PulseSettings.ReducedMotion)
                firstPlaceTime = 0;
        }

        void SetRunRank(string text, Color color)
        {
            endRank.text = text;
            endRank.color = color;
        }

        public void ShowState()
        {
            var state = game.Model.State;
            bool showGame = !showingSettings && !LeaderboardOpen;
            SetPanel(titleGroup, showGame && state == RunState.Title);
            SetPanel(pauseGroup, showGame && state == RunState.Paused);
            SetPanel(endGroup, showGame && state == RunState.Ended);
            SetPanel(settingsGroup, showingSettings);
            SetPanel(leaderboardGroup, LeaderboardOpen);
            hud.SetActive(showGame && (state == RunState.Playing || state == RunState.Paused));
            bool hasBest = PulseSettings.Runs > 0 || PulseSettings.Best > 0;
            string best = hasBest ? PulseSettings.Best.ToString("N0") : "—";
            titleBest.text = hudBest.text = best;
            if (state == RunState.Ended)
            {
                endScore.text = game.Model.ScorePoints.ToString("N0");
                endScore.rectTransform.anchoredPosition = new Vector2(0, game.NewRecord ? 0 : 86);
                endRecord.gameObject.SetActive(game.NewRecord);
                endBest.text = "Best  " + best;
                endBest.gameObject.SetActive(!game.NewRecord && hasBest);
            }
            flashAlpha = PulseSettings.ReducedMotion ? 0 : .18f;
        }

        public void Render(float dt)
        {
            var model = game.Model;
            if (hud.activeSelf) RenderHud(model, dt);
            Fade(titleGroup, dt);
            Fade(pauseGroup, dt);
            Fade(endGroup, dt);
            Fade(settingsGroup, dt);
            Fade(leaderboardGroup, dt);
            RenderFirstPlace(dt);
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0, dt * 2);
            flash.color = PulsePalette.Alpha(PulsePalette.Void, flashAlpha);
        }

        void RenderFirstPlace(float dt)
        {
            if (firstPlaceTime >= FirstPlaceDuration || !endGroup.gameObject.activeSelf) return;
            firstPlaceTime = PulseSettings.ReducedMotion ? FirstPlaceDuration : Mathf.Min(firstPlaceTime + dt, FirstPlaceDuration);
            float progress = firstPlaceTime / FirstPlaceDuration;
            float spread = 1 - (1 - progress) * (1 - progress);
            rankBadge.localScale = Vector3.one * (1 + .12f * Mathf.Sin(Mathf.PI * spread));
            rankConfetti.gameObject.SetActive(progress < 1);
            rankConfetti.alpha = 1 - progress;
            for (int i = 0; i < confettiPieces.Length; i++)
            {
                float side = i < ConfettiPerSide ? -1 : 1;
                float fan = i % ConfettiPerSide - (ConfettiPerSide - 1) * .5f;
                var piece = confettiPieces[i];
                piece.anchoredPosition = new Vector2(side * (280 + (126 - 14 * Mathf.Abs(fan)) * spread),
                    fan * (12 + 28 * spread) - 42 * progress * progress);
                piece.localRotation = Quaternion.Euler(0, 0, side * (fan * 18 + 180 * progress));
            }
        }

        void RenderHud(PulseSimulation model, float dt)
        {
            shownScore += (model.Score - shownScore) * (1 - Mathf.Exp(-18 * dt));
            long score = (long)shownScore;
            if (score != lastScore)
            {
                lastScore = score;
                scoreText.text = score.ToString("N0");
            }
            UpdateGuide(model);
        }

        void UpdateGuide(PulseSimulation model)
        {
            bool playing = model.State == RunState.Playing;
            SteeringMode mode = game.Input.EffectiveMode;
            if (playing && Mathf.Abs(game.Input.Steering) >= .14f)
                PulseSettings.MarkLearned(mode);
            bool visible = playing && model.Elapsed < 8 && !PulseSettings.HasLearned(mode);
            if (visible && !guide.activeSelf)
            {
                guideTitle.text = mode switch
                {
                    SteeringMode.Tilt => "Tilt left or right",
                    SteeringMode.Twist => "Turn your phone",
                    _ => "Drag left or right"
                };
            }
            guide.SetActive(visible);
        }

        static void SetPanel(CanvasGroup group, bool visible)
        {
            bool wasVisible = group.gameObject.activeSelf;
            group.gameObject.SetActive(visible);
            if (visible && !wasVisible) group.alpha = PulseSettings.ReducedMotion ? 1 : 0;
        }

        static void Fade(CanvasGroup group, float dt)
        {
            if (group.gameObject.activeSelf && group.alpha < 1)
                group.alpha = PulseSettings.ReducedMotion ? 1 : Mathf.MoveTowards(group.alpha, 1, dt / .21f);
        }

        GameObject Panel(string name, bool scrim, out CanvasGroup group)
        {
            var r = Rect(safe, name, Vector2.zero, Vector2.one);
            group = r.gameObject.AddComponent<CanvasGroup>();
            r.gameObject.SetActive(false);
            if (scrim)
            {
                float opacity = name switch { "Results" => .22f, "Settings" => .16f, _ => .62f };
                var background = Fill(r, "Scrim", PulsePalette.Alpha(PulsePalette.Void, opacity));
                background.raycastTarget = true;
            }
            return r.gameObject;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        static RectTransform At(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = Rect(parent, name, new Vector2(x, y), new Vector2(x, y));
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        static UnityEngine.UI.Image Fill(Transform parent, string name, Color color) =>
            Icon(Rect(parent, name, Vector2.zero, Vector2.one), null, color);

        static UnityEngine.UI.Image Icon(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        UnityEngine.UI.Image Surface(RectTransform rect, Color color)
        {
            var image = Icon(rect, rounded, color);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        static TextMeshProUGUI Label(Transform p, string text, float x, float y, int size, Color color,
            float width, float height, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, float spacing = 0)
        {
            var r = At(p, text.Length > 0 ? text : "Label", x, y, width, height);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.characterSpacing = spacing;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        static void FitNumber(TMP_Text text, float minimumSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = text.fontSize;
        }

        UnityEngine.UI.Button Button(Transform p, string text, float x, float y, float w, float h,
            Color color, UnityAction action, bool primary = false, bool iconOnly = false)
        {
            var r = At(p, text + " button", x, y, w, h);
            var hit = Icon(r, null, PulsePalette.Alpha(PulsePalette.Panel, .01f));
            hit.raycastTarget = true;
            var face = Rect(r, "Face", Vector2.zero, Vector2.one);
            var image = Surface(face, color);
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = image;
            var colors = b.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = PulsePalette.Muted;
            colors.disabledColor = PulsePalette.Alpha(PulsePalette.Muted, .65f);
            b.colors = colors;
            b.onClick.AddListener(() => { game.Audio.Click(); action(); });
            var nav = b.navigation;
            nav.mode = UnityEngine.UI.Navigation.Mode.None;
            b.navigation = nav;
            r.gameObject.AddComponent<PulseButtonMotion>().Visual = face;
            if (!iconOnly)
                Label(face, text, .5f, .5f, primary ? 45 : 38, primary ? PulsePalette.Void : PulsePalette.Ink, w - 28, h - 10, bold: true);
            return b;
        }

        Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const int radius = 25;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var corner = new Vector2(
                    Mathf.Max(0, Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius)),
                    Mathf.Max(0, Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius)));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - corner.magnitude) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            roundedTexture.SetPixels32(pixels);
            roundedTexture.Apply(false, true);
            return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0,
                SpriteMeshType.FullRect, Vector4.one * radius);
        }

        void OnDestroy()
        {
            Destroy(rounded);
            Destroy(roundedTexture);
        }
    }
}
