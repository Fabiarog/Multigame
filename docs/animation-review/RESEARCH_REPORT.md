# MultiGame — Relatório de Pesquisa de Animações & Referências

> Análise técnica, bibliotecas de assets, licenças e referências para jogos de cartas 3D
> Data: 2026-09-13 | Plataforma: Godot 4.7.2 .NET / Blender 5.2.1

---

## 1. Pesquisa de Bibliotecas e Fontes Externas

| Fonte | Licença | Compatibilidade | Veredito | Justificativa |
|---|---|---|---|---|
| **Mixamo (Adobe)** | Termos de Serviço Adobe (Uso livre em jogos comerciais, proibida redistribuição crua dos arquivos FBX/anim) | Exige rigging com esqueleto humanoide Mixamo (22–65 bones) | **Candidato Seletivo com Retargeting** | Útil para poses base e gestos genéricos (nod, defeat, sigh), mas exige retarget cuidadoso para preservar rigs customizados (ex: asas do Corvo, cauda da Onça/Zeca). |
| **Quaternius (Universal Animations)** | CC0 1.0 Universal (Domínio Público) | Low-poly humanóide modular (15–20 bones) | **Excelente Referência Estrutural** | Rigs simples e elegantes. Curvas de timing estilizadas com poses dinâmicas. Curvas limpas para inspeção. |
| **Kay Lousberg (KayKit Animations)** | CC0 1.0 Universal (Domínio Público) | Rigs humanóides estilizados | **Excelente Referência de Timing** | Antecipações exageradas e follow-throughs claros adequados para visão isométrica / de mesa. |
| **OpenGameArt.org** | Várias (CC-BY 3.0, CC-BY 4.0, GPL, CC0) | Variada | **Uso Restrito** | Licenças virais (GPL) devem ser evitadas. Apenas CC0 ou CC-BY compatível com atribuição em `CREDITS.md`. |
| **Poly Pizza (MCP Integrado)** | CC0 / CC-BY (com atribuição) | Modelos e props estáticos | **Uso Ativo via MCP** | Ideal para adereços de mesa, fichas, cartas temáticas. |
| **PolyHaven (MCP Integrado)** | CC0 | HDRIs, PBR Textures | **Uso Ativo via MCP** | Iluminação e materiais das mesas e salões. |

---

## 2. Dinâmica de Animação em Jogos de Cartas (Análise Comparativa)

Analisamos o design de movimento de referências no gênero:
- **Inscryption**: Movimentos misteriosos, ameaçadores e pausados. Personagens inclinam-se sobre a mesa; respiração e tensão perceptíveis em silêncio.
- **Hearthstone**: Animações de mesa enxutas (0.4s a 0.8s) para interações de cartas, com reações viscerais em vitórias/derrotas.
- **Pokerstars VR / Vegas Infinite**: Micro-movimentos constantes (olhar cartas, cruzar braços, tamborilar dedos) que impedem os avatares de parecerem manequins plásticos.
- **Balatro**: Embora 2D, o impacto audiovisual (screen shake, cards slamming, timing de contagem de fichas) demonstra a importância do feedback cinético.

### Princípios Extraídos para o MultiGame:
1. **Leitura Rápida sem Obstrução**: Nenhuma micro-reação deve durar mais de 1.2s ou travar a vez do jogador.
2. **Camadas de Intensidade (Layering)**:
   - Em rodadas normais: micro-aceno (`nod`), suspiro leve (`micro_sigh`), ajuste de postura (`seat_adjust`).
   - Em momentos de Truco / All-In: inclinação agressiva (`lean_forward`), bater na mesa, risada desafiadora.
   - Resolução de vazas (`trick_win` / `trick_lose`): reações contidas (0.8–1.2s) — **NUNCA disparar a animação de vitória de partida completa (`victory`) para uma vaza intermediária**.

---

## 3. Diretrizes de Curvas de Animação (Graph Editor & 12 Princípios Disney)

Para evitar movimentos robóticos (splines lineares secas) ou flácidos:
1. **Antecipação (Anticipation)**: Antes de jogar a carta ou gritar truco, 2–4 frames de recuo no torso/cabeça.
2. **Arco de Movimento (Arcs)**: Mãos e asas devem percorrer trajetórias parabólicas na mesa, não retas secas.
3. **Sobreposição e Atraso (Follow-Through & Overlap)**:
   - Corvo: Cabeça lidera; bico aponta primeiro; penas/asas abrem e assentam 3–5 frames depois.
   - Onça: Orelhas reagem com atraso de 2 frames ao movimento da cabeça; cauda ondula com amortecimento laplaciano.
   - Barão: O casaco pesado tem inércia; segue o torso com arrasto.
4. **Facilidade de Entrada e Saída (Ease In / Ease Out)**:
   - Usar tangentes Bézier ponderadas para assentar repousos orgânicos.
   - Evitar `CONSTANT` hold acidental no meio do clipe.
