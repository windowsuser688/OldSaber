using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config.Stores;
using IPA.Utilities;
using SiraUtil.Zenject;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Newtonsoft.Json.Linq;
using System.IO;

namespace OldSaber
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public sealed class Plugin
    {
        internal static IPA.Logging.Logger Log;
        internal static PluginConfig Config;
        internal static bool Active;
        private GameObject host;
        private ClassicDebrisPatches debrisPatches;
        [Init] public Plugin(IPA.Logging.Logger logger, IPA.Config.Config config, Zenjector zenjector)
        {
            Log=logger; Config=config.Generated<PluginConfig>();
            CleanupLegacyConfig();
            zenjector.UseLogger(logger);
            zenjector.Install(Location.App,c=>c.BindInstance(Config).AsSingle());
            zenjector.Install(Location.Menu,c=>c.BindInterfacesTo<SettingsView>().AsSingle());
            zenjector.Install<OldNotesInstaller>(Location.Player | Location.Tutorial);
        }

        private static void CleanupLegacyConfig()
        {
            var path = Path.Combine(UnityGame.UserDataPath, "OldSaber.json");
            if (!File.Exists(path)) return;

            try
            {
                var json = JObject.Parse(File.ReadAllText(path));
                if (json.Property("OldMenu") == null)
                    return;

                json.Remove("OldMenu");
                File.WriteAllText(path, json.ToString());
                Log?.Info("Removed legacy OldMenu setting from UserData/OldSaber.json");
            }
            catch (Exception ex)
            {
                Log?.Warn($"Failed to remove legacy OldMenu config entry: {ex.Message}");
            }
        }

        [OnEnable] public void Enable()
        {
            Active=true;
            OldNotesAssets.Load();
            debrisPatches = new ClassicDebrisPatches();
            debrisPatches.Enable();
            host = new GameObject("OldSaber.Host");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<EnvironmentHost>();
            host.AddComponent<ClassicGuiHost>();
        }
        [OnDisable] public void Disable() { Active=false; ClassicBomb.Restore(); OldNotesAssets.Restore(); debrisPatches?.Dispose(); debrisPatches=null; if (host) Object.Destroy(host); }
    }

    internal static class Fields
    {
        internal static FieldInfo Find(Type type, string name)
        {
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
                type = type.BaseType;
            }
            throw new MissingFieldException(name);
        }
        internal static T Get<T>(object obj, string name) { return (T)Find(obj.GetType(), name).GetValue(obj); }
        internal static void Set(object obj, string name, object value) { Find(obj.GetType(), name).SetValue(obj, value); }
    }

    public sealed class EnvironmentHost : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<ClassicStage> stages = new System.Collections.Generic.List<ClassicStage>();
        private readonly System.Collections.Generic.HashSet<int> pending = new System.Collections.Generic.HashSet<int>();
        private void OnEnable()
        {
            SceneManager.sceneLoaded += Loaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) Loaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }
        private void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (Plugin.Active && Plugin.Config.OldEnvironment && SceneCatalog.ResourceFor(scene.name) != null && pending.Add(scene.handle)) StartCoroutine(Install(scene));
        }
        private IEnumerator Install(Scene scene)
        {
            // Scene callbacks precede Zenject injection and Start. Wait for usable dependencies.
            yield return null;
            for (int attempt = 0; attempt < 300 && Plugin.Active && Plugin.Config.OldEnvironment && scene.IsValid() && scene.isLoaded; attempt++)
            {
                var effects = Resources.FindObjectsOfTypeAll<LightSwitchEventEffect>()
                    .Where(x => x.gameObject.scene == scene).ToArray();
                var sample = effects.FirstOrDefault();
                if (sample != null && Fields.Get<BeatmapCallbacksController>(sample, "_beatmapCallbacksController") != null)
                {
                    GameObject root = null;
                    try
                    {
                        root = new GameObject("OldSaber.Restored");
                        root.SetActive(false);
                        SceneManager.MoveGameObjectToScene(root, scene);
                        var stage = root.AddComponent<ClassicStage>();
                        stage.Build(scene, effects);
                        stages.RemoveAll(s => !s);
                        stages.Add(stage);
                        root.SetActive(true);
                        stage.Commit();
                        Plugin.Log.Info("Classic 0.10.1 " + scene.name + " loaded.");
                    }
                    catch (Exception ex)
                    {
                        if (root) Object.Destroy(root);
                        Plugin.Log.Error("Classic environment failed; original visuals retained/restored. " + ex);
                    }
                    pending.Remove(scene.handle);
                    yield break;
                }
                yield return null;
            }
            pending.Remove(scene.handle);
            Plugin.Log.Warn(scene.name + " unloaded or lighting dependencies unavailable; replacement skipped.");
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= Loaded;
            StopAllCoroutines();
            pending.Clear();
            foreach (var stage in stages) if (stage) Object.Destroy(stage.gameObject);
            stages.Clear();
        }
    }
}
