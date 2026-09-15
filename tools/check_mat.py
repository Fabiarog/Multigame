import bpy
bf = r'c:\workspace\multigame\assets\modelos 3d detalhados\Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend'
bpy.ops.wm.open_mainfile(filepath=bf)

for m in bpy.data.materials:
    print('Material:', m.name, 'use_nodes:', m.use_nodes)
    if m.node_tree:
        for n in m.node_tree.nodes:
            print('  Node:', n.name, n.type)
            if n.type == 'TEX_IMAGE' and n.image:
                print('    Image:', n.image.name, n.image.size[:])

for o in bpy.data.objects:
    print('Obj:', o.name, o.type, 'loc:', o.location, 'rot:', o.rotation_euler, 'scale:', o.scale, 'dim:', o.dimensions)
    print('  bbox:', [list(b) for b in o.bound_box])
