extends SceneTree

func _init():
    var project_dir = 'c:/workspace/multigame'
    change_scene_to_file('res://hub/scenes/HubMain.tscn')
    await create_timer(0.5).timeout
    for i in range(5): await process_frame
    
    # Click Coleção
    for btn in current_scene.find_children('*', 'Button', true, false):
        if btn.text == 'Coleção':
            btn.emit_signal('pressed')
            break
            
    await create_timer(0.5).timeout
    for i in range(5): await process_frame
    
    var chars_to_test = ['Aki', 'Seu Corvo', 'Barão da Meia-Noite', 'Dama de Copas', 'Zeca', 'Iara']
    for char_name in chars_to_test:
        for btn in current_scene.find_children('*', 'Button', true, false):
            if char_name in btn.text:
                btn.emit_signal('pressed')
                break
        await create_timer(0.6).timeout
        for i in range(5): await process_frame
        await RenderingServer.frame_post_draw
        var shot = root.get_texture().get_image()
        var safe_name = char_name.to_lower().replace(' ', '_').replace('ã', 'a').replace('õ', 'o')
        shot.save_png('c:/workspace/multigame/docs/screenshots/colecao_' + safe_name + '.png')
        print('Captured colecao for ', char_name)
        
    print('ALL CHARACTERS CAPTURED!')
    quit(0)
