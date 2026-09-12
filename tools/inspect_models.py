import bpy

for name in ["nina", "dama", "morgana", "aki"]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    filepath = f"assets/models/club/{name}.glb"
    bpy.ops.import_scene.gltf(filepath=filepath)
    print(f"=== {name.upper()}.GLB ===")
    arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
    if arm:
        print(f"  Armature: {arm.name}, rot: {arm.rotation_euler}, bones: {len(arm.data.bones)}")
        if arm.animation_data:
            print(f"  NLA tracks ({len(arm.animation_data.nla_tracks)}): {[t.name for t in arm.animation_data.nla_tracks]}")
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    for m in meshes:
        print(f"  Mesh: {m.name}, verts: {len(m.data.vertices)}, rot: {m.rotation_euler}, loc: {m.location}")
        for mat in m.data.materials:
            if mat:
                base_color = None
                tex = None
                if mat.node_tree:
                    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
                    if bsdf:
                        if 'Base Color' in bsdf.inputs:
                            base_color = bsdf.inputs['Base Color'].default_value[:]
                            if bsdf.inputs['Base Color'].is_linked:
                                tex = bsdf.inputs['Base Color'].links[0].from_node.name
                print(f"    Material: {mat.name}, color: {base_color}, tex: {tex}")
