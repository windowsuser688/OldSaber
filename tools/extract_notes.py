"""Export classic gameplay note prefabs, debris, materials and compiled shaders."""
import base64,hashlib,json
from collections import deque,Counter
from pathlib import Path
import UnityPy
from UnityPy.classes.PPtr import PPtr
from extract_classic import ClassicTypeTreeGenerator,pointers,json_value
src=Path('Beat Saber_Data');out=Path('extracted/OldSaberNotes')
for folder in ['objects','raw','meshes','textures','shaders']:(out/folder).mkdir(parents=True,exist_ok=True)
e=UnityPy.load(str(src/'sharedassets4.assets'))
g=ClassicTypeTreeGenerator('2017.4.1f1');g.load_local_dll_folder(str(src/'Managed'));e.typetree_generator=g
names={'GameNote','GhostNote','BombNote','NoteDebris'}
roots=[o for o in list(e.objects) if o.type.name=='GameObject' and o.read().m_Name in names]
assert {o.read().m_Name for o in roots}==names
queue=deque(roots);seen=set();records=[];errors=[];previews=[]
def key(o):return Path(o.assets_file.name).name+'__'+str(o.path_id)
physics={}
while queue:
 o=queue.popleft();k=key(o)
 if k in seen:continue
 seen.add(k);record={'id':k,'type':o.type.name}
 (out/'raw'/(k+'.bin')).write_bytes(o.get_raw_data())
 try:
  t=o.read_typetree();record['name']=t.get('m_Name','')
  if o.type.name=='MonoBehaviour':
   h=o.parse_monobehaviour_head();record['script']=h.m_Script.deref().read().m_ClassName
   t['m_Script']={'m_FileID':h.m_Script.m_FileID,'m_PathID':h.m_Script.m_PathID};t['m_Name']=h.m_Name
   if record['script']=='NoteDebris':physics['debris']=t
  if o.type.name=='Rigidbody' and t['m_GameObject']['m_PathID']==91:physics['rigidbody']=t
  (out/'objects'/(k+'.json')).write_text(json.dumps(t,default=json_value,indent=2))
  refs=[]
  for ptr in pointers(t):
   try:
    obj=PPtr(**ptr,assetsfile=o.assets_file).deref();refs.append(key(obj));queue.append(obj)
   except Exception as ex:errors.append({'id':k,'reference':ptr,'error':str(ex)})
  record['references']=refs
  try:
   if o.type.name=='Mesh':(out/'meshes'/(k+'.obj')).write_text(o.read().export())
   elif o.type.name=='Texture2D' and t['m_Width']>0 and t['m_Height']>0:o.read().image.save(out/'textures'/(k+'.png'))
   elif o.type.name=='Shader':
    record['shaderName']=t.get('m_ParsedForm',{}).get('m_Name',t.get('m_Name',''))
    (out/'shaders'/(k+'.shader.txt')).write_text(o.read().export())
  except Exception as ex:previews.append({'id':k,'error':str(ex)})
 except Exception as ex:errors.append({'id':k,'error':str(ex)})
 records.append(record)
manifest={'sourceGameVersion':'0.10.1','sourceUnityVersion':'2017.4.1f1','sourceSHA256':hashlib.sha256((src/'sharedassets4.assets').read_bytes()).hexdigest(),'roots':[{'id':key(o),'name':o.read().m_Name} for o in roots],'counts':dict(Counter(r['type'] for r in records)),'objects':records,'errors':errors,'previewErrors':previews,'shaderNotice':'Raw bytes and decoded shader metadata are extracted compiled Unity shaders. Shader text is a reconstructed inspection export, not original shader source and not a portable 1.40.8 asset bundle.'}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2))
(out/'debris-settings.json').write_text(json.dumps(physics,indent=2))
# Build the runtime profile from the actual prefab, not C# field defaults.
from UnityPy.helpers.MeshHelper import MeshHandler
debris_obj=next(o for o in roots if o.read().m_Name=='NoteDebris')
centroid=PPtr(**physics['debris']['_centroidComputationMesh'],assetsfile=debris_obj.assets_file).deref()
mesh=MeshHandler(centroid.read());mesh.process()
collider=next(json.loads((out/'objects'/(r['id']+'.json')).read_text()) for r in records
              if r['type']=='SphereCollider' and json.loads((out/'objects'/(r['id']+'.json')).read_text())['m_GameObject']['m_PathID']==debris_obj.path_id)
profile={'lifetime':physics['debris']['_lifeTime'],'mass':physics['rigidbody']['m_Mass'],
         'drag':physics['rigidbody']['m_Drag'],'angularDrag':physics['rigidbody']['m_AngularDrag'],
         'colliderRadius':collider['m_Radius'],'centroidVertices':[list(v) for v in mesh.m_Vertices],
         'cutoutCurve':physics['debris']['_cutoutCurve']}
Path('OldSaber/Assets/ClassicDebris.json').write_text(json.dumps(profile,indent=2)+'\n')
print(json.dumps(manifest['counts'],indent=2));print('Errors:',len(errors),'preview errors:',len(previews));print('Physics:',json.dumps(physics))
if errors:raise SystemExit('Incomplete extraction: inspect manifest.json')
