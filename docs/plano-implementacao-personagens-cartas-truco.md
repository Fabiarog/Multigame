# Plano de implementação — personagens, cartas e Truco

## Entregue nesta rodada

| Área | Entrega |
| --- | --- |
| Cenário | Poker e Truco agora usam o cenário de cassino por toda a tela; ele deixa de ser apenas uma faixa atrás da mesa. |
| Avatar | O tamanho do avatar foi ajustado à mesa. Os sprites atuais de camisa/calça são composições completas (não recortes transparentes), então o avatar usa a composição de roupa visível e não deixa a camada de cabelo apagar a vestimenta. |
| Acessibilidade | Novo menu com redução de tremores, redução de animações e filtros de protanopia, deuteranopia e tritanopia. O filtro pode afetar somente cartas ou a tela inteira. |
| Poker | Cartas selecionadas agora voam da mão para o centro da mesa; aparecem nome da mão, pontos e detalhamento de chips × multiplicador. A quantidade de bots e a dificuldade alteram a meta da rodada. |
| Truco | A mão começa embaralhada e só é distribuída após o corte do jogador. Cartas manilhas mostram `MANILHA` e o naipe, por exemplo `Paus (Zap)`. Aceitar/aumentar Truco retoma o turno que estava pausado, sem substituir carta já jogada. |
| Pena | Em 2v2/3v3, depois do corte, a Pena vai ao aliado mais próximo no sentido anti-horário. Se ele guardar, ela é a primeira carta da mão e recebe apenas outras duas; se devolver, ela substitui a Vira/tombo e ele recebe três cartas. |
| Equipes | O Hub oferece 1v1, 2v2 e 3v3. Salas LAN permitem sortear times ou o host mover cada pessoa entre Equipe 1 e 2. Faltas de assentos são preenchidas por bots ao iniciar. |
| Animações | O fluxo mostra embaralhamento, corte, ida e retorno da Pena, distribuição por assento, recolhimento das cartas e passagem do monte ao próximo distribuidor. “Reduzir animações” encurta toda a sequência. |

## Próximos passos propostos

1. **Assets de avatar reais:** exportar corpo, camisa, calça, cabelo e acessórios em PNG com transparência, todos no mesmo grid 4×4. Assim cada seletor do perfil pode combinar peças sem depender do fallback atual.
2. **Rodadas completas por equipes:** converter as mesas 2v2/3v3 — já com assentos, pena e composição de equipes — em turnos completos de todos os jogadores, com cartas individuais para aliados e adversários.
3. **Acessibilidade adicional:** adicionar tamanho de fonte, alto contraste e leitura por voz aos filtros e à redução de movimento já implementados.
4. **Multijogador LAN:** sincronizar corte, distribuição, Pena, ações de Truco e escolha de bots no estado de rede. O hub já registra Poker e Truco para iniciar salas.
5. **Polimento audiovisual:** ligar sons opcionais a cada etapa da nova sequência e adicionar partículas discretas para manilha e vitória.

## Decisões necessárias

As três decisões anteriores foram resolvidas: Pena guardada + duas cartas, aliado imediato anti-horário e suporte aos três filtros de daltonismo nos dois escopos. Não há decisão funcional pendente para esta etapa.

## Ideias adicionais

- Relíquias visuais colecionáveis com descrição ao passar o cursor e recompensa ao derrotar cada chefe.
- Histórico dos três últimos tombos do Truco, com a carta vencedora destacada.
- Presets de personagem (salvar conjuntos de roupa/cabelo) e desbloqueios por vitória.
- Sons opcionais e indicadores não dependentes de cor para seleção, manilha, Truco e pontuação.
- Replay curto da última mão, com linha do tempo de cortes, penas, aumentos e tombos.
- Perfil de estilo de jogo por bot (agressivo, cauteloso, blefador), exibido antes da partida.
- Mesa temática desbloqueável, clima/iluminação e trilhas sonoras por cenário.
- Espectador LAN e reconexão segura: quem cai volta à mesma equipe e o bot segura o lugar enquanto isso.
- Conquistas por estratégia, como vencer com pena virada em tombo ou aceitar Doze com manilha baixa.
