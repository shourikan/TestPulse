using NUnit.Framework;
using UnityEngine;

namespace Pulse.Tests
{
    public class PulseWakeTests
    {
        [Test] public void WakeFollowsTravelDistanceAcrossFrameRates()
        {
            var a = new PulseWake(20, .065f);
            var b = new PulseWake(20, .065f);
            Vector2 velocity = new Vector2(-1, -1).normalized * 3;
            for (int i = 0; i < 30; i++) a.Advance(velocity / 30);
            for (int i = 0; i < 120; i++) b.Advance(velocity / 120);
            Assert.That(a.Count, Is.EqualTo(20));
            Assert.That(b.Count, Is.EqualTo(20));
            for (int i = 0; i < a.Count; i++)
                Assert.That(Vector2.Distance(a[i], b[i]), Is.LessThan(.0001f));
        }

        [Test] public void TurningPreservesOlderPathInsteadOfRotatingTheWholeTail()
        {
            var wake = new PulseWake(4, 1);
            wake.Advance(Vector2.down * 2);
            wake.Advance(Vector2.right);
            Assert.That(wake[0], Is.EqualTo(Vector2.zero));
            Assert.That(wake[1], Is.EqualTo(Vector2.right));
            Assert.That(wake[2], Is.EqualTo(new Vector2(1, -1)));
            Assert.That(wake[3], Is.EqualTo(new Vector2(1, -2)));
        }

        [Test] public void PausingFreezesWakeAndRestartClearsItsHistory()
        {
            var wake = new PulseWake(20, .065f);
            wake.Advance(Vector2.down);
            Vector2 oldest = wake[wake.Count - 1];
            wake.Advance(Vector2.zero);
            Assert.That(wake[wake.Count - 1], Is.EqualTo(oldest));
            wake.Reset();
            Assert.That(wake.Count, Is.EqualTo(1));
            Assert.That(wake[0], Is.EqualTo(Vector2.zero));
        }
    }
}
