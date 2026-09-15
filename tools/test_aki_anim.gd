extends SceneTree

func _init() -> void:
    var sc = load("res://assets/models/club/aki.glb")
    var inst = sc.instantiate()
    root.add_child(inst)
    var anim = inst.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    print("Aki anim list: ", anim.get_animation_list())
    anim.play("idle")
    # Advance animation
    anim.advance(0.5)
    var bones = inst.find_children("*", "Skeleton3D", true, false)
    print("Skeletons: ", bones.size())
    if bones.size() > 0:
        var sk = bones[0] as Skeleton3D
        print("Bone 0: ", sk.get_bone_name(0), " pose: ", sk.get_bone_pose_position(0), " rot: ", sk.get_bone_pose_rotation(0))
        for b in range(min(5, sk.get_bone_count())):
            print("  Bone ", b, ": ", sk.get_bone_name(b), " rest: ", sk.get_bone_rest(b))
    quit()
