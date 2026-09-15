# Integração do conteúdo existente — 13/09/2026

Base recebida: d98a569. Preservada no GitHub em Backup 6154ebd antes das alterações. Publicação destinada a review; main preservada.

## O que existia nos arquivos e faltava no jogo

| Recurso | Lacuna encontrada | Integração realizada |
|---|---|---|
| TrucoAuthority, FodinhaAuthority e HoldemAuthority | Lobby apontava para NetworkTable.tscn inexistente | Cena de mesa e interface que consomem a projeção privada do host |
| Texas Hold’em | Regras existentes, sem opção no menu | Modalidade multiplayer separada do pôquer roguelike, fichas fictícias |
| Fodinha em rede | Opção LAN explicitamente bloqueada | Criação/entrada de sala para quatro assentos, vagas restantes com bots |
| Madrid e La Mesa de los Recuerdos | Carregados pelo ciclo de cenários, ausentes da seleção de salão | Dois novos botões, descrições e destaque de seleção |
| TableStage | Sem consumidor para partidas autoritativas | Mesa 2.5D, mãos adversárias fechadas, cartas públicas animadas, corte e reação de Truco |
| Tema escolhido pelo host | Não chegava à nova mesa remota | Tema incluído na projeção e aplicado sem gravar preferências do cliente |
| Erros de lobby | Status genérico escondia a mensagem do gerenciador | Motivo de erro e instrução de prontidão aparecem no lobby |
| Desconexão | Serviço antigo continuava processando a conexão encerrada | Serviço legado desativado; saída limpa e aviso de host desconectado |
| Nomes de cartas Texas | Expressão de precedência incorreta na conversão | Rank e naipe corrigidos |

## Validação

- Compilação .NET: zero erros/avisos.
- Regras solo existentes: 531 asserções aprovadas.
- Interface solo/hub: 27 capturas, 25 ações, zero falhas e zero alertas de layout.
- Rede: dois processos Godot independentes por modalidade, inclusive com renderização OpenGL e a cena NetworkTable carregada. Resultados iguais nos dois destinatários; logs com PIDs, matchId, sequência, ACKs e rejeições em docs/network-qa.
- Cliente envia JSON malformado e tentativa de score/winner falsos: host rejeita ambos. O teste não registra as mãos privadas em logs compartilhados.
- Screenshots de cada assento em docs/network-qa; evidenciam a interface real, não apenas modelos de regras.
- Avisos de ObjectDB no encerramento do harness ainda existem; não são classificados como resolvidos.

Reproduzir: .NET 8 em DOTNET_ROOT, executar `dotnet build`, `./tools/network_integration.ps1 -Visual` e `./tools/visual_smoke.ps1 -SkipBuild`. O harness usa perfis de dados separados e MULTIGAME_NET_QA para acelerar somente o teste. Nunca configurar essa variável ao jogar normalmente.

## Ainda não são recursos completos para disponibilizar

- Relay, matchmaking e autenticação em OnlineMultiplayerManager são stubs, não serviços operacionais. Não anunciar conexão automática pela internet.
- Os sete cenários antigos de ScenarioManager são definições/cores de fallback; não correspondem a sete salões 3D prontos adicionais.
- Reentrada autenticada e migração de host não existem. O cliente que sai é substituído por bot na mesa atual; host que sai encerra a partida.
- A apresentação da mesa de rede é inicial: controles de mão em texto, cartas comunitárias Texas em linha; não há paridade completa com cutscenes e distribuição cinematográfica do solo.
- Testes de dois computadores físicos, WAN, perda/latência artificial, ações simultâneas, reconexão e auditoria aprofundada de potes paralelos ainda precisam ser executados. Dois processos no mesmo PC não comprovam WAN/LAN entre máquinas.
- ENet direto não criptografa tráfego. O hash detecta inconsistência da projeção; não é proteção contra um host malicioso.

## Próximos passos

1. Completar a matriz de QA acima antes de anunciar multiplayer finalizado.
2. Integrar distribuição/entrada cinematográfica com barreiras de apresentação do host, sem acelerar o turno sobre animações em andamento.
3. Unificar o cadastro de jogos/cenários para evitar novas opções disponíveis apenas por atalhos.
4. Auditar modelos candidatos no Blender antes de substituir assets; esta etapa integra código existente e não altera geometria.

## Pacote local

Game Hub.exe foi atualizado por exportação Windows Debug (versão de teste). Exportação e abertura independente do menu com `--headless --quit-after 120` retornaram 0. A exportação Release falhou porque o diretório local 4.7.2.stable.mono não contém windows_release_x86_64.exe. Não confundir este pacote de teste com uma distribuição Release. Evidência de abertura em docs/network-qa/package.log.
