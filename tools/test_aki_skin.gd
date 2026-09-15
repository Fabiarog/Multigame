extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    root.add_child(m)
    var ap = m.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    var sk = m.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
    print("Initial Pelvis pose: ", sk.get_bone_pose_position(1), " rot: ", sk.get_bone_pose_rotation(1))
    print("Initial Thigh.L pose: ", sk.get_bone_pose_position(15), " rot: ", sk.get_bone_pose_rotation(15))
    ap.play("idle")
    ap.advance(0.1)
    print("After idle 0.1s Pelvis pose: ", sk.get_bone_pose_position(1), " rot: ", sk.get_bone_pose_rotation(1))
    print("After idle 0.1s Thigh.L pose: ", sk.get_bone_pose_position(15), " rot: ", sk.get_bone_pose_rotation(15))
    
    # Check mesh skinning
    var mesh_inst = m.find_children("*", "MeshInstance3D", true, false)[0] as MeshInstance3D
    print("Mesh: ", mesh_inst.name, " skin: ", mesh_inst.skin != null, " skeleton path: ", mesh_inst.skeleton)
    var skin = mesh_inst.skin
    if skin != null:
        print("Skin bind count: ", skin.get_bind_count())
        for i in range(min(5, skin.get_bind_count())):
            print("  Bind ", i, " name: ", skin.get_bind_name(i), " bone: ", skin.get_bind_bone(i))
    m.free()
    quit(0)
