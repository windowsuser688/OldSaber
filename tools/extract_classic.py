"""Export the supplied classic scene and its referenced assets without editing game files."""
import argparse
import base64
import hashlib
import json
from collections import Counter, deque
from pathlib import Path

import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


class ClassicTypeTreeGenerator(TypeTreeGenerator):
    def get_nodes_up(self, assembly, fullname):
        node = super().get_nodes_up(assembly, fullname)
        # The generator omits the native MonoBehaviour enabled-byte padding
        # for Unity 2017. Without it, m_Script is read three bytes too early.
        for child in node.m_Children:
            if child.m_Name == "m_Enabled":
                child.m_MetaFlag |= 0x4000
        return node


def json_value(value):
    if isinstance(value, bytes):
        return {"base64": base64.b64encode(value).decode("ascii")}
    raise TypeError(type(value).__name__)


def pointers(value):
    if isinstance(value, dict):
        if "m_FileID" in value and "m_PathID" in value:
            if value["m_PathID"]:
                yield value
        else:
            for child in value.values():
                yield from pointers(child)
    elif isinstance(value, list):
        for child in value:
            yield from pointers(child)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("data", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    metadata = UnityPy.load(str(args.data / "globalgamemanagers"))
    settings = {o.type.name: o.read_typetree() for o in metadata.objects
                if o.type.name in ("BuildSettings", "PlayerSettings")}
    build = settings["BuildSettings"]
    scene_index = next(i for i, s in enumerate(build["scenes"])
                       if s.endswith("/DefaultEnvironment.unity"))
    scene_path = args.data / f"level{scene_index}"
    env = UnityPy.load(str(scene_path))
    generator = ClassicTypeTreeGenerator(build["m_Version"])
    generator.load_local_dll_folder(str(args.data / "Managed"))
    env.typetree_generator = generator
    pending = deque(env.objects)
    seen = set()
    records, errors = [], []
    args.output.mkdir(parents=True, exist_ok=True)
    for folder in ("objects", "meshes", "textures", "raw"):
        (args.output / folder).mkdir(exist_ok=True)
    while pending:
        obj = pending.popleft()
        key = f"{Path(obj.assets_file.name).name}__{obj.path_id}"
        if key in seen:
            continue
        seen.add(key)
        record = {"id": key, "type": obj.type.name,
                  "file": Path(obj.assets_file.name).name, "pathID": obj.path_id}
        try:
            tree = obj.read_typetree()
            record["name"] = tree.get("m_Name", "")
            if obj.type.name == "MonoBehaviour":
                head = obj.parse_monobehaviour_head()
                # Use UnityPy's native header parser for native fields. Its
                # generated-tree fast reader mishandles this header's padding.
                tree["m_Script"] = {"m_FileID": head.m_Script.m_FileID,
                                    "m_PathID": head.m_Script.m_PathID}
                tree["m_Name"] = head.m_Name
                script = head.m_Script.deref().read()
                record["script"] = script.m_ClassName
            (args.output / "objects" / f"{key}.json").write_text(
                json.dumps(tree, default=json_value, indent=2), encoding="utf-8")
            references = []
            from UnityPy.classes.PPtr import PPtr
            for ptr in pointers(tree):
                try:
                    target = PPtr(m_FileID=ptr["m_FileID"], m_PathID=ptr["m_PathID"],
                                  assetsfile=obj.assets_file).deref()
                    references.append(f"{Path(target.assets_file.name).name}__{target.path_id}")
                    pending.append(target)
                except Exception as exc:
                    errors.append({"object": key, "reference": ptr, "error": str(exc)})
            record["references"] = references
            if obj.type.name == "Mesh":
                (args.output / "meshes" / f"{key}.obj").write_text(obj.read().export(), encoding="utf-8")
            elif obj.type.name == "Texture2D":
                if tree["m_Width"] > 0 and tree["m_Height"] > 0:
                    obj.read().image.save(args.output / "textures" / f"{key}.png")
                else:
                    record["previewSkipped"] = "Empty runtime-generated texture (zero dimensions)."
        except Exception as exc:
            record["error"] = str(exc)
            errors.append({"object": key, "error": str(exc)})
        # Retain exact bytes even if decoding or preview export fails.
        (args.output / "raw" / f"{key}.bin").write_bytes(obj.get_raw_data())
        records.append(record)
    report = {
        "sourceGameVersion": settings["PlayerSettings"]["bundleVersion"],
        "sourceUnityVersion": build["m_Version"],
        "targetGameVersion": "1.40.8",
        "scene": build["scenes"][scene_index],
        "sceneFile": scene_path.name,
        "sceneSHA256": hashlib.sha256(scene_path.read_bytes()).hexdigest(),
        "counts": dict(Counter(r["type"] for r in records)),
        "objects": records, "errors": errors,
        "status": "Source extraction only; not an installable mod or Unity asset bundle."
    }
    (args.output / "manifest.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items() if k not in ("objects", "errors")}, indent=2))
    print(f"Extraction issues: {len(errors)} (see manifest.json)")


if __name__ == "__main__":
    main()
