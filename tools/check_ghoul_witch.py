import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/ghoul_ue5.fbx")
head_bone = next((b for b in bpy.data.armatures[0].bones if "head" in b.name.lower()), None)
print("Ghoul head bone head:", head_bone.head_local, "tail:", head_bone.tail_local if head_bone else None)
claws = bpy.data.objects.get("Claws")
if claws:
    avg_y = sum((claws.matrix_world @ v.co).y for v in claws.data.vertices) / len(claws.data.vertices)
    print("Ghoul claws avg Y:", avg_y)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/source/Witch_skeletal_mesh.fbx")
head_bone = next((b for b in bpy.data.armatures[0].bones if "head" in b.name.lower()), None)
print("Witch head bone head:", head_bone.head_local, "tail:", head_bone.tail_local if head_bone else None)
print("Witch armature rot:", bpy.data.objects.get("Armature").rotation_euler)
