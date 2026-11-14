"""Check the exported batch ranges against the supplied bundle and floor dimensions."""
import json
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parent.parent
x=json.loads((root/'OldSaber/Assets/OldMenuGeometry.json').read_text())
env=UnityPy.load(str(root/'OldMenu/Resources/content'))
go=env.container['assets/prefabs/oldschoolmenuenvironment.prefab'].read()
tr=next(c.component for c in go.m_Component if c.component.type.name=='Transform')
assert len(x['renderers'])==14
for r in x['renderers']:
 t=tr
 for i in r['path']:t=t.read().m_Children[i]
 g=t.read().m_GameObject.read();assert g.m_Name==r['name']
 renderer=next(c.component for c in g.m_Component if c.component.type.name=='MeshRenderer')
 batch=renderer.read_typetree()['m_StaticBatchInfo']
 assert (batch['firstSubMesh'],batch['subMeshCount'])==(r['first'],r['count'])
 m=x['meshes'][r['mesh']];triangles=m['triangles'][r['first']:r['first']+r['count']]
 assert len(triangles)==r['count']
 ids={i for part in triangles for i in part};assert ids and min(ids)>=0 and max(ids)<len(m['vertices'])
 if r['name']=='MirrorSurface':
  v=[m['vertices'][i] for i in ids];extent=[max(p[a] for p in v)-min(p[a] for p in v) for a in range(3)]
  assert abs(extent[0]-3)<.001 and extent[1]<.001 and abs(extent[2]-2)<.001,extent
print('PASS: all 14 batch ranges match source renderers; player platform is a flat 3 × 2 metre surface, not the entire environment mesh.')
