"""Check the source HUD/font extraction and the embedded runtime artifact."""
import base64,json,math
from pathlib import Path
from io import BytesIO
from PIL import Image
import dnfile
root=Path(__file__).resolve().parent.parent
data=json.loads((root/'OldSaber/Assets/ClassicGUI.json').read_text())
seen=set();texts=set();images=set()
for n in data['nodes']:
    assert not n['parent'] or n['parent'] in seen
    seen.add(n['id'])
    for name in ('m_LocalPosition','m_LocalRotation','m_LocalScale'):
        assert all(math.isfinite(v) for v in n['transform'][name].values())
    if 'text' in n:
        t=n['text'];texts.add(t['id']);assert t['font'] in data['fonts']
        available={g['id'] for g in data['fonts'][t['font']]['glyphs']}
        assert all(ord(c) in available or c.isspace() for c in t['m_text']),t['m_text']
    if 'image' in n:images.add(n['image']['id'])
for key in ('score','combo','multiplier'):assert set(data['bindings'][key])<=texts
for key in ('energy','progress'):assert data['bindings'][key] in images
assert data['bindings']['energyPanel'] in seen
assert {'Beon SDF','Teko-Medium SDF','Teko-Medium SDF No Glow'}=={f['name'] for f in data['fonts'].values()}
for key,font in data['fonts'].items():
    raw=json.loads((root/'extracted/OldSaberGUI/objects'/(key+'.json')).read_text())
    assert font['glyphs']==raw['m_glyphInfoList'] and font['face']==raw['m_fontInfo']
    image=Image.open(BytesIO(base64.b64decode(data['textures'][font['atlas']])))
    assert image.size==(int(font['face']['AtlasWidth']),int(font['face']['AtlasHeight']))
    assert image.getextrema()[3][1]>0,'Font distance field must occupy alpha'
    assert all(c in {g['id'] for g in font['glyphs']} for c in range(48,58))
    for g in font['glyphs']:
        if g['id']==32 or g['width']<=0 or g['height']<=0:continue
        x=int(g['x']);y=image.height-int(g['y']+g['height']+.5);w=int(g['width']+.5);h=int(g['height']+.5)
        assert 0<=x<=x+w<=image.width and 0<=y<=y+h<=image.height,(key,g['id'])
assert (root/'extracted/OldSaberGUI/fonts/Beon.ttf').stat().st_size>0
assembly=dnfile.dnPE(str(root/'dist/Plugins/OldSaber.dll'));resources={str(r.name):r.data for r in assembly.net.resources}
assert resources['OldSaber.ClassicGUI.json']==(root/'OldSaber/Assets/ClassicGUI.json').read_bytes()
manifest=json.loads(resources['OldSaber.manifest.json']);assert manifest['version']=='1.0.0'
print('PASS: HUD parent order, counter bindings, Beon/Teko source metrics, glyph atlas coordinates, textures and embedded resource.')
print('NOT TESTED: Unity font rendering, HUD placement or gameplay lifecycle in VR.')
