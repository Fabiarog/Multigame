# MultiGame — Clube de cartas

Pôquer roguelike e truco em Godot .NET/C#, com mesas em **2.5D**, personagens pixel art e uma identidade de clube brasileiro: feltro verde, papel creme e detalhes de latão.

![Abertura do clube](docs/screenshots/abertura.png)

## O que está jogável

- **Pôquer roguelike solo:** seleção de cartas, prévia de pontuação, descartes, metas por rodada, relíquias, loja e tutorial. A apresentação dos rivais usa o novo elenco.
- **Truco solo:** 1×1, 2×2 e 3×3 com os demais lugares controlados por IA. Cada participante joga sua própria mão; o resultado do tombo considera a melhor carta de cada equipe.
- **Pena nas equipes:** o aliado que recebe a carta decide se fica com ela. A IA resolve sua própria decisão no solo; o jogador local recebe os botões quando é o destinatário.
- **Apresentação:** mesa 3D com personagens 2D, leques de cartas ocultas e animações de embaralhar, cortar, entregar a pena, distribuir, jogar e recolher no truco. O pôquer anima distribuição, seleção e pontuação.
- **Elenco:** Nina, Bento, Seu Corvo e Dona Onça, com duas poses cada; seleção de personagem nos ajustes. Nesta versão, a escolha é cosmética.
- **Áudio:** três loops originais sintetizados, efeitos de cartas e transições entre trilhas; volumes de música/efeitos e seleção de faixa.
- **Ajustes:** perfil, áudio, opções visuais e acessibilidade, incluindo redução de movimento e modos de cor.

## Limites atuais

**O multiplayer de partida ainda precisa de implementação.** A estrutura anterior de rede/lobby LAN foi preservada, mas não sincroniza turnos, baralho nem decisões entre computadores. Portanto, a decisão de pena pelo aliado humano remoto é uma entrega futura, detalhada no plano. Não considere o botão LAN uma partida multiplayer completa.

**Fodinha** aparece como “em breve”, sem regras implementadas. A progressão roguelike completa do truco, habilidades exclusivas dos novos personagens e novos bosses mecânicos também estão no plano. Os novos retratos não representam novas habilidades de boss já prontas.

## Executar

Versões verificadas nesta revisão: **Godot 4.7.2 .NET** e **SDK .NET 8.0.424**. O SDK do projeto usa `Godot.NET.Sdk/4.7.2` e `net8.0`.

1. Instale o Godot **.NET** e o SDK .NET 8.
2. Importe `project.godot` no editor, compile C# e pressione **F5**.

No PowerShell, também é possível usar o launcher:

```powershell
$env:DOTNET_ROOT = 'C:\caminho\para\dotnet'
$env:GODOT_BIN = 'C:\caminho\para\Godot_v4.7.2-stable_mono_win64_console.exe'
.\tools\play.ps1
```

O launcher também encontra o ambiente temporário preparado neste PC enquanto ele existir em `%TEMP%\multigame-tools`. Para uso permanente, configure os caminhos acima. O executável antigo que já estava no repositório não foi reexportado; use o projeto fonte para ver esta revisão.

O identificador interno `Game Hub` foi mantido para preservar o caminho dos ajustes existentes. O título visível foi atualizado para MultiGame.

## Verificação

Com as mesmas variáveis de ambiente configuradas:

```powershell
.\tools\visual_smoke.ps1
```

O script compila C#, importa recursos, verifica duplas/trios e captura o renderizador real em uma janela fora da tela. Os saves de teste ficam em um `APPDATA` temporário isolado. Imagens em `docs/screenshots`; relatório JSON e logs no diretório temporário informado ao final. O teste falha em exceções de runtime, ações esperadas ausentes ou elementos de interface fora dos limites.

## Organização e próximos passos

| Caminho | Responsabilidade |
| --- | --- |
| `core/visuals` | Tema, cartas, personagens e mesa 2.5D compartilhados |
| `core/systems` | Preferências, áudio e persistência existente |
| `core/networking` | Estrutura de lobby/rede a completar |
| `hub` | Abertura, coleção, ajustes e lobby |
| `games/poker_roguelike` | Corrida de pôquer, pontuação e loja |
| `games/truco` | Regras, turnos por lugar e apresentação do truco |
| `assets` | Acervo preservado e novos recursos visuais/sonoros |
| `tools` | Launcher, geração de áudio e verificações |

- [Plano completo: implementado, pendências, critérios e perguntas](docs/plano-repaginacao-clube.md).
- [Origem das artes, prompts, áudio e licenças de fontes](assets/art-provenance.md).
- [Plano anterior de personagens/cartas/truco](docs/plano-implementacao-personagens-cartas-truco.md), mantido como histórico.

O estado anterior foi preservado na branch **Backup** antes das alterações. Esta repaginada é entregue em **review**, mantendo **main** como estava.
