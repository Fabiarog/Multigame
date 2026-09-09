import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

# Remove WGT widgets, metarig, etc.
for o in list(bpy.data.objects):
    if o.name.startswith("WGT") or o.name in ("metarig", "Cube", "Cube.001", "Icosphere", "Sphere"):
        bpy.data.objects.remove(o, do_unlink=True)

rig = bpy.data.objects.get("rig")
print("Remaining objects:", [o.name for o in bpy.data.objects])

# Scale and position
rig.scale = Vector((0.075, 0.075, 0.075))
# Feet at frame 120 originally at Z = -10.77 * 0.075 in local rig space.
# If rig.location.z is adjusted:
bpy.context.scene.frame_set(120)
bpy.context.view_layer.update()
foot_l = rig.pose.bones.get("foot_ik.L") or rig.pose.bones.get("foot_fk.L")
# Head of foot bone in world space
current_foot_z = (rig.matrix_world @ foot_l.head).z
print(f"Current foot world Z at frame 120 (with loc=0): {current_foot_z:.4f}")
# Offset rig so foot is exactly at Z = 0.0
rig.location.z = -current_foot_z
bpy.context.view_layer.update()
new_foot_z = (rig.matrix_world @ foot_l.head).z
print(f"New foot world Z after offset: {new_foot_z:.4f}")
