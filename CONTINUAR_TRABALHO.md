# Prompt de continuidade — MultiGame

## Estado mais recente — Patch 27 validado, 13/09/2026

Priorize este bloco sobre os relatos históricos abaixo. Projeto `C:/workspace/multigame`; branch `review`, base `a1981dd`. Backup preservado no GitHub em `8f73b34`, preservando integralmente o estado recebido; main mantida intacta em `58b85b8`. Remoto `pc-casa` (`https://github.com/Fabiarog/Multigame.git`).

Leia `AGENTS.md`, `docs/MISSAO_MODELOS_3D.md` e `docs/MISSAO_VISUAL_PREMIUM.md`.

### O que foi implementado e validado no Patch 27:
1. **Dona Onça — Reconstrução Completa (Padrão Referência 2):**
   - Eliminação do modelo legado de peças cilíndricas primitivas (Classe C/D).
   - Reconstrução via escultura orgânica estilizada (23.535 vértices, 23.332 polígonos): cabeça felina expressiva com rosetas, orelhas pontiagudas, casaco aveludado bordô sob medida com filigranas douradas, blusa creme, espartilho estruturado, calças ajustadas e cauda longa articulada em 5 seções (`Tail.01..05`).
   - Mãos anatômicas de 5 dígitos com garras douradas retráteis, pontes poligonais entre mãos e coxas completamente eliminadas via `bmesh`.
   - Armature canônico unificado (26 bones) compatível com a hierarquia de Corvo e Bento (`Root`, `Pelvis`, `Spine`, `Chest`, `Neck`, `Head`, `Ear.L/R`, `Shoulder.L/R`, `UpperArm.L/R`, `Forearm.L/R`, `Hand.L/R`, `CardSocket.R`, `Thigh.L/R`, `Shin.L/R`, `Foot.L/R`, `Tail.01..05`).
   - Marcador empty `Head` em `(0, -0.05, 1.55)` para conformidade estrita com a câmera POV em primeira pessoa (`Position.Y > 1.3f`).
   - Skinning com particionamento zonal e suavização laplaciana rápida em memória.
   - 9 Ações NLA com postura sentada nativa embutida nos keyframes esqueléticos (`idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`, `play_card`, `idle_table_01`, `idle_table_02`).
   - Retratos de estúdio 3D em alta resolução: `assets/models/club/onca_3d.png` e `onca.png`.
   - Asset runtime exportado: `assets/models/club/onca.glb` (1 malha isolada, 26 bones, 9 ações). Staging em `art/blender/patch26/onca_refined.glb` e `.blend` master em `art/blender/patch26/onca_refined.blend`.
2. **Correção Definitiva de Deformação dos Chefes (Morgana e Carniçal):**
   - Vértices dos ombros e axilas repesados no Blender MCP.
   - Arestas anômalas com estiramento excessivo na pose medida de vitória: Carniçal = 0, Morgana = 0 (reduzidas de 218 → 74 → 0).
3. **Fidelidade Visual do Classic Club (`TableStage.VisualTarget.cs`):**
   - Mesa de luxo (`club_table_premium.glb`) com borda chanfrada de couro, friso de latão e entalhes de madeira. Cadeiras de veludo (`club_chair_premium.glb`).
   - Cartas físicas chanfradas com 1,8cm de espessura e sombras de contato (`club_card_blank.glb`).
   - Iluminação calibrada e sonda de reflexão ambiente (`ReflectionProbe`) estática com máscara isolando personagens e cartas móveis.
4. **Arquitetura Musical e Trilha Dinâmica (`docs/PLANO_MUSICA.md`):**
   - Especificação de trilhas originais para os 4 salões (Classic Club, Barão Lounge, Dama Salon, Cyber Casino) e leitmotivs para os 4 chefes.
   - Sistema de 4 stems/camadas dinâmicas (Base, Tensão, Truco, Clímax) sincronizadas em C# com crossfades.
   - Faixas master de referência adicionadas em `assets/Musics/`.
5. **Validação de QA (100% Aprovada):**
   - `dotnet build`: 0 erros, 0 avisos.
   - `visual_smoke.ps1 -CameraOnly`: `CAMERA_QA PASS []`.
   - `visual_smoke.ps1`: `GAMEPLAY_QA PASS | 530 assertions passed: solo 2v2, 3v3, full seat turns, deck uniqueness, AI/human Pena ownership`.
   - `visual_smoke.ps1`: `VISUAL_QA_RESULT PASS | screenshots=27 actions=25 layout_issues=0 failures=0`.

### Comandos de Reprodução:
- **Build C#:** `dotnet build`
- **Camera QA:** `powershell -ExecutionPolicy Bypass -File tools/visual_smoke.ps1 -CameraOnly`
- **Gameplay e Visual QA:** `powershell -ExecutionPolicy Bypass -File tools/visual_smoke.ps1`

### Próximos Passos Recomendados:
1. Reconstrução de Nina e Bento (os próximos personagens da Classe D com esqueleto por peças) seguindo a mesma pipeline de sucesso da Dona Onça (escultura estilizada orgânica, rig canônico de 26 bones, 9 ações NLA com postura sentada e retratos de estúdio).
2. Expansão dos cenários do Lounge do Barão e Cassino Cyber com a mesma fidelidade da mesa e iluminação do Classic Club.

