using NUnit.Framework;
using UnityEngine;

namespace Pulse.Tests
{
    public class PulseMotionControlTests
    {
        static Vector3 Gravity(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0);
        }

        [TestCase(-1f)] [TestCase(1f)]
        public void ReturningFromExcessTiltRespondsOnTheFirstSample(float sign)
        {
            var control = new PulseMotionControl();
            control.ReadTilt(Gravity(0), 2);
            Assert.That(control.ReadTilt(Gravity(sign * 70), 2), Is.EqualTo(sign).Within(.0001f));
            float returning = control.ReadTilt(Gravity(sign * 69), 2);
            Assert.That(Mathf.Abs(returning), Is.LessThan(.97f));
            Assert.That(Mathf.Sign(returning), Is.EqualTo(sign));
        }

        [Test] public void AmountOfOverTiltDoesNotIncreaseReturnDelay()
        {
            var a = new PulseMotionControl(); var b = new PulseMotionControl();
            a.ReadTilt(Gravity(0), 2); b.ReadTilt(Gravity(0), 2);
            a.ReadTilt(Gravity(35), 2); b.ReadTilt(Gravity(80), 2);
            float first = a.ReadTilt(Gravity(34), 2), second = b.ReadTilt(Gravity(79), 2);
            Assert.That(second, Is.EqualTo(first).Within(.0001f));
        }

        [Test] public void HeldTiltDoesNotCreepAndResetRecalibrates()
        {
            var control = new PulseMotionControl(); control.ReadTilt(Gravity(12), 2);
            float held = control.ReadTilt(Gravity(17), 2);
            for (int i = 0; i < 120; i++) Assert.That(control.ReadTilt(Gravity(17), 2), Is.EqualTo(held).Within(.0001f));
            control.ReadTilt(Gravity(80), 2); control.Reset();
            Assert.That(control.ReadTilt(Gravity(0), 2), Is.Zero);
        }

        [Test] public void InvalidInitialSampleDoesNotCalibrate()
        {
            var control = new PulseMotionControl();
            Assert.That(control.ReadTilt(Vector3.zero, 1), Is.Zero);
            Assert.That(control.Calibrated, Is.False);
            Assert.That(control.ReadTilt(Gravity(25), 1), Is.Zero);
            Assert.That(control.Calibrated, Is.True);
        }

        [Test] public void TiltUsesGravityDirectionRatherThanItsMagnitude()
        {
            var a = new PulseMotionControl(); var b = new PulseMotionControl();
            a.ReadTilt(Gravity(0), 1); b.ReadTilt(Gravity(0) * 9.81f, 1);
            Assert.That(a.ReadTilt(Gravity(10), 1), Is.EqualTo(b.ReadTilt(Gravity(10) * 9.81f, 1)).Within(.0001f));
        }

        [TestCase(.5f)] [TestCase(1f)] [TestCase(2f)]
        public void TwistAtRestDoesNotFeedSensitivityBackIntoItsStoredCommand(float sensitivity)
        {
            var control = new PulseMotionControl();
            float command = control.ReadTwist(-.5f, .1f, sensitivity);
            for (int i = 0; i < 180; i++)
                Assert.That(control.ReadTwist(0, 1f / 60, sensitivity), Is.EqualTo(command).Within(.00001f));
        }

        [TestCase(-1f)] [TestCase(1f)]
        public void TwistDiscardsExcessRotationAtTheLimit(float sign)
        {
            var control = new PulseMotionControl();
            for (int i = 0; i < 60; i++) control.ReadTwist(-sign * 3, 1f / 60, 2);
            float returning = control.ReadTwist(sign * .1f, 1f / 60, 2);
            Assert.That(Mathf.Abs(returning), Is.LessThan(1));
            Assert.That(Mathf.Sign(returning), Is.EqualTo(sign));
        }

        [Test] public void SmoothingIsFrameRateIndependentAndReleasesImmediately()
        {
            float a = 0, b = 0;
            for (int i = 0; i < 30; i++) a = PulseMotionControl.Smooth(a, .75f, 1f / 30);
            for (int i = 0; i < 120; i++) b = PulseMotionControl.Smooth(b, .75f, 1f / 120);
            Assert.That(a, Is.EqualTo(b).Within(.0001f));
            Assert.That(PulseMotionControl.Smooth(1, .8f, 1f / 60), Is.LessThan(1));
            float reverse = 1;
            for (int i = 0; i < 3; i++) reverse = PulseMotionControl.Smooth(reverse, -1, 1f / 60);
            Assert.That(reverse, Is.LessThan(0));
        }
    }
}
