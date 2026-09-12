extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    print("=== Aki Scene Structure ===")
    _print_tree(m, 0)
    var anim = m.find_children("*", "AnimationPlayer", true, false)
    if anim.size() > 0:
        var ap = anim[0] as AnimationPlayer
        print("AnimationPlayer found: ", ap.name, " current: ", ap.current_animation)
        print("Anims: ", ap.get_animation_list())
    var skeletons = m.find_children("*", "Skeleton3D", true, false)
    for s in skeletons:
        print("Skeleton: ", s.name, " with ", s.get_bone_count(), " bones:")
        for i in range(s.get_bone_count()):
            print("  Bone ", i, ": ", s.get_bone_name(i), " parent=", s.get_bone_parent(i))
    m.free()
    quit(0)

func _print_tree(node: Node, indent: int):
    var pad = ""
    for i in range(indent): pad += "  "
    print(pad + node.name + " (" + node.get_class() + ")")
    for c in node.get_children():
        _print_tree(c, indent + 1)
