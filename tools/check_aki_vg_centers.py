import bpy
import math
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
for m in mesh_objs:
    m.rotation_euler.z += math.pi
    mw = m.matrix_world.copy()
    m.parent = None
    m.matrix_world = mw

# Clean non-mesh
for o in list(bpy.data.objects):
    if o.type != 'MESH':
        bpy.data.objects.remove(o, do_unlink=True)

# Join
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objs:
    m.select_set(True)
bpy.context.view_layer.objects.active = mesh_objs[0]
bpy.ops.object.join()
mesh_obj = mesh_objs[0]

# Ground
bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
min_z = min(b.z for b in bbox)
mesh_obj.location.z -= min_z
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

# Scale
bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
height = max(b.z for b in bbox)
scale_factor = 1.85 / max(0.1, height)
mesh_obj.scale = (scale_factor, scale_factor, scale_factor)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Check bounds of arms and head
for vg in mesh_obj.vertex_groups:
    if any(k in vg.name.lower() for k in ["upperarm", "lowerarm", "hand", "head"]):
        verts = [mesh_obj.matrix_world @ v.co for v in mesh_obj.data.vertices for g in v.groups if g.group == vg.index and g.weight > 0.5]
        if verts:
            avg_x = sum(v.x for v in verts) / len(verts)
            avg_y = sum(v.y for v in verts) / len(verts)
            avg_z = sum(v.z for v in verts) / len(verts)
            print(f"VG {vg.name}: {len(verts)} verts, center=({avg_x:.3f}, {avg_y:.3f}, {avg_z:.3f})")
