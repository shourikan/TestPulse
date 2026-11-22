using UnityEngine;

namespace Pulse
{
    public static class PulsePalette
    {
        public static readonly Color Void = Hex("12001B"), Panel = Hex("20152F"), Line = Hex("49345E");
        public static readonly Color Mint = Hex("54F1AA"), Violet = Hex("BB77FF"), Gold = Hex("FFC461");
        public static readonly Color Blue = Hex("53C7FF"), Pink = Hex("FF6A96"), Ink = Hex("F3EDF9"), Muted = Hex("BEB0CD");
        public static Color Alpha(Color c, float a) { c.a = a; return c; }
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
    }
}
