extends SceneTree
## Run with visual_smoke.ps1: it isolates user:// before autoloads initialize.
## Captures the real renderer, checks visible UI geometry and exercises card actions.

const SCENES = {
	"abertura": "res://hub/scenes/HubMain.tscn",
	"poker": "res://games/poker_roguelike/scenes/PokerGame.tscn",
	"truco": "res://games/truco/scenes/TrucoGame.tscn",
	"fodinha": "res://games/fodinha/scenes/FodinhaGame.tscn",
}

var output_dir: String
var report_path: String
var strict_layout := true
var report := {"screenshots": [], "actions": [], "layout_issues": [], "failures": []}


func _initialize() -> void:
	call_deferred("run_checks")


func run_checks() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 2 or not OS.has_environment("MULTIGAME_QA_APPDATA"):
		push_error("Use tools/visual_smoke.ps1 to isolate saves and supply output paths.")
		quit(2)
		return
	output_dir = args[0]
	report_path = args[1]
	strict_layout = not args.has("--allow-layout-warnings")
	var isolated_dir := OS.get_environment("MULTIGAME_QA_APPDATA").replace("\\", "/").trim_suffix("/")
	var user_dir := OS.get_user_data_dir().replace("\\", "/")
	if not user_dir.to_lower().begins_with((isolated_dir + "/").to_lower()):
		push_error("QA aborted: user:// is outside the isolated APPDATA directory.")
		quit(2)
		return
	if DisplayServer.get_name() == "headless":
		push_error("Screenshots require a renderer; use the offscreen Windows launcher.")
		quit(2)
		return
	DirAccess.make_dir_recursive_absolute(output_dir)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(1280, 720))
	DisplayServer.window_set_position(Vector2i(-20000, -20000))
	root.size = Vector2i(1280, 720)
	# Seed the existing RNG input so repeated captures use the same opening deal.
	seed(20260905)
	for scene_name in SCENES:
		var error := change_scene_to_file(SCENES[scene_name])
		if error != OK:
			fail("Cannot open %s: %s" % [scene_name, error])
			continue
		await settle(0.65)
		if current_scene == null:
			fail("Scene did not initialize: " + scene_name)
			continue
		if scene_name == "truco":
			if find_button("Pular entrada") != null:
				press_button("Pular entrada")
			await wait_for_button("Cortar o baralho", 10.0)
			await capture("truco-corte")
			if press_button("Cortar o baralho"):
				await settle(1.75)
				# Also works if a future default opens a team match.
				if find_button("Sem pena") != null:
					press_button("Sem pena")
					await settle(1.75)
		if scene_name == "poker":
			await capture("boss-entrada")
			await settle(2.3)
		await capture(scene_name)
		if scene_name == "abertura":
			await check_hub_menus()
		elif scene_name == "poker":
			await check_poker_actions()
		elif scene_name == "truco":
			await check_truco_actions()
		elif scene_name == "fodinha":
			if find_button("Pular entrada") != null: press_button("Pular entrada")
			await wait_for_button("Palpite: 0", 5.0)
			press_button("Como jogar")
			await capture("fodinha-regras")
			for dialog in current_scene.find_children("*", "AcceptDialog", true, false): dialog.hide(); dialog.queue_free()
			press_button("Palpite: 0")
			await settle(2.5)
			var card = current_scene.find_child("HandCard0", true, false)
			if card == null or card.disabled: fail("Fodinha did not enable the local card after bidding")
			else: card.emit_signal("pressed"); report.actions.append("Played Fodinha card")
			await wait_for_button("Próxima mão", 9.0)
			await capture("fodinha-resultado")
			press_button("Próxima mão")
			await settle(.4)
			await capture("fodinha-mao-2")
	await check_team_tables()
	await check_window_sizes()
	var file := FileAccess.open(report_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(report, "\t"))
	else:
		fail("Cannot write QA report: " + report_path)
	var failed: bool = not report.failures.is_empty() or (strict_layout and not report.layout_issues.is_empty())
	print("VISUAL_QA_RESULT ", "FAIL" if failed else "PASS", " | screenshots=", report.screenshots.size(),
		" actions=", report.actions.size(), " layout_issues=", report.layout_issues.size(), " failures=", report.failures.size())
	print("VISUAL_QA_REPORT ", report_path)
	await load("res://tools/qa_teardown.gd").finish(self)
	quit(1 if failed else 0)


func settle(seconds := 0.2) -> void:
	await create_timer(seconds).timeout
	for frame in range(4):
		await process_frame
	await RenderingServer.frame_post_draw


func capture(name: String) -> void:
	await RenderingServer.frame_post_draw
	var screenshot := root.get_texture().get_image()
	var path := output_dir.path_join(name + ".png")
	if screenshot == null or screenshot.is_empty():
		fail("Empty viewport capture: " + name)
		return
	var error := screenshot.save_png(path)
	if error != OK:
		fail("Cannot save screenshot %s: %s" % [name, error])
		return
	report.screenshots.append({"name": name, "width": screenshot.get_width(), "height": screenshot.get_height()})
	check_layout(current_scene, name)
	print("VISUAL_QA_SCREENSHOT ", path)


