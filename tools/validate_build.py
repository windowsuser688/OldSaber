"""Check exported runtime data and the built managed package (does not run Unity)."""
import json, math, zipfile
from pathlib import Path
import dnfile
root=Path(__file__).resolve().parent.parent
s=json.loads((root/'OldSaber/Assets/scene.json').read_text())
def validate_scene(s):
 seen=set()
 for n in s['nodes']:
  assert n['id'] not in seen
  assert not n['parent'] or n['parent'] in seen, ('parent order',n['id'])
  seen.add(n['id'])
  assert n['name'] not in ['GameHUD','SceneSetup','LightEffects','MirrorOccluders','BurnedMarksArea']
  for field in ['position','rotation','scale']:
   assert all(math.isfinite(v) for v in n[field])
  if 'mesh' in n:
   assert n['mesh'] in s['meshes']
   count=n['batch']['subMeshCount'];first=n['batch']['firstSubMesh']
   if count: assert first+count<=len(s['meshes'][n['mesh']]['triangles'])
  if 'beam' in n:
   assert n['beam']['_length']>0 and n['beam']['_width']>0
 for k,m in s['meshes'].items():
  assert m['vertices']
  assert all(math.isfinite(v) for p in m['vertices'] for v in p)
  for sub in m['triangles']:
   assert len(sub)%3==0
   assert all(0<=i<len(m['vertices']) for i in sub)
  for channel in ('normals','uv','uv2','tangents','colors'):
   assert len(m[channel]) in (0,len(m['vertices'])), (k,channel)
   assert all(math.isfinite(v) for value in m[channel] for v in value), (k,channel)
 return seen

seen=validate_scene(s)
# Platform regression: keep the source's combined structures and both horizontal
# surfaces, but never replace its runtime burn-mark overlay with opaque geometry.
by_id={n['id']:n for n in s['nodes']}
for node_id in (165,193,251,214,285,258,279,198,218,227,239,185,233):
 n=by_id[f'level7__{node_id}']
 assert 'mesh' in n and n['active'], ('missing platform/structure',node_id)
 ancestor=n
 while ancestor['parent']:
  ancestor=by_id[ancestor['parent']]
  assert ancestor['active'], ('hidden platform parent',node_id)
assert not any('SaberBurnMarks' in n.get('materials',[]) for n in s['nodes'])
for node_id,width,depth in ((198,3.,2.),(218,4.75,140.)):
 n=by_id[f'level7__{node_id}'];vertices=s['meshes'][n['mesh']]['vertices']
 for axis,expected in ((0,width),(2,depth)):
  extent=(max(v[axis] for v in vertices)-min(v[axis] for v in vertices))*n['scale'][axis]
  assert abs(extent-expected)<.001, ('platform extent',node_id,axis,extent)
assert sorted(r['count'] for r in s['rings'])==[15,90]
assert len([n for n in s['nodes'] if 'rotationEvent' in n])==8
assert {n['rotationEvent'] for n in s['nodes'] if 'rotationEvent' in n}=={12,13}
assert len(s['spectra'])==2
for r in s['rings']:
 assert len(r['nodes'])==r['count'] and all(n in seen for n in r['nodes'])
for sp in s['spectra']:
 assert len(sp['nodes'])==128 and all(n in seen for n in sp['nodes'])
 for node_id in sp['nodes']:
  n=by_id[node_id]
  assert 'mesh' in n and n['active']
  parent=by_id[n['parent']]
  # With the exported base-at-zero mesh, the runtime baseline must place tips
  # above the floor and still leave a positive range for the audio response.
  baseline=max(sp['min'],-parent['position'][1]+.1)
  assert baseline<sp['max']
  assert parent['position'][1]+baseline>0
  while n['parent']:
   n=by_id[n['parent']]
   assert n['active'], ('hidden visualizer parent',node_id)
