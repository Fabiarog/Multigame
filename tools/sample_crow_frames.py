import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

rig = bpy.data.objects.get("rig")
scene = bpy.context.scene

print("Checking rig poses across frames:")
for f in [1, 30, 60, 90, 120, 150, 182]:
    scene.frame_set(f)
    root_b = rig.pose.bones.get("root") or rig.pose.bones.get("spine")
    wing_l = rig.pose.bones.get("wing_ik.L") or rig.pose.bones.get("upperarm.L") or rig.pose.bones.get("Wing.L")
    head_b = rig.pose.bones.get("head")
    print(f"Frame {f}:")
    if root_b:
        print(f"  root loc: {root_b.location}, rot: {root_b.rotation_euler}")
    if wing_l:
        print(f"  wing loc: {wing_l.location}, rot: {wing_l.rotation_euler}")
    if head_b:
        print(f"  head loc: {head_b.location}, rot: {head_b.rotation_euler}")
