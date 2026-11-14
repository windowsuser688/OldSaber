"""Check every bundled script and event against the compiled 1.40.8 plugin."""
from pathlib import Path
import re, zipfile
import UnityPy,dnfile
root=Path(__file__).resolve().parent.parent
cache={}
def types(path):
    path=str(path)
    if path not in cache:
        pe=dnfile.dnPE(path)
        cache[path]={str(t.TypeNamespace)+'.'+str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
    return cache[path]
plugin=root/'dist/Plugins/OldSaber.dll'
compiled=types(plugin)
env=UnityPy.load(str(root/'OldSaber/Assets/OldMenu.bundle'))
scripts={}
for o in env.objects:
    if o.type.name!='MonoScript':continue
    d=o.read_typetree();name=d['m_Namespace']+'.'+d['m_ClassName'];assembly=d['m_AssemblyName']
    path=plugin if assembly=='OldSaber.dll' else root/'Managed'/assembly
    assert path.exists(), (name,assembly)
    assert name in types(path),(name,assembly)
    scripts[o.path_id]=(name,assembly)
# Confirm original prefab events still target methods compiled into OldSaber.
objects={o.path_id:o for o in env.objects}
def walk(value):
    if isinstance(value,dict):
        if 'm_MethodName' in value and value['m_MethodName'] and value.get('m_Target',{}).get('m_PathID'):
            target=objects[value['m_Target']['m_PathID']].read_typetree()
            script=scripts.get(target.get('m_Script',{}).get('m_PathID'))
            if script and script[1]=='OldSaber.dll':
                assert value['m_MethodName'] in {str(m.row.Name) for m in compiled[script[0]].MethodList},(script,value['m_MethodName'])
        for v in value.values():walk(v)
    elif isinstance(value,list):
        for v in value:walk(v)
for o in env.objects:
    if o.type.name=='MonoBehaviour':walk(o.read_typetree())
# Fields used by legacy environment adapters must actually exist in this game.
alltypes={}
for p in (root/'Managed').glob('*.dll'):
    if p.name.startswith(('System','Unity','Mono','mscorlib')):continue
    for n,t in types(p).items():alltypes[n.rsplit('.',1)[-1]]=t
for p in (root/'OldMenu').rglob('*.cs'):
    for typ,field in re.findall(r'FieldAccessor<(\w+),[^>]+>\.GetAccessor\("([^"]+)"\)',p.read_text(encoding='utf-8-sig')):
        assert field in {str(f.row.Name) for f in alltypes[typ].FieldList},(p,typ,field)
with zipfile.ZipFile(root/'dist/OldSaber-1.0.0-bs1.40.8.zip') as z:
    assert z.read('Plugins/OldSaber.dll')==plugin.read_bytes()
    assert z.read('Plugins/OldSaber.pdb')==(root/'dist/Plugins/OldSaber.pdb').read_bytes()
print(f'PASS: {len(scripts)} bundle script identities, prefab event methods, native field accessors, DLL/PDB package integrity.')
print('NOT TESTED: Unity prefab instantiation, VR input/rendering, live BeatLeader requests and gameplay transitions.')
