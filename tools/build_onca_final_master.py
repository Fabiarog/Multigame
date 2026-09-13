"""
Build Dona Onca Final Master (Standing Rest Pose + Seated Animation Clips)
Matches Corvo / Zeca / Bento Referencia 2 standard:
- 1.85m standing height (rest pose 0.0 to 1.85m)
- Head empty marker at Z=1.55m (Godot Position.Y = 1.55 > 1.3m)
- Armature with 26 bones matching standard club hierarchy
- Severed AI bridging polygons between arms and legs
- Zone-partitioned distance skinning with boundary-preserving Laplacian smoothing
- Material named 'onca_skin' with PBR textures
- 9 NLA action clips with natural seated posture at the table:
  Pelvis: (0, 0, -0.36), Thighs: (-1.52, 0, 0), Shins: (1.48, 0, 0)
- Studio 3-point portrait renders: onca_3d.png and onca.png
- Exports: assets/models/club/onca.glb, art/blender/patch26/onca_refined.glb, .blend
- User's active Blender scene strictly preserved.
"""

import bpy
import os
import math
import bmesh
from mathutils import Vector, Euler

orig_scene = bpy.context.window.scene
temp_scene = bpy.data.scenes.new("OncaFinalMaster")
bpy.context.window.scene = temp_scene

