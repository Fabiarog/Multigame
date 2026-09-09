import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

body = bpy.data.objects.get("Body")
mat = body.data.materials[0]
for n in mat.node_tree.nodes:
    if n.type == 'NORMAL_MAP':
        print("Normal Map inputs:", [i.name for i in n.inputs if i.is_linked])
        for i in n.inputs:
            if i.is_linked:
                for l in i.links:
                    print("  linked from:", l.from_node.name, l.from_node.type)
                    if l.from_node.type == 'TEX_IMAGE':
                        print("  image:", l.from_node.image.name, l.from_node.image.filepath)
