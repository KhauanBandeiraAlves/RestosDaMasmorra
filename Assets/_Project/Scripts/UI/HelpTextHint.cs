using UnityEngine;

namespace RestosDaMasmorra.UI
{
    // Minimal first-time control hint: stays fully visible for a few seconds, then fades
    // out and deactivates. No multi-step tutorial, no player input required to dismiss it.
    public class HelpTextHint : MonoBehaviour
    {
        [SerializeField] float visibleSeconds = 6f;
        [SerializeField] float fadeSeconds = 1.5f;

        CanvasGroup group;
        float timer;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < visibleSeconds) return;

            float fadeT = (timer - visibleSeconds) / fadeSeconds;
            group.alpha = Mathf.Clamp01(1f - fadeT);
            if (fadeT >= 1f) gameObject.SetActive(false);
        }
    }
}
