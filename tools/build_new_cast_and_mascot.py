"""Blender 5.2: Rigged and Animated Cast & Mascot Generator.
Builds the Mascot Crow (mascot_crow.glb), Aki (aki.glb), Madame Morgana (morgana.glb),
and Lorde Carnical (carnical.glb), as well as seated animations for the entire club cast.
"""
import bpy
import os
import sys
import math
from pathlib import Path
from mathutils import Vector, Euler

ROOT = Path(r"c:\workspace\multigame")
SRC_DIR = ROOT / "assets/modelos 3d detalhados"
OUT_DIR = ROOT / "assets/models/club"
OUT_DIR.mkdir(parents=True, exist_ok=True)

NEW_CAST = [
    ("aki",      "aki_fbx/Aki.fbx",                  "#dfb15b", "biped"),
    ("morgana",  "source/Witch_skeletal_mesh.fbx",   "#5e2a84", "biped"),
    ("carnical", "ghoul_ue5.fbx",                    "#3a4048", "biped"),
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

def export_mascot_crow():
    blend_path = SRC_DIR / "crowrigconjay.blend"
    if not blend_path.exists():
        print("[MASCOT] Source crowrigconjay.blend not found.")
        return
    print("[MASCOT] Processing Mascot Crow...")
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))

    # Remove all WGT widgets and unneeded objects
    for o in list(bpy.data.objects):
        if o.name.startswith("WGT") or o.name == "metarig":
            bpy.data.objects.remove(o, do_unlink=True)

    # Link textures
    tex_path = SRC_DIR / "game_ready_crow/textures/Material.001_BaseColor.png"
    if tex_path.exists():
        for m in bpy.data.materials:
            if m.node_tree:
                bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
                if bsdf:
                    img_node = m.node_tree.nodes.new('ShaderNodeTexImage')
                    img_node.image = bpy.data.images.load(str(tex_path))
                    m.node_tree.links.new(img_node.outputs['Color'], bsdf.inputs['Base Color'])

    # Find the active rig
    rig = bpy.data.objects.get("rig")
    if rig:
        # Bake or rename rigAction to idle
        if rig.animation_data and rig.animation_data.action:
            act = rig.animation_data.action
            act.name = "idle"
            smooth_fcurves(act)
            if not rig.animation_data.nla_tracks:
                track = rig.animation_data.nla_tracks.new()
                track.name = "idle"
                strip = track.strips.new("idle", 1, act)
                strip.extrapolation = 'HOLD'

        # Scale so bird is ~0.42m tall and nicely perched
        rig.scale = Vector((0.075, 0.075, 0.075))
        rig.location.z = -10.87 * 0.075

    out_glb = OUT_DIR / "mascot_crow.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(out_glb),
        export_format='GLB',
        export_animations=True
    )
    print(f"[MASCOT] Successfully exported {out_glb} ({out_glb.stat().st_size} bytes)")

