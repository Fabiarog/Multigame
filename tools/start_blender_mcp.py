import bpy

try:
    bpy.ops.preferences.addon_enable(module='blender_mcp')
    print("[BlenderMCP] Addon enabled.")
except Exception as e:
    print("[BlenderMCP] Addon enable notice:", e)

try:
    bpy.ops.blendermcp.start_server()
    print("[BlenderMCP] Server started successfully on port 9876!")
except Exception as e:
    print("[BlenderMCP] Server start error:", e)
