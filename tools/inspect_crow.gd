extends SceneTree

func _init():
    var m = load("res://temp/test_crow_split.glb").instantiate()
    print("=== Mascot Crow Tree ===")
    _print_tree(m, 0)
    var anim = m.find_children("*", "AnimationPlayer", true, false)
    print("AnimationPlayers: ", anim.size())
    for a in anim:
        var ap = a as AnimationPlayer
        print("  AnimPlayer: ", ap.name, " anims: ", ap.get_animation_list())
    m.free()
    quit(0)

func _print_tree(node: Node, indent: int):
    var pad = ""
    for i in range(indent): pad += "  "
    print(pad + node.name + " (" + node.get_class() + ")")
    for c in node.get_children():
        _print_tree(c, indent + 1)
