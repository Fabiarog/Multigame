# MULTIGAME — LOG DE ATUALIZAÇÕES, ARQUITETURA E GUIA DE DESENVOLVIMENTO

## Patch 25 — 12/09/2026 — Ritmo, transições e polimento de imagem

Base recebida `1f30bf0`; preservada no GitHub em Backup `43e7f9a` antes das alterações. Trabalho destinado a review; main preservada.

- **Ritmo:** ReduceMotion deixa de encurtar a decisão das IAs no Truco/Fodinha e o tempo de leitura da pontuação do pôquer. Textos de acessibilidade atualizados.
- **Pôquer:** fase Scoring bloqueia ações concorrentes; removido HandDealt duplicado; entrega da interface usa geração para ignorar callbacks antigos e manter os botões bloqueados até pousar.
- **Fodinha:** animação da mão só se repete quando as cartas mudam; cliques bloqueados durante voo/coleta; vitória da vaza aguarda o pouso da última carta.
- **Gestos e câmera:** limpar fila antes de trocar clipes; projeção de balões em coordenadas lógicas; mesma posição ampla durante alternância e repouso da câmera, eliminando salto; tecla C protegida contra consumo por controles focados.
- **Gráficos/cenário existente:** menos grão e relevo do feltro, atenuação de detalhes subpixel, MSAA sem FXAA adicional, sombras de contato mais discretas e correção do smoothstep da vinheta. Arquitetura, identidades e pesos dos modelos preservados; nenhuma reconstrução de esqueleto foi feita neste patch.
- **Validação:** compilação sem erros/avisos; regras PASS (509 asserções na última execução), interface PASS (27 capturas, 25 ações, zero falhas/layout), câmera PASS nos três modos incluindo 4K; Vulkan leve/ultra em 720p/4K sem erros de shader. Persistem avisos de objetos retidos ao encerrar o QA. Rede entre computadores não testada.
- **Ferramentas:** orientações dos plugins consultadas; CLI game-dev ausente, validação executada com as ferramentas Godot do projeto. Nenhum pacote pago ou novo modelo gerado. Fontes recentes de Aki/mascote mantidas.

Evidências e limites: [PATCH25_VALIDACAO.md](docs/PATCH25_VALIDACAO.md). Próximas etapas/perguntas em PLANO_EVOLUCAO.md e prompt atualizado em CONTINUAR_TRABALHO.md.

---

> **Público-alvo:** Agentes de Inteligência Artificial (Antigravity/Gemini/Claude/GPT) e Engenheiros de Software que forem trabalhar neste repositório.
> **Última Atualização:** Setembro/2026

---

## 1. Visão Geral do Projeto e Stack Técnico

MultiGame (conhecido internamente como `GameHub`) é um ecossistema de jogos de cartas clássicos e contemporâneos (Truco Paulista/Mineiro, Pôquer Roguelike estilo corrida de metas, e expansões planejadas como Fodinha), desenvolvido com uma estética de clube aristocrático vintage brasileiro (veludo, papel creme, feltro verde escuro e detalhes em latão e ouro).

