extends SceneTree

func _init():
    var scene = load("res://assets/models/club/corvo.glb")
    var inst = scene.instantiate()
    var anim: AnimationPlayer = null
    for c in inst.find_children("*", "AnimationPlayer", true, false):
        anim = c
        break
    if anim:
        var a = anim.get_animation("idle")
        print("Idle track count: ", a.get_track_count(), " loop: ", a.loop_mode)
        for i in range(a.get_track_count()):
            print("Track ", i, ": ", a.track_get_path(i), " keys: ", a.track_get_key_count(i))
            for k in range(min(3, a.track_get_key_count(i))):
                print("   t=", a.track_get_key_time(i, k), " val=", a.track_get_key_value(i, k))
    quit()
