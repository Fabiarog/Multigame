"""Repair Morgana shoulder vertex weights in Blender.
Replaces inappropriate Forearm weights on the upper shoulder (z > 1.18, abs(x) < 0.25)
with smooth blends of Shoulder, UpperArm, and Chest.
"""
import bpy
import json
from pathlib import Path
from mathutils.kdtree import KDTree
from mathutils import Vector

ROOT = Path(r"c:\workspace\multigame")
glb_path = ROOT / "assets/models/club/morgana.glb"
blend_path = ROOT / "art/blender/patch26/morgana_refined.blend"
staging_glb = ROOT / "art/blender/patch26/morgana_refined.glb"

original_scene = bpy.context.window.scene
changed = 0

try:
    scene = bpy.data.scenes.new("FixMorgana")
    bpy.context.window.scene = scene
    bpy.ops.import_scene.gltf(filepath=str(staging_glb if staging_glb.exists() else glb_path))
    
    mesh_obj = next(o for o in scene.objects if o.type == 'MESH')
    armature = next(o for o in scene.objects if o.type == 'ARMATURE')
    
    # Ensure vertex groups exist
    for vg_name in ['Shoulder.L', 'Shoulder.R', 'UpperArm.L', 'UpperArm.R', 'Chest', 'Spine']:
        if vg_name not in mesh_obj.vertex_groups:
            mesh_obj.vertex_groups.new(name=vg_name)
            
    v_groups = {vg.name: vg for vg in mesh_obj.vertex_groups}
    
    # Identify shoulder transition vertices
    # Region: abs(x) between 0.11 and 0.24, z between 1.18 and 1.34, y between -0.10 and 0.12
    for v in mesh_obj.data.vertices:
        co = v.co
        abs_x = abs(co.x)
        if 0.11 <= abs_x <= 0.25 and 1.18 <= co.z <= 1.35 and -0.12 <= co.y <= 0.12:
            side = 'L' if co.x > 0 else 'R'
            forearm_name = f'Forearm.{side}'
            upper_name = f'UpperArm.{side}'
            shoulder_name = f'Shoulder.{side}'
            
            # Check current weights
            weights = {mesh_obj.vertex_groups[g.group].name: g.weight for g in v.groups}
            has_forearm = weights.get(forearm_name, 0.0)
            has_chest = weights.get('Chest', 0.0)
            has_spine = weights.get('Spine', 0.0)
            
            # If vertex has forearm weight in the upper shoulder, that's completely anatomically wrong
            if has_forearm > 0.05 or (has_chest > 0.8 and abs_x > 0.13):
                # Calculate factor based on distance from chest center to arm
                # abs_x = 0.12 -> closer to chest; abs_x = 0.24 -> closer to upper arm
                t = (abs_x - 0.11) / (0.25 - 0.11) # 0.0 at inner seam, 1.0 at outer shoulder
                t = max(0.0, min(1.0, t))
                
                # Smooth blending:
                # Inner: mostly Chest / Shoulder
                # Outer: mostly UpperArm / Shoulder
                w_chest = (1.0 - t) * 0.45
                w_shoulder = 0.40 + 0.15 * (1.0 - abs(t - 0.5) * 2) # peak in middle
                w_upper = t * 0.55
                
                # Normalize
                total = w_chest + w_shoulder + w_upper
                w_chest /= total
                w_shoulder /= total
                w_upper /= total
                
                # Clear forearm and spine from this shoulder vertex
                if forearm_name in v_groups:
                    v_groups[forearm_name].remove([v.index])
                if 'Spine' in v_groups and abs_x > 0.13:
                    v_groups['Spine'].remove([v.index])
                    
                v_groups['Chest'].add([v.index], w_chest, 'REPLACE')
                v_groups[shoulder_name].add([v.index], w_shoulder, 'REPLACE')
                v_groups[upper_name].add([v.index], w_upper, 'REPLACE')
                changed += 1
                
    print(f"Fixed {changed} vertices on Morgana's shoulders.")
    
    # Export back to staging and assets
    scene.frame_set(0)
    bpy.ops.object.select_all(action='DESELECT')
    for o in scene.objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = armature
    
    bpy.ops.export_scene.gltf(
        filepath=str(staging_glb),
        export_format='GLB',
        use_selection=True,
        use_active_scene=True,
        export_animations=True,
        export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,
        export_frame_range=False,
        export_def_bones=True
    )
    # Also write assets/models/club/morgana.glb
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format='GLB',
        use_selection=True,
        use_active_scene=True,
        export_animations=True,
        export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,
        export_frame_range=False,
        export_def_bones=True
    )
    
finally:
    bpy.context.window.scene = original_scene
    for o in list(scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(scene)

print("EXPORT COMPLETE")
