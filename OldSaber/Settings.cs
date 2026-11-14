using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.MenuButtons;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using Zenject;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OldSaber
{
    public class PluginConfig
    {
        public virtual bool OldEnvironment { get; set; } = true;
        public virtual bool OldNotes { get; set; } = true;
        public virtual bool OldSabers { get; set; } = false;
        public virtual bool OldObstacles { get; set; } = false;
        public virtual bool OldGUI { get; set; } = false;
    }
    internal sealed class SettingsView : IInitializable, IDisposable
    {
        private readonly PluginConfig config;
        private readonly GameplaySetup setup;
        private readonly BSMLSettings settings;
        public SettingsView(PluginConfig config, GameplaySetup setup, BSMLSettings settings, MenuButtons menuButtons)
        {
            this.config=config;this.setup=setup;this.settings=settings;
        }
        [UIValue("old-environment")]
        public bool OldEnvironment {get=>config.OldEnvironment;set=>config.OldEnvironment=value;}
        [UIValue("old-notes")]
        public bool OldNotes {get=>config.OldNotes;set=>config.OldNotes=value;}
        [UIValue("old-sabers")]
        public bool OldSabers {get=>config.OldSabers;set=>config.OldSabers=value;}
        [UIValue("old-obstacles")]
        public bool OldObstacles {get=>config.OldObstacles;set=>config.OldObstacles=value;}
        [UIValue("old-gui")]
        public bool OldGUI {get=>config.OldGUI;set=>config.OldGUI=value;}
        public void Initialize() {setup.AddTab("OldSaber","OldSaber.Settings.bsml",this);settings.AddSettingsMenu("OldSaber","OldSaber.Settings.bsml",this);}
        public void Dispose() {setup.RemoveTab("OldSaber");settings.RemoveSettingsMenu(this);}
    }
}
