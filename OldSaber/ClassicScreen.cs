using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using UImage = UnityEngine.UI.Image;
namespace OldSaber
{
    internal sealed class ClassicScreen
    {
        internal GameObject Root;
        internal readonly Dictionary<string,TMP_Text> Texts=new Dictionary<string,TMP_Text>();
        internal readonly Dictionary<string,Button> Buttons=new Dictionary<string,Button>();
        internal ClassicScreen(ClassicMenuAssets assets,string screen,Transform parent,float width,float height)
        {
            var nodes=new Dictionary<string,Transform>();string rootId=(string)assets.Data["screens"][screen];
            foreach(var n in assets.Data["nodes"])
            {
                string id=(string)n["id"],pid=(string)n["parent"],name=(string)n["name"];
                if(id!=rootId && (pid==null || !nodes.ContainsKey(pid)))continue;
                var go=ClassicGuiNodeFactory.Create(name,(bool)n["rect"],n["text"]!=null,true,out var text);
                go.layer=parent.gameObject.layer;go.transform.SetParent(id==rootId?parent:nodes[pid],false);nodes[id]=go.transform;
                if(id==rootId)Root=go;
                if(text)
                {
                    var d=n["text"];text.font=assets.Fonts.Font((string)d["font"]);text.fontSharedMaterial=text.font.material;
                    text.text=(string)d["m_text"];text.fontSize=(float)d["m_fontSize"];text.color=Color.white;
                    text.alignment=(TextAlignmentOptions)(int)d["m_textAlignment"];text.autoSizeTextContainer=false;text.enableAutoSizing=false;
                    text.enableWordWrapping=name=="SongNameText";text.raycastTarget=false;Texts[name]=text;
                }
                if(n["image"]!=null)
                {
                    var d=n["image"];var image=go.AddComponent<UImage>();image.raycastTarget=false;image.color=ClassicGuiState.ColorValue(d["m_Color"]);
                    if(d["texture"]!=null)image.sprite=assets.Sprite((string)d["texture"]);
                    image.type=(UImage.Type)((int?)d["m_Type"]??0);image.preserveAspect=name=="Icon" || (bool?)d["m_PreserveAspect"]==true;
                }
                if(n["button"]!=null){var hit=go.AddComponent<UImage>();hit.color=Color.clear;var b=go.AddComponent<Button>();b.targetGraphic=hit;Buttons[name]=b;}
                ClassicMenuAssets.Layout(go.transform,n);go.SetActive(id==rootId || (bool)n["active"]);
            }
            Canvas.ForceUpdateCanvases();
            foreach(var n in assets.Data["nodes"])if(nodes.TryGetValue((string)n["id"],out var tr))ClassicMenuAssets.Layout(tr,n);
            ClassicMenuAssets.Rect((RectTransform)Root.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,height));
        }
        internal void Set(string name,string value){if(Texts.TryGetValue(name,out var text))text.text=value;}
    }
    internal sealed class NativeMenuMask
    {
        private sealed class Saved {internal CanvasGroup Group;internal bool Owned,Interactable,Raycasts;internal float Alpha;}
        private readonly List<Saved> saved=new List<Saved>();
        internal NativeMenuMask(Transform root)
        {
            foreach(Transform child in root)
            {
                var group=child.GetComponent<CanvasGroup>();bool owned=!group;if(!group)group=child.gameObject.AddComponent<CanvasGroup>();
                saved.Add(new Saved{Group=group,Owned=owned,Alpha=group.alpha,Interactable=group.interactable,Raycasts=group.blocksRaycasts});
            }
        }
        internal void Hide(){foreach(var s in saved)if(s.Group){s.Group.alpha=0;s.Group.interactable=false;s.Group.blocksRaycasts=false;}}
        internal void Restore(){foreach(var s in saved)if(s.Group){s.Group.alpha=s.Alpha;s.Group.interactable=s.Interactable;s.Group.blocksRaycasts=s.Raycasts;}}
        internal void Dispose(){Restore();foreach(var s in saved)if(s.Owned && s.Group)Object.Destroy(s.Group);saved.Clear();}
    }
}