---

## Estado mais recente — Patch 26 validado, 12/09/2026

Priorize este bloco sobre os relatos históricos abaixo. Projeto `C:/workspace/multigame`; branch `review`, base `10c4b4d18ece647bd52cc42e3f50cad658ec8f5c`. Backup já publicado em `a4d12eac60cd8a2b340a90aa93bf6614000e3e74`, árvore idêntica à base; main preservada. Remoto `pc-casa`.

Leia `AGENTS.md` e `docs/MISSAO_MODELOS_3D.md`. A missão mais recente pede personagens estilizados, leves e reconhecíveis, com prioridade para animação; não é uma conversão automática para AAA realista. Blender 5.2.1 via MCP em 127.0.0.1:9876 obrigatório para o trabalho 3D. Reiniciado nesta retomada com `tools/start_blender_mcp.py`. Não fechar nem substituir cenas do usuário.

Implementação: os 11 candidatos foram gerados no Blender e integrados nos GLBs do jogo. Repouso de 4 segundos com microanimação de cabeça/peito/partes secundárias, antecipação e atraso por canal, acomodação ao final dos gestos, reações de aproximadamente 2,2 segundos. Contatos de jogar/embaralhar/cortar/distribuir mantêm duração aproximada. Preservados 333 clipes, malhas, texturas e ossos deformadores. Ajuste discreto na altura da cabeça dos três modelos por peças e rugosidade de tecidos sem textura de rugosidade. Oito fontes Blender receberam controles de autoria IK com influência zero: não são IK validado em execução.

A conversão experimental de Nina/Bento/Onça para esqueleto falhou na comparação após exportação (peças giravam/se separavam), foi rejeitada e removida do script de produção. Esses três preservam os pivôs antigos. Não afirmar que houve retopologia, repintura completa ou teste de todas as poses extremas. A missão completa ainda requer trabalho específico nessas áreas.

Correção adicional depois da publicação inicial `da0a00d`: foram identificadas pontas extensas na vitória de Morgana/Carniçal, já presentes nos originais. 236 + 175 vértices distais dos braços tinham pesos rígidos no peito/pélvis. `repair_limb_fallback_weights` interpola pesos dos braços próximos. Carniçal passou de 300 arestas muito esticadas para zero na pose medida; Morgana de 218 para 74, com ombro ainda pendente. Os modelos corrigidos foram integrados e revalidados: regras PASS (531), interface PASS (27 capturas/25 ações), 1.895 amostras de limites e regressão de deformação PASS. Exportação pelo executável principal terminou com código 0 e o pacote iniciou o menu em headless com saída 0. Complemento destinado à mesma review; conferir o hash remoto. O wrapper console travou após gravar o pacote e foi encerrado; a repetição pelo executável principal resolveu a etapa. `tools/inspect_cast26_deformation_mcp.py` aceita VERSION=refined via runpy e verifica o ganho. Comparação visual em `docs/patch26/correcao-pesos-bosses.jpg`. Não reverter essa correção ao continuar.

Fontes editáveis comprimidas e relatórios: `art/blender/patch26`. As cópias locais `_original.glb` e `_refined.glb` são staging ignorado pelo Git. `refine_all_cast26_mcp.py` usa a base Git fixa acima, evitando aplicar novamente o refinamento sobre o resultado. Rodar via `blender_bridge.py execute_code --code-file tools/refine_all_cast26_mcp.py --timeout 300`; depois `tools/validate_cast26.py` e a prévia via `tools/preview_all_cast26_mcp.py --timeout 600`. Nunca executar duas tarefas MCP simultaneamente. Conferir status interno do JSON, além do código de saída.

Evidência já obtida: compatibilidade de 11 personagens/333 clipes; 1.895 amostras de limites de poses exportadas (cinco fases por clipe, originais Corvo/Onça incluídos). Renderizações de idle/truco/victory para todos. Comparações em `docs/patch26`. Regras Godot PASS com 531 verificações na execução final; interface PASS (27 capturas/25 ações, zero falhas/layout), câmera PASS incluindo 4K nos três modos. Windows exportado; pacote iniciou menu em headless com saída 0. Documentação pronta; conferir publicação no remoto review com `git status` e `git ls-remote` antes de reportar. A missão completa de modelagem não está encerrada: continuar por Onça, validando nova estrutura por poses extremas e comparação após exportação, antes de migrar Nina/Bento.

Para QA/exportar, usar SDK .NET 8 portátil em `C:/Users/Lucas/AppData/Local/Temp/multigame-tools/dotnet` em DOTNET_ROOT **e primeiro no PATH**; Godot console em `C:/Users/Lucas/AppData/Local/Temp/multigame-tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`. `tools/visual_smoke.ps1` isola saves. Logs atuais `temp/patch26-*`. Um aviso de objeto retido ao encerrar o teste de regras persiste; não confundir com falha de partida.

## Estado mais recente — 12/09/2026, Patch 25

