"""Local Blender MCP: targeted face topology and architectural edge highlights.
Keeps the user's base sources, original open scene, animation hierarchy and palettes.
Each output has a separate *_fidelity.blend source; reruns start from saved inputs.
"""
import bpy
import bmesh
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art/blender'
OUT = ROOT / 'assets/models/club'
original_scene = bpy.context.window.scene
report = []

def active(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def descendants(obj):
    return [obj] + [nested for child in obj.children for nested in descendants(child)]

def export(scene, ident, animated=False):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in scene.objects:
        if obj.type not in ('LIGHT','CAMERA'): obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / f'{ident}.glb'), export_format='GLB',
        use_selection=True, use_active_scene=True, export_current_frame=True,
        export_animations=animated, export_animation_mode='NLA_TRACKS', export_force_sampling=True)
    bpy.data.libraries.write(str(ART / f'{ident}_fidelity.blend'), {scene}, fake_user=True)

def character(ident):
    with bpy.data.libraries.load(str(ART / f'{ident}_mcp.blend'), link=False) as (src, dst):
        dst.scenes = [src.scenes[0]]
    scene = dst.scenes[0]
    scene.name = f'Fidelity_{ident}'
    bpy.context.window.scene = scene
    scene.frame_set(48)
    root = next(o for o in scene.objects if o.parent is None and o.type == 'EMPTY')
    before = sum(len(o.data.polygons) for o in descendants(root) if o.type == 'MESH')
    head = next(o for o in descendants(root) if o.type == 'EMPTY' and o.name.startswith('Head'))
    for mesh in [o for o in head.children if o.type == 'MESH']:
        active(mesh)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.separate(type='MATERIAL')
        bpy.ops.object.mode_set(mode='OBJECT')
    pieces = [o for o in head.children if o.type == 'MESH']
    for mesh in pieces:
        slot = mesh.data.polygons[0].material_index if mesh.data.polygons else 0
        name = mesh.data.materials[slot].name.lower() if mesh.data.materials else ''
        # Small face features benefit most in close-ups; don't inflate hidden bodies.
        organic = any(token in name for token in ('_skin','beak','whitefur','catnose','earpink','brighteyes','bentohair'))
        if organic and len(mesh.data.polygons) < 18000:
            active(mesh)
            mod = mesh.modifiers.new('Face silhouette refinement', 'SUBSURF')
            mod.levels = mod.render_levels = 1
            bpy.ops.object.modifier_apply(modifier=mod.name)
            for p in mesh.data.polygons: p.use_smooth = True
    bpy.ops.object.select_all(action='DESELECT')
    for mesh in pieces: mesh.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    bpy.context.object.name = 'HeadMesh'
    after = sum(len(o.data.polygons) for o in descendants(root) if o.type == 'MESH')
    export(scene, ident, True)
    scene.render.resolution_x, scene.render.resolution_y = 640, 800
    scene.render.filepath = str(OUT / f'{ident}_3d.png')
    bpy.ops.render.render(write_still=True)
    report.append({'id':ident,'before_faces':before,'after_faces':after,'mesh_groups':4})

def architecture(ident):
    inputs = ART / 'fidelity-inputs'
    inputs.mkdir(exist_ok=True)
    source = inputs / f'{ident}.glb'
    if not source.exists(): shutil.copy2(OUT / source.name, source)
    scene = bpy.data.scenes.new(f'Fidelity_{ident}')
    bpy.context.window.scene = scene
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes = [o for o in scene.objects if o.type == 'MESH']
    before = sum(len(o.data.polygons) for o in meshes)
    for obj in meshes:
        # Keep floor/rug planar. Round millwork and furniture edges to catch light.
        if any(token in obj.name for token in ('Floor','Rug','Canvas','BackWall','SideWall')): continue
        active(obj)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        # glTF splits vertices along hard normals/UV seams. Weld coincident
        # positions before beveling, otherwise every edge is an open boundary.
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=.000001)
        bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
        mesh.to_mesh(obj.data)
        mesh.free()
        bevel = obj.modifiers.new('Crafted architectural edges', 'BEVEL')
        bevel.width = .012 if 'Pilaster' in obj.name else .007
        bevel.segments = 4
        bevel.limit_method = 'ANGLE'
        bevel.angle_limit = .45
        bevel.use_clamp_overlap = True
        bevel.harden_normals = True
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        for polygon in obj.data.polygons: polygon.use_smooth = True
        normals = obj.modifiers.new('Stable flat face shading', 'WEIGHTED_NORMAL')
        normals.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=normals.name)
    after = sum(len(o.data.polygons) for o in meshes)
    export(scene, ident)
    report.append({'id':ident,'before_faces':before,'after_faces':after,'mesh_groups':len(meshes)})

try:
    for ident in globals().get('FIDELITY_CAST', ['corvo','onca','nina','bento','iara','zeca','barao','dama']): character(ident)
    for ident in globals().get('FIDELITY_ROOMS', ['room_classic_club','room_barao_lounge','room_dama_salon','room_cyber_casino','club_chair']): architecture(ident)
    manifest = OUT / 'fidelity-pass.json'
    previous = json.loads(manifest.read_text()) if manifest.exists() else []
    merged = {entry['id']:entry for entry in previous}
    merged.update({entry['id']:entry for entry in report})
    manifest.write_text(json.dumps(list(merged.values()),indent=2),encoding='utf-8')
    print(json.dumps(report))
finally:
    bpy.context.window.scene = original_scene
