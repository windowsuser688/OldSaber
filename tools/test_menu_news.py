"""Run the actual C# leaderboard selection policy without a Unity player or NuGet packages."""
from pathlib import Path
import subprocess,json,os
root=Path(__file__).resolve().parent.parent
version,base=subprocess.check_output(['dotnet','--list-sdks'],text=True).strip().splitlines()[-1].split(' ',1)
base=Path(base.strip('[]'));csc=base/version/'Roslyn/bincore/csc.dll'
pack=sorted((base.parent/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((pack/'ref').iterdir())
out=root/'dist/tests';out.mkdir(exist_ok=True)
args=['-nologo','-nostdlib+','-target:exe',f'-out:"{out}/MenuNewsContentTests.dll"']
args += [f'-r:"{p}"' for p in refs.glob('*.dll')]
args += [f'-r:"{root}/Managed/Newtonsoft.Json.dll"']
import shutil
shutil.copyfile(root/'Managed/Newtonsoft.Json.dll',out/'Newtonsoft.Json.dll')
args += [f'"{root}/OldSaber/MenuNewsContent.cs"',f'"{root}/tests/MenuNewsContentTests.cs"']
rsp=out/'menu-news.rsp';rsp.write_text('\n'.join(args))
(out/'MenuNewsContentTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':pack.name}}}))
env=dict(os.environ,DOTNET_CLI_HOME='/tmp/classic-dotnet')
subprocess.run(['dotnet',str(csc),'-noconfig','@'+str(rsp)],check=True,env=env)
subprocess.run(['dotnet',str(out/'MenuNewsContentTests.dll')],check=True,env=env)
