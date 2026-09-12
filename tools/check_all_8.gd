extends SceneTree

func _init():
    var ids = ['nina', 'bento', 'corvo', 'onca', 'iara', 'zeca', 'barao', 'dama']
    var required_clips = ['entrance', 'truco', 'victory', 'boss_intro', 'flourish', 'idle', 'play_card']
    for id in ids:
        var path = 'res://assets/models/club/' + id + '.glb'
        var packed = load(path)
        if not packed:
            print('ERROR: could not load ', path)
            continue
        var model = packed.instantiate()
        var anims = []
        for a in model.find_children('*', 'AnimationPlayer', true, false):
            anims.append(a)
        var anim_list = anims[0].get_animation_list() if anims.size() > 0 else []
        var has_all = true
        for req in required_clips:
            var found = false
            for c in anim_list:
                if c == req or c.ends_with('/' + req):
                    found = true
                    break
            if not found:
                has_all = false
                print('  Missing clip ', req, ' in ', id)
        var meshes = []
        for m in model.find_children('*', 'MeshInstance3D', true, false):
            meshes.append(m)
        var heads = []
        for h in model.find_children('Head*', 'Node3D', true, false):
            if not (h is MeshInstance3D):
                heads.append(h)
        print(id, ': clips_ok=', has_all, ' (total clips=', anim_list.size(), '), meshes=', meshes.size(), ', heads=', heads.size())
        if heads.size() > 0:
            print('   head pos: ', heads[0].position)
        model.free()
    print('Done checking all 8.')
    quit(0)
