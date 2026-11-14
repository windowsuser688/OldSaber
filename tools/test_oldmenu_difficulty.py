"""Verify the source colors and hidden viewport used by the difficulty fix."""
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parent.parent
env=UnityPy.load(str(root/'OldMenu/Resources/content'))
found=False;viewport=False
for obj in env.objects:
 if obj.type.name!='MonoBehaviour':continue
 d=obj.read();script=d.m_Script.read().m_ClassName
 if script=='DifficultyTableCell':
  t=obj.read_typetree();color=obj.assets_file.objects[t['_highlightImage']['m_PathID']].read_typetree()['m_Color']
  for key,value in zip(('r','g','b','a'),(0,192/255,1,64/255)):assert abs(color[key]-value)<1e-6
  found=True
 if script=='Mask':
  g=d.m_GameObject.read();tr=g.m_Component[0].component.read()
  if g.m_Name=='Viewport' and tr.m_Father.read().m_GameObject.read().m_Name=='DifficultyTableView':
   assert not obj.read_typetree()['m_ShowMaskGraphic'];viewport=True
assert found and viewport
print('PASS: source difficulty viewport is hidden; original cyan highlight is #00C0FF at 64/255 alpha.')
