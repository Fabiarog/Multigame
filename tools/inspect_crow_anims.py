import bpy

print("=== INSPECTING crowrigconjay.blend ===")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

for act in bpy.data.actions:
    print(f"Action in blend: {act.name}, frames: {act.frame_range}")

for obj in bpy.data.objects:
    if obj.type in ('ARMATURE', 'MESH'):
        print(f"Object: {obj.name} ({obj.type}), loc={obj.location}, rot={obj.rotation_euler}")

print("\n=== INSPECTING mascot_crow.glb ===")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/mascot_crow.glb")
for act in bpy.data.actions:
    print(f"Action in glb: {act.name}, frames: {act.frame_range}")

for obj in bpy.data.objects:
    print(f"GLB Object: {obj.name} ({obj.type}), loc={obj.location}, rot={obj.rotation_euler}, scale={obj.scale}")
