# Patch 25 — revisão de animação e imagem

Data: 12/09/2026. Base recebida: 1f30bf0. Backup: 43e7f9a, publicado antes das alterações.

## Escopo implementado

- Truco e Fodinha: acessibilidade não acelera mais as decisões das IAs.
- Pôquer: proteção de pontuação contra chamadas repetidas, uma única distribuição por jogada e bloqueio da interface até terminar a entrega.
- Fodinha: reação depois do pouso, proteção de cliques e entrada da mão somente quando suas cartas mudam.
- Personagens: transições limpam gestos pendentes antes de iniciar o novo clipe; modelos e pesos preservados.
- Cenário: feltro com filtragem do relevo por tamanho de pixel, grão reduzido, sombras de contato menos fortes, preservação de letras sem FXAA adicional e vinheta com interpolação válida.
- Balões de reação usam coordenadas lógicas da mesa, inclusive quando a renderização está em 4K.

## Evidência

- Compilação C#: sem erros ou avisos.
- Regras: PASS, 509 asserções na última execução; inclui regressão nova de pontuação duplicada e distribuição do pôquer. O número varia com o percurso das IAs.
- Interface com OpenGL em velocidade normal: PASS, 27 capturas, 25 ações, zero falhas e zero problemas de layout. Relatório preservado em `docs/patch25/visual-report.json`.
- Câmera: teste de alternância, arrasto, limites de pescoço, redução de movimento e resolução interna 4K nos três modos. Relatório final em `docs/patch25/camera-report.json`.
- Vulkan: cenário sintético com seis participantes e cartas, perfis leve/ultra em 720p e 4K, sem erros de shader. Não é uma comparação de desempenho; outros testes rodaram simultaneamente e não há baseline para prometer ganho de FPS.
- Limitação conhecida: avisos de 1–2 objetos Godot retidos no encerramento dos testes; sem exceções durante as ações aprovadas. Multiplayer entre computadores e revisão individual de todos os clipes/ossos não foram realizados.
- Exportação Windows concluída após priorizar o SDK .NET 8 no PATH (o SDK global 10 causava erro no editor). Pacote local abriu o menu no teste headless, saída 0; avisou três objetos retidos ao sair. `Game Hub.exe` deve ser distribuído junto de `Game Hub.pck` e da pasta de dados .NET. O PCK continua ignorado no Git conforme a regra existente do projeto.

O teste de câmera encontrou duas referências diferentes de posição para a visão elevada. Foram unificadas mantendo a visão ampla da mesa. A tecla C também é capturada antes dos controles de cartas, respeitando campos de texto e ajustes abertos.

## Continuação visual proposta

Manter a mesa central e o relógio com o mascote como referências. Priorizar silhuetas legíveis, mãos e cartas; preservar Corvo e Onça. Revisar poses de entrada, truco e vitória em cada modelo antes de aumentar polígonos. Uma mudança de arquitetura deve começar por planta e enquadramentos aprovados, conforme o plugin Build 3D Game Rooms. Nenhum novo GLB é apresentado como final nesta etapa.

Perguntas para a próxima etapa: prefere o salão mais acolhedor, de madeira e luz âmbar, ou mais sombrio? Entre Corvo e Onça, qual deve receber primeiro o passe completo de pesos e gestos?
