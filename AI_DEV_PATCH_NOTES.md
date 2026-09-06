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
> **Nota de QA:** A suíte valida 42 asserções críticas em `tools/GameplayChecks.cs` (incluindo integridade de baralho, turnos de truco 1v1, 2v2 e 3v3, e garantia de que todos os modelos exportam os 5 clipes de animação) e captura 20 screenshots em `docs/screenshots/`.

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
