using TMPro;
using UnityEngine;

namespace Pulse
{
    // Recycled sprites and particles keep the scrolling field allocation-free.
    public sealed class PulseWorldView : MonoBehaviour
    {
        sealed class Piece
        {
            public Transform Root;
            public SpriteRenderer Core, Halo;
        }
        sealed class Spark
        {
            public SpriteRenderer Sprite;
            public Vector2 Position, Velocity;
            public float Life, Duration, Size;
            public Color Color;
            public bool Bubble;
        }
        sealed class Floater
        {
            public TextMeshPro Text;
            public Vector2 Position, Velocity;
            public float Life, Duration;
        }
        Piece[] pieces;
        Piece player;
        SpriteRenderer shieldRing;
        LineRenderer pulseArc;
        readonly LineRenderer[] chargeArcs = new LineRenderer[PulseSimulation.ShieldsToActivate];
        const float InnerShieldRadius = .46f;
        const float FloaterParallax = .7f;
        const float MaxScoreFontSize = 4.4f;
        const float ScoreFontGrowthSeconds = 80;
        readonly Spark[] sparks = new Spark[160];
        readonly Floater[] floaters = new Floater[12];
        readonly SpriteRenderer[] stars = new SpriteRenderer[60], trail = new SpriteRenderer[20];
        readonly Vector2[] starPositions = new Vector2[60];
        readonly PulseWake wake = new PulseWake(20, .065f);
        Vector2 previousFieldOffset;
        Vector2 deathWakeOffset;
        Sprite circle, ring, glow, diamond, spike, shield;
        Material material;
        Transform field;
        int sparkIndex, floaterIndex;
        float pop, shake, clock, deathAge;
        public int ActiveParticles { get; private set; }

