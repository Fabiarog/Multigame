import bpy
import math
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

# Face direction test
body = bpy.data.objects.get("Body")
face = bpy.data.objects.get("Face")
hair = bpy.data.objects.get("Hair")

# Rotate by 180 degrees around Z
for m in (body, face, hair):
    m.rotation_euler.z += math.pi

bpy.context.view_layer.update()

for m in (body, face, hair):
    mw = m.matrix_world.copy()
    m.parent = None
    m.matrix_world = mw
    bpy.ops.object.select_all(action='DESELECT')
    m.select_set(True)
    bpy.context.view_layer.objects.active = m
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Delete head vertices of Body (Z > 1.38 in Body)
bpy.context.view_layer.objects.active = body
bpy.ops.object.mode_set(mode='EDIT')
import bmesh
bm = bmesh.from_edit_mesh(body.data)
del_verts = [v for v in bm.verts if v.co.z > 1.38]
print(f"Deleting {len(del_verts)} head mannequin vertices from Body")
bmesh.ops.delete(bm, geom=del_verts, context='VERTS')
bmesh.update_edit_mesh(body.data)
bpy.ops.object.mode_set(mode='OBJECT')

# Join meshes
bpy.ops.object.select_all(action='DESELECT')
for m in (body, face, hair):
    m.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
mesh_obj = body

# Check bounds and materials
print(f"Unified mesh verts: {len(mesh_obj.data.vertices)}")
print("Materials after join:")
for i, mat in enumerate(mesh_obj.data.materials):
    print(f"  Mat {i}: {mat.name}")
    # Check if this mat has image texture
    if mat.node_tree:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE':
                print(f"    Texture: {node.image.name if node.image else None}")

# Check eye iris normal
iris_polys = [p for p in mesh_obj.data.polygons if "eyeiris" in mesh_obj.data.materials[p.material_index].name.lower()]
if iris_polys:
    avg_n = sum((p.normal for p in iris_polys), Vector((0,0,0))) / len(iris_polys)
    print(f"Eye Iris normal after 180° rotation: {avg_n}")
