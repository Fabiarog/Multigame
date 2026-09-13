# Classic Club — referência visual e validação do complemento 27

## Escopo e origem

A missão recebida foi preservada em [MISSAO_VISUAL_PREMIUM.md](MISSAO_VISUAL_PREMIUM.md). Auditoria, ranking de impacto, pesquisa de tecnologias/plugins, riscos e plano estão em [PLANO_VISUAL27.md](PLANO_VISUAL27.md). O alvo deste passe é Classic Club com Corvo, mesa, cadeiras, cartas e cenário completo. Não é a conclusão da evolução de todos os personagens e ambientes.

A base visual anterior é a1981dd. Backup remoto 8f73b34 preserva o estado recebido; main permanece em 58b85b8. Durante o trabalho, b12c34f incorporou a integração visual e alterações paralelas de personagens/música. A validação final usa uma cópia isolada dessa base, com o material corrigido da carta e verificações de regressão atualizadas. Alterações musicais posteriores na pasta principal não fazem parte desta medição.

## O que mudou de fato

- Blender 5.2.1 LTS pelo MCP local 9876, em cenas temporárias que restauram a cena aberta. Fontes editáveis em `art/blender/patch27`.
- Mesa com borda oval de couro, frisos contínuos de latão, costura representada por um filete fino e madeira com bevel. Não há entalhes esculpidos novos. São 8.448 vértices / 16.888 triângulos na fonte.
- Carta com cantos arredondados e bevel na espessura. Suas proporções são estilizadas para leitura na mesa; não representam a espessura física de uma carta real.
- Sala: 266 objetos agrupados em seis zonas estáticas, preservando 30.404 vértices / 15.856 triângulos de fonte. Cadeira: 2.952 vértices / 1.564 triângulos; acabamento de material, sem reconstrução geométrica.
- Parede, livros, tapete, madeira e veludo com cores/rugosidades recalibradas. Feltro mantém shader procedural. Não foram produzidos novos mapas AO/normal nem UV2.
- Luzes de acento por assento, sombra principal com bias menor e reflexão estática mascarada para excluir atores/cartas. Atlas global limitado a quatro reflexos de 128 pixels.
- Correções: cadeiras e luzes acompanham sala/assentos; intensidade própria do Cyber preservada; sombra do baralho excluída da animação de cartas e sombras de contato sem projetar outra sombra.

## Método de comparação

Godot **4.7.2.stable.mono.official.ed1daf0bf**, Forward+/Vulkan, **NVIDIA GeForce RTX 3050**. Dois Corvos, 18 cartas, mesma composição, perfis baixo/ultra, 1920×1080 e 3840×2160, POV/mesa. VSync desativado, aquecimento antes de cada amostra e 90 intervalos reais de quadros; mediana e percentil 95 registrados. Não equivale a um perfil separado de CPU e GPU.

As capturas mantêm composição, mas partículas e o sistema operacional impedem identidade de pixels entre execuções. Editor aberto e atividades externas não são controlados; diferenças pequenas podem ser ruído. Os números descrevem este computador e esta cena, não todos os modos nem hardware mínimo. O benchmark não testa animações: elas são verificadas separadamente.

`render_memory_bytes` é a estimativa de recursos do renderer. Não é leitura independente de VRAM física. `process_ms` é uma amostra pontual do monitor e não deve ser usada como resultado de otimização de CPU.

## Técnicas de iluminação

Comparação incremental em 1080p ultra: a variante `none` remove o probe; as demais mantêm SSAO/SSR do perfil e acrescentam a técnica indicada. SDFGI e SSIL são desativados entre variantes. VoxelGI é preparado em volume de 28×10×24, subdivisão 64; SDFGI usa célula mínima 0,3 e energia 0,7; SSIL, intensidade 0,6. A memória da execução sequencial pode reter alocações anteriores.

**Escolha de produção: ReflectionProbe Once.** Oferece leitura de metal/couro com custo moderado. SSIL acrescentou custo sem ganho suficiente para este enquadramento. A configuração SDFGI testada escureceu demasiadamente o fundo; VoxelGI alterou bastante os metais e exigiu custo adicional. Esses resultados rejeitam as configurações testadas, não as tecnologias em geral.

**LightmapGI ainda não testado:** falta UV2 e uma cena de bake estática no editor. Não existe resultado de bake a apresentar. O próximo ciclo deve comparar essa opção antes de expandir a solução para todas as salas.

Pesquisa oficial: [comparação de GI](https://docs.godotengine.org/en/4.7/tutorials/3d/global_illumination/introduction_to_global_illumination.html), [ReflectionProbe](https://docs.godotengine.org/en/4.7/classes/class_reflectionprobe.html), [LightmapGI](https://docs.godotengine.org/en/4.7/classes/class_lightmapgi.html), [demos oficiais MIT](https://github.com/godotengine/godot-demo-projects). A [demo Compositor Effects da Godot Foundation](https://store.godotengine.org/asset/godot-foundation/compositor-effects-post-processing-demo/) foi encontrada, mas não integrada: dependência desnecessária neste passe e marcada instável na consulta. Nenhum plugin externo instalado ou código de terceiros copiado.

## Reprodução

Definir `DOTNET_ROOT` para o SDK .NET 8 e `GODOT_BIN` para o executável Godot .NET. Compilar antes de executar. `tools/golden_smoke.ps1` aceita `-Mode regression`, `-Mode scene` e `-Mode gi`, com `-OutputDirectory` opcional; isola os saves. `tools/visual_smoke.ps1` verifica regras/interface, e `-CameraOnly` verifica câmeras. Executar medições sequencialmente, sem renderizações Blender simultâneas.

## Limites e próxima etapa

Revisar a emissão da lareira, contato das cadeiras e postura dos personagens antes de considerar a direção visual encerrada. Aumentar polígonos indiscriminadamente não resolve esses pontos. Preparar UV2/LightmapGI; comparar deslocamento de câmera e iluminação em partida longa; medir CPU/memória de sistema e multiplayer entre máquinas. As correções de Onça/Morgana recebidas em paralelo não foram auditadas geometricamente neste complemento; não generalizar a evidência de dois Corvos para o elenco inteiro.
