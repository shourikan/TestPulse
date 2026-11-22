using System;
using System.Globalization;
using UnityEngine;

namespace Pulse
{
    public enum SteeringMode { Touch, Tilt, Twist }
    public static class PulseSettings
    {
        const string Prefix = "pulse.v1.";
        public static long Best { get; private set; }
        public static long LeaderboardBest { get; private set; }
        public static int Runs { get; private set; }
        public static float Music = .55f, Sfx = .75f, Sensitivity = 1;
        public static bool Haptics = true, ReducedMotion;
        public static SteeringMode Controls;
        static int learnedSteering;
        public static void Load()
        {
            long.TryParse(PlayerPrefs.GetString(Prefix + "best64"), NumberStyles.None,
                CultureInfo.InvariantCulture, out long savedBest);
            Best = Math.Max(savedBest, Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "best")));
            LeaderboardBest = long.TryParse(PlayerPrefs.GetString(Prefix + "leaderboardBest"), NumberStyles.None,
                CultureInfo.InvariantCulture, out long onlineBest) ? onlineBest : -1;
            Runs = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "runs"));
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "music", .55f));
            Sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "sfx", .75f));
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "sensitivity", 1), .5f, 2);
            Haptics = PlayerPrefs.GetInt(Prefix + "haptics", 1) == 1;
            ReducedMotion = PlayerPrefs.GetInt(Prefix + "motion", 0) == 1;
            Controls = (SteeringMode)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "controls"), 0, 2);
            learnedSteering = PlayerPrefs.GetInt(Prefix + "steeringLearned");
        }
        public static bool HasLearned(SteeringMode mode) => (learnedSteering & (1 << (int)mode)) != 0;

        public static void MarkLearned(SteeringMode mode)
        {
            if (HasLearned(mode)) return;
            learnedSteering |= 1 << (int)mode;
            PlayerPrefs.SetInt(Prefix + "steeringLearned", learnedSteering);
            PlayerPrefs.Save();
        }

        public static bool SaveRun(long score)
        {
            bool record = score > Best;
            Best = Math.Max(Best, score); Runs++;
            LeaderboardBest = Math.Max(LeaderboardBest, score);
            Save();
            return record;
        }
        public static void Save()
        {
            PlayerPrefs.SetString(Prefix + "best64", Best.ToString(CultureInfo.InvariantCulture));
            if (LeaderboardBest >= 0)
                PlayerPrefs.SetString(Prefix + "leaderboardBest", LeaderboardBest.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(Prefix + "runs", Runs);
            PlayerPrefs.SetFloat(Prefix + "music", Music); PlayerPrefs.SetFloat(Prefix + "sfx", Sfx);
            PlayerPrefs.SetFloat(Prefix + "sensitivity", Sensitivity);
            PlayerPrefs.SetInt(Prefix + "haptics", Haptics ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "motion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "controls", (int)Controls);
            PlayerPrefs.Save();
        }
    }
}
