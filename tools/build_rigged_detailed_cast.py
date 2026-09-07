"""Blender 5.2: Rigged and Animated High-Fidelity Character Cast Generator.
Builds skeletal armatures, applies automatic weight skinning, animates 7 fluid Bezier
action clips per character, and exports to assets/models/club/{ident}.glb and portraits.
"""
import bpy
import os
import math
from pathlib import Path
import mathutils
from mathutils import Vector, Euler

ROOT = Path(r"c:\workspace\multigame")
SRC_DIR = ROOT / "assets/modelos 3d detalhados"
OUT_DIR = ROOT / "assets/models/club"
OUT_DIR.mkdir(parents=True, exist_ok=True)

# Character definitions: ident, blend_filename, skin_hex, extra_type
DETAILED_CAST = [
    ("corvo", "Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend", "#18202c", "wings"),
    ("barao", "Meshy_AI_The_Dapper_Owl_0907034231_texture.blend",    "#3f324c", "wings"),
    ("dama",  "Meshy_AI_Cobra_Queen_0907035025_texture.blend",       "#cca038", "snake"),
    ("zeca",  "Meshy_AI_Dapper_Fox_Detective_0907034820_texture.blend", "#c25524", "tail"),
    ("iara",  "Meshy_AI_Capybara_Lady_0907035257_texture.blend",     "#966d48", "biped"),
]

def hex_to_rgb(hex_str):
    h = hex_str.lstrip("#")
    return tuple(int(h[i:i+2], 16) / 255.0 for i in (0, 2, 4)) + (1.0,)

def smooth_fcurves(action):
    if not action:
        return
    fc_list = []
    if hasattr(action, 'fcurves'):
        fc_list.extend(action.fcurves)
    if hasattr(action, 'layers'):
        for layer in action.layers:
            for strip in getattr(layer, 'strips', []):
                for cb in getattr(strip, 'channelbags', []):
                    fc_list.extend(getattr(cb, 'fcurves', []))
    for fc in fc_list:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = 'AUTO_CLAMPED'
            kp.handle_right_type = 'AUTO_CLAMPED'

