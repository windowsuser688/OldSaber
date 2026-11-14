using UnityEngine;
namespace OldSaber
{
    internal static class ClassicGuiLayout
    {
        internal static bool StaticVisible(string name,bool sourceActive)=>sourceActive &&
            name!="TextOverlayPanelOutline0" && name!="TextOverlayPanelOutline1" && name!="AnimationCircle";
        internal static void Rect(RectTransform rect,Vector2 min,Vector2 max,Vector2 pivot,Vector2 size,Vector2 anchored,float z)
        {
            rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.sizeDelta=size;
            rect.anchoredPosition3D=new Vector3(anchored.x,anchored.y,z);
        }
    }
}
