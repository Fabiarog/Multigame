extends SceneTree

func _init():
    var packed = load("res://assets/models/club/aki.glb")
    var m = packed.instantiate()
    root.add_child(m)
    print("=== GODOT SCENE TREE FOR AKI.GLB ===")
    print_tree(m, 0)
    
    var sk = m.find_children("*", "Skeleton3D", true, false)
    if sk.size() > 0:
        var skeleton = sk[0] as Skeleton3D
        print("\nSkeleton found: ", skeleton.get_path(), " bone count: ", skeleton.get_bone_count())
        for i in range(skeleton.get_bone_count()):
            print("  Bone ", i, ": ", skeleton.get_bone_name(i), " parent: ", skeleton.get_bone_parent(i))
            
    var mesh_insts = m.find_children("*", "MeshInstance3D", true, false)
    for mi in mesh_insts:
        var mesh_node = mi as MeshInstance3D
        print("\nMeshInstance3D: ", mesh_node.name, " path: ", mesh_node.get_path(), " skeleton_path: ", mesh_node.skeleton)
        var resolved_sk = mesh_node.get_node_or_null(mesh_node.skeleton)
        print("  Resolved skeleton: ", resolved_sk)
        var skin = mesh_node.skin
        if skin:
            print("  Skin bind count: ", skin.get_bind_count())
            for b in range(min(10, skin.get_bind_count())):
                var bname = skin.get_bind_name(b)
                var bbone = skin.get_bind_bone(b)
                var in_sk = resolved_sk.find_bone(bname) if resolved_sk else -99
                print("    Bind ", b, " name: '", bname, "' skin.bone: ", bbone, " sk.find_bone: ", in_sk)
        else:
            print("  NO SKIN!")
            
    m.free()
    quit(0)

func print_tree(node: Node, indent: int):
    var pad = ""
    for i in range(indent):
        pad += "  "
    print(pad, "- ", node.name, " (", node.get_class(), ")")
    for c in node.get_children():
        print_tree(c, indent + 1)
