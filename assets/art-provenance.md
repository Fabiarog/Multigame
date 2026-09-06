# Origem dos recursos da repaginada

Data: 5 de setembro de 2026. Direção: clube brasileiro de cartas, verde profundo, papel envelhecido e latão.

## Imagens geradas

- `ui/club-table.png`: ilustração original gerada com a ferramenta ImageGen integrada ao Codex. Brief: natureza-morta de mesa de cartas com feltro verde escuro, cartas antigas, fichas e luminária de latão; acabamento de guache e gravura, luz quente, sem texto. O brief aqui é uma descrição do resultado, não uma transcrição do prompt original.
- `sprites/characters/club/club-cast.png`: spritesheet original gerada com a mesma ferramenta; 1774 × 887, transparência alfa, quatro colunas e duas poses por personagem. Recorte em atlas pelo código, sem extrair novos arquivos. Personagens: Nina, Bento, Seu Corvo e Dona Onça.
- Não foram usados personagens ou trilhas de Balatro. A referência orienta o gênero, não a reprodução de sua arte.

### Prompt enviado para os personagens

```text
Use case: stylized-concept. Asset type: production-ready character spritesheet for a 2.5D Brazilian poker/truco roguelike card club. Exactly FOUR original characters in FOUR equal-width columns, exactly TWO pose rows. A regular 4 columns x 2 rows grid, no drawn grid lines, no text. Each cell contains one separate centered waist-up character at identical scale, entire head and arms within cell with 12% transparent padding. Column 1: Nina, adult Brazilian woman with short black curly hair, copper round glasses and an emerald waistcoat, holding cards. Column 2: Bento, adult brown-skinned Brazilian man with thick dark moustache and cream shirt/rust suspenders, holding cards. Column 3: an original anthropomorphic crow in dark green tailored vest and small antique brass spectacles, holding cards. Column 4: an original anthropomorphic female jaguar with spotted fur and burgundy jacket, holding cards. Row 1 calm attentive idle pose with cards in hand. Row 2 SAME characters, SAME clothes and scale, confident cheerful reacting pose with one arm raised, cards in other hand. Art style polished detailed pixel art game sprites, crisp visible pixels, 1990s adventure portraits but clean modern readable silhouettes, restrained palette dark emerald, antique cream, muted burgundy and copper. Soft warm lighting. Truly TRANSPARENT alpha background in every cell, absolutely no background color, no checkerboard pattern, no scenic backdrop, no shadows outside character silhouettes, no tables, no frames, no words, no numbers. No existing franchise likenesses. Landscape sheet, 2048x1024 if possible, grid four columns of square512x512 cells and two rows.
```

## Recursos feitos em código

- Atualização Blender: `models/club/*.glb` e retratos PNG construídos e renderizados no **Blender 5.2.1**, por `tools/build_blender_cast.py`. Modelos originais, sem downloads de modelos prontos. Fontes editáveis em `art/blender/*.blend`. Corvo e Onça são reinterpretações do elenco original desta repaginada.
- `models/cards/*.png`: 52 faces geradas por `tools/generate_card_faces.ps1`, com valor/naipe e moldura desenhados em código. O jogo mapeia essas texturas em malhas de carta com espessura.
- Música adicional: `copper-steps.wav`, `midnight-baron.wav`; vinhetas `arrival.wav`, `boss-arrival.wav`, produzidas pelo mesmo sintetizador original. Total atualizado: cinco loops.

- `ui/club-mark.svg`: marca geométrica de espadas, vetorial, criada nesta repaginada.
- Faces e versos de cartas: desenhados por `core/visuals/PlayingCard.cs`, seguindo os valores reais do jogo.
- Mesa, aro, baralho e leques: malhas simples em `core/visuals/TableStage.cs`.
- `audio/*.wav`: composições e efeitos sintetizados pelo script original `tools/generate_club_audio.py`, sem samples externos. As três músicas são loops curtos de protótipo; não são faixas longas produzidas em estúdio.

## Fontes

Distribuídas sob SIL Open Font License 1.1; os textos de licença acompanham cada família em `fonts/`.

- [DM Sans](https://github.com/google/fonts/tree/main/ofl/dmsans): interface.
- [Cormorant Garamond](https://github.com/google/fonts/tree/main/ofl/cormorantgaramond): títulos.
- [DM Mono](https://github.com/google/fonts/tree/main/ofl/dmmono): números e indicadores.

## Acervo anterior

Em 06/09/2026, o Patch 8 refinou os fontes Blender recebidos do usuário através do addon MCP local: mais geometria e acabamento nos oito personagens, novos gestos/detalhes em Corvo, Onça, Barão e Dama, correção de repouso nos oito GLBs e relógio original com pêndulo. Scripts `tools/refine_club_mcp.py` e `tools/polish_cast_mcp.py`, fontes separados `art/blender/*_mcp.blend` e `club_clock.blend`. O gerador base recebido também foi corrigido para aplicar materiais e modificadores corretamente. Nenhum modelo, textura ou música de serviço externo foi baixado nesta etapa. O atlas pixel art atualizado pelo usuário foi preservado.

Os JPGs de cenários, cartas, avatares e roupas existentes foram preservados. Sua autoria/licença não foi alterada nem inferida. Alguns têm fundo verde ou quadriculado incorporado e precisam de tratamento/fonte com alfa para uso como recortes. O novo elenco e as cartas desenhadas em código evitam depender dessas limitações na mesa principal.
