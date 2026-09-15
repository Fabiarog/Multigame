extends SceneTree

func _init():
    var scene = load("res://assets/models/club/corvo.glb")
    var inst = scene.instantiate()
    root.add_child(inst)
    var anim: AnimationPlayer = null
    for c in inst.find_children("*", "AnimationPlayer", true, false):
        anim = c
        break
    if anim:
        var idle_anim = anim.get_animation("idle")
        idle_anim.loop_mode = Animation.LOOP_LINEAR
        anim.play("play_card")
        anim.queue("idle")
        for f in range(60):
            anim.advance(0.1)
            if f % 5 == 0:
                print('step ', f, ' cur: ', anim.current_animation, ' time: ', anim.current_animation_position, ' playing: ', anim.is_playing())
    quit()
