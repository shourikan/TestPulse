using System;
using UnityEngine;

namespace Pulse
{
    public enum RunState { Title, Playing, Paused, Ended }
    public enum ActorKind { Hazard, Gold, Shield }
    public enum FeedbackKind { Gold, Shield, Impact, ShieldBreak, Death, Pulse, Slow, Record }

    public sealed class PulseActor
    {
        public ActorKind Kind;
        public Vector2 Position;
        public float Radius => Kind == ActorKind.Hazard ? .14f : .25f;
        public float Parallax => Kind == ActorKind.Hazard ? 1 : Kind == ActorKind.Gold ? .8f : .6f;
    }

    // The Processing rules in resolution-independent units. No scene objects or wall-clock timers.
    public sealed class PulseSimulation
    {
        public const float StartSpeed = 2.25f, PlayerRadius = .27f, PulseDuration = 10;
        public const float MaxSteeringAngle = 60;
        public const int ActorCount = 64;
        public const int ShieldsToActivate = 3;
        public const int GoldBaseValue = 100, BreakthroughBaseValue = 40;
        const double SecondsPerRewardStep = 40;
        const float ResultsDriftSpeed = .78f;
        public readonly PulseActor[] Actors = new PulseActor[ActorCount];
        public RunState State { get; private set; } = RunState.Title;
        public double Score { get; private set; }
        public long ScorePoints => (long)Score;
        public double Elapsed { get; private set; }
        double RewardMultiplier => 1 + Elapsed / SecondsPerRewardStep;
        public float Speed { get; private set; } = StartSpeed;
        public float SpeedLimit { get; private set; } = 11.25f;
        public float PulseLeft { get; private set; }
        public float InvulnerableLeft { get; private set; }
        public int Shields { get; private set; }
        public bool PulseReady => State == RunState.Playing && Shields >= ShieldsToActivate && PulseLeft <= 0;
        public int Collected { get; private set; }
        public bool Slowing { get; private set; }
        public float HalfHeight = 9.6f;
        public float Steering { get; private set; }
        public Vector2 FieldOffset { get; private set; }
        public Vector2 ScrollDirection
        {
            get
            {
                float angle = Steering * MaxSteeringAngle * Mathf.Deg2Rad;
                return new Vector2(-Mathf.Sin(angle), -Mathf.Cos(angle));
            }
        }
        public event Action<FeedbackKind, Vector2, long> Feedback;
        System.Random random = new System.Random();
        bool recordAnnounced;
        long best;

        public PulseSimulation()
        {
            for (int i = 0; i < Actors.Length; i++)
                Actors[i] = new PulseActor { Kind = i < 50 ? ActorKind.Hazard : i < 60 ? ActorKind.Gold : ActorKind.Shield };
        }

        public void Start(int seed, long previousBest, bool gentleOpening = false)
        {
            random = new System.Random(seed);
            best = previousBest;
            Score = Elapsed = PulseLeft = InvulnerableLeft = Steering = 0;
            FieldOffset = Vector2.zero;
            Speed = StartSpeed;
            SpeedLimit = 11.25f;
            Shields = Collected = 0;
            Slowing = recordAnnounced = false;
            foreach (var actor in Actors)
            {
                Respawn(actor, initial: true, gentleOpening);
            }
            State = RunState.Playing;
        }

        public void Pause() { if (State == RunState.Playing) State = RunState.Paused; }
        public void Resume() { if (State == RunState.Paused) State = RunState.Playing; }
        public void Home() { State = RunState.Title; PulseLeft = 0; }
        public bool ActivatePulse()
        {
            if (!PulseReady) return false;
            Shields -= ShieldsToActivate;
            PulseLeft = PulseDuration;
            Feedback?.Invoke(FeedbackKind.Pulse, Vector2.zero, 0);
            return true;
        }

        public void Step(float dt, float steering)
        {
            if (State != RunState.Playing || dt <= 0) return;
            // Bound catch-up after stalls and sweep collisions so high-speed hazards cannot tunnel.
            float remaining = Mathf.Min(dt, .1f);
            while (remaining > .00001f && State == RunState.Playing)
            {
                float step = Mathf.Min(remaining, 1f / 120f);
                Tick(step, Mathf.Clamp(steering, -1, 1));
                remaining -= step;
            }
        }

