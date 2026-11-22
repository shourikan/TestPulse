#if PULSE_FIREBASE_AUTH && PULSE_FIREBASE_FIRESTORE && (UNITY_ANDROID || UNITY_EDITOR)
#define PULSE_LEADERBOARD
#endif
#if PULSE_FIREBASE_ANALYTICS && UNITY_ANDROID && !UNITY_EDITOR
#define PULSE_ANALYTICS
#endif

using System;
using System.Threading.Tasks;
using UnityEngine;
#if PULSE_LEADERBOARD || PULSE_ANALYTICS
using Firebase;
#endif
#if PULSE_LEADERBOARD
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
#endif
#if PULSE_ANALYTICS
using Firebase.Analytics;
#endif

namespace Pulse
{
    public readonly struct PulseLeaderboardEntry
    {
        public readonly string Name;
        public readonly long Score;
        public readonly bool IsYou;

        public PulseLeaderboardEntry(string name, long score, bool isYou)
        {
            Name = name;
            Score = score;
            IsYou = isYou;
        }
    }

    public sealed class PulseOnline
    {
        public const int LeaderboardSize = 10;
        const double PlayTimeInterval = 30;
        double reportedPlayTime;

#if PULSE_LEADERBOARD || PULSE_ANALYTICS
        Task initialization;

        Task InitializeAsync() => initialization ??= CheckFirebaseAsync();

        static async Task CheckFirebaseAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException("Firebase dependencies unavailable: " + status);
            if (string.IsNullOrEmpty(FirebaseApp.DefaultInstance.Options.ProjectId))
                throw new InvalidOperationException("Firebase app configuration is missing.");
        }
#endif

#if PULSE_LEADERBOARD
        const string Collection = "pulseLeaderboard";
        const string ScoreField = "score";
        const double RetryInterval = 30;
        Task<AuthResult> signIn;
        Task scoreSync;
        Task<PulseLeaderboardEntry[]> leaderboardRequest;
        double retryAt;
        long confirmedScore = -1;
#endif

#if PULSE_ANALYTICS
        const int MaxQueuedAnalyticsEvents = 64;
        int queuedAnalyticsEvents;
        bool analyticsFailed;
#endif

#if PULSE_LEADERBOARD && UNITY_EDITOR
        public bool LeaderboardAvailable { get; } = System.IO.File.Exists(
            System.IO.Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json"));
#elif PULSE_LEADERBOARD
        public bool LeaderboardAvailable => true;
#else
        public bool LeaderboardAvailable => false;
#endif

        public bool ScorePending
        {
            get
            {
#if PULSE_LEADERBOARD
                return LeaderboardAvailable && PulseSettings.LeaderboardBest > confirmedScore;
#else
                return false;
#endif
            }
        }

        public void Update()
        {
#if PULSE_LEADERBOARD
            if (Time.realtimeSinceStartupAsDouble >= retryAt)
                SyncScore();
#endif
        }

        public void SyncScore()
        {
#if PULSE_LEADERBOARD
            if (ScorePending && Application.internetReachability != NetworkReachability.NotReachable)
                _ = SyncScoreAsync();
#endif
        }

        public Task<PulseLeaderboardEntry[]> GetLeaderboardAsync(bool fresh = false)
        {
#if PULSE_LEADERBOARD
            if (fresh || leaderboardRequest == null || leaderboardRequest.IsCompleted)
                leaderboardRequest = FetchLeaderboardAsync();
            return leaderboardRequest;
#else
            return Task.FromResult<PulseLeaderboardEntry[]>(null);
#endif
        }

#if PULSE_LEADERBOARD
        async Task<string> GetPlayerIdAsync()
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser is FirebaseUser guest) return guest.UserId;
            if (signIn == null || signIn.IsCompleted)
                signIn = auth.SignInAnonymouslyAsync();
            return (await signIn).User.UserId;
        }

        Task SyncScoreAsync()
        {
            if (scoreSync == null || scoreSync.IsCompleted)
                scoreSync = SubmitScoresAsync();
            return scoreSync;
        }

        async Task SubmitScoresAsync()
        {
            if (!ScorePending) return;
            retryAt = Time.realtimeSinceStartupAsDouble + RetryInterval;
            try
            {
                await InitializeAsync();
                var playerId = await GetPlayerIdAsync();
                var store = FirebaseFirestore.DefaultInstance;
                var entry = store.Collection(Collection).Document(playerId);
                while (ScorePending)
                {
                    long score = PulseSettings.LeaderboardBest;
                    await store.RunTransactionAsync(async transaction =>
                    {
                        var saved = await transaction.GetSnapshotAsync(entry);
                        if (!saved.Exists || score > saved.GetValue<long>(ScoreField))
                            transaction.Set(entry, new Dictionary<string, object> { [ScoreField] = score });
                    });
                    confirmedScore = score;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Pulse] Could not submit leaderboard score: " + error.Message);
            }
        }

        async Task<PulseLeaderboardEntry[]> FetchLeaderboardAsync()
        {
            if (!LeaderboardAvailable) return null;
            try
            {
                await InitializeAsync();
                var playerId = await GetPlayerIdAsync();
                await SyncScoreAsync();
                var scores = await FirebaseFirestore.DefaultInstance.Collection(Collection)
                    .OrderByDescending(ScoreField).Limit(LeaderboardSize).GetSnapshotAsync(Source.Server);
                var entries = new List<PulseLeaderboardEntry>(scores.Count);
                foreach (var score in scores.Documents)
                {
                    string id = score.Id;
                    string guest = "Guest " + id.Substring(Math.Max(0, id.Length - 6));
                    entries.Add(new PulseLeaderboardEntry(guest, score.GetValue<long>(ScoreField), id == playerId));
                }
                return entries.ToArray();
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Pulse] Could not load leaderboard: " + error.Message);
                return null;
            }
        }
