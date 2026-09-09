extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    var meshes = m.find_children("*", "MeshInstance3D", true, false)
    for mi in meshes:
        var mesh_inst = mi as MeshInstance3D
        var mesh = mesh_inst.mesh
        for s in range(mesh.get_surface_count()):
            var mat = mesh.surface_get_material(s) as BaseMaterial3D
            if mat:
                print("Surface ", s, " (", mat.resource_name, "):")
                print("  transparency: ", mat.transparency, " cull_mode: ", mat.cull_mode, " albedo_color: ", mat.albedo_color)
                print("  albedo_texture: ", mat.albedo_texture)
    m.free()
    quit(0)
