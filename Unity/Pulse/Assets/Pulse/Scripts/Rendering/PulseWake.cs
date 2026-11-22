using UnityEngine;

namespace Pulse
{
    // Samples the travelled path at fixed distances. Existing marks move with the
    // field; steering never rotates or pulls the whole tail towards a new angle.
    public sealed class PulseWake
    {
        readonly Vector2[] positions;
        readonly float spacing;
        float distanceSinceSample;
        public int Count { get; private set; }
        public Vector2 this[int index] => positions[index];

        public PulseWake(int capacity, float spacing)
        {
            positions = new Vector2[capacity];
            this.spacing = spacing;
            Reset();
        }

        public void Reset()
        {
            System.Array.Clear(positions, 0, positions.Length);
            Count = 1;
            distanceSinceSample = 0;
        }

        public void Advance(Vector2 displacement)
        {
            float distance = displacement.magnitude;
            if (distance <= .000001f) return;
            for (int i = 0; i < Count; i++) positions[i] += displacement;

            float nextSample = spacing - distanceSinceSample;
            for (; nextSample <= distance; nextSample += spacing)
            {
                Count = Mathf.Min(Count + 1, positions.Length);
                for (int i = Count - 1; i > 0; i--) positions[i] = positions[i - 1];
                // This mark was emitted partway through the frame and has only
                // travelled the remaining fraction of the field displacement.
                positions[0] = displacement * (1 - nextSample / distance);
            }
            distanceSinceSample = distance - (nextSample - spacing);
        }
    }
}
