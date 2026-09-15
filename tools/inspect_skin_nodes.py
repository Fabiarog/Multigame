import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

body = bpy.data.objects.get("Body")
mat = body.data.materials[0]
print("Mat name:", mat.name)
if mat.node_tree:
    for n in mat.node_tree.nodes:
        print(f"  Node: {n.name} ({n.type})")
        if n.type == 'TEX_IMAGE':
            print(f"    Image: {n.image}")
