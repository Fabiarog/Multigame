import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/aki.glb")

for obj in bpy.data.objects:
    print(f"{obj.type}: {obj.name}, parent={obj.parent.name if obj.parent else None}, loc={obj.location}, scale={obj.scale}")
    if obj.type == "MESH":
        print(f"  verts: {len(obj.data.vertices)}")
        for m in obj.data.materials:
            print(f"  mat: {m.name if m else 'None'}")
            if m and m.node_tree:
                for n in m.node_tree.nodes:
                    if n.type == 'BSDF_PRINCIPLED':
                        bc = n.inputs['Base Color']
                        if bc.is_linked:
                            print(f"    Base Color linked to: {bc.links[0].from_node.name}")
                        else:
                            print(f"    Base Color value: {list(bc.default_value)}")
