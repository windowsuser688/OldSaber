"""Guard the initialization order that previously left opaque boxes at world zero."""
from pathlib import Path
s=(Path(__file__).resolve().parent.parent/'OldMenu/Behaviours/TubeLight.cs').read_text()
a=s.index('var boxLightGameObject =');b=s.index('_parametricBoxControllerAccessor(ref _tubeBloomPrePassLight) =',a);setup=s[a:b]
assert 'boxLightTransform.SetParent(transform, false);' in setup
assert 'boxLightTransform.localPosition = Vector3.zero;' in setup
refresh=setup.index('boxLightParametricBoxController.Refresh();')
enable=setup.index('boxLightGameObject.SetActive(true);')
assert refresh<enable
for field in ['width','height','length','heightCenter','color','alphaMultiplier']:
 assert setup.index('boxLightParametricBoxController.'+field+' =')<refresh
assert setup.index('_meshRendererAccessor(ref boxLightParametricBoxController) =')<refresh
print('PASS: light-box source uses local parenting and initializes geometry/color before first activation (static initialization-order regression check).')
