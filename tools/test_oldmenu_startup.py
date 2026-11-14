"""Exercise the actual initializer with BoolSO's null-dereferencing implicit conversion."""
from pathlib import Path
import subprocess,json,os,re
root=Path(__file__).resolve().parent.parent
source=(root/'OldMenu/Behaviours/TubeLight.cs').read_text()
body=re.search(r'public void Construct\(\)\s*\{(.*?)\n        \}',source,re.S).group(1)
out=root/'dist/tests';out.mkdir(exist_ok=True)
harness='''using System;
class ScriptableObject { public static T CreateInstance<T>() where T:new() => new T(); }
class BoolSO { public bool value; public static implicit operator bool(BoolSO obj)=>obj.value; }
class Test {
 BoolSO _mainEffectPostProcessEnabled;
 public void Construct(){BODY}
 static void Main(){var t=new Test();t.Construct();
 if(t._mainEffectPostProcessEnabled==null || !t._mainEffectPostProcessEnabled.value)throw new Exception("Not initialized");
 var original=t._mainEffectPostProcessEnabled;t.Construct();
 if(!ReferenceEquals(original,t._mainEffectPostProcessEnabled))throw new Exception("Recreated on reinjection");
 original.value=false;t.Construct();
 if(!ReferenceEquals(original,t._mainEffectPostProcessEnabled) || original.value)throw new Exception("Confused false value with null object");
 Console.WriteLine("PASS: OldMenu light initializes from null; repeated injection preserves true and false BoolSO objects.");
 }}'''.replace('BODY',body)
(out/'OldMenuStartupTests.cs').write_text(harness)
version,base=subprocess.check_output(['dotnet','--list-sdks'],text=True).strip().splitlines()[-1].split(' ',1)
base=Path(base.strip('[]'));csc=base/version/'Roslyn/bincore/csc.dll'
pack=sorted((base.parent/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((pack/'ref').iterdir());dll=out/'OldMenuStartupTests.dll'
args=['-nologo','-nostdlib+','-target:exe',f'-out:{dll}']+[f'-r:{p}' for p in refs.glob('*.dll')]+[str(out/'OldMenuStartupTests.cs')]
(out/'OldMenuStartupTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':pack.name}}}))
env=dict(os.environ,DOTNET_CLI_HOME='/tmp/classic-dotnet')
subprocess.run(['dotnet',str(csc),*args],check=True,env=env)
subprocess.run(['dotnet',str(dll)],check=True,env=env)
