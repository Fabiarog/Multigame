extends SceneTree
## Synthetic maximum Truco table: six Blender actors + eighteen physical cards.
func _initialize():
	call_deferred("run")

func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):
		push_error("Use the isolated QA launcher."); quit(2); return
	var args = OS.get_cmdline_user_args()
	var helper = load("res://tools/GameplayChecks.cs").new()
	root.add_child(helper)
	helper.ConfigureTeams(3)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_position(Vector2i(-20000,-20000))
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	Engine.max_fps = 0
	change_scene_to_file("res://games/truco/scenes/TrucoGame.tscn")
	await create_timer(5).timeout
	var results = []
	for size in [Vector2i(1280,720),Vector2i(3840,2160)]:
		DisplayServer.window_set_size(size); root.size = size
		for ultra in [false,true]:
			helper.ConfigureBenchmark(ultra)
			for i in range(35): await process_frame
			var durations = []
			var last = Time.get_ticks_usec()
			for i in range(120):
				await process_frame
				var now = Time.get_ticks_usec()
				durations.append((now-last)/1000.0); last=now
			durations.sort()
			var label = "ultra" if ultra else "leve"
			results.append({"size":str(size),"quality":label,"median_frame_ms":durations[60],"p95_frame_ms":durations[114],"draw_calls":Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),"primitives":Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)})
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png(args[0].path_join("mesa-%sx%s-%s.png" % [size.x,size.y,label]))
	var file=FileAccess.open(args[1],FileAccess.WRITE)
	file.store_string(JSON.stringify({"renderer":RenderingServer.get_current_rendering_method(),"gpu":RenderingServer.get_video_adapter_name(),"synthetic_scene":true,"measurements":results},"\t"))
	print("STAGE_BENCHMARK ",JSON.stringify(results))
	change_scene_to_file("res://hub/scenes/HubMain.tscn")
	await create_timer(.5).timeout
	await load("res://tools/qa_teardown.gd").finish(self)
	quit()
