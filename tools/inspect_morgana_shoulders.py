"""Inspect Morgana's shoulder vertices and vertex groups around x in [0.10, 0.35], z in [1.15, 1.45]."""
import bpy
from pathlib import Path
import json

ROOT = Path(r"c:\workspace\multigame")
glb_path = ROOT / "assets/models/club/morgana.glb"

original_scene = bpy.context.window.scene
info = {}

try:
    scene = bpy.data.scenes.new("InspectMorgana")
    bpy.context.window.scene = scene
    bpy.ops.import_scene.gltf(filepath=str(glb_path))
    
    mesh = next(o for o in scene.objects if o.type == 'MESH')
    armature = next(o for o in scene.objects if o.type == 'ARMATURE')
    
    bone_heads = {b.name: [round(c, 4) for c in (armature.matrix_world @ b.head)] for b in armature.data.bones if any(s in b.name for s in ('Clavicle','UpperArm','Forearm','Chest','Neck','Shoulder'))}
    info["bones"] = bone_heads
    
    # Check suspect indices from deformation report
    suspect_indices = [13015, 13017, 12767, 12769, 42606, 42607, 42957]
    suspects_detail = []
    for idx in suspect_indices:
        v = mesh.data.vertices[idx]
        w = {mesh.vertex_groups[g.group].name: round(g.weight, 4) for g in v.groups}
        suspects_detail.append({
            "index": idx,
            "co": [round(c, 4) for c in v.co],
            "weights": w
        })
    info["suspects"] = suspects_detail
    
finally:
    bpy.context.window.scene = original_scene
    for o in list(scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(scene)

out_file = ROOT / "temp/inspect_morgana_shoulders.json"
out_file.write_text(json.dumps(info, indent=2), encoding="utf-8")
print(json.dumps(info, indent=2))
