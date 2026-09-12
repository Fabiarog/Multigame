extends SceneTree

func _initialize():
	call_deferred("run")

func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):
		push_error("Use the isolated visual_smoke.ps1 launcher.")
		quit(2)
		return
	var checks = load("res://tools/GameplayChecks.cs").new()
	# Only this isolated rules harness accelerates time. Accessibility must not.
	Engine.time_scale = 12.0
	root.add_child(checks)
	checks.Completed.connect(func(passed, detail):
		print("GAMEPLAY_QA ", "PASS" if passed else "FAIL", " | ", detail)
		change_scene_to_file("res://hub/scenes/HubMain.tscn")
		await create_timer(1.0).timeout
		await load("res://tools/qa_teardown.gd").finish(self)
		quit(0 if passed else 1))
	checks.Run()
