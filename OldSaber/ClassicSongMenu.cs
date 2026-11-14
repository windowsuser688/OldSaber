using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace OldSaber
{
    public sealed class ClassicSongMenu:MonoBehaviour
    {
        private ClassicMenuAssets assets;private NativeMenuMask mask;private RectTransform root;
        private StandardLevelDetailView view;private ClassicScreen detail;
        private BeatmapDifficultySegmentedControlController difficulty;
        private BeatmapCharacteristicSegmentedControlController modes;
        private readonly List<KeyValuePair<BeatmapDifficulty,Button>> buttons=new List<KeyValuePair<BeatmapDifficulty,Button>>();
        private Button play,practice,mode,favorite;private float nextUpdate;
        internal void Build(StandardLevelDetailView owner)
        {
            view=owner;assets=new ClassicMenuAssets(view.GetComponentInChildren<TMP_Text>(true).font);
            difficulty=Fields.Get<BeatmapDifficultySegmentedControlController>(view,"_beatmapDifficultySegmentedControlController");
            modes=Fields.Get<BeatmapCharacteristicSegmentedControlController>(view,"_beatmapCharacteristicSegmentedControlController");
            mask=new NativeMenuMask(transform);root=assets.Panel(transform,"OldSaber.ClassicSongSelection");
            ClassicMenuAssets.Rect(root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,80));
            detail=new ClassicScreen(assets,"detail",root,50,80);((RectTransform)detail.Root.transform).anchoredPosition=new Vector2(10,0);
            play=detail.Buttons["PlayButton"];play.onClick.AddListener(()=>view.actionButton.onClick.Invoke());
            var choices=assets.Panel(root,"Difficulty");ClassicMenuAssets.Rect(choices,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-25,0),new Vector2(20,80));
            var heading=assets.Text(choices,"DIFFICULTY",3.2f);ClassicMenuAssets.Rect(heading.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,35),new Vector2(20,6));
            foreach(BeatmapDifficulty value in Enum.GetValues(typeof(BeatmapDifficulty)))
            {
                var d=value;var b=assets.Button(choices,d==BeatmapDifficulty.ExpertPlus?"Expert+":d.ToString(),new Vector2(0,18-buttons.Count*9),new Vector2(19,8),()=>Select(d));
                buttons.Add(new KeyValuePair<BeatmapDifficulty,Button>(d,b));
            }
            favorite=assets.Button(choices,"FAVORITE",new Vector2(0,27),new Vector2(19,6),()=>
            {
                var toggle = Fields.Get<object>(view, "_favoriteToggle");
                var prop = toggle.GetType().GetProperty("isOn");
                if (prop == null) return;
                prop.SetValue(toggle, !(bool)prop.GetValue(toggle));
            });
            practice=assets.Button(choices,"PRACTICE",new Vector2(0,-28),new Vector2(19,7),()=>view.practiceButton.onClick.Invoke());
            mode=assets.Button(choices,"MODE",new Vector2(0,-37),new Vector2(19,7),NextMode);
            foreach(var b in new[]{practice,mode,favorite})b.GetComponentInChildren<TMP_Text>().fontSize=2.8f;
            LateUpdate();
        }
        private void Select(BeatmapDifficulty d)
        {
            var available=Fields.Get<List<BeatmapDifficulty>>(difficulty,"_difficulties");int index=available.IndexOf(d);if(index<0)return;
            var control=Fields.Get<HMUI.TextSegmentedControl>(difficulty,"_difficultySegmentedControl");control.SelectCellWithNumber(index);
            typeof(BeatmapDifficultySegmentedControlController).GetMethod("HandleDifficultySegmentedControlDidSelectCell",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(difficulty,new object[]{control,index});
        }
        private void NextMode()
        {
            var available=Fields.Get<List<BeatmapCharacteristicSO>>(modes,"_currentlyAvailableBeatmapCharacteristics");if(available.Count==0)return;
            int index=(available.IndexOf(modes.selectedBeatmapCharacteristic)+1)%available.Count;
            var control=Fields.Get<HMUI.IconSegmentedControl>(modes,"_segmentedControl");control.SelectCellWithNumber(index);
            typeof(BeatmapCharacteristicSegmentedControlController).GetMethod("HandleBeatmapCharacteristicSegmentedControlDidSelectCell",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(modes,new object[]{control,index});
        }
        private void LateUpdate()
        {
            if(!root)return;mask.Hide();var parent=(RectTransform)transform;float scale=Mathf.Min(parent.rect.width/70,parent.rect.height/80);root.localScale=Vector3.one*scale;
            root.anchoredPosition=new Vector2(-Mathf.Max(0,(parent.rect.width-70*scale)/2)+2,0);
            if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.1f;
            var available=Fields.Get<List<BeatmapDifficulty>>(difficulty,"_difficulties");
            foreach(var pair in buttons){pair.Value.interactable=available.Contains(pair.Key);ClassicMenuAssets.SetSelected(pair.Value,pair.Key==difficulty.selectedDifficulty);}
            play.interactable=view.actionButton.interactable;practice.interactable=view.practiceButton.interactable && view.practiceButton.gameObject.activeInHierarchy;
            mode.GetComponentInChildren<TMP_Text>().text=modes.selectedBeatmapCharacteristic?modes.selectedBeatmapCharacteristic.serializedName:"MODE";
            var favoriteToggle = Fields.Get<object>(view, "_favoriteToggle");
            var favoriteProp = favoriteToggle?.GetType().GetProperty("isOn");
            favorite.GetComponentInChildren<TMP_Text>().text = favoriteProp != null && (bool)favoriteProp.GetValue(favoriteToggle) ? "FAVORITED" : "FAVORITE";
            var level=Fields.Get<BeatmapLevel>(view,"_beatmapLevel");if(level==null)return;
            detail.Set("SongNameText",level.songName+"\n<size=70%>"+level.songAuthorName+"</size>");
            var duration=TimeSpan.FromSeconds(Math.Max(0,level.songDuration));detail.Set("DurationValueText",((int)duration.TotalMinutes)+":"+duration.Seconds.ToString("00"));
            detail.Set("BPMValueText",level.beatsPerMinute.ToString("0.#"));
            var parameters=Fields.Get<LevelParamsPanel>(view,"_levelParamsPanel");
            detail.Set("NotesCountValueText",Fields.Get<TextMeshProUGUI>(parameters,"_notesCountText").text);
            detail.Set("ObstaclesCountValueText",Fields.Get<TextMeshProUGUI>(parameters,"_obstaclesCountText").text);
            var stats=Fields.Get<PlayerData>(view,"_playerData")?.TryGetPlayerLevelStatsData(view.beatmapKey);
            detail.Set("HighScoreValueText",stats!=null && stats.validScore?stats.highScore.ToString():"-");
            detail.Set("MaxComboValueText",stats!=null && stats.validScore?stats.maxCombo.ToString():"-");
            detail.Set("MaxRankValueText",stats!=null && stats.validScore?RankModel.GetRankName(stats.maxRank):"-");
        }
        private void OnDisable(){mask?.Restore();}
        private void OnDestroy(){mask?.Dispose();if(root)Object.Destroy(root.gameObject);assets?.Dispose();}
    }
}
