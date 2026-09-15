import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

# Remove WGT widgets, metarig, etc.
for o in list(bpy.data.objects):
    if o.name.startswith("WGT") or o.name in ("metarig", "Cube", "Cube.001", "Icosphere", "Sphere"):
        bpy.data.objects.remove(o, do_unlink=True)

rig = bpy.data.objects.get("rig")
rig.scale = Vector((0.075, 0.075, 0.075))
bpy.context.scene.frame_set(120)
bpy.context.view_layer.update()
foot_l = rig.pose.bones.get("foot_ik.L") or rig.pose.bones.get("foot_fk.L")
rig.location.z = -(rig.matrix_world @ foot_l.head).z
bpy.context.view_layer.update()

# Original action
orig_act = rig.animation_data.action

# Create 'landing' action (frames 1 to 105)
act_landing = orig_act.copy()
act_landing.name = "landing"

# Create 'idle' action (frames 106 to 182 shifted to 1 to 77)
act_idle = bpy.data.actions.new(name="idle")

# Bake or sample the pose from frame 106 to 182 into act_idle
rig.animation_data.action = act_idle
pose_bones = list(rig.pose.bones)

for f_src in range(106, 183):
    f_dst = f_src - 105
    bpy.context.scene.frame_set(f_src)
    rig.animation_data.action = orig_act
    bpy.context.view_layer.update()
    # Read transforms
    transforms = []
    for pb in pose_bones:
        transforms.append((pb, pb.location.copy(), pb.rotation_quaternion.copy() if pb.rotation_mode == 'QUATERNION' else pb.rotation_euler.copy()))
    
    rig.animation_data.action = act_idle
    for pb, loc, rot in transforms:
        pb.location = loc
        pb.keyframe_insert(data_path="location", frame=f_dst)
        if pb.rotation_mode == 'QUATERNION':
            pb.rotation_quaternion = rot
            pb.keyframe_insert(data_path="rotation_quaternion", frame=f_dst)
        else:
            pb.rotation_euler = rot
            pb.keyframe_insert(data_path="rotation_euler", frame=f_dst)

print(f"Landing action created: {act_landing.name}")
print(f"Idle action created: {act_idle.name}, frames: 1 to 77")

# Add both to NLA tracks so GLTF exporter exports both animations
for track in list(rig.animation_data.nla_tracks):
    rig.animation_data.nla_tracks.remove(track)
track_landing = rig.animation_data.nla_tracks.new()
track_landing.name = "landing"
strip_landing = track_landing.strips.new("landing", 1, act_landing)
strip_landing.action_frame_start = 1
strip_landing.action_frame_end = 105

track_idle = rig.animation_data.nla_tracks.new()
track_idle.name = "idle"
strip_idle = track_idle.strips.new("idle", 1, act_idle)
strip_idle.action_frame_start = 1
strip_idle.action_frame_end = 77

# Export test
out_test = "temp/test_crow_split.glb"
bpy.ops.export_scene.gltf(
    filepath=out_test,
    export_format='GLB',
    export_animations=True
)
print(f"Exported test crow GLB: {out_test}")
