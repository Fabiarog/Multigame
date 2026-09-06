"""Patch 8, executed INSIDE Blender through tools/blender_bridge.py execute_code.
Appends the user's source scenes without touching the open scene or original .blend files.
Rebuild base characters first, then run this refinement to reproduce the runtime assets.
"""
import bpy
import math
import runpy
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
helpers = runpy.run_path(str(ROOT / 'tools/build_blender_cast.py'))
shape, mat, empty, animate = [helpers[k] for k in ('shape', 'mat', 'empty', 'animate')]
OUT = ROOT / 'assets/models/club'
SOURCE = ROOT / 'art/blender'
CLIPS = ['entrance', 'truco', 'victory', 'boss_intro', 'flourish']
original_scene = bpy.context.window.scene
report = []

def hierarchy(obj):
    return [obj] + [desc for child in obj.children for desc in hierarchy(child)]

def export(root, ident):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in hierarchy(root):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(OUT / f'{ident}.glb'), export_format='GLB',
        use_selection=True, use_active_scene=True, export_current_frame=True,
        export_animation_mode='NLA_TRACKS', export_force_sampling=True,
        export_frame_range=True)

def pose(obj, clip, rotations, offsets=None):
    frames = [1, 12, 22, 34, 48]
    offsets = offsets or [(0, 0, 0)] * 5
    animate(obj, clip, list(zip(frames, offsets, rotations)))

