import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/dama.glb")
print("=== DAMA DETAIL ===")
m = bpy.data.materials.get("dama_skin")
if m and m.node_tree:
    for n in m.node_tree.nodes:
        print("Node:", n.name, n.type)
        if n.type == "TEX_IMAGE":
            img = n.image
            print("  Image:", img.name if img else None, "size:", img.size[:] if img else None, "path:", img.filepath if img else None)
        if n.type == "BSDF_PRINCIPLED":
            for inp in n.inputs:
                if inp.is_linked:
                    print("  Linked input:", inp.name, "from:", inp.links[0].from_node.name)
                elif inp.name in ("Base Color", "Roughness", "Metallic"):
                    print("  Default input:", inp.name, inp.default_value)
