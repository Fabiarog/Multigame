# Plano simples de evolução — MultiGame

Atualizado em 07/09/2026. Este plano parte do estado já existente: pôquer roguelike, truco solo com equipes de IA, Fodinha solo, mesa 2.5D/POV e o elenco no Blender.

## Agora: ritmo e estabilidade

- Sincronizar cada etapa da partida com o fim visível da animação: embaralhar, cortar, distribuir, jogar, recolher e mostrar resultado.
- Manter o jogador bloqueado somente enquanto a carta está chegando à mão; nunca liberar a próxima ação com uma carta ainda voando.
- Tratar a opção **Reduzir animações** como acessibilidade: ela encurta movimentos e esperas de propósito. O texto da opção deve deixar isso claro.
- Testar pôquer, truco 1x1/2x2/3x3 e Fodinha em janela e 4K, registrando imagens e falhas no log de atualização.

## Próximo passe de personagens

- Usar como referência os cinco modelos detalhados já prontos: Corvo, Barão, Dama, Iara e Zeca.
- Consolidar pesos e esqueleto deles antes de migrar Nina, Bento e Onça: raiz sem deformação, coluna/peito sem translação agressiva, clavículas sustentando os braços e poses sem atravessar o tórax.
- Garantir sete clipes por personagem: `idle`, `entrance`, `truco`, `victory`, `boss_intro`, `flourish` e `play_card`.
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
2. Para o próximo modelo detalhado, você prefere priorizar **Onça**, **Nina** ou **Bento**? Hoje Corvo, Barão, Dama, Iara e Zeca são a base mais avançada.
3. No Fodinha, você quer que a campanha roguelike comece com relíquias/modificadores simples ou primeiro com mais personagens e chefes?

