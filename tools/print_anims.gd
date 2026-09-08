extends SceneTree

func _init():
    for n in ["nina", "bento", "onca", "iara", "zeca", "dama", "barao", "corvo"]:
        var m = load("res://assets/models/club/" + n + ".glb").instantiate()
        var a = m.find_children("*", "AnimationPlayer", true, false)[0]
        print("=== " + n.to_upper() + " (" + str(a.get_animation_list().size()) + " anims) ===")
        for clip in a.get_animation_list():
            print("  " + clip)
        m.free()
    quit(0)

