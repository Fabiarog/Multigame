# Prompt de continuidade — MultiGame

> Documento criado primeiro, a pedido de Lucas, em 06/09/2026, antes de iniciar a nova etapa de câmera. Leia este arquivo e `AI_DEV_PATCH_NOTES.md`, confira o estado real do Git e continue o trabalho pendente. Não recomece a repaginada nem descarte mudanças existentes.

## Pedido vigente do usuário

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
