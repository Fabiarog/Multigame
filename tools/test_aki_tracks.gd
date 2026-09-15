extends SceneTree

func _init() -> void:
    var sc = load("res://assets/models/club/aki.glb")
    var inst = sc.instantiate()
    var anim = inst.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    var a = anim.get_animation("idle")
    print("Track count: ", a.get_track_count())
    for i in range(min(15, a.get_track_count())):
        print("  Track ", i, ": ", a.track_get_path(i))
    quit()
