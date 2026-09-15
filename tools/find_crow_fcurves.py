import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath="assets/modelos 3d detalhados/crowrigconjay.blend")

rig = bpy.data.objects.get("rig")
act = rig.animation_data.action if rig.animation_data else None

# Check animated bone names in all layers/strips/fcurves
animated_paths = set()
for fcurve in getattr(act, 'fcurves', []):
    animated_paths.add(fcurve.data_path)

if hasattr(act, 'layers'):
    for layer in act.layers:
        for strip in getattr(layer, 'strips', []):
            for cb in getattr(strip, 'channelbags', []):
                for fc in getattr(cb, 'fcurves', []):
                    animated_paths.add(fc.data_path)

print(f"Total animated data paths: {len(animated_paths)}")
for p in sorted(list(animated_paths))[:30]:
    print(" ", p)
