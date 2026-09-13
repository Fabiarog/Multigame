# Direção de arte e trabalho 3D

- Preferência explícita de Lucas: usar Blender para criar ou refinar modelos, rigs e animações, pelo MCP local em `127.0.0.1:9876`. Conferir conexão; não substituir isso por um ajuste de código apresentado como trabalho de modelagem.
- Seguir `docs/MISSAO_MODELOS_3D.md`: evolução estilizada, leve e reconhecível. Animação tem prioridade; não aplicar subdivisão indiscriminada nem converter o elenco para hiper-realismo.
- Auditar cada asset antes de alterar. Preservar fontes, gerar candidatos separados, comparar poses/animações no Blender e validar importação no Godot antes da substituição.
- Especificar o que foi de fato realizado: rig de peças rígidas não é retopologia orgânica; controle IK de autoria não prova suporte a IK em runtime; validação de limites não substitui inspeção visual de deformação.
- Preservar a versão recebida em Backup no GitHub e publicar o resultado em review. Não alterar main. Preservar o histórico de Backup quando houver divergência.
- Atualizar AI_DEV_PATCH_NOTES.md e CONTINUAR_TRABALHO.md com evidências, pendências e comandos de reprodução. Não descartar a cena aberta no Blender.

## Estratégia Canônica de Assets 3D (Blender MCP `127.0.0.1:9876`)

1. **Inspeção Prévia e Preservação de Cena:**
   - Chamar sempre `get_scene_info()` antes de qualquer operação.
   - Capturar screenshot do viewport com `get_viewport_screenshot()` ANTES e DEPOIS de executar alterações para validação visual.
   - Criar e operar estritamente em cenas temporárias (`bpy.data.scenes.new(...)`), restaurando a cena original do usuário ao final.
   - Nunca executar seleções ou exportações sem deselecionar objetos em todas as cenas (`select_all(action='DESELECT')`), evitando vazamento de malhas (`Cube`, `Light`) para o GLB final.

2. **Hierarquia de Integrações e Busca de Assets:**
   - Verificar status via ferramentas nativas: `get_polyhaven_status()`, `get_sketchfab_status()`, `get_polypizza_status()`, `get_hyper3d_status()`, `get_hunyuan3d_status()`.
   - **Prioridade de Origem:**
     1. *Objetos específicos/existentes:* Sketchfab $\rightarrow$ PolyHaven.
     2. *Assets estilizados/low-poly:* Poly Pizza (CC0/CC-BY com atribuição) $\rightarrow$ Sketchfab.
     3. *Mobiliário e objetos genéricos:* PolyHaven $\rightarrow$ Sketchfab.
     4. *Itens customizados/personagens originais:* Hyper3D Rodin ou Hunyuan3D (geração de modelo único, polling e importação com verificação de bounding box).
     5. *Iluminação e HDRIs:* PolyHaven HDRIs.
     6. *Materiais e Texturas:* PolyHaven textures.
   - Scripting procedural no Blender somente quando nenhuma integração atender ou quando geometria paramétrica for requerida.

3. **Performance e Integridade no Blender Python:**
   - Evitar chamadas repetitivas de C-API do Blender em loops gigantes (ex: `vertex_groups.add` ou modificadores por vértice): calcular difusão laplaciana e normalização de pesos em dicionários nativos Python antes de transferir para a malha.
   - Verificar `world_bounding_box` de cada asset importado para garantir escala e repouso corretos (ex: Z=0 no piso, escala 1:1 métrica).
   - Registrar feedback de trajetória (`record_trajectory_feedback`) em aprovações (`accept`), rejeições (`reject`) e correções (`correction`).

