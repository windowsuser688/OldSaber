"""Compile against the user's local 1.40.8 assemblies using the installed .NET SDK."""
from pathlib import Path
import subprocess,os,json,zipfile
root=Path(__file__).resolve().parent.parent
managed=root/'Managed'
if not (managed/'Main.dll').exists(): raise SystemExit('Missing 1.40.8 Managed/Main.dll')
sdks=subprocess.check_output(['dotnet','--list-sdks'],text=True).strip().splitlines()
version,base=sdks[-1].split(' ',1)
csc=Path(base.strip('[]'))/version/'Roslyn/bincore/csc.dll'
out=root/'dist/Plugins';out.mkdir(parents=True,exist_ok=True)
args=['-nologo','-nostdlib+','-target:library','-langversion:latest','-optimize+', '-deterministic+','-debug:portable',f'-pdb:"{out}/OldSaber.pdb"',f'-out:"{out}/OldSaber.dll"']
args += [f'-reference:"{p}"' for p in sorted(managed.glob('*.dll'))]
args += [f'-resource:"{p}",OldSaber.{p.name}' for p in sorted((root/'OldSaber/Assets').glob('*.json')) if p.name!='ClassicNotes.json']
# Use the supplied project's ScriptableObject type and adapted asset bundle.
plugin_dir=Path(os.environ.get('BEATSABER_PLUGINS',str(Path.home()/'.local/share/BSManager/BSInstances/1.40.8/Plugins')))
for name in ('BSML.dll','SiraUtil.dll'):
 p=plugin_dir/name
 if not p.exists(): raise SystemExit(f'Missing {p}; set BEATSABER_PLUGINS to your 1.40.8 Plugins directory')
 args.append(f'-reference:"{p}"')
args += [f'"{root}/OldNotes/DataStores/PluginDataSO.cs"']
args += [f'-resource:"{root}/OldSaber/Assets/OldNotes.bundle",OldSaber.OldNotes.bundle']
args += [f'-resource:"{root}/OldSaber/Assets/Settings.bsml",OldSaber.Settings.bsml']
args += [f'-resource:"{root}/OldSaber/manifest.json",OldSaber.manifest.json']
old_saber_exclude = {
    'OldSaber/ClassicMenu.cs',
    'OldSaber/ClassicLeaderboard.cs',
    'OldSaber/ClassicMenuNews.cs',
    'OldSaber/ClassicReleaseInfo.cs',
    'OldSaber/ClassicSongMenu.cs',
    'OldSaber/ClassicSongBrowser.cs',
    'OldSaber/ClassicMenuEnvironment.cs',
    'OldSaber/ClassicScreen.cs',
}
args += [
    f'"{p}"' for p in sorted((root/'OldSaber').glob('*.cs'))
    if p.relative_to(root).as_posix() not in old_saber_exclude
]
rsp=root/'dist/build.rsp';rsp.write_text('\n'.join(args))
env=dict(os.environ,DOTNET_CLI_HOME='/tmp/classic-dotnet')
subprocess.run(['dotnet',str(csc),'-noconfig','@'+str(rsp)],check=True,env=env)
manifest=json.loads((root/'OldSaber/manifest.json').read_text())
package=f"OldSaber-{manifest['version']}-bs{manifest['gameVersion']}.zip"
with zipfile.ZipFile(root/'dist'/package,'w',zipfile.ZIP_DEFLATED) as z:
 z.write(out/'OldSaber.dll','Plugins/OldSaber.dll')
 z.write(out/'OldSaber.pdb','Plugins/OldSaber.pdb')
 if (root/'INSTALL.md').exists():z.write(root/'INSTALL.md','OldSaber-INSTALL.md')
print('Built dist/'+package)
