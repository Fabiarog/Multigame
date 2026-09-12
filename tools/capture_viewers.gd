extends SceneTree

func _init():
    var project_dir = 'c:/workspace/multigame'
    var packed_hub = load('res://hub/scenes/HubMain.tscn')
    if not packed_hub:
        print('Could not load HubMain.tscn')
        quit(1)
        return
    var hub = packed_hub.instantiate()
    root.add_child(hub)
    
    # Wait frames
    await process_frame
    await process_frame
    
    # Switch to Collectibles
    hub.call('SwitchUi', 2) # Collectibles
    await process_frame
    await process_frame
    
    var viewer = hub.find_child('CharacterViewer3D', true, false)
    if not viewer:
        print('CharacterViewer3D not found!')
        quit(1)
        return
        
    var char_indices = {'corvo': 2, 'iara': 4, 'zeca': 5, 'barao': 6, 'dama': 7}
    for ident in char_indices:
        var idx = char_indices[ident]
        viewer.call('SetCharacter', idx)
        # Give time to render
        for i in range(5):
            await process_frame
            
        var img = root.get_viewport().get_texture().get_image()
        var out_path = 'c:/workspace/multigame/docs/screenshots/viewer_' + ident + '.png'
        img.save_png(out_path)
        print('Saved viewer screenshot for ', ident, ' -> ', out_path)
        
    hub.free()
    print('ALL VIEWER SCREENSHOTS CAPTURED!')
    quit(0)
