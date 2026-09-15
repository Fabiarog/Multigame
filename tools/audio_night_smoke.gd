extends SceneTree
var failures = []
func _initialize():
    call_deferred("run")
func expect(audio, track, label):
    if audio.get("CurrentTrack") != track:
        failures.append(label + ": expected " + track + " got " + str(audio.get("CurrentTrack")))
func run():
    if not OS.has_environment("MULTIGAME_QA_APPDATA"):
        quit(2)
        return
    var audio = root.get_node("AudioManager")
    var settings = root.get_node("SettingsManager")
    settings.set("MusicTrack", 10)
    audio.PlayMenuMusic()
    expect(audio, "mexico", "menu selection")
    audio.BeginRoomMusic("classic_club")
    expect(audio, "midnight-club", "room ignores menu")
    settings.set("MusicTrack", 9)
    audio.RefreshTrack()
    expect(audio, "midnight-club", "settings cannot replace room")
    audio.PlayRoomMusic("mexico_recuerdos")
    expect(audio, "mexico", "room switch")
    audio.PlayBossMusic(7)
    expect(audio, "barao", "boss priority")
    audio.PlayRoomMusic("madrid_salon")
    audio.RefreshTrack()
    expect(audio, "barao", "room cannot interrupt boss")
    audio.PlayMusic("last-manilha")
    expect(audio, "barao", "tension cannot interrupt boss")
    audio.BeginRoomMusic("madrid_salon")
    expect(audio, "madrid", "new room releases boss")
    audio.PlayMenuMusic()
    expect(audio, "madrid", "return uses menu selection")
    settings.set("MusicTrack", 0)
    audio.RefreshTrack()
    expect(audio, "menu", "automatic menu theme")
    var stage = load("res://core/visuals/TableStage.cs").new()
    root.add_child(stage)
    stage.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    stage.SetRoomTheme("mexico_recuerdos")
    await create_timer(2).timeout
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png("C:/workspace/multigame/docs/audio-night-qa/mexico.png")
    stage.SetLookAngles(0, -.20)
    await create_timer(.5).timeout
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png("C:/workspace/multigame/docs/audio-night-qa/mexico-up.png")
    print("AUDIO_NIGHT_QA ", "PASS" if failures.is_empty() else "FAIL", " ", failures)
    await load("res://tools/qa_teardown.gd").finish(self)
    quit(0 if failures.is_empty() else 1)
