"""Check extracted saber geometry and trail assets against the shipped resource."""
import base64,json,math
from pathlib import Path
import UnityPy,dnfile
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).resolve().parent.parent
data=json.loads((root/'OldSaber/Assets/ClassicSabers.json').read_text())
assert sorted(s['type'] for s in data['sabers'])==[0,1]
for key,m in data['meshes'].items():
    filename,pathid=key.rsplit('__',1)
    path=root/'Beat Saber_Data'/filename
    if not path.exists():path=root/'Beat Saber_Data/Resources'/filename
    env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.path_id==int(pathid) and Path(o.assets_file.name).name==filename)
    h=MeshHandler(obj.read());h.process()
    assert m['vertices']==[list(v) for v in h.m_Vertices]
    assert m['triangles']==[[i for t in sub for i in t] for sub in h.get_triangles()]
    assert all(math.isfinite(x) for v in m['vertices'] for x in v)
    assert all(0<=i<len(m['vertices']) for sub in m['triangles'] for i in sub)
for saber in data['sabers']:
    seen=set()
    for n in saber['nodes']:
        assert n['parent'] is None or n['parent'] in seen
        seen.add(n['id'])
        if 'mesh' in n:assert n['mesh'] in data['meshes']
    trail=saber['trail']
    assert trail['start'] in seen and trail['end'] in seen
    assert (trail['MaxFrame'],trail['Granularity'],trail['_skipFirstFrames'])==(20,60,4)
    tex=data['materials'][trail['rendererMaterial']]['textures']['_MainTex']
    assert base64.b64decode(data['textures'][tex])==(root/'extracted/OldSaberSabers/textures'/(tex+'.png')).read_bytes()
assembly=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'))
resources={str(r.name):r.data for r in assembly.net.resources}
assert resources['OldSaber.ClassicSabers.json']==(root/'OldSaber/Assets/ClassicSabers.json').read_bytes()
assert json.loads(resources['OldSaber.manifest.json'])['version']=='1.0.0'
print('PASS: both classic saber hierarchies, exact source meshes, trail texture/settings and embedded resources (version 1.0.0).')
