extends SceneTree

func _init():
    var m = load("res://assets/models/club/corvo.glb").instantiate()
    print("=== Corvo Scene Structure ===")
    pr(m, 0)
    var skeletons = m.find_children("*", "Skeleton3D", true, false)
    for s in skeletons:
        print("Skeleton: ", s.name, " with ", s.get_bone_count(), " bones:")
        for i in range(s.get_bone_count()):
            print("  Bone ", i, ": ", s.get_bone_name(i), " parent=", s.get_bone_parent(i))
    m.free()
    quit(0)

func pr(n, d):
    print("  ".repeat(d) + n.name + " (" + n.get_class() + ")")
    for c in n.get_children():
        pr(c, d + 1)
