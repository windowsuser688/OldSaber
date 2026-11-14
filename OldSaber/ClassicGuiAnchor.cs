using TMPro;
using UnityEngine;
namespace OldSaber
{
    internal sealed class ClassicGuiAnchor
    {
        private readonly Transform group;
        private readonly RectTransform classic,native;
        private readonly TMP_Text classicText,nativeText;
        private readonly Vector3 baseScale;
        private readonly Quaternion relativeRotation;
        private readonly bool matchSize;
        internal ClassicGuiAnchor(Transform group,RectTransform classic,RectTransform native,TMP_Text classicText=null,TMP_Text nativeText=null,bool matchSize=true)
        {
            this.group=group;this.classic=classic;this.native=native;
            this.classicText=classicText;this.nativeText=nativeText;
            this.matchSize=matchSize;
            baseScale=group.localScale;
            relativeRotation=Quaternion.Inverse(group.rotation)*classic.rotation;
        }
        private static float Height(TMP_Text text)
        {
            var face=text.font.faceInfo;
            return ClassicGuiSizing.TextHeight(text.fontSize,face.pointSize,face.scale,face.capLine>0?face.capLine:face.ascentLine,text.transform.lossyScale.y,text.isOrthographic);
        }
        internal void Update()
        {
            if(!group || !classic || !native)return;
            // Reset before measuring so repeated updates cannot compound scaling.
            group.localScale=baseScale;
            group.rotation=native.rotation*Quaternion.Inverse(relativeRotation);
            if(matchSize)
            {
                float desired,current;
                if(classicText && nativeText){desired=Height(nativeText);current=Height(classicText);}
                else {desired=Mathf.Abs(native.rect.width*native.lossyScale.x);current=Mathf.Abs(classic.rect.width*classic.lossyScale.x);}
                group.localScale=baseScale*ClassicGuiSizing.Ratio(desired,current);
            }
            group.position+=native.TransformPoint(native.rect.center)-classic.TransformPoint(classic.rect.center);
        }
    }
}
