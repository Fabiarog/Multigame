extends SceneTree
func _initialize():call_deferred("run")
func run():
	if not OS.has_environment("MULTIGAME_QA_APPDATA"):quit(2);return
	var host=Control.new();root.add_child(host);current_scene=host;host.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var helper=load("res://tools/GameplayChecks.cs").new();host.add_child(helper)
	var stage=helper.CreateGoldenStage()
	for seats in [2,4,6,2]:
		stage.SetCast(2,seats)
		for theme in ["classic_club","cyber_casino","barao_lounge","dama_salon","classic_club"]:
			stage.SetRoomTheme(theme)
			await process_frame;await process_frame
			helper.CheckGoldenState(seats,theme=="classic_club")
	helper.ConfigureCameraMotion(false)
	for cutting in [false,true]:
		stage.AnimateDeck(cutting)
		await create_timer(.15).timeout
		helper.CheckGoldenState(2,true)
		await create_timer(1).timeout
		helper.CheckGoldenState(2,true)
	print("GOLDEN_REGRESSION_PASS: room/chair/probe/seat lifecycle and deck animation")
	await load("res://tools/qa_teardown.gd").finish(self);quit()