Trabalhe em `C:/workspace/multigame`. Leia este bloco antes dos relatos históricos abaixo. A base recebida é `1f30bf0` (review), com 11 personagens e quatro salas; não recrie as correções da Aki nem do mascote. Backup no GitHub: `43e7f9a`, árvore idêntica à base recebida, preservando também a história anterior de Backup. Main não deve ser alterada. Remoto: pc-casa, https://github.com/Fabiarog/Multigame.git.

Implementado nesta etapa: tempos de IA independentes de ReduceMotion; fase Scoring protegendo o pôquer; remoção de HandDealt duplicado; geração de distribuição protegendo callbacks antigos da interface; fila de gestos limpa; reação de Fodinha após a carta pousar; proteção contra cliques durante voo; mão sem repetir entrada a cada palpite; projeção de balões corrigida para densidade física; feltro com detalhe atenuado quando subpixel; menos grão, sem FXAA adicional, oclusão de contato mais suave e smoothstep de vinheta corrigido.

Validação concluída: regras PASS (509 asserções na última execução), visual PASS (27 capturas/25 ações), câmera PASS incluindo 4K nos três modos; Vulkan leve/ultra sem erros de shader. Relatórios em `docs/patch25`, resumo em `docs/PATCH25_VALIDACAO.md`, logs locais em `temp/patch25-*`. Executável Windows exportado e menu aberto no teste do pacote. Priorizar DOTNET_ROOT e PATH para o SDK .NET 8 portátil; o SDK global 10 falha na exportação. O teste isolado de regras usa Engine.time_scale=12; isso não entra no jogo nem no teste visual. Não garantir ausência de todos os bugs; há avisos de objetos retidos no encerramento. A câmera elevada teve suas duas posições conflitantes unificadas mantendo enquadramento amplo.

Próximo trabalho: revisar clipes completos e pesos dos 11 personagens no Blender, começando por Corvo/Onça; comparar todas as salas em POV, mesa e 4K com Vulkan; testar duas máquinas para multiplayer; preparar proposta visual de nova arquitetura antes de substituir GLBs. Nesta etapa não foram regenerados esqueletos nem exportados modelos novos. Preservar identidades e fontes existentes.

Plugins pedidos: Build 3D Game Rooms e Game Development Studio. O segundo exige CLI game-dev separada, não encontrada neste ambiente. Não inventar recibos de plugin nem instalar dependências pagas. Para nova composição de sala, seguir os gates explícitos Function/Form/Runtime do SKILL.md; o polimento atual mantém a arquitetura existente. Atualizar este arquivo, PLANO_EVOLUCAO.md e AI_DEV_PATCH_NOTES.md com resultados reais, então publicar apenas os arquivos revisados em review.

> Documento criado primeiro, a pedido de Lucas, em 06/09/2026, antes de iniciar a nova etapa de câmera. Leia este arquivo e `AI_DEV_PATCH_NOTES.md`, confira o estado real do Git e continue o trabalho pendente. Não recomece a repaginada nem descarte mudanças existentes.

## Pedido vigente do usuário

### Atualização de continuidade — Patch 24 Concluído com Sucesso

- **Redesign Panorâmico de Pôquer Balatro:** Removido o letterbox escuro e o fundo opaco com bandeja pesada. O cenário 3D (`TableStage`) opera em tela cheia (`FullRect`). A mão do jogador agora flutua elegantemente na parte inferior da tela. O painel lateral direito ("quadradão pro lado") unifica corrida, pontos, meta, recursos, relíquias ativas, combinação de mão e botões de ação com âncoras calibradas milimetricamente (0 avisos de layout).
- **Postura Sentada Realista:** Pélvis rebaixada para 0.52m (altura da almofada) com pernas flexionadas a 90° (coxas -88°, canelas +85°). Os personagens agora jogam confortavelmente sentados em suas poltronas.
- **Novos Modelos 3D Integrados:**
  - **Mascote Corvo ("Edgar" / "Corvinho") (`mascot_crow.glb`):** Empoleirado como sentinela no topo do relógio de salão do Classic Club com animação idle de observação.
  - **Aki (`aki.glb`, `aki_3d.png`):** Nova personagem jogável (total de jogáveis elevado para 7).
  - **Madame Morgana (`morgana.glb`, `morgana_3d.png`):** Nova boss exclusiva (rodadas 5–6 no Classic Club).
  - **Lorde Carniçal (`carnical.glb`, `carnical_3d.png`):** Novo boss exclusivo (rodadas 7–8 no Cyber Casino).
  - Rodízio de 4 chefes nas 8 rodadas com suas respectivas salas e trilhas temáticas.
- **Upgrade de Nina, Bento e Dona Onça:** Modelos regenerados com pernas sentadas a 90° e retratos de estúdio em alta definição.
- **Coleção Expandida:** Suporte aos 11 personagens com rolagem suave (`ScrollContainer`) e lore conceitual detalhado para cada um.
- **Validação:** `GameplayChecks.cs` (336 asserções PASS), `visual_smoke.ps1` (27 screenshots, 0 layout issues PASS), `visual_smoke.ps1 -CameraOnly` (PASS), executável Windows `Game Hub.exe` (103 MB) exportado.