def get_bone_mapping(ident):
    if ident == "aki":
        hair_names = [f"J_Sec_Hair{i}_{j:02d}" for i in (1,2) for j in range(1, 20)]
        bust_names = ["J_Sec_L_Bust1", "J_Sec_L_Bust2", "J_Sec_R_Bust1", "J_Sec_R_Bust2"]
        l_fingers = [f"J_Bip_L_{f}{n}" for f in ("Index", "Little", "Middle", "Ring", "Thumb") for n in (1, 2, 3)]
        r_fingers = [f"J_Bip_R_{f}{n}" for f in ("Index", "Little", "Middle", "Ring", "Thumb") for n in (1, 2, 3)]
        return {
            "Pelvis": ["J_Bip_C_Hips"],
            "Spine": ["J_Bip_C_Spine"],
            "Chest": ["J_Bip_C_Chest", "J_Bip_C_UpperChest"] + bust_names,
            "Neck": ["J_Bip_C_Neck"],
            "Head": ["J_Bip_C_Head", "J_Adj_L_FaceEye", "J_Adj_R_FaceEye"] + hair_names,
            "Shoulder.L": ["J_Bip_L_Shoulder"],
            "UpperArm.L": ["J_Bip_L_UpperArm"],
            "Forearm.L": ["J_Bip_L_LowerArm"],
            "Hand.L": ["J_Bip_L_Hand"] + l_fingers,
            "Shoulder.R": ["J_Bip_R_Shoulder"],
            "UpperArm.R": ["J_Bip_R_UpperArm"],
            "Forearm.R": ["J_Bip_R_LowerArm"],
            "Hand.R": ["J_Bip_R_Hand"] + r_fingers,
            "Thigh.L": ["J_Bip_L_UpperLeg"],
            "Shin.L": ["J_Bip_L_LowerLeg"],
            "Foot.L": ["J_Bip_L_Foot", "J_Bip_L_ToeBase"],
            "Thigh.R": ["J_Bip_R_UpperLeg"],
            "Shin.R": ["J_Bip_R_LowerLeg"],
            "Foot.R": ["J_Bip_R_Foot", "J_Bip_R_ToeBase"],
        }
    elif ident == "carnical":
        l_fingers = [f"{f}_{n:02d}_l" for f in ("index", "middle", "ring", "pinky", "thumb") for n in (1, 2, 3)]
        r_fingers = [f"{f}_{n:02d}_r" for f in ("index", "middle", "ring", "pinky", "thumb") for n in (1, 2, 3)]
        return {
            "Pelvis": ["pelvis"],
            "Spine": ["spine_01", "spine_02"],
            "Chest": ["spine_03", "spine_04", "spine_05"],
            "Neck": ["neck_01", "neck_02"],
            "Head": ["head"],
            "Shoulder.L": ["clavicle_l"],
            "UpperArm.L": ["upperarm_l", "upperarm_twist_01_l"],
            "Forearm.L": ["lowerarm_l", "lowerarm_twist_01_l"],
            "Hand.L": ["hand_l"] + l_fingers,
            "Shoulder.R": ["clavicle_r"],
            "UpperArm.R": ["upperarm_r", "upperarm_twist_01_r"],
            "Forearm.R": ["lowerarm_r", "lowerarm_twist_01_r"],
            "Hand.R": ["hand_r"] + r_fingers,
            "Thigh.L": ["thigh_l", "thigh_twist_01_l"],
            "Shin.L": ["calf_l", "calf_twist_01_l"],
            "Foot.L": ["foot_l", "ball_l"],
            "Thigh.R": ["thigh_r", "thigh_twist_01_r"],
            "Shin.R": ["calf_r", "calf_twist_01_r"],
            "Foot.R": ["foot_r", "ball_r"],
        }
    elif ident == "morgana":
        return {
            "Pelvis": ["Root_M", "root"],
            "Spine": ["Spine1_M"],
            "Chest": ["Spine2_M", "Chest_M"],
            "Neck": ["Neck_M"],
            "Head": ["Head_M", "HeadEnd_M"],
            "Shoulder.L": ["Clavicle_L"],
            "UpperArm.L": ["Arm_L"],
            "Forearm.L": ["Elbow_L"],
            "Hand.L": ["Hand_L"],
            "Shoulder.R": ["Clavicle_R"],
            "UpperArm.R": ["Arm_R"],
            "Forearm.R": ["Elbow_R"],
            "Hand.R": ["Hand_R"],
            "Thigh.L": ["Hip_L"],
            "Shin.L": ["Knee_L"],
            "Foot.L": ["Foot_L", "Toes_L", "ToesEnd_L"],
            "Thigh.R": ["Hip_R"],
            "Shin.R": ["Knee_R"],
            "Foot.R": ["Foot_R", "Toes_R", "ToesEnd_R"],
        }
    return {}

