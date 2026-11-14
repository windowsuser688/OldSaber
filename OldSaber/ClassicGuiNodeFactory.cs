using TMPro;
using UnityEngine;
namespace OldSaber
{
    internal static class ClassicGuiNodeFactory
    {
        internal static GameObject Create(string name,bool rect,bool hasText,bool uiText,out TMP_Text text)
        {
            var go=rect?new GameObject(name,typeof(RectTransform)):new GameObject(name);
            // TextMeshPro's font setter accesses renderer caches initialized in
            // Awake. Add text while active, before joining the inactive HUD tree.
            text=hasText?(uiText?(TMP_Text)go.AddComponent<TextMeshProUGUI>():go.AddComponent<TextMeshPro>()):null;
            return go;
        }
    }
}
