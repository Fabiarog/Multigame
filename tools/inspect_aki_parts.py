import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

body = bpy.data.objects.get("Body")
face = bpy.data.objects.get("Face")
hair = bpy.data.objects.get("Hair")

print("Body loc:", body.location, "parent:", body.parent.name)
print("Face loc:", face.location, "parent:", face.parent.name)
print("Hair loc:", hair.location, "parent:", hair.parent.name)

# Bounding box of each in world space
for obj in (body, face, hair):
    bbox = [obj.matrix_world @ Vector(b) for b in obj.bound_box]
    min_z = min(b.z for b in bbox)
    max_z = max(b.z for b in bbox)
    min_y = min(b.y for b in bbox)
    max_y = max(b.y for b in bbox)
    min_x = min(b.x for b in bbox)
    max_x = max(b.x for b in bbox)
    print(f"{obj.name}: X[{min_x:.3f}, {max_x:.3f}], Y[{min_y:.3f}, {max_y:.3f}], Z[{min_z:.3f}, {max_z:.3f}]")
