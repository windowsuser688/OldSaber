"""Export classic note meshes in Unity coordinates for runtime replacement."""
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).resolve().parent.parent
env=UnityPy.load(str(root/'Beat Saber_Data/sharedassets4.assets'))
meshes={}
for obj in env.objects:
    if obj.type.name!='Mesh' or obj.path_id not in (22,23): continue
    h=MeshHandler(obj.read());h.process()
    data={key:[list(v[:size]) for v in values] if values else [] for key,values,size in (
        ('vertices',h.m_Vertices,3),('normals',h.m_Normals,3),('uv',h.m_UV0,2),
        ('uv2',h.m_UV1,2),('tangents',h.m_Tangents,4),('colors',h.m_Colors,4))}
    data['triangles']=[[i for t in ts for i in t] for ts in h.get_triangles()]
    meshes['body' if obj.path_id==22 else 'arrow']=data
assert set(meshes)=={'body','arrow'}
(root/'OldSaber/Assets/ClassicNotes.json').write_text(json.dumps(meshes,separators=(',',':')))
print('Exported classic body and arrow meshes')
