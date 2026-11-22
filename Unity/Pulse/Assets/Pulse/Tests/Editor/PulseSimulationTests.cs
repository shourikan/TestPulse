using NUnit.Framework;
using UnityEngine;

namespace Pulse.Tests
{
    public class PulseSimulationTests
    {
        static PulseSimulation Run()
        {
            var m = new PulseSimulation(); m.Start(42, 1000); Clear(m); return m;
        }
        static void Clear(PulseSimulation m) { foreach (var a in m.Actors) a.Position = new Vector2(12, 100); }
        static void Pick(PulseSimulation m, int index) { m.Actors[index].Position = Vector2.zero; m.Step(.01f, 0); }

        [Test] public void ScoreAndSpeedAgreeAcrossFrameRates()
        {
            var a = Run(); var b = Run();
            for (int i = 0; i < 300; i++) a.Step(1f / 30, .3f);
            for (int i = 0; i < 1200; i++) b.Step(1f / 120, .3f);
            Assert.That(a.Elapsed, Is.EqualTo(b.Elapsed).Within(.002f));
            Assert.That(a.Score, Is.EqualTo(b.Score).Within(.1f));
            Assert.That(a.Speed, Is.EqualTo(b.Speed).Within(.001f));
        }
        [Test] public void UnprotectedHazardEndsRun() { var m = Run(); Pick(m, 0); Assert.That(m.State, Is.EqualTo(RunState.Ended)); }

        [Test]
        public void ResultsDriftKeepsTheFinalHeadingAndRunTotals()
        {
            var model = Run();
            model.Step(.08f, .6f);
            model.Actors[0].Position = Vector2.zero;
            model.Step(.01f, .6f);
            Assert.That(model.State, Is.EqualTo(RunState.Ended));
            Assert.That(model.Actors[0].Position.y, Is.GreaterThan(model.HalfHeight));

            Vector2 before = model.FieldOffset;
            double score = model.Score, elapsed = model.Elapsed;
            model.Actors[50].Position = Vector2.zero;
            int feedback = 0;
            model.Feedback += (_, _, _) => feedback++;
            model.DriftAfterDeath(.1f);
            Vector2 movement = model.FieldOffset - before;
            model.DriftAfterDeath(.1f);

            Assert.That(Vector2.Distance(movement, model.ScrollDirection * .078f), Is.LessThan(.0001f));
            Assert.That(Vector2.Distance(model.FieldOffset - before, movement * 2), Is.LessThan(.0001f));
            Assert.That(model.Score, Is.EqualTo(score));
            Assert.That(model.Elapsed, Is.EqualTo(elapsed));
            Assert.That(model.Collected, Is.Zero);
            Assert.That(feedback, Is.Zero);
        }
        [Test] public void GoldAwardsScoreAndRecycles()
        {
            var m = Run(); Pick(m, 50);
            Assert.That(m.Score, Is.GreaterThanOrEqualTo(100)); Assert.That(m.Collected, Is.EqualTo(1));
            Assert.That(m.Actors[50].Position.y, Is.GreaterThan(m.HalfHeight));
        }
        [Test] public void PassiveShieldAbsorbsHitAndProvidesGrace()
        {
            var m = Run(); Pick(m, 60); Pick(m, 0); Pick(m, 1);
            Assert.That(m.Shields, Is.Zero); Assert.That(m.State, Is.EqualTo(RunState.Playing));
            m.Step(.1f, 0); m.Step(.1f, 0); m.Step(.1f, 0); m.Step(.1f, 0); m.Step(.1f, 0); m.Step(.1f, 0); m.Step(.1f, 0);
            Pick(m, 0); Assert.That(m.State, Is.EqualTo(RunState.Ended));
        }
        [Test] public void PulseCostsThreeAndExpiresInGameTime()
        {
            var m = Run(); Assert.That(m.ActivatePulse(), Is.False);
            Pick(m, 60); Pick(m, 61); Pick(m, 62);
            Assert.That(m.ActivatePulse(), Is.True); Assert.That(m.Shields, Is.Zero);
            Pick(m, 0); Assert.That(m.State, Is.EqualTo(RunState.Playing));
            m.Pause(); float left = m.PulseLeft; m.Step(1, 0); Assert.That(m.PulseLeft, Is.EqualTo(left));
            m.Resume(); Clear(m); for (int i = 0; i < 601; i++) m.Step(1f / 60, 0);
            Assert.That(m.PulseLeft, Is.Zero); Pick(m, 0); Assert.That(m.State, Is.EqualTo(RunState.Ended));
        }