### Atualização de continuidade — Patch 23 Concluído com Sucesso

- **Direção Cinematográfica & Fim da Tela Preta:** Corrigido o vetor de interpolação da cutscene de entrada em `TableStage.cs`, onde a descida afundava a lente para `Y = -0.30m` no interior da mesa de madeira sólida (`Y = 0.08m` topo, `-0.15m` base), gerando tela preta. Todas as câmeras foram fixadas em `Y >= 1.58m`, garantindo visão limpa.
- **Close-Up e Pôquer com Chefe:** No Pôquer (`boss == true`), adicionado plano geral do salão (`Y = 2.75m`), corte direto para close-up fechado no rosto e olhos do chefe (`Fov = 28.0f`, distância 1.12m), disparo de animação temática do boss (`boss_intro`/`flourish`), música misteriosa (`midnight-baron`) e som de chegada (`boss-arrival`), seguido de varredura superior suave até o POV do jogador (`Y = 1.63m`).
- **Tomada do Canto do Salão e Personagens Sentando (Truco & Fodinha):** Em mesas sem chefe (`boss == false`), o primeiro plano agora posiciona a câmera estrategicamente no canto alto do salão (`Vector3(-4.9f, 3.45f, 4.6f)`, `Fov = 52.0f`) enquadrando o salão aristocrático completo (lareira com iluminação trêmula, relógio de pêndulo, carrinho de bar clássico e poltronas); todos os personagens sentam simultaneamente em suas poltronas (`PlayGesture(i, "entrance")`), com transição orbital panorâmica elevada antes do POV.
- **Recolhimento Realista de Cartas e Encaixe Físico do Baralho:** Eliminados todos os `Label3D` flutuantes das cartas descartadas (`lbl.QueueFree()`). As cartas jogadas da rodada são unidas viradas para baixo no centro; o maço do baralho se eleva no ar (`liftHeight = 0.16m + n * 0.018m`); as cartas da mesa deslizam diretamente para baixo do baralho; o maço suspenso desce com efeito elástico amortecido e som táctil de corte (`"cut"`), formando um bloco perfeitamente alinhado e homogêneo com a capa dourada no topo.
- **QA e Automação 100% Aprovados:**
  - `gameplay_smoke.gd`: **287 asserções aprovadas** (`GAMEPLAY_QA PASS`).
  - `visual_smoke.ps1`: **27 screenshots geradas**, 25 ações, 0 falhas e 0 avisos de layout (`VISUAL_QA PASS`).
  - `visual_smoke.ps1 -CameraOnly`: **100% aprovado** sem desvios (`CAMERA_QA PASS []`).
  - Executáveis `Game Hub.exe` e `Game Hub.pck` re-exportados e prontos para jogar.

### Atualização de continuidade — Patch 22 Concluído com Sucesso

- **Bugs de Animação e POV:** Corrigida a lógica de visibilidade no POV (`!isLocal`), eliminando qualquer torso/cabeça clipando na câmera do jogador local. Em modo Mesa (Overhead), todos os ocupantes são visíveis. A carta jogada agora nasce à frente da lente em POV, e animações esqueléticas suprimem o solavanco rígido em `PlayTableAction`.
- **Ritmo do Truco:** Identificado que `settings.cfg` estava com `ReduceMotion=true` (acessibilidade extrema que zerava os tempos de espera), corrigido para `false`. No `TrucoGameManager`, foi inserido o token `_handId` para prevenir sobreposição assíncrona entre mãos, além de pausas dramáticas de suspense no grito e resposta de Truco.
- **Cenário Classic Club HD:** Integrado o carrinho de bar vintage (`club_bar_cart.glb`) e iluminação dinâmica com cintilação suave na lareira (`_fireplaceLight`).
- **QA e Automação 100% Aprovados:**
  - `gameplay_smoke.gd`: **287 asserções aprovadas** (`GAMEPLAY_QA PASS`).
  - `visual_smoke.ps1`: **27 screenshots geradas**, 25 ações, 0 falhas e 0 avisos de layout.
  - `visual_smoke.ps1 -CameraOnly`: **100% aprovado** (`CAMERA_QA PASS []`).
  - Executáveis `Game Hub.exe` e `Game Hub.pck` re-exportados e prontos para jogar.

