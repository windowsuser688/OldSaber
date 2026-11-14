"""Extract the 0.10.1 saber hierarchies, meshes, materials and trail textures/settings."""
import base64,json,sys
from pathlib import Path
import UnityPy
from UnityPy.classes.PPtr import PPtr
from UnityPy.helpers.MeshHelper import MeshHandler
from extract_classic import ClassicTypeTreeGenerator,json_value
root=Path(__file__).resolve().parent.parent
out=root/'extracted/OldSaberSabers'
for sub in ('objects','raw','meshes','textures'):(out/sub).mkdir(parents=True,exist_ok=True)
e=UnityPy.load(str(root/'Beat Saber_Data/level3'))
g=ClassicTypeTreeGenerator('2017.4.1f1');g.load_local_dll_folder(str(root/'Beat Saber_Data/Managed'));e.typetree_generator=g
builtin=UnityPy.load(str(root/'Beat Saber_Data/Resources/unity default resources'))
builtin_objects={o.path_id:o for o in builtin.objects}
def ref(o,p):
 if p['m_FileID'] and 'unity default resources' in o.assets_file.externals[p['m_FileID']-1].path:return builtin_objects[p['m_PathID']]
 return PPtr(**p,assetsfile=o.assets_file).deref()
def key(o):return Path(o.assets_file.name).name+'__'+str(o.path_id)
records={};data={'sourceVersion':'0.10.1','sabers':[],'meshes':{},'materials':{},'textures':{}}
def save(o):
 k=key(o)
 if k not in records:
  t=o.read_typetree();(out/'raw'/(k+'.bin')).write_bytes(o.get_raw_data());(out/'objects'/(k+'.json')).write_text(json.dumps(t,default=json_value,indent=2));records[k]={'type':o.type.name,'name':t.get('m_Name','')}
 return k

def mesh(o):
 k=save(o)
 if k not in data['meshes']:
  h=MeshHandler(o.read());h.process();d={key:[list(v[:size]) for v in values] if values else [] for key,values,size in [('vertices',h.m_Vertices,3),('normals',h.m_Normals,3),('uv',h.m_UV0,2),('uv2',h.m_UV1,2),('tangents',h.m_Tangents,4),('colors',h.m_Colors,4)]};d['triangles']=[[i for t in ts for i in t] for ts in h.get_triangles()];data['meshes'][k]=d
  (out/'meshes'/(k+'.obj')).write_text(o.read().export())
 return k

def material(o):
 k=save(o)
 if k in data['materials']:return k
 t=o.read_typetree();shader=ref(o,t['m_Shader']);save(shader)
 d={'name':t['m_Name'],'shader':shader.read_typetree().get('m_ParsedForm',{}).get('m_Name',''),'floats':dict(t['m_SavedProperties']['m_Floats']),'colors':dict(t['m_SavedProperties']['m_Colors']),'textures':{}}
 for name,value in t['m_SavedProperties']['m_TexEnvs']:
  if not value['m_Texture']['m_PathID']:continue
  tex=ref(o,value['m_Texture']);tk=save(tex)
  if tex.type.name=='Texture2D':
   path=out/'textures'/(tk+'.png');tex.read().image.save(path);data['textures'][tk]=base64.b64encode(path.read_bytes()).decode();d['textures'][name]=tk
 data['materials'][k]=d
 return k

def comps(go):return [ref(go,c['component']) for c in go.read_typetree()['m_Component']]
def vec(t):return [t[k] for k in ('x','y','z')]
def walk(tr,nodes,parent=None):
 t=tr.read_typetree();save(tr);go=ref(tr,t['m_GameObject']);save(go);cs=comps(go)
 n={'id':key(tr),'name':go.read().m_Name,'parent':parent,'position':vec(t['m_LocalPosition']),'rotation':[t['m_LocalRotation'][k] for k in ('x','y','z','w')],'scale':vec(t['m_LocalScale'])};nodes.append(n)
 for c in cs:
  if c.type.name=='MeshFilter':save(c);n['mesh']=mesh(ref(c,c.read_typetree()['m_Mesh']))
  if c.type.name=='MeshRenderer':save(c);n['materials']=[material(ref(c,p)) for p in c.read_typetree()['m_Materials']]
 for ch in t['m_Children']:walk(ref(tr,ch),nodes,key(tr))
scene=next(a for a in e.assets if a.name=='level3')
for go in list(scene.objects.values()):
 if go.type.name!='GameObject' or go.read().m_Name!='Saber':continue
 cs=comps(go);mb={c.parse_monobehaviour_head().m_Script.deref().read().m_ClassName:c for c in cs if c.type.name=='MonoBehaviour'}
 if 'Saber' not in mb:continue
 st=mb['Saber'].read_typetree();tt=mb['XWeaponTrail'].read_typetree();save(mb['Saber']);save(mb['XWeaponTrail'])
 tr=next(c for c in cs if c.type.name=='Transform');nodes=[]
 for ch in tr.read_typetree()['m_Children']:walk(ref(tr,ch),nodes)
 trail={k:tt[k] for k in ('MaxFrame','Granularity','MyColor','_skipFirstFrames')}
 trail['start']=key(ref(mb['XWeaponTrail'],tt['PointStart']));trail['end']=key(ref(mb['XWeaponTrail'],tt['PointEnd']));trail['material']=material(ref(mb['XWeaponTrail'],tt['MyMaterial']))
 prefab=ref(mb['XWeaponTrail'],tt['_trailRendererPrefab']);save(prefab)
 # Renderer material actually used by the 2017 trail implementation.
 rt=prefab.read_typetree();renderer=ref(prefab,rt['_meshRenderer']);save(renderer)
 trail['rendererMaterial']=material(ref(renderer,renderer.read_typetree()['m_Materials'][0]))
 data['sabers'].append({'type':st['_saberType'],'nodes':nodes,'trail':trail})
assert sorted(s['type'] for s in data['sabers'])==[0,1]
(root/'OldSaber/Assets/ClassicSabers.json').write_text(json.dumps(data,separators=(',',':')))
(out/'manifest.json').write_text(json.dumps({'objects':records,'sabers':data['sabers']},indent=2))
print('Extracted',len(data['sabers']),'sabers,',len(data['meshes']),'meshes,',len(data['materials']),'materials,',len(data['textures']),'textures')
for k,m in data['materials'].items():print(k,m['name'],m['shader'],m['textures'])
for s in data['sabers']:print(s['type'],s['trail'])