p=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'))
resources={str(r.Name) for r in p.net.mdtables.ManifestResource.rows}
assert {'OldSaber.manifest.json','OldSaber.scene.json','OldSaber.TriangleEnvironment.json','OldSaber.BigMirrorEnvironment.json','OldSaber.NiceEnvironment.json'} <= resources
refs={str(r.Name) for r in p.net.mdtables.AssemblyRef.rows}
assert {'Main','HMRendering','IPA.Loader','UnityEngine.CoreModule','BeatmapCore'}<=refs
assert 'Assembly-CSharp' not in refs, 'Must not link the classic game scripts into the new game'
assert 'Rendering' in refs, 'Native mirror components must be linked'
assert 'OldSaber.ClassicDebris.json' in resources
assert {'OldSaber.OldNotes.bundle','OldSaber.Settings.bsml'} <= resources
assert {'BSML','SiraUtil'} <= refs
assert str(p.net.mdtables.Assembly.rows[0].Name)=='OldSaber'
# Both original floor planes remain in the mesh data and use the mirror policy.
# Its texture cache is per camera (not per plane), so both use one raised plane
# at runtime. Check that the source surfaces are planar and upward-facing.
reflective=[n for n in s['nodes'] if any(m in ('PlayersPlaceMirror','TrackMirror') for m in n.get('materials',[]))]
assert len(reflective)==2
assert {n['materials'][0] for n in reflective}=={'PlayersPlaceMirror','TrackMirror'}
for n in reflective:
 assert len(n['materials'])==1 and n['active']
 mesh=s['meshes'][n['mesh']]
 assert max(v[1] for v in mesh['vertices'])-min(v[1] for v in mesh['vertices'])<1e-6
 assert all(normal[1]>.999 for normal in mesh['normals'])
 ancestor=n
 while True:
  assert all(abs(ancestor['rotation'][i])<1e-6 for i in (0,1,2))
  assert all(scale>0 for scale in ancestor['scale'])
  if not ancestor['parent']:break
  ancestor=by_id[ancestor['parent']]
# Flicker regression: compare the actual imported solid runway top against the
# common mirror height. A zero-height mirror (0.1.3) must fail this check.
import re
mirror_source=(root/'OldSaber/ClassicMirrors.cs').read_text()
mirror_height=float(re.search(r'const float SurfaceHeight\s*=\s*([.0-9]+)f',mirror_source).group(1))
runway=by_id['level7__193']
solid_top=max(v[1] for v in s['meshes'][runway['mesh']]['vertices'])
assert mirror_height-solid_top >= .001, ('mirror intersects runway depth',mirror_height,solid_top)
assert mirror_height < .005, 'Mirror must remain below the original platform trim'
# Check all private field hooks against the target assemblies, including base-class fields.
checks={'Main.dll':{'LightSwitchEventEffect':['_lightManager','_beatmapCallbacksController'],'AudioTimeSyncController':['_audioSource'],
 'NoteDebris':['_physics','_meshTransform','_cutoutCurve','_materialPropertyBlockController','_lifeTime']},
 'HMRendering.dll':{'BloomPrePassLight':['_lightType'],'TubeBloomPrePassLight':['_mainEffectPostProcessEnabled','_center','_parametricBoxController','_disableRenderersOnZeroAlpha','_dynamic3SliceSprite','_forceUseBakedGlow','_bakedGlowWidthScale'],'ParametricBoxController':['_meshRenderer'],'Parametric3SliceSpriteController':['_widthMultiplier']},
 'Rendering.dll':{'Mirror':['_mirrorRenderer','_renderer','_mirrorMaterial','_noMirrorMaterial']}}
for dll,types in checks.items():
 a=dnfile.dnPE(str(root/'Managed'/dll))
 for name,expected in types.items():
  typ=next(t for t in a.net.mdtables.TypeDef.rows if str(t.TypeName)==name)
  fields={str(f.row.Name) for f in typ.FieldList}
  assert set(expected)<=fields,(dll,name,expected)
