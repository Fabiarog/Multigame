extends SceneTree

func _init():
    var packed = load('res://temp/test_corvo_rigged.glb')
    if not packed:
        print('FAILED to load packed scene!')
        quit(1)
        return
    var node = packed.instantiate()
    print('Loaded root node: ', node.name)
    var animators = []
    for c in node.find_children('*', 'AnimationPlayer', true, false):
        animators.append(c)
    print('AnimationPlayers found: ', animators.size())
    for anim in animators:
        print('  Animator: ', anim.name, ' path: ', anim.get_path())
        for a in anim.get_animation_list():
            var anim_obj = anim.get_animation(a)
            print('    Clip: ', a, ' length: ', anim_obj.length if anim_obj else 0, ' tracks: ', anim_obj.get_track_count() if anim_obj else 0)
    var meshes = []
    for m in node.find_children('*', 'MeshInstance3D', true, false):
        meshes.append(m)
    print('MeshInstances: ', meshes.size())
    for m in meshes:
        print('  Mesh: ', m.name, ' surfaces: ', m.mesh.get_surface_count() if m.mesh else 0)
        if m.mesh:
            for s in range(m.mesh.get_surface_count()):
                var mat = m.mesh.surface_get_material(s)
                print('    Surface ', s, ' mat: ', mat.resource_name if mat else 'null')
    var heads = []
    for h in node.find_children('Head*', 'Node3D', true, false):
        heads.append(h)
    print('Head nodes found: ', heads.size())
    for h in heads:
        print('  Head: ', h.name, ' pos: ', h.position, ' (isMesh: ', h is MeshInstance3D, ')')
    node.free()
    print('=== TEST IMPORT SUCCESSFUL ===')
    quit(0)
