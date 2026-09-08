extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    root.add_child(m)
    var ap = m.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    var mi = m.find_children("*", "MeshInstance3D", true, false)[0] as MeshInstance3D
    var sk = m.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
    print("MeshInstance3D: ", mi.name)
    print("  skeleton path: ", mi.skeleton)
    var target_sk = mi.get_node_or_null(mi.skeleton)
    print("  target skeleton found: ", target_sk != null, " is same: ", target_sk == sk)
    print("  skin: ", mi.skin != null)
    if mi.skin:
        print("  skin bind count: ", mi.skin.get_bind_count())
        for i in range(min(5, mi.skin.get_bind_count())):
            print("    bind ", i, ": name=", mi.skin.get_bind_name(i), " bone_idx in sk=", sk.find_bone(mi.skin.get_bind_name(i)))
    
    # Check corvo for comparison
    var c = load("res://assets/models/club/corvo.glb").instantiate()
    root.add_child(c)
    var c_mi = c.find_children("*", "MeshInstance3D", true, false)[0] as MeshInstance3D
    var c_sk = c.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
    print("Corvo MeshInstance3D: ", c_mi.name)
    print("  skeleton path: ", c_mi.skeleton)
    var c_target = c_mi.get_node_or_null(c_mi.skeleton)
    print("  target skeleton found: ", c_target != null, " is same: ", c_target == c_sk)
    print("  skin: ", c_mi.skin != null)
    if c_mi.skin:
        print("  skin bind count: ", c_mi.skin.get_bind_count())
        for i in range(min(5, c_mi.skin.get_bind_count())):
            print("    bind ", i, ": name=", c_mi.skin.get_bind_name(i), " bone_idx in sk=", c_sk.find_bone(c_mi.skin.get_bind_name(i)))
    
    m.free()
    c.free()
    quit(0)
