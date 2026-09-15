extends SceneTree

func _init():
    var ids = ['nina', 'bento', 'corvo', 'onca', 'iara', 'zeca', 'barao', 'dama']
    for id in ids:
        var path = 'res://assets/models/club/' + id + '.glb'
        var packed = load(path)
        var model = packed.instantiate()
        var meshes = []
        for m in model.find_children('*', 'MeshInstance3D', true, false):
            meshes.append(m)
        for m in meshes:
            for s in range(m.mesh.get_surface_count()):
                var mat = m.mesh.surface_get_material(s)
                if mat and mat.resource_name.begins_with(id + '_skin'):
                    var c = mat.albedo_color
                    var diff = max(c.r, max(c.g, c.b)) - min(c.r, min(c.g, c.b))
                    print(id, ' mat=', mat.resource_name, ' albedo=', c, ' diff=', diff)
        model.free()
    quit(0)
