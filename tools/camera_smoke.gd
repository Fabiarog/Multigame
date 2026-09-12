extends SceneTree
## Renderer-backed POV checks; run through visual_smoke.ps1 for isolated saves.
var failures := []
var measurements := []
var output_dir: String

func _initialize():
	call_deferred("run")

func check(condition: bool, detail: String):
	if not condition: failures.append(detail)

func settle(seconds := .3):
	await create_timer(seconds).timeout
	for i in range(4): await process_frame
	await RenderingServer.frame_post_draw

func capture(label: String):
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(output_dir.path_join(label + ".png"))

func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"): quit(2); return
	var args = OS.get_cmdline_user_args()
	output_dir = args[0]
	var helper = load("res://tools/GameplayChecks.cs").new()
	root.add_child(helper)
	helper.ConfigureCameraMotion(false)
	DisplayServer.window_set_size(Vector2i(1280,720)); root.size = Vector2i(1280,720)
	for game in ["poker_roguelike", "truco", "fodinha"]:
		var scene = {"poker_roguelike":"PokerGame", "truco":"TrucoGame", "fodinha":"FodinhaGame"}[game]
		change_scene_to_file("res://games/%s/scenes/%s.tscn" % [game, scene])
		await settle(.4)
		for button in current_scene.find_children("*", "Button", true, false):
			if button.text == "Pular entrada" and button.is_visible_in_tree(): button.emit_signal("pressed")
		await settle(.7)
		var table = null
		for control in current_scene.find_children("*", "Control", true, false):
			if control.has_method("SetLookAngles"): table = control; break
		if table == null: check(false, game + ": missing stage"); continue
		var camera = table.find_child("MainCamera", true, false)
		check(camera.projection == Camera3D.PROJECTION_PERSPECTIVE, game + ": default POV")
		check(camera.position.y > 1.6 and camera.position.y < 1.66, game + ": raised eyes")
		await capture(game + "-pov-center")
		# Feed real mouse input through the viewport, including right-button ownership.
		var start = table.get_global_rect().get_center()
		var move = InputEventMouseMotion.new(); move.position = start; move.global_position = start
		Input.parse_input_event(move); await process_frame
		var down = InputEventMouseButton.new(); down.button_index = MOUSE_BUTTON_RIGHT; down.pressed = true; down.position = start; down.global_position = start
		Input.parse_input_event(down); await process_frame
		move = InputEventMouseMotion.new(); move.position = start + Vector2(90, 0); move.global_position = move.position; move.relative = Vector2(90,0); move.button_mask = MOUSE_BUTTON_MASK_RIGHT
		Input.parse_input_event(move); await settle(.3)
		check(table.LookAngles.x < -5, game + ": right-drag moves neck")
		down = InputEventMouseButton.new(); down.button_index = MOUSE_BUTTON_RIGHT; down.pressed = false; down.position = move.position; down.global_position = move.position
		Input.parse_input_event(down)
		table.SetLookAngles(999, 999); await settle(.8)
		check(abs(table.LookAngles.x - 48) < .2 and abs(table.LookAngles.y - 12) < .2, game + ": positive neck limits")
		await capture(game + "-pov-left-up")
		table.SetLookAngles(-999, -999); await settle(.8)
		check(abs(table.LookAngles.x + 48) < .2 and abs(table.LookAngles.y + 12) < .2, game + ": negative neck limits")
		await capture(game + "-pov-right-down")
		for button in table.find_children("*", "Button", true, false):
			if button.text == "Centralizar": button.emit_signal("pressed")
		await settle(.8)
		check(table.LookAngles.length() < .2, game + ": recenter button")
		var key = InputEventKey.new(); key.keycode = KEY_C; key.pressed = true
		Input.parse_input_event(key); await settle(.2)
		# The received Patch 24 uses perspective for its elevated table view.
		check(camera.position.y > 6.15 and camera.position.y < 6.25, game + ": C restores elevated table view")
		await capture(game + "-overhead")
		key = InputEventKey.new(); key.keycode = KEY_C; key.pressed = false; Input.parse_input_event(key)
		table.ToggleCameraMode()
		helper.ConfigureCameraMotion(true); table.SetLookAngles(20, 5); await settle(.2)
		var stable = camera.transform
		await settle(.2)
		check(table.LookAngles.is_equal_approx(Vector2(20,5)) and camera.transform.is_equal_approx(stable), game + ": reduced motion keeps deliberate look without sway")
		table.SetLookAngles(0,0)
		DisplayServer.window_set_size(Vector2i(3840,2160)); root.size = Vector2i(3840,2160)
		await settle(.4)
		var pixels = helper.CheckRenderResolution()
		pixels.merge({"game":game,"eye_height":camera.position.y,"yaw_limit":48,"pitch_limit":12})
		measurements.append(pixels)
		await capture(game + "-pov-4k")
		DisplayServer.window_set_size(Vector2i(1280,720)); root.size = Vector2i(1280,720)
		helper.ConfigureCameraMotion(false)
	var report = FileAccess.open(args[1], FileAccess.WRITE)
	report.store_string(JSON.stringify({"failures":failures,"measurements":measurements}, "\t"))
	print("CAMERA_QA ", "PASS" if failures.is_empty() else "FAIL", " ", JSON.stringify(failures))
	await load("res://tools/qa_teardown.gd").finish(self)
	quit(0 if failures.is_empty() else 1)
