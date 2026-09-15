import bpy
import math

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

body = bpy.data.objects.get("Body")
face = bpy.data.objects.get("Face")

print("Before rot - Body rot:", body.rotation_euler, "Face rot:", face.rotation_euler)
for m in (body, face):
    m.rotation_euler.z += math.pi

mw_without_update = body.matrix_world.to_euler()
print("Body matrix_world rotation WITHOUT update:", mw_without_update)

bpy.context.view_layer.update()
mw_with_update = body.matrix_world.to_euler()
print("Body matrix_world rotation WITH update:", mw_with_update)
