"""Adapt OldMenu's bundled prefabs to the merged OldSaber assembly and 1.40.8."""
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parent.parent
env=UnityPy.load(str(root/'OldMenu/Resources/content'))
count=0
for obj in env.objects:
    if obj.type.name!='MonoScript':continue
    data=obj.read_typetree()
    if data['m_AssemblyName']=='OldschoolBeatSaber.dll':
        data['m_AssemblyName']='OldSaber.dll'; count+=1
    elif data['m_Namespace']=='HMUI' and data['m_ClassName']=='Interactable':
        data['m_AssemblyName']='Interactable.dll'
    obj.save_typetree(data)
assert count>=25, 'Missing OldMenu script references'
(root/'OldSaber/Assets/OldMenu.bundle').write_bytes(env.file.save())
print(f'Adapted {count} OldMenu script references; original prefab layout/assets retained.')

# Prefab instantiation does not preserve the original scene's static batching.
# Export each renderer's submesh range; positions are already in batch-root space.
import json
from UnityPy.helpers.MeshHelper import MeshHandler
root_go=env.container['assets/prefabs/oldschoolmenuenvironment.prefab'].read()
root_tr=next(c.component for c in root_go.m_Component if c.component.type.name=='Transform')
records=[];meshes={}
def visit(tr,path):
    t=tr.read();go=t.m_GameObject.read();components=[c.component for c in go.m_Component]
    renderer=next((c for c in components if c.type.name=='MeshRenderer'),None)
    meshfilter=next((c for c in components if c.type.name=='MeshFilter'),None)
    if renderer and meshfilter:
        d=renderer.read_typetree();batch=d['m_StaticBatchInfo']
        if batch['subMeshCount']:
            assert d['m_StaticBatchRoot']['m_PathID']==0,'Unexpected non-root static batch'
            mesh=meshfilter.read().m_Mesh;key=str(mesh.path_id)
            if key not in meshes:
                h=MeshHandler(mesh.read());h.process()
                meshes[key]={'vertices':[list(v) for v in h.m_Vertices],'normals':[list(v) for v in h.m_Normals] if h.m_Normals else [],
                    'uv':[list(v[:2]) for v in h.m_UV0] if h.m_UV0 else [],
                    'uv2':[list(v[:2]) for v in h.m_UV1] if h.m_UV1 else [],
                    'tangents':[list(v) for v in h.m_Tangents] if h.m_Tangents else [],
                    'colors':[list(v) for v in h.m_Colors] if h.m_Colors else [],
                    'triangles':[[i for tri in triangles for i in tri] for triangles in h.get_triangles()]}
            records.append({'path':path,'name':go.m_Name,'mesh':key,'first':batch['firstSubMesh'],'count':batch['subMeshCount']})
    for i,child in enumerate(t.m_Children):visit(child,path+[i])
visit(root_tr,[])
(root/'OldSaber/Assets/OldMenuGeometry.json').write_text(json.dumps({'renderers':records,'meshes':meshes},separators=(',',':')))
print(f'Exported {len(records)} static-batch renderer ranges for OldMenu geometry.')
