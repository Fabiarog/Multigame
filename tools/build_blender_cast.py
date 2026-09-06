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
    p = next((node for node in m.node_tree.nodes if node.type == 'BSDF_PRINCIPLED'), None)
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
        bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=1, radius2=0, depth=2)
    elif kind == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=1, depth=2)
    elif kind == 'torus':
        bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=12, major_radius=1, minor_radius=0.25)
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

    if subsurf > 0 and kind == 'cylinder':
        b = o.modifiers.new(name="RoundedCaps", type='BEVEL')
        b.width = .10
        b.segments = 3
        b.limit_method = 'ANGLE'
        b.harden_normals = True
    elif subsurf > 0:
        s = o.modifiers.new(name="Subsurf", type='SUBSURF')
        s.levels = subsurf
        s.render_levels = subsurf

    o.data.materials.append(material)
    for modifier in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
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

    if action:
        fc_list = []
        if hasattr(action, 'fcurves'):
            fc_list.extend(action.fcurves)
        if hasattr(action, 'layers'):
            for layer in action.layers:
                for strip in getattr(layer, 'strips', []):
                    for cb in getattr(strip, 'channelbags', []):
                        fc_list.extend(getattr(cb, 'fcurves', []))
        for fcurve in fc_list:
            for kp in fcurve.keyframe_points:
                kp.interpolation = 'BEZIER'
                kp.handle_left_type = 'AUTO_CLAMPED'
                kp.handle_right_type = 'AUTO_CLAMPED'

    track = obj.animation_data.nla_tracks.new()
    track.name = clip
    strip = track.strips.new(clip, 1, action)
    strip.extrapolation = 'HOLD' if clip == 'idle' else 'NOTHING'

    obj.animation_data.action = None
    obj.location = base_pos
    obj.rotation_euler = base_rot

