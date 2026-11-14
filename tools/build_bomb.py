"""Follow the 0.10.1 BombNote prefab to its mesh and export Unity-space geometry."""
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
from UnityPy.classes.PPtr import PPtr
root=Path(__file__).resolve().parent.parent
env=UnityPy.load(str(root/'Beat Saber_Data/sharedassets4.assets'))
def ref(obj,p):return PPtr(**p,assetsfile=obj.assets_file).deref()
def components(go):return [ref(go,c['component']) for c in go.read_typetree()['m_Component']]
bomb=next(o for o in env.objects if o.type.name=='GameObject' and o.read_typetree()['m_Name']=='BombNote')
transform=next(o for o in components(bomb) if o.type.name=='Transform')
filters=[]
def walk(t):
    go=ref(t,t.read_typetree()['m_GameObject'])
    filters.extend(o for o in components(go) if o.type.name=='MeshFilter')
    for child in t.read_typetree()['m_Children']:walk(ref(t,child))
walk(transform)
assert len(filters)==1
obj=ref(filters[0],filters[0].read_typetree()['m_Mesh'])
h=MeshHandler(obj.read());h.process()
data={key:[list(v[:size]) for v in values] if values else [] for key,values,size in (
    ('vertices',h.m_Vertices,3),('normals',h.m_Normals,3),('uv',h.m_UV0,2),
    ('uv2',h.m_UV1,2),('tangents',h.m_Tangents,4),('colors',h.m_Colors,4))}
data['triangles']=[[i for triangle in triangles for i in triangle] for triangles in h.get_triangles()]
data['source']={'gameVersion':'0.10.1','file':Path(obj.assets_file.name).name,'prefabPathID':bomb.path_id,'meshPathID':obj.path_id,'name':obj.read().m_Name}
body=ref(filters[0],filters[0].read_typetree()['m_GameObject'])
renderer=next(o for o in components(body) if o.type.name=='MeshRenderer')
material=ref(renderer,renderer.read_typetree()['m_Materials'][0]).read_typetree()
data['material']={'name':material['m_Name'],'floats':dict(material['m_SavedProperties']['m_Floats']),
                  'colors':dict(material['m_SavedProperties']['m_Colors'])}
(root/'OldSaber/Assets/ClassicBomb.json').write_text(json.dumps(data,separators=(',',':')))
print(f"Extracted {data['source']}: {len(h.m_Vertices)} vertices, {sum(len(s)//3 for s in data['triangles'])} triangles")