#endif

        public void RunStarted(SteeringMode steering)
        {
            reportedPlayTime = 0;
#if PULSE_ANALYTICS
            LogAnalytics(() => FirebaseAnalytics.LogEvent("run_started", "steering_mode", steering.ToString().ToLowerInvariant()));
#endif
        }

        public void RecordPlayTime(double elapsed, bool flush = false)
        {
            double duration = elapsed - reportedPlayTime;
            if (duration < PlayTimeInterval && !flush) return;
            long milliseconds = (long)(duration * 1000);
            if (milliseconds <= 0) return;
            reportedPlayTime += milliseconds / 1000d;
#if PULSE_ANALYTICS
            LogAnalytics(() => FirebaseAnalytics.LogEvent("play_time", "play_time_ms", milliseconds));
#endif
        }

        public void RunCompleted(long score, double elapsed, bool newRecord)
        {
            RecordPlayTime(elapsed, flush: true);
#if PULSE_ANALYTICS
            LogAnalytics(() => FirebaseAnalytics.LogEvent("run_completed",
                new Parameter("score", score),
                new Parameter("duration_ms", (long)(elapsed * 1000)),
                new Parameter("new_record", newRecord ? 1L : 0L)));
#endif
            SyncScore();
        }

        public void Track(string eventName)
        {
#if PULSE_ANALYTICS
            LogAnalytics(() => FirebaseAnalytics.LogEvent(eventName));
#endif
        }

#if PULSE_ANALYTICS
        async void LogAnalytics(Action send)
        {
            if (analyticsFailed || queuedAnalyticsEvents >= MaxQueuedAnalyticsEvents) return;
            queuedAnalyticsEvents++;
            try
            {
                await InitializeAsync();
                send();
            }
            catch (Exception error)
            {
                if (!analyticsFailed)
                    Debug.LogWarning("[Pulse] Firebase Analytics unavailable: " + error.Message);
                analyticsFailed = true;
            }
            finally
            {
                queuedAnalyticsEvents--;
            }
        }
#endif
    }
}
