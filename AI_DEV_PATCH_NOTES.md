# MULTIGAME — LOG DE ATUALIZAÇÕES, ARQUITETURA E GUIA DE DESENVOLVIMENTO
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
  * Descoberta local por broadcast UDP na porta `42424`.

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
