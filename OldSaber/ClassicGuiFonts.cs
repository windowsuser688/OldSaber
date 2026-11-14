using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal sealed class ClassicGuiFonts
    {
        private readonly JObject data;
        private readonly TMP_FontAsset fallback;
        private readonly List<Object> owned;
        private readonly Dictionary<string,TMP_FontAsset> fonts=new Dictionary<string,TMP_FontAsset>();
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        internal ClassicGuiFonts(JObject data,TMP_FontAsset fallback,List<Object> owned)
        {this.data=data;this.fallback=fallback;this.owned=owned;}
        internal Texture2D Texture(string key)
        {
            if(textures.TryGetValue(key,out var result))return result;
            result=new Texture2D(2,2,TextureFormat.RGBA32,false,true);owned.Add(result);
            if(!ImageConversion.LoadImage(result,Convert.FromBase64String((string)data["textures"][key])))throw new InvalidOperationException("Invalid classic HUD texture "+key);
            result.name="OldSaber.GUI."+key;result.filterMode=FilterMode.Bilinear;result.wrapMode=TextureWrapMode.Clamp;
            textures[key]=result;return result;
        }
        internal TMP_FontAsset Font(string key)
        {
            if(fonts.TryGetValue(key,out var font))return font;
            var d=data["fonts"][key];var f=d["face"];
            font=ScriptableObject.CreateInstance<TMP_FontAsset>();owned.Add(font);font.name="OldSaber."+(string)d["name"];
            Fields.Set(font,"m_Version","1.1.0");
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            font.faceInfo=new FaceInfo{familyName=(string)f["Name"],styleName="Classic",pointSize=(int)(float)f["PointSize"],scale=(float)f["Scale"],
                lineHeight=(float)f["LineHeight"],ascentLine=(float)f["Ascender"],capLine=(float)f["CapHeight"],meanLine=(float)f["CenterLine"],baseline=(float)f["Baseline"],descentLine=(float)f["Descender"],
                superscriptOffset=(float)f["SuperscriptOffset"],superscriptSize=(float)f["SubSize"],subscriptOffset=(float)f["SubscriptOffset"],subscriptSize=(float)f["SubSize"],
                underlineOffset=(float)f["Underline"],underlineThickness=(float)f["UnderlineThickness"],strikethroughOffset=(float)f["strikethrough"],strikethroughThickness=(float)f["strikethroughThickness"],tabWidth=(float)f["TabWidth"]};
            int height=(int)(float)f["AtlasHeight"];
            font.atlasTextures=new[]{Texture((string)d["atlas"])};
            Fields.Set(font,"m_AtlasWidth",(int)(float)f["AtlasWidth"]);Fields.Set(font,"m_AtlasHeight",height);
            Fields.Set(font,"m_AtlasPadding",(int)(float)f["Padding"]);Fields.Set(font,"m_AtlasRenderMode",GlyphRenderMode.SDF16);
            var glyphs=new List<Glyph>();var chars=new List<TMP_Character>();uint index=1;
            foreach(var g in d["glyphs"])
            {
                uint code=(uint)g["id"];
                // Same legacy-to-TextCore mapping used by TMP's own font upgrader.
                var rect=ClassicGuiGlyphs.Rect(code,(float)g["x"],(float)g["y"],(float)g["width"],(float)g["height"],height);
                var glyph=new Glyph(index++,new GlyphMetrics((float)g["width"],(float)g["height"],(float)g["xOffset"],(float)g["yOffset"],(float)g["xAdvance"]),rect,(float)g["scale"],0);
                glyphs.Add(glyph);chars.Add(new TMP_Character(code,font,glyph));
            }
            Fields.Set(font,"m_GlyphTable",glyphs);Fields.Set(font,"m_CharacterTable",chars);
            font.fallbackFontAssetTable=new List<TMP_FontAsset>{fallback};
            font.normalStyle=(float)d["normalStyle"];font.boldStyle=(float)d["boldStyle"];font.boldSpacing=(float)d["boldSpacing"];font.italicStyle=(byte)(int)d["italicStyle"];font.tabSize=(byte)(int)d["tabSize"];
            var shader=Shader.Find("TextMeshPro/Distance Field");
            if(!shader)shader=fallback.material.shader;
            var mat=new Material(shader){name=font.name+".Material"};owned.Add(mat);
            var md=data["materials"][(string)d["material"]];
            foreach(var p in ((JObject)md["floats"]).Properties())if(mat.HasProperty(p.Name))mat.SetFloat(p.Name,(float)p.Value);
            foreach(var p in ((JObject)md["colors"]).Properties())if(mat.HasProperty(p.Name))mat.SetColor(p.Name,ClassicGuiState.ColorValue(p.Value));
            mat.SetTexture("_MainTex",font.atlasTextures[0]);
            mat.SetFloat("_TextureWidth",font.atlasTextures[0].width);mat.SetFloat("_TextureHeight",height);mat.SetFloat("_GradientScale",(float)f["Padding"]+1);
            // The archived material stores a green editor glow and dark vertex
            // tint. Keep runtime HUD lettering neutral white as requested.
            mat.SetColor("_FaceColor",Color.white);
            if(mat.HasProperty("_GlowColor"))mat.SetColor("_GlowColor",Color.clear);
            mat.DisableKeyword("GLOW_ON");
            font.material=mat;font.ReadFontAssetDefinition();fonts[key]=font;return font;
        }
    }
}
