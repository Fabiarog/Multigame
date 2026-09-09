extends SceneTree

func _init():
    var m = load("res://assets/models/club/aki.glb").instantiate()
    var meshes = m.find_children("*", "MeshInstance3D", true, false)
    for mi in meshes:
        var mesh_inst = mi as MeshInstance3D
        var mesh = mesh_inst.mesh
        print("Mesh: ", mesh_inst.name, " surfaces: ", mesh.get_surface_count())
        for s in range(mesh.get_surface_count()):
            var mat = mesh.surface_get_material(s)
            var mat_name = mat.resource_name if mat else "NULL"
            var albedo_tex = null
            if mat is StandardMaterial3D:
                albedo_tex = (mat as StandardMaterial3D).albedo_texture
            elif mat is ORMMaterial3D:
                albedo_tex = (mat as ORMMaterial3D).albedo_texture
            print("  Surface ", s, ": name='", mat_name, "' type=", mat.get_class() if mat else "none", " albedo_texture=", albedo_tex)
    m.free()
    quit(0)
