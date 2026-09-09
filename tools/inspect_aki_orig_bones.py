import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
print(f"Armature {arm.name} has {len(arm.data.bones)} bones:")
for b in arm.data.bones:
    if "arm" in b.name.lower() or "hand" in b.name.lower() or "head" in b.name.lower():
        print(f"  {b.name}: head={b.head_local}, tail={b.tail_local}")
