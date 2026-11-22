using UnityEngine;
using UnityEngine.EventSystems;

namespace Pulse
{
    public sealed class PulseButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform Visual { private get; set; }
        float target = 1;
        public void OnPointerDown(PointerEventData e) { target = .96f; }
        public void OnPointerUp(PointerEventData e) { target = 1; }
        public void OnPointerExit(PointerEventData e) { target = 1; }
        void OnDisable()
        {
            target = 1;
            if (Visual != null) Visual.localScale = Vector3.one;
        }

        void Update()
        {
            if (Visual == null) return;
            Visual.localScale = PulseSettings.ReducedMotion
                ? Vector3.one
                : Vector3.Lerp(Visual.localScale, Vector3.one * target, 1 - Mathf.Exp(-25 * Time.unscaledDeltaTime));
        }
    }
}
