extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    root.add_child(m)
    var ap = m.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    var anim = ap.get_animation("idle")
    print("idle animation tracks count: ", anim.get_track_count())
    for t in range(min(15, anim.get_track_count())):
        print("  Track ", t, ": path=", anim.track_get_path(t), " type=", anim.track_get_type(t))
    m.free()
    quit(0)
