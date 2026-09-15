extends SceneTree

func _init():
    var vp = SubViewport.new()
    vp.size = Vector2i(640, 640)
    vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
    root.add_child(vp)

    var cam = Camera3D.new()
    cam.position = Vector3(0, 1.2, 2.5)
    cam.look_at(Vector3(0, 1.0, 0), Vector3.UP)
    vp.add_child(cam)

    var light = DirectionalLight3D.new()
    light.rotation_degrees = Vector3(-45, 45, 0)
    vp.add_child(light)

    var model = load("res://assets/models/club/aki.glb").instantiate()
    vp.add_child(model)

    var ap = model.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
    print("Playing idle on Aki...")
    ap.play("idle")
    ap.advance(0.5)

    # Force render
    await process_frame
    await process_frame

    var img = vp.get_texture().get_image()
    img.save_png("docs/screenshots/test_aki_render.png")
    print("Saved docs/screenshots/test_aki_render.png")
    quit(0)
