extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    root.add_child(m)
    var sk = m.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
    var mesh_inst = m.find_children("*", "MeshInstance3D", true, false)[0] as MeshInstance3D
    var skin = mesh_inst.skin
    print("Skeleton bone count: ", sk.get_bone_count())
    for b in range(sk.get_bone_count()):
        print("  sk bone ", b, ": ", sk.get_bone_name(b))
    print("Skin bind count: ", skin.get_bind_count())
    for i in range(skin.get_bind_count()):
        var bname = skin.get_bind_name(i)
        var bidx = sk.find_bone(bname)
        var bone_in_skin = skin.get_bind_bone(i)
        print("  bind ", i, ": name='", bname, "' find_bone=", bidx, " skin.get_bind_bone=", bone_in_skin)
    m.free()
    quit(0)
