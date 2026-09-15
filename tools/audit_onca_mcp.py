"""Audit Onca model structure, hierarchy, meshes, materials and animations in Blender without altering user scene."""
import bpy
import json
from pathlib import Path

ROOT = Path(r"c:\workspace\multigame")
glb_path = ROOT / "assets/models/club/onca.glb"

original_scene = bpy.context.window.scene
audit = {}

try:
    scene = bpy.data.scenes.new("AuditOnca")
    bpy.context.window.scene = scene
    
    bpy.ops.import_scene.gltf(filepath=str(glb_path))
    
    objects = list(scene.objects)
    audit["object_count"] = len(objects)
    audit["objects"] = []
    
    meshes = [o for o in objects if o.type == 'MESH']
    armatures = [o for o in objects if o.type == 'ARMATURE']
    empties = [o for o in objects if o.type == 'EMPTY']
    
    audit["mesh_count"] = len(meshes)
    audit["armature_count"] = len(armatures)
    audit["empty_count"] = len(empties)
    
    total_verts = sum(len(m.data.vertices) for m in meshes)
    total_faces = sum(len(m.data.polygons) for m in meshes)
    audit["total_vertices"] = total_verts
    audit["total_faces"] = total_faces
    
    for o in objects:
        parent_name = o.parent.name if o.parent else None
        obj_info = {
            "name": o.name,
            "type": o.type,
            "parent": parent_name,
            "location": [round(v, 4) for v in o.location],
        }
        if o.type == 'MESH':
            obj_info["verts"] = len(o.data.vertices)
            obj_info["faces"] = len(o.data.polygons)
            obj_info["materials"] = [m.name for m in o.data.materials if m]
            obj_info["vertex_groups"] = [vg.name for vg in o.vertex_groups]
        if o.animation_data:
            tracks = [t.name for t in o.animation_data.nla_tracks]
            obj_info["nla_tracks"] = tracks
            if o.animation_data.action:
                obj_info["active_action"] = o.animation_data.action.name
        audit["objects"].append(obj_info)
        
    all_actions = list(bpy.data.actions)
    audit["actions"] = [{"name": a.name, "frame_range": [round(f, 2) for f in a.frame_range]} for a in all_actions if any(o.name in a.name or 'onca' in a.name.lower() or a.name in ('idle','entrance','truco','victory','boss_intro','flourish','play_card') for o in objects)]

finally:
    bpy.context.window.scene = original_scene
    for o in list(scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(scene)

out_file = ROOT / "temp/audit_onca.json"
out_file.parent.mkdir(parents=True, exist_ok=True)
out_file.write_text(json.dumps(audit, indent=2), encoding="utf-8")
print(f"AUDIT COMPLETE: {total_verts} verts, {total_faces} faces, {len(armatures)} armatures, {len(empties)} empties")