### Ferramentas e Versões Oficiais do Ambiente:
* **Engine:** Godot Engine v4.7.2 Mono/C# (`Godot_v4.7.2-stable_mono_win64_console.exe`).
* **SDK / Linguagem:** .NET 8.0 SDK (C# 12, target `net8.0`).
* **Modelagem 3D & Render:** Blender 5.2.1 LTS (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`).
* **Protocolo MCP (Model Context Protocol):** `blender-mcp` v1.9.1 (`C:\Users\Lucas\.local\bin\blender-mcp.exe`), porta local `9876`.
* **Processamento de Imagens / Sprites:** Python 3.12 com `Pillow` (PIL 12.3.0).
* **Gerenciador de Pacotes Python:** `uv` / `uvx` (`C:\Users\Lucas\.local\bin\uv.exe`).
* **Shell de Automação:** PowerShell 5.1/7.

---

## 2. Changelog Cumulativo de Patches Implementados

### Patch 23: Direção Cinematográfica de Câmeras, Close-Up no Chefe, Tomada do Canto do Salão e Baralho Físico — 07/09/2026

1. **Revisão Cinematográfica de Câmeras e Cutscene (`core/visuals/TableStage.cs`):**
   * **Eliminação de Penetração na Geometria da Mesa (Fim da Tela Preta):** Corrigido o cálculo de altura da câmera durante as transições de corte e entrada. Anteriormente, na interpolação do Shot 2 para o Shot 3, o vetor de descida afundava a lente para `Y = -0.30m` (abaixo da superfície do feltro que fica em `Y = 0.08m`), penetrando na malha de madeira sólida da mesa e bloqueando a visão com tela preta. Todas as trajetórias de câmera agora impõem estritamente `Y >= 1.58m`, garantindo visão limpa sem atravessar a mesa.
   * **Cinemática de Chefe no Pôquer (`boss == true`):**
     * Disparo da trilha sonora temática dramática (`midnight-baron`) e efeito sonoro de chegada (`boss-arrival`).
     * Tomada inicial do salão em plano alto (`Y = 2.75m`).
     * Corte direto para close-up fechado e detalhado na face do chefe (`Fov = 28.0f`, câmera a 1.12m dos olhos, `Y = 1.62m`), com execução da animação temática do boss (`boss_intro` ou `flourish`).
     * Varredura aérea cinematográfica sobre a mesa (`Y = 2.45m`) descendo com elegância até o ponto de vista em primeira pessoa (`POV`, `Y = 1.63m`).
   * **Cinemática de Canto de Sala e Personagens Sentando no Truco e Fodinha (`boss == false`):**
     * Shot 1 posicionado estrategicamente no canto superior do salão (`Vector3(-4.9f, 3.45f, 4.6f)`, `Fov = 52.0f`), enquadrando a sala completa do Classic Club (lareira cintilante, relógio de pêndulo, carrinho de bar vintage, poltronas estofadas e lustre).
     * Os personagens entram em cena e sentam em suas respectivas poltronas simultaneamente (`PlayGesture(i, "entrance")`).
     * Shot 2 executa um arco orbital elevado sobre o perímetro da mesa (`Y = 2.40m -> 2.25m`).
     * Shot 3 desce suavemente pelo topo do feltro até a perspectiva em primeira pessoa do jogador local.

2. **Recolhimento Realista de Cartas e Encaixe Perfeito do Baralho (`core/visuals/TableStage.cs`):**
   * **Eliminação de Rótulos Flutuantes:** Todos os nós `Label3D` associados às cartas jogadas são imediatamente ocultados e descartados (`QueueFree()`), eliminando o artefato de textos flutuantes como `☆ Adversário 2 ☆ MANILHA` sobre o baralho.
   * **Elevação do Baralho e Inserção por Baixo:** Conforme o desenho e referência de corte solicitados, ao recolher a rodada, as cartas jogadas são unidas com a face para baixo no centro da mesa. O maço principal do baralho se ergue verticalmente (`liftHeight = 0.16m + n * 0.018m`), enquanto as cartas recolhidas deslizam para baixo dele até a base do feltro (`Y = 0.082m`).
   * **Fechamento e Alinhamento do Montinho:** O maço suspenso desce e repousa sobre as novas cartas com amortecimento elástico (`Back.Out`), produzindo o efeito sonoro de corte (`"cut"`) e formando um bloco compacto, alinhado e esteticamente correto com o verso dourado no topo.

3. **Validação de QA e Build:**
   * **Gameplay QA (`tools/gameplay_smoke.gd`):** 287 asserções aprovadas com êxito (`GAMEPLAY_QA PASS`).
   * **Visual QA (`tools/visual_smoke.ps1`):** 27 capturas de tela e 25 ações interativas aprovadas com 0 falhas e 0 avisos de layout (`layout_issues=0, failures=0`).
   * **Camera QA (`tools/visual_smoke.ps1 -CameraOnly`):** Suíte de câmera 100% aprovada (`CAMERA_QA PASS []`).
   * **Exportação Desktop:** Executável `Game Hub.exe` e arquivo de recursos `Game Hub.pck` atualizados para Windows Desktop x86_64.

---

### Patch 22: Correção Definitiva de Animações, Ritmo Natural do Truco e Cenário Classic Club HD — 07/09/2026

1. **Correção dos Bugs de Animação e Câmera POV (`core/visuals/TableStage.cs`):**
   * **Ocultação do Jogador Local em POV:** Inversão da lógica de visibilidade corrigida em `UpdateCameraPosition`. Em primeira pessoa (POV), a malha do personagem local (`_actors[seat]`), sua cadeira (`_chairs[seat]`) e o leque 3D estático (`_hands[seat]`) são ocultados (`!isLocal`), impedindo que a respiração e os gestos do modelo atravessem a câmera do jogador. No modo Mesa (Overhead), todos os participantes e mobiliário voltam a ser totalmente visíveis.
   * **Ponto de Origem da Carta em POV:** No método `PlayCard`, quando a jogada é realizada pelo jogador local em POV, a posição inicial da carta é computada à frente do cone de visão da câmera (`_camera.GlobalPosition + Down * 0.28f + Forward * 0.48f`), eliminando o artefato de cartas surgindo por trás da cabeça e atravessando o crânio do avatar.
   * **Harmonização de Animação Esquelética e Rígida:** `PlayTableAction` agora inspeciona se o ator possui um `AnimationPlayer` com clipe ativo. Caso haja animação esquelética em curso, o solavanco da raiz é suprimido; caso contrário, o impulso é aplicado no eixo local frontal do ator em vez de inclinar no eixo X global da sala.
   * **Padronização do Botão de Pular Entrada:** O botão da cutscene cinematográfica foi nomeado como `SkipButton` e definido com texto `"Pular entrada"`, garantindo conformidade com a automação de QA e com o atalho `ESC`.

2. **Calibração do Ritmo e Velocidade do Truco (`games/truco/scripts/TrucoGameManager.cs`, `user://settings.cfg`):**
   * **Causa Raiz Resolvida:** O arquivo de configuração do usuário (`settings.cfg`) continha `ReduceMotion=true`, o que ativava o modo de acessibilidade extrema em que `WaitForAnimation` retornava imediatamente (`0.0s`), fazendo turnos, cartas e rodadas voarem instantaneamente. O arquivo foi ajustado para `ReduceMotion=false`.
   * **Prevenção de Condições de Corrida Assíncronas (`_handId`):** Implementado contador incremental `_handId` no ciclo de vida de cada mão. Todas as rotinas assíncronas com espera (`CutDeckForSeat`, `DeliverPena`, `ResolvePenaInternal`, `DealAfterCut`, `PlaySeatCard`, `ResolveCurrentRound`, `RespondToTruco`, `AIRespondToTruco` e `FinishHandSequence`) verificam `_handId == thisHand` após cada `await`, impedindo que callbacks atrasados sobrescrevam o estado de mãos subsequentes.
   * **Tensão e Suspense nas Chamadas de Truco:** Adicionada pausa dramática de reflexão e reação em `RespondToTruco` (`0.65s`) e `AIRespondToTruco` (`0.85s`), com avisos nítidos em tela ("TRUCO ACEITO!", "TRUCO RECUSADO!").

3. **Cenário Classic Club Aprimorado e Polimento Visual:**
   * **Carrinho de Bar de Luxo (`res://assets/models/club/club_bar_cart.glb`):** Instanciado no salão ao lado da lareira em `(4.8f, -0.72f, -3.8f)`, enriquecendo o ambiente aristocrático vintage com garrafas e taças 3D.
   * **Iluminação Quente da Lareira (`_fireplaceLight`):** Luz pontual omnidirecional adicionada na coordenada da lareira (`0, 0.85f, -5.75f`) com cor âmbar (#ff6a18), raio de alcance de 7.8m e efeito de cintilação suave (*flicker*) dinâmico no `_Process`.
   * **Fallback no `AvatarComposite.cs`:** Suprimidos avisos de textura ausente (`default_base`, `default_shirt`) com rotas de salvaguarda silenciosas.

4. **Validação de QA e Build:**
   * **Gameplay QA (`tools/gameplay_smoke.gd`):** 287 asserções aprovadas com êxito (`GAMEPLAY_QA PASS`).
   * **Visual QA (`tools/visual_smoke.ps1`):** 27 capturas de tela e 25 ações interativas aprovadas com 0 falhas e 0 avisos de layout (`layout_issues=0, failures=0`).
   * **Camera QA (`tools/visual_smoke.ps1 -CameraOnly`):** 100% de aprovação nos testes de visão POV, limites de rotação cervical e alternância Overhead (`CAMERA_QA PASS`).
   * **Exportação Desktop:** `Game Hub.exe` e `Game Hub.pck` re-exportados com sucesso para Windows Desktop x86_64.

---

### Patch 1: Pipeline de Resoluções, Gráficos e Rede Híbrida
* **Arquitetura de Vídeo (`core/systems/VideoSettingsManager.cs`):**
  * Detecção e aplicação nativa de 720p (HD), 1080p (Full HD), 1440p (2K/Quad HD) e 2160p (4K Ultra HD).
  * Suporte a modos de exibição: Janela (*Windowed*), Tela Cheia Exclusiva (*Exclusive Fullscreen*) e Tela Cheia sem Bordas (*Borderless*).
  * Escala interna de renderização 3D (*Render Scale*) ajustável de 50% a 100%, desacoplada da resolução do monitor para otimização de performance.
* **Pipeline Gráfico PBR (`core/systems/GraphicsQualityManager.cs`):**
  * Presets: Leve, Médio e Ultra.
  * SSAO (*Screen-Space Ambient Occlusion*) ativo para sombras de contato sob mesas e personagens.
  * SSR (*Screen-Space Reflections*) com contagem dinâmica de passos (36 a 56 steps).
  * Tonemapping ACES com exposição calibrada e Bloom/Glow suave.
* **Rede Híbrida (`core/networking/NetworkManager.cs` e `hub/scripts/LobbyUI.cs`):**
  * Suporte híbrido arquitetado para LAN via ENet (`ENetMultiplayerPeer`) e WAN/Online via WebSockets (`WebSocketMultiplayerPeer`).
  * Descoberta local por broadcast UDP na porta `42429`.

### Patch 18: Cadência Natural e Deliberada da IA, Tensão nas Chamadas de Truco, Ocultação da Malha em POV e Enquadramento dos Modelos 3D Detalhados — 07/09/2026

1. **Cadência Natural da Partida e IA Deliberada (`games/truco/scripts/TrucoGameManager.cs`, `games/poker_roguelike/scripts/PokerGameManager.cs`):**
   * **IA pensando em ritmo humano:** o timer de reflexão no Truco foi calibrado de `0.8s–1.4s` para `1.8s–2.8s` (`_rng.RandiRange(18, 28) / 10f`), eliminando a sensação de jogadas "na velocidade da luz".
   * **Tensão ao pedir Truco:** ao solicitar Truco, a IA executa o gesto de desafio corporal (`truco`), com uma pausa dramática de antecipação (`0.65s`) antes do aviso e efeito sonoro na tela.
   * **Pausa de resposta ao Truco:** o oponente agora pondera por `2.2s` (`RespondAIToTrucoDelayed`) antes de responder ao grito de Truco do jogador.
   * **Ritmo de Tombos e Corte:** intervalo entre cartas jogadas estendido para `1.30s`, pausa após o tombo aumentada para `1.85s` e tempo de corte de baralho ajustado para `1.15s`.
   * **Ritmo no Pôquer:** em `PokerGameManager.PlayHand()`, adicionado delay de `0.85s` para que todas as cartas cheguem fisicamente à mesa antes de iniciar a contagem e efeitos de pontuação, seguido de pausa de contemplação de `2.2s` da mão vencida.

2. **Destaque Visual aos Modelos 3D Detalhados à Mesa (`games/truco/scripts/TrucoUI.cs`, `core/systems/SettingsManager.cs`):**
   * **Oponente 3D Detalhado:** o rival padrão no Truco 1v1 foi alterado de Bento (procedural antigo) para Barão da Meia-Noite / Seu Corvo (modelos com Armature, texturas ricas e animações completas).
   * **Perfil Padrão:** `CharacterId` padrão atualizado de `"nina"` para `"corvo"`.
   * **Animações Reativas:** ao pedir/aumentar truco é acionado o gesto `truco`, e ao vencer uma vaza/mão é acionado o gesto `victory`.

3. **Correção de Visão em Primeira Pessoa (POV) e Mesa 3D (`core/visuals/TableStage.cs`):**
   * **Ocultação do Jogador Local:** no assento local (`Seat 0`), a malha do personagem é ocultada (`_actors[i].Visible = !isLocal`), eliminando qualquer torso, pescoço ou cabelo bloqueando a visão das cartas e da mesa em POV e Table view.
   * **Proximidade e Enquadramento:** assento do oponente em 1v1 aproximado de $Z = -3.10\text{m}$ para $Z = -2.50\text{m}$, com câmera POV em $FOV = 48^\circ$ e mira centralizada em `Vector3(0, 0.72f, -0.35f)`, enquadrando o rival à altura dos olhos, sob a iluminação direta do lustre da mesa.
   * **Física de Impacto e Sombra:** a sombra de contato permanece colada ao feltro verde durante o arco do voo da carta, e ao aterrissar ocorre um micro-amortecimento físico elástico (*settling bounce*) de 15mm.
   * **Modo Padrão POV:** `DefaultCameraMode` padrão atualizado para `"pov"`.

4. **Validação de QA e Executável:**
   * `GameplayChecks.cs`: 171 asserções aprovadas com êxito.
   * `visual_smoke.ps1`: 27 capturas de tela e 25 ações interativas aprovadas com 0 falhas e 0 problemas de layout.
   * `Game Hub.exe` e `Game Hub.pck` re-exportados para Windows Desktop x86_64.

### Patch 17: Ajuste de Cadência das Cartas Jogadas, Pausa de Tombos no Truco e Animações Orgânicas Fluidas — 07/09/2026

1. **Cadência e Velocidade das Cartas Jogadas (`core/visuals/TableStage.cs`, `games/truco/scripts/TrucoGameManager.cs`, `games/poker_roguelike/scripts/PokerUI.cs`):**
   * **Problema Original:** As cartas viajavam em apenas `0.36s` a `0.42s`, parecendo teletransportar-se sem peso ou tato. No Truco, ao finalizar um tombo, a rodada seguinte iniciava imediatamente (0s de intervalo), impedindo a leitura das cartas na mesa e do placar. No Pôquer, as cartas eram disparadas com intervalo de apenas `60ms`.
   * **Nova Física e Duração de Lançamento (`TableStage.PlayCard`):**
     * Duração de voo estendida de `0.42s` para `0.82s` (jogada normal) e de `0.36s` para `0.65s` (batida de manilha especial), com interpolação `Cubic.EaseOut`.
     * O arco parabólico (`peakArc = 0.48m / 0.70m`) agora permite que o olho humano acompanhe com clareza a carta saindo da mão, girando suavemente no ar e estalando com precisão contra o feltro da mesa no exato momento do impacto sonoro.
   * **Pausa e Contemplação de Tombos no Truco (`TrucoGameManager.ResolveCurrentRound`):**
     * Inserida uma pausa de `1.35s` (`await WaitForAnimation(1.35f)`) após a resolução de cada tombo e `1.05s` após cada carta jogada (`PlaySeatCard`).
     * As cartas permanecem visíveis lado a lado na mesa enquanto o status da rodada é exibido ("Você ganhou o tombo!", "Oponente ganhou o tombo!", "Empate!"), garantindo clareza tática antes do início da próxima vaza.
   * **Ritmo de Lançamento no Pôquer (`PokerUI.cs`):**
     * Cartas jogadas da mão escalonadas com intervalo de `150ms` (`i * 0.15f`) e do dealer com `160ms` (`0.20f + i * 0.16f`), criando um efeito cascata ritmado e agradável.

2. **Descongelamento e Fluidez Expressiva dos Modelos 3D (`tools/build_rigged_detailed_cast.py`, `core/visuals/CharacterViewer3D.cs`):**
   * **Problema Original:** As rotações de repouso (`idle`) tinham amplitude diminuta ($\sim 1^\circ$ a $2^\circ$, 8mm), fazendo os personagens parecerem estátuas congeladas ("quase travadas"). Além disso, no visualizador da Coleção, o Godot importava as faixas com `LoopMode = None`, congelando a animação após 2,6 segundos.
   * **Novas Amplo-Curvas Bezier nos 5 Personagens Detalhados:**
     * `idle`: Respiração viva do tórax ($+5\text{cm}$ de elevação, $8^\circ$ de expansão), arco de coluna, balanceamento do quadril ($3.5\text{cm}$), micro-movimentos dos ombros, punhos segurando cartas, olhar vivo com inclinação de cabeça e pestanejo natural, fanning elegante de asas para Seu Corvo e Barão ($\sim 18^\circ$), balanço rítmico da cauda felpuda do Zeca ($\sim 25^\circ$) e ondulação sinuosa em onda S da Dama de Copas.
     * Gestos de ação (`truco`, `victory`, `boss_intro`, `flourish`, `play_card` e `entrance`) reescritos com arcos de antecipação dinâmicos, movimentos de torso e finalizações expressivas.
   * **Loop Infinito e Transições Suaves na Coleção (`CharacterViewer3D.PlayAnimation`):**
     * Ação `idle` configurada com `LoopMode = Animation.LoopModeEnum.Linear` tanto na inicialização quanto ao selecionar o botão de repouso.
     * Ao acionar gestos ("Desafio", "Vitória", "Jogar", "Floreio"), o modelo executa a ação dramática completa com transição de 0,2s e, ao terminar, retorna automaticamente ao ciclo de respiração contínua sem travar.

3. **Validação de QA e Build:**
   * **Gameplay QA C# (`GameplayChecks.cs`):** 171 asserções aprovadas com êxito (`GAMEPLAY_QA PASS`).
   * **Fumaça Visual (`tools/visual_smoke.gd`):** 27 capturas de tela e 25 ações com 0 falhas e 0 avisos de layout (`layout_issues=0, failures=0`).
   * **Visualizador 3D:** Screenshots da Coleção atualizados com enquadramento integral e iluminação de estúdio.
   * **Executável Windows:** `Game Hub.exe` e `Game Hub.pck` re-exportados com sucesso.

---

### Patch 16: Esqueleto Articulado (Armature Rigging), Deformação Orgânica e Animações Bezier nos Personagens 3D Detalhados — 07/09/2026

1. **Modelos 3D Detalhados e Rigging com Pesos Automáticos (Automatic Skinning Weights):**
   * **Problema com Modelos Rígidos Fatiados:** Fatiar modelos complexos gerados por IA em peças sólidas cria aberturas ocas nas juntas (cotovelos, joelhos, ombros) e fendas visíveis.
   * **Solução com Armature Completo no Blender:** Script automatizado (`tools/build_rigged_detailed_cast.py`) cria esqueletos proporcionais e vincula malhas contínuas usando `ARMATURE_AUTO` (pesos de vértices suaves):
     * Hierarquia óssea: `Root` -> `Pelvis` -> `Spine` -> `Chest` -> `Neck` -> `Head`, com cadeias completas de membros `Shoulder.L/R`, `UpperArm.L/R`, `Forearm.L/R`, `Hand.L/R`, `Thigh.L/R`, `Shin.L/R`, `Foot.L/R`.
     * Apêndices anatômicos especializados: `Wing.L/R` (Corvo e Barão), `Tail.01/02` (Zeca) e cauda sinuosa de 5 segmentos `Tail.01..Tail.05` (Dama).
     * Normalização: Modelos escalados para altura humana de 1.85m e aterrados na base $Z=0$.
     * Marcador `Head` (Node3D empty) em $Y=1.58\text{m}$ parentado ao esqueleto para suporte nativo ao clipping de cabeça em POV do Godot.
     * Preservação integral das texturas PBR 2K (albedo atlas, normal e roughness).

2. **7 Clipes de Animação Bezier Fluídos por Personagem:**
   * Curvas de animação com interpolação `BEZIER` geradas proceduralmente para 7 ações de cada personagem:
     * `idle`: ciclo de respiração sutil (peito, coluna, asas/cauda e cabeça).
     * `entrance`: reverência aristocrática clássica, passos e saudação com as mãos/asas abertas.
     * `truco`: avanço vigoroso à frente com impacto dramático e inclinação sobre a mesa.
     * `victory`: celebração triunfante com braços erguidos e postura orgulhosa.
     * `boss_intro`: postura intimidadora de mestre do salão com braços cruzados e peso firme.
     * `flourish`: exibição elegante em giro gracioso com asas/mãos abertas.
     * `play_card`: movimento natural estendendo o braço para lançar a carta na mesa.
   * Faixas NLA dedicadas configuradas para exportação compatível com Godot glTF.

3. **Integração no Clube, Visualizador 3D e Retratos de Estúdio:**
   * Exportação dos arquivos `.glb` otimizados para `assets/models/club/{corvo, barao, dama, zeca, iara}.glb`.
   * Geração de retratos 3D de alta fidelidade com iluminação de estúdio para `assets/models/club/{ident}_3d.png`.
   * Enquadramento no visualizador 3D (`CharacterViewer3D.cs`): distância da câmera calibrada para `2.85f` e escala `0.95f`, exibindo o corpo inteiro dos personagens sobre o plinto dourado sem cortes.
   * Coexistência harmoniosa: Nina, Bento e Dona Onça permanecem operacionais com seus modelos procedurais até a geração de seus assets detalhados.

4. **Validação de QA e Build:**
   * **Bateria C# (`GameplayChecks.cs`):** 172 asserções aprovadas com êxito (`GAMEPLAY_QA PASS`).
   * **Fumaça Visual (`tools/visual_smoke.gd`):** 27 capturas de tela e 25 ações com 0 falhas e 0 avisos de layout (`layout_issues=0, failures=0`).
   * **Executável Windows:** `Game Hub.exe` e `Game Hub.pck` re-exportados com sucesso.

---

### Patch 15: Proporção Real das Cartas na Mesa 3D, Cenário Imersivo em Tela Aberta no Truco e Loja Balatro Roguelike — 06/09/2026

1. **Proporção e Dimensões das Cartas na Mesa 3D (`core/visuals/TableStage.cs`):**
   * **Problema Original:** As cartas jogadas na mesa eram instanciadas com `Size = (1.25f, 0.032f, 1.76f)`, mais que o dobro do baralho físico na mesa (`0.58f × 0.82f`), parecendo gigantes e exigindo um truque de encolhimento artificial (`Scale = 0.464f`) ao serem guardadas.
   * **Nova Geometria Proporcional:** As cartas agora nascem e permanecem com `Size = (0.58f, 0.018f, 0.82f)` com escala uniforme `1.0`. Todos os componentes associados foram refinados:
     * Sombra de contato: `0.64f × 0.88f`.
     * Moldura de ouro traseira: `0.62f × 0.014f × 0.86f`.
     * Face da carta: `0.56f × 0.80f`.
     * Placa de identificação/manilha: `Position = (0, 0.06f, -0.52f)`, com `FontSize = 22, PixelSize = 0.0028f`.
     * Bandeja de feltro de descarte: redimensionada de `1.35f × 1.85f` para `0.66f × 0.90f`.
   * **Animação de Recolha e Retorno ao Baralho (`CollectRoundCardsToDiscard`):**
     * As cartas jogadas da mão/tombo reúnem-se ordenadamente no centro da mesa em uma pilha única `(0, 0.16f, 0.10f)`.
     * Em seguida, deslizam em bloco suavemente direto para cima do baralho físico em `(-1.85f, 0.082f, 0.20f)`, mantendo suas proporções naturais e eliminando artefatos de escala.

2. **Cenário Imersivo em Tela Aberta e HUD Discreto no Truco (`games/truco/scripts/TrucoUI.cs`):**
   * **Eliminação de Molduras Estreitas:** O viewport 3D da mesa (`_stage`) agora ocupa 100% da tela em tela cheia aberta (`LayoutPreset.FullRect`), permitindo contemplar todo o salão clássico, iluminação e oponentes.
   * **Placar Discreto no Canto Superior Esquerdo:** Cartão translúcido e compacto contendo o placar `NÓS : ELES`, meta de 12 pontos, valor da aposta atual (`VALE X PONTOS`), indicador de tombos (`TOMBO 01 / 03`) e menção sutil a quem distribui e quem corta o baralho.
   * **Vira Compacta no Canto Superior Direito:** Carta do tombo em formato compacto (64×92 px) com ordem de manilhas e botão `"Voltar ao clube"`.
   * **Mão em Primeira Pessoa (POV):** Cartas do jogador flutuam na base da tela com o status da vez (`_statusLabel`, ex.: *"Sua vez! Escolha uma carta."*) posicionado diretamente abaixo das cartas da mão.
   * **Atalhos Rápidos e Menu [Esc]:** Os botões que poluíam a tela (cenário, câmera e ajustes) foram ocultados por padrão. Pressionar `Esc` abre o painel de ajustes in-game, `M` alterna o cenário da sala, e `C` cicla as câmeras.

3. **Loja de Pôquer Roguelike Estilo Balatro (`games/poker_roguelike/scripts/`):**
   * **Referência Visual e Funcional do Balatro:** Recriação completa da tela de loja entre rodadas inspirada na referência oficial:
     * **Sidebar Esquerda:**
       * Letreiro neon clássico `SHOP ("Improve your run!")` com bordas vermelhas iluminadas.
       * Pontuação da rodada com ícone de chip azul.
       * Fórmula matemática `[Fichas Azuis] × [Mult Vermelho]`.
       * Pílulas de recursos: `Mãos: X` (azul) e `Descartes: X` (vermelho).
       * Saldo em fichas/dinheiro `FICHAS: $ X`.
       * Marcador de progressão `Ante: X / 8` e `Rodada: X`.
       * Botões de suporte: `Info da Corrida` e `Ajustes [Esc]`.
     * **Inventário Superior:**
       * Barra de 5 slots de Coringas (`CORINGAS (X / 5)`) com miniaturas dos itens adquiridos e slots vazios demarcados.
       * 2 slots de Consumíveis (`CONSUMÍVEIS (0 / 2)`).
     * **Painel Central de Compras:**
       * Coluna de ações com botões verticais `Próxima rodada →` (dourado/vermelho) e `Reroll $ 5` (verde escuro).
       * Vitrine de Coringas com tags de preço destacadas (`$ 4`, `$ 5`, `$ 6`), ícone, nome e descrição de efeito.
       * Fileira inferior de consumíveis: `CUPOM DO ANTE · $ 10` (efeitos permanentes de +1 Mão, +1 Descarte ou +1 Mult) e Pacotes Booster (`PACOTE BUFFOON 🎁 $ 4` e `PACOTE CELESTIAL ✨ $ 4`).
     * **Baralho Físico:** Pilha de cartas no canto inferior direito com contador dinâmico (ex.: `🎴 Baralho: 37 / 52`).
   * **Novos Coringas no `RelicManager.cs`:**
     * `JokerClassic` (+4 Mult para toda combinação).
     * `FlushBoost` (+30 Fichas para mãos de Flush).
     * `PairMaster` (+20 Fichas e +1 Mult para Pares e Dois Pares).
     * `GoldenTicket` (+3 Fichas de bônus ganhas no fim da rodada).
     * `ChaosDice` (+1 a +6 Mult aleatório a cada mão jogada).

4. **Validação de QA e Build:**
   * **Bateria C# (`GameplayChecks.cs`):** **171 asserções aprovadas com êxito** (`GAMEPLAY_QA PASS`).
   * **Fumaça Visual (`tools/visual_smoke.gd`):** **25 capturas de tela e 21 ações com 0 falhas e 0 avisos de layout** (`layout_issues=0, failures=0`, incluindo `poker-loja.png`).
   * **Executável Windows Atualizado:** `Game Hub.exe` e `Game Hub.pck` gerados com sucesso.

---

### Patch 2: Alinhamento Físico da Mesa e Assentos (Seat 0)
* **Problema Original:** O assento do jogador (`Seat 0`) estava posicionado com deslocamento assimétrico ($X = -1.8\text{m}$) e rotação multiplicada por `0.65f`, gerando uma inclinação diagonal distorcida em relação às cartas e à cadeira.
* **Arquivos Modificados:** `core/visuals/TableStage.cs`.
* **Solução:**
  * Seat 0 centralizado no eixo exato da mesa em `Vector3(0.0f, -0.72f, 3.10f)` para configurações de 2 e 4 lugares.
  * O personagem (`actor`), a poltrona (`chair`) e o leque de cartas na mão (`hand`) compartilham o mesmo eixo frontal direto ($0^\circ$).

---

### Patch 3: Limpeza da Mesa 3D & Remoção de Overlays 2D
* **Problema Original:** Havia barras 2D desenhadas diretamente sobre o feltro 3D (`_tableOpponentRow`, `_tablePlayerRow`, textos `"ADVERSÁRIO / CARTAS NA MESA"` e `"VOCÊ / CARTAS NA MESA"`), ocupando mais de 200px verticais e obstruindo a visão 3D.
* **Arquivos Modificados:** `games/truco/scripts/TrucoUI.cs`.
* **Solução:**
  * Remoção completa das caixas e faixas de cartas 2D sobrepostas à mesa.
  * Implementação de um badge discreto e flutuante no topo (`tableHud`) contendo apenas o número do tombo (`"TOMBO 01 / 03"`) e a mensagem de status da vez.
  * A mesa agora exibe exclusivamente as cartas físicas 3D jogadas no feltro.

---

### Patch 4: Qualidade dos Pixels e Apresentação das Cartas 3D
* **Arquivos Modificados:** `core/visuals/TableStage.cs`.
* **Solução:**
  * `SubViewport` configurado com resolução nativa `1280x720`, MSAA 4X (`Viewport.Msaa.Msaa4X`) e FXAA ativo (`Viewport.ScreenSpaceAAEnum.Fxaa`).
  * Texturas das cartas com filtragem anisotrópica (`LinearWithMipmapsAnisotropic`) e iluminação per-pixel com emissão sutil para manter a legibilidade dos naipes mesmo em penumbra.
  * Dimensões das cartas aumentadas para $1.25\text{m} \times 1.76\text{m}$ (com borda dourada para manilhas especiais).
  * Cartas posicionadas planas sobre o feltro em $Y = 0.095\text{m}$ e centralizadas em $Z = -0.25\text{m}$, garantindo visibilidade total sem colisão com o personagem do jogador.
  * Animação `CollectRoundCardsToDiscard`: cartas do tombo se juntam no centro da mesa, tombam ao contrário (face para baixo) e deslizam suavemente para o montinho de descarte ao lado do baralho.

---

### Patch 5: Cenários 3D Ricos Gerados no Blender 5.2
* **Script de Automação:** `tools/build_club_rooms.py`.
* **Modelos Gerados (`assets/models/club/`):**
  * `room_classic_club.glb` — Clube clássico com tons de mogno e veludo verde.
  * `room_barao_lounge.glb` — Lounge noturno do Barão em roxo imperial e dourado.
  * `room_dama_salon.glb` — Salão da Dama em tons escarlate e champanhe.
  * `room_cyber_casino.glb` — Cassino cyberpunk contemporâneo com neon ciano e magenta.
  * `club_chair.glb` — Poltrona estofada de luxo com detalhes em latão.
* **Elementos de Cenário Modelados:**
  * Tapete de cassino ornamental de 3 camadas com bordas e medalhões dourados sob a mesa.
  * Paredes com *boiserie*, rodapés altos, painéis almofadados em relevo e papel de parede adamascado.
  * 4 pilastras clássicas caneladas, cornijas com dentículos e 3 quadros a óleo com molduras chanfradas douradas.
  * Cortinados laterais de veludo com braçadeiras de ouro e arandelas de bronze com iluminação pontual quente.

---

### Patch 6: Super Upgrade dos 8 Personagens & Animações Bezier
* **Artes Conceituais em Pixel Art 16-bit:**
  * Criada a folha mestre unificada [assets/sprites/characters/club/club-cast.png](file:///c:/workspace/multigame/assets/sprites/characters/club/club-cast.png) em matriz $8 \times 2$ ($3544 \times 886\text{px}$):
    * Linha 0: Poses Neutras à mesa com cartas na mão.
    * Linha 1: Poses de Reação / Vitória / Comemoração.
  * Elenco Completo:
    * `0: Nina` (Inventora — óculos redondos, cabelos cacheados, colete esmeralda).
    * `1: Bento` (Anfitrião — bigode clássico, suspensórios de couro, relógio de ouro).
    * `2: Seu Corvo` (Observador — corvo cavalheiro, pince-nez com corrente, fraque esmeralda).
    * `3: Dona Onça` (Veterana — onça-pintada, jaqueta de veludo bordô, joias de rubi).
    * `4: Iara` (Capivara aristocrata — flor vitória-régia na orelha, colete esmeralda, calma e serena).
    * `5: Zeca` (Raposa malandra — chapéu fedora clássico com fita azul, colete marinho, relógio de bolso).
    * `6: Barão da Meia-Noite` (Boss Coruja — smoking púrpura, monóculo de ouro, asas e coroa dourada).
    * `7: Dama de Copas` (Boss Serpente — rainha cobra encapuzada, tiara de rubis, vestido escarlate vitoriano).
* **Modelos 3D de Alta Fidelidade (`tools/build_blender_cast.py`):**
  * Geometria suavizada (*Smooth Shading*) em todas as formas orgânicas e tecidos.
  * Alfaiataria completa modelada: colarinhos estruturados, lapelas chanfradas, botões dourados em relevo, abotoaduras, gravatas/cravats e detalhes anatômicos exclusivos.
  * Renders de estúdio 3D gerados automaticamente em 320x400 com iluminação 3-point (`assets/models/club/{ident}_3d.png`).
* **5 Animações Bezier por Personagem:**
  1. `entrance` (48 frames) — Entrada com balanço natural e aceno à mesa.
  2. `truco` (48 frames) — Desafio explosivo projetando tronco e mão sobre a mesa.
  3. `victory` (48 frames) — Comemoração triunfante com punho erguido.
  4. `boss_intro` (48 frames) — Abertura teatral intimidadora com capa/asas abertas.
  5. `flourish` (48 frames) — Truque refinado de exibição e rotação de cartas no ar.
* **Código C# Atualizado:**
  * `core/visuals/CharacterCatalog.cs`: Método `Portrait(index, react)` atualizado para indexar diretamente as 8 colunas da folha mestre de sprites.

---

### Patch 7: Integração com o MCP Server do Blender em Tempo Real
* **O que é:** Conexão nativa entre o agente de IA e a sessão aberta do Blender através do *Model Context Protocol* (MCP).
* **Componentes Instalados:**
  * Servidor Executável: `C:\Users\Lucas\.local\bin\blender-mcp.exe`.
  * Addon do Blender: `C:\Users\Lucas\AppData\Roaming\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py`.
  * Configuração Global: `C:\Users\Lucas\.gemini\config\mcp_config.json`.
* **Porta de Comunicação:** `9876` (TCP Localhost).
* **Ferramentas MCP Disponíveis para o Agente:**
  * `get_scene_info` — Inspeciona objetos, materiais e hierarquias da cena ativa.
  * `get_viewport_screenshot` — Captura screenshots em tempo real da 3D Viewport do Blender.
  * `execute_blender_code` — Executa código Python interativo (`bpy`) na janela ativa.
  * Pesquisa e download de assets de Poly Haven, Sketchfab e Poly Pizza.

---

### Patch 8: Refinamento pelo Blender MCP, Poses de Repouso e Iluminação da Mesa — 06/09/2026

**Base preservada:** snapshot inicial `86ce062` e, depois, os fontes de construção mais recentes do usuário em `ec149ea`, enviados à branch **Backup** antes de seus respectivos refinamentos. Implementação na branch **review**, mantendo o acervo de personagens e as quatro salas recebidas.

* **Blender conectado:** `tools/blender_bridge.py` conversa com o protocolo JSON do addon local na porta `9876`, usando `get_scene_info` e `execute_code`. `tools/refine_club_mcp.py` foi executado na instância aberta do Blender. As cenas originais não são apagadas; fontes refinadas são salvas separadamente.
* **Direção de arte confirmada pelo usuário:** preservar essência, silhueta, roupas, cores e personalidade; elevar o acabamento da construção existente. `tools/polish_cast_mcp.py` aplica subdivisão seletiva da anatomia, chanfros nas bordas e ajuste de rugosidade de tecidos nos oito personagens. Mantém quatro grupos articulados por modelo, com mais geometria nas curvas. Os números antes/depois ficam em `assets/models/club/cast-polish.json`; os retratos de estúdio passam a 640×800. A referência AAA orienta o acabamento, sem transformar o elenco em outros personagens.
* **Corvo e Onça:** punhos com penas e broche no Corvo, detalhes do focinho e lapela na Onça. Novos movimentos articulados: saudação contida e abertura das asas no Corvo; preparação do ombro, desafio com a pata e comemoração na Onça. O floreio cosmético já desbloqueado por vitórias usa os novos gestos.
* **Bosses:** Barão abre as asas e faz uma reverência; Dama inclina o corpo e observa os dois lados antes do desafio. Continuam exclusivos de boss. Mantidos os cinco nomes de clipes exigidos em todos os oito GLBs.
* **Correção importante nos oito modelos:** o exportador usava o frame 0, fora das faixas NLA, zerando a posição de cabeça/braços em repouso. Agora exporta a pose atual dentro da faixa. Personagens completos também com **Reduzir movimento** e antes da primeira animação. O gerador base recebeu a mesma correção.
* **Materiais e construção no Blender:** busca do shader pelo tipo do nó, em vez de seu nome traduzido, corrigindo materiais brancos. Modificadores são aplicados em cada peça antes da união, evitando que o modificador de um objeto deforme todas as lapelas e botões. Camisa, colete e acessórios recebem separação em profundidade; mangas usam extremidades arredondadas. O Corvo conserva a camisa creme da identidade anterior. Fontes são salvos por cena, sem substituir a sessão aberta do Blender.
* **Cenário:** relógio de salão original, com pedestal de madeira, mostrador creme, latão e pêndulo animado, integrado às mesas. Usa duas malhas e nenhuma luz adicional. O pêndulo pausa com redução de movimento. Luzes das arandelas reposicionadas para a frente da parede existente.
* **Mesa e clareza:** estado da rodada movido para o placar lateral no Truco, liberando o rosto do participante ao fundo. Enquadramento ajustado para a coroa do boss na área panorâmica do pôquer. Leques avançados à frente da roupa para continuarem visíveis, mantendo o alinhamento de cada assento. Cartas físicas mantidas, com superfície fosca sem emissão branca; corrigido o espaçamento do descarte na redução de movimento. Removidos os dois contêineres antigos de cartas 2D que permaneciam órfãos na memória.
* **Gráficos:** a mesa respeita a chave geral, os controles individuais de AO/reflexos, VFX e o renderer. Perfil básico usa MSAA 2× e iluminação sem sombras dinâmicas; modo detalhado usa MSAA 4× e sombras/opções extras. FXAA, SSAO e SSR não são ativados no Compatibility. O gerenciador agora avisa os mundos privados dos SubViewports mesmo sem WorldEnvironment na raiz. Continuam sendo efeitos de rasterização/espaço de tela, sem ray tracing por hardware; GI da mesa usa preenchimento ambiente, sem SDFGI.
* **Áudio e manutenção:** players param e liberam streams ao sair. QA headless usa saída de áudio Dummy e não ignora erros de recursos ao encerrar.

**Arquivos de arte:** oito fontes `art/blender/*_mcp.blend`, `art/blender/club_clock.blend`, oito GLBs de personagens atualizados, `club_clock.glb`, oito retratos de estúdio 640×800, `assets/models/club/mcp-refinements.json` e `cast-polish.json`. O atlas pixel art recebido permanece como retrato da interface.

**Reproduzir sobre os fontes atuais**, com o servidor Blender ligado:

```powershell
python tools/blender_bridge.py get_scene_info
python tools/blender_bridge.py execute_code --code-file tools/refine_club_mcp.py --timeout 120
python tools/blender_bridge.py execute_code --code-file tools/polish_cast_mcp.py --timeout 180
```

Se regenerar primeiro os oito personagens com `build_blender_cast.py`, reaplique `refine_club_mcp.py` e depois `polish_cast_mcp.py`. Para arte manual, use os fontes `_mcp.blend`; o script de refinamento parte dos fontes base, não de edições manuais posteriores nesses arquivos. Ambos também funcionam no Blender em background.

Para reconstruir também os fontes base pelo servidor, use `python tools/blender_bridge.py execute_code --code-file tools/rebuild_cast_mcp.py --timeout 180` antes dos dois passos acima. Esse comando recria os `.blend` base a partir do gerador e trabalha em cenas separadas. O polimento final levou o Corvo de **27.828 para 63.184 faces** e a Onça de **26.502 para 53.106 faces**; são faces Blender, não a contagem de triângulos após exportação/LOD.

**Próximas entregas:** missões além das três vitórias, poderes mecânicos próprios dos bosses e sincronização de partida entre computadores. Esta rodada não adiciona rede de turnos nem o modo Fodinha. Decisões para a próxima etapa: habilidades de Barão/Dama e tipos de missão para desbloquear reações.

**Validação final:** build C# sem erros/avisos; **71 asserções de integração aprovadas**, incluindo os cinco clipes, as quatro partes de cada personagem, cores dos materiais, repouso sem animação, partidas solo 2×2/3×3 e decisões de Pena. QA visual: **20 capturas, 18 ações, zero falhas e zero problemas de enquadramento de interface detectados**; [relatório](docs/qa-mcp.json). Há ainda avisos de ObjectDB ao encerrar o Godot (1–2 instâncias nos testes), sem erro de recurso na execução final. O encerramento do QA drena o mixer de áudio antes de liberar os players.

**Executável:** `Game Hub.exe`/`Game Hub.pck` e runtime .NET local reexportados em **debug para revisão**. Os templates release instalados continuam incompletos. Para copiar a versão a outro PC, incluir também `data_GameHub_windows_x86_64` (gerado, fora do Git) ou exportar novamente a partir do projeto.

**Desempenho medido com os modelos polidos:** teste sintético com seis personagens e 18 cartas, RTX 3050, Vulkan/Forward+, VSync desligado, 120 quadros por combinação após aquecimento. Mediana/P95 em 720p: Leve **1,318/1,652 ms**, Ultra **1,873/2,174 ms**. Saída 4K: Leve **3,947/4,332 ms**, Ultra **4,477/4,860 ms**. [Relatório bruto](docs/benchmark-mcp.json) e capturas em `docs/screenshots-mcp/`. A janela 4K inclui a interface e o SubViewport da mesa com sua escala atual; não é uma medição de ray tracing nativo nem garantia de FPS para outras máquinas ou partidas prolongadas. Não comparar diretamente com a medição anterior ao polimento: a resolução do desktop e a carga da máquina mudaram durante a sessão.

---

### Patch 9: Nitidez em 4K, Fodinha e Refinamento de Geometria — 06/09/2026

**Base preservada:** `dd9f7bc` enviado à branch **Backup** antes das alterações. Trabalho na branch **review**; `main` preservada. Experimentos locais não rastreados do usuário não entram no commit.

**Correção da imagem pixelada:** `TableStage` usava `SubViewportContainer.Stretch`, que impunha o tamanho lógico da interface ao render 3D. A janela 4K apenas ampliava essa textura pequena. Agora um `Control` apresenta a textura com filtragem linear, e o SubViewport recebe o tamanho físico da área da mesa, calculado a partir das transformações do canvas e da janela. A interface mantém seu tamanho de leitura. No pôquer em 4K, a mesa passa de **948×245 para 2844×735 pixels**; no truco, **724×375 para 2172×1125**; em Fodinha, **1002×421 para 3006×1263**. Esses são os pixels ocupados pela mesa, não o tamanho da janela inteira. A escala 3D de 50–100% continua disponível: para máxima nitidez, usar 100%. A correção também vale ao executar pelo editor; a prévia 3D nativa do editor continua sujeita às preferências próprias do Godot.

**Geometria via Blender MCP:** `tools/refine_fidelity_mcp.py` executado na sessão local, porta 9876, restaurando a cena aberta. Subdivisão seletiva dos detalhes dos rostos dos oito personagens, mantendo silhuetas, materiais, roupas, quatro grupos articulados e cinco animações. Fontes separados `art/blender/*_fidelity.blend`; os `_mcp.blend` anteriores permanecem intactos. As quatro salas recebem chanfros nas molduras, colunas e arandelas, com vértices soldados antes do bevel e normais corrigidas. Poltronas também recebem esse acabamento. A mesa procedural usa 192 segmentos no aro/feltro, antes 48; fichas mantêm 48. LOD automático do Godot continua habilitado.

| Modelo | Faces anteriores | Faces atuais |
| --- | ---: | ---: |
| Corvo | 63.184 | 64.336 |
| Onça | 53.106 | 102.066 |
| Nina | 57.112 | 80.152 |
| Bento | 57.054 | 137.694 |
| Iara | 47.506 | 93.586 |
| Zeca | 43.802 | 82.394 |
| Barão | 39.029 | 52.853 |
| Dama | 44.987 | 70.547 |
| Cada sala | 2.534 | 13.158 |
| Poltrona | 1.704 | 4.488 |

Contagens de faces no Blender, não triângulos desenhados após exportação/LOD. Manifesto: `assets/models/club/fidelity-pass.json`. Cenários partem de GLBs preservados em `art/blender/fidelity-inputs`. Retratos de estúdio foram reexportados. Não há novos assets de terceiros.

**Sombreamento:** sombra da luz principal disponível no perfil básico com VFX/Forward+; mapa direcional 2048 no básico e 4096 no detalhado, filtro suave e bias ajustado para evitar autosombreamento triangular nas paredes. Atlas de sombras locais 2048/4096; luz central com sombra somente no detalhado. MSAA 2×/4×; FXAA removido da mesa para não suavizar detalhes em excesso. AO/reflexos continuam opcionais, GI usa preenchimento ambiente. Sem ray tracing por hardware e sem SDFGI.

**Fodinha jogável:** novo módulo `games/fodinha`, registrado no hub. Solo com você e três IAs, cada um por si. Escolha confirmada pelo usuário: **cinco vidas, perda da diferença absoluta entre palpite e vitórias**. Nove mãos: 1, 2, 3, 4, 5, 4, 3, 2 e 1 cartas. Usa baralho de 40 cartas, vira/manilha do truco, qualquer naipe permitido, empate de cartas comuns vencido pela primeira carta jogada. Palpites livres, inclusive o último. A abertura gira; vencedor da vaza abre a seguinte. Zero vidas elimina; termina com um sobrevivente ou ao fim das nove mãos, vencendo quem tem mais vidas, com vitória compartilhada em igualdade. Essas escolhas da primeira variante estão expostas no botão **Como jogar**.

O modo inclui entrada dos quatro personagens, leques dos adversários, cartas físicas que saem dos lugares e se acumulam no descarte, reações de vitória/palpite, placar de vidas/palpites/vitórias, passagem de mão e reinício. Reutiliza música Copper Steps e efeitos originais. IAs recebem apenas a própria mão e dados públicos; não acessam cartas privadas dos outros. Após a eliminação local, é possível acompanhar as mãos seguintes ou reiniciar. Vitória local conta para o floreio cosmético existente. LAN fica indisponível para Fodinha nesta primeira versão.

**Validação:** compilação C# sem avisos/erros; 78 asserções, incluindo simulação de 100 partidas completas de Fodinha, limites de palpites, ordem de turnos, manilhas, desempate, baralho sem repetição, eliminação e encerramento. Capturas do renderer real: 24 telas/23 ações, sem falhas ou problemas de enquadramento detectados. `docs/qa-fodinha.json` e `docs/screenshots-fodinha`. Teste adicional dos três jogos em 1080p/4K, escalas 100%/50%, com dimensões registradas em `docs/resolution-fidelity.json`; imagens em `docs/screenshots-fidelity`. Permanecem avisos anteriores de 1–2 instâncias ObjectDB no encerramento, sem erro de recurso.

**Desempenho:** teste sintético de seis personagens e 18 cartas, RTX 3050, Forward+/Vulkan, 120 quadros após aquecimento, VSync desligado. Em 4K, mediana/P95: básico **7,305/7,734 ms**, detalhado **11,153/11,516 ms**; em 720p: **1,330/1,638 ms** e **2,388/2,733 ms**. Registro em `docs/benchmark-fidelity.json`. Diferentemente do Patch 8, o alvo da mesa acompanha agora a densidade física da janela. São medições locais, sem garantia de FPS em outras máquinas; o pequeno ajuste final de bias não muda o orçamento de renderização.

**Reprodução:** `python tools/blender_bridge.py execute_code --code-file tools/refine_fidelity_mcp.py --timeout 1200`, depois compilar/importar e executar `tools/visual_smoke.ps1`. O benchmark usa `-BenchmarkOnly`; `tools/fidelity_resolution.gd` faz a inspeção de pixels dos três jogos em APPDATA isolado. Arte manual deve ser preservada antes de regenerar outputs. `Game Hub.exe`/PCK e runtime local atualizados em debug para revisão, com a mesma limitação de templates release do Patch 8.

**Próximas etapas planejadas:** variantes regionais de Fodinha (por exemplo, restrição do último palpite e carta na testa), rede com autoridade do servidor, progressão própria do modo; materiais com texturas e normais dedicadas a tecido/penas/pelagem, rig facial e novos gestos, mantendo o elenco atual. A melhoria entregue é de nitidez, geometria e luz; não há conversão fotorealista nem novas missões/bosses mecânicos nesta etapa.

**Pacote de revisão:** os presets de exportação excluem `tools/*` e os experimentos `test_*.glb`; esses arquivos continuam disponíveis no projeto. O executável exportado inicializou o hub com sucesso. O fechamento rápido desse pacote ainda pode emitir até três avisos ObjectDB, já observados no build anterior.

---

### Patch 10: Perspectiva em Primeira Pessoa (POV), Rodízio do Baralho, Articulação Anatômica 3D e Descarte Proporcional — 06/09/2026

**Base preservada:** Snapshot de segurança preservado na branch **Backup** (`a9ea168d3fbe9f4ba27743d7a5b15a4c31a0f921`). Trabalho realizado e consolidado na branch **review**; `main` preservada intacta. Experimentos locais do usuário (`test_*.glb`, etc.) mantidos fora do commit.

1. **Câmera Imersiva em Primeira Pessoa (POV) com Rotação Limitada de Pescoço (`core/visuals/TableStage.cs`):**
   * Ponto de vista do jogador no assento local (`LocalSeatIndex = 0` por padrão) elevado para `2.35m` acima da origem do assento (que repousa em $-0.72\text{m}$, totalizando $1.63\text{m}$ do plano de referência, correspondente à altura dos olhos de um participante sentado à mesa).
   * Projeção em perspectiva com campo de visão vertical calibrado em $50^\circ$ (`KeepAspect = Height`).
   * Rotação suave do olhar por arraste com botão direito do mouse (`MouseButton.Right` drag) restrita anatomicamente: limite horizontal (yaw) de até $\pm 48^\circ$ e vertical (pitch) de até $\pm 12^\circ$, impedindo giros irreais de $360^\circ$ e dispensando captura exclusiva do ponteiro do mouse (permitindo interações com cartas a qualquer momento).
   * Deslocamento sutil do pescoço (*body lean*) acompanhando o olhar e micro-respiração orgânica (~1–2 mm) sincronizada com o ritmo do jogo.
   * Ocultação do próprio corpo/cadeira do jogador local no assento ativo para prevenir oclusão e *clipping* de câmera.
   * Alternância instantânea entre modo Primeira Pessoa (POV) e visão clássica aérea 2.5D através da tecla `C` ou botão de interface `"Visão: POV [C]"` / `"Visão: Mesa [C]"`. Botão `"Centralizar"` disponível para recentralizar o olhar de imediato.

2. **Rodízio e Automação de Baralho (Truco e Fodinha):**
   * **Truco (`games/truco/scripts/TrucoGameManager.cs` e `TrucoUI.cs`):** Nova fase `TrucoPhase.Shuffling`. IAs embaralham, cortam e distribuem automaticamente com temporizações dedicadas. O jogador humano corta somente quando a sua vez de corte chega (`CutterIsPlayer`), sem exigir comandos manuais para turnos dos bots. Rotação do distribuidor e cortador sincronizada anti-horária a cada mão. Decisão de Pena pela IA resolvida autonomamente; oferta e aceitação/rejeição de Pena apresentadas ao humano exclusivamente quando ele for o destinatário.
   * **Fodinha (`games/fodinha/scripts/FodinhaMatch.cs` e `FodinhaUI.cs`):** Fase `Cutting` formalizada com validação de participante autorizado (`CutterSeat`), mãos vazias antes do corte e revelação da vira exclusivamente após o corte autorizado. Descarte de participantes eliminados na rotação de distribuidor e cortador.

3. **Redesenho Anatômico 3D & Hierarquia de Braço no Blender 5.2 (`tools/build_blender_cast.py`):**
   * **Hierarquia Articulada em 2 Fases:** Braços divididos entre ombro (`ArmL`, `ArmR`) e antebraço/cotovelo (`ForearmL`, `ForearmR`), eliminando o efeito de haste rígida esticada. Todas as 6 animações (`idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`) reescritas com flexão orgânica do cotovelo e pronação/supinação das mãos.
   * **Alfaiataria Contornada em V:** Torsos remodelados com afunilamento em V, lapelas curvas em 3 níveis (`LapelGorge`, `LapelRoll`, `LapelLower`), decote integrado e punhos de camisa de linho branco com abotoaduras douradas sob as mangas.
   * **Redesenho Completo do Corvo (`corvo`):** Eliminação da silhueta anterior. Construção de crânio esguio com cúlmen arqueado projetado para a frente, mandíbula inferior afilada, cerdas nariais, plumagem na crista e óculos *pince-nez* dourados repousados sobre a ponte nasal.
   * Reconstrução e reexportação dos 8 modelos `.glb` e retratos de estúdio `.png`.

4. **Descarte de Cartas Proporcional e Alinhado:**
   * Na fase de recolhimento de cartas ao término de cada vaza/rodada, a escala das cartas jogadas é reduzida dinamicamente de `1.0` para `0.464f` (medida idêntica à do baralho físico na mesa: $0.58 \times 0.82$).
   * Rótulos 3D de identificação e selo de MANILHA esmaecem suavemente (`modulate:a` $\to 0$).
   * As cartas são empilhadas face para baixo na bandeja de descarte ao lado do baralho (`Position = new Vector3(-1.85f, 0.082f + pileIdx * 0.018f, -0.65f)`).

5. **Estabilidade de Recursos e Caching:**
   * Caching estático de texturas de mapa em `TrucoGameManager` e `PokerGameManager`, eliminando falhas de coleta de `GCHandle` por troca repetida de cenas sob o garbage collector do .NET.

6. **Validação de QA e Executável:**
   * **Bateria Automatizada (`visual_smoke.ps1`):** **155 asserções aprovadas com êxito** em `GameplayChecks.cs` (rodízio 2v2/3v3, unicidade de baralho, autoridade de corte/pena, integridade de modelos articulados).
   * **Fumaça Visual:** **24 capturas de tela e 21 ações aprovadas com 0 falhas**, testadas nos renderizadores `Forward+` e `GL Compatibility` em resolução 4K Ultra HD.
   * **Executável Exportado:** `Game Hub.exe` e `Game Hub.pck` gerados com sucesso para Windows x86_64 em modo debug; abertura autônoma sem interface validada via `Game Hub.console.exe` em APPDATA temporário.

---

### Patch 11: Separação Anatômica Modular em 9 Partes, Gesto de Jogada (`play_card`) e Imersão em Primeira Pessoa (POV) — 06/09/2026

**Base preservada:** Trabalho executado e verificado na branch **review** com preservação da identidade dos 8 personagens e sem adição de bibliotecas externas proprietárias.

1. **Separação Anatômica Modular em 9 Partes Independentes (`tools/build_blender_cast.py`):**
   * Desacoplamento estrutural completo da malha dos 8 personagens (`nina`, `bento`, `corvo`, `onca`, `iara`, `zeca`, `barao`, `dama`) em 9 nós de malha modulares independentes:
     * `PelvisMesh`: pernas e calças de alfaiataria em repouso sentadas na poltrona do clube, sapatos Oxford de couro polido.
     * `BodyMesh`: torso em V estilizado, coletes adamascados, peitilho de linho, lapelas curvas em 3 níveis (`LapelGorge`, `LapelRoll`, `LapelLower`), botões dourados, gravatas, suspensórios, correntes e capas.
     * `HeadMesh`: crânio anatômico, feições expressivas, bicos arqueados, cerdas nariais, óculos *pince-nez*, monóculos, orelhas felinas e coroas aristocráticas.
     * `ArmLMesh` / `ArmRMesh`: ombros esferoidais articulados e mangas dos braços superiores (bíceps).
     * `ForearmLMesh` / `ForearmRMesh`: cotovelos mecânicos/orgânicos articulados, mangas de antebraço, bainhas e abotoaduras de ouro.
     * `HandLMesh` / `HandRMesh`: pulsos desacoplados, palmas com sulcos, nós dos dedos, polegares articulados e garras orgânicas (especialmente no Seu Corvo e na Onça).
   * Eliminação de deformações espúrias: a respiração e inclinação do tronco ocorrem sem mover as pernas sentadas, e a pronação/supinação do pulso opera livremente em relação ao antebraço.

2. **Nova Animação de Jogar Carta na Mesa (`play_card`):**
   * Criada em curvas Bezier no Blender 5.2.1 LTS (48 quadros a 24 fps) para todos os 8 personagens, expandindo o catálogo para 7 clipes por ator (`idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`, `play_card`).
   * Coreografia fluida de colocar a carta no feltro:
     * Quadro 1-12: torso inclina-se levemente para a frente na direção da mesa, antebraço levanta trazendo a carta para cima da borda da mesa.
     * Quadro 13-26: extensão do braço e do cotovelo projetando a carta ao centro do feltro, com rotação descendente do pulso ("snap").
     * Quadro 27-36: mão assenta a carta sobre o feltro com toque amortecido.
     * Quadro 37-48: recolhimento suave do braço de volta à poltrona, retornando ao ciclo de respiração do `idle`.

3. **Imersão em Primeira Pessoa (POV) com Preservação do Próprio Corpo (`core/visuals/TableStage.cs`):**
   * Ao jogar em visão de Primeira Pessoa (`CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov`), apenas a cabeça do jogador local (`seat == LocalSeatIndex`) é desativada (`Visible = false`), evitando oclusão da lente.
   * O peito, as lapelas, os ombros, os antebraços e as mãos permanecem visíveis no campo periférico do olhar. Ao olhar para baixo ou em direção à mesa, o jogador enxerga seu próprio corpo na poltrona do clube.
   * Sincronização de lançamento: ao disparar `PlayCard`, `TableStage` aciona `PlayGesture(seat, isSpecial ? "flourish" : "play_card")`. O jogador em primeira pessoa vê seu próprio braço estendendo-se e colocando a carta no feltro, sincronizado com o efeito sonoro de contato com o feltro.

4. **Robustez de Entrada e Redimensionamento de Janela (`TableStage.cs` e `tools/camera_smoke.gd`):**
   * O teste de contenção de ponteiro (`inside`) em `TableStage._Input` agora calcula a proporção de escala da janela (`Root.Size / Root.ContentScaleSize`), assegurando que o controle de rotação de pescoço por arraste funcione perfeitamente em 720p, 1080p, 1440p e 4K Ultra HD.

5. **Validação de QA e Executável:**
   * **Bateria C# (`GameplayChecks.cs`):** Atualizada para validar os 9 nós modulares e 7 clipes de animação em todos os modelos. **155 asserções aprovadas com êxito**.
   * **Suíte de Câmera (`camera_smoke.gd`):** **100% aprovada (`CAMERA_QA PASS []`)** em Pôquer, Truco e Fodinha.
   * **Fumaça Visual Completa (`visual_smoke.ps1`):** **24 capturas de tela e 21 ações com 0 falhas de layout**.
   * **Executável Exportado:** `Game Hub.exe` e `Game Hub.pck` gerados com sucesso para Windows Desktop x86_64.

---

### Patch 12: Mãos Articuladas com 5 Falanges, Carta Física na Mão, Acessórios Exclusivos e Recuo Táctil POV — 06/09/2026

**Base preservada:** Executado e validado integralmente na branch **review** do repositório `Multigame`.

1. **Mãos Articuladas em 5 Falanges Individuais (`tools/build_blender_cast.py`):**
   * Substituição de blocos únicos de juntas por anatomia completa de 5 dígitos em cada mão (`ThumbProximal`, `ThumbDistal`, `IndexProximal`, `IndexDistal`, `MiddleProximal`, `MiddleDistal`, `RingProximal`, `RingDistal`, `PinkyProximal`, `PinkyDistal`).
   * No Seu Corvo: garras de queratina negra polida em 3 articulações frontais e esporão traseiro (`DigitBase`, `DigitMid`, `ClawTip`, `SpurBase`, `SpurTip`).
2. **Carta 3D Física na Mão Ativa (`HeldCardBack`, `HeldCardFace`, `HeldCardTrim`):**
   * Modelada com espessura de papel e detalhes de corte fino, aninhada entre o polegar e os dedos indicador/médio na mão ativa de cada personagem.
   * Na animação `play_card`, a carta é fisicamente transportada na mão até o instante do contato com o feltro (frame 26).
3. **Acessórios Exclusivos de Braço e Mão por Personagem:**
   * **Nina:** Luva de inventora sem dedos com costuras e rebites dourados sobre as juntas (`FingerlessGlove`, `GloveRivet`).
   * **Bento:** Relógio clássico de pulso com aro de ouro e mostrador de esmalte marfim (`WatchBezel`, `WatchDial`, `LeatherStrap`) e anel no polegar direito (`BentoThumbRing`).
   * **Seu Corvo:** Anel de sinete dourado com monograma lapidado em rubi no dedo indicador (`CorvoSignetRing`, `CorvoSignetSeal`) e penugem nas mangas.
   * **Dona Onça:** Garras retráteis douradas na ponta de cada dedo e rosetas de pelagem no pulso.
   * **Iara:** Pulseira de ouro com flor de lótus rosa esculpida e pérola no pulso direito (`LotusBangle`, `LotusBlossom`, `PearlBead`).
   * **Zeca:** Luvas brancas de veludo de croupier com botões perolados no punho (`CroupierGlove`, `GlovePearlButton`).
   * **Barão:** Anel imperial com safira azul profunda lapidada no indicador esquerdo (`SapphireRingBand`, `SapphireOvalGem`).
   * **Dama:** Bracelete de serpente enrolada em ouro no antebraço direito com olhos de rubi (`SerpentCoil1`, `SerpentCoil2`, `SerpentHeadGold`, `SerpentRubyEye`).
4. **Imersão Táctil POV com Micro-Recuo no Impacto (`core/visuals/TableStage.cs`):**
   * Campo de visão (FOV) do POV ajustado para 54°, proporcionando melhor enquadramento periférico das mangas de alfaiataria, abotoaduras e mãos apoiadas na borda de couro da mesa.
   * Ao bater a carta no feltro em primeira pessoa, um micro-recuo táctil (`_tactileRecoilY`) de -0.016f com retorno em transição `Back.Out` é aplicado à câmera, criando uma sensação física e satisfatória de autoridade e peso ao colocar a carta na mesa.
5. **Verificação de QA & Build:**
   * 0 avisos e 0 erros de compilação C#.
   * `camera_smoke.gd`: 100% aprovado (`CAMERA_QA PASS []`).
   * `visual_smoke.ps1`: 155 asserções aprovadas com êxito e 24 capturas de tela sem qualquer falha visual.
   * Executável e pacote de assets (`Game Hub.exe` e `Game Hub.pck`) atualizados e prontos para distribuição.

---

### Patch 12: Cenários 3D Ricos, Seletor Dinâmico de Mapas, Animações de Colocar Cartas na Mão e Fase 2 — 06/09/2026

**Base preservada:** Snapshot de segurança preservado na branch **review** com total fidelidade anatômica e arquitetônica, mantendo os 8 personagens e os 4 salões do clube.

1. **Cenários 3D Ricos Gerados no Blender 5.2 (`tools/build_club_rooms.py`):**
   * **Teto com Vigas e Caixotões (*Coffered Ceiling*):**
     * Teto em madeira nobre a $Z = 7.35\text{m}$ decorado com vigas longitudinais e transversais, molduras douradas e medalhões roseta nas interseções, proporcionando um teto luxuoso visível ao olhar para cima em Primeira Pessoa (POV).
   * **Lustre Candelabro Suspenso (*Grand Chandelier*):**
     * Haste central de latão e canopla suspensa no teto a $Z = 5.25\text{m}$, com corpo esférico central, finial inferior, 8 braços curvos radiais, pratos de latão com velas brilhantes e pingentes de cristal lapidado.
   * **Aparador / Bar Aristocrático (*Credenza / Drinks Bar*):**
     * Móvel de mogno polido com tampo de mármore branco encostado à parede de fundo ($Y = 6.0\text{m}$), equipado com puxadores de latão, 2 decantadores de cristal lapidado com licores âmbar e rubi, 3 garrafas clássicas de destilados, balde de gelo em latão e taças de cristal.
   * **Pedestal Cabideiro com Chapéu Fedora (*Brass Coat & Hat Stand*):**
     * Coluna torneada de latão com ganchos superiores e chapéu Fedora clássico em feltro escuro repousado sobre um dos ganchos.
   * **Paredes Laterais Ornamentadas:**
     * Flancos esquerdo e direito ($X = \pm 13.5\text{m}$) agora contam com painéis em relevo *boiserie*, molduras douradas e arandelas com iluminação pontual quente.
   * Reconstrução e reexportação dos 4 modelos: `room_classic_club.glb`, `room_barao_lounge.glb`, `room_dama_salon.glb`, `room_cyber_casino.glb`.

2. **Seletor de Mapas / Cenários em Tempo Real (`TableStage.cs`, `SettingsManager.cs`, `HubMain.cs`):**
   * **Alternância Dinâmica na Mesa:** Botão integrado ao HUD da mesa (`cameraTools`) `"Cenário: [Salão Clássico] [M]"` e atalho de teclado `M`, permitindo que o jogador mude instantaneamente entre os 4 temas (`classic_club`, `barao_lounge`, `dama_salon`, `cyber_casino`) durante partidas de Truco, Pôquer ou Fodinha, sem reiniciar o jogo.
   * **Luzes Dinâmicas de Ambiente:** Ao alternar de cenário, o lustre central, as arandelas laterais e o rim light adaptam suas cores e intensidades dinamicamente (dourado clássico, púrpura imperial, âmbar escarlate ou neon ciano).
   * **Persistência de Preferência:** Propriedade `RoomTheme` adicionada a `SettingsManager.cs`, gravada na seção `[Visuals]` do `settings.cfg`.
   * **Menu de Ajustes do Hub:** Seletor de cenário adicionado na aba de vídeo do menu principal do Hub para escolha prévia da sala padrão.

3. **Animações de Colocar Cartas na Mão (`TableStage.cs`, `TrucoUI.cs`, `FodinhaUI.cs`):**
   * **Distribuição 3D Parabólica:** Em `TableStage.AnimateDeal`, as cartas viajam com arco suave e rotação dinâmica saindo do baralho até o leque físico do assento (`_hands[seat]`). Ao chegarem, emitem som de contato (`"deal"`) e ativam sutil reação corporal do personagem receptor (`PlayTableAction(seat)`).
   * **Entrada Escalonada na Mão 2D:** No Truco (`TrucoUI.cs`) e no Fodinha (`FodinhaUI.cs`), as cartas entram na mão com um leve atraso sequencial (*stagger* de 0.05s a 0.06s), deslizando suavemente de baixo para cima com escala expansiva amortecida (`Back.Out`). Totalmente compatível com o modo `ReduceMotion`.

4. **Validação de QA e Executável:**
   * **Bateria C# (`GameplayChecks.cs`):** **172 asserções aprovadas com êxito** (+17 novas asserções cobrindo integridade dos novos nós de cenário, presença do lustre, caixotões, credenza e persistência do tema).
   * **Fumaça Visual:** **24 capturas de tela e 20 ações com 0 falhas de layout**.
   * **Executável Exportado:** `Game Hub.exe` e `Game Hub.pck` gerados com sucesso para Windows x86_64.

---

### Patch 13: Menu em Etapas, Cenários 3D Únicos, Visualizador Arkham City, Trajes, Balões de Reação e Tipografia Dinâmica — 06/09/2026

1. **Menu Principal em Etapas & Criação de Partida (`hub/scripts/HubClub.cs`):**
   * Fluxo progressivo (*bottom-up*) de 3 etapas para criar e iniciar partidas:
     * **Etapa 1:** Seleção do Jogo (`Pôquer Roguelike`, `Truco Paulista`, `Fodinha`).
     * **Etapa 2:** Configuração de Parâmetros (Modo de jogo, Oponentes / Bots, Dificuldade, Equipes 1v1 / 2v2 / 3v3).
     * **Etapa 3:** Escolha do Cenário / Salão 3D com cartões interativos dos 4 salões (`classic_club`, `barao_lounge`, `dama_salon`, `cyber_casino`), exibindo miniatura, descrição arquitetônica e botão dourado "Sentar à Mesa →".

2. **Configuração de Câmera POV vs Mesa e Menu de Ajustes In-Game (`SettingsManager.cs`, `TableStage.cs`, `HubMain.cs`):**
   * Configuração `DefaultCameraMode` ("table" ou "pov") salva em `settings.cfg` e ajustável na aba Vídeo.
   * Modal de ajustes in-game em tempo real (`_inGameSettingsModal`) acionado por `Key.Escape` ou botão `"⚙ Ajustes [Esc]"` no HUD da mesa durante partidas de Truco, Pôquer e Fodinha (permite alternar câmera, trocar de salão 3D, regular volumes e ativar redução de movimento sem interromper a partida).

3. **Cenários 3D Arquitetonicamente Únicos (`tools/build_club_rooms.py`):**
   * Cada um dos 4 salões agora possui geometria, iluminação e adereços 3D totalmente exclusivos:
     * `classic_club`: Lareira monumental em tijolo e alvenaria com chamas crepitantes e brasas, estantes de livros arqueadas com volumes coloridos, relógio de carrilhão com pêndulo de latão.
     * `barao_lounge`: Vitrais ogivais góticos azuis com vista para céu noturno e lua cheia, lareira gótica em pedra esculpida com brasão de coruja, pedestais com corujas em pedra, candelabros de ferro forjado e velas púrpuras.
     * `dama_salon`: Estilo Belle Époque, espelhos de chão ovais com molduras em volutas douradas, carrinho móvel de champanhe (*chariot à champagne*) com balde de gelo, taças de cristal e garrafas, pedestais de mármore branco com rosas escarlates.
     * `cyber_casino`: Janela panorâmica futurista com skyline 3D de arranha-céus iluminados e faixas de neon, painéis de fibra de carbono com trilhas em LED ciano/magenta, anel holográfico de cartas pairando no teto.
   * No Pôquer Roguelike, o cenário da mesa agora acompanha a progressão temática do chefe enfrentado (Barão -> `barao_lounge`, Dama -> `dama_salon`, rodadas finais -> `cyber_casino`).

4. **Visualizador 3D de Personagens Estilo *Batman: Arkham City* (`CharacterViewer3D.cs`):**
   * SubViewport 3D interativo com pedestal de exposição circular (*trophy plinth*) com anel emissivo dourado e iluminação cinematográfica de 3 pontos (Key, Fill, Rim Light).
   * Rotação orbital 360° suave por clique e arrasto, zoom suave por scroll do mouse, e gatilhos interativos de animações (`Repouso`, `Desafio & Blefe`, `Comemoração`, `Jogada de Carta`, `Floreio`).
   * Integrado na tela de **Coleção** (Galeria 3D dos 8 personagens e chefes) e na tela de **Ajustes** (aba de Perfil).

5. **Novos Trajes, Reações Emocionais e Batida de Carta Física (`CharacterViewer3D.cs`, `TableStage.cs`, `SettingsManager.cs`):**
   * Seletor de trajes (*Traje Nobre Clássico*, *Alta Noite*, *Clube Vintage Dourado*) com aplicação dinâmica de tinting nos tecidos dos modelos 3D e persistência em `CharacterOutfit`.
   * Balões de reação emocional flutuantes (`ShowReactionBubble`) sobre os assentos dos personagens (Truco, Blefe, Tensão, Vitória) com animação suave de fade e elevação.
   * Batida de manilha e jogadas de peso em `PlayCard` com arco balístico mais alto, velocidade dinâmica e micro-tremor de mesa (*camera shake*) no impacto.

6. **Pôquer Roguelike: Tipografia e Animações Dinâmicas de Cartas / Mãos (`PokerUI.cs`):**
   * Banner comemorativo tipográfico animado (`_handCelebrationBanner`) exibido no centro da tela com *punch-scale* elástico (`0.15 -> 1.35 -> 1.0`), inclinação angular e paletas de cores dinâmicas conforme a raridade da mão (`★ POW! FULL HOUSE! ★`, `⚡ ROYAL FLUSH! ⚡`, `💥 QUADRA! 💥`, etc.).

7. **Validação de QA e Build:**
   * **Bateria C# (`GameplayChecks.cs`):** **171 asserções aprovadas com êxito** (validação dos 4 salões arquitetônicos, do visualizador 3D Arkham City, trajes, câmera padrão e cenários).
   * **Fumaça Visual:** **24 capturas de tela e 20 ações com 0 falhas de layout**.

---

### Patch 14: Correção Anatômica dos Braços 3D, Animação de Jogar Cartas na Mesa, Responsividade Total de Ajustes e Novo Menu Vertical — 06/09/2026

1. **Correção Anatômica das Articulações dos Braços no Blender (`tools/build_blender_cast.py`):**
   * **Causa Raiz Identificada:** No sistema de eixos do Blender, a frente do peito e a mesa apontam para $-Y$, enquanto as costas do personagem apontam para $+Y$. Rotações de ombro/cotovelo positivas em $X$ dobravam os membros em direção às costas (para trás).
   * **Correção:** Conversão de todas as rotações de flexão de braço para valores negativos em $X$ em todos os 7 clipes de animação (`idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`, `play_card`), garantindo articulação natural para a frente em direção à mesa.
   * Todos os 8 modelos `.glb` (`corvo`, `onca`, `dama`, `barao`, `zeca`, `iara`, `bento`, `nina`) e seus portraits foram regerados e reimportados com sucesso.

2. **Reanimação da Jogada de Carta (`play_card`):**
   * Redesenhada a animação de colocar a carta na mesa:
     * **Fase 1 (Frames 1-12):** Elevação da carta e foco visual com rotação sutil de pulso.
     * **Fase 2 (Frames 13-26):** Alcance profundo à frente de 22 cm ($Y = -0.22$, rot $X = -1.15$) estendendo o cotovelo e antebraço rente ao feltro da mesa com batida plana da carta.
     * **Fase 3 (Frames 27-36):** Fixação tátil firme da carta sobre a mesa.
     * **Fase 4 (Frames 37-48):** Retorno fluido do braço à pose de repouso.

3. **Responsividade Total da Tela de Ajustes/Configurações (`hub/scripts/HubMain.cs`):**
   * **Problema Original:** O visualizador 3D e a lista de ajustes empurravam o rodapé com os botões "Voltar" e "Salvar alterações" para fora da viewport em monitores 720p ($Y > 720\text{px}$).
   * **Solução:**
     * O corpo das opções foi encapsulado em um `ScrollContainer` com `SizeFlagsVertical = SizeFlags.ExpandFill`.
     * O container de botões de ação (`actions`) foi movido para um rodapé *sticky* fixo, garantindo que os botões "Voltar" e "Salvar alterações" estejam permanentemente visíveis e clicáveis em qualquer resolução.
     * Otimizada a altura mínima do visualizador 3D na aba de ajustes (190px) e os espaçamentos gerais.

4. **Novo Menu Principal Vertical & Fluxo de Criação (`hub/scripts/HubClub.cs`):**
   * **Lista Vertical Inicial:** A tela inicial do Hub agora apresenta os botões organizados em uma linha vertical clássica e elegante:
     * `Jogar` (botão primário em destaque dourado)
     * `Entrar em sala` (lobby LAN / multiplayer)
     * `Ajustes & Configurações`
     * `Acessibilidade`
     * `Coleção` (Galeria 3D dos personagens)
     * `Créditos`
     * `Sair`
   * **Entrada Direta no Pôquer Roguelike:** Ao selecionar o modo Pôquer Roguelike, o botão de ação dispara imediatamente o início da partida ("Iniciar Pôquer Roguelike →"), sem exigir seleção manual de mapa (já que o mapa do salão 3D se adapta automaticamente a cada chefe).
   * **Partidas de Truco e Fodinha:** Continuam dispondo do fluxo de configuração de bots, regras, equipes (1v1, 2v2, 3v3) e escolha do salão 3D.
   * Adicionado botão `"← Voltar ao Menu"` no topo da tela de criação para retorno imediato ao menu inicial.

5. **Tela de Créditos Integrada (`hub/scripts/HubMain.cs`):**
   * Implementado o painel modal `_creditsPanel` com ficha técnica completa do projeto, direção de arte, engenharia e botão "Voltar".

6. **Validação de QA e Build:**
   * **Bateria C# (`GameplayChecks.cs`):** **171 asserções aprovadas com êxito**.
   * **Fumaça Visual (`tools/visual_smoke.gd`):** **24 capturas de tela e 20 ações com 0 falhas e 0 avisos de layout** (`layout_issues=0, failures=0`).
   * **Executável Atualizado:** `Game Hub.exe` e `Game Hub.pck` exportados com êxito para Windows Desktop.

---

### Patch 15: Menu de Ajustes [Esc] Global, Encaixe Anatômico das Cartas 3D, Mãos na POV e Galeria de Artes Conceituais 360° — 07/09/2026

1. **Menu de Ajustes [Esc] em Camada Global Dedicada (`TableStage.cs`, `PokerUI.cs`):**
   * **Causa Raiz do Bug:** No Pôquer Roguelike, o `TableStage` ficava confinado dentro do container `_tableArea`. Consequentemente, o modal de ajustes `_inGameSettingsModal` era renderizado na camada visual do container da mesa, sendo sobreposto pelas cartas 2D da mão do jogador e rótulos do HUD. Além disso, na loja Balatro e em telas modais, o evento de tecla `Esc` não era repassado ao `TableStage`.
   * **Solução Implementada:**
     * Em `TableStage.cs`, o `_inGameSettingsModal` foi migrado para um `CanvasLayer` independente (`_inGameSettingsCanvas`) com `Layer = 120`. Isso garante que o modal de ajustes fique garantidamente sobreposto a todos os elementos visuais do jogo (HUD, lojas, cartas, avisos e rodapés).
     * Em `PokerUI.cs`, foi implementado `_UnhandledInput` capturando a tecla `Key.Escape`, permitindo abrir e fechar as configurações tanto durante a rodada quanto durante a fase de compras na Loja do Balatro.
     * Captura offscreen `poker-loja-ajustes.png` validou o escurecimento total de fundo e sobreposição completa da interface da loja com botões 100% interativos.

2. **Personagens 3D Segurando Fisicamente as Cartas na Mesa (`build_blender_cast.py`, `TableStage.cs`):**
   * **Problema:** Os braços dos modelos 3D dos oponentes ficavam esticados para cima ou abertos em ângulo obtuso, com as cartas flutuando no ar a uma distância considerável das mãos.
   * **Solução:**
     * Em `tools/build_blender_cast.py`, a pose do ciclo `idle` foi ajustada anatomicamente: ombros posicionados para a frente ($X = -0.42$, $Z = \pm 0.15$), antebraços inclinados em direção ao centro ($X = -0.88$, $Z = \mp 0.12$) e mãos com pulsos flexionados para dentro ($X = -0.18$, $Y = \pm 0.25$, $Z = \mp 0.18$), posicionando os dedos e garras ao redor do leque de cartas.
     * Todos os 8 modelos `.glb` (`nina`, `bento`, `corvo`, `onca`, `iara`, `zeca`, `barao`, `dama`) foram reconstruídos via Blender 5.2.
     * Em `TableStage.cs`, a ancoragem do leque de cartas de cada assento (`_hands[seat]`) foi reposicionada de `0.72m` à frente para `0.44m` à frente e `0.88m` de altura (`pos + toCenter * 0.44f + Vector3(0, 0.88f, 0)`), encaixando o leque diretamente entre as mãos esquerda e direita do personagem.

3. **Ilustração Estilizada de Mãos na Câmera em Primeira Pessoa (POV) (`TrucoUI.cs`, `PokerUI.cs`):**
   * Criada a textura transparente `assets/sprites/ui/pov_hands.png` retratando as mãos do jogador em ilustração *vintage sketch*, posicionadas por baixo das cartas da mão no HUD local.
   * No Truco (`TrucoUI.cs`), a ilustração é ativada dinamicamente quando a perspectiva está em Primeira Pessoa (`TableStage.CameraPerspectiveMode.FirstPersonPov`) e ocultada na visão panorâmica clássica.
   * No Pôquer (`PokerUI.cs`), a ilustração acompanha a área inferior da mão com opacidade sutil de 35%, criando profundidade sem poluir a leitura dos valores das cartas.
   * Para os oponentes e visões externas da mesa, o modelo 3D físico do personagem continua realizando todas as animações normalmente.

4. **Artes Conceituais e Pranchas 360° de Corpo Inteiro (`assets/sprites/concept/`, `HubMain.cs`):**
   * **Geração e Processamento:**
     * Geradas ilustrações conceituais de corpo inteiro de alta fidelidade para todos os 8 personagens (`nina`, `bento`, `corvo`, `onca`, `iara`, `zeca`, `barao`, `dama`), expandindo a arte dos bustos originais com vestimentas vitorianas completas, pernas, calças sob medida, botas, caudas, garras e detalhes anatômicos.
     * Script `tools/process_concept_assets.py` estruturou as pranchas completas (`{name}_sheet.png`) e fatiou os 4 ângulos ortogonais/perspectivos (`0_front.png`, `1_three_quarter.png`, `2_side.png`, `3_back.png`) na pasta `assets/sprites/concept/`.
   * **Galeria Dual na Tela de Coleção:**
     * A tela de **Coleção** do Hub agora apresenta um seletor superior de modos:
       * `🎮 Modelos 3D (Troféus)`: Visualizador 3D com órbita, zoom e animações.
       * `🎨 Artes Conceituais 360°`: Galeria de pranchas conceituais dos personagens em corpo inteiro.
     * Seletor de ângulos interativo: `Frente (0°)`, `3/4 Frontal (45°)`, `Perfil (90°)`, `Costas (180°)`, `Prancha 360°`.
     * Fichas detalhadas de figurino sob medida, tecidos e *lore* para cada personagem.

5. **Validação de QA e Executável:**
   * **Compilação C# (.NET 8):** 0 erros, 0 avisos.
   * **Bateria Automatizada (`GameplayChecks.cs`):** **171 asserções aprovadas com êxito**.
   * **Fumaça Visual (`visual_smoke.gd`):** **27 capturas de tela e 25 ações interativas** com **0 falhas e 0 problemas de layout**.
   * **Executável Atualizado:** `Game Hub.exe` e `Game Hub.pck` gerados com sucesso para Windows Desktop x86_64.

---

## 3. Guia de Operações e Comandos Essenciais

Para qualquer IA ou desenvolvedor executando tarefas neste projeto, utilize sempre os comandos abaixo:

### Compilar a Solução C#:
```powershell
$env:DOTNET_ROOT = "C:\Users\Lucas\AppData\Local\Temp\multigame-tools\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet build GameHub.csproj
```

### Regerar os Cenários das Salas no Blender:
```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/build_club_rooms.py
```

### Regerar os Modelos 3D dos Personagens e Animações no Blender:
```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/build_blender_cast.py
```

### Recompor a Folha de Sprites 16-bit (8 Personagens):
```powershell
python tools/assemble_cast_sprites.py
```

### Reimportar Recursos no Godot (Headless):
```powershell
& "C:\Users\Lucas\AppData\Local\Temp\multigame-tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --editor --quit
```

### Executar a Suíte Completa de Testes de Fumaça e Visual QA:
```powershell
$env:DOTNET_ROOT = "C:\Users\Lucas\AppData\Local\Temp\multigame-tools\dotnet"
$env:GODOT_BIN = "C:\Users\Lucas\AppData\Local\Temp\multigame-tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
& powershell -ExecutionPolicy Bypass -File tools\visual_smoke.ps1 -AllowLayoutWarnings
```
> **Nota de QA atualizada pelo Patch 8:** A suíte valida 71 asserções em `tools/GameplayChecks.cs`, incluindo materiais, poses, cinco clipes por modelo, integridade do baralho e turnos solo 2v2/3v3. A fumaça visual também exercita 1v1 e pôquer, com 20 capturas em `docs/screenshots/`.

---

## 4. Regras de Ouro e Diretrizes para Modificações Futuras

1. **Nunca Reintroduzir Cartas 2D Sobrepostas ao Feltro 3D:**
   * No Truco, a mesa (`tableSurface`) deve permanecer limpa. As cartas jogadas são objetos 3D físicos gerenciados por `_stage.PlayCard()`. Overlays 2D devem se limitar ao HUD flutuante no topo ou à mão do jogador na parte inferior.
2. **Preservar a Coaxialidade no Assento 0 (`TableStage.cs`):**
   * O personagem, a poltrona e o leque de cartas na mão devem sempre compartilhar a mesma rotação `rotY` e estar alinhados ao eixo da mesa ($X = 0$). Nunca aplique multiplicadores arbitrários na rotação do Seat 0.
3. **Contrato de Animações nos Modelos GLB:**
   * Qualquer alteração no script de personagens deve obrigatoriamente manter os 5 clipes: `entrance`, `truco`, `victory`, `boss_intro`, `flourish`. A suíte de testes falhará imediatamente se algum desses clipes for omitido.
4. **Resoluções e Acessibilidade:**
   * Todas as telas e janelas de jogo devem respeitar as preferências do `SettingsManager` (como `ReduceMotion`) e se ajustar perfeitamente de 720p até 4K.
5. **Atualização Contínua deste Documento:**
   * Sempre que você (IA ou desenvolvedor) implementar novas funcionalidades (por exemplo, a lógica de rede multiplayer, novo modo Fodinha, ou habilidades exclusivas de bosses), **adicione uma nova seção de Patch neste arquivo** para manter o histórico unificado.

---

## 19. Patch 19 — Calibração de Ritmo em 2v2/3v3 e Rigging/Animações Anatômicas Fluidas (Sem Deformações)

### Problemas Solucionados
1. **Ritmo Acelerado nos Modos Dupla (2v2) e Trio (3v3):**
   - No Truco, a distribuição de 12 e 18 cartas ocorria em intervalo de 45ms, parecendo uma metralhadora.
   - Após a distribuição e revelação do Vira, o primeiro assento jogava instantaneamente sem qualquer pausa de leitura.
   - A decisão de oferecer e aceitar a pena durava menos de 2 segundos no total (`_aiPenaTimer` de 0.75s, decisão em 0.9s e resolução em 0.45s).
   - Entre os tombos (vazas), as cartas jogadas permaneciam na mesa e o próximo tombo jogava cartas exatamente por cima das anteriores, causando sobreposição visual e sensação caótica.
2. **Animações "Meio Bugadas" com Deformações na Malha:**
   - Em `tools/build_rigged_detailed_cast.py`, o osso filho `Chest` estava sofrendo translação relativa de até `-38cm` em `truco` e `-28cm` em `play_card`, rasgando os polígonos entre o abdômen e as costelas.
   - O osso `Root` estava com `use_deform = True`, prendendo vértices do chão e esticando os pés quando o quadril se movia.
   - As clavículas (`Shoulder.L` e `Shoulder.R`) não possuíam rotação nos movimentos de braço levantado (`victory`, `truco`), provocando colapso das axilas ("candy-wrapper").
   - Em `boss_intro`, rotações extremas dos antebraços faziam as mãos penetrarem o tórax.

### Modificações Técnicas
1. **Ritmo e Cadência (`TrucoGameManager.cs`, `TableStage.cs`, `TrucoUI.cs`):**
   - `TableStage.cs`: Intervalo de distribuição ajustado de `emitted++ * .045f` para `emitted++ * .13f`, gerando distribuição rítmica, realista e audível.
   - `TrucoGameManager.cs`:
     - Pausa de corte: `0.90f` (era 0.45f).
     - Reflexão da IA para oferecer pena: `1.80f` (era 0.75f).
     - Reflexão do bot para decidir ficar com a pena: `1.80f` (era 0.90f).
     - Exibição do resultado da pena: `1.30f` (era 0.45f).
     - Pausa de observação pós-distribuição: `1.50f` após revelar o Vira e definir a Manilha antes do início das jogadas.
     - Observação da carta descida: `1.50f` (era 1.30f).
     - Intervalo entre tombos: `2.10f` (era 1.85f).
   - `TrucoUI.cs`: Em `OnRoundResolved`, acionado `_ = _stage?.CollectRoundCardsToDiscard();`, reunindo suavemente as cartas da vaza no centro da mesa e deslizando-as para a pilha de descarte antes do início do próximo tombo.
2. **Rigging e Animações Anatômicas (`tools/build_rigged_detailed_cast.py`):**
   - `add_bone`: Adicionado parâmetro `deform = True`, com `root = add_bone("Root", ..., deform=False)` garantindo que o osso raiz não capture pesos de vértices da malha.
   - Eliminadas 100% das translações do osso filho `Chest` (`key_loc("Chest")`) em todos os 7 clipes (`idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish`, `play_card`). A inclinação e projeção do tronco agora é puramente angular via flexão coordenada de `Spine` e `Chest`.
   - Adicionadas rotações naturais de clavícula (`Shoulder.L` e `Shoulder.R`) para sustentar braços levantados em `victory`, batida na mesa em `truco`, extensão ao feltro em `play_card` e postura aristocrática em `boss_intro`.
   - Regenerados todos os 5 modelos GLB (`corvo.glb`, `barao.glb`, `dama.glb`, `zeca.glb`, `iara.glb`) e seus retratos de estúdio em Blender 5.2.
3. **Validação e Export:**
   - `dotnet build GameHub.csproj`: Compilado com êxito (0 erros, 0 avisos).
   - `GameplayChecks.cs`: 171 asserções válidas (PASS).
   - `visual_smoke.ps1`: 27 capturas realizadas, 25 ações, 0 falhas, 0 avisos de layout (PASS).
   - Binário final reexportado: `Game Hub.exe` (103 MB) e `Game Hub.pck` (319 MB).

---

## 20. Patch 20 — Sincronização de ritmo entre cartas, regras e entrada — 07/09/2026

### Correções

- A mesa passou a concentrar a cadência de distribuição em um único contrato: cada carta sai a cada 180 ms, leva 420 ms no voo e recebe tempo de assentamento. Truco e Fodinha consultam essa duração, portanto não iniciam a vira, palpite ou próxima ação com cartas ainda no ar.
- No Fodinha, a distribuição de mãos grandes agora espera o último voo terminar. Palpites das IAs, corte, avanço de vaza e jogadas receberam pausas de leitura; isso elimina a sequência acelerada e as cartas se sobrepondo visualmente.
- No pôquer, oito cartas entram em cadência de 120 ms e 420 ms de voo. Os botões de selecionar, descartar e jogar ficam indisponíveis até a última carta chegar à mão.
- A opção de acessibilidade foi renomeada para esclarecer seu efeito: reduzir animações também encurta esperas. Com ela desligada, o ritmo normal permanece ativo.

### Modelos e testes

- O passe anatômico dos cinco modelos detalhados foi preservado: raiz sem deformação, tronco sem translações agressivas, clavículas ativas e sete clipes Bezier. Nina, Bento e Onça continuam com seus modelos preservados para um passe dedicado posterior.
- `GameplayChecks.cs` agora verifica que todos os oito personagens mantêm os sete clipes, duração de leitura entre 0,5 e 5 segundos e velocidade de reprodução normal.
- Compilação .NET: 0 erros e 0 avisos. QA de regras e contratos: 235 asserções aprovadas, incluindo rodízio, distribuição, Pena, integridade de baralho e duração dos clipes.
- `tools/visual_smoke.gd` passou a aguardar o fim real de distribuições e o desbloqueio da mão, evitando que a automação de QA esconda erros de ritmo.

---

## 21. Patch 21 — Braços da Vitória Anatômicos, Marcenaria da Estante Aberta, Ritmo Deliberado das IAs e Cutscenes Cinemáticas 3D — 07/09/2026

### 1. Correção Anatômica da Vitória (`tools/build_rigged_detailed_cast.py` & Modelos GLB)
* **Causa Raiz:** No sistema local dos ossos `UpperArm.L` e `UpperArm.R` (cabeça no ombro e cauda apontando para o cotovelo), rotação positiva em X projetava o antebraço e a mão para trás no espaço de mundo (+Y), gerando o efeito invertido onde o personagem jogava os braços para trás das costas em vez de comemorar.
* **Correção:**
  * Rotação invertida para X negativo (`-1.85 rad`) em coordenação com elevação das clavículas (`Shoulder.L/R`), curvatura suave dos antebraços (`Forearm.L/R`), elevação do queixo (`Head` +0.24 rad) e peito estufado triunfante (`Chest` -0.18 rad).
  * O personagem agora ergue os dois braços em "V" vitorioso para o alto e para a frente com punhos/palmas estendidas.
  * Regenerados todos os 5 modelos rigged (`corvo.glb`, `barao.glb`, `dama.glb`, `zeca.glb`, `iara.glb`) e seus retratos de estúdio em Blender 5.2.

### 2. Marcenaria da Estante de Livros do Salão Clássico (`tools/build_club_rooms.py` & `room_classic_club.glb`)
* **Causa Raiz do Z-Fighting:** No Salão Clássico, `Bookcase_{-4.8}` e `Bookcase_{4.8}` haviam sido gerados como cubos sólidos maciços de madeira de 0.55m de profundidade. As prateleiras e as capas dos 56 livros coloridos ocupavam exatamente o mesmo plano de profundidade (`wall_y - 0.425`), causando cintilação e conflito poligonal na face frontal.
* **Solução:** Reconstruída como marcenaria de luxo aberta:
  * Painel traseiro fino (`bs_back`) encostado na parede.
  * Colunas laterais estruturais (`bs_left`, `bs_right`) de 12cm de espessura.
  * Rodapé esculpido e frontão superior arqueado clássico.
  * Frente 100% aberta com prateleiras e livros em recesso e sombras profundas, eliminando 100% do z-fighting.

### 3. Calibração do Ritmo e Pensamento das IAs (`TrucoGameManager.cs` e `FodinhaUI.cs`)
* **Eliminação do Truco Robótico na Rodada 0:** A IA agora só pede Truco se já houver disputa real na mesa (cartas já jogadas) ou a partir da Rodada 1. O Truco às cegas na primeira fração de segundo da mão foi 100% extinto.
* **Intervalo de Reflexão Realista no Truco:** `_aiThinkTimer` ajustado para `2.2s a 3.6s` com status visível no HUD (`"{Nome} pensando..."`). Adicionada pausa de `1.85s` após abrir o Vira para o jogador poder contemplar a mão e a manilha. Batida de Truco na mesa agora conta com antecipação dramática de `1.35s` antes do banner surgir.
* **Cadência Humana no Fodinha:** Cada bot agora possui um estado deliberado de reflexão: `1.90s` antes de declarar palpite (com status `"{Nome} analisando as cartas para o palpite…"`) e `2.20s` antes de escolher a jogada (com status `"{Nome} calculando a jogada…"`). Pausa pós-distribuição ajustada para `2.00s`.

### 4. Cutscenes Cinemáticas 3D com Múltiplos Ângulos (`core/visuals/TableStage.cs`)
* **Direção de Cena:** Criada sequência cinemática em 3 tomadas integradas ao `TableStage`:
  1. **Plano 1 (Grua Ampla):** Câmera sobrevoa o lustre do salão em perspectiva suave (50° FOV) descendo de `(0, 6.4, 4.4)` para `(0, 3.8, 3.2)` em 1.7s, apresentando a mesa e o relógio de pêndulo com cartela dourada de apresentação.
  2. **Plano 2 (Contra-plongée do Rival / Chefe):** Câmera foca em ângulo baixo dramático (`Fov = 42°`) no adversário principal (Seu Corvo / Barão), que executa `boss_intro` ou `entrance` enquanto uma cartela com moldura dourada estampa seu nome e descrição aristocrática.
  3. **Plano 3 (Travelling para o Assento 0):** Câmera desliza suavemente em arco até a posição exata dos olhos do jogador (`eyePos`), entregando o controle suavemente para a distribuição de cartas.
* **Letterbox Anamórfico 2.35:1:** Barras pretas cinematográficas com transição suave.
* **Botão [Pular - ESC]:** Botão no canto superior direito e manipulador de teclado (`ESC` / `Espaço`) permitindo cancelar a cutscene instantaneamente a qualquer momento sem travar o jogo.
* **Respeito a `ReduceMotion`:** Pula instantaneamente as cutscenes para jogadores com sensibilidade a movimento ou testes unitários automatizados.

### 5. Validação e Binários
* `GameplayChecks.cs`: **235 asserções aprovadas com êxito** (PASS).
* `visual_smoke.ps1`: **27 capturas visuais, 25 ações, 0 falhas, 0 problemas de layout** (PASS).
* Binários finais reexportados: `Game Hub.exe` (103 MB) e `Game Hub.pck` (300 MB).

---

## 22. Patch 24 — Redesign Panorâmico de Pôquer, Postura Sentada, Novos Modelos 3D (Aki, Morgana, Carniçal), Mascote Corvo e Upgrade Visual — 07/09/2026

### 1. Redesign Panorâmico do Pôquer Roguelike (`games/poker_roguelike/scripts/PokerUI.cs`)
* **Eliminação de Letterbox Escuro e Bandeja Pesada:** O palco 3D (`TableStage`) agora preenche 100% da tela em modo panorâmico (`FullRect`).
* **Mão Flutuante e Limpa:** As cartas do jogador flutuam na parte inferior da tela sem bandejas ou placas escuras sobrepostas, valorizando o feltro verde da mesa e a atmosfera do clube.
* **Painel Unificado Lateral à Direita ("Quadradão pro Lado"):** Status consolidado da corrida (rodada atual, pontos conquistados, meta da mesa, barra de progresso dourada, mãos e descartes restantes, fichas acumuladas, relíquias ativas), avaliação de combinações em tempo real e botões de ação ("Jogar mão", "Descartar", "Voltar ao clube").
* **Painel do Chefe no Topo Esquerdo:** Cartela compacta com nome do boss, retrato 3D de estúdio e cartas da banca.
* **Resolução de Bounds e Âncoras:** Âncoras e margens calibradas milimetricamente para que nenhum elemento ultrapasse os limites da tela (0 layout issues).

### 2. Postura Sentada Relaxada de Todo o Elenco (`TableStage.cs`, `tools/build_rigged_detailed_cast.py` & `tools/build_blender_cast.py`)
* **Anatomia Sentada na Poltrona:** Pélvis rebaixada para 0.52m (altura da almofada da poltrona), coxas a -88° e canelas a +85°. Todos os personagens agora permanecem naturalmente sentados durante o jogo.
* **Encaixe no Estofado:** Alinhamento fino em `TableStage.cs`: poltrona recuada (`pos - toCenter * 0.12f`) e personagem posicionado no centro do estofado (`pos - toCenter * 0.04f`).

### 3. Integração dos Novos Modelos 3D (`assets/modelos 3d detalhados/`)
* **Mascote Corvo ("Edgar" / "Corvinho") (`mascot_crow.glb`):** Exportado do arquivo `crowrigconjay.blend` com animação idle de observação e respiração, materiais e texturas vinculadas, empoleirado no topo do relógio de salão do Classic Club.
* **Nova Personagem Jogável: Aki (`aki.glb`, `aki_3d.png`):** "A estrategista misteriosa · precisão afiada a cada jogada." Rig padrão de 25 ossos, 39 animações completas com postura sentada, materiais PBR. Total de jogáveis elevado para 7.
* **Nova Boss: Madame Morgana, a Bruxa (`morgana.glb`, `morgana_3d.png`):** "Boss exclusivo · feitiços sombrios e apostas fatais." Rig padrão de 25 ossos e 39 animações completas.
* **Novo Boss: Lorde Carniçal (`carnical.glb`, `carnical_3d.png`):** "Boss exclusivo · a fome insaciável pelas suas fichas." Rig padrão de 25 ossos e 39 animações completas.
* **Rodízio de 4 Chefes nas 8 Rodadas do Pôquer:**
  - Rodadas 1–2: Barão da Meia-Noite (Lounge Gótico, trilha *midnight-baron*)
  - Rodadas 3–4: Dama de Copas (Salão Belle Époque, trilha *velvet-table*)
  - Rodadas 5–6: Madame Morgana (Salão Clássico, trilha *saloon-swing*)
  - Rodadas 7–8: Lorde Carniçal (Cyber Casino, trilha *cyber-tango*)

### 4. Upgrade de Fidelidade: Nina, Bento e Dona Onça
* Regenerados com geometria de pernas sentadas a 90° e retratos de estúdio em alta definição (`nina_3d.png`, `bento_3d.png`, `onca_3d.png`), equiparando a qualidade com o restante do elenco.

### 5. Coleção do Clube e Histórias Conceituais (`hub/scripts/HubMain.cs`)
* Painel da Coleção atualizado com suporte aos 11 personagens em grid de 2 colunas com recorte de texto (`ClipText`) e rolagem automática (`ScrollContainer`), evitando qualquer transbordamento de viewport.
* Adicionadas histórias conceituais de figurino detalhadas para Aki, Madame Morgana e Lorde Carniçal.

### 6. Validação e Binários Finais
* **Compilação C#:** 0 erros, 0 avisos.
* **QA de Gameplay (`GameplayChecks.cs`):** **336 asserções aprovadas** (`GAMEPLAY_QA PASS`).
* **QA Visual (`visual_smoke.ps1`):** **27 screenshots capturadas**, 25 ações, 0 falhas, 0 problemas de layout (`VISUAL_QA PASS`).
* **QA de Câmera (`visual_smoke.ps1 -CameraOnly`):** **Aprovado com êxito** (`CAMERA_QA PASS []`).
* **Executável Windows:** `Game Hub.exe` (103 MB) exportado e pronto para execução.
