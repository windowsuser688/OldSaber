using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;
namespace OldSaber
{
    // Restore the old release-info side panel on the actual right-hand screen,
    // instead of treating release notes as only a tab on the central menu.
    public sealed class ClassicReleaseInfo:MonoBehaviour
    {
        private ClassicMenuAssets assets;private NativeMenuMask mask;private RectTransform root;
        private MainMenuViewController main;
        internal void Build(MainMenuViewController owner,TMP_FontAsset fallback)
        {
            main=owner;assets=new ClassicMenuAssets(fallback);mask=new NativeMenuMask(transform);
            root=assets.Panel(transform,"OldSaber.ReleaseInfo");ClassicMenuAssets.Rect(root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,80));
            root.gameObject.SetActive(false);root.gameObject.AddComponent<ClassicMenuNews>().Initialize(assets,true);
            LateUpdate();
        }
        private void LateUpdate()
        {
            if(!root)return;bool visible=main && main.isActiveAndEnabled;
            if(root.gameObject.activeSelf!=visible)root.gameObject.SetActive(visible);
            if(visible){mask.Hide();var p=(RectTransform)transform;root.localScale=Vector3.one*Mathf.Min(p.rect.width/100,p.rect.height/80);}else mask.Restore();
        }
        private void OnDisable(){mask?.Restore();}
        private void OnDestroy(){mask?.Dispose();if(root)Object.Destroy(root.gameObject);assets?.Dispose();}
    }
}
