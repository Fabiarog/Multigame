# Plano de implementação — MultiGame / Clube de cartas

Revisão: 5 de setembro de 2026. Documento de entrega e continuação; “implementado” indica código e recursos presentes nesta revisão, não apenas intenção.

## Direção escolhida

Clube brasileiro de cartas com feltro verde, madeira escura, papel creme e latão. Tipografia editorial nos títulos, interface simples nas ações e números bem legíveis. A personalidade vem do elenco pixel art, dos gestos de mesa e do som. A referência de gênero é Balatro, mas a marca, o desenho das cartas e os personagens são próprios desta revisão.

A dinâmica **2.5D foi preservada nas partidas de pôquer e truco**: mesa, luzes, câmera e leques existem em 3D; personagens são sprites voltados para a câmera. A mão jogável e os indicadores ficam em uma camada de interface para manter a leitura. O hub usa a ilustração estática para apresentar o clube.

Decisões já confirmadas pelo usuário: preservar 2.5D; animar baralho, corte, entrega e jogadas; manter cartas visíveis nas mãos dos oponentes pelo verso; deixar a IA aliada decidir sua própria pena no solo; no multiplayer, delegar essa decisão ao aliado humano destinatário; ampliar personagens e música. Essas decisões não precisam ser reconfirmadas.

## Auditoria do acervo

| Recurso anterior | Diagnóstico e uso nesta entrega |
| --- | --- |
| `backgrounds/cyber_casino.jpg`, `neon_lounge.jpg`, `retro_arcade.jpg` | Bons cenários pixel art; preservados como acervo. A mesa principal ganhou composição unificada própria. |
| `cards/cards_spritesheet.jpg` | Arte com repetições/inconsistências de valores e fundo verde. Faces jogáveis substituídas por desenho vetorial orientado ao valor real da carta. |
| Avatares JPG de aranha/tartaruga | Quadriculado incorporado não equivale a transparência. Preservados; novos personagens têm alfa real. |
| Spritesheets de aranha/tartaruga | Acervo mantido; não foram apagadas as classes antigas de comportamento. |
| Grades de base/cabelo/calça/camisa | Contêm composições inteiras em JPG; ainda não constituem camadas de roupa intercambiáveis. Seletores antigos foram retirados da tela de perfil, preservando dados compatíveis. |
| Retrato de truco e amostras antigas | Preservados; a coleção continua apresentando parte do acervo. |

Nenhuma licença do acervo anterior foi presumida. Os recursos novos e as licenças das fontes estão em [art-provenance.md](../assets/art-provenance.md).

## Entregue nesta revisão

| Área | Implementação concreta |
| --- | --- |
| Identidade | Paleta verde/creme/latão, marca de espadas, DM Sans, Cormorant Garamond e DM Mono. Tema compartilhado com foco de teclado visível. |
| Abertura | Nova composição, ilustração de mesa, escolha de jogo e configuração da partida, navegação para coleção/ajustes/acessibilidade/LAN, Fodinha desabilitado como futuro módulo. |
| Mesa 2.5D | `TableStage.cs`: mesa oval, borda de madeira, detalhe de latão, luz quente, câmera ortográfica, personagens, baralho físico e versos nas mãos. Lugares para 2, 4 ou 6 participantes. |
| Cartas | `PlayingCard.cs`: valor e naipe corretos, verso próprio, seleção e marcação de manilha distintas; naipes desenhados sem depender de glifos da fonte. |
| Pôquer | Nova lateral de metas/recursos, mão e prévia de pontos, rival com retrato, tutorial, resultados e loja com o mesmo tema. Bloqueio de input durante pontuação. |
| Truco | Placar, vira/manilha, histórico de tombos, mão, botões de truco e diálogos de pena/resultados redesenhados. |
| Equipes solo | Todos os lugares de 2×2 e 3×3 possuem mão e turno; melhor carta por equipe determina o tombo; IA poupa cartas quando o aliado já vence. |
| Pena solo | Destinatário segue o aliado do distribuidor; IA escolhe guardar ou tornar tombo. Quando o destinatário é o jogador local, a interface espera a escolha real. Proteções contra corte e resolução duplicados. |
| Elenco | Nina, Bento, Seu Corvo e Dona Onça; duas poses por personagem, seleção de avatar e apresentação de rivais. Cosmético nesta etapa. |
| Áudio | Três loops originais: “Depois da meia-noite”, “Mesa de veludo” e “Última manilha”; efeitos sintetizados de cartas, seleção, pontuação, vitória e truco. Troca de trilha com transição de volume. |
| Preferências | Personagem e trilha persistidos, controles de volume operantes, redução de movimento respeitada nas principais animações e melhor contraste nos naipes alternativos. |
| Ferramentas | Launcher PowerShell, gerador de áudio reproduzível, integração de regras e capturas do renderizador com dados de usuário isolados. |

