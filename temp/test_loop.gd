extends SceneTree

func _init():
    var scene = load("res://assets/models/club/corvo.glb")
    var inst = scene.instantiate()
    var anim: AnimationPlayer = null
    for c in inst.find_children("*", "AnimationPlayer", true, false):
        anim = c
        break
    if anim:
        var idle_anim = anim.get_animation("idle")
        idle_anim.loop_mode = Animation.LOOP_LINEAR
        print("idle loop mode set to: ", idle_anim.loop_mode)
        anim.play("play_card")
        anim.queue("idle")
        print("current anim: ", anim.current_animation)
        print("assigned anim: ", anim.assigned_animation)
        print("queue: ", anim.get_queue())
    quit()
