# Patch 27 — Classic Club como referência visual

Base a1981dd; Backup 8f73b34 inclui os arquivos novos recebidos. Main preservada. Godot instalado: 4.7.2.stable.mono.official.ed1daf0bf. Renderer da referência: Forward+/Vulkan.

## Auditoria e ordem de impacto

1. Materiais metálicos sem reflexão ambiente; luzes amplas sobrepostas e lareira com emissão estourada. Impacto alto em toda a imagem.
2. Mesa construída com cilindros sobrepostos, poltronas muito uniformes e contornos rígidos. Impacto alto nas áreas mais vistas.
3. Fundos competem com as cartas: livros saturados, tapete e parede muito próximos do feltro, ausência de hierarquia de intensidade.
4. Muitas malhas pequenas: Classic Club tem 266 objetos/30.404 vértices; carrinho tem 80 objetos/17.308 vértices. Possível redução de chamadas com agrupamento de objetos estáticos por material.
5. Todos os ambientes auditados têm uma camada UV; ainda não há UV2 dedicado nem bake LightmapGI validado.
6. Corvo será a referência de personagem. Não substituir sua identidade nem repetir o refinamento de pesos do Patch 26.

## Direção proposta para a primeira sala

Classic Club: verde petróleo profundo, nogueira, couro castanho e latão envelhecido; cartas creme com maior prioridade de leitura. Luz principal quente sobre a mesa, preenchimento mais frio e discreto nos rostos, fontes práticas âmbar. Fundo menos saturado que as cartas, exposição estática e bloom restrito às fontes. Sem DOF ou motion blur durante a partida.

Mesa: borda de couro arredondada, costura discreta, friso de latão e madeira com bevel real. Poltrona: veludo fosco, madeira escura e ferragens legíveis. Manter a arquitetura e melhorar os assets existentes; não escalar às demais salas antes da comparação.

## Técnicas candidatas e pesquisa

- ReflectionProbe por zona, atualização Once: primeiro candidato para devolver leitura ao metal; excluir atores/cartas da captura estática.
- SSAO moderado e sombra principal: contato. MSAA 2x/4x: manter letras legíveis.
- SSIL: comparar como complemento; não chamar de GI completa.
- SDFGI: comparar o custo em 1080p/4K e observar vazamentos.
- VoxelGI: avaliar bake na sala estática, com volume limitado.
- LightmapGI: candidato de longo prazo para salas estáticas; necessita UV2 e bake em editor, com probes para personagens.
- Demo oficial de GI e demo Compositor Effects pesquisadas. A demo de compositor da Godot Foundation é MIT, atualizada em 08/07/2026, exige Godot 4.7 e está marcada como instável. Não integrar: os efeitos nativos atendem este passe sem novo compositor/dependência.
- Biblioteca de assets e GitHub consultados. CLI game-dev não está instalada: não haverá recibos dessa ferramenta; produção e medições usam Blender MCP e o Godot local, sem serviço remoto substituto.

Fontes: https://docs.godotengine.org/en/4.7/tutorials/3d/global_illumination/introduction_to_global_illumination.html ; https://docs.godotengine.org/en/4.7/classes/class_reflectionprobe.html ; https://docs.godotengine.org/en/4.7/tutorials/3d/lights_and_shadows.html ; https://github.com/godotengine/godot-demo-projects ; https://store.godotengine.org/asset/godot-foundation/compositor-effects-post-processing-demo/ . Nenhum código externo copiado nem plugin instalado.

## Etapas e orçamento

1. Captura/benchmark fixo antes de alterações, sem partida/IA: concluído em docs/patch27/before.
2. Ajustar hierarquia de luz, reflexos e sombras; comparar variantes nativas.
3. Blender: mesa de referência, material/acabamento do salão e poltrona; preservar originais no Backup e fontes isoladas.
4. Integrar somente o Classic Club como alvo inicial; conferir POV e mesa.
5. Benchmark comparável, regressão visual, gameplay, câmeras, exportação e publicação em review.

Orçamento inicial da referência: até 2 luzes com sombra, até 2 probes Once; sem física adicional; geometria estática preferencialmente abaixo de 80 mil triângulos para sala/mesa/cadeiras, excluindo personagens. Meta provisória: 1080p baixo abaixo de 8 ms e ultra abaixo de 12 ms de mediana no computador de teste; 4K ultra abaixo de 33 ms. Não é garantia para outro hardware. Memória registrada é a estimativa do renderer Godot, não medição independente de VRAM física. Tempos de CPU/GPU não são intercambiáveis: frame time medido é tempo de quadro observado.

## Riscos e decisões pendentes

Reflexo estático pode reter personagens se as máscaras forem erradas; oclusão excessiva pode esconder cartas; GI pode vazar em paredes finas; novos polígonos podem aumentar sombras/draw calls. Comparar e rejeitar alternativas sem ganho perceptível.

Pergunta opcional para a próxima etapa: depois do Classic Club, priorizar o Lounge do Barão ou o Cassino Cyber? O trabalho no primeiro alvo independe dessa resposta.
