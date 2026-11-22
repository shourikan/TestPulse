using UnityEngine;

namespace Pulse
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PulseSafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int size;
        void LateUpdate()
        {
            var safe = Screen.safeArea;
            if (safe == last && size.x == Screen.width && size.y == Screen.height) return;
            last = safe; size = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
