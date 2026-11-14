"""Extract the supplied 0.10.1 menu screens, fonts, sprites and original layout."""
import base64,json
from pathlib import Path
import UnityPy
from UnityPy.classes.PPtr import PPtr
from UnityPy.helpers.MeshHelper import MeshHandler
from extract_classic import ClassicTypeTreeGenerator,json_value
root=Path(__file__).resolve().parent.parent;out=root/'extracted/OldMenu'
for folder in ('objects','fonts','textures','raw'):(out/folder).mkdir(parents=True,exist_ok=True)
e=UnityPy.load(str(root/'Beat Saber_Data/level1'));g=ClassicTypeTreeGenerator('2017.4.1f1');g.load_local_dll_folder(str(root/'Beat Saber_Data/Managed'));e.typetree_generator=g
builtin=UnityPy.load(str(root/'Beat Saber_Data/Resources/unity default resources'));bi={o.path_id:o for o in builtin.objects}
def ref(o,p):
 if p['m_FileID'] and 'unity default resources' in o.assets_file.externals[p['m_FileID']-1].path:return bi[p['m_PathID']]
 return PPtr(**p,assetsfile=o.assets_file).deref()
def key(o):return Path(o.assets_file.name).name+'__'+str(o.path_id)
def tree(o):return o.read_typetree()
def save(o):
 k=key(o);t=tree(o)
 if o.type.name=='MonoBehaviour':
  h=o.parse_monobehaviour_head();t['m_Script']={'m_FileID':h.m_Script.m_FileID,'m_PathID':h.m_Script.m_PathID};t['m_Name']=h.m_Name
 (out/'raw'/(k+'.bin')).write_bytes(o.get_raw_data());(out/'objects'/(k+'.json')).write_text(json.dumps(t,default=json_value,indent=2));return k
D={'version':'0.10.1','nodes':[],'fonts':{},'textures':{},'materials':{},'meshes':{},'bindings':{}}
def texture(o,sprite=False):
 k=save(o)
 if k not in D['textures']:
  path=out/'textures'/(k+'.png');o.read().image.save(path);D['textures'][k]=base64.b64encode(path.read_bytes()).decode()
 return k

def material(o):
 k=save(o)
 if k not in D['materials']:
  t=tree(o);D['materials'][k]={'name':t['m_Name'],'floats':dict(t['m_SavedProperties']['m_Floats']),'colors':dict(t['m_SavedProperties']['m_Colors'])}
 return k

def font(o):
 k=save(o)
 if k in D['fonts']:return k
 t=tree(o);D['fonts'][k]={'name':o.parse_monobehaviour_head().m_Name,'face':t['m_fontInfo'],'glyphs':t['m_glyphInfoList'],'atlas':texture(ref(o,t['atlas'])),'material':material(ref(o,t['material'])),'normalStyle':t['normalStyle'],'boldStyle':t['boldStyle'],'boldSpacing':t['boldSpacing'],'italicStyle':t['italicStyle'],'tabSize':t['tabSize']}
 (out/'fonts'/(k+'.json')).write_text(json.dumps(D['fonts'][k],indent=2));return k

def components(go):return [ref(go,c['component']) for c in tree(go)['m_Component']]
def mesh(o):
 k=save(o)
 if k in D['meshes']:return k
 h=MeshHandler(o.read());h.process();D['meshes'][k]={name:[list(v[:size]) for v in values] if values else [] for name,values,size in [('vertices',h.m_Vertices,3),('normals',h.m_Normals,3),('uv',h.m_UV0,2),('uv2',h.m_UV1,2),('tangents',h.m_Tangents,4),('colors',h.m_Colors,4)]};D['meshes'][k]['triangles']=[[i for tri in sub for i in tri] for sub in h.get_triangles()];return k