- **Status Atual — ETAPAS CONCLUÍDAS E VALIDADAS (Patch 10, 11 & 12):**
  - **Mãos Articuladas em 5 Falanges e Acessórios Exclusivos (Patch 12):** Cada um dos 8 personagens (`nina`, `bento`, `corvo`, `onca`, `iara`, `zeca`, `barao`, `dama`) possui mãos com 5 dígitos individuais e falanges anatômicas (`ThumbProximal/Distal`, `IndexProximal/Distal`, `MiddleProximal/Distal`, `RingProximal/Distal`, `PinkyProximal/Distal`), além de garras em queratina e esporão no Seu Corvo. Cada personagem recebeu acabamento exclusivo nos pulsos e mãos (luvas sem dedos com rebites na Nina, relógio clássico e anel no Bento, anel de sinete de rubi no Corvo, garras douradas na Onça, bracelete de lótus na Iara, luvas de croupier peroladas no Zeca, anel de safira no Barão e bracelete de serpente na Dama).
  - **Carta 3D Física na Mão:** Carta com espessura e acabamento fino mantida entre o polegar e o indicador na mão ativa durante a animação `play_card`. O jogador e adversários transportam fisicamente a carta até o feltro no frame 26.
  - **Imersão Táctil POV com Micro-Recuo:** FOV do POV calibrado em 54° para enquadrar mangas e mãos confortavelmente na borda de couro da mesa. Ao bater a carta no feltro em primeira pessoa, um micro-recuo táctil suave (`_tactileRecoilY` com transição `Back.Out`) é aplicado à câmera, transmitindo sensação física e firmeza de impacto.
  - **Separação Anatômica Modular em 9 Partes (Blender 5.2):** 9 nós de malha independentes (`PelvisMesh`, `BodyMesh`, `HeadMesh`, `ArmLMesh`, `ArmRMesh`, `ForearmLMesh`, `ForearmRMesh`, `HandLMesh`, `HandRMesh`). Torso respira e se inclina sem distorcer pernas sentadas.
  - **7 Animações Bezier por Modelo:** `idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`, `play_card`.
  - **Corte, Rodízio do Baralho e Câmera POV:** 100% automatizados com IA, rodízio e limites de pescoço (±48° yaw, ±12° pitch).
  - **Validação de Testes Automatizados:** 155 asserções em `GameplayChecks.cs` aprovadas; suíte de câmera (`camera_smoke.gd`) 100% aprovada (`CAMERA_QA PASS []`); 24 capturas de tela e 21 ações aprovadas com 0 falhas em `visual_smoke.ps1`.
  - **Executável Exportado:** `Game Hub.exe` e `Game Hub.pck` atualizados para Windows Desktop x86_64.

Você está continuando a implementação do jogo **MultiGame**, um clube de cartas roguelike em Godot .NET/C#, com pôquer, truco e agora Fodinha. Responda em português brasileiro. O usuário quer execução prática, com poucas interrupções e sem pedidos repetidos de confirmação.

Últimos pedidos, todos cumulativos:

1. Corrigir o rodízio do baralho. As IAs devem embaralhar, cortar, distribuir e jogar automaticamente nas próprias vezes. O humano deve cortar **somente quando for sua vez**. Distribuidor e cortador avançam junto com a ordem dos lugares, sem exigir que o usuário comande os bots. Preservar a decisão de pena pela IA aliada ou pelo jogador local quando ele for o destinatário.
2. Consertar animações já adicionadas que apresentem problemas.
3. Melhorar o POV: a visão em primeira pessoa está baixa; levantar a câmera e permitir olhar para os lados com limite semelhante ao movimento de um pescoço. **Não permitir giro de 360 graus.** Adicionar movimento/animação que acompanhe suavemente a câmera.
4. Criar primeiro este prompt de continuidade, completo, para outro agente continuar de onde parou. Manter o documento atualizado com o estado final ou pontos realmente pendentes.

Direção de arte persistente: preservar a essência dos personagens, especialmente Corvo e Onça, e o estilo 2.5D; aumentar a qualidade da geometria, materiais, sombras e animações. Não substituir a identidade do elenco nem alegar acabamento AAA/fotorrealista se o resultado não justificar. Bosses exclusivos: Barão da Meia-Noite e Dama de Copas. Seis personagens jogáveis: Nina, Bento, Corvo, Onça, Iara e Zeca.

## Local correto e fluxo de Git autorizado

- Projeto: **`C:\workspace\multigame`**. O diretório padrão da conversa pode ser `C:\Users\Lucas\Documents\ChatGPT\Multi Game`; ele NÃO é a pasta de trabalho do jogo. Passe o diretório correto explicitamente aos comandos.
- Shell: PowerShell no Windows. Acesso local liberado; não usar parâmetro `sandbox_permissions`.
- Remote: **`pc-casa`**, URL `https://github.com/Fabiarog/Multigame.git`.
- Branch de implementação: **`review`**.
- **`main` deve continuar intacta**, último hash verificado `58b85b804c79dcc02da9882276d993247356577e`.
- O usuário autorizou preservar o estado anterior em **`Backup`** antes de editar e publicar as alterações em **`review`**. Não pedir autorização de novo para esse fluxo.
- Na abertura da etapa de corte/câmera, `Backup` foi atualizada localmente e no GitHub para **`fd67728d4517e793b9f060209f5ecc16aed31422`**.
- `review` publicada antes desta etapa também aponta para `fd67728` — commit `feat: render tables at native pixel density and add Fodinha solo`.
- Há modificações desta etapa ainda NÃO commitadas; confira `git status`, `git diff` e este documento antes de agir. Não publicar uma implementação incompleta só porque a conversa foi compactada.
- Não criar subagentes: a instrução vigente só permite delegação se o usuário ou instruções locais a solicitarem expressamente.

### Arquivos de experimentos do usuário: preservar

