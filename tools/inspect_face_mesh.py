import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

face = bpy.data.objects.get("Face")
print("Face materials:")
for m in face.data.materials:
    print(" ", m.name if m else None)

body = bpy.data.objects.get("Body")
head_body_verts = [v for v in body.data.vertices if v.co.z > 1.38]
print(f"Body vertices above 1.38: {len(head_body_verts)} out of {len(body.data.vertices)}")
