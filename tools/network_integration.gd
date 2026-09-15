extends SceneTree
func _initialize():
    call_deferred("run")
func run():
    if not OS.has_environment("MULTIGAME_NET_QA"):
        quit(2)
        return
    root.add_child(load("res://tools/NetworkIntegrationChecks.cs").new())