Estes arquivos já estavam não rastreados antes do trabalho. Não apagar, sobrescrever nem incluir automaticamente em commits:

```
scratch_aaa_test.py
test_export_no_apply.glb
test_export_no_apply.glb.import
test_head.glb
test_head.glb.import
tools/inspect_glb.gd
tools/inspect_glb.gd.uid
tools/inspect_materials.gd
tools/inspect_materials.gd.uid
tools/test_anim_export.py
tools/test_head_export.py
```

Os presets de exportação excluem `tools/*` e `test_*.glb`, para não distribuir ferramentas/experimentos dentro do pacote. Não usar `git add .` indiscriminadamente.

## Estado entregue antes desta etapa — Patch 9

O registro cumulativo obrigatório é **`AI_DEV_PATCH_NOTES.md`**. O usuário pediu explicitamente que cada entrega seja acrescentada nesse mesmo arquivo. Patches anteriores são histórico; não sobrescrever suas descrições como se fossem promessas vigentes.

Patch 9 (`fd67728`) já foi implementado, testado, exportado e enviado ao GitHub:

- Corrigida a pixelização da mesa em 4K. `core/visuals/TableStage.cs` passou de `SubViewportContainer` para `Control`, mostrando um `SubViewport` por `TextureRect` com filtro linear. O tamanho interno acompanha os pixels físicos da área da mesa, usando a transformação final do viewport e a transformação do canvas. A interface mantém a base lógica 1280×720.
- No pôquer em janela 3840×2160, a mesa renderiza em 2844×735; no truco em 2172×1125; em Fodinha em 3006×1263. A mesa ocupa só uma parte da tela: não confundir esses tamanhos com uma janela menor. Escala de render 50–100% segue aplicada pelo Godot.
- Sombras direcionais 2048/4096, MSAA 2×/4×, FXAA removido da mesa; ajustes de bias para evitar artefatos triangulares de autosombra. Efeitos são rasterização/SSAO/SSR, **não ray tracing por hardware**. `ReduceMotion` e VFX continuam respeitados.
- Oito personagens receberam geometria adicional nos detalhes dos rostos, preservando quatro grupos articulados e cinco clipes: `entrance`, `truco`, `victory`, `boss_intro`, `flourish`.
- Quatro salas e poltrona receberam chanfros e normais corrigidas no Blender. Mesa procedural passou de 48 para 192 segmentos; fichas continuam com 48.
- Fontes atuais de arte: `art/blender/*_fidelity.blend`; anteriores `_mcp.blend` preservados. Cenários originais da etapa guardados em `art/blender/fidelity-inputs`. Manifesto: `assets/models/club/fidelity-pass.json`. Script: `tools/refine_fidelity_mcp.py`.
- Fodinha implementada em `games/fodinha`: solo com três IAs, cinco vidas, perda `abs(palpite - vitórias)` — regra escolhida explicitamente pelo usuário. Mãos 1,2,3,4,5,4,3,2,1. Baralho de 40 cartas, vira/manilha do truco, sem obrigação de seguir naipe; empate comum favorece primeira carta jogada. Palpites livres. Zero vidas elimina. Vencedor final por sobrevivência ou mais vidas, com empate compartilhado. Essas escolhas estão expostas em “Como jogar”.
- **Rede de partida ainda não está implementada.** Lobby LAN é estrutura anterior. Fodinha desabilita LAN e só oferece solo. Não alegar multiplayer funcional.
- QA anterior: 78 asserções (incluindo 100 partidas de Fodinha), 24 telas e 23 ações sem problemas de layout. Arquivos: `docs/qa-fodinha.json`, `docs/resolution-fidelity.json`, `docs/benchmark-fidelity.json`, `docs/screenshots-fodinha`, `docs/screenshots-fidelity`.
- Benchmark anterior sintético, seis personagens e 18 cartas: 4K básico mediana 7,305 ms/P95 7,734 ms; detalhado 11,153/11,516 ms na RTX 3050. Não garantir esses números após alterar câmera/enquadramento nem para outras máquinas.

## Alterações de corte em andamento — ainda precisam concluir a validação

### `games/truco/scripts/TrucoGameManager.cs`

Foi identificado que a IA já tinha um temporizador de corte, mas esperava **10 segundos** e a API/UI deixavam o humano cortar na vez dela.

Alterações feitas nesta etapa:

- Nova fase `TrucoPhase.Shuffling`, acrescentada ao final do enum para preservar os valores anteriores.
- `StartNewHand` embaralha e entra nessa fase. Após 0,85 s (0,05 s com movimento reduzido), passa a `Cutting`.
- IA corta 0,65 s após o embaralhamento (0,15 s com movimento reduzido).
- `CutDeck()` é agora a solicitação local: só age quando `CutterIsPlayer`. A ação real fica em `CutDeckForSeat`, usada pela IA com validação de lugar e fase.
- `CanOfferPena` e `CanResolvePena` distinguem quem pode oferecer/decidir. `GivePena()` e `ResolvePena()` validam o pedido local. Métodos privados `DeliverPena` e `ResolvePenaInternal` executam decisões da IA.
- `DealAfterCut` agora emite explicitamente a fase `Dealing`.
- Rotação existente: `DealerSeatIndex` avança +1 ao terminar uma mão; cortador é o lugar anterior ao distribuidor. `StartMatch` começa no distribuidor 0. Não girar o distribuidor duas vezes na mesma mão.

