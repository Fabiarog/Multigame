extends SceneTree
## Physical viewport validation in all three games, independent of UI layout scale.
func _initialize():
	call_deferred("run")

func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):
		push_error("Use an isolated APPDATA."); quit(2); return
	var args = OS.get_cmdline_user_args()
	var helper = load("res://tools/GameplayChecks.cs").new()
	root.add_child(helper)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_position(Vector2i(-20000,-20000))
	var results = []
	for game in ["poker_roguelike", "truco", "fodinha"]:
		var scene = {"poker_roguelike":"PokerGame", "truco":"TrucoGame", "fodinha":"FodinhaGame"}[game]
		change_scene_to_file("res://games/%s/scenes/%s.tscn" % [game,scene])
		await create_timer(2.5).timeout
		for size in [Vector2i(1920,1080), Vector2i(3840,2160)]:
			DisplayServer.window_set_size(size); root.size = size
			for scale in [1.0,0.5]:
				helper.ConfigureFidelity(scale)
				for i in range(10): await process_frame
				var measurement = helper.CheckRenderResolution()
				measurement.merge({"game":game,"window":str(size)})
				results.append(measurement)
				await RenderingServer.frame_post_draw
				if scale == 1.0: root.get_texture().get_image().save_png(args[0].path_join("%s-%s.png" % [game,size.x]))
		# Optional diagnostic isolates shadow artifacts without changing the game defaults.
		if args.has("--shadow-diagnostic"):
			for light in current_scene.find_children("*", "Light3D", true, false): light.shadow_enabled = false
			for i in range(10): await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png(args[0].path_join("%s-no-shadows.png" % game))
	var report = FileAccess.open(args[1],FileAccess.WRITE)
	report.store_string(JSON.stringify(results,"\t"))
	print("FIDELITY_RESOLUTION_PASS ",JSON.stringify(results))
	await load("res://tools/qa_teardown.gd").finish(self)
	quit()
