# Passe 30 — acabamento da lareira

Base recebida 87a7b98, preservada em Backup 18bdc3b antes da edição. Main permanece intacta. Preservar o sistema novo de atenção/direção de partida.

Problema confirmado pelo Blender MCP: três troncos inteiramente associados a ClassicFireGlow, produzindo superfícies amarelas sem leitura de madeira. Acabamentos da lareira têm apenas 24 vértices e quinas rígidas.

Escopo: usar o modelo editável do Patch 27; madeira carbonizada fosca e emissão restrita às pontas; bevel físico no acabamento; manter arquitetura, máscaras, número de luzes e agrupamento espacial. Preparar UV2 separada, se a exportação puder ser verificada, sem afirmar bake LightmapGI concluído. Sem assets externos ou geração paga: o ajuste usa objetos existentes e geometria paramétrica de bordas.

Blender 5.2.1 MCP 9876, cena temporária e original restaurada. Fonte de saída separada em art/blender/patch30. Reimportar e capturar no Godot 4.7.2 antes da integração final. Comparar 1080p/4K e executar regressões de sala, cartas e interface.

Pesquisa: [materiais StandardMaterial3D](https://docs.godotengine.org/en/stable/tutorials/3d/standard_material_3d.html) e [BaseMaterial3D 4.7](https://docs.godotengine.org/en/4.7/classes/class_basematerial3d.html). Emissão de material e iluminação projetada na sala são controles distintos; este passe mantém a fonte de luz existente. Nenhum plugin necessário. CLI game-dev ausente, então não existem recibos de pacote dessa ferramenta.

Não escalar ainda às outras salas. Próxima pergunta opcional permanece: Lounge do Barão ou Cassino Cyber depois de validar o Classic Club?

## Resultado de autoria e revisão

- Troncos: madeira carbonizada fosca e brasas nas faces das pontas, em vez de emissão em todo o tronco. Deslocamento de 0,30 unidade para frente para evitar interseção com o fundo.
- Painel de fundo: espessura reduzida de 0,50 para 0,06 e colocado à frente da parede, atrás dos troncos. Não foi aberto um buraco na arquitetura da parede.
- Quatro peças de acabamento: soldagem dos vértices coincidentes, seguida de bevel de 0,035/3 segmentos. Cada peça passou de 24 para 96 vértices. O primeiro candidato sem soldagem não produziu bevel e foi rejeitado; o candidato com fundo atrás da parede também foi rejeitado.
- Sala de fonte: 30.404 → 30.692 vértices e 15.856 → 16.560 triângulos. Mantidos seis grupos; arquivo GLB 1.254.764 → 1.600.876 bytes, incluindo UV2. Nenhuma luz ou efeito de pós-processamento adicionado.
- UV2: criada depois do agrupamento para evitar sobreposição entre objetos unidos; exportada em todas as 37 primitivas. Verificação de presença/formato/limites 0–1 passou. Não substitui avaliação de densidade, margens, sobreposição ou qualidade de bake no editor.
- Fontes de autoria e exportação separadas em art/blender/patch30; a fonte original do Patch 27 não foi modificada. Cena aberta no Blender restaurada. Integrações consultadas estavam desativadas; nenhum download ou serviço de geração foi usado.
- Feedback local de revisão registrado neste plano, pois record_trajectory_feedback não está disponível entre as ferramentas desta sessão.

Próximo passo de iluminação: construir cena estática de bake no Godot usando a UV2 candidata, ajustar densidade e comparar LightmapGI/probes com o perfil atual. Não habilitar LightmapGI sem essa comparação.
