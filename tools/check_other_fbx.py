import bpy

for name, fbx in [('carnical', 'assets/modelos 3d detalhados/ghoul_ue5.fbx'), ('morgana', 'assets/modelos 3d detalhados/source/Witch_skeletal_mesh.fbx')]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    if arms:
        arm = arms[0]
        print(name, 'armature:', arm.name)
        for b in arm.data.bones:
            if any(k in b.name.lower() for k in ['upperarm', 'arm_l', 'arm_r']):
                print(' ', b.name, 'head:', b.head_local, 'tail:', b.tail_local)
