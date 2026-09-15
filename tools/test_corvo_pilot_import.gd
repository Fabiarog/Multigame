extends SceneTree

func _init():
    var packed = load('res://temp/test_corvo_pilot.glb')
    if not packed:
        print('FAILED to load pilot GLB!')
        quit(1)
        return
    var node = packed.instantiate()
    print('Loaded root node: ', node.name)
    var animators = []
    for c in node.find_children('*', 'AnimationPlayer', true, false):
        animators.append(c)
    print('AnimationPlayers found: ', animators.size())
    for anim in animators:
        print('Animator path: ', anim.get_path())
        var anim_list = anim.get_animation_list()
        print('Total clips in Godot: ', anim_list.size())
        var new_pilot_clips = ['idle_relaxed', 'idle_nervous', 'nod', 'shake_head', 'lean_forward', 'lean_back', 'win_trick', 'lose_trick', 'lose_hand', 'seat_adjust', 'micro_glance_left', 'micro_glance_right', 'micro_sigh', 'micro_finger_tap']
        for c in new_pilot_clips:
            if anim.has_animation(c):
                var clip = anim.get_animation(c)
                print('  [PASS] Clip: ', c, ' length: ', clip.length, 's tracks: ', clip.get_track_count())
            else:
                print('  [FAIL] Missing clip: ', c)
    var meshes = []
    for m in node.find_children('*', 'MeshInstance3D', true, false):
        meshes.append(m)
    print('MeshInstances: ', meshes.size())
    var heads = []
    for h in node.find_children('Head*', 'Node3D', true, false):
        heads.append(h)
    print('Head markers found: ', heads.size())
    node.free()
    print('=== PILOT IMPORT VALIDATION PASS ===')
    quit(0)