func check_layout(node: Node, scene_name: String) -> void:
	if node is Control and node.is_visible_in_tree() and not node.get_meta("qa_ignore_layout", false):
		var control := node as Control
		var relevant := control is Label or control is BaseButton or control is PanelContainer
		if relevant and control.modulate.a > 0.05 and control.size.x > 0 and control.size.y > 0 and not in_scroll(control):
			var rect: Rect2 = control.get_global_transform_with_canvas() * Rect2(Vector2.ZERO, control.size)
			var viewport_rect := root.get_visible_rect().grow(2.0)
			if not viewport_rect.encloses(rect):
				layout_issue(scene_name, control, "outside_viewport", str(rect))
			var minimum := control.get_combined_minimum_size()
			if control.size.x + 2 < minimum.x or control.size.y + 2 < minimum.y:
				layout_issue(scene_name, control, "below_minimum_size", "size=%s minimum=%s" % [control.size, minimum])
	for child in node.get_children():
		check_layout(child, scene_name)


func in_scroll(control: Control) -> bool:
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer:
			return true
		ancestor = ancestor.get_parent()
	return false


func layout_issue(scene_name: String, control: Control, kind: String, detail: String) -> void:
	var text := ""
	if control is Label or control is Button:
		text = control.text
	report.layout_issues.append({"screen": scene_name, "node": str(control.get_path()), "text": text, "kind": kind, "detail": detail})
	print("VISUAL_QA_LAYOUT ", scene_name, " | ", kind, " | ", text, " | ", detail)


func find_button(prefix: String) -> Button:
	for node in current_scene.find_children("*", "Button", true, false):
		if node.is_visible_in_tree() and not node.disabled and node.text.begins_with(prefix):
			return node
	return null


func press_button(prefix: String) -> bool:
	var button := find_button(prefix)
	if button == null:
		fail("Required enabled button not found: " + prefix)
		return false
	button.emit_signal("pressed")
	report.actions.append("Pressed: " + prefix)
	return true


func wait_for_button(prefix: String, timeout_seconds := 7.0) -> void:
	var deadline := Time.get_ticks_msec() + int(timeout_seconds * 1000)
	while find_button(prefix) == null and Time.get_ticks_msec() < deadline:
		await process_frame


func check_hub_menus() -> void:
	var menus := {"Coleção": "colecao", "Ajustes": "ajustes", "Acessibilidade": "acessibilidade", "Entrar em sala": "entrar-sala"}
	for button_text in menus:
		if not press_button(button_text):
			continue
		await settle(0.35)
		await capture(menus[button_text])
		if not press_button("Voltar"):
			return
		await settle(0.2)
		if find_button(button_text) == null:
			fail("Hub navigation did not return to the opening menu after " + button_text)


func check_window_sizes() -> void:
	if change_scene_to_file(SCENES.abertura) != OK:
		fail("Cannot reopen the hub for alternate window sizes.")
		return
	await settle(0.35)
	for size in [Vector2i(1600, 900), Vector2i(1024, 768)]:
		DisplayServer.window_set_size(size)
		root.size = size
		await settle(0.35)
		await capture("abertura-%sx%s" % [size.x, size.y])


func check_team_tables() -> void:
	var config = load("res://tools/GameplayChecks.cs").new()
	root.add_child(config)
	for team_size in [2, 3]:
		config.ConfigureTeams(team_size)
		change_scene_to_file(SCENES.truco)
		await settle(0.65)
		if team_size == 2:
			await capture("truco-entrada")
		if find_button("Pular entrada") != null:
			press_button("Pular entrada")
		await wait_for_button("Cortar o baralho", 10.0)
		if press_button("Cortar o baralho"):
			await settle(0.55)
			await capture("truco-%sx%s-pena" % [team_size, team_size])
			if press_button("Entregar a pena"):
				await settle(2.6)
				await capture("truco-%sx%s" % [team_size, team_size])
	config.ConfigureTeams(1)
	config.queue_free()


func playable_cards() -> Array[Control]:
	var cards: Array[Control] = []
	for node in current_scene.find_children("*", "PanelContainer", true, false):
		if node.is_visible_in_tree() and node.focus_mode == Control.FOCUS_ALL and node.mouse_default_cursor_shape == Control.CURSOR_POINTING_HAND:
			cards.append(node)
	return cards


func activate_card(card: Control) -> void:
	var event := InputEventMouseButton.new()
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = true
	report.actions.append("Card GUI input: " + str(card.get_path()))
	# A played Truco card is removed synchronously by its real GUI handler.
	card.emit_signal("gui_input", event)


func check_poker_actions() -> void:
	var cards := playable_cards()
	if cards.size() != 8:
		fail("Poker opening hand should expose 8 playable cards; found %s." % cards.size())
		return
	activate_card(cards[0])
	await settle()
	await capture("poker-selecao")
	if not press_button("Descartar"):
		return
	await settle(0.5)
	cards = playable_cards()
	if cards.size() != 8:
		fail("Poker discard should replenish the hand to 8 cards.")
		return
	activate_card(cards[0])
	await settle()
	if press_button("Jogar mão"):
		await settle(0.9)
		await capture("poker-pilha")
		await settle(1.6)
		await capture("poker-jogada")


func check_truco_actions() -> void:
	var cards := playable_cards()
	if cards.size() != 3:
		fail("Truco dealt hand should expose 3 playable cards; found %s." % cards.size())
		return
	activate_card(cards[0])
	await settle(2.5)
	await capture("truco-jogada")
	# The other team can lead next. Remaining cards need not be enabled at this instant.
	var remaining := 0
	for node in current_scene.find_children("*", "PanelContainer", true, false):
		if node.is_visible_in_tree() and node.focus_mode == Control.FOCUS_ALL:
			remaining += 1
	if remaining != 2:
		fail("Truco should have 2 cards after the first card is played; found %s." % remaining)


func fail(message: String) -> void:
	report.failures.append(message)
	push_error("VISUAL_QA_FAILURE " + message)
