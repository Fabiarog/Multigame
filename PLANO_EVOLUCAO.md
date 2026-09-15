# Plano simples de evolução — MultiGame

Atualizado em 12/09/2026. Base: 11 personagens, quatro salas, pôquer roguelike, truco com equipes de IA, Fodinha solo e mesa 2.5D/POV. Patch 25 corrigiu ritmo/estabilidade e Patch 26 integrou um primeiro passe de animação em 11 personagens via Blender, preservando 333 clipes. Nova arquitetura, retopologia e revisão de pesos continuam próximas etapas.

## Agora: ritmo e estabilidade

- Sincronizar cada etapa da partida com o fim visível da animação: embaralhar, cortar, distribuir, jogar, recolher e mostrar resultado.
- Manter o jogador bloqueado somente enquanto a carta está chegando à mão; nunca liberar a próxima ação com uma carta ainda voando.
- **Implementado:** reduzir movimentos preserva os tempos de leitura e decisão. Aceleração de regras existe somente no teste isolado.
- Testar pôquer, truco 1x1/2x2/3x3 e Fodinha em janela e 4K, registrando imagens e falhas no log de atualização.

## Entregue no Patch 26

- Repouso de 4 segundos, microanimações, antecipação e acomodação dos gestos; reações mais legíveis sem acelerar as regras.
- Corrigidos 411 vértices nos braços de Morgana/Carniçal que formavam pontas ao levantar as mãos; ombros de Morgana continuam exigindo revisão.
- Fontes Blender editáveis dos 11 personagens e controles IK de autoria em oito deles. Auditoria e comparações em `docs/PATCH26_VALIDACAO.md`.
- Testes de regras, interface, câmera/4K e exportação Windows aprovados. Não houve aumento de geometria.

## Próximo passe de personagens

- Priorizar Onça, conforme a preferência já expressa, usando Corvo e os demais modelos detalhados como referência. Depois migrar Nina e Bento sem perder sua identidade.
- Consolidar pesos e esqueleto deles antes de migrar Nina, Bento e Onça: raiz sem deformação, coluna/peito sem translação agressiva, clavículas sustentando os braços e poses sem atravessar o tórax.
- Preservar os 333 clipes existentes e garantir os sete clipes básicos por personagem: `idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish` e `play_card`.
- Fazer uma revisão visual por clipe antes de substituir qualquer modelo jogável. Os arquivos base dos três que ainda não têm fonte detalhada devem continuar preservados.

## Cenário e apresentação

- Ajustar sombras de contato, materiais do feltro e iluminação de sala sem aumentar a carga dos perfis leves.
- Revisar as entradas de partida e de chefe para que a câmera, a fala e a animação tenham tempo de leitura.
- Manter a visão 2.5D disponível e o POV com olhar limitado, botão direito para olhar, tecla `C` para alternar e botão `Centralizar`.

## Depois

- Definir progressão roguelike do Fodinha: recompensas, modificadores de partida e desbloqueios por personagem.
- Implementar sincronização de partida multiplayer para turnos, baralho, palpites e decisões de pena; o lobby atual não substitui essa etapa.
- Criar uma biblioteca de reações desbloqueáveis, ligada a missões e conquistas dos personagens.

## Perguntas curtas para a próxima decisão

1. Você quer que o ritmo normal tenha mais clima de mesa real (mais pausado) ou de jogo rápido, mas sem sobreposição? Vou manter um meio-termo até sua resposta.
2. Na evolução da Onça, prefere manter roupa e acessórios atuais ou dar mais destaque aos detalhes felinos? Até uma resposta, preservar o figurino e trabalhar primeiro proporções/articulações.
3. No Fodinha, você quer que a campanha roguelike comece com relíquias/modificadores simples ou primeiro com mais personagens e chefes?
