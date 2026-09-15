extends SceneTree

func _init():
    var model = load("res://assets/models/club/corvo.glb").instantiate()
    var anim = model.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    print("Initial is_playing: ", anim.is_playing())
    anim.play("play_card", 0.22)
    print("After play: current_animation=", anim.current_animation, " is_playing=", anim.is_playing())
    anim.queue("idle")
    print("After queue: current_animation=", anim.current_animation, " queue=", anim.get_queue())
    anim.advance(0.5)
    print("After advance 0.5s: current_animation=", anim.current_animation, " pos=", anim.current_animation_position)
    anim.advance(1.5)
    print("After advance 2.0s: current_animation=", anim.current_animation, " is_playing=", anim.is_playing(), " pos=", anim.current_animation_position)
    anim.advance(0.5)
    print("After advance 2.5s: current_animation=", anim.current_animation, " is_playing=", anim.is_playing(), " pos=", anim.current_animation_position)
    anim.advance(2.0)
    print("After advance 4.5s: current_animation=", anim.current_animation, " is_playing=", anim.is_playing(), " pos=", anim.current_animation_position)
    model.free()
    quit(0)

