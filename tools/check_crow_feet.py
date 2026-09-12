import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/mascot_crow.glb")

rig = bpy.data.objects.get("rig")
scene = bpy.context.scene

# Check feet position at frame 120
for f in [1, 50, 106, 120, 150, 180]:
    scene.frame_set(f)
    foot_l = rig.pose.bones.get("foot_ik.L") or rig.pose.bones.get("foot_fk.L")
    foot_r = rig.pose.bones.get("foot_ik.R") or rig.pose.bones.get("foot_fk.R")
    if foot_l:
        fl_world = rig.matrix_world @ foot_l.head
        fr_world = rig.matrix_world @ foot_r.head
        print(f"Frame {f:3d}: Foot.L={fl_world}, Foot.R={fr_world}")