### Cobertura de animação

| Ação | Estado |
| --- | --- |
| Abertura de menus | Transição breve de opacidade; foco e interação dos botões |
| Mesa e personagens | Movimento ambiente discreto da câmera e personagens; troca de pose e reação à jogada |
| Embaralhar/cortar no truco | Pilhas móveis, cartas se juntando e separação do corte |
| Entrega e resolução da pena | Carta vai do baralho ao lugar destinatário e depois à mão ou ao tombo |
| Distribuição no truco | Cartas saem da posição projetada do baralho em direção a cada lugar |
| Jogada do oponente/aliado | Carta parte da mão do participante que jogou; contagem do leque é atualizada |
| Fim da mão no truco | Recolhimento e passagem para o próximo distribuidor |
| Distribuição no pôquer | Versos voam da mesa até a mão, com entrada em sequência |
| Seleção e resultado no pôquer | Realce de seleção, pontuação e apresentação de resultado |

“Animação para tudo” orienta o restante do projeto: esta entrega cobre o ciclo principal das cartas, mas não inclui animação quadro a quadro exclusiva para cada personagem, todos os efeitos possíveis de relíquia ou cinematográficas de boss. A redução de movimento simplifica o deslocamento sem mudar regras e decisões.

### Conteúdo sonoro

Os loops têm aproximadamente 24 s, 21 s e 18 s, com instrumentos sintetizados e sem samples externos. São três bases musicais funcionais para esta versão, não uma trilha final extensa. O script permite gerar novamente os WAVs. A evolução musical está na etapa 4.

## Verificação e limites

- Compilação com Godot .NET 4.7.2 / SDK .NET 8.0.424, sem avisos de C# nem erros.
- Resultado da execução final: **22 verificações de regras aprovadas; 17 capturas; 18 ações de interface; zero falhas e zero elementos fora dos limites**. Houve aviso residual de `ObjectDB` ao encerrar o renderizador, sem erro de runtime.
- Integração de truco: mãos únicas, vira fora das mãos, participação de todos os lugares em duplas/trios, rotação de distribuidor, pena automática da IA e decisão explícita do jogador local.
- Testes visuais: abertura, coleção, ajustes, acessibilidade, entrada de sala, pôquer e truco; seleção/descarte/jogada/corte; mesas em equipe e redimensionamento do hub em 1280×720, 1600×900 e 1024×768.
- Capturas reais disponíveis em [screenshots](screenshots). O launcher informa relatório JSON e logs da execução. As verificações não equivalem a partidas multiplayer, cobertura de todas as combinações de regras ou validação em outros sistemas operacionais.
- O motor original já apresentava aviso de instâncias `ObjectDB` no encerramento. A ferramenta acusa erros de runtime; o aviso residual de limpeza deve continuar sendo acompanhado separadamente de falhas funcionais.

O multiplayer existente contém infraestrutura de lobby, mas **não sincroniza a partida**. A interface agora informa isso. O pedido de pena pelo aliado humano remoto não está concluído nesta revisão: exige o modelo de autoridade da etapa 1. Fodinha continua sem implementação. Novos personagens não ganharam habilidades exclusivas nem novos bosses mecânicos.

## Etapa 1 — Multiplayer de partida e pena por destinatário

Prioridade mais alta, antes de prometer partida LAN jogável.

1. Definir host autoritativo, mapeamento estável `peer → lugar → equipe`, comandos com identificador de mão/turno e snapshots de reconexão.
2. Manter embaralhamento e estado das mãos no host. Distribuir a cada cliente apenas sua mão e informações públicas; os demais recebem contagens/versos.
3. Sincronizar corte, oferta de pena, recebimento, decisão, distribuição, jogadas, truco, resultado e troca de distribuidor. O host valida fase e autoria de todo comando.
4. Enviar a decisão de pena exclusivamente ao **aliado humano que recebeu a carta**. Os outros participantes aguardam. A escolha do host não substitui a escolha do aliado. Se o destinatário for bot, o host executa sua IA.
5. Separar tempo de animação da autoridade das regras, para impedir que latência ou redução de movimento alterem turnos.
6. Definir saída/reconexão: reserva de lugar, prazo e política de substituição por IA. Exibir estado de conexão e feedback de comandos recusados.

