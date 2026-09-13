extends SceneTree
## Fixed composition for before/after captures; no AI turns or changing cast.
func _initialize():call_deferred("run")
func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):quit(2);return
	var args=OS.get_cmdline_user_args()
	DirAccess.make_dir_recursive_absolute(args[0])
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	Engine.max_fps=0
	var host=Control.new();root.add_child(host);current_scene=host;host.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var helper=load("res://tools/GameplayChecks.cs").new();host.add_child(helper)
	var stage=helper.CreateGoldenStage()
	var measures=[]
	for size in [Vector2i(1920,1080),Vector2i(3840,2160)]:
		DisplayServer.window_set_size(size);root.size=size
		for ultra in [false,true]:
			helper.ConfigureBenchmark(ultra)
			for i in range(60):await process_frame
			for pov in [true,false]:
				if (stage.CurrentCameraMode==0)!=pov:stage.ToggleCameraMode()
				for i in range(40):await process_frame
				var dt=[];var last=Time.get_ticks_usec()
				for i in range(90):
					await process_frame
					var now=Time.get_ticks_usec();dt.append((now-last)/1000.0);last=now
				dt.sort()
				var label="%s-%s-%s"%[size.x,"ultra" if ultra else "baixo","pov" if pov else "mesa"]
				measures.append({"label":label,"median_ms":dt[45],"p95_ms":dt[85],"draw_calls":Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),"primitives":Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME),"render_memory_bytes":Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED),"process_ms":Performance.get_monitor(Performance.TIME_PROCESS)*1000})
				await RenderingServer.frame_post_draw
				root.get_texture().get_image().save_png(args[0].path_join(label+".png"))
	var f=FileAccess.open(args[0].path_join("report.json"),FileAccess.WRITE)
	f.store_string(JSON.stringify({"version":Engine.get_version_info(),"gpu":RenderingServer.get_video_adapter_name(),"renderer":RenderingServer.get_current_rendering_method(),"measurements":measures},"\t"));f.close()
	print("GOLDEN_QA_PASS ",JSON.stringify(measures))
	await load("res://tools/qa_teardown.gd").finish(self)
	quit()
