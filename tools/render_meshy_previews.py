import bpy
import os
import math

folder = r'c:\workspace\multigame\assets\modelos 3d detalhados'
blend_files = [f for f in os.listdir(folder) if f.endswith('.blend')]
out_dir = r'c:\workspace\multigame\temp\meshy_previews'
os.makedirs(out_dir, exist_ok=True)

for bf in blend_files:
    path = os.path.join(folder, bf)
    bpy.ops.wm.open_mainfile(filepath=path)
    
    mesh_obj = None
    for o in bpy.data.objects:
        if o.type == 'MESH':
            mesh_obj = o
            break
    if not mesh_obj:
        continue

    # Clean cameras and lights
    for o in list(bpy.data.objects):
        if o.type in ('CAMERA', 'LIGHT'):
            bpy.data.objects.remove(o, do_unlink=True)

    # Use Workbench or Eevee
    bpy.context.scene.render.engine = 'BLENDER_EEVEE'
    
    # World background
    if not bpy.data.worlds:
        world = bpy.data.worlds.new('World')
        bpy.context.scene.world = world
    world = bpy.context.scene.world
    world.use_nodes = True
    bg_node = world.node_tree.nodes.get('Background')
    if bg_node:
        bg_node.inputs[0].default_value = (0.2, 0.22, 0.25, 1.0)
        bg_node.inputs[1].default_value = 1.0

    # Bounding box center
    bbox = [mesh_obj.matrix_world @ mathutils.Vector(b) for b in mesh_obj.bound_box] if 'mathutils' in globals() else mesh_obj.bound_box
    import mathutils
    bbox_world = [mesh_obj.matrix_world @ mathutils.Vector(b) for b in mesh_obj.bound_box]
    center = sum(bbox_world, mathutils.Vector((0,0,0))) / 8.0
    dim = mesh_obj.dimensions
    max_dim = max(dim.x, dim.y, dim.z)

    # Set up Camera (Front-Quarter View)
    cam_data = bpy.data.cameras.new(name='PreviewCam')
    cam_data.lens = 50
    cam_obj = bpy.data.objects.new('PreviewCam', cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    cam_dist = max_dim * 1.8
    cam_obj.location = (center.x + cam_dist * 0.5, center.y - cam_dist * 0.86, center.z + 0.2)
    
    # Track to constraint
    track = cam_obj.constraints.new(type='TRACK_TO')
    track.target = mesh_obj
    track.track_axis = 'TRACK_NEGATIVE_Z'
    track.up_axis = 'UP_Y'
    
    # Add strong Sun light
    light_data = bpy.data.lights.new(name='Sun', type='SUN')
    light_data.energy = 5.0
    light_obj = bpy.data.objects.new('Sun', light_data)
    light_obj.location = (center.x + 3, center.y - 3, center.z + 5)
    bpy.context.scene.collection.objects.link(light_obj)
    l_track = light_obj.constraints.new(type='TRACK_TO')
    l_track.target = mesh_obj
    l_track.track_axis = 'TRACK_NEGATIVE_Z'
    l_track.up_axis = 'UP_Y'

    # Fill Sun
    fill_data = bpy.data.lights.new(name='FillSun', type='SUN')
    fill_data.energy = 2.5
    fill_obj = bpy.data.objects.new('FillSun', fill_data)
    fill_obj.location = (center.x - 3, center.y + 3, center.z + 2)
    bpy.context.scene.collection.objects.link(fill_obj)
    f_track = fill_obj.constraints.new(type='TRACK_TO')
    f_track.target = mesh_obj
    f_track.track_axis = 'TRACK_NEGATIVE_Z'
    f_track.up_axis = 'UP_Y'

    bpy.context.scene.render.resolution_x = 640
    bpy.context.scene.render.resolution_y = 800
    bpy.context.scene.render.film_transparent = False

    out_file = os.path.join(out_dir, bf.replace('.blend', '.png'))
    bpy.context.scene.render.filepath = out_file
    bpy.ops.render.render(write_still=True)
    print(f'Rendered: {out_file}')

