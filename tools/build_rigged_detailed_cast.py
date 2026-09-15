"""Blender 5.2: Rigged and Animated High-Fidelity Character Cast Generator.
Builds skeletal armatures with articulated ears (Zeca), wings (Corvo, Barão), and tail,
applies automatic weight skinning, animates 39 rich Bezier action clips per character,
and exports to assets/models/club/{ident}.glb and portraits.
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

    def add_bone(name, head, tail, parent=None, deform=True):
        b = eb.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        b.use_deform = deform
        if parent:
            b.parent = parent
            b.use_connect = False
        return b

    # Core Spine Chain
    root = add_bone("Root", (0, 0, 0), (0, 0, 0.15), deform=False)
    pelvis = add_bone("Pelvis", (0, 0, 0.88), (0, 0, 1.02), root, deform=True)
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

    # Card socket marker bone on dominant right hand (non-deforming)
    card_sock = add_bone("CardSocket.R", (-0.18, -0.26, 0.80), (-0.18, -0.32, 0.80), h_r, deform=False)

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
        # Articulated fox ears parented to Head
        ear_l = add_bone("Ear.L", (0.10, -0.04, 1.78), (0.16, -0.02, 2.00), head, deform=True)
        ear_r = add_bone("Ear.R", (-0.10, -0.04, 1.78), (-0.16, -0.02, 2.00), head, deform=True)

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

    def key_rot(bone_name, action, keys):
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb:
            return
        pb.rotation_mode = 'XYZ'
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

    def add_action(name, total_frames, curves):
        act = bpy.data.actions.new(name=name)
        arm_obj.animation_data.action = act

        c = dict(curves)
        # Postura sentada na cadeira: coxas para frente (-88°), canelas para baixo (+85°), pélvis no assento (-0.36m)
        if char_type != "snake":
            c["Thigh.L"] = (None, [(1, (-1.536, 0, 0)), (total_frames, (-1.536, 0, 0))])
            c["Thigh.R"] = (None, [(1, (-1.536, 0, 0)), (total_frames, (-1.536, 0, 0))])
            c["Shin.L"] = (None, [(1, (1.484, 0, 0)), (total_frames, (1.484, 0, 0))])
            c["Shin.R"] = (None, [(1, (1.484, 0, 0)), (total_frames, (1.484, 0, 0))])

        if "Pelvis" in c and c["Pelvis"][0]:
            p_locs = [(f, (l[0], l[1] - 0.36, l[2])) for f, l in c["Pelvis"][0]]
            c["Pelvis"] = (p_locs, c["Pelvis"][1])
        else:
            c["Pelvis"] = ([(1, (0, -0.36, 0)), (total_frames, (0, -0.36, 0))], None)

        for bone_name, (loc_keys, rot_keys) in c.items():
            if loc_keys:
                key_loc(bone_name, act, loc_keys)
            if rot_keys:
                key_rot(bone_name, act, rot_keys)
        smooth_fcurves(act)
        track = arm_obj.animation_data.nla_tracks.new()
        track.name = name
        strip = track.strips.new(name, 1, act)
        if name in ("idle", "idle_table_01", "idle_table_02"):
            strip.extrapolation = 'HOLD'
        return act

    # Secondary acting generators for Corvo (wings), Zeca (tail + ears), and Dama (snake coils)
    def sec_curves(clip_type, total_f):
        res = {}
        if char_type == "wings":
            if clip_type in ("big_win", "victory"):
                res["Wing.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.20, 0.95, 0.60)), (total_f, (0, 0, 0))])
                res["Wing.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.20, -0.95, -0.60)), (total_f, (0, 0, 0))])
            elif clip_type in ("truco", "all_in", "flourish"):
                res["Wing.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (-0.35, 0.85, 0.45)), (total_f, (0, 0, 0))])
                res["Wing.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (-0.35, -0.85, -0.45)), (total_f, (0, 0, 0))])
            elif clip_type in ("boss_intro", "entrance"):
                res["Wing.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.4), (-0.25, 0.60, 0.35)), (total_f, (0, 0, 0))])
                res["Wing.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.4), (-0.25, -0.60, -0.35)), (total_f, (0, 0, 0))])
            elif clip_type in ("bid_confident", "play_card_dramatic"):
                res["Wing.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (-0.15, 0.35, 0.15)), (total_f, (0, 0, 0))])
                res["Wing.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (-0.15, -0.35, -0.15)), (total_f, (0, 0, 0))])
            else:
                # Idle subtle breathing flutter
                res["Wing.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (-0.08, 0.12, 0.08)), (total_f, (0, 0, 0))])
                res["Wing.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (-0.08, -0.12, -0.08)), (total_f, (0, 0, 0))])

        if char_type == "tail":
            # Continuous expressive tail acting in every single clip
            if clip_type in ("big_win", "victory", "trick_win"):
                res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.3), (0.22, 0.50, 0.20)), (int(total_f*0.7), (0.18, -0.50, -0.20)), (total_f, (0, 0, 0))])
                res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (0.28, 0.65, 0.25)), (int(total_f*0.75), (0.24, -0.65, -0.25)), (total_f, (0, 0, 0))])
                res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.18, 0.12, 0.10)), (total_f, (0, 0, 0))])
                res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.18, -0.12, -0.10)), (total_f, (0, 0, 0))])
            elif clip_type in ("truco", "all_in", "taunt"):
                # Tail coils back in anticipation then snaps forward
                res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.25), (-0.18, 0.35, 0.12)), (int(total_f*0.55), (0.35, -0.55, -0.20)), (total_f, (0, 0, 0))])
                res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.28), (-0.22, 0.45, 0.15)), (int(total_f*0.6), (0.42, -0.70, -0.25)), (total_f, (0, 0, 0))])
                res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (0.25, 0.08, 0.15)), (total_f, (0, 0, 0))])
                res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (0.25, -0.08, -0.15)), (total_f, (0, 0, 0))])
            elif clip_type in ("lose", "trick_lose", "life_lost", "bad_beat", "decline_truco"):
                # Tail tucked low between legs in dejection
                res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.4), (-0.32, 0.08, 0)), (total_f, (0, 0, 0))])
                res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (-0.38, 0.10, 0)), (total_f, (0, 0, 0))])
                res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (-0.22, 0.18, -0.12)), (total_f, (0, 0, 0))])
                res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (-0.22, -0.18, 0.12)), (total_f, (0, 0, 0))])
            elif clip_type in ("think", "inspect_hand", "suspicious"):
                # Curious, inquisitive upward curve with subtle ear twitches
                res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.4), (0.16, 0.28, 0.15)), (int(total_f*0.75), (0.08, -0.22, -0.10)), (total_f, (0, 0, 0))])
                res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (0.24, 0.40, 0.22)), (int(total_f*0.8), (0.12, -0.32, -0.15)), (total_f, (0, 0, 0))])
                res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.3), (0.20, 0.14, 0.10)), (int(total_f*0.65), (-0.08, 0.05, 0)), (total_f, (0, 0, 0))])
                res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (-0.08, -0.10, -0.05)), (int(total_f*0.7), (0.22, -0.16, -0.12)), (total_f, (0, 0, 0))])
            else:
                # Default smooth living tail wag
                res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.3), (0.12, 0.35, 0.14)), (int(total_f*0.75), (-0.10, -0.35, -0.14)), (total_f, (0, 0, 0))])
                res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (0.16, 0.50, 0.20)), (int(total_f*0.8), (-0.12, -0.50, -0.20)), (total_f, (0, 0, 0))])
                res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.08, 0.05, 0.04)), (total_f, (0, 0, 0))])
                res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.08, -0.05, -0.04)), (total_f, (0, 0, 0))])

        if char_type == "snake":
            for i, bn in enumerate(["Tail.01", "Tail.02", "Tail.03", "Tail.04", "Tail.05"]):
                phase = i * 0.12
                res[bn] = (None, [
                    (1, (0, 0, 0)),
                    (int(total_f * (0.3 + phase*0.5)), (0.08, 0.30, 0)),
                    (int(total_f * (0.7 + phase*0.2)), (-0.08, -0.30, 0)),
                    (total_f, (0, 0, 0))
                ])
        return res

    # =========================================================================
    # 1. CORE PRESERVED & ENHANCED CLIPS (Contracts strictly preserved)
    # =========================================================================

    # --- IDLE (64 frames, loop) ---
    c_idle = {
        "Pelvis": ([(1, (0, 0, 0)), (32, (0, 0, 0.015)), (64, (0, 0, 0))], None),
        "Spine": (None, [(1, (0, 0, 0)), (32, (-0.05, 0, 0)), (64, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (32, (-0.08, 0, 0)), (64, (0, 0, 0))]),
        "Neck": (None, [(1, (0, 0, 0)), (20, (0.03, 0.03, 0)), (44, (-0.03, -0.03, 0)), (64, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.06, 0.08, 0.04)), (36, (0.08, 0, 0)), (50, (-0.05, -0.08, -0.04)), (64, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0.10, 0.06, 0.04)), (32, (-0.12, 0.02, 0.01)), (64, (0.10, 0.06, 0.04))]),
        "UpperArm.R": (None, [(1, (0.10, -0.06, -0.04)), (32, (-0.12, -0.02, -0.01)), (64, (0.10, -0.06, -0.04))]),
        "Forearm.L": (None, [(1, (-0.14, 0.05, 0)), (32, (0.10, 0.02, 0)), (64, (-0.14, 0.05, 0))]),
        "Forearm.R": (None, [(1, (-0.14, -0.05, 0)), (32, (0.10, -0.02, 0)), (64, (-0.14, -0.05, 0))]),
        "Hand.L": (None, [(1, (0, 0, 0)), (32, (-0.12, 0.06, 0)), (64, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (32, (-0.12, -0.06, 0)), (64, (0, 0, 0))]),
    }
    c_idle.update(sec_curves("idle", 64))
    add_action("idle", 64, c_idle)

    # --- ENTRANCE (48 frames) ---
    c_ent = {
        "Pelvis": ([(1, (0, 0.18, 0.05)), (20, (0, -0.04, -0.02)), (48, (0, 0, 0))], None),
        "Spine": (None, [(1, (0.15, 0, 0)), (20, (-0.32, 0, 0)), (36, (-0.12, 0, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0.12, 0, 0)), (20, (-0.26, 0, 0)), (36, (-0.10, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (-0.12, 0, 0)), (20, (0.30, 0, 0)), (36, (0.10, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.65, -0.25, -0.28)), (36, (-0.30, -0.10, -0.10)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (20, (-0.45, 0.15, 0.10)), (36, (-0.20, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (20, (-0.25, 0.15, 0.12)), (48, (0, 0, 0))]),
    }
    c_ent.update(sec_curves("entrance", 48))
    add_action("entrance", 48, c_ent)

    # --- TRUCO (48 frames) ---
    c_truco = {
        "Pelvis": ([(1, (0, 0, 0)), (10, (0, 0.04, 0.02)), (22, (0, -0.06, -0.03)), (36, (0, -0.03, -0.01)), (48, (0, 0, 0))], None),
        "Spine": (None, [(1, (0, 0, 0)), (10, (0.16, 0, 0)), (22, (-0.32, 0, 0)), (36, (-0.18, 0, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (10, (0.18, 0, 0)), (22, (-0.36, 0, 0)), (36, (-0.20, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (10, (-0.18, 0.08, 0)), (22, (0.35, 0, 0)), (36, (0.20, 0, 0)), (48, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (10, (0.12, -0.08, 0.10)), (22, (-0.22, 0.14, -0.12)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (10, (0.65, 0.18, 0.25)), (22, (-0.95, -0.18, -0.25)), (36, (-0.68, -0.12, -0.16)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (10, (-0.75, 0, 0)), (22, (-0.45, 0, -0.18)), (36, (-0.30, 0, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (10, (-0.25, 0, 0)), (22, (0.42, 0, 0)), (36, (0.18, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (22, (-0.35, 0.18, 0.16)), (48, (0, 0, 0))]),
    }
    c_truco.update(sec_curves("truco", 48))
    add_action("truco", 48, c_truco)

    # --- VICTORY (48 frames) ---
    c_vic = {
        "Pelvis": ([(1, (0, 0, 0)), (14, (0, 0.02, 0.03)), (28, (0, 0, 0.05)), (48, (0, 0, 0))], None),
        "Spine": (None, [(1, (0, 0, 0)), (28, (-0.14, 0.02, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (28, (-0.18, 0.02, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (28, (0.24, 0.04, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (28, (-1.85, 0.40, 1.35)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (28, (-1.85, -0.40, -1.35)), (48, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (28, (0.25, 0.08, 0)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (28, (0.25, -0.08, 0)), (48, (0, 0, 0))]),
        "Hand.L": (None, [(1, (0, 0, 0)), (28, (0.35, 0, 0.20)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (28, (0.35, 0, -0.20)), (48, (0, 0, 0))]),
    }
    c_vic.update(sec_curves("victory", 48))
    add_action("victory", 48, c_vic)

    # --- BOSS_INTRO (56 frames) ---
    c_boss = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.14, 0.08, 0)), (36, (-0.14, -0.08, 0)), (56, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.12, 0.24, 0.08)), (36, (0.12, -0.24, -0.08)), (56, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.50, 0.26, 0.22)), (56, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (24, (-1.10, 0.12, 0.20)), (56, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.54, -0.24, -0.20)), (56, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (24, (-1.10, -0.12, -0.20)), (56, (0, 0, 0))]),
    }
    c_boss.update(sec_curves("boss_intro", 56))
    add_action("boss_intro", 56, c_boss)

    # --- FLOURISH (48 frames) ---
    c_flourish = {
        "Chest": (None, [(1, (0, 0, 0)), (16, (-0.22, 0.20, 0.10)), (32, (0.12, -0.18, -0.06)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (0.22, -0.16, -0.06)), (32, (-0.10, 0.16, 0.05)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.65, -0.25, -0.25)), (30, (-0.85, -0.32, -0.32)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.65, 0.18, 0.18)), (30, (-0.35, -0.14, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (16, (0.35, 0.18, 0.15)), (30, (-0.40, -0.22, -0.18)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (22, (-0.40, 0.25, 0.18)), (48, (0, 0, 0))]),
    }
    c_flourish.update(sec_curves("flourish", 48))
    add_action("flourish", 48, c_flourish)

    # --- PLAY_CARD (48 frames, synchronized CardGrab @12, CardRelease @24) ---
    c_play = {
        "Pelvis": ([(1, (0, 0, 0)), (24, (0, -0.03, -0.015)), (48, (0, 0, 0))], None),
        "Spine": (None, [(1, (0, 0, 0)), (12, (0.08, 0, 0)), (24, (-0.20, 0.04, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (12, (0.10, -0.04, 0)), (24, (-0.24, 0.06, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (12, (-0.12, 0, 0)), (24, (0.28, -0.04, 0)), (48, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (12, (0.06, -0.05, 0.04)), (24, (-0.16, 0.10, -0.08)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (12, (0.35, -0.15, 0.20)), (24, (-0.85, -0.20, -0.24)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (12, (-0.65, 0.12, -0.08)), (24, (-0.42, -0.08, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (-0.20, 0.10, 0.05)), (24, (0.36, -0.12, 0.10)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.18, 0.10, 0.08)), (48, (0, 0, 0))]),
    }
    c_play.update(sec_curves("play_card", 48))
    add_action("play_card", 48, c_play)

    # =========================================================================
    # 2. TABLE IDLES & CONTEMPLATION
    # =========================================================================

    # --- IDLE_TABLE_01 (48 frames, relaxed attentive table rest) ---
    c_tab1 = {
        "Pelvis": ([(1, (0, 0, 0)), (24, (0, 0, 0.010)), (48, (0, 0, 0))], None),
        "Chest": (None, [(1, (0, 0, 0)), (24, (-0.05, 0.03, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (0.05, -0.08, 0)), (32, (-0.04, 0.08, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.08, 0.05, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.08, -0.05, 0)), (48, (0, 0, 0))]),
    }
    c_tab1.update(sec_curves("idle_table_01", 48))
    add_action("idle_table_01", 48, c_tab1)

    # --- IDLE_TABLE_02 (48 frames, leaning back against chair) ---
    c_tab2 = {
        "Pelvis": ([(1, (0, 0, 0)), (24, (0, 0.03, -0.01)), (48, (0, 0, 0))], None),
        "Spine": (None, [(1, (0, 0, 0)), (24, (0.08, 0, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (24, (0.10, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (24, (-0.12, 0.04, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.25, 0.15, 0.10)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.25, -0.15, -0.10)), (48, (0, 0, 0))]),
    }
    c_tab2.update(sec_curves("idle_table_02", 48))
    add_action("idle_table_02", 48, c_tab2)

    # --- IDLE_IMPATIENT (40 frames, slight finger tap & shift) ---
    c_impatient = {
        "Head": (None, [(1, (0, 0, 0)), (14, (-0.06, 0.12, 0.04)), (28, (0.06, -0.10, -0.04)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (15, (-0.20, -0.10, 0)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (15, (-0.45, 0, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (0.25, 0, 0)), (18, (-0.15, 0, 0)), (24, (0.25, 0, 0)), (30, (-0.15, 0, 0)), (40, (0, 0, 0))]),
    }
    c_impatient.update(sec_curves("idle_impatient", 40))
    add_action("idle_impatient", 40, c_impatient)

    # --- THINK (48 frames, calculating evaluation; Corvo pince-nez, Zeca chin-scratch) ---
    c_think = {
        "Chest": (None, [(1, (0, 0, 0)), (20, (-0.12, 0.06, 0)), (36, (-0.08, 0.04, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (20, (0.14, 0.16, 0.08)), (36, (0.10, 0.12, 0.06)), (48, (0, 0, 0))]),
        # Right hand raises toward face / glasses / chin
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.60, -0.22, 0.18)), (36, (-0.50, -0.18, 0.15)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (20, (-1.05, 0.10, 0.12)), (36, (-0.95, 0.08, 0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (20, (0.25, 0.12, -0.10)), (36, (0.20, 0.10, -0.08)), (48, (0, 0, 0))]),
    }
    c_think.update(sec_curves("think", 48))
    add_action("think", 48, c_think)

    # --- INSPECT_HAND (44 frames, tilting cards toward gaze) ---
    c_inspect = {
        "Chest": (None, [(1, (0, 0, 0)), (20, (-0.14, 0, 0)), (44, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (20, (0.22, 0, 0)), (44, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (20, (-0.45, 0.18, 0.14)), (44, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (20, (-0.80, 0.10, 0)), (44, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.45, -0.18, -0.14)), (44, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (20, (-0.80, -0.10, 0)), (44, (0, 0, 0))]),
    }
    c_inspect.update(sec_curves("inspect_hand", 44))
    add_action("inspect_hand", 44, c_inspect)

    # --- HOLD_CARDS (40 frames, defensive cards-to-chest posture) ---
    c_hold = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.10, 0, 0)), (40, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (18, (-0.35, 0.22, 0.16)), (40, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (18, (-0.95, 0.12, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.35, -0.22, -0.16)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (18, (-0.95, -0.12, 0)), (40, (0, 0, 0))]),
    }
    c_hold.update(sec_curves("hold_cards", 40))
    add_action("hold_cards", 40, c_hold)

    # =========================================================================
    # 3. EXTENDED CARD MANIPULATION ACTIONS
    # =========================================================================

    # --- PLAY_CARD_FAST (36 frames, crisp flick onto felt) ---
    c_fast = {
        "Chest": (None, [(1, (0, 0, 0)), (14, (-0.16, 0.03, 0)), (36, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (8, (0.25, -0.10, 0.15)), (16, (-0.75, -0.15, -0.20)), (36, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (8, (-0.50, 0, 0)), (16, (-0.35, 0, 0)), (36, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (8, (-0.25, 0, 0)), (16, (0.45, 0, 0)), (36, (0, 0, 0))]),
    }
    c_fast.update(sec_curves("play_card", 36))
    add_action("play_card_fast", 36, c_fast)

    # --- PLAY_CARD_DRAMATIC (52 frames, high arc and emphatic table slam) ---
    c_dramatic = {
        "Pelvis": ([(1, (0, 0, 0)), (16, (0, 0.04, 0.02)), (28, (0, -0.05, -0.02)), (52, (0, 0, 0))], None),
        "Chest": (None, [(1, (0, 0, 0)), (16, (0.15, -0.06, 0)), (28, (-0.32, 0.08, 0)), (52, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (-0.15, 0, 0)), (28, (0.30, 0, 0)), (52, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (0.75, -0.25, 0.35)), (28, (-1.10, -0.22, -0.30)), (52, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.85, 0.15, 0)), (28, (-0.45, -0.10, -0.15)), (52, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (16, (-0.35, 0, 0)), (28, (0.50, -0.15, 0.15)), (52, (0, 0, 0))]),
    }
    c_dramatic.update(sec_curves("play_card_dramatic", 52))
    add_action("play_card_dramatic", 52, c_dramatic)

    # --- DEAL (48 frames, dealing cards across table) ---
    c_deal = {
        "Chest": (None, [(1, (0, 0, 0)), (16, (-0.12, -0.15, 0)), (32, (-0.12, 0.15, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.55, -0.35, -0.25)), (32, (-0.55, 0.20, 0.15)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.40, 0.15, 0)), (32, (-0.40, -0.15, 0)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (16, (0.30, 0, 0)), (32, (0.30, 0, 0)), (48, (0, 0, 0))]),
    }
    c_deal.update(sec_curves("deal", 48))
    add_action("deal", 48, c_deal)

    # --- SHUFFLE (48 frames, riffle card shuffle at table) ---
    c_shuffle = {
        "Chest": (None, [(1, (0, 0, 0)), (24, (-0.16, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (24, (0.20, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.42, 0.18, 0.15)), (48, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (16, (-0.70, 0, 0)), (24, (-0.85, 0.10, 0)), (32, (-0.70, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.42, -0.18, -0.15)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.70, 0, 0)), (24, (-0.85, -0.10, 0)), (32, (-0.70, 0, 0)), (48, (0, 0, 0))]),
    }
    c_shuffle.update(sec_curves("shuffle", 48))
    add_action("shuffle", 48, c_shuffle)

    # --- CUT_DECK (40 frames, single-handed cut) ---
    c_cut = {
        "Chest": (None, [(1, (0, 0, 0)), (20, (-0.15, -0.06, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (12, (-0.45, -0.20, 0)), (20, (-0.65, -0.10, 0)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (12, (-0.60, 0.10, 0)), (20, (-0.40, 0, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (-0.15, 0, 0)), (20, (0.35, 0, 0)), (40, (0, 0, 0))]),
    }
    c_cut.update(sec_curves("cut_deck", 40))
    add_action("cut_deck", 40, c_cut)

    # =========================================================================
    # 4. POKER ROGUELIKE ACTIONS
    # =========================================================================

    # --- CHECK (36 frames, rap knuckles on felt twice) ---
    c_check = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.12, 0.04, 0)), (36, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (12, (-0.45, -0.15, 0)), (36, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (12, (-0.65, 0, 0)), (36, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (-0.20, 0, 0)), (16, (0.30, 0, 0)), (20, (-0.15, 0, 0)), (24, (0.30, 0, 0)), (36, (0, 0, 0))]),
    }
    c_check.update(sec_curves("check", 36))
    add_action("check", 36, c_check)

    # --- BET (40 frames, push chips forward with confidence) ---
    c_bet = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.16, 0.04, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.65, -0.18, -0.15)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (18, (-0.45, 0.05, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (18, (0.28, -0.05, 0)), (40, (0, 0, 0))]),
    }
    c_bet.update(sec_curves("bet", 40))
    add_action("bet", 40, c_bet)

    # --- ALL_IN (48 frames, forceful two-handed forward push) ---
    c_allin = {
        "Pelvis": ([(1, (0, 0, 0)), (22, (0, -0.06, -0.02)), (48, (0, 0, 0))], None),
        "Chest": (None, [(1, (0, 0, 0)), (22, (-0.30, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (22, (0.25, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (22, (-0.80, 0.25, 0.20)), (48, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (22, (-0.35, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (22, (-0.80, -0.25, -0.20)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (22, (-0.35, 0, 0)), (48, (0, 0, 0))]),
    }
    c_allin.update(sec_curves("all_in", 48))
    add_action("all_in", 48, c_allin)

    # --- FOLD (40 frames, discard hand face-down) ---
    c_fold = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.10, -0.05, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.08, 0.12, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.50, -0.15, -0.12)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (18, (-0.30, 0.10, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (18, (-0.35, 0, 0)), (40, (0, 0, 0))]),
    }
    c_fold.update(sec_curves("fold", 40))
    add_action("fold", 40, c_fold)

    # --- SHOWDOWN (48 frames, expose hand with flourish) ---
    c_showdown = {
        "Chest": (None, [(1, (0, 0, 0)), (22, (-0.18, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (22, (0.16, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (22, (-0.60, 0.35, 0.25)), (48, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (22, (0.15, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (22, (-0.60, -0.35, -0.25)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (22, (0.15, 0, 0)), (48, (0, 0, 0))]),
    }
    c_showdown.update(sec_curves("showdown", 48))
    add_action("showdown", 48, c_showdown)

    # =========================================================================
    # 5. TRUCO ACTIONS
    # =========================================================================

    # --- ACCEPT_TRUCO (40 frames, firm nod and decisive table smack) ---
    c_acc = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.22, 0.04, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.24, -0.04, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (10, (0.45, 0, 0.15)), (18, (-0.75, -0.15, -0.20)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (10, (-0.60, 0, 0)), (18, (-0.35, 0, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (18, (0.35, 0, 0)), (40, (0, 0, 0))]),
    }
    c_acc.update(sec_curves("accept_truco", 40))
    add_action("accept_truco", 40, c_acc)

    # --- DECLINE_TRUCO (40 frames, head shake, retreating cards) ---
    c_dec = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (0.12, 0, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (12, (-0.05, 0.20, 0)), (24, (-0.05, -0.20, 0)), (40, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (18, (-0.20, 0.12, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.20, -0.12, 0)), (40, (0, 0, 0))]),
    }
    c_dec.update(sec_curves("decline_truco", 40))
    add_action("decline_truco", 40, c_dec)

    # =========================================================================
    # 6. FODINHA ACTIONS
    # =========================================================================

    # --- BID_CONFIDENT (40 frames, chest out, chin high) ---
    c_bconf = {
        "Spine": (None, [(1, (0, 0, 0)), (18, (-0.12, 0, 0)), (40, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.16, 0, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.18, 0, 0)), (40, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (18, (-0.25, 0.15, 0.10)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.25, -0.15, -0.10)), (40, (0, 0, 0))]),
    }
    c_bconf.update(sec_curves("bid_confident", 40))
    add_action("bid_confident", 40, c_bconf)

    # --- BID_UNCERTAIN (40 frames, head tilt, questioning look) ---
    c_bunc = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.06, 0.08, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.12, 0.22, 0.10)), (40, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (18, (0.08, -0.05, -0.05)), (40, (0, 0, 0))]),
    }
    c_bunc.update(sec_curves("bid_uncertain", 40))
    add_action("bid_uncertain", 40, c_bunc)

    # --- BID_ZERO (40 frames, casual dismissive wave) ---
    c_bzero = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (0.08, -0.04, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.06, 0.10, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.40, -0.22, 0)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (18, (-0.45, 0.12, 0)), (40, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (18, (0.25, -0.20, 0)), (40, (0, 0, 0))]),
    }
    c_bzero.update(sec_curves("bid_zero", 40))
    add_action("bid_zero", 40, c_bzero)

    # --- TRICK_WIN (40 frames, crisp satisfied nod) ---
    c_twin = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.14, 0.04, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.18, -0.04, 0)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (18, (-0.40, -0.15, -0.10)), (40, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (18, (-0.50, 0.10, 0)), (40, (0, 0, 0))]),
    }
    c_twin.update(sec_curves("trick_win", 40))
    add_action("trick_win", 40, c_twin)

    # --- TRICK_LOSE (40 frames, small flinch and head dip) ---
    c_tlose = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (0.10, 0, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.14, 0.08, 0)), (40, (0, 0, 0))]),
    }
    c_tlose.update(sec_curves("trick_lose", 40))
    add_action("trick_lose", 40, c_tlose)

    # --- LIFE_LOST (44 frames, dramatic recoil/shudder) ---
    c_llost = {
        "Pelvis": ([(1, (0, 0, 0)), (16, (0, 0.08, -0.02)), (44, (0, 0, 0))], None),
        "Chest": (None, [(1, (0, 0, 0)), (16, (0.22, 0, 0)), (44, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (-0.24, 0, 0)), (44, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (16, (-0.45, 0.20, 0.15)), (44, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.45, -0.20, -0.15)), (44, (0, 0, 0))]),
    }
    c_llost.update(sec_curves("life_lost", 44))
    add_action("life_lost", 44, c_llost)

    # =========================================================================
    # 7. EMOTES & REACTION CLIPS
    # =========================================================================

    # --- SMALL_WIN (40 frames, subtle aristocratic smile) ---
    c_swin = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.08, 0.04, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.10, -0.06, 0)), (40, (0, 0, 0))]),
    }
    c_swin.update(sec_curves("small_win", 40))
    add_action("small_win", 40, c_swin)

    # --- BIG_WIN (56 frames, triumphant double arms raise) ---
    c_bwin = {
        "Pelvis": ([(1, (0, 0, 0)), (28, (0, 0, 0.06)), (56, (0, 0, 0))], None),
        "Chest": (None, [(1, (0, 0, 0)), (28, (-0.20, 0, 0)), (56, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (28, (0.28, 0, 0)), (56, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (28, (-1.90, 0.45, 1.40)), (56, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (28, (-1.90, -0.45, -1.40)), (56, (0, 0, 0))]),
    }
    c_bwin.update(sec_curves("big_win", 56))
    add_action("big_win", 56, c_bwin)

    # --- LOSE (48 frames, dejected slump) ---
    c_lose = {
        "Spine": (None, [(1, (0, 0, 0)), (24, (0.16, 0, 0)), (48, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (24, (0.20, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (24, (-0.25, 0, 0)), (48, (0, 0, 0))]),
        "Shoulder.L": (None, [(1, (0, 0, 0)), (24, (0.12, 0, 0)), (48, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (24, (0.12, 0, 0)), (48, (0, 0, 0))]),
    }
    c_lose.update(sec_curves("lose", 48))
    add_action("lose", 48, c_lose)

    # --- BAD_BEAT (48 frames, hand to forehead in shock) ---
    c_bbeat = {
        "Chest": (None, [(1, (0, 0, 0)), (22, (0.18, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (22, (-0.22, 0.08, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (22, (-0.75, -0.25, 0.20)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (22, (-1.20, 0.15, 0)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (22, (0.35, 0, 0)), (48, (0, 0, 0))]),
    }
    c_bbeat.update(sec_curves("bad_beat", 48))
    add_action("bad_beat", 48, c_bbeat)

    # --- SUSPICIOUS (44 frames, narrowing gaze at rival) ---
    c_susp = {
        "Chest": (None, [(1, (0, 0, 0)), (20, (-0.08, 0.12, 0)), (44, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (20, (0.06, 0.24, 0.06)), (44, (0, 0, 0))]),
    }
    c_susp.update(sec_curves("suspicious", 44))
    add_action("suspicious", 44, c_susp)

    # --- SURPRISED (40 frames, sudden recoil) ---
    c_surp = {
        "Chest": (None, [(1, (0, 0, 0)), (16, (0.20, 0, 0)), (40, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (-0.24, 0, 0)), (40, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (16, (-0.35, 0.20, 0.10)), (40, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.35, -0.20, -0.10)), (40, (0, 0, 0))]),
    }
    c_surp.update(sec_curves("surprised", 40))
    add_action("surprised", 40, c_surp)

    # --- LAUGH (44 frames, amused chuckle) ---
    c_laugh = {
        "Chest": (None, [(1, (0, 0, 0)), (10, (-0.10, 0, 0)), (18, (-0.04, 0, 0)), (26, (-0.10, 0, 0)), (34, (-0.04, 0, 0)), (44, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (10, (0.14, 0, 0)), (18, (0.06, 0, 0)), (26, (0.14, 0, 0)), (34, (0.06, 0, 0)), (44, (0, 0, 0))]),
    }
    c_laugh.update(sec_curves("laugh", 44))
    add_action("laugh", 44, c_laugh)

    # --- TAUNT (44 frames, provocative beckon) ---
    c_taunt = {
        "Chest": (None, [(1, (0, 0, 0)), (20, (-0.14, 0.08, 0)), (44, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (20, (0.12, -0.10, 0)), (44, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.55, -0.20, -0.15)), (44, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (15, (-0.75, 0, 0)), (22, (-0.55, 0, 0)), (29, (-0.75, 0, 0)), (44, (0, 0, 0))]),
    }
    c_taunt.update(sec_curves("taunt", 44))
    add_action("taunt", 44, c_taunt)

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
    print(f"[{ident.upper()}] Successfully exported GLB with 39 clips: {out_glb} ({out_glb.stat().st_size} bytes)")

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
    print("=== STARTING EXPANDED RIGGED CAST GENERATION (39 CLIPS) ===")
    for row in DETAILED_CAST:
        build_character(*row)
    print("=== ALL 5 DETAILED CHARACTERS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
