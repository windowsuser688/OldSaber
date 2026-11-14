"""Validate original menu assets and the modern controller contracts used by the port."""
import base64,json,math,zipfile
from pathlib import Path
from io import BytesIO
from PIL import Image
import dnfile
root=Path(__file__).resolve().parent.parent
menu_root=root/'extracted'/'OldMenu'
if not menu_root.exists():
    menu_root=root/'extracted'/'OldSaberMenu'
    if not menu_root.exists():
        raise FileNotFoundError('Missing extracted menu directory: extracted/OldMenu or extracted/OldSaberMenu')
d=json.loads((root/'OldSaber/Assets/ClassicMenu.json').read_text())
seen=set();buttons=[]
for n in d['nodes']:
 assert n['parent'] is None or n['parent'] in seen
 seen.add(n['id'])
 assert all(math.isfinite(v) for prop in ('m_LocalPosition','m_LocalRotation','m_LocalScale') for v in n['transform'][prop].values())
 if 'button' in n:buttons.append(n['name'])
 if 'text' in n:assert n['text']['font'] in d['fonts']
 if 'image' in n and 'texture' in n['image']:assert n['image']['texture'] in d['textures']
assert set(d['screens'].values())<=seen
for n in d['nodes']:
 if 'image' in n:
  source=json.loads((menu_root/'objects'/(n['image']['id']+'.json')).read_text())
  assert n['image'].get('m_PreserveAspect')==source.get('m_PreserveAspect')
assert {'SoloButton','PartyButton','TutorialButton','SettingsButton','QuitButton'}<=set(buttons)
for key,metadata in d['sprites'].items():
 img=Image.open(BytesIO(base64.b64decode(d['textures'][key])))
 b=metadata['border'];assert b['x']+b['z']<=img.width and b['y']+b['w']<=img.height
 source=json.loads((menu_root/'objects'/(key+'.json')).read_text())
 assert b==source['m_Border'] and metadata['pixelsPerUnit']==source['m_PixelsToUnits']
for key,f in d['fonts'].items():
 source=json.loads((menu_root/'objects'/(key+'.json')).read_text())
 assert f['face']==source['m_fontInfo'] and f['glyphs']==source['m_glyphInfoList']
 assert ord('+') in {g['id'] for g in f['glyphs']},'Expert+ glyph missing'
pe=dnfile.dnPE(str(root/'Managed/Main.dll'));types={str(t.TypeName):t for t in pe.net.mdtables.TypeDef}
for name,fields in {
'MainMenuViewController':['_soloButton','_partyButton','_optionsButton','_quitButton','_howToPlayButton','_campaignButton','_multiplayerButton','_beatmapEditorButton'],
'StandardLevelDetailView':['_beatmapDifficultySegmentedControlController','_beatmapCharacteristicSegmentedControlController','_beatmapLevel','_levelParamsPanel','_playerData','_favoriteToggle'],
'LevelFilteringNavigationController':['_beatmapLevelsModel','_enableCustomLevels'],
'LevelCollectionViewController':['_levelCollectionTableView'],
'LevelCollectionTableView':['_beatmapLevels','_showLevelPackHeader'],
'LevelParamsPanel':['_notesCountText','_obstaclesCountText'],
'BeatmapCharacteristicSegmentedControlController':['_currentlyAvailableBeatmapCharacteristics','_segmentedControl'],
'BeatmapDifficultySegmentedControlController':['_difficultySegmentedControl','_difficulties']}.items():
 assert set(fields)<={str(f.row.Name) for f in types[name].FieldList}
assert 'HandleDifficultySegmentedControlDidSelectCell' in {str(m.row.Name) for m in types['BeatmapDifficultySegmentedControlController'].MethodList}
for typename,methods in {'LevelFilteringNavigationController':['HandleLevelSearchViewControllerDidFilterBeatmapLevelCollection','UpdateCustomSongs'],'LevelCollectionTableView':['SelectLevel'],'BeatmapCharacteristicSegmentedControlController':['HandleBeatmapCharacteristicSegmentedControlDidSelectCell']}.items():
 assert set(methods)<={str(m.row.Name) for m in types[typename].MethodList}
bsml=dnfile.dnPE(str(Path.home()/'.local/share/BSManager/BSInstances/1.40.8/Plugins/BSML.dll'));bt={str(t.TypeName):t for t in bsml.net.mdtables.TypeDef}
for name,methods in {'BSMLSettings':['PresentSettings','AddSettingsMenu','RemoveSettingsMenu'],'MenuButtons':['get_Buttons'],'MenuButton':['ButtonClicked','get_Interactable']}.items():
 assert set(methods)<={str(m.row.Name) for m in bt[name].MethodList}
pe=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'));resources={str(r.name):r.data for r in pe.net.resources}
assert resources['OldSaber.ClassicMenu.json']==(root/'OldSaber/Assets/ClassicMenu.json').read_bytes()
assert json.loads(resources['OldSaber.manifest.json'])['version']=='1.0.0'
with zipfile.ZipFile(root/'dist/OldSaber-1.0.0-bs1.40.8.zip') as z:
 assert z.read('Plugins/OldSaber.dll')==(root/'dist/Plugins/OldSaber.dll').read_bytes()
assert b'old-menu' in resources['OldSaber.Settings.bsml']
assert b'restart required' in resources['OldSaber.Settings.bsml']
assert {'changelog','logo'} <= set(d['screens'])
e=json.loads((root/'OldSaber/Assets/ClassicMenuEnvironment.json').read_text())
assert resources['OldSaber.ClassicMenuEnvironment.json']==(root/'OldSaber/Assets/ClassicMenuEnvironment.json').read_bytes()
assert e['sceneName']=='Menu' and e['sourceFile']=='level1'
parents=set()
for n in e['nodes']:
 assert not n['parent'] or n['parent'] in parents
 parents.add(n['id'])
 if 'mesh' in n:
  mesh=e['meshes'][n['mesh']]
  for triangles in mesh['triangles']:assert all(0<=i<len(mesh['vertices']) for i in triangles)
  assert n['batch']['firstSubMesh']+n['batch']['subMeshCount']<=len(mesh['triangles'])
assert len(e['meshes'])==5 and any(n.get('beam') for n in e['nodes'])
assert '_data' in {str(f.row.Name) for f in types['MenuEnvironmentManager'].FieldList}
print('PASS: six source menu layouts, extracted menu environment, button assets, sliced sprite borders, font metrics and Expert+ glyph, modern controller and BSML contracts, settings and packaged assets.')
print('NOT TESTED: VR pointer interactions, menu transitions, startup/restart in Unity, other menu-mod compatibility.')