def walk(tr,parent=None):
 t=tree(tr);go=ref(tr,t['m_GameObject']);gt=tree(go);cs=components(go);save(go);save(tr)
 n={'id':key(tr),'parent':parent,'name':gt['m_Name'],'active':bool(gt['m_IsActive']),'transform':{k:v for k,v in t.items() if k not in ('m_GameObject','m_Children','m_Father')},'rect':tr.type.name=='RectTransform'};D['nodes'].append(n)
 for c in cs:
  if c.type.name=='Canvas':n['canvas']=tree(c);save(c)
  if c.type.name!='MonoBehaviour':continue
  name=c.parse_monobehaviour_head().m_Script.deref().read().m_ClassName;ct=tree(c);save(c)
  if name in ('TextMeshProUGUI','TextMeshPro'):
   n['text']={k:v for k,v in ct.items() if k in ('m_text','m_fontSize','m_fontColor','m_fontStyle','m_textAlignment','m_margin','m_enableAutoSizing')};n['text'].update({'id':key(c),'ui':name=='TextMeshProUGUI','font':font(ref(c,ct['m_fontAsset'])),'material':material(ref(c,ct['m_sharedMaterial']))})
  if name=='Button':n['button']=ct.get('m_OnClick',{})
  if name in ('Image','RawImage'):
   n['image']={k:v for k,v in ct.items() if k in ('m_Color','m_Type','m_PreserveAspect','m_FillMethod','m_FillAmount','m_FillClockwise','m_FillOrigin')};n['image']['id']=key(c)
   ptr=ct.get('m_Sprite',ct.get('m_Texture'))
   if ptr and ptr['m_PathID']:
    obj=ref(c,ptr);n['image']['texture']=texture(obj,name=='Image')
    if obj.type.name=='Sprite':
     st=tree(obj);D.setdefault('sprites',{})[key(obj)]={'border':st['m_Border'],'pixelsPerUnit':st['m_PixelsToUnits']}
  if name=='ScoreUIController':D['bindings']['score']=[key(ref(c,p)) for p in ct['_scoreTexts']]
  if name=='ComboUIController':D['bindings']['combo']=[key(ref(c,ct['_comboText']))]
  if name=='ScoreMultiplierUIController':D['bindings']['multiplier']=[key(ref(c,p)) for p in ct['_multiplierTexts']];D['bindings']['progress']=key(ref(c,ct['_multiplierProgressImage']))
  if name=='GameEnergyUIPanel':D['bindings']['energy']=key(ref(c,ct['_energyBar']));D['bindings']['energyPanel']=key(tr)
  # Legacy Unity Text components are inactive alternatives; archive their embedded font.
  if name=='Text':
   fo=ref(c,ct['m_FontData']['m_Font']);ft=tree(fo);save(fo)
   if ft.get('m_FontData'):(out/'fonts'/(ft['m_Name']+'.ttf')).write_bytes(bytes(ft['m_FontData']))
 if 'text' not in n:
  mf=next((c for c in cs if c.type.name=='MeshFilter'),None);mr=next((c for c in cs if c.type.name=='MeshRenderer'),None)
  if mf and mr:
   n['mesh']=mesh(ref(mf,tree(mf)['m_Mesh']));n['materials']=[material(ref(mr,p)) for p in tree(mr)['m_Materials']]
 for ch in t['m_Children']:walk(ref(tr,ch),key(tr))
a=next(a for a in e.assets if a.name=='level1')
D['screens']={}
for name,pid in [('main',117),('songs',424),('detail',304),('difficulty',9),('changelog',51),('logo',418),('leaderboard',567)]:
 go=a.objects[pid];tr=next(c for c in components(go) if c.type.name in ('Transform','RectTransform'))
 D['screens'][name]=key(tr);walk(tr)
D['screens']['leaderboardRow']=key(next(c for c in components(a.objects[341]) if c.type.name=='RectTransform'))
(root/'OldSaber/Assets/ClassicMenu.json').write_text(json.dumps(D,separators=(',',':')))
(out/'manifest.json').write_text(json.dumps({k:v for k,v in D.items() if k not in ('textures','meshes')},indent=2))
print('Extracted menu:',len(D['nodes']),'nodes,',len(D['fonts']),'font assets,',len(D['textures']),'textures,',len(D['meshes']),'meshes');print(D['bindings']);print([(v['name'],len(v['glyphs'])) for v in D['fonts'].values()])
