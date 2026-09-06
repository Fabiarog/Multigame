"""Non-destructive quality pass over the same cast: anatomy subdivision and tailored bevels.
Run after refine_club_mcp.py, through blender_bridge.py or Blender background Python.
Original base sources stay intact; the finished sources use the _mcp.blend suffix.
"""
import bpy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'art/blender'
OUT = ROOT / 'assets/models/club'
original_scene = bpy.context.window.scene
report = []

def descendants(root):
    return [root] + [item for child in root.children for item in descendants(child)]

def active(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def polish(ident):
    source = SOURCE / f'{ident}_mcp.blend'
    if ident not in ('corvo','onca','barao','dama'):
        source = SOURCE / f'{ident}.blend'
    with bpy.data.libraries.load(str(source), link=False) as (src, dst):
        dst.scenes = [src.scenes[0]]
    scene = dst.scenes[0]
    scene.name = f'Polish_{ident}'
    bpy.context.window.scene = scene
    root = next(o for o in scene.objects if o.parent is None and o.type == 'EMPTY')
    # A rerun should not repeatedly subdivide sources already polished by this script.
    if root.get('club_polish_v1'):
        print(f'{ident}: already polished; regenerate refinement before changing quality settings.')
        return
    scene.frame_set(48)
    before = sum(len(o.data.polygons) for o in descendants(root) if o.type == 'MESH')
    for part in [o for o in descendants(root) if o.type == 'EMPTY' and o != root]:
        meshes = [o for o in part.children if o.type == 'MESH']
        if not meshes: continue
        for obj in meshes:
            active(obj)
            for modifier in list(obj.modifiers):
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.mesh.separate(type='MATERIAL')
            bpy.ops.object.mode_set(mode='OBJECT')
        pieces = [o for o in part.children if o.type == 'MESH']
        for obj in pieces:
            active(obj)
            if not obj.data.polygons: continue
            material = obj.data.materials[obj.data.polygons[0].material_index]
            name = material.name.lower() if material else ''
            anatomy = any(key in name for key in ('_skin', 'whitefur', 'beak', 'earpink', 'brighteyes', 'pupil', 'glint', 'catnose', 'bentohair'))
            if anatomy and len(obj.data.polygons) < 12000:
                # Only organic surfaces: lapels, spectacles and buttons keep their shapes.
                modifier = obj.modifiers.new('Anatomy refinement', 'SUBSURF')
                modifier.subdivision_type = 'CATMULL_CLARK'
                modifier.levels = modifier.render_levels = 1
                bpy.ops.object.modifier_apply(modifier=modifier.name)
                for polygon in obj.data.polygons: polygon.use_smooth = True
            else:
                modifier = obj.modifiers.new('Tailored edge highlights', 'BEVEL')
                modifier.width = .006
                modifier.segments = 3
                modifier.limit_method = 'ANGLE'
                modifier.angle_limit = .55
                modifier.use_clamp_overlap = True
                modifier.harden_normals = True
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            if material:
                shader = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
                if shader and ('_coat' in name or 'silklapel' in name or '_satin' in name):
                    shader.inputs['Roughness'].default_value = .82 if '_coat' in name else .48
        bpy.ops.object.select_all(action='DESELECT')
        for obj in pieces: obj.select_set(True)
        bpy.context.view_layer.objects.active = pieces[0]
        bpy.ops.object.join()
        bpy.context.object.name = part.name.split('.')[0] + 'Mesh'
    root['club_polish_v1'] = True
    scene.frame_set(48)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in descendants(root): obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(OUT / f'{ident}.glb'), export_format='GLB',
        use_selection=True, use_active_scene=True, export_current_frame=True,
        export_animation_mode='NLA_TRACKS', export_force_sampling=True, export_frame_range=True)
    after = sum(len(o.data.polygons) for o in descendants(root) if o.type == 'MESH')
    scene.render.resolution_x, scene.render.resolution_y = 640, 800
    scene.render.filepath = str(OUT / f'{ident}_3d.png')
    bpy.data.libraries.write(str(SOURCE / f'{ident}_mcp.blend'), {scene}, fake_user=True)
    bpy.ops.render.render(write_still=True)
    report.append({'id':ident, 'source_faces':before, 'polished_faces':after, 'mesh_groups':4})

try:
    # Individual invocations can set CAST_IDS in the MCP execution namespace.
    for ident in globals().get('CAST_IDS', ['corvo','onca','barao','dama','nina','bento','iara','zeca']):
        polish(ident)
    print(json.dumps(report))
    manifest = OUT / 'cast-polish.json'
    previous = json.loads(manifest.read_text()) if manifest.exists() else []
    by_id = {entry['id']:entry for entry in previous}
    by_id.update({entry['id']:entry for entry in report})
    manifest.write_text(json.dumps(list(by_id.values()),indent=2),encoding='utf-8')
finally:
    bpy.context.window.scene = original_scene