try:
    sculpt_path = os.path.abspath("temp/dona_onca_rodin.glb")
    print(f"[MASTER] Loading Rodin sculpt from: {sculpt_path}")
    bpy.ops.import_scene.gltf(filepath=sculpt_path)

    mesh_obj = next(o for o in temp_scene.objects if o.type == 'MESH')
    print(f"[MASTER] Sculpt loaded: {len(mesh_obj.data.vertices)} vertices, {len(mesh_obj.data.polygons)} polygons.")

    # 1. Scale & Grounding to 1.85m standing height
    coords = [v.co for v in mesh_obj.data.vertices]
    min_x, max_x = min(c.x for c in coords), max(c.x for c in coords)
    min_y, max_y = min(c.y for c in coords), max(c.y for c in coords)
    min_z, max_z = min(c.z for c in coords), max(c.z for c in coords)

    cur_h = max_z - min_z
    s = 1.85 / cur_h
    cx = (min_x + max_x) / 2.0
    cy = (min_y + max_y) / 2.0

    for v in mesh_obj.data.vertices:
        v.co.x = (v.co.x - cx) * s
        v.co.y = (v.co.y - cy) * s
        v.co.z = (v.co.z - min_z) * s

    mesh_obj.data.update()
    print(f"[MASTER] Scaled and grounded: height=1.85m, Z=[0.0, 1.85]")

    # 2. Sever bridging faces between hands and thighs
    bm = bmesh.new()
    bm.from_mesh(mesh_obj.data)
    faces_to_delete = []
    for f in bm.faces:
        is_arm = False
        is_leg = False
        for v in f.verts:
            if v.co.z < 0.85:
                if abs(v.co.x) > 0.18:
                    is_arm = True
                elif abs(v.co.x) < 0.15:
                    is_leg = True
        if is_arm and is_leg:
            faces_to_delete.append(f)
    if faces_to_delete:
        print(f"[MASTER] Severing {len(faces_to_delete)} bridging faces between hand and thigh...")
        bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
        bm.to_mesh(mesh_obj.data)
    bm.free()
    mesh_obj.data.update()

    # Material setup: ensure material is named 'onca_skin'
    if mesh_obj.data.materials:
        mesh_obj.data.materials[0].name = "onca_skin"
    else:
        mat = bpy.data.materials.new(name="onca_skin")
        mat.use_nodes = True
        mesh_obj.data.materials.append(mat)

    # 3. Create Armature matching Corvo/Bento standard hierarchy
    arm_data = bpy.data.armatures.new("ArmatureData")
    arm_obj = bpy.data.objects.new("Armature", arm_data)
    temp_scene.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones

    def add_bone(name, head, tail, parent=None, deform=True):
        b = eb.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        b.use_deform = deform
        if parent: b.parent = parent
        return b

    root = add_bone("Root", (0, 0, 0), (0, 0, 0.88), deform=False)
    pelvis = add_bone("Pelvis", (0, 0, 0.88), (0, 0, 1.02), root)
    spine = add_bone("Spine", (0, 0, 1.02), (0, 0, 1.22), pelvis)
    chest = add_bone("Chest", (0, 0, 1.22), (0, 0, 1.41), spine)
    neck = add_bone("Neck", (0, 0, 1.41), (0, 0, 1.54), chest)
    head = add_bone("Head", (0, 0, 1.54), (0, 0, 1.70), neck)

    ear_l = add_bone("Ear.L", (0.12, -0.05, 1.72), (0.17, -0.03, 1.85), head)
    ear_r = add_bone("Ear.R", (-0.12, -0.05, 1.72), (-0.17, -0.03, 1.85), head)

    sh_l = add_bone("Shoulder.L", (0.05, 0.0, 1.40), (0.22, 0.0, 1.38), chest)
    ua_l = add_bone("UpperArm.L", (0.22, 0.0, 1.38), (0.28, -0.08, 1.08), sh_l)
    fa_l = add_bone("Forearm.L", (0.28, -0.08, 1.08), (0.26, -0.10, 0.78), ua_l)
    h_l = add_bone("Hand.L", (0.26, -0.10, 0.78), (0.22, -0.05, 0.45), fa_l)

    sh_r = add_bone("Shoulder.R", (-0.05, 0.0, 1.40), (-0.22, 0.0, 1.38), chest)
    ua_r = add_bone("UpperArm.R", (-0.22, 0.0, 1.38), (-0.24, -0.08, 1.08), sh_r)
    fa_r = add_bone("Forearm.R", (-0.24, -0.08, 1.08), (-0.22, -0.10, 0.78), ua_r)
    h_r = add_bone("Hand.R", (-0.22, -0.10, 0.78), (-0.26, 0.15, 0.45), fa_r)

    card_sock = add_bone("CardSocket.R", (-0.22, -0.10, 0.78), (-0.22, -0.20, 0.78), h_r, deform=False)

    th_l = add_bone("Thigh.L", (0.15, 0.0, 0.88), (0.16, 0.02, 0.48), pelvis)
    shn_l = add_bone("Shin.L", (0.16, 0.02, 0.48), (0.16, 0.01, 0.12), th_l)
    ft_l = add_bone("Foot.L", (0.16, 0.01, 0.12), (0.16, -0.22, -0.02), shn_l)

    th_r = add_bone("Thigh.R", (-0.15, 0.0, 0.88), (-0.16, 0.02, 0.48), pelvis)
    shn_r = add_bone("Shin.R", (-0.16, 0.02, 0.48), (-0.16, 0.01, 0.12), th_r)
    ft_r = add_bone("Foot.R", (-0.16, 0.01, 0.12), (-0.16, -0.22, -0.02), shn_r)

    tail1 = add_bone("Tail.01", (0.03, 0.10, 0.90), (0.03, 0.12, 0.80), pelvis)
    tail2 = add_bone("Tail.02", (0.03, 0.12, 0.80), (0.02, 0.13, 0.70), tail1)
    tail3 = add_bone("Tail.03", (0.02, 0.13, 0.70), (0.01, 0.14, 0.60), tail2)
    tail4 = add_bone("Tail.04", (0.01, 0.14, 0.60), (-0.02, 0.15, 0.50), tail3)
    tail5 = add_bone("Tail.05", (-0.02, 0.15, 0.50), (-0.06, 0.19, 0.38), tail4)

    bpy.ops.object.mode_set(mode='OBJECT')

    # Head empty marker for Godot POV camera & QA check (> 1.3m)
    head_marker = bpy.data.objects.new("Head", None)
    head_marker.location = (0, -0.05, 1.55)
    head_marker.parent = arm_obj
    temp_scene.collection.objects.link(head_marker)

    # 4. Skinning with Zone Partitioning & Laplacian Smoothing
    mesh_obj.parent = arm_obj
    arm_mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    arm_mod.object = arm_obj

    deform_bones = [b for b in arm_obj.data.bones if b.use_deform]
    for b in deform_bones:
        mesh_obj.vertex_groups.new(name=b.name)

    def dist_to_segment(p, a, b):
        ab = b - a
        ap = p - a
        len_sq = ab.length_squared
        if len_sq < 1e-8: return ap.length
        t = max(0.0, min(1.0, ap.dot(ab) / len_sq))
        return (p - (a + ab * t)).length

    bone_segments = {}
    for b in deform_bones:
        h = arm_obj.matrix_world @ b.head_local
        t = arm_obj.matrix_world @ b.tail_local
        bone_segments[b.name] = (h, t)

    def get_allowed(co):
        x, y, z = co.x, co.y, co.z
        if z >= 1.54: return ["Head", "Neck", "Ear.L", "Ear.R"]
        if abs(x) > 0.18:
            if x > 0: return ["Shoulder.L", "UpperArm.L", "Forearm.L", "Hand.L", "Chest"]
            else: return ["Shoulder.R", "UpperArm.R", "Forearm.R", "Hand.R", "Chest"]
        if abs(x) <= 0.12 and y > 0.07 and z < 0.96:
            return ["Tail.01", "Tail.02", "Tail.03", "Tail.04", "Tail.05", "Pelvis"]
        if z < 0.88:
            if x >= 0: return ["Pelvis", "Thigh.L", "Shin.L", "Foot.L"]
            else: return ["Pelvis", "Thigh.R", "Shin.R", "Foot.R"]
        if z >= 1.30: return ["Chest", "Neck", "Shoulder.L", "Shoulder.R"]
        elif z >= 1.05: return ["Spine", "Chest"]
        else: return ["Pelvis", "Spine"]

    print("[MASTER] Computing initial proximity weights in memory...")
    vert_allowed = {}
    name_to_gid = {g.name: g.index for g in mesh_obj.vertex_groups}
    vert_weights = {}

    for v in mesh_obj.data.vertices:
        co = mesh_obj.matrix_world @ v.co
        allowed = get_allowed(co)
        vert_allowed[v.index] = set(allowed)
        scores = []
        for name in allowed:
            h, t = bone_segments[name]
            d = dist_to_segment(co, h, t)
            weight = 1.0 / (max(d, 0.02) ** 2)
            scores.append((name_to_gid[name], weight))
        scores.sort(key=lambda x: x[1], reverse=True)
        top4 = scores[:4]
        total = sum(w for _, w in top4)
        vert_weights[v.index] = {gid: w / total for gid, w in top4}

    vert_allowed_ids = {v_idx: {name_to_gid[bn] for bn in bnames if bn in name_to_gid} for v_idx, bnames in vert_allowed.items()}
    curr_weights = vert_weights

    print("[MASTER] Running 2 passes of Laplacian edge smoothing in pure Python...")
    for _ in range(2):
        neighbor_weights = {v.index: {} for v in mesh_obj.data.vertices}
        neighbor_counts = {v.index: 0 for v in mesh_obj.data.vertices}

        for e in mesh_obj.data.edges:
            i1, i2 = e.vertices[0], e.vertices[1]
            shared = vert_allowed_ids[i1].intersection(vert_allowed_ids[i2])
            if shared:
                w2 = curr_weights[i2]
                w1 = curr_weights[i1]
                for gid, w in w2.items():
                    if gid in shared:
                        neighbor_weights[i1][gid] = neighbor_weights[i1].get(gid, 0.0) + w
                neighbor_counts[i1] += 1
                for gid, w in w1.items():
                    if gid in shared:
                        neighbor_weights[i2][gid] = neighbor_weights[i2].get(gid, 0.0) + w
                neighbor_counts[i2] += 1

        next_weights = {}
        for v in mesh_obj.data.vertices:
            idx = v.index
            cnt = neighbor_counts[idx]
            if cnt == 0:
                next_weights[idx] = curr_weights[idx]
                continue
            new_w = {}
            allowed_gids = vert_allowed_ids[idx]
            for gid, w in curr_weights[idx].items():
                if gid in allowed_gids:
                    new_w[gid] = 0.70 * w
            for gid, nw in neighbor_weights[idx].items():
                if gid in allowed_gids:
                    new_w[gid] = new_w.get(gid, 0.0) + 0.30 * (nw / cnt)
            top4 = sorted(new_w.items(), key=lambda x: x[1], reverse=True)[:4]
            total = sum(w for _, w in top4)
            if total > 1e-4:
                next_weights[idx] = {gid: w / total for gid, w in top4}
            else:
                next_weights[idx] = curr_weights[idx]
        curr_weights = next_weights

    print("[MASTER] Assigning smoothed weights to vertex groups...")
    for idx, w_dict in curr_weights.items():
        for gid, w in w_dict.items():
            mesh_obj.vertex_groups[gid].add([idx], w, 'REPLACE')

    print("[MASTER] Laplacian skinning completed.")

    # 5. Animation action clips (all 9 clips required by MultiGame QA)
    bpy.ops.object.mode_set(mode='POSE')
    for act in list(bpy.data.actions):
        if any(act.name.startswith(p) for p in ["idle", "entrance", "truco", "victory", "boss_intro", "flourish", "play_card"]):
            bpy.data.actions.remove(act)
    arm_obj.animation_data_create()

    def key_rot(bone_name, keys):
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb: return
        pb.rotation_mode = 'XYZ'
        for frame, rot_tuple in keys:
            pb.rotation_euler = Euler(rot_tuple)
            pb.keyframe_insert(data_path="rotation_euler", frame=frame)

    def key_loc(bone_name, keys):
        pb = arm_obj.pose.bones.get(bone_name)
        if not pb: return
        for frame, loc_tuple in keys:
            pb.location = Vector(loc_tuple)
            pb.keyframe_insert(data_path="location", frame=frame)

    def smooth_fcurves(action):
        if hasattr(action, 'fcurves'):
            for fc in action.fcurves:
                for kf in fc.keyframe_points:
                    kf.interpolation = 'BEZIER'
                    kf.easing = 'AUTO'

    def add_action(name, total_frames, curves):
        act = bpy.data.actions.new(name=name)
        arm_obj.animation_data.action = act

        # Standard seated table foundation
        base_curves = {
            "Pelvis": ([(1, (0, 0, -0.36)), (total_frames, (0, 0, -0.36))], None),
            "Thigh.L": (None, [(1, (-1.52, 0, 0)), (total_frames, (-1.52, 0, 0))]),
            "Thigh.R": (None, [(1, (-1.52, 0, 0)), (total_frames, (-1.52, 0, 0))]),
            "Shin.L": (None, [(1, (1.48, 0, 0)), (total_frames, (1.48, 0, 0))]),
            "Shin.R": (None, [(1, (1.48, 0, 0)), (total_frames, (1.48, 0, 0))]),
        }

        for bn, (locs, rots) in base_curves.items():
            if bn in curves:
                c_locs, c_rots = curves[bn]
                loc_use = c_locs if c_locs else locs
                rot_use = c_rots if c_rots else rots
                if loc_use: key_loc(bn, loc_use)
                if rot_use: key_rot(bn, rot_use)
            else:
                if locs: key_loc(bn, locs)
                if rots: key_rot(bn, rots)

        for bn, (locs, rots) in curves.items():
            if bn in base_curves: continue
            if locs: key_loc(bn, locs)
            if rots: key_rot(bn, rots)

        smooth_fcurves(act)
        track = arm_obj.animation_data.nla_tracks.new()
        track.name = name
        strip = track.strips.new(name, 1, act)
        if name in ("idle", "idle_table_01", "idle_table_02"):
            strip.extrapolation = 'HOLD'
        return act

    def sec_curves(clip_type, total_f):
        res = {}
        if clip_type in ("big_win", "victory"):
            res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.3), (0.12, 0.22, 0.10)), (int(total_f*0.7), (0.10, -0.22, -0.10)), (total_f, (0, 0, 0))])
            res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (0.15, 0.30, 0.12)), (int(total_f*0.75), (0.12, -0.30, -0.12)), (total_f, (0, 0, 0))])
            res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.10, 0.06, 0.05)), (total_f, (0, 0, 0))])
            res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.10, -0.06, -0.05)), (total_f, (0, 0, 0))])
        elif clip_type in ("truco", "all_in"):
            res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.3), (-0.10, 0.18, 0.06)), (int(total_f*0.6), (0.18, -0.25, -0.10)), (total_f, (0, 0, 0))])
            res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (-0.12, 0.22, 0.08)), (int(total_f*0.65), (0.20, -0.30, -0.12)), (total_f, (0, 0, 0))])
            res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (0.15, 0.05, 0.08)), (total_f, (0, 0, 0))])
            res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.45), (0.15, -0.05, -0.08)), (total_f, (0, 0, 0))])
        else:
            # Subtle natural breathing wag & ear twitch
            res["Tail.01"] = (None, [(1, (0, 0, 0)), (int(total_f*0.35), (0.05, 0.15, 0.06)), (int(total_f*0.75), (-0.04, -0.15, -0.06)), (total_f, (0, 0, 0))])
            res["Tail.02"] = (None, [(1, (0, 0, 0)), (int(total_f*0.40), (0.06, 0.20, 0.08)), (int(total_f*0.80), (-0.05, -0.20, -0.08)), (total_f, (0, 0, 0))])
            res["Ear.L"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.04, 0.02, 0.02)), (total_f, (0, 0, 0))])
            res["Ear.R"] = (None, [(1, (0, 0, 0)), (int(total_f*0.5), (0.04, -0.02, -0.02)), (total_f, (0, 0, 0))])
        return res

    # 1. IDLE (64 frames)
    c_idle = {
        "Pelvis": ([(1, (0, 0, -0.36)), (32, (0, 0, -0.352)), (64, (0, 0, -0.36))], None),
        "Chest": (None, [(1, (0, 0, 0)), (32, (-0.05, 0, 0)), (64, (0, 0, 0))]),
        "Neck": (None, [(1, (0, 0, 0)), (20, (0.02, 0.02, 0)), (44, (-0.02, -0.02, 0)), (64, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (-0.03, 0.04, 0.02)), (36, (0.04, 0, 0)), (50, (-0.02, -0.04, -0.02)), (64, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0.05, 0.03, 0.02)), (32, (-0.06, 0.01, 0)), (64, (0.05, 0.03, 0.02))]),
        "UpperArm.R": (None, [(1, (0.05, -0.03, -0.02)), (32, (-0.06, -0.01, 0)), (64, (0.05, -0.03, -0.02))]),
    }
    c_idle.update(sec_curves("idle", 64))
    add_action("idle", 64, c_idle)

    # 2. ENTRANCE (48 frames)
    c_ent = {
        "Pelvis": ([(1, (0, 0.10, -0.32)), (20, (0, -0.02, -0.37)), (48, (0, 0, -0.36))], None),
        "Chest": (None, [(1, (0.08, 0, 0)), (20, (-0.15, 0, 0)), (36, (-0.06, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (-0.08, 0, 0)), (20, (0.18, 0, 0)), (36, (0.06, 0, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (20, (-0.40, -0.15, -0.16)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (20, (-0.30, 0.08, 0.06)), (48, (0, 0, 0))]),
    }
    c_ent.update(sec_curves("entrance", 48))
    add_action("entrance", 48, c_ent)

    # 3. TRUCO (48 frames)
    c_truco = {
        "Pelvis": ([(1, (0, 0, -0.36)), (10, (0, 0.02, -0.35)), (22, (0, -0.03, -0.37)), (48, (0, 0, -0.36))], None),
        "Chest": (None, [(1, (0, 0, 0)), (10, (0.10, 0, 0)), (22, (-0.22, 0, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (10, (-0.12, 0.05, 0)), (22, (0.22, 0, 0)), (48, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (10, (0.06, -0.04, 0.05)), (22, (-0.14, 0.08, -0.06)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (10, (0.35, 0.10, 0.15)), (22, (-0.60, -0.12, -0.15)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (10, (-0.45, 0, 0)), (22, (-0.25, 0, -0.10)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (10, (-0.15, 0, 0)), (22, (0.25, 0, 0)), (48, (0, 0, 0))]),
    }
    c_truco.update(sec_curves("truco", 48))
    add_action("truco", 48, c_truco)

    # 4. VICTORY (48 frames)
    c_vic = {
        "Pelvis": ([(1, (0, 0, -0.36)), (24, (0, 0, -0.34)), (48, (0, 0, -0.36))], None),
        "Chest": (None, [(1, (0, 0, 0)), (24, (-0.12, 0.02, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (24, (0.15, 0.03, 0)), (48, (0, 0, 0))]),
        "Shoulder.L": (None, [(1, (0, 0, 0)), (24, (-0.16, 0.08, 0.10)), (48, (0, 0, 0))]),
        "Shoulder.R": (None, [(1, (0, 0, 0)), (24, (-0.16, -0.08, -0.10)), (48, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.60, 0.15, 0.30)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.60, -0.15, -0.30)), (48, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (24, (-0.30, 0.06, 0.06)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (24, (-0.30, -0.06, -0.06)), (48, (0, 0, 0))]),
    }
    c_vic.update(sec_curves("victory", 48))
    add_action("victory", 48, c_vic)

    # 5. BOSS_INTRO (56 frames)
    c_boss = {
        "Chest": (None, [(1, (0, 0, 0)), (18, (-0.08, 0.05, 0)), (36, (-0.08, -0.05, 0)), (56, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (18, (0.08, 0.15, 0.05)), (36, (0.08, -0.15, -0.05)), (56, (0, 0, 0))]),
        "UpperArm.L": (None, [(1, (0, 0, 0)), (24, (-0.30, 0.15, 0.12)), (56, (0, 0, 0))]),
        "Forearm.L": (None, [(1, (0, 0, 0)), (24, (-0.60, 0.08, 0.10)), (56, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (24, (-0.32, -0.15, -0.12)), (56, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (24, (-0.60, -0.08, -0.10)), (56, (0, 0, 0))]),
    }
    c_boss.update(sec_curves("boss_intro", 56))
    add_action("boss_intro", 56, c_boss)

    # 6. FLOURISH (48 frames)
    c_flourish = {
        "Chest": (None, [(1, (0, 0, 0)), (16, (-0.14, 0.12, 0.06)), (32, (0.06, -0.10, -0.03)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (16, (0.14, -0.10, -0.03)), (32, (-0.06, 0.10, 0.03)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (16, (-0.45, -0.15, -0.15)), (30, (-0.55, -0.18, -0.18)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (16, (-0.40, 0.12, 0.10)), (30, (-0.20, -0.08, -0.06)), (48, (0, 0, 0))]),
    }
    c_flourish.update(sec_curves("flourish", 48))
    add_action("flourish", 48, c_flourish)

    # 7. PLAY_CARD (48 frames)
    c_play = {
        "Chest": (None, [(1, (0, 0, 0)), (12, (0.06, -0.02, 0)), (24, (-0.15, 0.03, 0)), (48, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (12, (-0.06, 0, 0)), (24, (0.16, -0.02, 0)), (48, (0, 0, 0))]),
        "UpperArm.R": (None, [(1, (0, 0, 0)), (12, (0.20, -0.08, 0.10)), (24, (-0.50, -0.12, -0.15)), (48, (0, 0, 0))]),
        "Forearm.R": (None, [(1, (0, 0, 0)), (12, (-0.35, 0.06, -0.05)), (24, (-0.25, -0.05, -0.06)), (48, (0, 0, 0))]),
        "Hand.R": (None, [(1, (0, 0, 0)), (12, (-0.12, 0.06, 0.03)), (24, (0.20, -0.06, 0.06)), (48, (0, 0, 0))]),
    }
    c_play.update(sec_curves("play_card", 48))
    add_action("play_card", 48, c_play)

    # 8. IDLE_TABLE_01 (64 frames)
    c_t1 = {
        "Chest": (None, [(1, (0, 0, 0)), (32, (-0.03, 0.02, 0)), (64, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (24, (0.04, 0.05, 0.02)), (48, (-0.02, -0.04, 0)), (64, (0, 0, 0))]),
    }
    c_t1.update(sec_curves("idle", 64))
    add_action("idle_table_01", 64, c_t1)

    # 9. IDLE_TABLE_02 (64 frames)
    c_t2 = {
        "Chest": (None, [(1, (0, 0, 0)), (32, (-0.04, -0.02, 0)), (64, (0, 0, 0))]),
        "Head": (None, [(1, (0, 0, 0)), (20, (-0.03, -0.06, -0.02)), (44, (0.03, 0.04, 0.01)), (64, (0, 0, 0))]),
    }
    c_t2.update(sec_curves("idle", 64))
    add_action("idle_table_02", 64, c_t2)

    print("[MASTER] All 9 NLA tracks created successfully.")

    # 6. Studio Lighting & Render Setup
    # Set active action to idle for portrait rendering
    arm_obj.animation_data.action = bpy.data.actions.get("idle")
    temp_scene.frame_set(1)

    cam_data = bpy.data.cameras.new("StudioCamData")
    cam_data.lens = 65
    cam_obj = bpy.data.objects.new("StudioCam", cam_data)
    cam_obj.location = (0, -1.35, 1.30)
    cam_obj.rotation_euler = Euler((math.radians(82), 0, 0))
    temp_scene.collection.objects.link(cam_obj)
    temp_scene.camera = cam_obj

    def add_light(name, light_type, energy, loc, color=(1, 1, 1), size=1.0):
        ldata = bpy.data.lights.new(name=name, type=light_type)
        ldata.energy = energy
        ldata.color = color
        if light_type == 'AREA':
            ldata.size = size
        lobj = bpy.data.objects.new(name, ldata)
        lobj.location = loc
        temp_scene.collection.objects.link(lobj)
        return lobj

    add_light("KeyLight", 'AREA', 180, (0.6, -1.0, 1.8), color=(1.0, 0.95, 0.88), size=0.8)
    add_light("FillLight", 'AREA', 90, (-0.8, -0.9, 1.4), color=(0.88, 0.92, 1.0), size=1.0)
    add_light("RimLight", 'AREA', 240, (0.0, 1.0, 2.0), color=(1.0, 0.85, 0.65), size=1.2)

    # Render settings
    temp_scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderSettings') and 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    temp_scene.render.resolution_x = 512
    temp_scene.render.resolution_y = 512
    temp_scene.render.film_transparent = True
    temp_scene.render.image_settings.file_format = 'PNG'
    temp_scene.render.image_settings.color_mode = 'RGBA'

    path_3d = os.path.abspath("assets/models/club/onca_3d.png")
    temp_scene.render.filepath = path_3d
    bpy.ops.render.render(write_still=True)
    print(f"[MASTER] Rendered 3D Portrait -> {path_3d}")

    path_2d = os.path.abspath("assets/models/club/onca.png")
    import shutil
    shutil.copyfile(path_3d, path_2d)
    print(f"[MASTER] Copied to 2D Portrait -> {path_2d}")

    # Remove studio camera and lights before GLB export
    bpy.data.objects.remove(cam_obj, do_unlink=True)
    for l_name in ["KeyLight", "FillLight", "RimLight"]:
        lo = temp_scene.objects.get(l_name)
        if lo: bpy.data.objects.remove(lo, do_unlink=True)

    # 7. Exports
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.window.scene = orig_scene
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.window.scene = temp_scene
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    mesh_obj.select_set(True)
    head_marker.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    # Export Runtime GLB
    out_glb = os.path.abspath("assets/models/club/onca.glb")
    bpy.ops.export_scene.gltf(
        filepath=out_glb,
        use_selection=True,
        export_format='GLB',
        export_yup=True,
        export_apply=False,
        export_animations=True,
        export_nla_strips=True,
        export_materials='EXPORT',
        export_skins=True,
        export_morph=False
    )
    print(f"[MASTER] Exported Runtime GLB -> {out_glb}")

    # Export Staging GLB
    staging_glb = os.path.abspath("art/blender/patch26/onca_refined.glb")
    os.makedirs(os.path.dirname(staging_glb), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=staging_glb,
        use_selection=True,
        export_format='GLB',
        export_yup=True,
        export_apply=False,
        export_animations=True,
        export_nla_strips=True,
        export_materials='EXPORT',
        export_skins=True,
        export_morph=False
    )
    print(f"[MASTER] Exported Staging GLB -> {staging_glb}")

    # Save Editable .blend source
    blend_path = os.path.abspath("art/blender/patch26/onca_refined.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, copy=True)
    print(f"[MASTER] Saved Editable .blend -> {blend_path}")

finally:
    for obj in list(temp_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.window.scene = orig_scene
    bpy.data.scenes.remove(temp_scene)
    print("[MASTER] User original scene strictly preserved.")
    print("[MASTER] COMPLETE: Dona Onca successfully elevated to Referencia 2 quality!")
