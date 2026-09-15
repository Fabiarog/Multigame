"""Build the user's current cast recipe in separate live Blender scenes.
Writes base sources; preserve manual edits before running. Then run refine and polish.
"""
import bpy
import runpy
from pathlib import Path

pipeline = runpy.run_path(str(Path(__file__).resolve().with_name('build_blender_cast.py')))
original = bpy.context.window.scene
try:
    for index, row in enumerate(pipeline['CAST']):
        bpy.context.window.scene = bpy.data.scenes.new('CastSource_' + row[0])
        pipeline['build'](index, *row)
finally:
    bpy.context.window.scene = original
