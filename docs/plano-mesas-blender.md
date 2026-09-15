# Mesas Blender, pilhas 3D e elenco — entrega e continuação

Data: 5 de setembro de 2026. Continuação da [repaginada do clube](plano-repaginacao-clube.md), incorporando as alterações posteriores do usuário em vídeo, gráficos e infraestrutura de rede.

Registro histórico desta etapa. A versão de 6 de setembro usa o elenco arredondado e os cenários atualizados pelo usuário, seguidos dos refinamentos via MCP descritos no **Patch 8** de [AI_DEV_PATCH_NOTES.md](../AI_DEV_PATCH_NOTES.md). Os números de desempenho abaixo pertencem à arte anterior; consulte o patch para a validação atual.

## Implementado

### Elenco e Blender

O **Blender 5.2.1 instalado neste computador foi utilizado de fato**, em modo background com `bpy`, para construir, animar, renderizar retratos e exportar o elenco. Os arquivos editáveis acompanham a entrega em [art/blender](../art/blender/README.md); os modelos usados pelo Godot ficam em `assets/models/club`.

| Personagem | Papel | Gesto visual |
| --- | --- | --- |
| Nina | Jogável | Aceno curto, cabeça inclinada e braço esquerdo |
| Bento | Jogável | Braço direito em destaque, movimento de anfitrião |
| Seu Corvo | Jogável | Óculos de latão, colete verde, inclinação e gesto de asa |
| Dona Onça | Jogável | Pelagem marcada, roupa vinho, gesto firme de pata |
| Iara | Nova jogável, capivara | Flor, roupa turquesa, gesto aberto |
| Zeca | Novo jogável, raposa | Chapéu azul, floreio lateral |
| Barão da Meia-Noite | Novo boss exclusivo, coruja | Coroa e capa; entrada com gesto mais amplo |
| Dama de Copas | Nova boss exclusiva, serpente | Coroa, presas e capa; inclinação de bote |

São reinterpretações poligonais articuladas, não esculturas realistas nem cópias tridimensionais exatas do spritesheet. Os retratos pixel art originais continuam preservados. Cada GLB tem quatro grupos de malha e cinco clipes exportados: entrada, truco, vitória, entrada de boss e floreio desbloqueável. As amplitudes e a lateralidade variam por personagem.

### Mesa e cartas

- A mesa continua em **2.5D**, com câmera ortográfica, geometria 3D e interface legível.
- Removidas as duas fileiras de três espaços fixos do truco. A mão clicável do jogador continua na interface.
- Cada jogada sai do lugar de quem jogou e termina em uma **pilha física no centro da mesa**, com espessura, altura acumulada e pequenas rotações. Cartas mais fracas de aliados também entram na pilha; não se mostra apenas a melhor carta da equipe.
- As 52 faces têm texturas próprias geradas em código. Os valores e naipes são os mesmos do estado real do jogo. O verso continua nas mãos alheias.
- A pilha permanece durante a mão do truco e é recolhida ao final. Resultado do tombo e da mão identifica a equipe vencedora. O solo usa o mesmo caminho de apresentação.
- No pôquer, as cartas escolhidas e a revelação da mão do chefe chegam à pilha central. A próxima distribuição limpa a apresentação da mão anterior.

### Cutscenes, reações e missão

- Antes de iniciar o truco, participantes entram por lados alternados e ocupam seus lugares, com nome em destaque e gesto de entrada.
- No pôquer, a primeira distribuição de cada rodada apresenta o boss exclusivo. A mesma lógica evita repetir a cutscene a cada descarte.
- Botão **Pular entrada**; redução de movimento elimina a sequência sem mudar turnos ou decisões.
- O pedido de truco toca o clipe do personagem que pediu. O painel de resposta fica na parte inferior, mantendo a mesa e a animação visíveis.
- Primeira missão cosmética funcional: **vencer três partidas com o mesmo personagem** libera seu floreio de truco. Progresso salvo por personagem em `CharacterMissionsV1`; descrição aparece nos ajustes. Vitória significa partida de truco completa ou corrida de pôquer completa, não um tombo.
- Os bosses ficam fora da seleção dos seis jogáveis. A identidade visual e as entradas estão prontas; novos poderes mecânicos de boss permanecem uma etapa de conteúdo/balanceamento.

### Iluminação, resolução e música

