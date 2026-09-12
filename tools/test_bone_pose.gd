extends SceneTree

func _init():
    var model = load("res://assets/models/club/corvo.glb").instantiate()
    var skeleton = model.find_child("Skeleton3D", true, false) as Skeleton3D
    if skeleton:
        var b_idx = skeleton.find_bone("CardSocket.R")
        if b_idx < 0:
            b_idx = skeleton.find_bone("Hand.R")
        print("Found bone index: ", b_idx, " name: ", skeleton.get_bone_name(b_idx))
        var pose = skeleton.get_bone_global_pose(b_idx)
        print("Bone global pose in skeleton: origin=", pose.origin)
    model.free()
    quit(0)
