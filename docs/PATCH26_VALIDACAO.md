# Patch 26 — Refinamento inicial de animação no Blender

12/09/2026. Base: `10c4b4d18ece647bd52cc42e3f50cad658ec8f5c`. Backup publicado: `a4d12eac60cd8a2b340a90aa93bf6614000e3e74`. Destino: review; main preservada.

## Resultado integrado

Os 11 GLBs de `assets/models/club` foram refinados no Blender 5.2.1, pelo MCP local na porta 9876. A direção artística segue a missão mais recente: estilizada, reconhecível e sem subdivisão indiscriminada. Foram preservadas as malhas, texturas, nomes de clipes e quantidade de ossos deformadores. A mudança principal está no movimento, e não em uma reconstrução visual completa.

- Repouso estendido para aproximadamente 4 segundos, com movimento discreto de cabeça, peito e partes secundárias quando existem canais correspondentes.
- Cabeça/pescoço antecipam o gesto; mãos, asas e cauda acompanham com atraso pequeno. A curva de tempo continua monotônica.
- Reações estendidas de aproximadamente 1,96 para 2,2 segundos; retorno à pose inicial nos últimos 12% do gesto. Entrada mantém seu destino de chegada. Repouso acomoda o fechamento nos últimos 15%.
- Jogar carta, cortar, distribuir e embaralhar preservam a duração aproximada para manter a sincronização existente. Reamostragem a 30 fps introduz arredondamento de no máximo um quadro.
- Ajuste discreto de 3,5 cm na altura da cabeça de Nina, Bento e Onça. Rugosidade de casacos/cetim ajustada apenas quando não é controlada por textura.
- Fontes `.blend` comprimidas e editáveis para todos os personagens. Oito fontes com esqueleto receberam 60 controles auxiliares no total para autoria IK, sem deformação e com influência zero. Os clipes FK existentes continuam sendo a referência no jogo.

## Auditoria por personagem

Vértices abaixo são contados após a importação no Blender; podem diferir dos acessores GLB devido a separação por materiais/normais. Zero ossos significa articulação por objetos/pivôs, não personagem sem animação.

| Personagem | Vértices | Ossos deformadores originais | Clipes preservados | Controles de autoria novos |
| --- | ---: | ---: | ---: | ---: |
| nina | 50,123 | 0 | 7 | 0 |
| bento | 47,840 | 0 | 7 | 0 |
| corvo | 106,772 | 23 | 39 | 8 |
| onca | 44,682 | 0 | 7 | 0 |
| iara | 45,530 | 21 | 39 | 8 |
| zeca | 40,299 | 25 | 39 | 8 |
| aki | 29,981 | 21 | 39 | 8 |
| barao | 40,662 | 23 | 39 | 8 |
| dama | 53,930 | 20 | 39 | 4 |
| morgana | 42,694 | 21 | 39 | 8 |
| carnical | 26,376 | 21 | 39 | 8 |

## Validação e evidência

- Compatibilidade estática: 11 personagens, 333 clipes, mesmos números de malhas e juntas; sem crescimento de geometria. GLBs somados: 93.435.060 → 94.637.548 bytes (+1,29%). O crescimento vem principalmente da reamostragem de animação.
- Blender: 1.895 amostras de limites espaciais, em cinco fases de cada clipe, incluindo os originais de Corvo e Onça. Isso detecta explosões/valores inválidos, mas não comprova boa deformação em todas as articulações.
- Inspeção visual: renders de idle, truco e vitória dos 11 personagens. Comparação original/candidato de Corvo e Onça. [Elenco](patch26/elenco-truco.jpg) e [comparação](patch26/comparacao-corvo-onca.jpg).
- Godot: compilação sem erros/avisos e importação sem erros. Regras PASS, 533 verificações; interface PASS, 27 capturas, 25 ações, zero falhas/layout. [Relatório visual](patch26/visual-report.json).
- Câmera PASS nos três modos, incluindo densidade 4K, limites do pescoço e alternância de visão. [Relatório](patch26/camera-report.json). [Mesa](patch26/truco-mesa.png) e [POV](patch26/truco-pov.png).
- Exportação Windows concluída (`Game Hub.exe` e `Game Hub.pck` locais); pacote iniciou o menu no teste headless, saída 0. O PCK é artefato local ignorado pelo Git, conforme a configuração existente.

## Limites e trabalho pendente

A tentativa de converter Nina/Bento/Onça de pivôs para esqueleto foi rejeitada: o GLB reimportado apresentou peças desalinhadas, apesar de matrizes próximas no Blender. O código experimental não faz parte do gerador de produção; nenhum candidato com essa falha foi integrado.

Não houve retopologia orgânica, pintura nova de pesos, dedos novos, novo sistema facial ou reconstrução de cenário neste passe. Os controles IK são preparação para autoria e precisam de ajuste de alvos/polos e validação em poses extremas antes de uso. Não afirmar que são IK de pernas com contato confiável no chão ou IK em execução. Os 333 clipes preservados não equivalem a 333 atuações novas.

Persistem avisos de 1–2 objetos retidos ao encerrar os processos de QA. Multiplayer entre duas máquinas, desempenho comparativo e novas poses extremas não foram validados nesta etapa. Os testes não garantem ausência de todos os bugs.

## Reprodução e fontes

Fontes e relatórios completos: `art/blender/patch26`. Scripts `tools/refine_all_cast26_mcp.py`, `tools/preview_all_cast26_mcp.py` e `tools/validate_cast26.py`. O gerador recupera os originais do commit fixo da base; assim não refina novamente os arquivos já alterados em um checkout limpo. Os GLBs de staging e renders individuais ficam ignorados pelo Git, enquanto os GLBs de execução e fontes Blender são versionados.

Executar somente uma operação Blender MCP de cada vez. Preservar a cena que estiver aberta. Usar `PYTHONIOENCODING=utf-8` no transporte. O CLI game-dev não está disponível neste ambiente; não foram produzidos recibos de pacote dessa ferramenta nem usados provedores pagos. Os assets derivados mantêm a proveniência dos arquivos existentes no repositório.
