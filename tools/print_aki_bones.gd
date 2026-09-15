extends SceneTree

func _init() -> void:
    var sc = load("res://assets/models/club/aki.glb")
    var inst = sc.instantiate()
    var sk = inst.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
    print("Skeleton3D bone count: ", sk.get_bone_count())
    for i in range(sk.get_bone_count()):
        print("  Bone ", i, ": ", sk.get_bone_name(i))
    quit()
