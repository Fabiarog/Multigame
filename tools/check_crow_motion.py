import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

rig = bpy.data.objects.get("rig")
scene = bpy.context.scene

for f in range(1, 183, 15):
    scene.frame_set(f)
    w_l = rig.pose.bones.get("Wing.L")
    hd = rig.pose.bones.get("head")
    w_rot = w_l.rotation_quaternion if w_l else None
    h_rot = hd.rotation_quaternion if hd else None
    print(f"Frame {f:3d}: Wing.L quat=({w_rot.w:.2f}, {w_rot.x:.2f}, {w_rot.y:.2f}, {w_rot.z:.2f}), Head quat=({h_rot.w:.2f}, {h_rot.x:.2f}, {h_rot.y:.2f}, {h_rot.z:.2f})")