def refine(ident):
    with bpy.data.libraries.load(str(SOURCE / f'{ident}.blend'), link=False) as (src, dst):
        dst.scenes = [src.scenes[0]]
    scene = dst.scenes[0]
    scene.name = f'MCP8_{ident}'
    bpy.context.window.scene = scene
    root = next(o for o in scene.objects if o.parent is None and o.type == 'EMPTY')
    parts = {o.name.split('.')[0]: o for o in hierarchy(root) if o.type == 'EMPTY'}
    body, head, left, right = [parts[k] for k in ('Body', 'Head', 'ArmL', 'ArmR')]
    scene.frame_set(48)
    rest_positions = {obj: obj.location.copy() for obj in (body, head, left, right)}
    for obj in (body, head, left, right):
        obj.animation_data_clear()
        obj.rotation_euler = (0, 0, 0)
        obj.location = rest_positions[obj]
    for mesh in [obj for obj in hierarchy(root) if obj.type == 'MESH']:
        bpy.ops.object.select_all(action='DESELECT')
        mesh.select_set(True)
        bpy.context.view_layer.objects.active = mesh
        for modifier in list(mesh.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
    gold = mat(f'{ident}_MCP8_brass', '#dcb76b', metal=.7, rough=.3)
    ink = mat(f'{ident}_MCP8_ink', '#182534', rough=.6)
    if ident == 'corvo':
        # Tapered feather cuffs follow each articulated wing, never a separate draw per feather.
        for arm, sign in ((left, -1), (right, 1)):
            for i in range(3):
                shape('WingCuff', (sign * (.12 + i * .035), .025, -.43 - i * .065),
                    (.055, .10, .20 - i * .02), ink, arm, 'ico', rot=(0, sign * -.25, 0))
        shape('EmeraldTiePin', (-.16, -.32, 1.12), (.055, .028, .085), gold, body, 'ico')
    if ident == 'onca':
        for side in (-1, 1):
            for i in range(3):
                shape('MuzzleFreckle', (side * (.09 + i * .055), -.515 + i * .018, .06 + (i % 2) * .035),
                    (.014, .012, .014), ink, head, 'ico')
        shape('LapelClasp', (.23, -.29, 1.10), (.06, .025, .07), gold, body, 'ico')

    # Join only new accents into each existing articulated mesh, keeping the four-part rig.
    for part in (body, head, left, right):
        meshes = [o for o in part.children if o.type == 'MESH']
        if len(meshes) > 1:
            bpy.ops.object.select_all(action='DESELECT')
            for obj in meshes: obj.select_set(True)
            bpy.context.view_layer.objects.active = meshes[0]
            bpy.ops.object.join()

    zero = (0, 0, 0)
    for clip in CLIPS:
        # Every action settles exactly into its rest pose before the next gesture.
        b = [zero, (.03, 0, -.03), (.09, 0, .03), (.02, 0, 0), zero]
        h = [zero, (-.06, 0, -.15), (.08, 0, .12), (.02, 0, .03), zero]
        l = [zero, (.16, -.12, -.12), (.32, -.16, .1), (.1, 0, 0), zero]
        r = [zero, (-.16, .12, .12), (.25, .16, -.1), (.1, 0, 0), zero]
        if ident == 'corvo':
            # Measured pince-nez salute; the unlock opens both feathered cuffs.
            h = [zero, (0, -.08, -.22), (.12, .04, .16), (.05, 0, .08), zero]
            r = [zero, (-.8, -.25, -.55), (-1.45, -.45, -.6), (-.65, -.15, -.35), zero]
            if clip in ('flourish', 'victory'):
                l = [zero, (-.25, -.4, -.2), (-.65, -.85, -.35), (-.2, -.4, -.1), zero]
                r = [zero, (-.25, .4, .2), (-.65, .85, .35), (-.2, .4, .1), zero]
            if clip == 'truco':
                r = [zero, (-.9, -.15, -.35), (.85, -.2, -.25), (.65, -.12, -.18), zero]
        elif ident == 'onca':
            # A shoulder coil, then a forceful right-paw challenge.
            b = [zero, (-.06, 0, -.12), (.18, 0, .09), (.10, 0, .035), zero]
            h = [zero, (-.08, 0, -.1), (.17, 0, .1), (.08, 0, 0), zero]
            r = [zero, (-.65, .12, .2), (1.05, -.15, -.25), (.72, -.08, -.14), zero]
            l = [zero, (.08, -.1, -.05), (.42, -.2, .08), (.32, -.1, 0), zero]
            if clip in ('victory', 'flourish'):
                r = [zero, (-.45, .15, .1), (-1.7, .4, .2), (-1.3, .2, .1), zero]
                h = [zero, (-.08, 0, -.14), (-.2, 0, .12), (-.08, 0, 0), zero]
        elif ident == 'barao':
            # A broad wing reveal and a regal bow, distinct from the playable Corvo.
            l = [zero, (.1, -.3, -.2), (.3, -1.0, -.55), (.4, -.6, -.3), zero]
            r = [zero, (.1, .3, .2), (.3, 1.0, .55), (.4, .6, .3), zero]
            h = [zero, (-.22, 0, -.10), (.20, 0, .12), (.12, 0, 0), zero]
        else:
            # The Dama studies both sides before a serpentine forward challenge.
            b = [zero, (-.04, .08, -.12), (.15, -.08, .12), (.08, .04, -.04), zero]
            h = [zero, (-.06, .12, -.30), (.18, -.10, .28), (.12, .04, -.10), zero]
            l = [zero, (.2, -.3, -.2), (.65, -.55, -.2), (.4, -.25, -.1), zero]
            r = [zero, (-.2, .4, .15), (.85, .2, -.2), (.4, .15, 0), zero]
        if clip == 'entrance':
            # Keep the welcome restrained while the game slides the actor to their seat.
            b = [tuple(v * .45 for v in p) for p in b]
            l = [tuple(v * .35 for v in p) for p in l]
            r = [tuple(v * .35 for v in p) for p in r]
        for obj, rotations in ((body, b), (head, h), (left, l), (right, r)):
            pose(obj, clip, rotations)
    scene.frame_start, scene.frame_end, scene.render.fps = 1, 48, 24
    scene.frame_set(1)
    export(root, ident)
    scene.render.resolution_x, scene.render.resolution_y = 480, 600
    scene.render.filepath = str(OUT / f'{ident}_3d.png')
    bpy.data.libraries.write(str(SOURCE / f'{ident}_mcp.blend'), {scene}, fake_user=True)
    bpy.ops.render.render(write_still=True)
    report.append({'character': ident, 'clips': CLIPS, 'meshes': sum(o.type == 'MESH' for o in hierarchy(root))})

def clock():
    scene = bpy.data.scenes.new('MCP8_ClubClock')
    bpy.context.window.scene = scene
    root = empty('ClubClock')
    brass = mat('ClockBrass', '#cba76b', metal=.72, rough=.34)
    wood = mat('ClockWalnut', '#281811', rough=.68)
    face = mat('ClockIvory', '#f1dfb2', rough=.75)
    dark = mat('ClockInk', '#243832', rough=.7)
    shape('Pedestal', (0, .08, -1.08), (.42, .26, .48), wood, root, 'cube', smooth=False)
    shape('PedestalFoot', (0, .08, -1.52), (.53, .32, .065), brass, root, 'cube', smooth=False)
    shape('PedestalCap', (0, .08, -.63), (.53, .32, .06), brass, root, 'cube', smooth=False)
    # Front is -Y in Blender; glTF maps this toward the table in Godot.
    shape('Case', (0, .06, .2), (.57, .16, .8), wood, root, 'cube', smooth=False)
    shape('Dial', (0, -.12, .48), (.48, .48, .035), face, root, 'cylinder', rot=(math.pi/2, 0, 0))
    shape('Bezel', (0, -.16, .48), (.48, .48, .08), brass, root, 'torus', rot=(math.pi/2, 0, 0))
    for i in range(12):
        a = math.tau * i / 12
        shape('Hour', (.36 * math.sin(a), -.175, .48 + .36 * math.cos(a)), (.017, .012, .047), dark, root, 'cube', rot=(0, a, 0), smooth=False)
    shape('HourHand', (-.07, -.20, .53), (.11, .015, .022), dark, root, 'cube', rot=(0, .55, 0), smooth=False)
    shape('MinuteHand', (.07, -.21, .61), (.019, .016, .17), dark, root, 'cube', rot=(0, .45, 0), smooth=False)
    pendulum = empty('Pendulum', (0, -.13, .10), root)
    shape('Stem', (0, 0, -.23), (.018, .02, .23), brass, pendulum, 'cylinder')
    shape('Bob', (0, 0, -.46), (.11, .035, .11), brass, pendulum, 'ico')
    animate(pendulum, 'idle', [(1,(0,0,0),(0,-.12,0)), (25,(0,0,0),(0,.12,0)), (49,(0,0,0),(0,-.12,0))])
    for part in (root, pendulum):
        meshes = [o for o in part.children if o.type == 'MESH']
        bpy.ops.object.select_all(action='DESELECT')
        for obj in meshes: obj.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.join()
    scene.frame_start, scene.frame_end, scene.render.fps = 1, 49, 24
    scene.frame_set(1)
    export(root, 'club_clock')
    bpy.data.libraries.write(str(SOURCE / 'club_clock.blend'), {scene}, fake_user=True)
    report.append({'prop':'club_clock', 'clips':['idle'], 'meshes':2})

try:
    for ident in ('corvo', 'onca', 'barao', 'dama'):
        refine(ident)
    # The original export used frame 0, outside every NLA strip. Blender zeroed
    # the articulated pivots there, so static/reduced-motion actors lost their heads.
    for ident in ('nina', 'bento', 'iara', 'zeca'):
        with bpy.data.libraries.load(str(SOURCE / f'{ident}.blend'), link=False) as (src, dst):
            dst.scenes = [src.scenes[0]]
        scene = dst.scenes[0]
        scene.name = f'MCP8_Rest_{ident}'
        bpy.context.window.scene = scene
        scene.frame_set(48)
        root = next(o for o in scene.objects if o.parent is None and o.type == 'EMPTY')
        export(root, ident)
        report.append({'character':ident, 'rest_pose_fixed':True, 'clips':CLIPS})
    clock()
    (OUT / 'mcp-refinements.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))
finally:
    bpy.context.window.scene = original_scene