### `games/truco/scripts/TrucoUI.cs`

- Botão de cortar aparece/habilita só em `Cutting` e só para o lugar local.
- Textos identificam quem embaralha, corta e distribui.
- Rodapé do placar: `DISTRIBUI: nome` / `CORTA: nome`; verificar se nomes longos cabem em 720p.
- Leques esvaziados antes da distribuição.
- Oferta de pena só expõe botões quando `CanOfferPena`; decisão recebida só quando o destinatário é local.
- Animação de embaralhar não reabilita mais o botão de corte indiscriminadamente; fase do motor controla isso.
- Botão de truco oculto também durante `Shuffling`.

### Fodinha: corte real e rodízio recém-adicionados

`games/fodinha/scripts/FodinhaMatch.cs`:

- Nova fase `Cutting`. `PrepareRound` embaralha o baralho e deixa mãos vazias e vira fechada.
- Propriedades `DealerSeat`, `CutterSeat`. Cortador é o participante vivo anterior ao distribuidor.
- `Cut(int seat)` rejeita lugar/fase errados, rotaciona realmente o baralho e só então distribui cartas em ordem dos lugares e revela vira.
- Primeiro palpite/jogada da mão começa no participante vivo seguinte ao distribuidor. Isso altera a abertura anterior do Patch 9; documentar no próximo patch.
- `AdvanceRound` passa o distribuidor ao próximo participante vivo, ignorando eliminados.
- Atenção: `Vira` agora pode ser `null` antes do corte. Não consultar `Manilha` nesse período.

`games/fodinha/scripts/FodinhaUI.cs`:

- Campos `_cutReady` e `_dealing`, métodos `PrepareDeck` e `Cut`.
- A entrada dos personagens termina antes de embaralhar. Preparação dura 0,85 s e a IA espera mais 0,65 s para cortar.
- Corte local somente na própria vez. Após cortar, há 0,6 s de apresentação e as cartas são mostradas; as IAs retomam palpites/jogadas automaticamente.
- Próxima mão chama `PrepareDeck`. Mostra distribuidor/cortador e “Vira fechada” antes do corte.
- `TableStage.AnimateDeck(bool cutting)` move cartas da pilha física para simular separação/união. A pilha está agora agrupada sob `_deckPile` / `DeckPile`, preservando as coordenadas anteriores. Respeita redução de movimento.

### Testes desta etapa

- `tools/GameplayChecks.cs`: simulações de Fodinha agora cortam somente com o participante correto, testam mãos vazias antes do corte, duplo corte rejeitado, rotação e exclusão dos eliminados. Novo helper `PrepareTrucoHand` espera IA de verdade, tenta uma ação local indevida, espera a vez humana sem autoavanço e respeita a decisão local de pena.
- `tools/visual_smoke.gd`: não clica mais em “Cortar” na vez da IA. Espera corte/pena/distribuição automáticos e verifica que a mão local recebeu três cartas. Espera maior para a primeira oferta de palpite de Fodinha.
- **Execução iniciada, resultado ainda não lido na criação deste documento:** `tools/visual_smoke.ps1 -OutputDirectory docs/screenshots-cut-rotation`. Log externo: `%TEMP%\multigame-cut-qa.log`. Sessão de terminal da conversa: `27283` (esse ID pode não existir em outra conversa; nesse caso ler log e verificar processos).
- Não assumir que passou. Ler erros e resultados; corrigir falhas reais e atualizar os testes se estiverem esperando o comportamento antigo. Não enfraquecer verificações para obter PASS.
- A primeira avaliação visual deve conferir o placar do truco e a informação de distribuidor/cortador em Fodinha em 720p.

## Próxima etapa: câmera/POV e animações

**Ainda NÃO implementada na criação deste prompt.** Antes de editar, procurar se existem alterações novas do usuário, especialmente câmera em primeira pessoa, scripts POV ou cenas novas.

Base conhecida: `TableStage` cria uma câmera ortográfica em `(0, 6.2, 11)`, olhando para `(0, .7, 0)`, e `_Process` ajusta o enquadramento conforme a proporção do painel. Há um movimento ambiente pequeno, desligado por `ReduceMotion`. Essa implementação conhecida é a vista 2.5D da mesa; não inventar que já existe um modo POV se a busca não encontrar.

Plano para cumprir o pedido:

