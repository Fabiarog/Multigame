# Plano de implementação — personagens, cartas e Truco

## Entregue nesta rodada

| Área | Entrega |
| --- | --- |
| Cenário | O Poker ganhou um fundo 2D em tela cheia com `KeepAspectCovered`; o cenário deixa de ser apenas uma faixa atrás da mesa. |
| Avatar | O tamanho do avatar foi ajustado à mesa. Os sprites atuais de camisa/calça são composições completas (não recortes transparentes), então o avatar usa a composição de roupa visível e não deixa a camada de cabelo apagar a vestimenta. |
| Acessibilidade | Novo menu com redução de tremores, redução de animações e modo de cor. As preferências são salvas. |
| Poker | Cartas selecionadas agora voam da mão para o centro da mesa; aparecem nome da mão, pontos e detalhamento de chips × multiplicador. A quantidade de bots e a dificuldade alteram a meta da rodada. |
| Truco | A mão começa embaralhada e só é distribuída após o corte do jogador. Cartas manilhas mostram `MANILHA` e o naipe, por exemplo `Paus (Zap)`. |

## Próximos passos propostos

1. **Assets de avatar reais:** exportar corpo, camisa, calça, cabelo e acessórios em PNG com transparência, todos no mesmo grid 4×4. Assim cada seletor do perfil pode combinar peças sem depender do fallback atual.
2. **Bots de mesa:** transformar a configuração de 2–3 bots em oponentes independentes, cada um com posição, cartas, turno e dificuldade próprios. Nesta rodada, a escolha já modifica a meta do Poker; o fluxo completo de mesa precisa dessa expansão de regras.
3. **Acessibilidade visual:** aplicar filtros de cor ao jogo inteiro e adicionar tamanho de fonte/alto contraste, além das preferências já salvas.
4. **Animações do Truco:** mostrar fisicamente o monte embaralhando, a carta de corte e a distribuição uma a uma, tal como a nova animação de Poker.
5. **Multijogador LAN:** sincronizar corte, distribuição, ações de Truco e escolha de bots no estado de rede. O hub já registra Poker e Truco para iniciar salas.

## Decisões necessárias

1. O que significa **“dar pena para a dupla”** nas regras desejadas? Informe quando ocorre, quantos pontos/qual penalidade e se vale para Truco paulista, mineiro ou outra variação.
2. Em uma partida Solo com 2 ou 3 bots, todos jogam individualmente, ou existem duplas? Para Truco, a escolha define a formação de times e o fluxo de turnos.
3. O modo de cor deve alterar somente as cores dos naipes, ou a tela inteira? A segunda opção exige um filtro visual global.

## Ideias adicionais

- Relíquias visuais colecionáveis com descrição ao passar o cursor e recompensa ao derrotar cada chefe.
- Histórico dos três últimos tombos do Truco, com a carta vencedora destacada.
- Presets de personagem (salvar conjuntos de roupa/cabelo) e desbloqueios por vitória.
- Sons opcionais e indicadores não dependentes de cor para seleção, manilha, Truco e pontuação.
