# Multiplayer autoritativo — implementação e evidências

Base c04d306 preservada em Backup 7204838, incluindo os dois arquivos Madrid recebidos. Main não deve mudar. Missão: partidas reais, com host validando intenções e projeções privadas por destinatário.

## Auditoria inicial

NetworkManager usa ENet/UDP 7070, mas só registra nomes. LobbyManager anuncia uma cena e um seed, sem motor compartilhado, prontidão de carregamento ou sincronização de regras. ServerSetPlayerReady aceita peerId arbitrário do cliente. Cliente não recebe toda a configuração do lobby. Desconexão transmite RPC mesmo em cliente. TrucoGameManager é local/assento zero; FodinhaMatch é um modelo puro com quatro assentos; pôquer atual é roguelike solo. Não existe uso de WebSocket no projeto. OnlineMultiplayerManager tem ENet direto e stubs de relay/matchmaking; ReconnectionService/DisconnectHandler apenas simulam estados de reconexão/takeover, sem restabelecer uma partida.

## Decisões aprovadas

- Pôquer roguelike continua solo. Novo Texas Hold’em multiplayer com fichas fictícias, sem dinheiro real.
- ENet direto para esta etapa. Host possui deck, RNG, mãos e modelos. RPC recebe somente intenção, matchId, actionId e sequência observada. Identidade vem de GetRemoteSenderId.
- Snapshot público + mão do próprio destinatário. Nenhuma semente de baralho, mão alheia, deck ou hash de segredo é transmitido. Hash SHA-256 é da projeção enviada, por peer/sequence, para detectar divergências de sincronização; não autentica nem torna ENet criptografado.
- RPCs de resultado Authority; intenções AnyPeer restritas ao servidor, assento e fase. Limites de tamanho/taxa, versões, replay e ações obsoletas. Timers do host controlam distribuição e leitura dos resultados.
- Cliente desconectado vira bot definitivamente na mesa atual. Reentrada em mesa ativa recusada; pode entrar em novo lobby. Host desconectado encerra partida. Sem migração de host/reconexão autenticada nesta versão.
- Rede usa cena de mesa dedicada e modelos independentes do solo, evitando executar IA/placar local em cada cliente. Preservar o cenário 2.5D; paridade de toda a apresentação solo pode vir depois.

## QA necessário antes de conclusão

Dois processos independentes por modo, logs de PID/assento/sequence/hash e resultado; privacidade por projeção; intenções fora do turno, carta inexistente, score/winner falsos, duplicadas, simultâneas e JSON malformado; atraso/perda via proxy UDP; desconexão e tentativa de reentrada; saída do host. Regras Texas: ranking de cinco entre sete, blinds, apostas, fold/check/call/raise, all-in, potes paralelos e empates. Regras Truco: times, pena, manilha, empates, pedidos/respostas e placar. Não declarar testes de dois computadores ou WAN sem executá-los.

Referência técnica: [Godot 4.7, multiplayer de alto nível](https://docs.godotengine.org/en/4.7/tutorials/networking/high_level_multiplayer.html). UDP direto exige conectividade LAN/firewall; WAN depende de rota pública/encaminhamento UDP e pode ser impedida por CGNAT. Não prometer relay ou browser/WebSocket.

Status inicial: auditoria e desenho concluídos; implementação e QA em andamento. Este documento será atualizado com resultados reais.
