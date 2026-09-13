import bpy
import os

orig_scene = bpy.context.window.scene
temp_scene = bpy.data.scenes.new("InspectScene")
bpy.context.window.scene = temp_scene

try:
    for name in ["zeca.glb", "corvo.glb", "iara.glb", "barao.glb"]:
        p = os.path.abspath(os.path.join("assets", "models", "club", name))
        if not os.path.exists(p):
            continue
        print(f"=== {name} ===")
        # clear temp_scene
        for obj in list(temp_scene.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.ops.import_scene.gltf(filepath=p)
        for obj in temp_scene.objects:
            if obj.type == "ARMATURE":
                print(f"  ARMATURE: {obj.name}, bones={len(obj.data.bones)}")
                print(f"    Bones: {[b.name for b in obj.data.bones[:10]]}...")
            elif obj.type == "MESH":
                print(f"  MESH: {obj.name}, verts={len(obj.data.vertices)}, polys={len(obj.data.polygons)}")
                for slot in obj.material_slots:
                    if slot.material:
                        print(f"    Material: {slot.material.name}")
                        if slot.material.use_nodes:
                            for node in slot.material.node_tree.nodes:
                                if node.type == "TEX_IMAGE" and node.image:
                                    print(f"      Texture: {node.image.name} ({node.image.size[0]}x{node.image.size[1]})")
finally:
    for obj in list(temp_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.window.scene = orig_scene
    bpy.data.scenes.remove(temp_scene)
    print("Inspection complete.")
