"""Check local provider reflection contracts without requiring either provider at runtime."""
from pathlib import Path
import dnfile
root=Path(__file__).resolve().parent.parent
pe=dnfile.dnPE(str(root/'Managed/Main.dll'));types={str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
assert '_beatmapKey' in {str(f.row.Name) for f in types['PlatformLeaderboardViewController'].FieldList}
plugin=Path.home()/'.local/share/BSManager/BSInstances/1.40.8/Plugins/BeatLeader.dll'
if plugin.exists():
 pe=dnfile.dnPE(str(plugin));types={str(t.TypeNamespace)+'.'+str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
 profile=types['BeatLeader.DataManager.ProfileManager']
 assert 'HasProfile' in {str(f.row.Name) for f in profile.FieldList}
 assert 'get_Profile' in {str(m.row.Name) for m in profile.MethodList}
 assert 'get_pp' in {str(m.row.Name) for m in types['BeatLeader.Models.Player'].MethodList}
 assert 'BeatLeader.ViewControllers.LeaderboardView' in types
pe=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'))
assert not {'BeatLeader','ScoreSaber','LeaderboardCore'} & {str(r.Name) for r in pe.net.mdtables.AssemblyRef}
print('PASS: native beatmap binding, installed BeatLeader view/profile/PP contracts, no required BeatLeader/ScoreSaber/LeaderboardCore assembly references.')
print('NOT TESTED: live leaderboard data, VR layout, or ScoreSaber native profile-panel rendering.')