def build(index, ident, species, skinhex, coathex, accenthex, sechex):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    # Core High-Fidelity PBR Materials
    skin_mat = mat(f'{ident}_skin', skinhex, rough=0.55)
    coat_mat = mat(f'{ident}_coat', coathex, rough=0.78) # Rich tailored velvet
    satin_mat = mat(f'{ident}_satin', coathex, rough=0.25) # Silk/satin lapels
    accent_mat = mat(f'{ident}_accent', accenthex, metal=0.92, rough=0.16, spec=1.0) # Polished Gold / Brass
    linen_mat = mat('LinenShirt', '#f2ede0' if species == 'crow' else sechex, rough=0.60)
    leather_mat = mat('PolishedLeather', '#11141a', rough=0.22)
    amber_eyes = mat('AmberEye', '#f5b838', rough=0.06, spec=1.0, emit_hex='#f5b838', emit_strength=0.18)
    pupil_mat = mat('Pupil', '#05070a', rough=0.04, spec=1.0)
    glint_mat = mat('Glint', '#ffffff', rough=0.02, spec=1.0)
    ruby_mat = mat('RubyGem', '#c81428', metal=0.35, rough=0.08, spec=1.0, emit_hex='#a00818', emit_strength=0.25)
    emerald_mat = mat('EmeraldGem', '#167a48', metal=0.35, rough=0.08, spec=1.0, emit_hex='#0d5830', emit_strength=0.25)
    white_fur = mat('WhiteFur', '#f5f0e8', rough=0.85)

    # Actor Hierarchy: Root -> Body -> Head, ArmL, ArmR -> ForearmL, ForearmR
    rig = empty('Actor')
    body = empty('Body', (0, 0, 0), rig)
    head = empty('Head', (0, 0, 1.44), body)

    # Shoulder joints
    arm_l = empty('ArmL', (-0.42, 0.02, 1.12), body)
    arm_r = empty('ArmR', (0.42, 0.02, 1.12), body)

    # Elbow joints (children of upper arm)
    forearm_l = empty('ForearmL', (0, -0.02, -0.32), arm_l)
    forearm_r = empty('ForearmR', (0, -0.02, -0.32), arm_r)

    arms = [arm_l, arm_r]
    forearms = [forearm_l, forearm_r]

    # --- TAILORED SUIT TORSO WITH CURVED BESPOKE WAISTCOAT LAPELS ---
    # Torso: smooth fitted body with masculine/aristocratic V-taper
    shape('TorsoFitted', (0, 0.0, 0.82), (0.40, 0.24, 0.32), coat_mat, body, 'cylinder', subsurf=1)
    # Natural neck bridging torso to head
    shape('Neck', (0, -0.01, 1.28), (0.13, 0.13, 0.14), skin_mat, body, 'cylinder', subsurf=1)

    # Tailored Trousers and Polished Leather Shoes
    for side in [-1, 1]:
        shape('Trouser', (side * 0.19, 0.02, 0.26), (0.15, 0.16, 0.24), coat_mat, body, 'cylinder', subsurf=1)
        shape('Shoe', (side * 0.19, -0.05, 0.07), (0.14, 0.22, 0.07), leather_mat, body, 'ico', subsurf=1)

    # Crisp Linen Shirt Bib recessed into the V-opening (NOT a box)
    shape('ShirtBib', (0, -0.20, 1.04), (0.13, 0.035, 0.24), linen_mat, body, 'cylinder', rot=(0.06, 0, 0), subsurf=1)

    # Shirt Collar tips
    shape('ShirtCollarL', (-0.07, -0.20, 1.26), (0.045, 0.025, 0.05), linen_mat, body, 'cone', rot=(0.4, 0.3, -0.2), subsurf=1)
    shape('ShirtCollarR', (0.07, -0.20, 1.26), (0.045, 0.025, 0.05), linen_mat, body, 'cone', rot=(0.4, -0.3, 0.2), subsurf=1)

    # BESPOKE CURVED LAPEL ROLLS: Upper gorge, mid peak, and lower waist taper
    for side in [-1, 1]:
        # Upper collar gorge curving outward
        shape('LapelGorge', (side * 0.11, -0.22, 1.18), (0.040, 0.028, 0.09), satin_mat, body, 'cylinder', rot=(0.10, side * 0.14, -side * 0.12), subsurf=1)
        # Mid lapel roll forming the chest peak
        shape('LapelRoll', (side * 0.15, -0.23, 1.04), (0.048, 0.030, 0.12), satin_mat, body, 'cylinder', rot=(0.06, side * 0.16, -side * 0.06), subsurf=1)
        # Lower lapel tapering inward towards the buttons
        shape('LapelLower', (side * 0.10, -0.24, 0.88), (0.038, 0.026, 0.10), satin_mat, body, 'cylinder', rot=(0.04, -side * 0.12, side * 0.08), subsurf=1)

    # Golden Filigree Buttons along the vest midline
    for z in [0.72, 0.84, 0.96]:
        shape('GoldButton', (0.0, -0.25, z), (0.022, 0.012, 0.022), accent_mat, body, 'cylinder', rot=(math.pi / 2, 0, 0), bevel=0.01)

    # Tailored Neckwear nestled in the collar notch
    if species in ('owl', 'crow'):
        shape('BowtieKnot', (0, -0.24, 1.24), (0.028, 0.022, 0.028), accent_mat, body, 'ico')
        shape('BowtieL', (-0.07, -0.23, 1.24), (0.065, 0.022, 0.040), satin_mat, body, 'ico')
        shape('BowtieR', (0.07, -0.23, 1.24), (0.065, 0.022, 0.040), satin_mat, body, 'ico')
    elif species == 'serpent':
        shape('QueenBrooch', (0, -0.23, 1.20), (0.060, 0.018, 0.060), ruby_mat, body, 'ico', subsurf=1)
        shape('BroochFrame', (0, -0.22, 1.20), (0.075, 0.015, 0.075), accent_mat, body, 'torus', rot=(math.pi / 2, 0, 0))
    else:
        shape('CravatSilk', (0, -0.23, 1.18), (0.045, 0.020, 0.11), satin_mat, body, 'cylinder', rot=(0.06, 0, 0), subsurf=1)

    if ident == 'bento':
        for side in [-1, 1]:
            shape('LeatherSuspender', (side * 0.14, -0.23, 0.85), (0.026, 0.015, 0.28), mat('SuspenderLeather', '#562818', rough=0.45), body, 'cylinder', rot=(0.04, side * 0.08, 0), subsurf=1)
            shape('SuspenderBuckle', (side * 0.14, -0.24, 0.64), (0.030, 0.015, 0.030), accent_mat, body, 'cube', bevel=0.01)

    if ident in ('zeca', 'corvo'):
        shape('WatchChainLoop', (0.12, -0.24, 0.72), (0.075, 0.016, 0.045), accent_mat, body, 'torus', rot=(0.28, 0.15, 0.20))

    if index >= 6:
        shape('RoyalCape', (0, 0.16, 0.78), (0.54, 0.10, 0.60), mat('CapeVelvet', '#261220' if index == 7 else '#1c1426', rough=0.88), body, 'cylinder', rot=(-0.05, 0, 0), subsurf=1)
        shape('CapeBraid', (0, 0.10, 1.26), (0.44, 0.09, 0.10), accent_mat, body, 'torus')

    # --- ARTICULATED ARMS (SMOOTH TAILORED SLEEVE CYLINDERS) ---
    for j, arm in enumerate(arms):
        forearm = forearms[j]
        side_f = -1 if j == 0 else 1

        # 1. Upper Arm (parent: arm)
        shape('ShoulderJoint', (0, 0, 0), (0.14, 0.15, 0.14), coat_mat, arm, 'ico', subsurf=1)
        shape('UpperSleeve', (0, -0.01, -0.15), (0.12, 0.13, 0.14), coat_mat, arm, 'cylinder', subsurf=1)

        # 2. Forearm (parent: forearm)
        shape('ElbowHinge', (0, 0, 0), (0.105, 0.11, 0.105), coat_mat, forearm, 'ico', subsurf=1)
        shape('ForearmSleeve', (0, -0.015, -0.13), (0.10, 0.105, 0.13), coat_mat, forearm, 'cylinder', rot=(0.08, 0, 0), subsurf=1)

        # Tailored jacket cuff hem
        shape('JacketCuffHem', (0, -0.03, -0.25), (0.105, 0.11, 0.022), coat_mat, forearm, 'cylinder', rot=(0.08, 0, 0), bevel=0.015)

        # White linen shirt cuff extending neatly from inside the jacket sleeve
        shape('ShirtCuff', (0, -0.035, -0.28), (0.088, 0.092, 0.030), linen_mat, forearm, 'cylinder', rot=(0.08, 0, 0), bevel=0.01)
        shape('Cufflink', (side_f * 0.08, -0.035, -0.28), (0.016, 0.016, 0.016), accent_mat, forearm, 'ico')

        # 3. Anatomical Hands / Aristocratic Crow Claws (parent: forearm)
        h_mat = mat('CrowClaw', '#12161f', rough=0.35) if species == 'crow' else skin_mat
        shape('Palm', (0, -0.05, -0.34), (0.078, 0.088, 0.050), h_mat, forearm, 'ico', subsurf=1)

        if species == 'crow':
            for claw_k in [-1, 0, 1]:
                shape(f'Digit_{claw_k}', (claw_k * 0.034, -0.09, -0.35), (0.020, 0.040, 0.020), h_mat, forearm, 'cylinder', rot=(0.4, 0, 0), subsurf=1)
                shape(f'Claw_{claw_k}', (claw_k * 0.034, -0.14, -0.37), (0.014, 0.040, 0.016), mat('TalonHorn', '#080a0e', rough=0.15, spec=0.95), forearm, 'cone', rot=(math.pi / 2 + 0.35, 0, 0))
        else:
            shape('ThumbBase', (side_f * 0.06, -0.04, -0.32), (0.032, 0.035, 0.038), h_mat, forearm, 'ico', rot=(0.25, side_f * 0.40, 0), subsurf=1)
            shape('ThumbTip', (side_f * 0.08, -0.06, -0.35), (0.025, 0.028, 0.032), h_mat, forearm, 'ico', rot=(0.35, side_f * 0.50, 0))
            shape('FingerKnuckles', (0, -0.10, -0.36), (0.070, 0.050, 0.040), h_mat, forearm, 'cylinder', rot=(math.pi / 2, 0, 0), bevel=0.015)

        if ident == 'bento' and j == 0:
            shape('WatchStrap', (0, -0.035, -0.26), (0.098, 0.102, 0.030), mat('WatchStrap', '#3a1c10', rough=0.4), forearm, 'cylinder')
            shape('WatchBezel', (-0.085, -0.035, -0.26), (0.028, 0.028, 0.025), accent_mat, forearm, 'cylinder', rot=(0, math.pi / 2, 0), bevel=0.01)

    # --- HEAD SCULPTURE & ARISTOCRATIC EXPRESSIONS ---
    for side in [-1, 1]:
        shape('EyeSocket', (side * 0.13, -0.20, 0.20), (0.085, 0.04, 0.085), mat('EyeLid', '#12161c', rough=0.5), head, 'ico', subsurf=1)
        shape('EyeIris', (side * 0.13, -0.23, 0.20), (0.052, 0.020, 0.055), amber_eyes, head, 'ico')
        shape('EyePupil', (side * 0.13, -0.245, 0.20), (0.024, 0.012, 0.030), pupil_mat, head, 'ico')
        shape('EyeGlint1', (side * 0.14, -0.252, 0.215), (0.012, 0.009, 0.012), glint_mat, head, 'ico')
        shape('EyeGlint2', (side * 0.12, -0.248, 0.185), (0.007, 0.006, 0.007), glint_mat, head, 'ico')

    brow_color = '#141820' if species in ('human', 'crow', 'serpent') else skinhex
    brow_mat = mat(f'{ident}_brow', brow_color, rough=0.7)
    for side in [-1, 1]:
        shape('Eyebrow', (side * 0.13, -0.22, 0.28), (0.070, 0.022, 0.025), brow_mat, head, 'ico', rot=(0.12, side * 0.10, -side * 0.06), subsurf=1)

    # --- SPECIES-SPECIFIC ANATOMICAL SCULPTING ---
    if species == 'crow':
        # Aerodynamic elongated raven skull
        shape('HeadCranium', (0, 0.04, 0.20), (0.24, 0.30, 0.23), skin_mat, head, 'ico', subsurf=1)

        # GOTHIC ARISTOCRATIC RAVEN BEAK (PROMINENT FORWARD CULMEN)
        # Base of bill
        shape('BeakBase', (0, -0.20, 0.19), (0.070, 0.075, 0.10), mat('RavenBeak', '#0e1218', rough=0.22, spec=0.9), head, 'ico', subsurf=1)
        # Mid culmen (length along Z is 0.24, rotated forward around X by pi/2 + 0.10)
        shape('BeakCulmenMid', (0, -0.32, 0.17), (0.048, 0.055, 0.22), mat('RavenBeak', '#0e1218', rough=0.20, spec=0.92), head, 'cone', rot=(math.pi / 2 + 0.10, 0, 0), subsurf=1)
        # Needle beak tip (length along Z is 0.18, hooked down)
        shape('BeakTip', (0, -0.48, 0.13), (0.028, 0.035, 0.18), mat('RavenBeak', '#080a0e', rough=0.18, spec=0.95), head, 'cone', rot=(math.pi / 2 + 0.30, 0, 0))
        # Slender lower mandible fitting neatly inside upper bill
        shape('BeakLowerMandible', (0, -0.32, 0.12), (0.038, 0.040, 0.20), mat('RavenBeak', '#0a0d12', rough=0.25, spec=0.85), head, 'cone', rot=(math.pi / 2 + 0.04, 0, 0), subsurf=1)

        # Stiff black narial bristles
        shape('NarialBristles', (0, -0.22, 0.22), (0.060, 0.07, 0.022), mat('RavenBristles', '#050709', rough=0.9), head, 'cylinder', rot=(0.18, 0, 0), subsurf=1)

        # Streamlined crest feathers swept BACKWARDS along the nape
        shape('NapeCrestBack', (0, 0.24, 0.22), (0.14, 0.14, 0.16), skin_mat, head, 'cone', rot=(-0.75, 0, 0), subsurf=1)

        # Gold Pince-Nez Spectacles perched on the bridge of the beak
        for side in [-1, 1]:
            shape('SpecRim', (side * 0.10, -0.22, 0.20), (0.070, 0.070, 0.020), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('SpecBridge', (0, -0.23, 0.21), (0.035, 0.014, 0.009), accent_mat, head, 'cube', bevel=0.01)
        shape('SpecChain', (0.11, -0.20, 0.09), (0.005, 0.005, 0.11), accent_mat, head, 'cylinder')

    elif species == 'owl':
        shape('HeadCranium', (0, 0, 0.20), (0.30, 0.26, 0.32), skin_mat, head, 'ico', subsurf=1)
        for side in [-1, 1]:
            shape('OwlHorn', (side * 0.26, 0.02, 0.50), (0.08, 0.10, 0.22), skin_mat, head, 'cone', rot=(-0.2, side * 0.28, 0), subsurf=1)
            shape('FacialDisc', (side * 0.14, -0.14, 0.20), (0.16, 0.06, 0.18), mat('FacialFeather', '#5c486a', rough=0.8), head, 'ico', subsurf=1)
        shape('OwlHookBeak', (0, -0.30, 0.11), (0.07, 0.08, 0.16), accent_mat, head, 'cone', rot=(math.pi / 2 + 0.26, 0, 0), subsurf=1)
        shape('MonocleRim', (0.13, -0.24, 0.20), (0.09, 0.09, 0.022), accent_mat, head, 'torus', rot=(math.pi / 2, 0, 0))
        shape('MonocleChain', (0.15, -0.22, 0.05), (0.006, 0.006, 0.16), accent_mat, head, 'cylinder')
        for k in range(5):
            a = (k - 2) * 0.38
            shape('CrownSpike', (0.20 * math.sin(a), -0.09, 0.55 + 0.07 * (k % 2)), (0.04, 0.04, 0.13), accent_mat, head, 'cone')
        shape('CrownCirclet', (0, 0, 0.48), (0.26, 0.23, 0.05), accent_mat, head, 'cylinder', bevel=0.01)

    elif species == 'jaguar':
        shape('HeadCranium', (0, 0, 0.20), (0.30, 0.26, 0.32), skin_mat, head, 'ico', subsurf=1)
        shape('JaguarSnout', (0, -0.26, 0.13), (0.14, 0.15, 0.12), skin_mat, head, 'ico', subsurf=1)
        shape('JaguarNose', (0, -0.36, 0.15), (0.070, 0.055, 0.045), mat('BlackNose', '#1a1412', rough=0.35), head, 'ico')
        shape('ChinTuft', (0, -0.26, 0.04), (0.09, 0.09, 0.07), white_fur, head, 'ico', subsurf=1)
        for side in [-1, 1]:
            shape('PredatorEar', (side * 0.24, 0.04, 0.44), (0.10, 0.06, 0.15), skin_mat, head, 'ico', rot=(0.1, side * 0.25, 0), subsurf=1)
            shape('InnerEar', (side * 0.24, -0.01, 0.44), (0.06, 0.030, 0.09), white_fur, head, 'ico', rot=(0.1, side * 0.25, 0))

    elif species == 'capybara':
        shape('HeadCranium', (0, 0, 0.20), (0.30, 0.26, 0.32), skin_mat, head, 'ico', subsurf=1)
        shape('CapySnout', (0, -0.28, 0.13), (0.18, 0.18, 0.14), skin_mat, head, 'ico', subsurf=1)
        shape('CapyNose', (0, -0.39, 0.15), (0.09, 0.065, 0.055), mat('CapyNose', '#3c2820', rough=0.4), head, 'ico')
        for side in [-1, 1]:
            shape('CapyEar', (side * 0.22, 0.05, 0.35), (0.075, 0.055, 0.08), mat('DarkEar', '#5a3d28', rough=0.55), head, 'ico', subsurf=1)
        shape('WaterLilyBloom', (-0.16, 0.02, 0.48), (0.09, 0.09, 0.06), mat('LilyPink', '#e26b8e', rough=0.35, spec=0.9), head, 'ico', subsurf=1)
        shape('WaterLilyCenter', (-0.16, 0.02, 0.51), (0.030, 0.030, 0.030), mat('LilyGold', '#f5c040', rough=0.2, emit_hex='#f5c040', emit_strength=0.25), head, 'ico')

    elif species == 'fox':
        shape('HeadCranium', (0, 0, 0.20), (0.28, 0.26, 0.30), skin_mat, head, 'ico', subsurf=1)
        shape('FoxMuzzle', (0, -0.28, 0.12), (0.11, 0.12, 0.18), skin_mat, head, 'cone', rot=(math.pi / 2 + 0.1, 0, 0), subsurf=1)
        shape('FoxNoseTip', (0, -0.42, 0.11), (0.035, 0.035, 0.035), mat('FoxNose', '#121214', rough=0.25), head, 'ico')
        shape('FoxRuff', (0, -0.16, -0.02), (0.18, 0.14, 0.14), white_fur, head, 'ico', subsurf=1)
        for side in [-1, 1]:
            shape('FoxEar', (side * 0.20, 0.02, 0.48), (0.08, 0.065, 0.20), skin_mat, head, 'cone', rot=(-0.15, side * 0.22, 0), subsurf=1)
            shape('FoxEarInner', (side * 0.20, -0.02, 0.48), (0.045, 0.030, 0.13), white_fur, head, 'cone', rot=(-0.15, side * 0.22, 0))

    elif species == 'serpent':
        shape('HeadCranium', (0, 0, 0.20), (0.28, 0.26, 0.30), skin_mat, head, 'ico', subsurf=1)
        shape('CobraHood', (0, 0.07, 0.20), (0.38, 0.12, 0.36), coat_mat, head, 'ico', subsurf=1)
        shape('SerpentJaw', (0, -0.24, 0.13), (0.12, 0.16, 0.11), skin_mat, head, 'ico', subsurf=1)
        for k in range(5):
            a = (k - 2) * 0.36
            shape('CrownSpike', (0.18 * math.sin(a), -0.06, 0.51 + 0.06 * (k % 2)), (0.035, 0.035, 0.12), ruby_mat, head, 'cone')
        shape('CrownRing', (0, 0, 0.44), (0.23, 0.20, 0.045), accent_mat, head, 'cylinder', bevel=0.01)

    else:
        # Human Archetypes (Nina & Bento)
        shape('HeadCranium', (0, 0, 0.20), (0.30, 0.26, 0.32), skin_mat, head, 'ico', subsurf=1)
        shape('HumanNose', (0, -0.25, 0.15), (0.035, 0.045, 0.060), skin_mat, head, 'cone', rot=(math.pi / 2, 0, 0), subsurf=1)
        shape('HumanMouth', (0, -0.22, 0.06), (0.065, 0.022, 0.020), mat('Lips', '#9e5246', rough=0.5), head, 'ico')
        if ident == 'nina':
            shape('HairTop', (0, 0.03, 0.35), (0.31, 0.28, 0.20), mat('NinaHair', '#2c1e18', rough=0.55), head, 'ico', subsurf=1)
            for side in [-1, 1]:
                shape('HairBob', (side * 0.20, 0.00, 0.22), (0.15, 0.16, 0.20), mat('NinaHair', '#2c1e18', rough=0.55), head, 'ico', subsurf=1)
            shape('HairRibbon', (0, 0.15, 0.44), (0.08, 0.030, 0.06), accent_mat, head, 'ico')
        else:
            shape('HairBase', (0, 0.04, 0.36), (0.32, 0.26, 0.18), mat('BentoHair', '#2a1a12', rough=0.55), head, 'ico', subsurf=1)
            for side in [-1, 1]:
                shape('HairSideLock', (side * 0.19, 0.02, 0.40), (0.14, 0.11, 0.09), mat('BentoHair', '#2a1a12', rough=0.55), head, 'ico', subsurf=1)
                shape('HandlebarMoustache', (side * 0.08, -0.27, 0.05), (0.095, 0.030, 0.040), mat('BentoHair', '#2a1a12', rough=0.45), head, 'ico', rot=(0, side * 0.35, side * 0.22), subsurf=1)

    # --- JOIN MESHES PER ARTICULATED GROUP ---
    for group in [body, head] + arms + forearms:
        meshes = [o for o in group.children if o.type == 'MESH']
        if meshes:
            bpy.ops.object.select_all(action='DESELECT')
            for o in meshes:
                o.select_set(True)
            bpy.context.view_layer.objects.active = meshes[0]
            bpy.ops.object.join()
            bpy.context.object.name = f"{group.name}Mesh"
            for poly in bpy.context.object.data.polygons:
                poly.use_smooth = True

    # --- BEZIER PHYSICS ANIMATIONS (6 Required Clips) ---
    sign = -1 if index % 2 else 1

    for clip in ['idle', 'entrance', 'truco', 'victory', 'boss_intro', 'flourish']:
        if clip == 'idle':
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (18, (0, -0.008, 0.016), (0.014, 0, 0)),
                (34, (0, -0.014, 0.022), (0.020, sign * 0.010, 0)),
                (50, (0, -0.006, 0.010), (0.008, sign * 0.005, 0)),
                (64, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (20, (0, 0, 0.008), (-0.015, sign * 0.020, sign * 0.010)),
                (36, (0, 0, 0.012), (-0.020, -sign * 0.015, 0)),
                (52, (0, 0, 0.005), (-0.006, 0, 0)),
                (64, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                    (34, (0, 0, 0.006), (0.08, side_f * 0.04, 0)),
                    (64, (0, 0, 0), (0.10, side_f * 0.04, 0))
                ])
            for j, forearm in enumerate(forearms):
                animate(forearm, clip, [
                    (1,  (0, 0, 0), (0.45, 0, 0)),
                    (34, (0, -0.003, 0.004), (0.42, 0, 0)),
                    (64, (0, 0, 0), (0.45, 0, 0))
                ])

        elif clip == 'entrance':
            animate(body, clip, [
                (1,  (0, 0, 0.10), (0, sign * 0.06, 0)),
                (14, (0, 0, -0.03), (0.03, -sign * 0.03, 0)),
                (28, (0, 0, 0.02), (-0.01, sign * 0.01, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0.02), (0.06, -sign * 0.10, -sign * 0.06)),
                (16, (0, 0, 0.03), (-0.05, sign * 0.12, sign * 0.05)),
                (30, (0, 0, 0.01), (0.03, -sign * 0.04, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (-0.15, side_f * 0.05, 0)),
                    (16, (0, -0.05, 0.04), (0.20, sign * 0.10, 0.08 * side_f)),
                    (32, (0, -0.02, 0), (0.14, 0, 0)),
                    (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                ])
            for j, forearm in enumerate(forearms):
                animate(forearm, clip, [
                    (1,  (0, 0, 0), (0.20, 0, 0)),
                    (16, (0, 0, 0.02), (0.68, 0, 0)),
                    (32, (0, 0, 0.01), (0.52, 0, 0)),
                    (48, (0, 0, 0), (0.45, 0, 0))
                ])

        elif clip == 'truco':
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (10, (0, 0.04, -0.02), (-0.06, 0, 0)),
                (20, (0, -0.14, 0.06), (0.20, sign * 0.05, 0)),
                (32, (0, -0.08, 0.03), (0.12, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (10, (0, 0, 0.02), (-0.10, 0, 0)),
                (20, (0, -0.06, 0.04), (0.24, sign * 0.06, 0)),
                (34, (0, -0.03, 0.02), (0.10, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                is_active = (j == (0 if index % 2 == 0 else 1))
                if is_active:
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                        (10, (0, 0.04, 0.06), (-0.35, side_f * 0.10, 0)),
                        (20, (0, -0.12, 0.04), (0.35, side_f * 0.15, -side_f * 0.08)),
                        (34, (0, -0.08, 0.02), (0.25, side_f * 0.08, -side_f * 0.04)),
                        (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                    ])
                else:
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                        (14, (0, 0.02, 0), (-0.05, 0, 0)),
                        (24, (0, -0.06, -0.02), (0.16, -side_f * 0.05, 0)),
                        (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                    ])
            for j, forearm in enumerate(forearms):
                side_f = -1 if j == 0 else 1
                is_active = (j == (0 if index % 2 == 0 else 1))
                if is_active:
                    animate(forearm, clip, [
                        (1,  (0, 0, 0), (0.45, 0, 0)),
                        (10, (0, 0, 0.02), (1.05, 0, 0)),
                        (20, (0, -0.04, 0.01), (0.14, 0, -side_f * 0.12)),
                        (34, (0, -0.02, 0.01), (0.28, 0, -side_f * 0.06)),
                        (48, (0, 0, 0), (0.45, 0, 0))
                    ])
                else:
                    animate(forearm, clip, [
                        (1,  (0, 0, 0), (0.45, 0, 0)),
                        (24, (0, 0, 0), (0.58, 0, 0)),
                        (48, (0, 0, 0), (0.45, 0, 0))
                    ])

        elif clip == 'victory':
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.09), (-0.07, sign * 0.05, 0)),
                (26, (0, 0, 0.12), (-0.10, -sign * 0.03, 0)),
                (38, (0, 0, 0.05), (-0.03, sign * 0.02, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.05), (-0.15, sign * 0.10, sign * 0.06)),
                (26, (0, 0, 0.07), (-0.18, -sign * 0.06, -sign * 0.04)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                    (14, (0, -0.04, 0.16), (-0.80, side_f * 0.25, side_f * 0.20)),
                    (26, (0, -0.06, 0.20), (-0.90, side_f * 0.28, side_f * 0.22)),
                    (38, (0, -0.03, 0.10), (-0.42, side_f * 0.15, side_f * 0.10)),
                    (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                ])
            for j, forearm in enumerate(forearms):
                side_f = -1 if j == 0 else 1
                animate(forearm, clip, [
                    (1,  (0, 0, 0), (0.45, 0, 0)),
                    (14, (0, 0, 0.02), (1.25, 0, side_f * 0.25)),
                    (26, (0, 0, 0.03), (1.38, 0, side_f * 0.30)),
                    (38, (0, 0, 0.01), (0.85, 0, side_f * 0.12)),
                    (48, (0, 0, 0), (0.45, 0, 0))
                ])

        elif clip == 'boss_intro':
            animate(body, clip, [
                (1,  (0, 0, 0.06), (0, 0, 0)),
                (16, (0, -0.05, 0.14), (0.07, 0, 0)),
                (28, (0, -0.08, 0.10), (0.12, sign * 0.04, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0.02), (-0.12, 0, 0)),
                (16, (0, 0, 0.06), (-0.18, sign * 0.06, 0)),
                (28, (0, -0.03, 0.05), (0.14, 0, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                animate(arm, clip, [
                    (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                    (18, (side_f * 0.12, 0, 0.14), (0.15, side_f * 0.65, -side_f * 0.35)),
                    (32, (side_f * 0.06, -0.04, 0.06), (0.32, side_f * 0.35, -side_f * 0.18)),
                    (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                ])
            for j, forearm in enumerate(forearms):
                side_f = -1 if j == 0 else 1
                animate(forearm, clip, [
                    (1,  (0, 0, 0), (0.45, 0, 0)),
                    (18, (0, 0, 0.02), (0.22, side_f * 0.15, 0)),
                    (32, (0, 0, 0.01), (0.38, side_f * 0.08, 0)),
                    (48, (0, 0, 0), (0.45, 0, 0))
                ])

        elif clip == 'flourish':
            animate(body, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, -0.03, 0.03), (0.04, sign * 0.05, 0)),
                (28, (0, 0.01, 0.01), (-0.03, -sign * 0.02, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            animate(head, clip, [
                (1,  (0, 0, 0), (0, 0, 0)),
                (14, (0, 0, 0.02), (0.06, sign * 0.10, sign * 0.04)),
                (28, (0, 0, 0.01), (-0.04, -sign * 0.06, 0)),
                (48, (0, 0, 0), (0, 0, 0))
            ])
            for j, arm in enumerate(arms):
                side_f = -1 if j == 0 else 1
                is_active = (j == (0 if index % 2 == 0 else 1))
                if is_active:
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                        (14, (0, -0.08, 0.06), (-0.22, side_f * 0.35, -side_f * 0.20)),
                        (28, (0, -0.05, 0.03), (-0.08, side_f * 0.15, -side_f * 0.08)),
                        (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                    ])
                else:
                    animate(arm, clip, [
                        (1,  (0, 0, 0), (0.10, side_f * 0.04, 0)),
                        (20, (0, 0, 0.01), (0.07, 0, 0)),
                        (48, (0, 0, 0), (0.10, side_f * 0.04, 0))
                    ])
            for j, forearm in enumerate(forearms):
                side_f = -1 if j == 0 else 1
                is_active = (j == (0 if index % 2 == 0 else 1))
                if is_active:
                    animate(forearm, clip, [
                        (1,  (0, 0, 0), (0.45, 0, 0)),
                        (14, (0, 0, 0.02), (0.80, -side_f * 0.30, side_f * 0.20)),
                        (28, (0, 0, 0.01), (0.60, -side_f * 0.15, side_f * 0.10)),
                        (48, (0, 0, 0), (0.45, 0, 0))
                    ])
                else:
                    animate(forearm, clip, [
                        (1,  (0, 0, 0), (0.45, 0, 0)),
                        (48, (0, 0, 0), (0.45, 0, 0))
                    ])

    # Scene setup
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 64
    scene.render.fps = 24
    scene.frame_set(1)

    # Export glTF 2.0 with all NLA tracks and applied modifiers
    bpy.ops.object.select_all(action='SELECT')
    out_glb = OUT / f"{ident}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(out_glb),
        export_format='GLB',
        use_selection=True,
        use_active_scene=True,
        export_current_frame=True,
        export_apply=True,
        export_animation_mode='NLA_TRACKS',
        export_force_sampling=True,
        export_frame_range=True
    )

    # Studio Portrait Render (320x400 transparent PNG)
    bpy.ops.object.camera_add(location=(1.7, -4.6, 2.0))
    camera = bpy.context.object
    camera.name = 'PortraitCamera'
    camera.rotation_euler = (Vector((0, 0, 1.12)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
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
    bpy.data.libraries.write(str(SOURCE / f"{ident}.blend"), {scene}, fake_user=True)
    bpy.ops.render.render(write_still=True)

    print(f"[Blender] Stylized AAA model generated: {ident}.glb ({out_glb.stat().st_size} bytes)", flush=True)

def main():
    for i, row in enumerate(CAST):
        build(i, *row)

    manifest = {
        'generator': 'Blender 5.2.1 Stylized AAA Pipeline',
        'characters': [c[0] for c in CAST],
        'clips': ['idle', 'entrance', 'truco', 'victory', 'boss_intro', 'flourish'],
        'style': 'Stylized Triple-A high-fidelity character meshes, continuous organic anatomy, tailored clothing & PBR materials'
    }
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    print("[Blender] All 8 Stylized AAA characters built successfully!", flush=True)

if __name__ == '__main__':
    main()
