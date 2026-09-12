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
        for f in range(200):
            anim.advance(0.05)
        print("After 10 seconds: anim is playing? ", anim.is_playing(), " current: ", anim.current_animation, " pos: ", anim.current_animation_position)
    quit()