- Mantidas as opções de resolução até 4K, modo de janela, escala de renderização e gráficos adicionadas pelo usuário.
- Corrigida a escala lógica: a interface continua desenhada em 1280×720 e escala para a resolução de saída. Selecionar 4K não reduz mais textos e botões a um terço do tamanho.
- Luz principal quente, preenchimento frio, feltro com gradação suave e aro metálico dão profundidade na configuração leve.
- No Forward+, as opções avançadas habilitam sombras, SSAO, reflexos com passos limitados, brilho discreto e MSAA 2×. A mesa usa iluminação ambiente no lugar de um volume de GI dinâmico. A escala 3D do usuário também é aplicada ao SubViewport da mesa.
- Os controles chamados “ray tracing” no código recebido comandam aproximações de rasterização/screen space. **Esta entrega não implementa ray tracing por hardware.** A detecção do renderizador foi corrigida para usar o backend realmente ativo.
- Acrescentadas **Passos de cobre** e **O barão da noite**, além de vinhetas de entrada normal e de boss. Total: cinco loops originais sintetizados; não há vozes gravadas. O gerador de áudio acompanha o projeto.

## Validação

`tools/visual_smoke.ps1` compila, importa recursos, executa regras e captura o renderizador. As verificações incluem presença dos cinco clipes nos oito modelos, exclusividade dos bosses, missão por personagem, todas as cartas na pilha, duplas/trios e pena. Os saves de QA são isolados.

`tools/visual_smoke.ps1 -BenchmarkOnly` usa Forward+/Vulkan em uma mesa sintética com seis personagens e dezoito cartas, mede duração de frames e captura 720p/4K nos modos leve/ultra. Os números dependem do hardware, driver, carga do computador e renderização interna do SubViewport; não representam uma garantia universal de FPS nem medição isolada de tempo de GPU.

Foi corrigido também o serviço de reconexão que consultava o ID de multiplayer sem um peer após sair do lobby. Isso causava erros repetidos no solo e interferia na avaliação de desempenho.

Na medição com **NVIDIA GeForce RTX 3050 / Forward+ Vulkan**, a mesa sintética em 3840×2160 registrou mediana de **6,61 ms/frame no leve** e **7,18 ms/frame no ultra**; percentil 95 de 9,01 e 10,10 ms. Em 720p, as medianas foram 1,41 e 1,80 ms. Dados completos em [benchmark-blender.json](benchmark-blender.json). A medição inclui a saída 4K com a resolução interna configurada para o SubViewport; não equivale a ray tracing ou a um benchmark de renderização 3D nativa integral em 4K.

## Próximas etapas

1. Refinar os modelos com direção artística aprovada: proporções, bicos/focinhos, roupas e dedos; adicionar pernas articuladas para entradas caminhadas e poses de distribuição. Os atuais deslocamentos de entrada são estilizados.
2. Expandir missões e conquistas a partir do progresso por personagem já persistido: objetivos por modo, seleção manual de reação e galeria de desbloqueios.
3. Criar habilidades e regras próprias dos bosses, com telemetria de equilíbrio. A separação visual entre jogáveis e bosses já existe.
4. Completar a sincronização da partida multiplayer e da decisão de pena pelo aliado humano remoto. A infraestrutura de sessão/reconexão recebida foi preservada, mas esta revisão visual não atesta partidas entre computadores.
5. Definir a variante de Fodinha antes de implementar palpites/vazas e integrar o novo módulo à mesa.
6. Expandir os loops em trilhas mais longas e produzir efeitos vocais apenas após definir o tom dos personagens.

## Perguntas para continuar

- Você prefere manter essas figuras poligonais ou aproximar os modelos mais das proporções do Corvo e da Onça do pixel art?
- Quais poderes você imagina para o Barão e a Dama de Copas?
- As próximas missões devem focar em vitórias, blefes aceitos ou combinações específicas de cartas?

## Git e reprodução

Antes desta rodada, as alterações do usuário e seus exports foram preservados em **Backup**, commit `beae4e5`. A revisão visual anterior e as mudanças de resolução/rede estavam em `2697c2e`. Esta continuação é entregue em **review**, sem alterar `main`.

O script Blender recria fontes/exports; o script de cartas recria as 52 texturas; o script de áudio recria os WAVs. Consulte [fontes Blender](../art/blender/README.md) e [origem dos recursos](../assets/art-provenance.md).

O executável local foi reexportado em **debug para revisão**, com o PCK e o runtime .NET local atualizados. Os templates release disponíveis no PC estão incompletos. Para distribuir em outra máquina, exportar novamente ou enviar também `data_GameHub_windows_x86_64`, que é gerada e ignorada pelo Git.
