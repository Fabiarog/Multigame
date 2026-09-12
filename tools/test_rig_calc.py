import bpy
import mathutils
import os

bf = r'c:\workspace\multigame\assets\modelos 3d detalhados\Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend'
bpy.ops.wm.open_mainfile(filepath=bf)

mesh_obj = None
for o in bpy.data.objects:
    if o.type == 'MESH':
        mesh_obj = o
        break

print('Mesh found:', mesh_obj.name, 'verts:', len(mesh_obj.data.vertices))

# Get bounding box in world space
bbox = [mesh_obj.matrix_world @ mathutils.Vector(b) for b in mesh_obj.bound_box]
min_x = min(b.x for b in bbox)
max_x = max(b.x for b in bbox)
min_y = min(b.y for b in bbox)
max_y = max(b.y for b in bbox)
min_z = min(b.z for b in bbox)
max_z = max(b.z for b in bbox)

height = max_z - min_z
print(f'BBox: X[{min_x:.3f}, {max_x:.3f}], Y[{min_y:.3f}, {max_y:.3f}], Z[{min_z:.3f}, {max_z:.3f}], Height: {height:.3f}')
