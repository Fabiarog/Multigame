# MultiGame — Plano de Arquitetura Sonora e Música Dinâmica

## 1. Visão Geral e Princípios Artísticos

A sonorização e a trilha musical do MultiGame deixam de ser apenas loops de fundo genéricos e passam a compor a espinha dorsal da narrativa, ritmo e tensão das disputas de Truco e cartas.

### Diretrizes Fundamentais:
1. **Composições 100% Originais**: Todas as melodias, progressões harmônicas e arranjos devem ser originais, inspirando-se em épocas, estilos e instrumentos autênticos sem emular ou reproduzir peças protegidas por copyright.
2. **Unidade Cenário + Boss + Ritmo**: Cada ambiente possui sua assinatura tímbrica única que dialoga com a iluminação, arquitetura e os rivais que o habitam.
3. **Sistema Dinâmico em Camadas (Adaptive Layering)**: A música reage organicamente ao estado da mesa (rodada normal, empate, pedido de Truco, blefe decisivo e vida crítica) através de transições suaves e crossfades sincronizados.
4. **Fidelidade Tátil dos Efeitos (Foley)**: Cartas, feltro, madeira maciça, couro e fichas pesadas possuem peso físico e resposta sonora imediata às ações do jogador.

---

## 2. Assinatura Sonora por Cenário

Cada um dos quatro salões do MultiGame possui uma paleta instrumental e identidade acústica própria:

| Cenário | ID do Tema | Estilo Musical | Instrumentação Principal | Sensação / Atmosfera |
| :--- | :--- | :--- | :--- | :--- |
| **Salão Clássico** | `classic_club` | Aristocrático Acústico | Piano de cauda, quarteto de cordas, contrabaixo acústico pizzicato | Tradição, aristocracia vintage, lareira crepitante, peso do tempo |
| **Lounge do Barão** | `barao_lounge` | Jazz Noir Aveludado | Trompete com surdina Harmon, piano elétrico Rhodes, bateria com vassourinhas | Fumaça, mistério, meias-luzes, conversas sussurradas |
| **Salão da Dama** | `dama_salon` | Bossa Lounge Sofisticada | Violão de nylon acústico, flauta transversal doce, percussão orgânica brasileira | Charme sedutor, elegância tropical, calma perigosa |
| **Cassino Cyber** | `cyber_casino` | Dark Synthwave Melódico | Sintetizador analógico Moog, arpeggiators quentes, pads atmosféricos analógicos | Neon noturno, precisão digital, pulso hipnótico e moderno |

---

## 3. Leitmotivs dos Bosses Exclusivos

Cada chefe possui um motivo melódico marcante que o jogador reconhece imediatamente em sua entrada e ao longo do confronto:

### 1. Barão da Meia-Noite (`barao`)
- **Leitmotiv**: Frase cromática descendente em clarinete baixo e contrabaixo com arco (tempo 4/4 lento, rubato).
- **Entrada**: Staccato grave de piano em acorde menor de 9ª, seguido por sussurro de pratos.
- **Clímax**: Camada rítmica com pulso constante de bumbo abafado e contrabaixo agressivo.

### 2. Dama de Copas (`dama`)
- **Leitmotiv**: Arabesco ascendente em modo dórico com violão de nylon e trilos de flauta.
- **Entrada**: Floreio rápido de harpa/violão e acorde suspenso que não resolve de imediato.
- **Clímax**: Percussão acelerada com castanholas estilizadas e staccatos de cordas.

### 3. Madame Morgana (`morgana`)
- **Leitmotiv**: Progressão modal em harmonia menor harmônica executada por violoncelo solo e pad etéreo granular.
- **Entrada**: Eco reverberado de címbalos tibetanos e respiração orquestral profunda.
- **Clímax**: Ostinato tenso de violoncelos em uníssono, com acentos sincopados a cada pedido de truco.

### 4. Lorde Carniçal (`carnical`)
- **Leitmotiv**: Intervalo de trítono (quinta diminuta) em metais graves (tuba e trombone tenor) com ressonância cavernosa.
- **Entrada**: Tímpano único e seco, seguido por raspagem metálica sutil.
- **Clímax**: Batidas tribais secas, percussão de madeira oca e crescendo de cordas dissonantes.

---

## 4. Arquitetura do Sistema de Áudio em Camadas (Godot .NET)

### Estrutura de Camadas (Stems Sincronizados)

Cada tema de sala e boss é composto em 4 stems independentes de exatamente a mesma duração e compasso:

```
[Layer 1: Base Ambient]  ---> Sempre ativa (Volume: 0 dB)
[Layer 2: Tension]       ---> Ativa em empates ou rodada 3 (Fade-in 1.2s)
[Layer 3: Decisive/Truco]---> Ativa imediatamente ao pedir Truco (Fade-in 0.3s)
[Layer 4: Climax/Showdown]-> Ativa quando vidas <= 1 ou blefe final (Fade-in 0.5s)
```

### Implementação Técnica em C# (`MusicManager.cs` / `TableStage.cs`)

1. **Sincronização de Playback**:
   Todos os quatro canais (`AudioStreamPlayer`) são iniciados simultaneamente no frame 0 com volume `-80 dB` (mudo), e o volume linear (`db_to_linear` / `linear_to_db`) é manipulado via `Tween`:
   ```csharp
   public void SetMusicIntensity(MusicState state)
   {
       float baseDb = 0f;
       float tensionDb = state >= MusicState.Tension ? 0f : -80f;
       float trucoDb = state == MusicState.TrucoActive ? 0f : -80f;
       float climaxDb = state == MusicState.Climax ? 0f : -80f;

       var tween = CreateTween().SetParallel(true);
       tween.TweenProperty(_playerBase, "volume_db", baseDb, 0.8f);
       tween.TweenProperty(_playerTension, "volume_db", tensionDb, 1.2f);
       tween.TweenProperty(_playerTruco, "volume_db", trucoDb, 0.3f);
       tween.TweenProperty(_playerClimax, "volume_db", climaxDb, 0.5f);
   }
   ```

2. **Crossfades de Sala**:
   Ao trocar de tema (ex: Salão Clássico -> Lounge do Barão), um crossfade de 2.0 segundos é aplicado com equalização em corte de agudos momentâneo (lowpass filter) para evitar corte abrupto.

---

## 5. Design de Efeitos Sonoros Físicos (Foley & SFX)

O feedback auditivo reforça a materialidade e a estética premium da mesa de jogo:

1. **Cartas**:
   - `card_slide_felt_01..04`: Deslize aveludado sobre o feltro com camadas de tecido.
   - `card_snap_wood_01..03`: Batida firme da carta na mesa, com ressonância oca da madeira de jacarandá.
   - `card_fan_open / close`: Movimento suave de abertura do leque de cartas na mão do jogador.
2. **Fichas e Apostas**:
   - `chip_clack_ceramic_01..05`: Som seco e rico de fichas de argila/cerâmica de 14 gramas colidindo.
   - `chip_slide_stack`: Empurrar a pilha de fichas ao centro da mesa em apostas all-in.
3. **Ambiente e Cadeira**:
   - `chair_leather_creak`: Leve rangido do estofamento de couro capitonê quando o personagem se move.
   - `glass_crystal_clink`: Toque sutil de taça de cristal ao fundo nos salões clássico e da dama.
4. **Stingers de Resultado**:
   - `victory_round_stinger`: Acorde resolutivo em modo maior com brilho de cordas.
   - `defeat_round_stinger`: Cadência interrompida em tom menor sutil.
   - `truco_call_stinger`: Impacto percussivo tenso com eco estéreo de 400ms.
