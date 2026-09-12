import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='assets/modelos 3d detalhados/aki_fbx/Aki.fbx')

print('=== OBJECTS IN AKI.FBX ===')
for obj in bpy.data.objects:
    print(f'{obj.type}: {obj.name}, parent={obj.parent.name if obj.parent else None}')
    if obj.type == 'MESH':
        mats = [m.name if m else 'None' for m in obj.data.materials]
        print(f'   Vertices: {len(obj.data.vertices)}, Materials: {mats}')
        for m in obj.data.materials:
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type == 'TEX_IMAGE':
                        print(f'     Texture in mat {m.name}: {n.image.name if n.image else "NoImg"}, path={n.image.filepath if n.image else "NoPath"}')

print('=== ARMATURES & ANIMATIONS ===')
for arm in [o for o in bpy.data.objects if o.type == 'ARMATURE']:
    print('Armature:', arm.name)
    if arm.animation_data and arm.animation_data.action:
        print(' Action:', arm.animation_data.action.name)
for act in bpy.data.actions:
    print('Action in file:', act.name)
