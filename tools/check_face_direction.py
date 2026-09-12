import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

# Find head or nose or face vertices
face_mesh = bpy.data.objects.get("Face")
body_mesh = bpy.data.objects.get("Body")
print("Face loc:", face_mesh.matrix_world.translation)
verts = [face_mesh.matrix_world @ v.co for v in face_mesh.data.vertices]
avg_y = sum(v.y for v in verts) / len(verts)
print("Face avg Y:", avg_y)

arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
if arm:
    head_bone = arm.data.bones.get("J_Bip_C_Head")
    if head_bone:
        print("Aki original head bone head:", head_bone.head_local, "tail:", head_bone.tail_local)

# Compare with corvo or bento
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/corvo.glb")
corvo_head = next((o for o in bpy.data.objects if "Head" in o.name), None)
if corvo_head:
    print("Corvo head translation:", corvo_head.matrix_world.translation)