def build_character(ident, blend_name, skin_hex, char_type):
    blend_path = SRC_DIR / blend_name
    if not blend_path.exists():
        print(f"[ERROR] Source file not found: {blend_path}")
        return

    bpy.ops.wm.open_mainfile(filepath=str(blend_path))

    # Clean cameras and lights
    for o in list(bpy.data.objects):
        if o.type in ('CAMERA', 'LIGHT'):
            bpy.data.objects.remove(o, do_unlink=True)

    # Locate mesh object
    mesh_obj = None
    for o in bpy.data.objects:
        if o.type == 'MESH':
            mesh_obj = o
            break

    if not mesh_obj:
        print(f"[ERROR] No mesh found in {blend_name}")
        return

    print(f"[{ident.upper()}] Processing mesh '{mesh_obj.name}' with {len(mesh_obj.data.vertices)} vertices...")

    # Ground mesh so lowest point touches Z=0
    bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
    min_z = min(b.z for b in bbox)
    mesh_obj.location.z -= min_z
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

    # Re-evaluate dimensions
    bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
    height = max(b.z for b in bbox)
    scale_factor = 1.85 / max(0.1, height)
    mesh_obj.scale = (scale_factor, scale_factor, scale_factor)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Setup Material name and palette
    if mesh_obj.data.materials:
        m = mesh_obj.data.materials[0]
        m.name = f"{ident}_skin"
        rgba = hex_to_rgb(skin_hex)
        m.diffuse_color = rgba
        if m.node_tree:
            bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
            if bsdf and 'Base Color' in bsdf.inputs and not bsdf.inputs['Base Color'].is_linked:
                bsdf.inputs['Base Color'].default_value = rgba

    # Create Armature
    arm_data = bpy.data.armatures.new(f"{ident}_ArmatureData")
    arm_obj = bpy.data.objects.new("Armature", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')

    eb = arm_data.edit_bones

    def add_bone(name, head, tail, parent=None):
        b = eb.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        if parent:
            b.parent = parent
            b.use_connect = False
        return b

    # Core Spine Chain
    root = add_bone("Root", (0, 0, 0), (0, 0, 0.15))
    pelvis = add_bone("Pelvis", (0, 0, 0.88), (0, 0, 1.02), root)
    spine = add_bone("Spine", (0, 0, 1.02), (0, 0, 1.22), pelvis)
    chest = add_bone("Chest", (0, 0, 1.22), (0, 0, 1.42), spine)
    neck = add_bone("Neck", (0, 0, 1.42), (0, -0.02, 1.54), chest)
    head = add_bone("Head", (0, -0.02, 1.54), (0, -0.05, 1.82), neck)

    # Shoulders & Arms
    sh_l = add_bone("Shoulder.L", (0.05, 0, 1.40), (0.22, 0.01, 1.38), chest)
    ua_l = add_bone("UpperArm.L", (0.22, 0.01, 1.38), (0.33, 0.04, 1.08), sh_l)
    fa_l = add_bone("Forearm.L", (0.33, 0.04, 1.08), (0.26, -0.15, 0.84), ua_l)
    h_l = add_bone("Hand.L", (0.26, -0.15, 0.84), (0.18, -0.26, 0.80), fa_l)

    sh_r = add_bone("Shoulder.R", (-0.05, 0, 1.40), (-0.22, 0.01, 1.38), chest)
    ua_r = add_bone("UpperArm.R", (-0.22, 0.01, 1.38), (-0.33, 0.04, 1.08), sh_r)
    fa_r = add_bone("Forearm.R", (-0.33, 0.04, 1.08), (-0.26, -0.15, 0.84), ua_r)
    h_r = add_bone("Hand.R", (-0.26, -0.15, 0.84), (-0.18, -0.26, 0.80), fa_r)

    # Lower Body
    if char_type == "snake":
        # Serpent coil spine
        t1 = add_bone("Tail.01", (0, 0, 0.70), (0, 0.05, 0.50), pelvis)
        t2 = add_bone("Tail.02", (0, 0.05, 0.50), (0, 0.10, 0.32), t1)
        t3 = add_bone("Tail.03", (0, 0.10, 0.32), (0.12, 0.15, 0.16), t2)
        t4 = add_bone("Tail.04", (0.12, 0.15, 0.16), (0.25, 0.0, 0.06), t3)
        t5 = add_bone("Tail.05", (0.25, 0.0, 0.06), (-0.15, -0.15, 0.02), t4)
    else:
        # Biped legs
        th_l = add_bone("Thigh.L", (0.15, 0, 0.88), (0.16, 0.02, 0.48), pelvis)
        shn_l = add_bone("Shin.L", (0.16, 0.02, 0.48), (0.16, 0.01, 0.12), th_l)
        ft_l = add_bone("Foot.L", (0.16, 0.01, 0.12), (0.16, -0.18, 0.02), shn_l)

        th_r = add_bone("Thigh.R", (-0.15, 0, 0.88), (-0.16, 0.02, 0.48), pelvis)
        shn_r = add_bone("Shin.R", (-0.16, 0.02, 0.48), (-0.16, 0.01, 0.12), th_r)
        ft_r = add_bone("Foot.R", (-0.16, 0.01, 0.12), (-0.16, -0.18, 0.02), shn_r)

    if char_type == "wings":
        # Wings attached to chest/back
        w_l = add_bone("Wing.L", (0.12, 0.12, 1.35), (0.42, 0.32, 1.25), chest)
        w_r = add_bone("Wing.R", (-0.12, 0.12, 1.35), (-0.42, 0.32, 1.25), chest)

    if char_type == "tail":
        # Bushy fox tail
        tail1 = add_bone("Tail.01", (0, 0.16, 0.80), (0, 0.38, 0.60), pelvis)
        tail2 = add_bone("Tail.02", (0, 0.38, 0.60), (0, 0.48, 0.30), tail1)

    bpy.ops.object.mode_set(mode='OBJECT')

    # Add Head Node3D empty marker for Godot camera clipping & GameplayChecks
    head_marker = bpy.data.objects.new("Head", None)
    head_marker.location = (0, -0.05, 1.58)
    head_marker.parent = arm_obj
    bpy.context.collection.objects.link(head_marker)

    # Parent mesh with automatic weights
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print(f"[{ident.upper()}] Parented with ARMATURE_AUTO.")

    # Animation configuration
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')

    for pb in arm_obj.pose.bones:
        pb.rotation_mode = 'XYZ'

    arm_obj.animation_data_create()

    # Helper function to keyframe a bone with Bezier curves
    def key_rot(bone_name, action, keys):
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb:
            return
        for frame, rot_tuple in keys:
            pb.rotation_euler = rot_tuple
            pb.keyframe_insert(data_path="rotation_euler", frame=frame)

    def key_loc(bone_name, action, keys):
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb:
            return
        for frame, loc_tuple in keys:
            pb.location = loc_tuple
            pb.keyframe_insert(data_path="location", frame=frame)

    # -------------------------------------------------------------
    # 1. IDLE CLIP (64 frames, smooth looping breathing & alertness)
    # -------------------------------------------------------------
    act_idle = bpy.data.actions.new(name="idle")
    arm_obj.animation_data.action = act_idle

    # Pelvis gentle vertical breathing bounce
    key_loc("Pelvis", act_idle, [
        (1,  (0, 0, 0)),
        (32, (0, 0, 0.035)),
        (64, (0, 0, 0))
    ])
    # Spine breathing arch
    key_rot("Spine", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.08, 0, 0)),
        (64, (0, 0, 0))
    ])
    # Chest full respiration swell
    key_loc("Chest", act_idle, [
        (1,  (0, 0, 0)),
        (32, (0, 0.025, 0.05)),
        (64, (0, 0, 0))
    ])
    key_rot("Chest", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.14, 0, 0)),
        (64, (0, 0, 0))
    ])
    # Neck micro-movement
    key_rot("Neck", act_idle, [
        (1,  (0, 0, 0)),
        (20, (0.04, 0.04, 0)),
        (44, (-0.04, -0.04, 0)),
        (64, (0, 0, 0))
    ])
    # Head alert living gaze (subtle nod, glances left/right)
    key_rot("Head", act_idle, [
        (1,  (0, 0, 0)),
        (18, (-0.08, 0.12, 0.06)),
        (36, (0.10, 0, 0)),
        (50, (-0.06, -0.12, -0.06)),
        (64, (0, 0, 0))
    ])
    # Shoulders gentle breathing lift
    key_rot("Shoulder.L", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.05, 0.08, 0.06)),
        (64, (0, 0, 0))
    ])
    key_rot("Shoulder.R", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.05, -0.08, -0.06)),
        (64, (0, 0, 0))
    ])
    # Arms holding cards with natural organic breathing motion
    key_rot("UpperArm.L", act_idle, [
        (1,  (0.10, 0.06, 0.04)),
        (32, (-0.12, 0.02, 0.01)),
        (64, (0.10, 0.06, 0.04))
    ])
    key_rot("UpperArm.R", act_idle, [
        (1,  (0.10, -0.06, -0.04)),
        (32, (-0.12, -0.02, -0.01)),
        (64, (0.10, -0.06, -0.04))
    ])
    key_rot("Forearm.L", act_idle, [
        (1,  (-0.14, 0.05, 0)),
        (32, (0.10, 0.02, 0)),
        (64, (-0.14, 0.05, 0))
    ])
    key_rot("Forearm.R", act_idle, [
        (1,  (-0.14, -0.05, 0)),
        (32, (0.10, -0.02, 0)),
        (64, (-0.14, -0.05, 0))
    ])
    key_rot("Hand.L", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.12, 0.06, 0)),
        (64, (0, 0, 0))
    ])
    key_rot("Hand.R", act_idle, [
        (1,  (0, 0, 0)),
        (32, (-0.12, -0.06, 0)),
        (64, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_idle, [
            (1,  (0, 0, 0)),
            (32, (-0.18, 0.32, 0.20)),
            (64, (0, 0, 0))
        ])
        key_rot("Wing.R", act_idle, [
            (1,  (0, 0, 0)),
            (32, (-0.18, -0.32, -0.20)),
            (64, (0, 0, 0))
        ])
    if char_type == "tail":
        key_rot("Tail.01", act_idle, [
            (1,  (0, 0, 0)),
            (18, (0.10, 0.38, 0.15)),
            (48, (-0.08, -0.38, -0.15)),
            (64, (0, 0, 0))
        ])
        key_rot("Tail.02", act_idle, [
            (1,  (0, 0, 0)),
            (24, (0.15, 0.55, 0.22)),
            (54, (-0.12, -0.55, -0.22)),
            (64, (0, 0, 0))
        ])
    if char_type == "snake":
        key_rot("Tail.01", act_idle, [(1, (0, 0, 0)), (20, (0.06, 0.25, 0)), (44, (-0.06, -0.25, 0)), (64, (0, 0, 0))])
        key_rot("Tail.02", act_idle, [(1, (0, 0, 0)), (28, (-0.08, -0.30, 0)), (52, (0.08, 0.30, 0)), (64, (0, 0, 0))])
        key_rot("Tail.03", act_idle, [(1, (0, 0, 0)), (36, (0.08, 0.35, 0)), (58, (-0.08, -0.35, 0)), (64, (0, 0, 0))])
        key_rot("Tail.04", act_idle, [(1, (0, 0, 0)), (24, (-0.10, -0.38, 0)), (48, (0.10, 0.38, 0)), (64, (0, 0, 0))])
        key_rot("Tail.05", act_idle, [(1, (0, 0, 0)), (32, (0.12, 0.42, 0)), (56, (-0.12, -0.42, 0)), (64, (0, 0, 0))])

    smooth_fcurves(act_idle)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "idle"
    strip = track.strips.new("idle", 1, act_idle)
    strip.extrapolation = 'HOLD'

    # -------------------------------------------------------------
    # 2. ENTRANCE CLIP (48 frames, refined courtly arrival and bow)
    # -------------------------------------------------------------
    act_ent = bpy.data.actions.new(name="entrance")
    arm_obj.animation_data.action = act_ent

    key_loc("Pelvis", act_ent, [
        (1,  (0, 0.38, 0.14)),
        (24, (0, -0.10, -0.06)),
        (48, (0, 0, 0))
    ])
    key_rot("Spine", act_ent, [
        (1,  (0.25, 0, 0)),
        (20, (-0.52, 0, 0)),
        (36, (-0.20, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("Head", act_ent, [
        (1,  (-0.18, 0, 0)),
        (20, (0.42, 0, 0)),
        (36, (0.14, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.R", act_ent, [
        (1,  (0, 0, 0)),
        (20, (-0.95, -0.40, -0.45)),
        (36, (-0.45, -0.15, -0.15)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_ent, [
        (1,  (0, 0, 0)),
        (20, (-0.65, 0.25, 0.15)),
        (36, (-0.30, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.L", act_ent, [
        (1,  (0, 0, 0)),
        (20, (-0.35, 0.25, 0.20)),
        (48, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_ent, [(1, (0, 0, 0)), (20, (-0.25, 0.55, 0.30)), (48, (0, 0, 0))])
        key_rot("Wing.R", act_ent, [(1, (0, 0, 0)), (20, (-0.25, -0.55, -0.30)), (48, (0, 0, 0))])
    if char_type == "tail":
        key_rot("Tail.01", act_ent, [(1, (0, 0, 0)), (20, (0.15, 0.40, 0)), (48, (0, 0, 0))])

    smooth_fcurves(act_ent)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "entrance"
    strip = track.strips.new("entrance", 1, act_ent)

    # -------------------------------------------------------------
    # 3. TRUCO CLIP (48 frames, explosive high-stakes table challenge)
    # -------------------------------------------------------------
    act_truco = bpy.data.actions.new(name="truco")
    arm_obj.animation_data.action = act_truco

    key_loc("Chest", act_truco, [
        (1,  (0, 0, 0)),
        (10, (0, 0.18, 0.12)),
        (22, (0, -0.38, -0.06)),
        (36, (0, -0.24, -0.03)),
        (48, (0, 0, 0))
    ])
    key_rot("Chest", act_truco, [
        (1,  (0, 0, 0)),
        (10, (0.24, 0, 0)),
        (22, (-0.58, 0, 0)),
        (36, (-0.38, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("Head", act_truco, [
        (1,  (0, 0, 0)),
        (10, (-0.22, 0, 0)),
        (22, (0.48, 0, 0)),
        (36, (0.28, 0, 0)),
        (48, (0, 0, 0))
    ])
    # Dominant arm challenge slam
    key_rot("UpperArm.R", act_truco, [
        (1,  (0, 0, 0)),
        (10, (0.75, 0.25, 0.35)),
        (22, (-1.45, -0.25, -0.35)),
        (36, (-1.10, -0.18, -0.25)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_truco, [
        (1,  (0, 0, 0)),
        (10, (-0.85, 0, 0)),
        (22, (-0.65, 0, -0.30)),
        (36, (-0.45, 0, -0.20)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.L", act_truco, [
        (1,  (0, 0, 0)),
        (22, (-0.45, 0.25, 0.25)),
        (48, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_truco, [(1, (0, 0, 0)), (22, (-0.35, 0.85, 0.45)), (48, (0, 0, 0))])
        key_rot("Wing.R", act_truco, [(1, (0, 0, 0)), (22, (-0.35, -0.85, -0.45)), (48, (0, 0, 0))])
    if char_type == "tail":
        key_rot("Tail.01", act_truco, [(1, (0, 0, 0)), (22, (0.25, 0.50, 0)), (48, (0, 0, 0))])

    smooth_fcurves(act_truco)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "truco"
    strip = track.strips.new("truco", 1, act_truco)

    # -------------------------------------------------------------
    # 4. VICTORY CLIP (48 frames, triumphant celebration & arms raised)
    # -------------------------------------------------------------
    act_vic = bpy.data.actions.new(name="victory")
    arm_obj.animation_data.action = act_vic

    key_loc("Chest", act_vic, [
        (1,  (0, 0, 0)),
        (14, (0, 0.08, 0.12)),
        (28, (0, -0.04, 0.15)),
        (40, (0, 0, 0.08)),
        (48, (0, 0, 0))
    ])
    key_rot("Chest", act_vic, [
        (1,  (0, 0, 0)),
        (14, (0.28, 0, 0)),
        (28, (0.38, 0.08, 0)),
        (40, (0.22, 0.04, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("Head", act_vic, [
        (1,  (0, 0, 0)),
        (14, (-0.28, 0, 0)),
        (28, (-0.42, 0.06, 0)),
        (40, (-0.22, 0.02, 0)),
        (48, (0, 0, 0))
    ])
    # Both arms raised in triumph
    key_rot("UpperArm.L", act_vic, [
        (1,  (0, 0, 0)),
        (22, (1.75, 0.45, 0.55)),
        (34, (1.85, 0.35, 0.45)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.R", act_vic, [
        (1,  (0, 0, 0)),
        (22, (1.75, -0.45, -0.55)),
        (34, (1.85, -0.35, -0.45)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.L", act_vic, [
        (1,  (0, 0, 0)),
        (22, (-0.85, 0, 0)),
        (34, (-0.95, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_vic, [
        (1,  (0, 0, 0)),
        (22, (-0.85, 0, 0)),
        (34, (-0.95, 0, 0)),
        (48, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_vic, [(1, (0, 0, 0)), (24, (0.20, 0.95, 0.60)), (48, (0, 0, 0))])
        key_rot("Wing.R", act_vic, [(1, (0, 0, 0)), (24, (0.20, -0.95, -0.60)), (48, (0, 0, 0))])
    if char_type == "tail":
        key_rot("Tail.01", act_vic, [(1, (0, 0, 0)), (16, (0.20, 0.45, 0)), (32, (0.15, -0.45, 0)), (48, (0, 0, 0))])

    smooth_fcurves(act_vic)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "victory"
    strip = track.strips.new("victory", 1, act_vic)

    # -------------------------------------------------------------
    # 5. BOSS_INTRO CLIP (56 frames, imposing imperial dominance)
    # -------------------------------------------------------------
    act_boss = bpy.data.actions.new(name="boss_intro")
    arm_obj.animation_data.action = act_boss

    key_loc("Chest", act_boss, [
        (1,  (0, 0, 0)),
        (18, (0, 0.05, 0.08)),
        (36, (0, -0.05, 0.10)),
        (56, (0, 0, 0))
    ])
    key_rot("Chest", act_boss, [
        (1,  (0, 0, 0)),
        (18, (-0.18, 0.12, 0)),
        (36, (-0.18, -0.12, 0)),
        (56, (0, 0, 0))
    ])
    key_rot("Head", act_boss, [
        (1,  (0, 0, 0)),
        (18, (0.15, 0.32, 0.10)),
        (36, (0.15, -0.32, -0.10)),
        (56, (0, 0, 0))
    ])
    # Arms folded across chest
    key_rot("UpperArm.L", act_boss, [
        (1,  (0, 0, 0)),
        (24, (-0.95, 0.45, 0.55)),
        (56, (0, 0, 0))
    ])
    key_rot("Forearm.L", act_boss, [
        (1,  (0, 0, 0)),
        (24, (-1.55, -0.35, 0.15)),
        (56, (0, 0, 0))
    ])
    key_rot("UpperArm.R", act_boss, [
        (1,  (0, 0, 0)),
        (24, (-1.05, -0.40, -0.50)),
        (56, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_boss, [
        (1,  (0, 0, 0)),
        (24, (-1.45, 0.40, -0.15)),
        (56, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_boss, [(1, (0, 0, 0)), (28, (-0.25, 0.85, 0.45)), (56, (0, 0, 0))])
        key_rot("Wing.R", act_boss, [(1, (0, 0, 0)), (28, (-0.25, -0.85, -0.45)), (56, (0, 0, 0))])
    if char_type == "snake":
        key_rot("Tail.01", act_boss, [(1, (0, 0, 0)), (24, (0.12, 0.25, 0)), (56, (0, 0, 0))])
        key_rot("Tail.02", act_boss, [(1, (0, 0, 0)), (24, (-0.10, -0.28, 0)), (56, (0, 0, 0))])

    smooth_fcurves(act_boss)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "boss_intro"
    strip = track.strips.new("boss_intro", 1, act_boss)

    # -------------------------------------------------------------
    # 6. FLOURISH CLIP (48 frames, aristocratic card mastery & display)
    # -------------------------------------------------------------
    act_flourish = bpy.data.actions.new(name="flourish")
    arm_obj.animation_data.action = act_flourish

    key_rot("Chest", act_flourish, [
        (1,  (0, 0, 0)),
        (16, (-0.28, 0.28, 0.12)),
        (32, (0.15, -0.25, -0.08)),
        (48, (0, 0, 0))
    ])
    key_rot("Head", act_flourish, [
        (1,  (0, 0, 0)),
        (16, (0.28, -0.22, -0.08)),
        (32, (-0.12, 0.22, 0.06)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.R", act_flourish, [
        (1,  (0, 0, 0)),
        (16, (-0.75, -0.35, -0.35)),
        (30, (-1.05, -0.45, -0.45)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_flourish, [
        (1,  (0, 0, 0)),
        (16, (-0.85, 0.25, 0.25)),
        (30, (-0.45, -0.20, -0.15)),
        (48, (0, 0, 0))
    ])
    key_rot("Hand.R", act_flourish, [
        (1,  (0, 0, 0)),
        (16, (0.45, 0.25, 0.20)),
        (30, (-0.55, -0.30, -0.25)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.L", act_flourish, [
        (1,  (0, 0, 0)),
        (22, (-0.55, 0.35, 0.25)),
        (48, (0, 0, 0))
    ])
    smooth_fcurves(act_flourish)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "flourish"
    strip = track.strips.new("flourish", 1, act_flourish)

    # -------------------------------------------------------------
    # 7. PLAY_CARD CLIP (48 frames, reach forward & tactile snap onto felt)
    # -------------------------------------------------------------
    act_play = bpy.data.actions.new(name="play_card")
    arm_obj.animation_data.action = act_play

    key_loc("Chest", act_play, [
        (1,  (0, 0, 0)),
        (12, (0, 0.08, 0.05)),
        (24, (0, -0.28, -0.06)),
        (36, (0, -0.15, -0.03)),
        (48, (0, 0, 0))
    ])
    key_rot("Chest", act_play, [
        (1,  (0, 0, 0)),
        (12, (0.16, -0.08, 0)),
        (24, (-0.38, 0.12, 0)),
        (36, (-0.18, 0.05, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("Head", act_play, [
        (1,  (0, 0, 0)),
        (12, (-0.16, 0, 0)),
        (24, (0.38, -0.06, 0)),
        (36, (0.18, -0.03, 0)),
        (48, (0, 0, 0))
    ])
    # Active playing arm (Right arm)
    key_rot("UpperArm.R", act_play, [
        (1,  (0, 0, 0)),
        (12, (0.55, -0.28, 0.35)),
        (24, (-1.35, -0.35, -0.42)),
        (36, (-0.85, -0.20, -0.22)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.R", act_play, [
        (1,  (0, 0, 0)),
        (12, (-1.20, 0.20, -0.15)),
        (24, (-0.28, -0.12, -0.22)),
        (36, (-0.50, 0, -0.10)),
        (48, (0, 0, 0))
    ])
    key_rot("Hand.R", act_play, [
        (1,  (0, 0, 0)),
        (12, (-0.35, 0.18, 0.10)),
        (24, (0.55, -0.22, 0.18)), # Snap down onto felt
        (36, (0.18, 0, 0)),
        (48, (0, 0, 0))
    ])
    key_rot("UpperArm.L", act_play, [
        (1,  (0, 0, 0)),
        (24, (-0.25, 0.15, 0.10)),
        (48, (0, 0, 0))
    ])
    key_rot("Forearm.L", act_play, [
        (1,  (0, 0, 0)),
        (24, (-0.30, 0, 0)),
        (48, (0, 0, 0))
    ])
    if char_type == "wings":
        key_rot("Wing.L", act_play, [(1, (0, 0, 0)), (24, (-0.20, 0.35, 0.15)), (48, (0, 0, 0))])
        key_rot("Wing.R", act_play, [(1, (0, 0, 0)), (24, (-0.20, -0.35, -0.15)), (48, (0, 0, 0))])
    if char_type == "tail":
        key_rot("Tail.01", act_play, [(1, (0, 0, 0)), (24, (0.15, 0.35, 0)), (48, (0, 0, 0))])

    smooth_fcurves(act_play)
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = "play_card"
    strip = track.strips.new("play_card", 1, act_play)

    # Return to resting frame 1
    arm_obj.animation_data.action = None
    bpy.ops.object.mode_set(mode='OBJECT')

    # Export final GLB
    out_glb = OUT_DIR / f"{ident}.glb"
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(
        filepath=str(out_glb),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,
        export_materials='EXPORT'
    )
    print(f"[{ident.upper()}] Successfully exported GLB: {out_glb} ({out_glb.stat().st_size} bytes)")

    # Render High-Definition 3D Studio Portrait (320x400 PNG)
    bpy.ops.object.camera_add(location=(1.5, -3.2, 1.45))
    cam = bpy.context.object
    cam.name = "PortraitCam"
    target = Vector((0, 0, 1.25))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = 65
    bpy.context.scene.camera = cam

    # 3-Point Studio Lights
    for l_name, l_pos, l_energy in [
        ("Key", (-1.8, -2.5, 2.6), 550),
        ("Fill", (2.2, -1.8, 2.0), 300),
        ("Rim", (0, 2.5, 2.2), 450)
    ]:
        bpy.ops.object.light_add(type='AREA', location=l_pos)
        lt = bpy.context.object
        lt.name = l_name
        lt.data.energy = l_energy
        lt.data.size = 2.0
        lt.rotation_euler = (target - lt.location).to_track_quat('-Z', 'Y').to_euler()

    bpy.context.scene.render.engine = 'BLENDER_EEVEE'
    bpy.context.scene.render.resolution_x = 320
    bpy.context.scene.render.resolution_y = 400
    bpy.context.scene.render.film_transparent = True
    out_portrait = OUT_DIR / f"{ident}_3d.png"
    bpy.context.scene.render.filepath = str(out_portrait)
    bpy.ops.render.render(write_still=True)
    print(f"[{ident.upper()}] Rendered studio portrait: {out_portrait}")

def main():
    print("=== STARTING RIGGED CAST GENERATION ===")
    for row in DETAILED_CAST:
        build_character(*row)
    print("=== ALL 5 CHARACTERS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
