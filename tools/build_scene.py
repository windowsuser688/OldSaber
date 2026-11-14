"""Convert extracted classic assets to the compact, Unity-version-independent runtime scene."""
import json, sys
from pathlib import Path
import UnityPy
from UnityPy.classes.PPtr import PPtr
from UnityPy.helpers.MeshHelper import MeshHandler
from extract_classic import ClassicTypeTreeGenerator
E=None
g=ClassicTypeTreeGenerator('2017.4.1f1');g.load_local_dll_folder('Beat Saber_Data/Managed')
out={'nodes':[], 'meshes':{}, 'rings':[], 'spectra':[]}
def ref(o,p):return PPtr(**p,assetsfile=o.assets_file).deref()
def key(o):return Path(o.assets_file.name).name+'__'+str(o.path_id)
def tree(o):return o.read_typetree()
def vec(d):return [d[k] for k in ('x','y','z')]
def comp(go):
 return [ref(go,x['component']) for x in tree(go)['m_Component']]
def script(o):return o.parse_monobehaviour_head().m_Script.deref().read().m_ClassName
meshes={}
def node(tr,parent='',prefix=''):
 t=tree(tr);go=ref(tr,t['m_GameObject']);gt=tree(go)
 # Burn marks are a runtime render-texture overlay, not solid platform geometry.
 # Recreating it without its controller/texture can cover the entire platform.
 if gt['m_Name'] in ['GameHUD','SceneSetup','LightEffects','MirrorOccluders','BurnedMarksArea']:return None
 cs=comp(go); nid=prefix+key(tr)
 n={'id':nid,'parent':parent,'name':gt['m_Name'],'active':bool(gt['m_IsActive']), 'position':vec(t['m_LocalPosition']),'rotation':[t['m_LocalRotation'][k] for k in ('x','y','z','w')],'scale':vec(t['m_LocalScale'])}
 # The serialized scene root is inactive; the original game's scene lifecycle
 # activates it. Our replacement must do that explicitly, without enabling the
 # intentionally inactive source meshes already represented by CombinedMesh.
 if not parent:n['active']=True
 out['nodes'].append(n)
 mb={script(c):(c,tree(c)) for c in cs if c.type.name=='MonoBehaviour'}
 if 'TubeBloomPrePassLight' in mb:
  b=mb['TubeBloomPrePassLight'][1];n['beam']={k:b[k] for k in ['_ID','_width','_length','_center','_color']}
 elif any(c.type.name=='MeshFilter' for c in cs):
  mf=next(c for c in cs if c.type.name=='MeshFilter');mr=next((c for c in cs if c.type.name=='MeshRenderer'),None)
  if mr and tree(mf)["m_Mesh"]["m_PathID"]:
   rt=tree(mr);mo=ref(mf,tree(mf)['m_Mesh']);mk=key(mo);meshes[mk]=mo
   n['mesh']=mk;n['materials']=[tree(ref(mr,p))['m_Name'] for p in rt['m_Materials']]
   n['batch']=rt['m_StaticBatchInfo'];n['batchRoot']=prefix+key(ref(mr,rt['m_StaticBatchRoot'])) if rt['m_StaticBatchRoot']['m_PathID'] else ''
 if 'LightRotationEventEffect' in mb:
  b=mb['LightRotationEventEffect'][1];n['rotationEvent']=b['_event'];n['rotationAxis']=vec(b['_rotationVector'])
 if 'TrackLaneRingsManager' in mb:
  c,b=mb['TrackLaneRingsManager'];pref=ref(c,b['_trackLaneRingPrefab']);pg=ref(pref,tree(pref)['m_GameObject']);pt=next(x for x in comp(pg) if x.type.name=='Transform')
  ring={'parent':nid,'count':b['_ringCount'],'spacing':b['_ringPositionStep'],'nodes':[]}
  for i in range(b['_ringCount']):
   rid=node(pt,nid,f'{nid}/ring{i}/');rn=next(x for x in out['nodes'] if x['id']==rid);rn['position']=[0,0,i*b['_ringPositionStep']];ring['nodes'].append(rid)
  for typ,label in [('TrackLaneRingsRotationEffect','startup'),('TrackLaneRingsRotationEffectSpawner','spin'),('TrackLaneRingsPositionStepEffectSpawner','move')]:
   if typ in mb:ring[label]={k:v for k,v in mb[typ][1].items() if isinstance(v,(int,float)) and k.startswith('_')}
  out['rings'].append(ring)
 if 'SpectrogramColumns' in mb:
  c,b=mb['SpectrogramColumns'];pg=ref(c,b['_columnPrefab']);pt=next(x for x in comp(pg) if x.type.name=='Transform');sp={'nodes':[],'min':b['_minHeight'],'max':b['_maxHeight']}
  for i in range(128):
   sid=node(pt,nid,f'{nid}/spectrum{i}/');sn=next(x for x in out['nodes'] if x['id']==sid);sn['position']=[v*(i if i<64 else -(i-63)) for v in vec(b['_separator'])];sn['scale']=[b['_columnWidth'],b['_minHeight'],b['_columnDepth']];sp['nodes'].append(sid)
  out['spectra'].append(sp)
 for ch in t['m_Children']:node(ref(tr,ch),nid,prefix)
 return nid
def export_scene(scene_name, filename, scenes):
 global E,out,meshes
 index=next(i for i,path in enumerate(scenes) if path.endswith('/'+scene_name+'.unity'))
 E=UnityPy.load('Beat Saber_Data/level'+str(index));E.typetree_generator=g
 out={'sceneName':scene_name,'sourceFile':'level'+str(index),'nodes':[], 'meshes':{}, 'rings':[], 'spectra':[]}
 meshes={}
 roots=[o for o in list(E.objects) if o.type.name=='Transform' and tree(o)['m_Father']['m_PathID']==0 and tree(ref(o,tree(o)['m_GameObject']))['m_Name']=='Environment']
 if len(roots)!=1:raise ValueError('Expected one Environment root in '+scene_name)
 root=roots[0]
 node(root)
 for k,o in meshes.items():
  h=MeshHandler(o.read());h.process();out['meshes'][k]={'vertices':[list(v) for v in h.m_Vertices],'normals':[list(v) for v in h.m_Normals] if h.m_Normals else [],'uv':[list(v[:2]) for v in h.m_UV0] if h.m_UV0 else [],'triangles':[[i for t in ts for i in t] for ts in h.get_triangles()]}
  out['meshes'][k]['uv2']=[list(v[:2]) for v in h.m_UV1] if h.m_UV1 else []
  out['meshes'][k]['tangents']=[list(v) for v in h.m_Tangents] if h.m_Tangents else []
  out['meshes'][k]['colors']=[list(v) for v in h.m_Colors] if h.m_Colors else []
 (Path('OldSaber/Assets')/filename).write_text(json.dumps(out,separators=(',',':')))
 print(scene_name, 'exported',len(out['nodes']),'nodes,',len(out['meshes']),'meshes,',sum(r['count'] for r in out['rings']),'rings')

if __name__=='__main__':
 metadata=UnityPy.load('Beat Saber_Data/globalgamemanagers')
 scenes=next(o.read_typetree()['scenes'] for o in metadata.objects if o.type.name=='BuildSettings')
 for name,filename in [('DefaultEnvironment','scene.json'),('TriangleEnvironment','TriangleEnvironment.json'),('BigMirrorEnvironment','BigMirrorEnvironment.json'),('NiceEnvironment','NiceEnvironment.json')]:
  export_scene(name,filename,scenes)