Critério de conclusão: testes com 2, 4 e 6 clientes; mesma sequência pública em todos; mãos alheias não transmitidas; pena só aceita do destinatário; comandos repetidos/atrasados recusados; desconexão não trava a partida. Fazer captura com duas janelas reais além dos testes de protocolo.

## Etapa 2 — Roguelike, personagens e bosses

1. Extrair conteúdo para catálogos de dados: identidade, retratos, falas, raridade, passivas e efeitos por modo.
2. Criar corrida de truco com encontros, recompensas, risco, economia e bosses, reutilizando interfaces compartilhadas sem misturar regras de pôquer.
3. Dar habilidades distintas aos quatro personagens, com descrição visível antes da partida e log de efeito durante a jogada.
4. Adicionar bosses mecânicos com gatilhos explícitos, pistas visuais e limites de balanceamento; o elenco visual atual pode inspirá-los, mas não fixa poderes ainda.
5. Implementar salvamento de progresso/desbloqueios com versão de dados e migração.

Critério: uma corrida completa por modo, recompensas persistidas, efeitos determinísticos no host e testes focados em combinações de habilidade que alteram regras. Não chamar apenas troca de retrato de “boss novo”.

## Etapa 3 — Fodinha

Módulo próprio registrado ao lado de pôquer e truco. Reutilizar mesa, áudio, personagens, carta e infraestrutura de turnos; criar regras independentes para palpites e vazas.

Especificar antes de codificar: baralho e manilhas; quantidade de cartas por rodada; ordem de palpite; restrição ou não à soma dos palpites; necessidade de seguir naipe; desempate; vidas/pontos; eliminação e condição de vitória.

Fluxo proposto: distribuir → cada jogador palpita → jogar vazas → comparar previsto/realizado → atualizar vidas ou pontos → próxima rodada. Interface mostra palpite e vitórias por lugar, com confirmação clara antes de encerrar a fase de palpites.

Critério: exemplos de rodada aprovados, bots que fazem e perseguem palpites, regra de pontuação testada e sincronização com informação privada preservada.

## Etapa 4 — Acabamento audiovisual e experiência

- Expandir o elenco com atlas consistentes, mais poses e animações dedicadas de distribuição, blefe, vitória e derrota. Preparar camadas PNG reais para roupas se a customização modular continuar desejada.
- Produzir faixas originais mais longas com variação de intensidade por fase/boss; revisar mixagem e transições sem fadiga sonora.
- Animar efeitos de relíquias e passivas, entrada de bosses, fichas e recompensas, sempre com alternativa de movimento reduzido.
- Completar remapeamento de controles e navegação por controle, validar escala de texto e contraste em todas as telas, testar resoluções menores e diferentes proporções.
- Medir custo do SubViewport 3D e reduzir atualizações quando a mesa estiver coberta por menus. Exportar um novo executável com recursos e configuração de distribuição revisados.

Critério: desempenho estável em hardware-alvo definido, nenhuma informação dependente apenas de cor/som, ações essenciais por teclado/controle e export reproduzível.

## Perguntas para a próxima rodada

Estas perguntas não bloqueiam a repaginada entregue.

1. O nome comercial continua **MultiGame** ou o clube deve receber um nome próprio?
2. O primeiro multiplayer completo deve atender só **LAN** ou já precisa de salas pela internet?
3. Quais regras de **Fodinha** você usa, principalmente palpites, manilha, vidas/pontuação e progressão da quantidade de cartas?
4. Os personagens terão poderes compartilhados entre jogos ou uma habilidade diferente em cada modo?
5. Para a trilha final, você prefere seguir o clima atual, aproximar de jazz/bossa ou puxar mais para chiptune?

## Entrega Git

- Repositório: `Fabiarog/Multigame`, remoto local `pc-casa`.
- Estado original preservado em **Backup** antes das mudanças: `58b85b804c79dcc02da9882276d993247356577e`.
- Alterações desta entrega ficam em **review**; **main** não recebe a repaginada automaticamente.
- O arquivo executável antigo foi preservado. O código, recursos novos, testes e este plano compõem a revisão; export de distribuição é etapa própria.
