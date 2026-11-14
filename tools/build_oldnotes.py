"""Adapt the supplied OldNotes bundle's script identity for embedding in OldSaber."""
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parent.parent
env=UnityPy.load(str(root/'OldNotes/Resources/content'))
found=0
for obj in env.objects:
    if obj.type.name!='MonoScript': continue
    data=obj.read_typetree()
    if data['m_ClassName']=='PluginDataSO' and data['m_Namespace']=='ClassiNotes.DataStores':
        data['m_AssemblyName']='OldSaber.dll'
        obj.save_typetree(data)
        found+=1
assert found==1, 'Expected the OldNotes PluginDataSO script'
out=root/'OldSaber/Assets/OldNotes.bundle'
out.write_bytes(env.file.save())
print('Adapted OldNotes bundle script identity; material/shader/prefab data preserved')