        void Tick(float dt, float input)
        {
            Steering = input;
            Elapsed += dt;
            PulseLeft = Mathf.Max(0, PulseLeft - dt);
            InvulnerableLeft = Mathf.Max(0, InvulnerableLeft - dt);
            if (Speed > SpeedLimit && !Slowing)
            {
                Slowing = true;
                Feedback?.Invoke(FeedbackKind.Slow, Vector2.zero, 0);
            }
            if (Slowing)
            {
                Speed *= Mathf.Exp(-.8f * dt);
                if (Speed <= StartSpeed) { Speed = StartSpeed; Slowing = false; SpeedLimit += 3.9375f; }
            }
            else Speed *= Mathf.Exp(.075f * dt);
            float multiplier = 1 + (Speed - StartSpeed) / .5625f * .1f;
            Score += 10 * multiplier * dt;
            var movement = ScrollDirection * Speed * dt;
            FieldOffset += movement;
            for (int i = 0; i < Actors.Length; i++)
            {
                var actor = Actors[i];
                var old = actor.Position;
                actor.Position += movement * actor.Parallax;
                float radius = PlayerRadius + actor.Radius + (PulseLeft > 0 ? .3f : Shields > 0 ? .08f : 0);
                if (SegmentDistanceSquared(old, actor.Position) < radius * radius)
                {
                    if (actor.Kind == ActorKind.Gold)
                    {
                        long value = (long)Math.Round(GoldBaseValue * RewardMultiplier);
                        Score += value; Collected++;
                        Feedback?.Invoke(FeedbackKind.Gold, actor.Position, value);
                    }
                    else if (actor.Kind == ActorKind.Shield)
                    {
                        int gained = CollectShield();
                        Feedback?.Invoke(FeedbackKind.Shield, actor.Position, gained);
                    }
                    else if (PulseLeft > 0 || InvulnerableLeft > 0 || Shields > 0)
                    {
                        bool shieldLost = PulseLeft <= 0 && InvulnerableLeft <= 0;
                        if (shieldLost)
                        {
                            Shields--;
                            InvulnerableLeft = .65f;
                        }
                        long value = (long)Math.Round(BreakthroughBaseValue * RewardMultiplier);
                        Score += value;
                        Feedback?.Invoke(shieldLost ? FeedbackKind.ShieldBreak : FeedbackKind.Impact, actor.Position, value);
                    }
                    else
                    {
                        var impact = actor.Position;
                        Respawn(actor, false);
                        // Finish this tick's field movement before the results drift takes over.
                        for (int j = i + 1; j < Actors.Length; j++) MoveUncollected(Actors[j], movement);
                        EndAt(impact);
                        return;
                    }
                    Respawn(actor, false);
                }
                WrapField(actor);
            }
            if (best > 0 && ScorePoints > best && !recordAnnounced && State == RunState.Playing)
            {
                recordAnnounced = true;
                Feedback?.Invoke(FeedbackKind.Record, Vector2.zero, 0);
            }
        }

        int CollectShield()
        {
            if (PulseLeft > 0)
            {
                PulseLeft = Mathf.Min(PulseDuration, PulseLeft + PulseDuration / ShieldsToActivate);
                return 1;
            }
            if (Shields >= ShieldsToActivate) return 0;
            Shields++;
            return 1;
        }

        public void DriftAfterDeath(float dt)
        {
            if (State != RunState.Ended || dt <= 0) return;
            // Continue the final heading at a steady pace without ticking the run or collisions.
            var movement = ScrollDirection * (ResultsDriftSpeed * Mathf.Min(dt, .1f));
            FieldOffset += movement;
            foreach (var actor in Actors) MoveUncollected(actor, movement);
        }

        void MoveUncollected(PulseActor actor, Vector2 movement)
        {
            actor.Position += movement * actor.Parallax;
            WrapField(actor);
        }

        void WrapField(PulseActor actor)
        {
            if (actor.Position.y < -HalfHeight - 1) Respawn(actor, false);
            // Three-screen field, matching the original, wrapped horizontally for endless travel.
            if (actor.Position.x < -16.2f) actor.Position.x += 32.4f;
            if (actor.Position.x > 16.2f) actor.Position.x -= 32.4f;
        }

        public void End() => EndAt(Vector2.zero);

        void EndAt(Vector2 impact)
        {
            if (State != RunState.Playing) return;
            State = RunState.Ended;
            Feedback?.Invoke(FeedbackKind.Death, impact, ScorePoints);
        }

        void Respawn(PulseActor actor, bool initial, bool gentleOpening = false)
        {
            float initialDistance = gentleOpening && actor.Kind == ActorKind.Hazard ? 9.6f : 3.4f;
            float minY = initial ? initialDistance : HalfHeight + .8f;
            actor.Position = new Vector2(Range(-16.2f, 16.2f), Range(minY, HalfHeight * 2 + 2));
        }
        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
        static float SegmentDistanceSquared(Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float t = d.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(-Vector2.Dot(a, d) / d.sqrMagnitude);
            return (a + d * t).sqrMagnitude;
        }
    }
}
