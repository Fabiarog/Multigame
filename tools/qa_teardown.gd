extends RefCounted
## Let native nodes and managed resource wrappers settle before engine shutdown.
static func finish(tree: SceneTree) -> void:
	if is_instance_valid(tree.current_scene):
		tree.current_scene.queue_free()
	await tree.process_frame
	await tree.process_frame
	var children = tree.root.get_children()
	children.reverse()
	for node in children:
		if is_instance_valid(node): node.queue_free()
	await tree.process_frame
	await tree.process_frame
	var cleanup = load("res://tools/GameplayChecks.cs").new()
	cleanup.ReleaseManagedResources()
	cleanup.free()
	await tree.process_frame
