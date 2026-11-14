"""Validate original layout provenance and the installed BeatLeader data contract."""
from pathlib import Path
import json,dnfile
root=Path(__file__).resolve().parent.parent
d=json.loads((root/'OldSaber/Assets/ClassicMenu.json').read_text());nodes={n['id']:n for n in d['nodes']}
assert d['screens']['leaderboard']=='level1__1846'
assert d['screens']['leaderboardRow']=='level1__1650'
assert nodes['level1__1580']['text']['m_text']=='Highscores'
assert nodes['level1__1779']['transform']['m_SizeDelta']['x']==64
for id in ['level1__1811','level1__1700','level1__1529','level1__1462']:
 n=nodes[id];original=json.loads((root/'extracted/OldMenu/objects'/f'{id}.json').read_text())
 for key in ['m_AnchorMin','m_AnchorMax','m_Pivot','m_SizeDelta','m_AnchoredPosition']:
  assert n['transform'][key]==original[key],(id,key)
pe=dnfile.dnPE(str(Path.home()/'.local/share/BSManager/BSInstances/1.40.8/Plugins/BeatLeader.dll'))
types={str(t.TypeNamespace)+'.'+str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
for type,fields in {'BeatLeader.ScoresTableContent':['MainRowContents','ExtraRowContent'],'BeatLeader.Components.AbstractScoresTable`1':['_content','_mainRows','_extraRow'],'BeatLeader.Components.Pagination':['_upInteractable','_aroundInteractable','_downInteractable']}.items():
 assert set(fields)<={str(f.row.Name) for f in types[type].FieldList},type
for type,methods in {'BeatLeader.Components.ScopeSelector':['NavGlobalOnClick','NavFriendsOnClick'],'BeatLeader.Components.Pagination':['UpOnClick','AroundOnClick','DownOnClick'],'BeatLeader.IScoreRowContent':['GetValue'],'BeatLeader.Components.ScoreRow':['OnClick']}.items():
 assert set(methods)<={str(m.row.Name) for m in types[type].MethodList},type
print('PASS: extracted 0.10.1 Highscores/row transforms and installed BeatLeader score, scope, paging and detail contracts.')
