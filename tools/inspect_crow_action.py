import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

rig = bpy.data.objects.get("rig")
print("rig animation data:", rig.animation_data)
if rig.animation_data and rig.animation_data.action:
    act = rig.animation_data.action
    print(f"Action: {act.name}, range: {act.frame_range}")
    for fc in act.fcurves[:10]:
        print(f"  fcurve: {fc.data_path}[{fc.array_index}], points: {len(fc.keyframe_points)}")

for act in bpy.data.actions:
    print(f"Action in blend file: {act.name}")
