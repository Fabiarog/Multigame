import bpy
import math
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

# Scale factor calculation
mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
# Rotate 180
for m in mesh_objs:
    m.rotation_euler.z += math.pi
bpy.context.view_layer.update()

for m in mesh_objs:
    mw = m.matrix_world.copy()
    m.parent = None
    m.matrix_world = mw
    bpy.ops.object.select_all(action='DESELECT')
    m.select_set(True)
    bpy.context.view_layer.objects.active = m
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

for o in list(bpy.data.objects):
    if o.type != 'MESH':
        bpy.data.objects.remove(o, do_unlink=True)

bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objs:
    m.select_set(True)
bpy.context.view_layer.objects.active = mesh_objs[0]
bpy.ops.object.join()
mesh_obj = mesh_objs[0]

bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
min_z = min(b.z for b in bbox)
mesh_obj.location.z -= min_z
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
height = max(b.z for b in bbox)
scale_factor = 1.85 / max(0.1, height)
mesh_obj.scale = (scale_factor, scale_factor, scale_factor)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Now check vertex group centers
for vg_name in [
    "J_Bip_C_Hips", "J_Bip_C_Spine", "J_Bip_C_Chest", "J_Bip_C_UpperChest", "J_Bip_C_Neck", "J_Bip_C_Head",
    "J_Bip_L_Shoulder", "J_Bip_L_UpperArm", "J_Bip_L_LowerArm", "J_Bip_L_Hand",
    "J_Bip_R_Shoulder", "J_Bip_R_UpperArm", "J_Bip_R_LowerArm", "J_Bip_R_Hand",
    "J_Bip_L_UpperLeg", "J_Bip_L_LowerLeg", "J_Bip_L_Foot",
    "J_Bip_R_UpperLeg", "J_Bip_R_LowerLeg", "J_Bip_R_Foot"
]:
    vg = mesh_obj.vertex_groups.get(vg_name)
    if vg:
        verts = [v.co for v in mesh_obj.data.vertices for g in v.groups if g.group == vg.index and g.weight > 0.3]
        if verts:
            avg_co = sum(verts, Vector((0,0,0))) / len(verts)
            print(f"  {vg_name:20s}: ({avg_co.x:+.3f}, {avg_co.y:+.3f}, {avg_co.z:+.3f})")