        [Test]
        public void ShieldPickupDuringPulseRestoresOneChargeWorthOfTime()
        {
            var model = Run();
            for (int i = 0; i < PulseSimulation.ShieldsToActivate; i++) Pick(model, 60);
            Assert.That(model.ActivatePulse(), Is.True);
            for (int i = 0; i < 50; i++) model.Step(.1f, 0);
            float remaining = model.PulseLeft;
            Pick(model, 60);

            Assert.That(model.PulseLeft, Is.EqualTo(remaining - .01f
                + PulseSimulation.PulseDuration / PulseSimulation.ShieldsToActivate).Within(.001f));
            Assert.That(model.Shields, Is.Zero);
        }

        [Test]
        public void PulseRechargeCapsAtFullThenPickupsBankAfterExpiry()
        {
            var model = Run();
            for (int i = 0; i < PulseSimulation.ShieldsToActivate; i++) Pick(model, 60);
            Assert.That(model.ActivatePulse(), Is.True);
            model.Actors[60].Position = Vector2.zero;
            model.Step(1f / 120, 0);
            Assert.That(model.PulseLeft, Is.EqualTo(PulseSimulation.PulseDuration));
            Assert.That(model.Shields, Is.Zero);
            Clear(model);
            for (int i = 0; i < 101; i++) model.Step(.1f, 0);
            Assert.That(model.PulseLeft, Is.Zero);
            Pick(model, 60);
            Assert.That(model.Shields, Is.EqualTo(1));
        }
        [Test] public void PauseFreezesSimulationAndResumeContinues()
        {
            var m = Run(); m.Step(.1f, 0); m.Pause(); double score = m.Score;
            m.Step(.1f, 1); Assert.That(m.Score, Is.EqualTo(score));
            m.Resume(); m.Step(.1f, 0); Assert.That(m.Score, Is.GreaterThan(score));
        }
        [Test] public void RestartClearsAllRunState()
        {
            var m = Run(); Pick(m, 60); Pick(m, 61); Pick(m, 62); m.ActivatePulse(); Pick(m, 50);
            m.End(); m.Start(42, 1000);
            Assert.That(m.Score, Is.Zero); Assert.That(m.Elapsed, Is.Zero); Assert.That(m.Shields, Is.Zero);
            Assert.That(m.PulseLeft, Is.Zero); Assert.That(m.InvulnerableLeft, Is.Zero); Assert.That(m.Collected, Is.Zero);
            Assert.That(m.Speed, Is.EqualTo(PulseSimulation.StartSpeed)); Assert.That(m.Slowing, Is.False);
        }
        [Test] public void SameSeedProducesSameField()
        {
            var a = Run(); var b = Run(); a.Start(123, 0); b.Start(123, 0);
            for (int i = 0; i < a.Actors.Length; i++) Assert.That(a.Actors[i].Position, Is.EqualTo(b.Actors[i].Position));
        }
        [Test]
        public void GentleOpeningMovesOnlyHazardsFartherAhead()
        {
            var normal = new PulseSimulation();
            var gentle = new PulseSimulation();
            normal.Start(123, 0);
            gentle.Start(123, 0, gentleOpening: true);
            for (int i = 0; i < gentle.Actors.Length; i++)
            {
                var actor = gentle.Actors[i];
                if (actor.Kind == ActorKind.Hazard)
                    Assert.That(actor.Position.y, Is.GreaterThanOrEqualTo(normal.Actors[i].Position.y));
                else
                    Assert.That(actor.Position, Is.EqualTo(normal.Actors[i].Position));
            }
            for (int i = 0; i < 180; i++) gentle.Step(1f / 60, 0);
            Assert.That(gentle.State, Is.EqualTo(RunState.Playing));
        }

