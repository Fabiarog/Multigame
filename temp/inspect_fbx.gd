extends SceneTree

func _init():
    var scene = load("res://assets/animations/Sitting Idle.fbx")
    if scene == null:
        print("Failed to load FBX scene")
        quit(1)
        return
    var inst = scene.instantiate()
    print("Instantiated FBX scene: ", inst.name)
    for c in inst.find_children("*", "AnimationPlayer", true, false):
        var ap: AnimationPlayer = c
        print("Found AnimationPlayer: ", ap.name)
        for a in ap.get_animation_list():
            print("  Anim: ", a, " length: ", ap.get_animation(a).length, " tracks: ", ap.get_animation(a).get_track_count())
            for t in range(mini(5, ap.get_animation(a).get_track_count())):
                print("    track ", t, ": ", ap.get_animation(a).track_get_path(t))
    inst.free()
    quit(0)
