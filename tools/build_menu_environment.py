"""Extract the original 0.10.1 menu environment using the scene mesh exporter."""
import UnityPy
from build_scene import export_scene
metadata=UnityPy.load('Beat Saber_Data/globalgamemanagers')
scenes=next(o.read_typetree()['scenes'] for o in metadata.objects if o.type.name=='BuildSettings')
export_scene('Menu','ClassicMenuEnvironment.json',scenes)