        [Test]
        public void BreakingThroughHazardsAwardsPointsAndIdentifiesShieldLoss()
        {
            var model = Run();
            var impacts = new System.Collections.Generic.List<FeedbackKind>();
            var rewards = new System.Collections.Generic.List<long>();
            model.Feedback += (kind, position, value) =>
            {
                if (kind != FeedbackKind.Impact && kind != FeedbackKind.ShieldBreak) return;
                impacts.Add(kind);
                rewards.Add(value);
            };
            Pick(model, 60);
            Pick(model, 0);
            Pick(model, 1);
            Pick(model, 60);
            Pick(model, 61);
            Pick(model, 62);
            Assert.That(model.ActivatePulse(), Is.True);
            Pick(model, 2);
            Assert.That(impacts, Is.EqualTo(new[] { FeedbackKind.ShieldBreak, FeedbackKind.Impact, FeedbackKind.Impact }));
            Assert.That(rewards, Is.EqualTo(new[] { 40, 40, 40 }));
            Assert.That(model.Score, Is.EqualTo(120.7f).Within(.01f));
            Assert.That(model.Collected, Is.Zero);
        }

        [Test]
        public void RewardsKeepGrowingPastTheFormerCap()
        {
            var model = Run();
            long reward = 0;
            model.Feedback += (kind, position, value) => { if (kind == FeedbackKind.Gold) reward = value; };
            Pick(model, 50);
            Assert.That(reward, Is.EqualTo(100));

            for (int i = 0; i < 400; i++) { Clear(model); model.Step(.1f, 0); }
            Pick(model, 50);
            Assert.That(reward, Is.EqualTo(200));

            for (int i = 0; i < 600; i++) { Clear(model); model.Step(.1f, 0); }
            Pick(model, 50);
            Assert.That(reward, Is.EqualTo(350));

            Pick(model, 60);
            model.Feedback += (kind, position, value) => { if (kind == FeedbackKind.ShieldBreak) reward = value; };
            Pick(model, 0);
            Assert.That(reward, Is.EqualTo(140));

            for (int i = 0; i < 2000; i++) { Clear(model); model.Step(.1f, 0); }
            Pick(model, 50);
            Assert.That(reward, Is.EqualTo(850));
        }

        [Test] public void SlowCycleRaisesNextSpeedCeiling()
        {
            var m = Run();
            for (int i = 0; i < 2400 && m.SpeedLimit < 12; i++) { Clear(m); m.Step(1f / 60, 0); }
            Assert.That(m.SpeedLimit, Is.GreaterThan(11.25f)); Assert.That(m.Slowing, Is.False);
        }
        [Test] public void StalledFrameCannotAdvanceMoreThanTenthSecond()
        {
            var m = Run(); m.Step(10, 0); Assert.That(m.Elapsed, Is.EqualTo(.1f).Within(.001));
        }
        [Test] public void RecordEventFiresOnlyOnce()
        {
            var m = Run(); m.Start(1, 1); Clear(m); int events = 0;
            m.Feedback += (kind, pos, value) => { if (kind == FeedbackKind.Record) events++; };
            for (int i = 0; i < 9; i++) m.Step(1f / 60, 0);
            Assert.That(m.Score, Is.GreaterThan(1));
            Assert.That(m.ScorePoints, Is.EqualTo(1));
            Assert.That(events, Is.Zero);
            for (int i = 0; i < 60; i++) m.Step(1f / 60, 0);
            Assert.That(events, Is.EqualTo(1));
        }
        [TestCase(-1f)] [TestCase(1f)]
        public void FullSteeringIsCappedAtSixtyDegreesWithForwardTravel(float input)
        {
            var m = Run();
            Vector2 original = m.Actors[0].Position;
            for (int i = 0; i < 60; i++) m.Step(1f / 60, input);
            Vector2 displacement = m.Actors[0].Position - original;
            Assert.That(-displacement.y, Is.GreaterThan(0));
            Assert.That(Vector2.Angle(Vector2.down, m.ScrollDirection), Is.EqualTo(60).Within(.01f));
            Assert.That(Vector2.Distance(displacement, m.FieldOffset), Is.LessThan(.001f));
        }
        [Test] public void SimulationDoesNotAddASecondSteeringLag()
        {
            var m = Run(); m.Step(1f / 60, 1); m.Step(1f / 60, -.25f);
            Assert.That(m.Steering, Is.EqualTo(-.25f));
        }
    }
}
