"""Run the actual C# OldNotes asset loader with delayed-read Unity stand-ins without a Unity player or NuGet packages."""
from pathlib import Path
import subprocess,json,os
root=Path(__file__).resolve().parent.parent
version,base=subprocess.check_output(['dotnet','--list-sdks'],text=True).strip().splitlines()[-1].split(' ',1)
base=Path(base.strip('[]'));csc=base/version/'Roslyn/bincore/csc.dll'
pack=sorted((base.parent/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((pack/'ref').iterdir())
out=root/'dist/tests';out.mkdir(exist_ok=True)
args=['-nologo','-nostdlib+','-target:exe',f'-out:"{out}/OldNotesMaterialTests.dll"']
args += [f'-r:"{p}"' for p in refs.glob('*.dll')]
# Compile the actual material-state component with lifecycle stand-ins.
source=(root/'OldSaber/OldNotesAssets.cs').read_text()
component=out/'OldNotesMaterialState.cs'
component.write_text('using UnityEngine; using UnityEngine.Rendering; namespace OldSaber {\n'+source[source.index('    public sealed class OldNotesMaterialState'):])
args += [f'"{component}"',f'"{root}/tests/OldNotesMaterialTests.cs"']
rsp=out/'oldnotes-material.rsp';rsp.write_text('\n'.join(args))
(out/'OldNotesMaterialTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':pack.name}}}))
env=dict(os.environ,DOTNET_CLI_HOME='/tmp/classic-dotnet')
subprocess.run(['dotnet',str(csc),'-noconfig','@'+str(rsp)],check=True,env=env)
subprocess.run(['dotnet',str(out/'OldNotesMaterialTests.dll')],check=True,env=env)
