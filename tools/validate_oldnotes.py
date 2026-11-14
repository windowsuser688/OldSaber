"""Validate bundle port, serialized class layout, GUI bindings and packaged resources."""
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import UnityPy
import dnfile
root=Path(__file__).resolve().parent.parent
source=UnityPy.load(str(root/'OldNotes/Resources/content'))
ported=UnityPy.load(str(root/'OldSaber/Assets/OldNotes.bundle'))
a={o.path_id:o for o in source.objects}
b={o.path_id:o for o in ported.objects}
assert a.keys()==b.keys()
changes=[]
for key,obj in a.items():
    if obj.get_raw_data()==b[key].get_raw_data():continue
    changes.append(obj.type.name)
    before=obj.read_typetree();after=b[key].read_typetree()
    assert before['m_AssemblyName']=='ClassiNotes.dll'
    before['m_AssemblyName']='OldSaber.dll'
    assert before==after
assert changes==['MonoScript'],changes
assert 'assets/plugindataso.asset' in ported.container
# The exact supplied ScriptableObject source is compiled, preserving bundle field layout.
assembly=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'))
types=assembly.net.mdtables.TypeDef.rows
so=next(t for t in types if str(t.TypeNamespace)=='ClassiNotes.DataStores' and str(t.TypeName)=='PluginDataSO')
fields={str(f.row.Name) for f in so.FieldList}
assert {'_noteMaterial','_noteDebrisMaterial','_reflectionProbeContainer','_noteTrailPS','_bombNoteTrailPS'}<=fields
assert not any(str(t.TypeName)=='ClassicNotes' for t in types),'Obsolete mesh swap must not run alongside OldNotes'
resources={str(r.name):r.data for r in assembly.net.resources}
assert resources['OldSaber.OldNotes.bundle']==(root/'OldSaber/Assets/OldNotes.bundle').read_bytes()
xml=ET.fromstring(resources['OldSaber.Settings.bsml'])
toggles=xml.findall('.//bool-setting')
assert {t.attrib['value'] for t in toggles}=={'old-environment','old-notes','old-sabers','old-gui'}
assert all(t.attrib['apply-on-change']=='true' for t in toggles)
settings=(root/'OldSaber/Settings.cs').read_text()
assert all('[UIValue("'+t.attrib['value']+'")]' in settings for t in toggles)
manifest=json.loads(resources['OldSaber.manifest.json'])
assert {'BSIPA','BeatSaberMarkupLanguage','SiraUtil'}<=manifest['dependsOn'].keys()
print('PASS: OldNotes shader/material/prefab bytes preserved; only script assembly identity changed.')
print('PASS: compiled ScriptableObject fields, embedded bundle, four GUI bindings and required dependencies.')
print('NOT TESTED: in-game GUI interaction, config writes or shader rendering.')

# The shipped bomb geometry must match the mesh referenced by the 0.10.1 prefab.
from UnityPy.helpers.MeshHelper import MeshHandler
bomb=json.loads(resources['OldSaber.ClassicBomb.json'])
legacy=UnityPy.load(str(root/'Beat Saber_Data/sharedassets4.assets'))
objects={o.path_id:o for o in legacy.objects if Path(o.assets_file.name).name=='sharedassets4.assets'}
assert objects[92].read_typetree()['m_Name']=='BombNote'
assert objects[327].read_typetree()['m_Mesh']=={'m_FileID':0,'m_PathID':25}
mesh=objects[25];h=MeshHandler(mesh.read());h.process()
assert bomb['source']['meshPathID']==25 and mesh.read().m_Name=='Bomb'
assert bomb['vertices']==[list(v) for v in h.m_Vertices]
assert bomb['normals']==[list(v) for v in h.m_Normals]
assert bomb['triangles']==[[i for triangle in triangles for i in triangle] for triangles in h.get_triangles()]
assert len(bomb['vertices'])==214 and sum(len(s)//3 for s in bomb['triangles'])==100
assert all(0<=i<214 for sub in bomb['triangles'] for i in sub)
assert resources['OldSaber.ClassicBomb.json']==(root/'OldSaber/Assets/ClassicBomb.json').read_bytes()
print('PASS: embedded bomb mesh matches the 0.10.1 BombNote source vertices, normals and triangles exactly.')

material=objects[2].read_typetree()['m_SavedProperties']
assert bomb['material']['floats']==dict(material['m_Floats'])
assert bomb['material']['colors']==dict(material['m_Colors'])
print('PASS: bomb appearance parameters match the original material.')
