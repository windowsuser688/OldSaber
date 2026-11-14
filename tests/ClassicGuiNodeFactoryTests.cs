using System;
using OldSaber;
namespace UnityEngine
{
    public class Transform{}
    public class RectTransform:Transform{}
    public class GameObject
    {
        public bool activeInHierarchy=true;
        public GameObject(string name,params Type[] components)
        {foreach(var t in components)if(t==typeof(Transform))throw new Exception("Duplicate Transform requested");}
        public T AddComponent<T>() where T:TMPro.TMP_Text,new(){var text=new T();text.awake=activeInHierarchy;return text;}
    }
}
namespace TMPro
{
    public class TMP_Text
    {
        public bool awake;
        public object font {set{if(!awake)throw new Exception("Renderer cache not initialized before assigning font");}}
    }
    public class TextMeshPro:TMP_Text{}
    public class TextMeshProUGUI:TMP_Text{}
}
public static class ClassicGuiNodeFactoryTests
{
    public static void Main()
    {
        foreach(bool ui in new[]{false,true})
        {
            var go=ClassicGuiNodeFactory.Create("Score",true,true,ui,out var text);
            go.activeInHierarchy=false; // Now parent beneath the inactive HUD.
            text.font=new object();
            if(!text.awake)throw new Exception("Text was created before Awake could run");
        }
        ClassicGuiNodeFactory.Create("GameHUD",false,false,false,out var empty);
        if(empty!=null)throw new Exception("Plain transform unexpectedly got text");
        Console.WriteLine("PASS: both HUD text types initialize before inactive parenting/font assignment; no duplicate Transform.");
    }
}
