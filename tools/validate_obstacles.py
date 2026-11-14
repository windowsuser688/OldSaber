"""Validate extracted obstacle model, source bindings, modern hooks and packaging."""
import base64,json,math
from pathlib import Path
from io import BytesIO
import dnfile
from PIL import Image
root=Path(__file__).resolve().parent.parent
d=json.loads((root/'OldSaber/Assets/ClassicObstacles.json').read_text())
seen=set();cubes=[];visible=[]
for n in d['nodes']:
 assert n['parent'] is None or n['parent'] in seen
 seen.add(n['id'])
 assert all(math.isfinite(v) for prop in ('position','rotation','scale') for v in n[prop].values())
 if n.get('proceduralCube'):cubes.append(n['id'])
 if n.get('materials') and n['name']!='DepthWrite':visible.append(n)
 if 'mesh' in n:assert n['mesh'] in d['meshes']
 for k in n.get('materials',[]):assert k in d['materials']
assert len(cubes)==2 and len(visible)==22
assert set(d['bindings'].values()) <= seen and len(d['bindings'])==23
for mesh in d['meshes'].values():
 assert len(mesh['vertices'])==24
 for tris in mesh['triangles']:assert len(tris)%3==0 and all(0<=i<len(mesh['vertices']) for i in tris)
for key,texture in d['textures'].items():
 raw=base64.b64decode(texture);assert raw==(root/'extracted/OldSaberObstacles'/(key+'.png')).read_bytes()
 image=Image.open(BytesIO(raw));assert image.width>1 and image.height>1
for key,mat in d['materials'].items():
 source=json.loads((root/'extracted/OldSaberObstacles'/(key+'.json')).read_text())
 assert mat['floats']==dict(source['m_SavedProperties']['m_Floats'])
 for texture in mat['textures'].values():assert texture['id'] in d['textures']
pe=dnfile.dnPE(str(root/'Managed/Main.dll'))
types={str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
for typ,method,params in [('ObstacleMaterialSetter','Init',{'screenDisplacementEffects'}),('StretchableObstacle','SetAllProperties',{'width','height','length'}),('StretchableObstacle','SetSizeAndOffset',{'width','height','length'})]:
 m=next(m.row for m in types[typ].MethodList if str(m.row.Name)==method)
 assert params<={str(p.row.Name) for p in m.ParamList}
for typ,fields in [('StretchableObstacle',{'_obstacleFrame'}),('ObstacleController',{'_obstacleMaterialSetter'}),('ObstacleMaterialSetter',{'_hwCoreMaterial','_obstacleCoreRenderer'})]:
 assert fields<={str(f.row.Name) for f in types[typ].FieldList}
pe=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'));resources={str(r.name):r.data for r in pe.net.resources}
assert resources['OldSaber.ClassicObstacles.json']==(root/'OldSaber/Assets/ClassicObstacles.json').read_bytes()
assert b'old-obstacles' in resources['OldSaber.Settings.bsml']
assert json.loads(resources['OldSaber.manifest.json'])['version']=='1.0.0'
print('PASS: 22 obstacle surfaces/frame parts, 23 size bindings, source mesh/material/texture data, modern hook parameters and fields, embedded asset and toggle.')
print('NOT TESTED: Unity distortion rendering, VR eyes, pooling and gameplay collision integration.')