# Validate all selectable environments, not just the original default resource.
expected = {
 'scene.json': ('DefaultEnvironment','level7',[15,90],4.75),
 'TriangleEnvironment.json': ('TriangleEnvironment','level9',[15,30],4.75),
 'BigMirrorEnvironment.json': ('BigMirrorEnvironment','level8',[15,90],15.7),
 'NiceEnvironment.json': ('NiceEnvironment','level6',[10,90],8.7),
}
for filename,(scene_name,source_file,ring_counts,runway_width) in expected.items():
 data=json.loads((root/'OldSaber/Assets'/filename).read_text())
 ids=validate_scene(data);nodes={n['id']:n for n in data['nodes']}
 assert data['sceneName']==scene_name and data['sourceFile']==source_file
 roots=[n for n in data['nodes'] if not n['parent']]
 assert len(roots)==1 and roots[0]['active']
 assert sorted(r['count'] for r in data['rings'])==ring_counts
 assert len([n for n in data['nodes'] if 'rotationEvent' in n])==8
 assert len(data['spectra'])==2
 for group in data['rings']+data['spectra']:
  assert len(group['nodes'])==group.get('count',128)
  for node_id in group['nodes']:
   assert node_id in ids
 mirrors=[n for n in data['nodes'] if any(m in ('PlayersPlaceMirror','TrackMirror') for m in n.get('materials',[]))]
 assert len(mirrors)==2
 for n in mirrors:
  mesh=data['meshes'][n['mesh']]
  assert all(normal[1]>.999 for normal in mesh['normals'])
  ancestor=n
  while True:
   # Nice's standalone runway mirror is intentionally inactive in the source.
   if scene_name=='NiceEnvironment' and ancestor is n and n['materials']==['TrackMirror']:
    assert not ancestor['active']
   else:
    assert ancestor['active']
   if not ancestor['parent']:break
   ancestor=nodes[ancestor['parent']]
 runway=next(n for n in mirrors if n['materials']==['TrackMirror'])
 vertices=data['meshes'][runway['mesh']]['vertices']
 width=(max(v[0] for v in vertices)-min(v[0] for v in vertices))*runway['scale'][0]
 assert abs(width-runway_width)<.001,(scene_name,width)
 if scene_name=='TriangleEnvironment':
  triangular=next(r for r in data['rings'] if r['count']==30)
  assert nodes[triangular['parent']]['name']=='TriangleTrackLaneRings'
  assert triangular['move']['_songEventType']==9
  assert triangular['spin']['_songEventType']==8
 print(f"PASS: {scene_name}: {len(ids)} nodes, {sum(ring_counts)} rings, runway width {runway_width}.")
manifest=json.loads((root/'OldSaber/manifest.json').read_text())
assert manifest['gameVersion']=='1.40.8'
assert manifest['id']==manifest['name']=='OldSaber'
main=dnfile.dnPE(str(root/'Managed/Main.dll'))
debris_type=next(t for t in main.net.mdtables.TypeDef.rows if str(t.TypeName)=='NoteDebris')
init=next(m.row for m in debris_type.MethodList if str(m.row.Name)=='Init')
parameters={str(p.row.Name) for p in init.ParamList}
assert {'notePos','noteRot','noteScale','positionOffset','rotationOffset','cutPoint','cutNormal','force','torque','lifeTime','forceOnlySimplePhysics'}<=parameters
notes=json.loads((root/'extracted/OldSaberNotes/manifest.json').read_text())
assert not notes['errors'] and not notes['previewErrors']
note_ids={r['id'] for r in notes['objects']}
assert {r['name'] for r in notes['roots']}=={'GameNote','GhostNote','BombNote','NoteDebris'}
for record in notes['objects']:
 assert all(ref in note_ids for ref in record.get('references',[]))
 assert (root/'extracted/OldSaberNotes/raw'/(record['id']+'.bin')).exists()
 if record['type']=='Shader':assert (root/'extracted/OldSaberNotes/shaders'/(record['id']+'.shader.txt')).exists()
profile=json.loads((root/'OldSaber/Assets/ClassicDebris.json').read_text())
source=json.loads((root/'extracted/OldSaberNotes/debris-settings.json').read_text())
assert profile['lifetime']==source['debris']['_lifeTime']==1
assert profile['mass']==source['rigidbody']['m_Mass']==1
assert profile['drag']==source['rigidbody']['m_Drag']==0
assert profile['angularDrag']==source['rigidbody']['m_AngularDrag']==0
assert abs(profile['colliderRadius']-.15)<1e-6
assert len(profile['centroidVertices'])==24
assert profile['cutoutCurve']==source['debris']['_cutoutCurve']
with zipfile.ZipFile(root/'dist'/f"OldSaber-{manifest['version']}-bs{manifest['gameVersion']}.zip") as z:
 assert set(z.namelist())=={'Plugins/OldSaber.dll','Plugins/OldSaber.pdb','OldSaber-INSTALL.md'}
 assert z.read('Plugins/OldSaber.dll')==(root/'dist/Plugins/OldSaber.dll').read_bytes()
 assert z.read('Plugins/OldSaber.pdb')==(root/'dist/Plugins/OldSaber.pdb').read_bytes()
 assert z.read('Plugins/OldSaber.pdb')[:4]==b'BSJB'
 assert z.testzip() is None
print(f'PASS: {len(seen)} runtime nodes, 12 meshes, 105 rings, 8 rotating lasers, 2 spectrograms.')
print('PASS: mesh indices/transforms, parent order, target private fields, DLL references/resources, ZIP integrity.')
print('PASS: floor geometry, native glow fields and player-mirror integration fields.')
print('PASS: OldSaber identity, classic note/shader extraction, prefab physics profile and native debris hook fields/parameters.')
print('NOT TESTED: Unity execution, rendering, VR performance, or in-game mod compatibility.')
