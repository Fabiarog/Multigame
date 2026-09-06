"""Blender 5.2: Stylized Triple-A (AAA) original club cast.
Generates high-poly continuous organic meshes, tailored clothing, PBR velvet & metals,
expressive facial features, Bezier physics animations, GLBs, and studio portraits.
Run: & "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python tools/build_blender_cast.py
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'assets/models/club'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'art/blender'
SOURCE.mkdir(parents=True, exist_ok=True)

# 8 Club Cast definitions: (ident, species, skin_hex, coat_hex, accent_hex, secondary_hex)
CAST = [
    ('nina',  'human',    '#c48b62', '#165e3e', '#dfb15b', '#f2ece1'), # Nina: Inventor, emerald vest, gold, linen
    ('bento', 'human',    '#ad6e45', '#7a3224', '#cda052', '#f5f0e6'), # Bento: Host, burgundy/leather, brass, linen
    ('corvo', 'crow',     '#18202c', '#154a35', '#dfb15b', '#101620'), # Seu Corvo: Raven, emerald damask, gold, midnight
    ('onca',  'jaguar',   '#cb8938', '#681d33', '#dfb15b', '#f4ecd8'), # Dona Onça: Jaguar, maroon velvet, gold, cream
    ('iara',  'capybara', '#966d48', '#1a6354', '#e26b8e', '#f5f0e6'), # Iara: Capybara, teal silk, pink lily, ivory
    ('zeca',  'fox',      '#c25524', '#213a63', '#dfb15b', '#f5eee4'), # Zeca: Fox, navy vest, gold watch, white ruff
    ('barao', 'owl',      '#3f324c', '#271b37', '#dfb15b', '#fac44c'), # Barão: Horned Owl, midnight purple, gold, amber
    ('dama',  'serpent',  '#cca038', '#7c1524', '#c8182e', '#dfb15b')  # Dama: Queen Cobra, crimson velvet, rubies, gold
]

def mat(name, hex_color, metal=0.0, rough=0.65, spec=0.5, emit_hex=None, emit_strength=0.0):
    c = tuple(int(hex_color[i:i+2], 16) / 255 for i in (1, 3, 5)) + (1,)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    if p:
        linear_c = tuple((v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in c[:3]) + (1,)
        p.inputs['Base Color'].default_value = linear_c
        p.inputs['Metallic'].default_value = metal
        p.inputs['Roughness'].default_value = rough
        if 'Specular IOR Level' in p.inputs:
            p.inputs['Specular IOR Level'].default_value = spec
        elif 'Specular' in p.inputs:
            p.inputs['Specular'].default_value = spec
        if emit_hex and 'Emission Color' in p.inputs:
            ec = tuple(int(emit_hex[i:i+2], 16) / 255 for i in (1, 3, 5)) + (1,)
            p.inputs['Emission Color'].default_value = ec
            if 'Emission Strength' in p.inputs:
                p.inputs['Emission Strength'].default_value = emit_strength
    return m

def empty(name, pos=(0, 0, 0), parent=None):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.parent = parent
    o.location = pos
    return o

def shape(name, pos, scale, material, parent, kind='ico', rot=(0, 0, 0), smooth=True, subsurf=0, bevel=0.0):
    if kind == 'ico':
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1)
    elif kind == 'cone':
        bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=1, radius2=0, depth=2)
    elif kind == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=1, depth=2)
    elif kind == 'torus':
        bpy.ops.mesh.primitive_torus_add(major_segments=28, minor_segments=10, major_radius=1, minor_radius=0.25)
    else:
        bpy.ops.mesh.primitive_cube_add(size=2)

    o = bpy.context.object
    o.name = name
    o.parent = parent
    o.location = pos
    o.scale = scale
    o.rotation_euler = rot

    if smooth and kind in ('ico', 'cylinder', 'cone', 'torus'):
        for poly in o.data.polygons:
            poly.use_smooth = True

    if bevel > 0:
        b = o.modifiers.new(name="Bevel", type='BEVEL')
        b.width = bevel
        b.segments = 2

    if subsurf > 0:
        s = o.modifiers.new(name="Subsurf", type='SUBSURF')
        s.levels = subsurf
        s.render_levels = subsurf

    o.data.materials.append(material)
    return o

def animate(obj, clip, keys):
    base_pos = obj.location.copy()
    base_rot = obj.rotation_euler.copy()
    obj.animation_data_create()
    obj.animation_data.action = None

    for frame, loc, rot in keys:
        obj.location = base_pos + Vector(loc)
        obj.rotation_euler = tuple(base_rot[i] + rot[i] for i in range(3))
        obj.keyframe_insert('location', frame=frame)
        obj.keyframe_insert('rotation_euler', frame=frame)

    action = obj.animation_data.action
    action.name = f"{obj.name}_{clip}"

    track = obj.animation_data.nla_tracks.new()
    track.name = clip
    strip = track.strips.new(clip, 1, action)
    strip.extrapolation = 'NOTHING'

    obj.animation_data.action = None
    obj.location = base_pos
    obj.rotation_euler = base_rot

def build(index, ident, species, skinhex, coathex, accenthex, sechex):
    # Clean active Blender scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)

    # Core High-Fidelity PBR Materials
    skin_mat = mat(f'{ident}_skin', skinhex, rough=0.60)
    coat_mat = mat(f'{ident}_coat', coathex, rough=0.82) # Velvet coat
    satin_mat = mat(f'{ident}_satin', coathex, rough=0.28) # Silk/satin lapels
    accent_mat = mat(f'{ident}_accent', accenthex, metal=0.90, rough=0.18, spec=1.0) # Gold / Brass
    linen_mat = mat('LinenShirt', sechex, rough=0.65) # Shirt linen
    leather_mat = mat('PolishedLeather', '#141820', rough=0.25)
    amber_eyes = mat('AmberEye', '#f5b838', rough=0.08, spec=1.0, emit_hex='#f5b838', emit_strength=0.15)
    pupil_mat = mat('Pupil', '#05070a', rough=0.05, spec=1.0)
    glint_mat = mat('Glint', '#ffffff', rough=0.02, spec=1.0)
    ruby_mat = mat('RubyGem', '#c81428', metal=0.35, rough=0.08, spec=1.0, emit_hex='#a00818', emit_strength=0.25)
    emerald_mat = mat('EmeraldGem', '#167a48', metal=0.35, rough=0.08, spec=1.0, emit_hex='#0d5830', emit_strength=0.25)
    white_fur = mat('WhiteFur', '#f5f0e8', rough=0.85)

    # Actor Hierarchy
    rig = empty('Actor')
    body = empty('Body', (0, 0, 0), rig)
    head = empty('Head', (0, 0, 1.45), body)
    arm_l = empty('ArmL', (-0.46, 0.02, 1.12), body)
    arm_r = empty('ArmR', (0.46, 0.02, 1.12), body)
    arms = [arm_l, arm_r]

    # --- ANATOMICAL TORSO & TAILORED VEST ---
    # Contoured Torso
    shape('TorsoCore', (0, -0.02, 0.82), (0.48, 0.28, 0.58), coat_mat, body, 'ico', subsurf=1)
    shape('ChestContour', (0, -0.14, 0.96), (0.42, 0.22, 0.38), coat_mat, body, 'ico', subsurf=1)
    # Shirt front bib
    shape('ShirtBib', (0, -0.26, 0.96), (0.21, 0.04, 0.42), linen_mat, body, 'cube', bevel=0.02)
    # Wingtip / Standup shirt collars
    shape('ShirtCollarL', (-0.12, -0.28, 1.30), (0.07, 0.03, 0.09), linen_mat, body, 'cube', rot=(0.15, 0.25, -0.15), bevel=0.01)
    shape('ShirtCollarR', (0.12, -0.28, 1.30), (0.07, 0.03, 0.09), linen_mat, body, 'cube', rot=(0.15, -0.25, 0.15), bevel=0.01)

    # Peaked Waistcoat Lapels
    for side in [-1, 1]:
        shape('VestLapel', (side * 0.23, -0.29, 1.04), (0.12, 0.04, 0.34), satin_mat, body, 'cube', rot=(0.08, side * 0.32, -side * 0.08), bevel=0.02)
        shape('Trouser', (side * 0.23, 0.02, 0.32), (0.19, 0.20, 0.32), coat_mat, body, 'cylinder', subsurf=1)
        shape('Shoe', (side * 0.23, -0.07, 0.09), (0.19, 0.30, 0.10), leather_mat, body, 'ico', subsurf=1)

    # Golden Filigree Buttons
    for z in [0.64, 0.84, 1.04]:
        shape('GoldButton', (0.03, -0.31, z), (0.032, 0.018, 0.032), accent_mat, body, 'cylinder', rot=(math.pi / 2, 0, 0), bevel=0.01)

    # Neckwear
    if species in ('owl', 'crow'):
        shape('BowtieL', (-0.08, -0.31, 1.25), (0.08, 0.035, 0.06), satin_mat, body, 'ico')
        shape('BowtieR', (0.08, -0.31, 1.25), (0.08, 0.035, 0.06), satin_mat, body, 'ico')
        shape('BowtieKnot', (0, -0.32, 1.25), (0.035, 0.03, 0.035), accent_mat, body, 'ico')
    elif species == 'serpent':
        shape('QueenBrooch', (0, -0.29, 1.18), (0.08, 0.025, 0.08), ruby_mat, body, 'ico', subsurf=1)
        shape('BroochFrame', (0, -0.28, 1.18), (0.10, 0.02, 0.10), accent_mat, body, 'torus', rot=(math.pi / 2, 0, 0))
    else:
        shape('CravatSilk', (0, -0.29, 1.16), (0.065, 0.025, 0.16), satin_mat, body, 'cube', bevel=0.01)

    # Suspenders & Signet details for Bento
    if ident == 'bento':
        for side in [-1, 1]:
            shape('LeatherSuspender', (side * 0.17, -0.27, 0.95), (0.038, 0.02, 0.42), mat('SuspenderLeather', '#562818', rough=0.45), body, 'cube', bevel=0.01)
            shape('SuspenderBuckle', (side * 0.17, -0.28, 0.62), (0.042, 0.025, 0.04), accent_mat, body, 'cube', bevel=0.01)

    # Pocket Watch Chain for Zeca & Corvo
    if ident in ('zeca', 'corvo'):
        shape('WatchChainLoop', (0.15, -0.29, 0.74), (0.11, 0.02, 0.07), accent_mat, body, 'torus', rot=(0.35, 0.2, 0.3))

    # Royal Cape for Barão and Dama
    if index >= 6:
        shape('RoyalCape', (0, 0.22, 0.88), (0.66, 0.14, 0.74), mat('CapeVelvet', '#261220' if index == 7 else '#1c1426', rough=0.88), body, 'cube', bevel=0.03)
        shape('CapeBraid', (0, 0.12, 1.34), (0.56, 0.12, 0.14), accent_mat, body, 'torus')

    # --- ARTICULATED ARMS, FOREARMS, CUFFS & HANDS ---
    for j, arm in enumerate(arms):
        side_f = -1 if j == 0 else 1
        # Structured Shoulder
        shape('ShoulderJoint', (0, 0, 0), (0.18, 0.20, 0.20), coat_mat, arm, 'ico', subsurf=1)
        # Upper Arm
        shape('UpperArm', (0, 0, -0.24), (0.16, 0.18, 0.26), coat_mat, arm, 'cylinder', subsurf=1)
        # Forearm with elbow articulation
        shape('Forearm', (0, -0.06, -0.48), (0.15, 0.16, 0.24), coat_mat, arm, 'cylinder', rot=(0.24, 0, 0), subsurf=1)
        # Shirt Cuff
        shape('ShirtCuff', (0, -0.10, -0.66), (0.16, 0.15, 0.08), linen_mat, arm, 'cylinder', rot=(0.24, 0, 0), bevel=0.01)
        shape('Cufflink', (side_f * 0.11, -0.10, -0.66), (0.024, 0.024, 0.024), accent_mat, arm, 'ico')
        # Hand / Paw
        h_mat = mat('CrowClaw', '#12161f', rough=0.35) if species == 'crow' else skin_mat
        shape('Palm', (0, -0.16, -0.76), (0.15, 0.16, 0.14), h_mat, arm, 'ico', subsurf=1)

        # Bento's Gold Watch
        if ident == 'bento' and j == 0:
            shape('WatchStrap', (0, -0.10, -0.66), (0.17, 0.16, 0.055), mat('WatchStrap', '#3a1c10', rough=0.4), arm, 'cylinder')
            shape('WatchBezel', (-0.13, -0.10, -0.66), (0.042, 0.042, 0.038), accent_mat, arm, 'cylinder', rot=(0, math.pi / 2, 0), bevel=0.01)

    # --- SCULPTED HEAD, EXPRESSION & ACCESSORIES ---
    shape('HeadCranium', (0, 0, 0.24), (0.36, 0.32, 0.44), skin_mat, head, 'ico', subsurf=1)

    # Realistic Eyes: Socket, Iris, Pupil, Dual Glints
    for side in [-1, 1]:
        shape('EyeSocket', (side * 0.16, -0.28, 0.25), (0.11, 0.04, 0.11), mat('EyeLid', '#12161c', rough=0.5), head, 'ico', subsurf=1)
        shape('EyeIris', (side * 0.16, -0.32, 0.25), (0.065, 0.025, 0.07), amber_eyes, head, 'ico')
        shape('EyePupil', (side * 0.16, -0.34, 0.25), (0.032, 0.015, 0.042), pupil_mat, head, 'ico')
        shape('EyeGlint1', (side * 0.175, -0.35, 0.275), (0.018, 0.012, 0.018), glint_mat, head, 'ico')
        shape('EyeGlint2', (side * 0.15, -0.345, 0.235), (0.010, 0.008, 0.010), glint_mat, head, 'ico')

    # Species-Specific Anatomical Sculpting
    if species == 'crow':
        # Curved Raven Beak with nostrils
        shape('RavenBeak', (0, -0.48, 0.16), (0.12, 0.38, 0.14), mat('RavenBeak', '#0c1016', rough=0.20, spec=0.9), head, 'cone', rot=(math.pi / 2, 0, 0), subsurf=1)
        # Gold Pince-Nez Spectacles
        for side in [-1, 1]:
            shape('SpecRim', (side * 0.16, -0.34, 0.26), (0.11, 0.11, 0.035), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('SpecBridge', (0, -0.36, 0.27), (0.05, 0.02, 0.015), accent_mat, head, 'cube', bevel=0.01)
        shape('SpecChain', (0.18, -0.33, 0.13), (0.01, 0.01, 0.16), accent_mat, head, 'cylinder')
        # Layered Feather Tufts
        for side in [-1, 1]:
            for k in range(4):
                shape('FeatherCrest', (side * (0.28 + k * 0.02), 0.06, 0.30 - k * 0.12), (0.11, 0.20, 0.18), skin_mat, head, 'ico', subsurf=1)

    elif species == 'owl':
        # Horned Owl Tufts & Facial Discs
        for side in [-1, 1]:
            shape('OwlHorn', (side * 0.32, 0.02, 0.62), (0.11, 0.13, 0.28), skin_mat, head, 'cone', rot=(-0.2, side * 0.28, 0), subsurf=1)
            shape('OwlHornTip', (side * 0.32, -0.04, 0.64), (0.07, 0.07, 0.20), leather_mat, head, 'cone', rot=(-0.2, side * 0.28, 0))
            shape('FacialDisc', (side * 0.18, -0.22, 0.25), (0.22, 0.08, 0.24), mat('FacialFeather', '#5c486a', rough=0.8), head, 'ico', subsurf=1)
        shape('OwlHookBeak', (0, -0.44, 0.15), (0.10, 0.22, 0.16), accent_mat, head, 'cone', rot=(math.pi / 2 + 0.22, 0, 0), subsurf=1)
        # Gold Monocle on right eye
        shape('MonocleRim', (0.16, -0.34, 0.25), (0.12, 0.12, 0.03), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('MonocleChain', (0.18, -0.32, 0.08), (0.01, 0.01, 0.22), accent_mat, head, 'cylinder')
        # Aristocratic Crown
        for k in range(5):
            a = (k - 2) * 0.38
            shape('CrownSpike', (0.26 * math.sin(a), -0.14, 0.68 + 0.10 * (k % 2)), (0.055, 0.055, 0.18), accent_mat, head, 'cone')
        shape('CrownCirclet', (0, 0, 0.60), (0.33, 0.29, 0.07), accent_mat, head, 'cylinder', bevel=0.01)

    elif species == 'jaguar':
        # Jaguar Muzzle & Nose
        shape('JaguarMuzzle', (0, -0.33, 0.09), (0.25, 0.21, 0.17), white_fur, head, 'ico', subsurf=1)
        shape('JaguarNose', (0, -0.52, 0.13), (0.09, 0.06, 0.07), mat('CatNose', '#4c1e28', rough=0.35), head, 'ico')
        # Ears with Pink Interior
        for side in [-1, 1]:
            shape('JaguarEar', (side * 0.32, 0.03, 0.54), (0.13, 0.11, 0.14), skin_mat, head, 'ico', subsurf=1)
            shape('JaguarEarInner', (side * 0.32, -0.05, 0.54), (0.07, 0.05, 0.09), mat('EarPeach', '#cb6e7e', rough=0.5), head, 'ico')
            # Rosette spots
            for k in range(5):
                shape('RosetteSpot', (side * (0.24 + 0.02 * (k % 2)), -0.21, 0.42 - k * 0.11), (0.045, 0.022, 0.038), leather_mat, head, 'ico')
        # Gold Earring & Ruby Pendant
        shape('JaguarEarring', (0.42, -0.01, 0.14), (0.06, 0.06, 0.06), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('RubyDrop', (0, -0.28, 0.98), (0.06, 0.03, 0.07), ruby_mat, body, 'ico', subsurf=1)

    elif species == 'capybara':
        # Broad Noble Capybara Snout
        shape('CapySnout', (0, -0.36, 0.08), (0.32, 0.28, 0.23), skin_mat, head, 'ico', subsurf=1)
        shape('CapyLeatherNose', (0, -0.58, 0.14), (0.12, 0.07, 0.08), leather_mat, head, 'ico')
        # Rounded Ears
        for side in [-1, 1]:
            shape('CapyEar', (side * 0.33, 0.08, 0.46), (0.10, 0.09, 0.11), skin_mat, head, 'ico', subsurf=1)
        # Vitória-Régia Water Lily Flower
        shape('LilyCore', (0.35, -0.04, 0.52), (0.15, 0.11, 0.15), mat('LilyPink', '#f0809b', rough=0.35), head, 'ico', subsurf=1)
        shape('LilyStamen', (0.35, -0.12, 0.52), (0.065, 0.04, 0.065), accent_mat, head, 'ico')
        for petal_i in range(6):
            pa = petal_i * math.tau / 6
            shape('LilyPetal', (0.35 + 0.11 * math.cos(pa), -0.06, 0.52 + 0.11 * math.sin(pa)), (0.065, 0.045, 0.065), mat('LilyWhite', '#fcf2f4', rough=0.3), head, 'ico')

    elif species == 'fox':
        # Fox Muzzle & Cheeks
        shape('FoxMuzzle', (0, -0.36, 0.07), (0.21, 0.25, 0.15), skin_mat, head, 'cone', rot=(math.pi / 2, 0, 0), subsurf=1)
        shape('FoxNose', (0, -0.58, 0.10), (0.075, 0.055, 0.055), leather_mat, head, 'ico')
        shape('WhiteCheekL', (-0.18, -0.22, 0.08), (0.15, 0.13, 0.15), white_fur, head, 'ico', subsurf=1)
        shape('WhiteCheekR', (0.18, -0.22, 0.08), (0.15, 0.13, 0.15), white_fur, head, 'ico', subsurf=1)
        # Pointed Ears with Black Tips
        for side in [-1, 1]:
            shape('FoxEar', (side * 0.30, 0.02, 0.56), (0.12, 0.12, 0.23), skin_mat, head, 'cone', rot=(-0.2, side * 0.24, 0), subsurf=1)
            shape('FoxEarTip', (side * 0.32, 0.02, 0.72), (0.075, 0.075, 0.13), leather_mat, head, 'cone', rot=(-0.2, side * 0.24, 0))
            shape('FoxEarInner', (side * 0.28, -0.05, 0.54), (0.065, 0.045, 0.13), white_fur, head, 'cone', rot=(-0.2, side * 0.24, 0))
        # Fedora Hat
        shape('FedoraBrim', (0, -0.04, 0.58), (0.48, 0.42, 0.038), mat('FedoraFelt', '#454038', rough=0.72), head, 'cylinder', rot=(0.12, -0.15, 0), bevel=0.01)
        shape('FedoraCrown', (0, -0.04, 0.70), (0.28, 0.26, 0.18), mat('FedoraFelt', '#454038', rough=0.72), head, 'cylinder', rot=(0.12, -0.15, 0), subsurf=1)
        shape('FedoraRibbon', (0, -0.04, 0.62), (0.30, 0.27, 0.045), coat_mat, head, 'cylinder', rot=(0.12, -0.15, 0))

    elif species == 'serpent':
        # Royal Cobra Hood with Scaled Patterns
        for side in [-1, 1]:
            shape('CobraHood', (side * 0.34, 0.10, 0.16), (0.25, 0.13, 0.46), coat_mat, head, 'ico', subsurf=1)
            shape('HoodTrim', (side * 0.38, 0.10, 0.16), (0.075, 0.075, 0.42), accent_mat, head, 'cylinder')
        # Scaled Muzzle and Throat
        shape('CobraMuzzle', (0, -0.34, 0.12), (0.28, 0.21, 0.14), skin_mat, head, 'ico', subsurf=1)
        shape('CobraThroat', (0, -0.24, -0.12), (0.22, 0.17, 0.30), skin_mat, head, 'cylinder', subsurf=1)
        # Fangs
        for side in [-1, 1]:
            shape('CobraFang', (side * 0.15, -0.45, 0.03), (0.028, 0.028, 0.075), linen_mat, head, 'cone', rot=(math.pi, 0, 0))
        # Ruby Tiara
        shape('TiaraBase', (0, -0.06, 0.50), (0.28, 0.24, 0.055), accent_mat, head, 'cylinder', rot=(0.2, 0, 0), bevel=0.01)
        for k in range(5):
            a = (k - 2) * 0.40
            shape('TiaraSpike', (0.24 * math.sin(a), -0.22, 0.58 + 0.08 * (k % 2)), (0.045, 0.045, 0.13), accent_mat, head, 'cone')
            shape('TiaraRuby', (0.24 * math.sin(a), -0.23, 0.56 + 0.08 * (k % 2)), (0.038, 0.038, 0.038), ruby_mat, head, 'ico')

    else:
        # Humans: Nina & Bento
        shape('HumanNose', (0, -0.36, 0.14), (0.07, 0.085, 0.10), skin_mat, head, 'ico', subsurf=1)
        if ident == 'nina':
            # Volumetric curly dark locks
            shape('HairCrown', (0, 0.06, 0.40), (0.40, 0.34, 0.32), leather_mat, head, 'ico', subsurf=1)
            for k in range(16):
                a = k * math.tau / 16
                shape('CurlLock', (0.33 * math.cos(a), 0.08 + 0.22 * math.sin(a), 0.38 + 0.06 * (k % 3)), (0.13, 0.12, 0.13), leather_mat, head, 'ico', subsurf=1)
            # Gold Round Spectacles
            for side in [-1, 1]:
                shape('SpecRim', (side * 0.16, -0.33, 0.25), (0.105, 0.105, 0.028), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
            shape('SpecBridge', (0, -0.35, 0.26), (0.048, 0.018, 0.014), accent_mat, head, 'cube', bevel=0.01)
            # Gold Hoop Earrings & Emerald Pendant
            for side in [-1, 1]:
                shape('HoopEarring', (side * 0.38, 0.02, 0.18), (0.06, 0.06, 0.06), accent_mat, head, 'torus', rot=(0, math.pi / 2, 0))
            shape('EmeraldDrop', (0, -0.28, 1.02), (0.05, 0.025, 0.06), emerald_mat, body, 'ico', subsurf=1)

        else:
            # Bento: Combed hair and 3D handlebar moustache
            shape('HairBase', (0, 0.06, 0.44), (0.38, 0.31, 0.24), mat('BentoHair', '#2a1a12', rough=0.55), head, 'ico', subsurf=1)
            for side in [-1, 1]:
                shape('HairSideLock', (side * 0.22, 0.02, 0.50), (0.17, 0.13, 0.11), mat('BentoHair', '#2a1a12', rough=0.55), head, 'ico', subsurf=1)
                shape('HandlebarMoustache', (side * 0.11, -0.39, 0.02), (0.13, 0.038, 0.055), mat('BentoHair', '#2a1a12', rough=0.45), head, 'ico', rot=(0, side * 0.35, side * 0.22), subsurf=1)

    # --- JOIN MESHES PER ARTICULATED GROUP ---
    for group in [body, head] + arms:
        meshes = [o for o in group.children if o.type == 'MESH']
        if meshes:
            bpy.ops.object.select_all(action='DESELECT')
            for o in meshes:
                o.select_set(True)
            bpy.context.view_layer.objects.active = meshes[0]
            bpy.ops.object.join()
            bpy.context.object.name = f"{group.name}Mesh"

    # --- BEZIER PHYSICS-LIKE ANIMATIONS (5 Required Clips) ---
    sign = -1 if index % 2 else 1

    for clip in ['entrance', 'truco', 'victory', 'boss_intro', 'flourish']:
        if clip == 'entrance':
            # Natural rhythmic entry and settle
            animate(body, clip, [
                (1,  (0, 0, 0.12), (0, sign * 0.08, 0)),
                (14, (0, 0, -0.04), (0.04, -sign * 0.04, 0)),
                (28, (0, 0, 0.03), (-0.02, sign * 0.02, 0)),
                (40, (0, 0, 0), (0, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0.02), (0.08, -sign * 0.12, -sign * 0.08)),
                (16, (0, 0, 0.04), (-0.06, sign * 0.15, sign * 0.06)),
                (30, (0, 0, 0.01), (0.04, -sign * 0.05, 0)),
                (42, (0, 0, 0), (0, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0.35 * side_f, 0, 0)),
                    (16, (0, -0.08, 0.06), (-0.45 * side_f, sign * 0.15, 0.12 * side_f)),
                    (32, (0, -0.04, 0), (0.15 * side_f, 0, 0)),
                    (48, (0, 0, 0), (0, 0, 0))
                ])

        elif clip == 'truco':
            # Dramatic challenge! Body surges forward, arm challenges the table
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (10, (0, 0.06, -0.03), (-0.08, 0, 0)),
                (20, (0, -0.16, 0.08), (0.24, sign * 0.06, 0)),
                (32, (0, -0.10, 0.04), (0.15, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (10, (0, 0, 0.03), (-0.12, 0, 0)),
                (20, (0, -0.08, 0.05), (0.28, sign * 0.08, 0)),
                (34, (0, -0.04, 0.02), (0.12, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                if j == (0 if index % 2 == 0 else 1):
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0, 0, 0)),
                        (10, (0, 0.06, 0.12), (-0.65, sign * 0.20, 0.25)),
                        (20, (0, -0.18, 0.08), (0.85, sign * 0.30, -0.35)),
                        (34, (0, -0.12, 0.04), (0.55, sign * 0.15, -0.20)),
                        (48, (0, 0, 0), (0, 0, 0))
                    ])
                else:
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0, 0, 0)),
                        (10, (0, 0.04, 0), (-0.20, 0, 0)),
                        (20, (0, -0.12, -0.04), (0.35, -sign * 0.10, 0)),
                        (48, (0, 0, 0), (0, 0, 0))
                    ])

        elif clip == 'victory':
            # Triumphant celebration! Fist pump and proud stance
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (12, (0, 0, 0.10), (-0.08, sign * 0.06, 0)),
                (24, (0, 0, 0.14), (-0.12, -sign * 0.04, 0)),
                (36, (0, 0, 0.06), (-0.04, sign * 0.02, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.06), (-0.18, sign * 0.12, sign * 0.08)),
                (26, (0, 0, 0.08), (-0.22, -sign * 0.08, -sign * 0.05)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0, 0, 0)),
                    (14, (0, -0.06, 0.22), (-1.20, side_f * 0.35, side_f * 0.40)),
                    (26, (0, -0.08, 0.26), (-1.35, side_f * 0.40, side_f * 0.45)),
                    (38, (0, -0.04, 0.12), (-0.60, side_f * 0.20, side_f * 0.20)),
                    (48, (0, 0, 0), (0, 0, 0))
                ])

        elif clip == 'boss_intro':
            # Theatrical boss intimidation! Wide wing/cape flare and piercing bow
            animate(body, clip, [
                (1,  (0, 0, 0.08), (0, 0, 0)),
                (16, (0, -0.06, 0.16), (0.08, 0, 0)),
                (28, (0, -0.10, 0.12), (0.14, sign * 0.05, 0)),
                (42, (0, 0, 0.04), (0.04, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0.02), (-0.15, 0, 0)),
                (16, (0, 0, 0.08), (-0.22, sign * 0.08, 0)),
                (28, (0, -0.04, 0.06), (0.16, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0, 0, 0)),
                    (18, (side_f * 0.15, 0, 0.18), (0.30, side_f * 0.85, -side_f * 0.50)),
                    (32, (side_f * 0.08, -0.06, 0.08), (0.50, side_f * 0.45, -side_f * 0.25)),
                    (48, (0, 0, 0), (0, 0, 0))
                ])

        elif clip == 'flourish':
            # Card master flourish! Elegant arc and confident nod
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, -0.04, 0.04), (0.05, sign * 0.06, 0)),
                (28, (0, 0.02, 0.02), (-0.04, -sign * 0.03, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.03), (0.08, sign * 0.14, sign * 0.06)),
                (28, (0, 0, 0.01), (-0.06, -sign * 0.08, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0, 0, 0)),
                    (14, (0, -0.12, 0.10), (-0.55, side_f * 0.35, -side_f * 0.40)),
                    (28, (0, -0.08, 0.05), (-0.25, side_f * 0.15, -side_f * 0.15)),
                    (48, (0, 0, 0), (0, 0, 0))
                ])

    # Scene setup
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 48
    scene.render.fps = 24
    scene.frame_set(1)

    # Export glTF 2.0 with all NLA tracks and applied modifiers
    bpy.ops.object.select_all(action='SELECT')
    out_glb = OUT / f"{ident}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(out_glb),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,
        export_frame_range=True
    )

    # Studio Portrait Render (320x400 transparent PNG)
    bpy.ops.object.camera_add(location=(1.8, -4.8, 2.1))
    camera = bpy.context.object
    camera.name = 'PortraitCamera'
    camera.rotation_euler = (Vector((0, 0, 1.05)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 2.45
    scene.camera = camera

    for loc, energy, size in [((-3.2, -4.0, 5.5), 650, 3.5), ((3.5, 0.8, 4.2), 750, 2.8), ((0, 4.0, 2.5), 350, 2.0)]:
        bpy.ops.object.light_add(type='AREA', location=loc)
        light = bpy.context.object
        light.data.energy = energy
        light.data.shape = 'DISK'
        light.data.size = size
        light.rotation_euler = (-light.location).to_track_quat('-Z', 'Y').to_euler()

    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 320
    scene.render.resolution_y = 400
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'

    out_png_3d = OUT / f"{ident}_3d.png"
    scene.render.filepath = str(out_png_3d)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"{ident}.blend"))
    bpy.ops.render.render(write_still=True)

    print(f"[Blender] Stylized AAA model generated: {ident}.glb ({out_glb.stat().st_size} bytes)", flush=True)

def main():
    for i, row in enumerate(CAST):
        build(i, *row)

    manifest = {
        'generator': 'Blender 5.2.1 Stylized AAA Pipeline',
        'characters': [c[0] for c in CAST],
        'clips': ['entrance', 'truco', 'victory', 'boss_intro', 'flourish'],
        'style': 'Stylized Triple-A high-fidelity character meshes, continuous organic anatomy, tailored clothing & PBR materials'
    }
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    print("[Blender] All 8 Stylized AAA characters built successfully!", flush=True)

if __name__ == '__main__':
    main()