        public void Initialize()
        {
            circle = Resources.Load<Sprite>("Visuals/Circle"); ring = Resources.Load<Sprite>("Visuals/Ring");
            glow = Resources.Load<Sprite>("Visuals/Glow"); diamond = Resources.Load<Sprite>("Visuals/Diamond");
            spike = Resources.Load<Sprite>("Visuals/Spike");
            shield = Resources.Load<Sprite>("Visuals/Shield");
            material = Resources.Load<Material>("Visuals/Unlit");
            var backdrop = NewSprite("Field atmosphere", glow, PulsePalette.Alpha(PulsePalette.Line, .33f), transform, -30);
            backdrop.transform.localScale = new Vector3(29, 64, 1);
            field = new GameObject("Field").transform; field.SetParent(transform, false);
            pieces = new Piece[PulseSimulation.ActorCount];
            for (int i = 0; i < pieces.Length; i++)
                pieces[i] = MakePiece("Actor " + i, i < 50 ? ActorKind.Hazard : i < 60 ? ActorKind.Gold : ActorKind.Shield);
            player = MakePiece("Player", ActorKind.Shield);
            player.Core.sprite = circle; player.Core.color = PulsePalette.Mint;
            player.Core.transform.localScale = Vector3.one * .54f;
            player.Halo.color = PulsePalette.Alpha(PulsePalette.Mint, .26f);
            player.Halo.transform.localScale = Vector3.one * 1.25f;
            shieldRing = NewSprite("Shield field", ring, PulsePalette.Blue, field, 5);
            shieldRing.transform.localScale = Vector3.one * 1.28f;
            pulseArc = NewArc("Pulse countdown arc", 60);
            pulseArc.startColor = pulseArc.endColor = Color.Lerp(PulsePalette.Blue, PulsePalette.Ink, .45f);
            float slice = 2 * Mathf.PI / chargeArcs.Length;
            const float gap = .30f;
            for (int i = 0; i < chargeArcs.Length; i++)
            {
                chargeArcs[i] = NewArc("Shield charge " + (i + 1), 20);
                SetArc(chargeArcs[i], Mathf.PI * .5f - i * slice - gap * .5f, slice - gap);
            }
            for (int i = 0; i < stars.Length; i++)
            {
                starPositions[i] = new Vector2(Random.value, Random.value);
                stars[i] = NewSprite("Star " + i, circle, PulsePalette.Alpha(PulsePalette.Muted, Random.Range(.24f, .57f)), transform, -20);
                stars[i].transform.localScale = Vector3.one * Random.Range(.027f, .063f);
            }
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i] = NewSprite("Wake " + i, circle, PulsePalette.Mint, field, 0);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = new Spark { Sprite = NewSprite("Spark " + i, circle, PulsePalette.Mint, field, 15) };
                sparks[i].Sprite.enabled = false;
            }
            for (int i = 0; i < floaters.Length; i++)
            {
                var go = new GameObject("Feedback " + i);
                go.transform.SetParent(field, false);
                var t = go.AddComponent<TextMeshPro>();
                t.fontSize = 3; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
                t.enableAutoSizing = true;
                t.fontSizeMin = 2;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.rectTransform.sizeDelta = new Vector2(5, .8f);
                t.GetComponent<MeshRenderer>().sortingOrder = 20;
                t.enabled = false; floaters[i] = new Floater { Text = t };
            }
        }

        Piece MakePiece(string name, ActorKind kind)
        {
            var root = new GameObject(name).transform; root.SetParent(field, false);
            Color color = kind == ActorKind.Hazard ? PulsePalette.Violet : kind == ActorKind.Gold ? PulsePalette.Gold : PulsePalette.Blue;
            var p = new Piece { Root = root };
            p.Halo = NewSprite("Halo", glow, PulsePalette.Alpha(color, .13f), root, 1);
            p.Halo.transform.localScale = Vector3.one * (kind == ActorKind.Hazard ? .74f : 1f);
            p.Core = NewSprite("Core", kind == ActorKind.Hazard ? spike : kind == ActorKind.Gold ? diamond : shield, color, root, 3);
            p.Core.transform.localScale = Vector3.one * (kind == ActorKind.Hazard ? .46f : .58f);
            return p;
        }
        SpriteRenderer NewSprite(string name, Sprite sprite, Color color, Transform parent, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        public void ResetRun()
        {
            pop = shake = deathAge = 0;
            ActiveParticles = 0;
            foreach (var spark in sparks) { spark.Life = 0; spark.Sprite.enabled = false; }
            foreach (var floater in floaters) { floater.Life = 0; floater.Text.enabled = false; }
            wake.Reset(); previousFieldOffset = deathWakeOffset = Vector2.zero;
        }

        public void Render(PulseSimulation model, float dt, bool settingsOpen = false)
        {
            bool playing = model.State == RunState.Playing;
            bool paused = model.State == RunState.Paused;
            bool title = model.State == RunState.Title;
            bool ended = model.State == RunState.Ended;
            bool reducedMotion = PulseSettings.ReducedMotion;
            field.gameObject.SetActive(!settingsOpen);
            float visualDt = paused || settingsOpen ? 0 : ended && !reducedMotion ? Mathf.Min(dt, .05f) * .45f : dt;
            clock += visualDt;
            if (ended) deathAge += visualDt;
            pop = Mathf.MoveTowards(pop, 0, visualDt * 5);
            shake = Mathf.MoveTowards(shake, 0, visualDt * 5);
            field.localPosition = reducedMotion ? Vector3.zero : new Vector3(Mathf.Sin(clock * 93), Mathf.Cos(clock * 73)) * shake * .06f;
            Vector2 starArea = new Vector2(11.2f, model.HalfHeight * 2 + .4f);
            Vector2 starFlow = playing ? model.ScrollDirection * (model.Speed * .14f) : new Vector2(.08f, -.48f);
            for (int i = 0; i < stars.Length; i++)
            {
                if (!reducedMotion)
                {
                    Vector2 step = starFlow * (Mathf.Min(dt, .05f) * (.4f + i % 4 * .18f));
                    starPositions[i].x = Mathf.Repeat(starPositions[i].x + step.x / starArea.x, 1);
                    starPositions[i].y = Mathf.Repeat(starPositions[i].y + step.y / starArea.y, 1);
                }
                stars[i].transform.localPosition = Vector2.Scale(starPositions[i] - Vector2.one * .5f, starArea);
            }
            if (settingsOpen) return;
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i]; var actor = model.Actors[i];
                Vector2 preview = TitlePiece(i);
                p.Root.gameObject.SetActive(!title || preview != Vector2.zero);
                p.Root.localPosition = title ? preview : actor.Position;
                p.Root.localScale = Vector3.one * (title ? 1.9f : 1);
            }
            player.Root.gameObject.SetActive(!ended);
            player.Root.localPosition = Vector2.zero;
            float feedbackScale = reducedMotion ? 0 : pop * .21f;
            player.Root.localScale = Vector3.one * (title ? 1.72f : 1 + feedbackScale);
            RenderProtection(model, title);
            if (playing) wake.Advance(model.FieldOffset - previousFieldOffset);
            if (ended) deathWakeOffset += model.FieldOffset - previousFieldOffset;
            previousFieldOffset = model.FieldOffset;
            float wakeOpacity = ended ? Mathf.Clamp01(1 - deathAge * 1.9f) : 1;
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i].enabled = title || wakeOpacity > 0 && i < wake.Count;
                if (!trail[i].enabled) continue;
                float t = (i + 1f) / trail.Length;
                trail[i].transform.localPosition = title
                    ? new Vector2(-2.3f * t * t, -2.75f * t + .72f * t * t)
                    : wake[i] + deathWakeOffset;
                trail[i].transform.localScale = Vector3.one * Mathf.Lerp(title ? .43f : .26f, .025f, t);
                trail[i].color = PulsePalette.Alpha(PulsePalette.Mint, Mathf.Lerp(.8f, .1f, t) * wakeOpacity);
            }
            ActiveParticles = 0;
            foreach (var spark in sparks)
            {
                if (spark.Life <= 0) continue;
                spark.Life = Mathf.Max(0, spark.Life - visualDt);
                spark.Sprite.enabled = spark.Life > 0;
                if (spark.Life <= 0) continue;
                ActiveParticles++;
                if (!reducedMotion)
                {
                    spark.Position += spark.Velocity * visualDt;
                    spark.Velocity *= Mathf.Exp((spark.Bubble ? -1.55f : -3) * visualDt);
                    if (spark.Bubble) spark.Velocity += Vector2.down * (.63f * visualDt);
                }
                spark.Sprite.transform.localPosition = spark.Position + model.FieldOffset;
                float f = spark.Life / spark.Duration;
                float size = spark.Bubble ? Mathf.Lerp(.36f, 1, Mathf.SmoothStep(0, 1, f)) : f;
                float alpha = spark.Bubble ? Mathf.SmoothStep(0, 1, Mathf.Min(1, f * 2)) * spark.Color.a : f;
                spark.Sprite.transform.localScale = Vector3.one * spark.Size * size;
                spark.Sprite.color = PulsePalette.Alpha(spark.Color, alpha);
            }
            foreach (var f in floaters)
            {
                if (f.Life <= 0) continue;
                f.Life = Mathf.Max(0, f.Life - visualDt); f.Text.enabled = f.Life > 0;
                if (!reducedMotion)
                {
                    f.Position += f.Velocity * visualDt;
                    f.Velocity *= Mathf.Exp(-2.4f * visualDt);
                }
                f.Text.transform.localPosition = f.Position + model.FieldOffset * FloaterParallax;
                f.Text.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(f.Life / f.Duration * 2.2f));
            }
        }

        static Vector2 TitlePiece(int index) => index switch
        {
            0 => new Vector2(-2.18f, 1.35f),
            1 => new Vector2(2.06f, -1.65f),
            50 => new Vector2(1.50f, 2.27f),
            60 => new Vector2(2.55f, .44f),
            _ => Vector2.zero
        };

        void RenderProtection(PulseSimulation model, bool title)
        {
            bool inRun = !title && model.State != RunState.Ended;
            bool active = inRun && model.PulseLeft > 0;
            shieldRing.enabled = inRun && (active || model.Shields > 0);
            shieldRing.color = PulsePalette.Alpha(PulsePalette.Blue, active ? .85f : .42f);
            bool charged = model.Shields == chargeArcs.Length;
            bool showCharge = inRun && !active && model.Shields > 0;
            for (int i = 0; i < chargeArcs.Length; i++)
            {
                var arc = chargeArcs[i];
                arc.enabled = showCharge;
                if (!arc.enabled) continue;
                Color fill = charged ? Color.Lerp(PulsePalette.Blue, PulsePalette.Ink, .35f) : PulsePalette.Blue;
                arc.startColor = arc.endColor = i < model.Shields ? fill : PulsePalette.Alpha(PulsePalette.Muted, .28f);
            }
            pulseArc.enabled = active;
            if (!active) return;
            float sweep = Mathf.PI * 2 * model.PulseLeft / PulseSimulation.PulseDuration;
            SetArc(pulseArc, Mathf.PI * .5f, sweep);
        }

        LineRenderer NewArc(string name, int segments)
        {
            var arc = new GameObject(name).AddComponent<LineRenderer>();
            arc.transform.SetParent(field, false);
            arc.useWorldSpace = false;
            arc.sharedMaterial = material;
            arc.sortingOrder = 6;
            arc.widthMultiplier = .071f;
            arc.numCapVertices = 4;
            arc.positionCount = segments + 1;
            arc.enabled = false;
            return arc;
        }

        static void SetArc(LineRenderer arc, float start, float sweep)
        {
            int segments = arc.positionCount - 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = start - sweep * i / segments;
                arc.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * InnerShieldRadius);
            }
        }

        public void Feedback(PulseSimulation model, FeedbackKind kind, Vector2 position, long value)
        {
            bool shieldLost = kind == FeedbackKind.ShieldBreak;
            bool breakthrough = kind == FeedbackKind.Impact || shieldLost;
            Color color = kind == FeedbackKind.Gold || kind == FeedbackKind.Record ? PulsePalette.Gold : PulsePalette.Blue;
            // Convert the sampled contact point to field coordinates before the next simulation tick.
            Vector2 origin = position - model.FieldOffset;
            Vector2 playerOrigin = -model.FieldOffset;
            if (kind == FeedbackKind.Death)
            {
                Splash(playerOrigin, PulsePalette.Mint, 23, 1.3f, -model.ScrollDirection * .7f);
                Splash(origin, PulsePalette.Violet, 15, 1.05f, model.ScrollDirection * .3f);
            }
            else if (breakthrough)
            {
                Splash(origin, PulsePalette.Violet, 16, 1, -model.ScrollDirection * .45f);
                if (shieldLost) Splash(playerOrigin, PulsePalette.Blue, 11, 1, Vector2.zero);
            }
            else if (kind == FeedbackKind.Gold || kind == FeedbackKind.Shield)
            {
                Splash(origin, color, 16, 1.1f, -model.ScrollDirection * .32f, pickup: true);
            }
            else if (!PulseSettings.ReducedMotion)
            {
                int count = kind == FeedbackKind.Pulse || kind == FeedbackKind.Record ? 24 : 10;
                for (int i = 0; i < count; i++)
                {
                    var s = NextSpark(circle, origin, color, false);
                    s.Velocity = Random.insideUnitCircle.normalized * Random.Range(.8f, 3.5f);
                    s.Duration = s.Life = Random.Range(.3f, .8f);
                    s.Size = Random.Range(.045f, .13f);
                }
            }
            pop = 1;
            if (shieldLost) shake = 1;
            Vector2 labelOrigin = position - model.FieldOffset * FloaterParallax;
            if (kind == FeedbackKind.Gold || breakthrough)
            {
                float fontSize = Mathf.Lerp(3, MaxScoreFontSize, (float)(model.Elapsed / ScoreFontGrowthSeconds));
                ShowFloater(labelOrigin, "+" + value.ToString("N0"), kind == FeedbackKind.Gold ? PulsePalette.Gold : PulsePalette.Violet, fontSize);
            }
            if (kind == FeedbackKind.Shield && value > 0)
                ShowFloater(labelOrigin, "+1", PulsePalette.Blue, 3);
            if (shieldLost)
                ShowFloater(labelOrigin, "-1", PulsePalette.Blue, 3);
        }

        void ShowFloater(Vector2 position, string text, Color color, float fontSize)
        {
            float side = floaterIndex % 2 == 0 ? 1 : -1;
            var f = floaters[floaterIndex++ % floaters.Length];
            f.Duration = f.Life = .88f;
            f.Velocity = new Vector2(side * 1.45f, 1.2f);
            f.Position = position + new Vector2(side * .34f, .72f);
            for (int pass = 0; pass < 5; pass++)
            {
                bool overlaps = false;
                foreach (var other in floaters)
                    if (other != f && other.Life > .25f && Mathf.Abs(other.Position.x - f.Position.x) < 1.6f && Mathf.Abs(other.Position.y - f.Position.y) < .5f)
                        overlaps = true;
                if (!overlaps) break;
                f.Position.y += .51f;
            }
            f.Text.text = text;
            f.Text.fontSizeMax = fontSize;
            f.Text.color = Color.Lerp(color, PulsePalette.Ink, .2f);
            f.Text.enabled = true;
        }

        void Splash(Vector2 position, Color color, int count, float scale, Vector2 carry, bool pickup = false)
        {
            bool reducedMotion = PulseSettings.ReducedMotion;
            if (reducedMotion) count = 7;
            float phase = Random.value * Mathf.PI * 2;
            for (int i = 0; i < count; i++)
            {
                float angle = phase + (i + Random.value) / count * Mathf.PI * 2;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                bool outlined = i % 3 != 1;
                Color tint = Color.Lerp(color, PulsePalette.Ink, Random.Range(.06f, .34f));
                var s = NextSpark(outlined ? ring : circle, position + direction * Random.Range(.1f, .46f) * scale,
                    PulsePalette.Alpha(tint, outlined ? 1 : Random.Range(.64f, .89f)), true);
                s.Velocity = carry + direction * Random.Range(.75f, 3.4f) * scale;
                float life = pickup ? Random.Range(.48f, .76f) : Random.Range(.85f, 1.3f);
                s.Duration = s.Life = reducedMotion ? .52f : life * scale;
                s.Size = Random.Range(.14f, outlined ? .43f : .28f) * scale;
            }
        }

        Spark NextSpark(Sprite sprite, Vector2 position, Color color, bool bubble)
        {
            var spark = sparks[sparkIndex++ % sparks.Length];
            spark.Position = position;
            spark.Color = color;
            spark.Bubble = bubble;
            spark.Sprite.sprite = sprite;
            return spark;
        }
    }
}
