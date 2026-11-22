using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SetPreview
{
    public static string Portrait(int width = 540, int height = 960)
    {
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
        var sizeType = asm.GetType("UnityEditor.GameViewSize");
        var kind = Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
        var size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { kind, width, height, "Pulse " + width + "x" + height }, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        var viewType = asm.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(view, new object[] { index, null });
        view.Repaint();
        return "Portrait preview " + width + "x" + height;
    }
}
