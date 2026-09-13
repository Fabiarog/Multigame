---
name: blender-asset-strategy
description: Estratégia canônica de criação, importação, rigging e validação de assets 3D no Blender via MCP para o MultiGame.
---

# Estratégia Canônica de Criação de Assets no Blender via MCP

Este skill orienta a produção de modelos 3D, rigs, materiais e animações para o MultiGame no Blender 5.2 conectado via MCP em `127.0.0.1:9876`.

## 1. Verificação Inicial Obrigatória
1. Obter informações da cena ativa:
   ```python
   # Via MCP: get_scene_info()
   ```
2. Capturar screenshot do viewport antes de modificar:
   ```python
   # Via MCP: get_viewport_screenshot()
   ```

## 2. Isolamento de Cena e Preservação
Nunca crie ou manipule malhas diretamente na cena ativa do usuário (`Scene`).
Sempre use um padrão de isolamento estrito:

```python
orig_scene = bpy.context.window.scene
temp_scene = bpy.data.scenes.new("TempWorkScene")
bpy.context.window.scene = temp_scene

try:
    # Trabalho 3D aqui...
finally:
    # Limpeza segura
    for obj in list(temp_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.window.scene = orig_scene
    bpy.data.scenes.remove(temp_scene)
```

## 3. Prioridade de Busca de Assets
1. **Modelos Específicos:** `search_sketchfab_models()` $\rightarrow$ `download_polyhaven_asset(asset_type="models")`.
2. **Low-poly / Estilizados:** `search_polypizza_models()` $\rightarrow$ `download_polypizza_model()` (respeitar licença CC-BY com atribuição).
3. **Mobiliário Genérico:** `download_polyhaven_asset()` $\rightarrow$ Sketchfab.
4. **Personagens Únicos / Esculturas Customizadas:** `generate_hyper3d_model_via_text()` / `generate_hyper3d_model_via_images()` ou Hunyuan3D.
5. **HDRIs:** `download_polyhaven_asset(asset_type="hdris")`.
6. **Texturas PBR:** `download_polyhaven_asset(asset_type="textures")`.

## 4. Validação de Escala e Bounding Box
Após importar qualquer modelo gerado ou externo:
- Inspecione as dimensões e o ponto de apoio:
  ```python
  bbox = [obj.matrix_world @ Vector(b) for b in obj.bound_box]
  min_z = min(b.z for b in bbox)
  # O piso do jogo fica em Z = 0.0m
  obj.location.z -= min_z
  ```
- Personagens devem seguir o padrão canônico ereto de 1.85m (`Rest Pose` com Z de 0.0m a 1.85m).

## 5. Skinning de Alta Performance
- **NUNCA** faça milhares de chamadas repetitivas à C-API do Blender em loops Python (ex: chamar `vertex_groups.add([idx])` dezenas de milhares de vezes causa gargalo e timeout de socket).
- Calcule todas as distâncias e difusões laplacianas em memória (usando dicionários nativos Python `{vert_idx: {bone_name: weight}}`).
- Escreva na malha do Blender com uma única passada por vértice.

## 6. Exportação GLB Limpa (1 Única Malha Deformadora)
Antes de exportar com `export_scene.gltf`:
```python
bpy.context.window.scene = orig_scene
bpy.ops.object.select_all(action='DESELECT')
bpy.context.window.scene = temp_scene
bpy.ops.object.select_all(action='DESELECT')

arm_obj.select_set(True)
mesh_obj.select_set(True)
head_marker.select_set(True)
bpy.context.view_layer.objects.active = arm_obj

bpy.ops.export_scene.gltf(
    filepath=export_path,
    use_selection=True,
    export_format='GLB',
    export_animations=True,
    export_nla_strips=True
)
```

## 7. Registro de Trajetória
- Ao receber aprovação do usuário: `record_trajectory_feedback(feedback="accept")`.
- Ao receber rejeição: `record_trajectory_feedback(feedback="reject")`.
- Ao receber correções: `record_trajectory_feedback(feedback="correction", correction_text="...")`.