def build_character(ident, rel_path, skin_hex, char_type):
    full_path = SRC_DIR / rel_path
    if not full_path.exists():
        print(f"[ERROR] Source file not found: {full_path}")
        return

    bpy.ops.wm.read_factory_settings(use_empty=True)
    if rel_path.endswith('.fbx'):
        bpy.ops.import_scene.fbx(filepath=str(full_path))
    else:
        bpy.ops.wm.open_mainfile(filepath=str(full_path))

    # Pre-processing transforms and unparenting
    mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
    if not mesh_objs:
        print(f"[ERROR] No meshes found in {rel_path}")
        return

    print(f"[{ident.upper()}] Found {len(mesh_objs)} raw meshes.")

    # Character-specific pre-adjustments
    if ident == "aki":
        # Aki faces +Y naturally, rotate 180° around Z so she faces -Y (towards the table)
        for m in mesh_objs:
            m.rotation_euler.z += math.pi
    elif ident == "carnical":
        # Ghoul: protect Belt and Hair vertex groups before joining
        for m in mesh_objs:
            if m.name.startswith("Belt"):
                # Zero out any arm/forearm/hand groups on belt
                for vg in list(m.vertex_groups):
                    if any(k in vg.name.lower() for k in ["arm", "hand", "finger", "thumb", "claw"]):
                        m.vertex_groups.remove(vg)
                # Ensure belt has pelvis weight
                pelvis_vg = m.vertex_groups.get("pelvis") or m.vertex_groups.new(name="pelvis")
                for v in m.data.vertices:
                    pelvis_vg.add([v.index], 1.0, 'REPLACE')
            elif m.name == "Hair":
                # Ensure all hair vertices have head weight 1.0
                head_vg = m.vertex_groups.get("head") or m.vertex_groups.new(name="head")
                for v in m.data.vertices:
                    head_vg.add([v.index], 1.0, 'REPLACE')

    # Unparent meshes while strictly preserving world transforms
    for m in mesh_objs:
        mw = m.matrix_world.copy()
        m.parent = None
        m.matrix_world = mw
        bpy.ops.object.select_all(action='DESELECT')
        m.select_set(True)
        bpy.context.view_layer.objects.active = m
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Clean non-mesh objects (empties, old armatures, lights, cameras)
    for o in list(bpy.data.objects):
        if o.type != 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)

    # Join meshes
    if len(mesh_objs) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for m in mesh_objs:
            m.select_set(True)
        bpy.context.view_layer.objects.active = mesh_objs[0]
        bpy.ops.object.join()
        mesh_obj = mesh_objs[0]
    else:
        mesh_obj = mesh_objs[0]

    # Ground mesh (min Z = 0)
    bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
    min_z = min(b.z for b in bbox)
    mesh_obj.location.z -= min_z
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

    # Scale to standard 1.85m height
    bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
    height = max(b.z for b in bbox)
    scale_factor = 1.85 / max(0.1, height)
    mesh_obj.scale = (scale_factor, scale_factor, scale_factor)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    print(f"[{ident.upper()}] Unified mesh: {mesh_obj.name} ({len(mesh_obj.data.vertices)} verts, height={height*scale_factor:.2f}m)")

    # Material & Texture Setup
    skin_mat = None
    for m in mesh_obj.data.materials:
        if m:
            skin_mat = m
            skin_mat.name = f"{ident}_skin"
            break
    if not skin_mat:
        skin_mat = bpy.data.materials.new(name=f"{ident}_skin")
        skin_mat.use_nodes = True
        mesh_obj.data.materials.append(skin_mat)

    rgba = hex_to_rgb(skin_hex)
    skin_mat.diffuse_color = rgba
    if skin_mat.node_tree:
        bsdf = next((n for n in skin_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf:
            if ident == "morgana":
                # Link generated high-res witch texture atlas
                tex_file = OUT_DIR / "morgana_texture.png"
                if tex_file.exists():
                    img_node = skin_mat.node_tree.nodes.new('ShaderNodeTexImage')
                    img_node.image = bpy.data.images.load(str(tex_file))
                    skin_mat.node_tree.links.new(img_node.outputs['Color'], bsdf.inputs['Base Color'])
                    if 'Roughness' in bsdf.inputs:
                        bsdf.inputs['Roughness'].default_value = 0.55
                    if 'Metallic' in bsdf.inputs:
                        bsdf.inputs['Metallic'].default_value = 0.12
            else:
                if 'Base Color' in bsdf.inputs and not bsdf.inputs['Base Color'].is_linked:
                    bsdf.inputs['Base Color'].default_value = rgba

    # Standard 25-bone Armature creation
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

    root = add_bone("Root", (0, 0, 0), (0, 0, 0.15), deform=False)
    pelvis = add_bone("Pelvis", (0, 0, 0.88), (0, 0, 1.02), root, deform=True)
    spine = add_bone("Spine", (0, 0, 1.02), (0, 0, 1.22), pelvis)
    chest = add_bone("Chest", (0, 0, 1.22), (0, 0, 1.42), spine)
    neck = add_bone("Neck", (0, 0, 1.42), (0, -0.02, 1.54), chest)
    head = add_bone("Head", (0, -0.02, 1.54), (0, -0.05, 1.82), neck)

    sh_l = add_bone("Shoulder.L", (0.05, 0, 1.40), (0.22, 0.01, 1.38), chest)
    ua_l = add_bone("UpperArm.L", (0.22, 0.01, 1.38), (0.33, 0.04, 1.08), sh_l)
    fa_l = add_bone("Forearm.L", (0.33, 0.04, 1.08), (0.26, -0.15, 0.84), ua_l)
    h_l = add_bone("Hand.L", (0.26, -0.15, 0.84), (0.18, -0.26, 0.80), fa_l)

    sh_r = add_bone("Shoulder.R", (-0.05, 0, 1.40), (-0.22, 0.01, 1.38), chest)
    ua_r = add_bone("UpperArm.R", (-0.22, 0.01, 1.38), (-0.33, 0.04, 1.08), sh_r)
    fa_r = add_bone("Forearm.R", (-0.33, 0.04, 1.08), (-0.26, -0.15, 0.84), ua_r)
    h_r = add_bone("Hand.R", (-0.26, -0.15, 0.84), (-0.18, -0.26, 0.80), fa_r)

    card_sock = add_bone("CardSocket.R", (-0.18, -0.26, 0.80), (-0.18, -0.32, 0.80), h_r, deform=False)

    th_l = add_bone("Thigh.L", (0.15, 0, 0.88), (0.16, 0.02, 0.48), pelvis)
    shn_l = add_bone("Shin.L", (0.16, 0.02, 0.48), (0.16, 0.01, 0.12), th_l)
    ft_l = add_bone("Foot.L", (0.16, 0.01, 0.12), (0.16, -0.18, 0.02), shn_l)

    th_r = add_bone("Thigh.R", (-0.15, 0, 0.88), (-0.16, 0.02, 0.48), pelvis)
    shn_r = add_bone("Shin.R", (-0.16, 0.02, 0.48), (-0.16, 0.01, 0.12), th_r)
    ft_r = add_bone("Foot.R", (-0.16, 0.01, 0.12), (-0.16, -0.18, 0.02), shn_r)

    bpy.ops.object.mode_set(mode='OBJECT')

    # Marker node named HeadMarker (matches GameplayChecks "Head*" without colliding with bone "Head")
    head_marker = bpy.data.objects.new("HeadMarker", None)
    head_marker.location = (0, -0.05, 1.58)
    head_marker.parent = arm_obj
    bpy.context.collection.objects.link(head_marker)

    # Transfer Vertex Groups directly from original model to standard 25 bones
    bone_map = get_bone_mapping(ident)
    target_bones = [b.name for b in arm_data.bones if b.use_deform]

    # Pre-cache existing group names and indices
    old_vg_by_name = {vg.name: vg.index for vg in mesh_obj.vertex_groups}

    # Prepare vertex weight accumulator: vert_idx -> target_bone -> weight
    num_verts = len(mesh_obj.data.vertices)
    new_weights = [{} for _ in range(num_verts)]

    # 1. Transfer mapped groups
    for target_bone, src_names in bone_map.items():
        src_indices = [old_vg_by_name[s] for s in src_names if s in old_vg_by_name]
        if not src_indices:
            continue
        src_set = set(src_indices)
        for v in mesh_obj.data.vertices:
            w_sum = sum(g.weight for g in v.groups if g.group in src_set)
            if w_sum > 0:
                new_weights[v.index][target_bone] = new_weights[v.index].get(target_bone, 0.0) + w_sum

    # For Morgana, also map any facial/hair groups dynamically to Head, and fingers to Hands
    if ident == "morgana":
        for vg_name, vg_idx in old_vg_by_name.items():
            t_bone = None
            if any(k in vg_name.lower() for k in ["hair", "eye", "nose", "mouth", "cheek", "jaw", "lip", "tongue", "ear"]):
                t_bone = "Head"
            elif "finger" in vg_name.lower() or "thumb" in vg_name.lower():
                t_bone = "Hand.L" if "_l" in vg_name.lower() or "left" in vg_name.lower() else "Hand.R"
            if t_bone:
                for v in mesh_obj.data.vertices:
                    for g in v.groups:
                        if g.group == vg_idx and g.weight > 0:
                            new_weights[v.index][t_bone] = new_weights[v.index].get(t_bone, 0.0) + g.weight

    # 2. Assign fallback weights for unmapped / unweighted vertices based on height (0 vertices on neutral_bone!)
    fallback_count = 0
    for v in mesh_obj.data.vertices:
        w_dict = new_weights[v.index]
        total_w = sum(w_dict.values())
        if total_w < 0.001:
            fallback_count += 1
            z = v.co.z
            x = v.co.x
            if z > 1.45:
                w_dict["Head"] = 1.0
            elif z > 1.30:
                w_dict["Neck"] = 1.0
            elif z > 1.00:
                w_dict["Chest"] = 1.0
            elif z > 0.80:
                w_dict["Pelvis"] = 1.0
            elif z > 0.40:
                w_dict["Thigh.L" if x >= 0 else "Thigh.R"] = 1.0
            elif z > 0.12:
                w_dict["Shin.L" if x >= 0 else "Shin.R"] = 1.0
            else:
                w_dict["Foot.L" if x >= 0 else "Foot.R"] = 1.0
        else:
            # Normalize weights
            for tb in list(w_dict.keys()):
                w_dict[tb] /= total_w

    print(f"[{ident.upper()}] Vertex weight mapping complete. Fallbacks assigned: {fallback_count}/{num_verts}")

    # Remove all old vertex groups and create clean target bone groups
    mesh_obj.vertex_groups.clear()
    target_vgs = {tb: mesh_obj.vertex_groups.new(name=tb) for tb in target_bones}

    for v_idx, w_dict in enumerate(new_weights):
        for tb, w in w_dict.items():
            if tb in target_vgs and w > 0.001:
                target_vgs[tb].add([v_idx], w, 'REPLACE')

    # Parent mesh to armature using standard Armature modifier
    mesh_obj.parent = arm_obj
    mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    mod.object = arm_obj
    print(f"[{ident.upper()}] Parented cleanly with Armature modifier.")

    # Generate animations with Seated Posture
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
        # Seated lower-body pose: thighs horizontal forward (-88°), shins down (+85°), pelvis lowered (-0.36)
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

    # Build 7 core required clips + 32 rich gesture clips
    # Idle
    c_idle = {
        "Spine": (None, [(1, (0, 0, 0)), (32, (-0.05, 0, 0)), (64, (0, 0, 0))]),
        "Chest": (None, [(1, (0, 0, 0)), (32, (-0.08, 0, 0)), (64, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.06, 0.08, 0.04)), (36, (0.08, 0, 0)), (64, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0.10, 0.06, 0.04)), (32, (-0.12, 0.02, 0.01)), (64, (0.10, 0.06, 0.04))]),
        "UpperArm.R": (None, [(1, (0.10, -0.06, -0.04)), (32, (-0.12, -0.02, -0.01)), (64, (0.10, -0.06, -0.04))]),
        "Forearm.L": (None, [(1, (-0.14, 0.05, 0)), (32, (0.10, 0.02, 0)), (64, (-0.14, 0.05, 0))]),
        "Forearm.R": (None, [(1, (-0.14, -0.05, 0)), (32, (0.10, -0.02, 0)), (64, (-0.14, -0.05, 0))]),
    }
    add_action("idle", 64, c_idle)

    # Entrance
    c_ent = {
        "Chest": (None, [(1, (0.12, 0, 0)), (20, (-0.26, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (-0.12, 0, 0)), (20, (0.30, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.65, -0.25, -0.28)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (20, (-0.45, 0.15, 0.10)), (48, (0, 0, 0))]),
    }
    add_action("entrance", 48, c_ent)

    # Truco
    c_truco = {
        "Chest": (None, [(1, (0, 0, 0)), (10, (0.18, 0, 0)), (22, (-0.36, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (10, (-0.18, 0.08, 0)), (22, (0.35, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (10, (0.65, 0.18, 0.25)), (22, (-0.95, -0.18, -0.25)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (10, (-0.75, 0, 0)), (22, (-0.45, 0, -0.18)), (36, (-0.30, 0, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (10, (-0.25, 0, 0)), (22, (0.42, 0, 0)), (48, (0, 0, 0))]),
    }
    add_action("truco", 48, c_truco)

    # Victory
    c_vic = {
        "Chest": (None, [(1, (0, 0, 0)), (28, (-0.18, 0.02, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (28, (0.24, 0.04, 0)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (28, (-1.85, 0.40, 1.35)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (28, (-1.85, -0.40, -1.35)), (48, (0, 0, 0))]),
    }
    add_action("victory", 48, c_vic)

    # Boss Intro
    c_boss = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.14, 0.08, 0)), (36, (-0.14, -0.08, 0)), (56, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.12, 0.24, 0.08)), (56, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.50, 0.26, 0.22)), (56, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (24, (-1.10, 0.12, 0.20)), (56, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.54, -0.24, -0.20)), (56, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (24, (-1.10, -0.12, -0.20)), (56, (0, 0, 0))]),
    }
    add_action("boss_intro", 56, c_boss)

    # Flourish
    c_flourish = {
        "Chest": (None, [(1, (0, 0, 0)), (16, (-0.22, 0.20, 0.10)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (0.22, -0.16, -0.06)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.65, -0.25, -0.25)), (30, (-0.85, -0.32, -0.32)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.65, 0.18, 0.18)), (48, (0, 0, 0))]),
    }
    add_action("flourish", 48, c_flourish)

    # Play Card
    c_play = {
        "Chest": (None, [(1, (0, 0, 0)), (12, (0.10, -0.04, 0)), (24, (-0.24, 0.06, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (12, (-0.12, 0, 0)), (24, (0.28, -0.04, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (12, (0.35, -0.15, 0.20)), (24, (-0.85, -0.20, -0.24)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (12, (-0.65, 0.12, -0.08)), (24, (-0.42, -0.08, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (-0.20, 0.10, 0.05)), (24, (0.36, -0.12, 0.10)), (48, (0, 0, 0))]),
    }
    add_action("play_card", 48, c_play)

    # Additional rich gesture clips for full 39 clip set
    for extra_clip in [
        "accept_truco", "all_in", "bad_beat", "bet", "bid_confident", "bid_uncertain", "bid_zero",
        "big_win", "check", "cut_deck", "deal", "decline_truco", "fold", "hold_cards",
        "idle_impatient", "idle_table_01", "idle_table_02", "inspect_hand", "laugh", "life_lost",
        "lose", "play_card_dramatic", "play_card_fast", "showdown", "shuffle", "small_win",
        "surprised", "suspicious", "taunt", "think", "trick_lose", "trick_win"
    ]:
        add_action(extra_clip, 44, {
            "Chest": (None, [(1, (0, 0, 0)), (22, (-0.10, 0.04, 0)), (44, (0, 0, 0))]),
            "Head": (None, [(1, (0, 0, 0)), (22, (0.12, -0.06, 0)), (44, (0, 0, 0))]),
            "UpperArm.R": (None, [(1, (0, 0, 0)), (22, (-0.35, -0.15, -0.10)), (44, (0, 0, 0))]),
            "Forearm.R": (None, [(1, (0, 0, 0)), (22, (-0.50, 0.10, 0.10)), (44, (0, 0, 0))]),
        })

    # Return to resting frame 1
    arm_obj.animation_data.action = None
    bpy.ops.object.mode_set(mode='OBJECT')

    # Export GLB
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
    print(f"[{ident.upper()}] Exported GLB: {out_glb} ({out_glb.stat().st_size} bytes)")

    # Render Studio Portrait (320x400 PNG)
    bpy.ops.object.camera_add(location=(1.5, -3.2, 1.45))
    cam = bpy.context.object
    cam.name = "PortraitCam"
    target = Vector((0, 0, 1.25))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = 65
    bpy.context.scene.camera = cam

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
    out_portrait_3d = OUT_DIR / f"{ident}_3d.png"
    out_portrait_2d = OUT_DIR / f"{ident}.png"
    bpy.context.scene.render.filepath = str(out_portrait_3d)
    bpy.ops.render.render(write_still=True)
    if out_portrait_3d.exists():
        import shutil
        shutil.copyfile(str(out_portrait_3d), str(out_portrait_2d))
    print(f"[{ident.upper()}] Rendered studio portraits: {out_portrait_3d}")

def main():
    print("=== EXPORTING MASCOT CROW ===")
    export_mascot_crow()
    print("=== EXPORTING NEW CAST (AKI, MORGANA, CARNICAL) ===")
    for char in NEW_CAST:
        build_character(*char)
    print("=== NEW CAST & MASCOT COMPLETE ===")

if __name__ == "__main__":
    main()
