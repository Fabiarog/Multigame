extends SceneTree
func _initialize():call_deferred("run")
func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):quit(2);return
	var args=OS.get_cmdline_user_args();DirAccess.make_dir_recursive_absolute(args[0])
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED);DisplayServer.window_set_size(Vector2i(1920,1080));root.size=Vector2i(1920,1080)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED);Engine.max_fps=0
	var host=Control.new();root.add_child(host);current_scene=host;host.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var helper=load("res://tools/GameplayChecks.cs").new();host.add_child(helper)
	var stage=helper.CreateGoldenStage();helper.ConfigureBenchmark(true);stage.ToggleCameraMode()
	var environment=stage.find_child("TableLighting",true,false).environment
	var probe=stage.find_child("TableStaticReflection",true,false)
	var world=probe.get_parent()
	var voxel=VoxelGI.new();voxel.size=Vector3(28,10,24);voxel.position=Vector3(0,2,0);voxel.subdiv=VoxelGI.SUBDIV_64;world.add_child(voxel);voxel.visible=false
	var rows=[]
	for mode in ["none","probe","ssil","sdfgi","voxel"]:
		probe.visible=mode!="none";environment.ssil_enabled=mode=="ssil";environment.ssil_intensity=.6
		environment.sdfgi_enabled=mode=="sdfgi";environment.sdfgi_min_cell_size=.3;environment.sdfgi_energy=.7
		voxel.visible=mode=="voxel"
		if mode=="voxel":voxel.bake(world);voxel.data.energy=.7
		for i in range(120):await process_frame
		var times=[];var last=Time.get_ticks_usec()
		for i in range(90):
			await process_frame
			var now=Time.get_ticks_usec();times.append((now-last)/1000.0);last=now
		times.sort();rows.append({"mode":mode,"median_ms":times[45],"p95_ms":times[85],"render_memory_bytes":Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED)})
		await RenderingServer.frame_post_draw;root.get_texture().get_image().save_png(args[0].path_join(mode+".png"))
	rows.append({"mode":"lightmap","status":"not_baked","reason":"Current exported room has UV1 only; requires editor bake after a separate UV2/lightmap setup."})
	var f=FileAccess.open(args[0].path_join("report.json"),FileAccess.WRITE);f.store_string(JSON.stringify(rows,"\t"));f.close();print("GI_COMPARISON ",JSON.stringify(rows))
	await load("res://tools/qa_teardown.gd").finish(self);quit()
