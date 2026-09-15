"""Author Corvo's 14 pilot animations cleanly in Blender via MCP.

Preserves all 39 existing animations, maintains identical armature (23 bones),
materials, and head marker. Adds new high-impact clips:
idle_relaxed, idle_nervous, nod, shake_head, lean_forward, lean_back,
win_trick, lose_trick, lose_hand, seat_adjust, micro_glance_left,
micro_glance_right, micro_sigh, micro_finger_tap.
"""
import bpy
import math
from mathutils import Vector, Quaternion

def build_pilot_animations():
    orig_scene = bpy.context.window.scene
    temp_scene = bpy.data.scenes.new("CorvoPilotScene")
    bpy.context.window.scene = temp_scene

    try:
        # Import Corvo refined
        source_glb = "c:/workspace/multigame/art/blender/patch26/corvo_refined.glb"
        bpy.ops.import_scene.gltf(filepath=source_glb)

        arm = None
        mesh = None
        head_marker = None
        for o in temp_scene.objects:
            if o.type == 'ARMATURE':
                arm = o
            elif o.type == 'MESH':
                mesh = o
            elif o.type == 'EMPTY' and 'Head' in o.name:
                head_marker = o

        assert arm is not None, "Corvo Armature not found"
        print(f"Found Armature: {arm.name}, Mesh: {mesh.name if mesh else 'None'}, Head: {head_marker.name if head_marker else 'None'}")

        # Record rest transforms for all pose bones
        # Set to idle frame 0 first to grab the canonical seated rest pose
        for t in arm.animation_data.nla_tracks:
            t.mute = (t.name != 'idle')
        temp_scene.frame_set(0)
        bpy.context.view_layer.update()

        rest_poses = {}
        for pb in arm.pose.bones:
            rest_poses[pb.name] = {
                'loc': pb.location.copy(),
                'rot': pb.rotation_quaternion.copy(),
                'scale': pb.scale.copy()
            }

        # Helper to insert keyframe with relative transform
        def set_bone_key(pb, f, d_loc=None, d_rot_axis=None, d_rot_angle=0.0):
            base_loc = rest_poses[pb.name]['loc']
            base_rot = rest_poses[pb.name]['rot']

            if d_loc is not None:
                pb.location = base_loc + Vector(d_loc)
            else:
                pb.location = base_loc.copy()
            pb.keyframe_insert('location', frame=f)

            if d_rot_axis is not None and abs(d_rot_angle) > 1e-6:
                delta_q = Quaternion(d_rot_axis, d_rot_angle)
                pb.rotation_quaternion = base_rot @ delta_q
            else:
                pb.rotation_quaternion = base_rot.copy()
            pb.keyframe_insert('rotation_quaternion', frame=f)

        def set_all_rest(f):
            for pb in arm.pose.bones:
                pb.location = rest_poses[pb.name]['loc'].copy()
                pb.rotation_quaternion = rest_poses[pb.name]['rot'].copy()
                pb.keyframe_insert('location', frame=f)
                pb.keyframe_insert('rotation_quaternion', frame=f)

        new_clips = {}

        # ─────────────────────────────────────────────────────────────
        # 1. idle_relaxed (120 frames / 4.0s loop)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="idle_relaxed")
        arm.animation_data.action = act
        total_f = 120
        for f in range(total_f + 1):
            t = f / total_f
            # Subtle slow breathing
            breath = math.sin(2 * math.pi * t)
            # Gentle recline
            recline = -0.02 + 0.008 * breath
            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=recline)
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=0.015 * breath)
            # Observant micro sway
            head_sway = 0.018 * math.sin(2 * math.pi * t)
            head_tilt = 0.012 * math.sin(4 * math.pi * t)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=head_sway)
            # Wing ruffle in the middle
            if 0.30 <= t <= 0.70:
                wt = (t - 0.30) / 0.40
                ruffle = math.sin(2 * math.pi * wt) * 0.035
                set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=ruffle)
                set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-ruffle)
            else:
                set_bone_key(arm.pose.bones['Wing.L'], f)
                set_bone_key(arm.pose.bones['Wing.R'], f)
        new_clips['idle_relaxed'] = (act, total_f, True)

        # ─────────────────────────────────────────────────────────────
        # 2. idle_nervous (90 frames / 3.0s loop)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="idle_nervous")
        arm.animation_data.action = act
        total_f = 90
        for f in range(total_f + 1):
            t = f / total_f
            # Rapid shallow breathing (3 cycles)
            breath = math.sin(6 * math.pi * t)
            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=0.025 + 0.008 * breath)
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=0.022 * breath)
            # Tucked wings
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-0.04)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=0.04)
            # Bird-like darting head turns
            head_z = 0.0
            head_x = 0.0
            if 0.15 <= t <= 0.38:
                ht = (t - 0.15) / 0.23
                head_z = 0.12 * math.sin(math.pi * ht)
                head_x = -0.04 * math.sin(math.pi * ht)
            elif 0.50 <= t <= 0.75:
                ht = (t - 0.50) / 0.25
                head_z = -0.14 * math.sin(math.pi * ht)
                head_x = 0.03 * math.sin(math.pi * ht)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=head_z)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(1, 0, 0), d_rot_angle=head_x)
        new_clips['idle_nervous'] = (act, total_f, True)

        # ─────────────────────────────────────────────────────────────
        # 3. nod (24 frames / 0.8s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="nod")
        arm.animation_data.action = act
        total_f = 24
        for f in range(total_f + 1):
            t = f / total_f
            if t < 0.2: # Anticipation up
                a = t / 0.2
                angle_h = 0.03 * math.sin(math.pi * a)
                angle_n = 0.015 * math.sin(math.pi * a)
            elif t < 0.55: # Crisp dip down
                a = (t - 0.2) / 0.35
                angle_h = -0.16 * math.sin(math.pi * a)
                angle_n = -0.07 * math.sin(math.pi * a)
            elif t < 0.80: # Rebound
                a = (t - 0.55) / 0.25
                angle_h = 0.04 * math.sin(math.pi * a)
                angle_n = 0.02 * math.sin(math.pi * a)
            else: # Settle
                angle_h = 0.0
                angle_n = 0.0
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=angle_h)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(1, 0, 0), d_rot_angle=angle_n)
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=angle_n * 0.3)
        new_clips['nod'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 4. shake_head (24 frames / 0.8s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="shake_head")
        arm.animation_data.action = act
        total_f = 24
        for f in range(total_f + 1):
            t = f / total_f
            # Left-right oscillation with dampening envelope
            env = math.sin(math.pi * t)
            osc = math.sin(3.5 * math.pi * t)
            angle_z = 0.16 * env * osc
            angle_y = 0.04 * env * osc
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=angle_z)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(0, 1, 0), d_rot_angle=angle_y)
        new_clips['shake_head'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 5. lean_forward (32 frames / 1.07s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="lean_forward")
        arm.animation_data.action = act
        total_f = 32
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t) ** 1.5
            d_y = 0.04 * env
            d_spine = 0.11 * env
            d_chest = 0.06 * env
            # Gaze compensation: head looks up so eyes stay focused forward
            d_head = -0.14 * env
            set_bone_key(arm.pose.bones['Pelvis'], f, d_loc=(0, d_y, 0))
            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=d_spine)
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=d_chest)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=d_head)
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=0.06 * env)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-0.06 * env)
        new_clips['lean_forward'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 6. lean_back (32 frames / 1.07s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="lean_back")
        arm.animation_data.action = act
        total_f = 32
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t) ** 1.5
            d_y = -0.025 * env
            d_spine = -0.09 * env
            d_head = 0.05 * env # Chin tucks down calculatingly
            set_bone_key(arm.pose.bones['Pelvis'], f, d_loc=(0, d_y, 0))
            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=d_spine)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=d_head)
        new_clips['lean_back'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 7. win_trick (36 frames / 1.2s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="win_trick")
        arm.animation_data.action = act
        total_f = 36
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t)
            # Chest puff
            chest_puff = 0.07 * (env ** 1.3)
            # Wing flick
            wing_flick = 0.14 * (math.sin(math.pi * min(t * 1.6, 1.0)) ** 2) if t < 0.65 else 0.0
            # Satisfied nod in the middle
            nod = -0.09 * math.sin(math.pi * ((t - 0.4) / 0.4)) if 0.4 <= t <= 0.8 else 0.0
            head_lift = 0.06 * env + nod

            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=chest_puff)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=head_lift)
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=wing_flick)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-wing_flick)
            # Right hand subtle gesture
            r_hand = 0.08 * env
            set_bone_key(arm.pose.bones['Hand.R'], f, d_rot_axis=(1, 0, 0), d_rot_angle=r_hand)
        new_clips['win_trick'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 8. lose_trick (36 frames / 1.2s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="lose_trick")
        arm.animation_data.action = act
        total_f = 36
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t)
            # Shoulders sag, head dips
            head_dip = -0.11 * (env ** 1.2)
            chest_sag = -0.05 * (env ** 1.2)
            # Small annoyed head shake
            shake = 0.06 * math.sin(3 * math.pi * t) if 0.25 <= t <= 0.75 else 0.0

            set_bone_key(arm.pose.bones['Pelvis'], f, d_loc=(0, 0, -0.015 * env))
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=chest_sag)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=head_dip)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(0, 0, 1), d_rot_angle=shake)
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(1, 0, 0), d_rot_angle=-0.04 * env)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(1, 0, 0), d_rot_angle=-0.04 * env)
        new_clips['lose_trick'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 9. lose_hand (48 frames / 1.6s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="lose_hand")
        arm.animation_data.action = act
        total_f = 48
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t)
            recoil = -0.08 * (env ** 1.5)
            turn_away = 0.22 * (env ** 1.2)
            wing_clench = -0.06 * env
            flutter = 0.04 * math.sin(8 * math.pi * t) if 0.2 <= t <= 0.55 else 0.0

            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=recoil)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=turn_away)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(1, 0, 0), d_rot_angle=-0.06 * env)
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=wing_clench + flutter)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-wing_clench - flutter)
        new_clips['lose_hand'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 10. seat_adjust (36 frames / 1.2s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="seat_adjust")
        arm.animation_data.action = act
        total_f = 36
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t)
            pelvis_z = 0.02 * env
            spine_up = 0.05 * env
            shrug = 0.045 * env
            set_bone_key(arm.pose.bones['Pelvis'], f, d_loc=(0, 0, pelvis_z))
            set_bone_key(arm.pose.bones['Spine'], f, d_rot_axis=(1, 0, 0), d_rot_angle=spine_up)
            set_bone_key(arm.pose.bones['Shoulder.L'], f, d_rot_axis=(0, 0, 1), d_rot_angle=shrug)
            set_bone_key(arm.pose.bones['Shoulder.R'], f, d_rot_axis=(0, 0, 1), d_rot_angle=-shrug)
            set_bone_key(arm.pose.bones['Wing.L'], f, d_rot_axis=(0, 1, 0), d_rot_angle=0.03 * env)
            set_bone_key(arm.pose.bones['Wing.R'], f, d_rot_axis=(0, 1, 0), d_rot_angle=-0.03 * env)
        new_clips['seat_adjust'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 11. micro_glance_left (18 frames / 0.6s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="micro_glance_left")
        arm.animation_data.action = act
        total_f = 18
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t) ** 1.3
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=0.22 * env)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(0, 0, 1), d_rot_angle=0.07 * env)
        new_clips['micro_glance_left'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 12. micro_glance_right (18 frames / 0.6s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="micro_glance_right")
        arm.animation_data.action = act
        total_f = 18
        for f in range(total_f + 1):
            t = f / total_f
            env = math.sin(math.pi * t) ** 1.3
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(0, 0, 1), d_rot_angle=-0.22 * env)
            set_bone_key(arm.pose.bones['Neck'], f, d_rot_axis=(0, 0, 1), d_rot_angle=-0.07 * env)
        new_clips['micro_glance_right'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 13. micro_sigh (27 frames / 0.9s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="micro_sigh")
        arm.animation_data.action = act
        total_f = 27
        for f in range(total_f + 1):
            t = f / total_f
            # Inhale first then exhale drop
            if t < 0.35:
                inhale = math.sin(math.pi * (t / 0.35))
                chest_a = 0.03 * inhale
                head_a = 0.015 * inhale
            else:
                exhale = math.sin(math.pi * ((t - 0.35) / 0.65))
                chest_a = -0.045 * exhale
                head_a = -0.05 * exhale
            set_bone_key(arm.pose.bones['Chest'], f, d_rot_axis=(1, 0, 0), d_rot_angle=chest_a)
            set_bone_key(arm.pose.bones['Head'], f, d_rot_axis=(1, 0, 0), d_rot_angle=head_a)
        new_clips['micro_sigh'] = (act, total_f, False)

        # ─────────────────────────────────────────────────────────────
        # 14. micro_finger_tap (24 frames / 0.8s)
        # ─────────────────────────────────────────────────────────────
        act = bpy.data.actions.new(name="micro_finger_tap")
        arm.animation_data.action = act
        total_f = 24
        for f in range(total_f + 1):
            t = f / total_f
            tap = 0.0
            if t < 0.35:
                tap = 0.08 * math.sin(2 * math.pi * (t / 0.35))
            elif 0.35 <= t <= 0.70:
                tap = 0.07 * math.sin(2 * math.pi * ((t - 0.35) / 0.35))
            set_bone_key(arm.pose.bones['Hand.R'], f, d_rot_axis=(1, 0, 0), d_rot_angle=tap)
            set_bone_key(arm.pose.bones['Forearm.R'], f, d_rot_axis=(1, 0, 0), d_rot_angle=tap * 0.25)
        new_clips['micro_finger_tap'] = (act, total_f, False)

        # Add all new actions as NLA tracks on the armature
        arm.animation_data.action = None
        for name, (act_obj, frame_len, is_loop) in new_clips.items():
            # Remove any existing track with this name if present
            existing = next((t for t in arm.animation_data.nla_tracks if t.name == name), None)
            if existing:
                arm.animation_data.nla_tracks.remove(existing)
            track = arm.animation_data.nla_tracks.new()
            track.name = name
            strip = track.strips.new(name, 0, act_obj)
            strip.extrapolation = 'HOLD' if is_loop else 'NOTHING'

        print(f"Total NLA tracks after addition: {len(arm.animation_data.nla_tracks)}")

        # Clean selection for export
        orig_scene = bpy.data.scenes.get('Scene') or bpy.context.window.scene
        bpy.ops.object.select_all(action='DESELECT')
        
        # Deselect in all scenes
        for s in bpy.data.scenes:
            for obj in s.objects:
                obj.select_set(False)

        bpy.context.window.scene = temp_scene
        arm.select_set(True)
        if mesh: mesh.select_set(True)
        if head_marker: head_marker.select_set(True)
        bpy.context.view_layer.objects.active = arm

        # Export candidate to patch26/corvo_refined_pilot.glb
        candidate_glb = "c:/workspace/multigame/art/blender/patch26/corvo_refined_pilot.glb"
        bpy.ops.export_scene.gltf(
            filepath=candidate_glb,
            export_format='GLB',
            use_selection=True,
            use_active_scene=True,
            export_animations=True,
            export_animation_mode='NLA_TRACKS',
            export_force_sampling=True,
            export_frame_range=False,
            export_def_bones=True
        )
        print(f"Exported pilot candidate GLB: {candidate_glb}")

        # Also save compressed blend file
        candidate_blend = "c:/workspace/multigame/art/blender/patch26/corvo_refined_pilot.blend"
        bpy.data.libraries.write(candidate_blend, {temp_scene}, fake_user=True, compress=True)
        print(f"Saved candidate blend: {candidate_blend}")

    finally:
        bpy.context.window.scene = orig_scene
        for o in list(temp_scene.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.scenes.remove(temp_scene)

if __name__ == "__main__":
    build_pilot_animations()
