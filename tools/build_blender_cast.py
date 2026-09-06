"""Blender 5.2: High-aesthetic original club cast, tailored clothing, expressive heads,
articulated Bezier animations, GLBs, and transparent studio portraits.
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

# 8 club characters: ident, species, skin_color, coat_color, accent_color
CAST = [
    ('nina',  'human',    '#bb8057', '#1b5e40', '#d4af37'), # Emerald vest, gold trim
    ('bento', 'human',    '#a86a42', '#7a3424', '#c9934e'), # Burgundy/leather suspenders, brass
    ('corvo', 'crow',     '#1c2430', '#174d38', '#dfb15b'), # Emerald damask, gold pince-nez
    ('onca',  'jaguar',   '#c88a39', '#6d2139', '#e0b548'), # Maroon velvet, gold filigree
    ('iara',  'capybara', '#9c734b', '#1e6658', '#e8749a'), # Forest teal vest, pink lily
    ('zeca',  'fox',      '#bf5628', '#253d66', '#d69e38'), # Navy waistcoat, gold watch chain
    ('barao', 'owl',      '#473854', '#2d203d', '#bf9551'), # Midnight purple tuxedo, gold monocle
    ('dama',  'serpent',  '#cca038', '#801828', '#d62442')  # Gold scales, crimson velvet, rubies
]

def mat(name, hex_color, metal=0.0, rough=0.65, spec=0.5):
    c = tuple(int(hex_color[i:i+2], 16) / 255 for i in (1, 3, 5)) + (1,)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    if p:
        # Linear sRGB conversion
        linear_c = tuple((v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in c[:3]) + (1,)
        p.inputs['Base Color'].default_value = linear_c
        p.inputs['Metallic'].default_value = metal
        p.inputs['Roughness'].default_value = rough
        if 'Specular IOR Level' in p.inputs:
            p.inputs['Specular IOR Level'].default_value = spec
        elif 'Specular' in p.inputs:
            p.inputs['Specular'].default_value = spec
    return m

def empty(name, pos=(0, 0, 0), parent=None):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.parent = parent
    o.location = pos
    return o

def shape(name, pos, scale, material, parent, kind='ico', rot=(0, 0, 0), smooth=True):
    if kind == 'ico':
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1)
    elif kind == 'cone':
        bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=1, radius2=0, depth=2)
    elif kind == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=1, depth=2)
    elif kind == 'torus':
        bpy.ops.mesh.primitive_torus_add(major_segments=24, minor_segments=8, major_radius=1, minor_radius=0.2)
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

def build(index, ident, species, skinhex, coathex, accenthex):
    # Reset scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)

    # Core Materials
    skin = mat(f'{ident}_skin', skinhex, rough=0.62)
    coat = mat(f'{ident}_coat', coathex, rough=0.78)
    accent = mat(f'{ident}_accent', accenthex, metal=0.75, rough=0.25) # Gold/brass
    shirt = mat('FineLinen', '#f0ebd8', rough=0.60)
    silk_lapel = mat('SilkLapel', coathex, rough=0.35)
    leather_ink = mat('PolishedInk', '#10141a', rough=0.30)
    eyes_mat = mat('BrightEyes', '#f7c352', rough=0.20)
    pupil_mat = mat('Pupil', '#080a0e', rough=0.10)
    glint_mat = mat('Glint', '#ffffff', rough=0.10)
    rose_mat = mat('RosePetal', '#e0607a', rough=0.45)
    ruby_mat = mat('RoyalRuby', '#c4182e', metal=0.40, rough=0.15)
    white_fur = mat('WhiteFur', '#f4ede0', rough=0.85)

    # Hierarchy: Actor -> Body -> Head, ArmL, ArmR
    rig = empty('Actor')
    body = empty('Body', (0, 0, 0), rig)
    head = empty('Head', (0, 0, 1.44), body)
    arm_l = empty('ArmL', (-0.46, 0.02, 1.12), body)
    arm_r = empty('ArmR', (0.46, 0.02, 1.12), body)
    arms = [arm_l, arm_r]

    # --- BODY & TORSO ---
    # Waistcoat / Jacket main form
    shape('TorsoBase', (0, 0, 0.80), (0.50, 0.28, 0.62), coat, body, 'ico')
    # Crisp white shirt collar and chest bib
    shape('ShirtChest', (0, -0.26, 0.94), (0.22, 0.04, 0.44), shirt, body, 'cube', smooth=False)
    shape('ShirtCollarL', (-0.14, -0.28, 1.30), (0.08, 0.03, 0.10), shirt, body, 'cube', rot=(0.2, 0.3, -0.2), smooth=False)
    shape('ShirtCollarR', (0.14, -0.28, 1.30), (0.08, 0.03, 0.10), shirt, body, 'cube', rot=(0.2, -0.3, 0.2), smooth=False)

    # Waistcoat lapels
    for side in [-1, 1]:
        shape('Lapel', (side * 0.24, -0.29, 1.02), (0.13, 0.04, 0.34), silk_lapel, body, 'cube', rot=(0.1, side * 0.35, -side * 0.1), smooth=False)
        shape('Trouser', (side * 0.24, 0.03, 0.32), (0.20, 0.21, 0.32), coat, body, 'cylinder')
        shape('Shoe', (side * 0.24, -0.06, 0.09), (0.21, 0.32, 0.11), leather_ink, body, 'ico')

    # Golden buttons down waistcoat
    for z in [0.62, 0.84, 1.06]:
        shape('Button', (0.03, -0.31, z), (0.035, 0.02, 0.035), accent, body, 'cylinder', rot=(math.pi / 2, 0, 0))

    # Tie / Cravat / Bowtie
    if species in ('owl', 'crow'):
        # Bowtie or formal cravat
        shape('BowtieL', (-0.09, -0.31, 1.25), (0.08, 0.03, 0.06), coat, body, 'ico')
        shape('BowtieR', (0.09, -0.31, 1.25), (0.08, 0.03, 0.06), coat, body, 'ico')
        shape('BowtieKnot', (0, -0.32, 1.25), (0.04, 0.03, 0.04), accent, body, 'ico')
    elif species == 'serpent':
        # Queen ruby brooch
        shape('Brooch', (0, -0.29, 1.18), (0.08, 0.03, 0.08), ruby_mat, body, 'ico')
    else:
        # Necktie or gentleman cravat
        shape('Cravat', (0, -0.29, 1.18), (0.07, 0.025, 0.16), silk_lapel, body, 'cube', smooth=False)

    # Suspenders for Bento
    if ident == 'bento':
        for side in [-1, 1]:
            shape('Suspender', (side * 0.18, -0.27, 0.95), (0.04, 0.02, 0.42), mat('SuspenderLeather', '#5c2c1a', rough=0.5), body, 'cube', smooth=False)
            shape('SuspenderClasp', (side * 0.18, -0.28, 0.60), (0.045, 0.025, 0.04), accent, body, 'cube', smooth=False)

    # Pocket watch chain for Zeca & Corvo
    if ident in ('zeca', 'corvo'):
        shape('WatchChain', (0.16, -0.29, 0.72), (0.12, 0.02, 0.08), accent, body, 'torus', rot=(0.4, 0.2, 0.3))

    # Cape for Bosses (Barão and Dama)
    if index >= 6:
        shape('Cape', (0, 0.22, 0.88), (0.68, 0.16, 0.74), mat('RoyalCape', '#3b1828' if index == 7 else '#221630', rough=0.85), body, 'cube', smooth=False)
        shape('CapeCollar', (0, 0.10, 1.34), (0.58, 0.14, 0.16), mat('RoyalGoldTrim', '#d4af37', metal=0.8, rough=0.2), body, 'torus')

    # --- ARMS & HANDS ---
    for j, arm in enumerate(arms):
        side = -1 if j == 0 else 1
        shape('Shoulder', (0, 0, 0), (0.19, 0.21, 0.21), coat, arm, 'ico')
        shape('SleeveUpper', (0, 0, -0.24), (0.17, 0.19, 0.26), coat, arm, 'cylinder')
        shape('SleeveLower', (0, -0.06, -0.48), (0.16, 0.17, 0.24), coat, arm, 'cylinder', rot=(0.25, 0, 0))
        # Shirt Cuff
        shape('ShirtCuff', (0, -0.10, -0.66), (0.17, 0.16, 0.08), shirt, arm, 'cylinder', rot=(0.25, 0, 0))
        shape('Cufflink', (side * 0.12, -0.10, -0.66), (0.025, 0.025, 0.025), accent, arm, 'ico')
        # Hand / Paw / Claw
        hand_mat = skin if species not in ('crow', 'jaguar') else (mat('CrowClaw', '#12161f', rough=0.4) if species == 'crow' else skin)
        shape('Hand', (0, -0.16, -0.76), (0.16, 0.17, 0.15), hand_mat, arm, 'ico')

        # Wristwatch on left wrist for Bento
        if ident == 'bento' and j == 0:
            shape('WatchStrap', (0, -0.10, -0.66), (0.18, 0.17, 0.06), mat('WatchStrap', '#3e2014'), arm, 'cylinder')
            shape('WatchFace', (-0.14, -0.10, -0.66), (0.04, 0.04, 0.04), accent, arm, 'cylinder', rot=(0, math.pi / 2, 0))

    # --- HEAD & ANATOMY ---
    shape('Cranium', (0, 0, 0.22), (0.38, 0.32, 0.44), skin, head, 'ico')

    # Expressive Eyes with Sockets, Iris, Pupils, and Glints
    for side in [-1, 1]:
        shape('EyeSocket', (side * 0.17, -0.28, 0.25), (0.11, 0.04, 0.11), leather_ink, head, 'ico')
        shape('EyeIris', (side * 0.17, -0.32, 0.25), (0.065, 0.03, 0.07), eyes_mat, head, 'ico')
        shape('EyePupil', (side * 0.17, -0.34, 0.25), (0.035, 0.015, 0.045), pupil_mat, head, 'ico')
        shape('EyeGlint', (side * 0.185, -0.35, 0.28), (0.018, 0.012, 0.018), glint_mat, head, 'ico')

    # Species-Specific Anatomy
    if species == 'crow':
        # Raven Beak
        shape('Beak', (0, -0.46, 0.16), (0.13, 0.36, 0.15), mat('BeakSlate', '#0e1218', rough=0.25), head, 'cone', rot=(math.pi / 2, 0, 0))
        # Pince-nez golden spectacles
        for side in [-1, 1]:
            shape('SpecRim', (side * 0.17, -0.34, 0.26), (0.11, 0.11, 0.04), accent, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('SpecBridge', (0, -0.36, 0.27), (0.05, 0.02, 0.018), accent, head, 'cube', smooth=False)
        shape('SpecChain', (0.18, -0.33, 0.14), (0.01, 0.01, 0.16), accent, head, 'cylinder')
        # Crown feathers
        for side in [-1, 1]:
            for k in range(4):
                shape('Feather', (side * (0.28 + k * 0.02), 0.06, 0.28 - k * 0.12), (0.12, 0.22, 0.20), skin, head, 'ico')

    elif species == 'owl':
        # Horned Owl Tufts
        for side in [-1, 1]:
            shape('EarTuft', (side * 0.32, 0.02, 0.62), (0.12, 0.14, 0.28), skin, head, 'cone', rot=(-0.2, side * 0.3, 0))
            shape('EarTuftAccent', (side * 0.32, -0.04, 0.62), (0.08, 0.08, 0.22), leather_ink, head, 'cone', rot=(-0.2, side * 0.3, 0))
        # Hooked beak
        shape('OwlBeak', (0, -0.42, 0.15), (0.11, 0.22, 0.17), accent, head, 'cone', rot=(math.pi / 2 + 0.2, 0, 0))
        # Golden Monocle on right eye
        shape('MonocleRim', (0.17, -0.34, 0.25), (0.12, 0.12, 0.03), accent, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('MonocleChain', (0.19, -0.32, 0.08), (0.012, 0.012, 0.22), accent, head, 'cylinder')
        # Royal Crown
        for k in range(5):
            a = (k - 2) * 0.38
            shape('CrownPoint', (0.28 * math.sin(a), -0.14, 0.68 + 0.10 * (k % 2)), (0.06, 0.06, 0.18), accent, head, 'cone')
        shape('CrownBand', (0, 0, 0.60), (0.34, 0.30, 0.08), accent, head, 'cylinder')

    elif species == 'jaguar':
        # Muzzle & Nose
        shape('Muzzle', (0, -0.32, 0.08), (0.26, 0.22, 0.18), white_fur, head, 'ico')
        shape('Nose', (0, -0.52, 0.12), (0.10, 0.06, 0.07), mat('CatNose', '#4a1e28', rough=0.4), head, 'ico')
        # Jaguar Ears
        for side in [-1, 1]:
            shape('JaguarEar', (side * 0.32, 0.02, 0.54), (0.14, 0.12, 0.15), skin, head, 'ico')
            shape('JaguarEarInner', (side * 0.32, -0.06, 0.54), (0.08, 0.06, 0.10), mat('EarPink', '#c4687a', rough=0.5), head, 'ico')
            # Rosette spots
            for k in range(5):
                shape('Rosette', (side * (0.24 + 0.02 * (k % 2)), -0.22, 0.42 - k * 0.11), (0.05, 0.025, 0.04), leather_ink, head, 'ico')
        # Gold Earring
        shape('Earring', (0.42, -0.02, 0.14), (0.06, 0.06, 0.06), accent, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('RubyNecklace', (0, -0.28, 0.98), (0.06, 0.03, 0.06), ruby_mat, body, 'ico')

    elif species == 'capybara':
        # Broad gentle capybara snout
        shape('CapySnout', (0, -0.35, 0.08), (0.32, 0.28, 0.24), skin, head, 'ico')
        shape('CapyNose', (0, -0.58, 0.14), (0.12, 0.07, 0.08), leather_ink, head, 'ico')
        # Small rounded ears
        for side in [-1, 1]:
            shape('CapyEar', (side * 0.33, 0.08, 0.46), (0.11, 0.10, 0.12), skin, head, 'ico')
            shape('CapyEarInner', (side * 0.33, 0.02, 0.46), (0.06, 0.04, 0.08), rose_mat, head, 'ico')
        # Vitória-Régia Water Lily Flower behind left ear
        shape('LilyBase', (0.35, -0.04, 0.52), (0.16, 0.12, 0.16), rose_mat, head, 'ico')
        shape('LilyHeart', (0.35, -0.12, 0.52), (0.07, 0.04, 0.07), accent, head, 'ico')
        for petal_i in range(6):
            pa = petal_i * math.tau / 6
            shape('LilyPetal', (0.35 + 0.12 * math.cos(pa), -0.06, 0.52 + 0.12 * math.sin(pa)), (0.07, 0.05, 0.07), mat('LilyPink', '#f29bb0', rough=0.3), head, 'ico')

    elif species == 'fox':
        # Fox Snout with white cheeks
        shape('FoxSnout', (0, -0.36, 0.06), (0.22, 0.26, 0.16), skin, head, 'cone', rot=(math.pi / 2, 0, 0))
        shape('FoxNose', (0, -0.58, 0.10), (0.08, 0.06, 0.06), leather_ink, head, 'ico')
        shape('WhiteCheekL', (-0.18, -0.22, 0.08), (0.16, 0.14, 0.16), white_fur, head, 'ico')
        shape('WhiteCheekR', (0.18, -0.22, 0.08), (0.16, 0.14, 0.16), white_fur, head, 'ico')
        # Pointed ears with dark tips
        for side in [-1, 1]:
            shape('FoxEar', (side * 0.30, 0.02, 0.56), (0.13, 0.13, 0.24), skin, head, 'cone', rot=(-0.2, side * 0.25, 0))
            shape('FoxEarTip', (side * 0.32, 0.02, 0.72), (0.08, 0.08, 0.14), leather_ink, head, 'cone', rot=(-0.2, side * 0.25, 0))
            shape('FoxEarInner', (side * 0.28, -0.06, 0.54), (0.07, 0.05, 0.14), white_fur, head, 'cone', rot=(-0.2, side * 0.25, 0))
        # Fedora Hat
        shape('FedoraBrim', (0, -0.04, 0.58), (0.50, 0.44, 0.04), mat('FedoraFelt', '#4a443b', rough=0.7), head, 'cylinder', rot=(0.12, -0.15, 0))
        shape('FedoraCrown', (0, -0.04, 0.70), (0.30, 0.28, 0.18), mat('FedoraFelt', '#4a443b', rough=0.7), head, 'cylinder', rot=(0.12, -0.15, 0))
        shape('FedoraBand', (0, -0.04, 0.62), (0.32, 0.29, 0.05), coat, head, 'cylinder', rot=(0.12, -0.15, 0))

    elif species == 'serpent':
        # Royal Cobra Hood
        for side in [-1, 1]:
            shape('CobraHood', (side * 0.34, 0.10, 0.16), (0.26, 0.14, 0.48), coat, head, 'ico')
            shape('HoodTrim', (side * 0.38, 0.10, 0.16), (0.08, 0.08, 0.44), accent, head, 'cylinder')
        # Scaled Head and Underbelly
        shape('CobraMuzzle', (0, -0.34, 0.12), (0.30, 0.22, 0.15), skin, head, 'ico')
        shape('CobraThroat', (0, -0.24, -0.12), (0.24, 0.18, 0.32), skin, head, 'cylinder')
        # Fangs
        for side in [-1, 1]:
            shape('Fang', (side * 0.16, -0.46, 0.03), (0.032, 0.032, 0.08), shirt, head, 'cone', rot=(math.pi, 0, 0))
        # Ruby Tiara
        shape('TiaraBand', (0, -0.06, 0.50), (0.30, 0.26, 0.06), accent, head, 'cylinder', rot=(0.2, 0, 0))
        for k in range(5):
            a = (k - 2) * 0.40
            shape('TiaraSpike', (0.26 * math.sin(a), -0.22, 0.58 + 0.08 * (k % 2)), (0.05, 0.05, 0.14), accent, head, 'cone')
            shape('TiaraRuby', (0.26 * math.sin(a), -0.23, 0.56 + 0.08 * (k % 2)), (0.04, 0.04, 0.04), ruby_mat, head, 'ico')

    else:
        # Humans: Nina and Bento
        shape('HumanNose', (0, -0.36, 0.14), (0.075, 0.09, 0.11), skin, head, 'ico')
        if ident == 'nina':
            # Curly voluminous hair
            shape('HairBase', (0, 0.06, 0.38), (0.42, 0.34, 0.32), leather_ink, head, 'ico')
            for k in range(14):
                a = k * math.tau / 14
                shape('HairCurl', (0.34 * math.cos(a), 0.08 + 0.22 * math.sin(a), 0.38 + 0.06 * (k % 3)), (0.14, 0.13, 0.14), leather_ink, head, 'ico')
            # Gold spectacles
            for side in [-1, 1]:
                shape('SpecRim', (side * 0.17, -0.33, 0.25), (0.11, 0.11, 0.03), accent, head, 'torus', rot=(math.pi / 2, 0, 0))
            shape('SpecBridge', (0, -0.35, 0.26), (0.05, 0.02, 0.015), accent, head, 'cube', smooth=False)
            # Gold hoop earrings
            for side in [-1, 1]:
                shape('Earring', (side * 0.38, 0.02, 0.18), (0.065, 0.065, 0.065), accent, head, 'torus', rot=(0, math.pi / 2, 0))
        else:
            # Bento: Handlebar moustache and styled parted hair
            shape('HairBase', (0, 0.06, 0.44), (0.40, 0.32, 0.24), mat('BentoHair', '#2e1c14', rough=0.6), head, 'ico')
            for side in [-1, 1]:
                shape('HairWave', (side * 0.22, 0.02, 0.50), (0.18, 0.14, 0.12), mat('BentoHair', '#2e1c14', rough=0.6), head, 'ico')
                shape('Moustache', (side * 0.11, -0.39, 0.02), (0.14, 0.04, 0.06), mat('BentoHair', '#2e1c14', rough=0.5), head, 'ico', rot=(0, side * 0.35, side * 0.2))

    # --- JOINING MESHES PER ARTICULATED GROUP ---
    for group in [body, head] + arms:
        meshes = [o for o in group.children if o.type == 'MESH']
        if meshes:
            bpy.ops.object.select_all(action='DESELECT')
            for o in meshes:
                o.select_set(True)
            bpy.context.view_layer.objects.active = meshes[0]
            bpy.ops.object.join()
            bpy.context.object.name = f"{group.name}Mesh"

    # --- ANIMATION CLIPS (Bezier Smoothed) ---
    sign = -1 if index % 2 else 1
    # 5 required clips: entrance, truco, victory, boss_intro, flourish
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
                (10, (0, 0.06, -0.03), (-0.08, 0, 0)),      # Coil back
                (20, (0, -0.16, 0.08), (0.24, sign * 0.06, 0)), # Surge forward!
                (32, (0, -0.10, 0.04), (0.15, 0, 0)),       # Hold challenge
                (48, (0, 0, 0), (0, 0, 0))                  # Settle
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (10, (0, 0, 0.03), (-0.12, 0, 0)),
                (20, (0, -0.08, 0.05), (0.28, sign * 0.08, 0)), # Intense forward gaze
                (34, (0, -0.04, 0.02), (0.12, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                if j == (0 if index % 2 == 0 else 1):
                    # Dominant arm raises high and slams pointing forward!
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0, 0, 0)),
                        (10, (0, 0.06, 0.12), (-0.65, sign * 0.20, 0.25)), # Raise arm
                        (20, (0, -0.18, 0.08), (0.85, sign * 0.30, -0.35)), # Slam & point!
                        (34, (0, -0.12, 0.04), (0.55, sign * 0.15, -0.20)), # Hold point
                        (48, (0, 0, 0), (0, 0, 0))
                    ])
                else:
                    # Off-hand braces table
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0, 0, 0)),
                        (10, (0, 0, 0.04), (-0.20, 0, 0)),
                        (20, (0, -0.12, -0.04), (0.35, -sign * 0.10, 0)),
                        (48, (0, 0, 0), (0, 0, 0))
                    ])

        elif clip == 'victory':
            # Triumphant celebration! Chest up, fist pump / wing raise!
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (12, (0, 0, 0.10), (-0.08, sign * 0.06, 0)),
                (24, (0, 0, 0.14), (-0.12, -sign * 0.04, 0)),
                (36, (0, 0, 0.06), (-0.04, sign * 0.02, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.06), (-0.18, sign * 0.12, sign * 0.08)), # Triumphant tilt up
                (26, (0, 0, 0.08), (-0.22, -sign * 0.08, -sign * 0.05)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0, 0, 0)),
                    (14, (0, -0.06, 0.22), (-1.20, side_f * 0.35, side_f * 0.40)), # Fist pump up!
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
                (28, (0, -0.04, 0.06), (0.16, 0, 0)), # Piercing look at player
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0, 0, 0)),
                    (18, (side_f * 0.15, 0, 0.18), (0.30, side_f * 0.85, -side_f * 0.50)), # Wide majestic flare!
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
                    (14, (0, -0.12, 0.10), (-0.55, side_f * 0.35, -side_f * 0.40)), # Elegant presentation arc
                    (28, (0, -0.08, 0.05), (-0.25, side_f * 0.15, -side_f * 0.15)),
                    (48, (0, 0, 0), (0, 0, 0))
                ])

    # Setup export scene
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 48
    scene.render.fps = 24
    scene.frame_set(1)

    # Export glTF 2.0 with all NLA tracks
    bpy.ops.object.select_all(action='SELECT')
    out_glb = OUT / f"{ident}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(out_glb),
        export_format='GLB',
        use_selection=True,
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

    # Save 3D studio render as {ident}_3d.png and {ident}.png if not already customized
    out_png_3d = OUT / f"{ident}_3d.png"
    scene.render.filepath = str(out_png_3d)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"{ident}.blend"))
    bpy.ops.render.render(write_still=True)

    # Only overwrite {ident}.png if it does not exist, preserving the pixel art portrait
    out_png = OUT / f"{ident}.png"
    if not out_png.exists():
        import shutil
        shutil.copyfile(out_png_3d, out_png)

    print(f"[Blender] High-aesthetic model generated: {ident}.glb ({out_glb.stat().st_size} bytes)", flush=True)

def main():
    for i, row in enumerate(CAST):
        build(i, *row)

    manifest = {
        'generator': 'Blender 5.2.1 High-Aesthetic Pipeline',
        'characters': [c[0] for c in CAST],
        'clips': ['entrance', 'truco', 'victory', 'boss_intro', 'flourish'],
        'style': 'High-fidelity stylized 3D club cast, tailored clothing & PBR velvet/metals'
    }
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    print("[Blender] All 8 high-aesthetic characters built successfully!", flush=True)

if __name__ == '__main__':
    main()