1. Inspecionar câmera atual, controles, configurações, poses e animações existentes. Preservar a vista 2.5D do usuário.
2. Elevar o ponto de visão em primeira pessoa, mantendo cartas, mesa e adversários legíveis e evitando atravessar o próprio personagem/encosto.
3. Limitar yaw/pitch, como um pescoço; suavizar aceleração e retorno/transição. Nada de rotação infinita ou captura inesperada do mouse que impeça clicar nas cartas.
4. Integrar o gesto/animação que acompanha o olhar. Evitar movimentos bruscos, clipping de mãos/cartas e disputa entre animação e controlador da câmera. Movimento reduzido deve interromper balanços automáticos e manter controles utilizáveis.
5. Conferir se projecções `SeatScreenPosition`, `DeckScreenPosition`, animações de distribuição e overlays continuam alinhados após a mudança de câmera. Esses métodos usam `Camera3D.UnprojectPosition` convertido para coordenadas lógicas da mesa.
6. Executar capturas reais em vista de mesa/POV, olhar à esquerda/direita e centro; testar limites e retorno, abrir/fechar jogo e mudança de resolução. Não usar apenas compilação como prova visual.
7. Documentar exatamente o controle oferecido ao jogador e suas limitações no patch e no README. Se um novo botão/modo for necessário, seguir o tema do clube e manter acessibilidade.

## Ferramentas e comandos locais

Python 3.12:
`C:\Users\Lucas\AppData\Local\Programs\Python\Python312\python.exe`

.NET 8.0.424:
`C:\Users\Lucas\AppData\Local\Temp\multigame-tools\dotnet\dotnet.exe`

Godot 4.7.2 .NET console:
`C:\Users\Lucas\AppData\Local\Temp\multigame-tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`

Blender 5.2.1:
`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`

Blender MCP local: `127.0.0.1:9876`, addon instalado. `tools/blender_bridge.py get_scene_info` consulta a cena e `execute_code --code-file caminho.py --timeout 1200` executa Python pelo addon. Não precisa serviço externo. **Não rodar o gerador antigo `build_club_rooms.py` via MCP ao vivo: ele chama `read_factory_settings` e apagaria a cena aberta.** Usar cenas isoladas e restaurar a original, como nos scripts recentes.

Compilar/testar em PowerShell, no projeto:

```powershell
$env:DOTNET_ROOT = 'C:\Users\Lucas\AppData\Local\Temp\multigame-tools\dotnet'
$env:GODOT_BIN = 'C:\Users\Lucas\AppData\Local\Temp\multigame-tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
& tools/visual_smoke.ps1 -OutputDirectory docs/screenshots-cut-rotation
```

O launcher isola `APPDATA` para não alterar saves/configurações do usuário; faz build, import, gameplay headless e captura OpenGL real fora da tela. Timeout individual de 60 s. Se testes novos exigirem mais, dividir etapas ou ajustar prazo com justificativa; não esconder erros. Os logs completos e relatórios ficam em `%TEMP%\multigame-visual-qa-*`.

Exportar após a validação:

```powershell
& $env:GODOT_BIN --headless --path . --export-debug 'Windows Desktop' 'Game Hub.exe'
```

- Templates **release estão incompletos**. O pacote local de revisão é **debug**. Não prometer release.
- Arquivos de execução: `Game Hub.exe`, `Game Hub.pck`, `Game Hub.console.exe` e `data_GameHub_windows_x86_64`. O runtime é gerado e ignorado no Git; necessário ao transferir o pacote para outro PC.
- Verificação rápida do pacote: `Game Hub.console.exe --headless --audio-driver Dummy --quit-after 120`, com APPDATA temporário. O wrapper `.console.exe` permite esperar o encerramento e ler a saída.
- Não usar `--main-pack` com o executável exportado: o template foi compilado sem suporte a sobrescrever caminhos e recusa esse argumento. Isso é limitação do template, não uma falha de Fodinha.
- Avisos anteriores de 1–3 instâncias ObjectDB no encerramento já foram registrados. Diferenciar esses avisos de novos erros de recursos/exceções. Não gastar indefinidamente tentando corrigir vazamento genérico antigo fora do escopo.

## Critérios de término e entrega

1. Corte/distribuição dos bots automáticos, humano só age na sua vez; pena continua correta; rodízio testado em truco 1v1/2v2/3v3 e Fodinha.
2. POV elevado, limites de olhar funcionando, movimento suave e animação relacionada conferidos visualmente. Vista 2.5D preservada e controles documentados.
3. Compilação e testes pertinentes aprovados; capturas revisadas de verdade. Atualizar relatórios rastreados e informar limitações reais.
4. Acrescentar **Patch 10** (ou o próximo número realmente disponível) a `AI_DEV_PATCH_NOTES.md`, com tudo que foi alterado nesta etapa, testes e pendências. Atualizar este documento para que não deixe tarefas concluídas como se estivessem em aberto.
5. Reexportar pacote debug e verificar abertura. Fazer commit apenas dos arquivos autorizados da implementação e docs, publicar em `review`, conferir hashes remotos de `Backup`/`review`/`main`.
6. Final ao usuário conciso, em português, com resultado, controles da câmera e links absolutos para executável, patch e este prompt. Não afirmar que a parte multiplayer está pronta.

## Como retomar imediatamente

Leia o log de QA iniciado, confira `git diff` para confirmar as alterações descritas e corrija qualquer falha. Em seguida faça a busca pelo POV/câmera existente e implemente o pedido novo sem perder a correção do baralho. Se a sessão anterior tiver avançado, trate o Git e os resultados novos como fonte de verdade. Atualize a seção de andamento deste prompt ao concluir cada entrega material.
