# MULTIGAME — MISSÃO DE EVOLUÇÃO GRÁFICA DE ALTO NÍVEL

Você está trabalhando no projeto MultiGame, desenvolvido em Godot .NET/C#, com ambientes 3D, personagens estilizados em Blender, câmera 2.5D e POV, pôquer roguelike, Truco e Fodinha.

Sua missão é realizar uma EVOLUÇÃO VISUAL PROFUNDA do projeto.

Não se limite a aumentar resolução, ativar bloom ou adicionar shaders aleatórios.

Atue simultaneamente como:

- Senior Rendering Engineer
- Senior Technical Artist
- Senior Environment Artist
- Senior Lighting Artist
- Senior Material Artist
- Senior Shader Developer
- Senior Character Artist
- Senior Godot Developer
- Senior Blender Artist
- Senior VFX Artist
- Senior Optimization Engineer
- Senior UI/UX Artist
- Senior Cinematic Lighting Artist
- Senior Vibe Coder
- Graphics QA Engineer

O objetivo é transformar a apresentação atual em uma experiência visual estilizada de padrão comercial moderno, preservando a identidade do MultiGame.

A direção desejada é:

"stylized premium / high-end indie"

e NÃO:

"hiper-realismo genérico".

A evolução deve ser perceptível imediatamente ao comparar screenshots de antes e depois.

==================================================
0. REGRA PRINCIPAL
==================================================

NÃO tente melhorar os gráficos apenas aumentando:

- polígonos;
- resolução;
- sombras;
- partículas;
- bloom;
- contraste.

Qualidade visual deve surgir da combinação de:

MODELAGEM
+
MATERIAIS
+
ILUMINAÇÃO
+
SOMBRAS
+
COMPOSIÇÃO
+
PÓS-PROCESSAMENTO
+
VFX
+
CÂMERA
+
ANIMAÇÃO
+
DIREÇÃO DE ARTE.

Antes de qualquer alteração, audite o projeto.

==================================================
1. PESQUISA OBRIGATÓRIA ANTES DE IMPLEMENTAR
==================================================

Antes de iniciar cada grande etapa gráfica:

1. identificar a versão exata do Godot;
2. consultar a documentação oficial correspondente;
3. pesquisar melhorias disponíveis nas versões atuais;
4. pesquisar Godot Asset Library;
5. pesquisar GitHub;
6. pesquisar plugins open-source relevantes;
7. pesquisar shaders compatíveis;
8. analisar demos oficiais;
9. comparar técnicas alternativas;
10. verificar custo de performance;
11. verificar licença;
12. verificar manutenção recente.

Priorize nesta ordem:

1. recursos nativos do Godot;
2. soluções oficiais;
3. plugins open-source conhecidos;
4. shaders próprios;
5. implementação customizada.

Não instalar qualquer plugin cegamente.

Antes de usar plugin externo, verificar:

- compatibilidade;
- versão;
- licença;
- atividade do projeto;
- issues;
- dependências;
- impacto;
- necessidade real.

Se um plugin puder melhorar muito o projeto, avalie e utilize.

Se não houver vantagem clara, prefira solução nativa.

==================================================
2. DEFINA UM NOVO PADRÃO VISUAL
==================================================

Antes de editar assets, determine:

- contraste;
- exposição;
- saturação;
- temperatura;
- iluminação;
- rugosidade;
- cor ambiente;
- densidade de cenário;
- estilo dos materiais;
- intensidade dos reflexos;
- profundidade atmosférica;
- linguagem de luz dos personagens.

Crie uma direção visual consistente.

O jogador deve poder reconhecer MultiGame apenas olhando uma screenshot.

Evitar aparência de:

- demo técnica;
- assets desconectados;
- material plástico genérico;
- iluminação uniforme;
- cenário vazio;
- HDR exagerado;
- bloom excessivo.

==================================================
3. RENDERIZADOR
==================================================

Audite qual renderer está sendo utilizado.

Para desktop de maior qualidade, avaliar Forward+.

Aproveitar recursos modernos compatíveis quando apropriado:

- SDFGI;
- VoxelGI;
- LightmapGI;
- ReflectionProbes;
- SSAO;
- SSIL;
- SSR;
- fog volumétrico;
- tonemapping;
- glow;
- decals;
- TAA;
- FSR 2;
- anisotropic filtering;
- shadows avançadas;
- PCSS quando disponível.

NÃO habilitar tudo ao mesmo tempo.

Cada recurso precisa justificar seu custo.

==================================================
4. ILUMINAÇÃO GLOBAL
==================================================

Faça testes comparativos entre:

SDFGI
VoxelGI
LightmapGI
ReflectionProbes
SSIL

Não assumir que uma tecnologia é superior em todos os cenários.

Para salas relativamente estáticas:

avaliar LightmapGI para iluminação indireta de alta qualidade.

Para cenas mais dinâmicas:

avaliar SDFGI ou VoxelGI.

Use SSIL como complemento quando houver ganho visual perceptível.

ReflectionProbes devem ser utilizados estrategicamente.

==================================================
5. ILUMINAÇÃO DAS SALAS
==================================================

Cada sala deve possuir uma identidade de iluminação.

Não usar apenas:

DirectionalLight + WorldEnvironment.

Crie hierarquia:

KEY LIGHT
FILL LIGHT
RIM LIGHT
PRACTICAL LIGHTS
ENVIRONMENT LIGHTING.

Exemplos de fontes práticas:

- lustres;
- velas;
- abajures;
- lareira;
- letreiros;
- LEDs;
- luminárias;
- janelas;
- displays;
- máquinas.

Toda fonte visível deve parecer iluminar o espaço.

==================================================
6. PERSONAGENS
==================================================

Os personagens precisam estar perfeitamente integrados ao cenário.

Cada cadeira/posição importante deve possuir iluminação adequada para:

- rosto;
- mãos;
- cartas;
- silhueta.

Utilize rim lights discretas quando necessário.

Evite:

- rosto escuro;
- iluminação frontal chapada;
- pele/plumas sem volume;
- materiais estourados.

==================================================
7. SOMBRAS
==================================================

Revisar todas as sombras.

Investigar:

- resolução;
- shadow atlas;
- distância;
- bias;
- normal bias;
- cascades;
- PCSS;
- contato;
- estabilidade.

Priorizar sombras perceptíveis:

- personagens;
- cadeiras;
- cartas;
- mesa;
- props próximos.

Reduzir sombras desnecessárias de pequenos objetos.

==================================================
8. CONTACT SHADOWS
==================================================

Objetos devem parecer tocar superfícies.

Evitar personagens e objetos "flutuando".

Ajustar:

- SSAO;
- iluminação;
- sombras;
- posição;
- materiais.

Focar particularmente:

- pés;
- cadeiras;
- cartas;
- fichas;
- copos;
- objetos na mesa.

==================================================
9. REFLEXOS
==================================================

Implementar reflexão de maneira seletiva.

Utilizar:

- ReflectionProbes;
- SSR;
- cubemaps;
- environment reflection.

Materiais que podem responder:

- vidro;
- metal;
- madeira envernizada;
- couro;
- piso polido;
- ouro;
- latão.

Não transformar tudo em espelho.

==================================================
10. MATERIAIS PBR
==================================================

Auditar TODOS os materiais principais.

Cada material deve possuir lógica correta de:

- albedo;
- roughness;
- metallic;
- normal;
- AO;
- emission quando aplicável.

Evite valor único de roughness em tudo.

Crie variação perceptível.

Exemplo:

feltro ≠ madeira ≠ couro ≠ metal ≠ tecido ≠ pele.

==================================================
11. ROUGHNESS
==================================================

Roughness é prioridade visual.

Crie variação sutil.

Madeira:

variações de verniz e poros.

Couro:

brilho suave e irregular.

Feltro:

muito difuso.

Metal:

reflexos controlados.

Tecido:

roughness alta com microestrutura.

==================================================
12. NORMAL MAPS
==================================================

Utilizar normals para detalhes que não precisam de geometria.

Exemplos:

- tecido;
- madeira;
- couro;
- ornamentos;
- paredes;
- chão.

Não exagerar.

Normal map não deve parecer relevo artificial.

==================================================
13. DECALS
==================================================

Utilizar Decal nodes quando apropriado.

Exemplos:

- sujeira;
- desgaste;
- marcas;
- manchas;
- símbolos;
- logos;
- rachaduras;
- detalhes ambientais.

Decals devem enriquecer composição.

Não espalhar aleatoriamente.

==================================================
14. TEXEL DENSITY
==================================================

Padronizar densidade de texturas.

Um objeto pequeno não deve usar 4K enquanto uma parede gigante usa 512.

Criar critérios.

Exemplo inicial:

small props:
512–1024

medium props:
1024–2048

hero assets:
2048–4096

Ajustar conforme distância real de câmera.

==================================================
15. TEXTURAS
==================================================

Atualizar assets que apresentem:

- baixa resolução;
- compressão excessiva;
- seams;
- iluminação baked incorreta;
- contraste errado;
- bordas serrilhadas.

Usar ferramentas de geração/edição quando legalmente apropriado.

Não importar assets sem licença clara.

==================================================
16. MODELOS DE CENÁRIO
==================================================

Auditar todos os ambientes.

Eliminar impressão de:

"caixa vazia com uma mesa no centro".

Criar profundidade visual.

Utilizar:

foreground
midground
background.

Adicionar objetos com propósito.

==================================================
17. PROPS
==================================================

Criar bibliotecas reutilizáveis.

Exemplos:

- copos;
- garrafas;
- cinzeiros decorativos;
- livros;
- fichas;
- quadros;
- relógios;
- luminárias;
- cortinas;
- móveis;
- plantas;
- objetos temáticos.

Não poluir.

==================================================
18. ARQUITETURA
==================================================

As salas devem contar história.

Pergunte:

Quem usa este espaço?

Qual época representa?

Quem é o boss?

Qual classe social?

Qual clima?

Quais materiais predominam?

Como a iluminação reforça isso?

==================================================
19. BOSS ROOMS
==================================================

Cada boss importante deve possuir ambiente próprio ou variação forte.

Boss + sala + iluminação + música devem funcionar como uma unidade.

Exemplos conceituais:

luxo decadente
casino futurista
salão aristocrático
cassino clandestino
biblioteca privada
clube noturno.

==================================================
20. FOG
==================================================

Avaliar fog e volumetric fog.

Utilizar para:

- profundidade;
- separação;
- feixes;
- atmosfera.

Evitar transformar sala em neblina.

==================================================
21. VOLUMETRIC LIGHTING
==================================================

Adicionar somente onde houver fonte convincente.

Exemplos:

- janela;
- lustre;
- holofote;
- neon;
- porta aberta;
- lareira.

==================================================
22. COLOR GRADING
==================================================

Estabelecer color grading coerente.

Criar LUTs ou configurações equivalentes por ambiente quando apropriado.

Exemplos:

Classic Club:
quente, âmbar, verde profundo.

Cyber Casino:
azul/ciano com acentos magenta.

Boss:
contraste mais dramático.

Mas manter consistência do jogo.

==================================================
23. TONEMAPPING
==================================================

Configurar tonemapping conscientemente.

Evitar:

- highlights estourados;
- pretos sem detalhe;
- saturação excessiva.

Testar ACES quando apropriado.

==================================================
24. EXPOSIÇÃO
==================================================

Controle automático/estático deve evitar alterações irritantes entre planos.

Câmeras não devem mudar drasticamente brilho ao cortar.

==================================================
25. BLOOM / GLOW
==================================================

Glow deve ser seletivo.

Principalmente:

- lâmpadas;
- neon;
- magia;
- elementos especiais;
- indicadores.

NÃO usar bloom para "deixar bonito".

==================================================
26. SCREEN SPACE REFLECTIONS
==================================================

SSR pode ser útil para:

- pisos;
- madeira polida;
- superfícies brilhantes.

Mas possui limitações de screen space.

Combine com ReflectionProbes.

==================================================
27. SSAO
==================================================

SSAO deve reforçar contato.

Não deixar cantos pretos artificialmente.

==================================================
28. SSIL
==================================================

Use SSIL para detalhes de iluminação indireta dinâmica quando apropriado.

Não tratar SSIL como substituto completo de GI.

==================================================
29. SDFGI
==================================================

Avaliar SDFGI especialmente para ambientes maiores/dinâmicos.

Configurar:

- cascades;
- distance;
- energy;
- occlusion;
- performance.

==================================================
30. LIGHTMAPGI
==================================================

Para salas estáticas, testar LightmapGI cuidadosamente.

Ajustar:

- texel scale;
- quality;
- bounces;
- directional lightmap;
- probes.

Comparar visualmente e em performance.

==================================================
31. REFLECTION PROBES
==================================================

Adicionar probes por zonas.

Evitar um único probe gigantesco.

Exemplos:

- centro da sala;
- mesa;
- bar;
- área de boss.

==================================================
32. SHADERS CUSTOMIZADOS
==================================================

Criar shaders somente quando agregarem identidade.

Possibilidades:

- feltro;
- madeira;
- couro;
- metal envelhecido;
- hologramas;
- neon;
- vidro;
- cartas especiais;
- boss materials;
- UI 3D.

==================================================
33. SHADER DE FELTRO
==================================================

O feltro é central na composição.

Criar resposta visual com:

- roughness alta;
- normal sutil;
- fibras;
- variação micro;
- contraste controlado.

Evitar ruído subpixel.

==================================================
34. MADEIRA
==================================================

Melhorar:

- grain;
- roughness;
- edge wear sutil;
- verniz;
- reflexão.

==================================================
35. COURO
==================================================

Implementar:

- normal suave;
- roughness;
- highlights;
- costura.

==================================================
36. METAL
==================================================

O ouro/latão deve parecer metal.

Metallic correto.

Roughness coerente.

Reflexos.

Evitar amarelo plástico.

==================================================
37. VIDRO
==================================================

Usar transparência com cuidado.

Evitar custo excessivo.

Priorizar objetos próximos ou hero assets.

==================================================
38. CARTAS
==================================================

Elevar dramaticamente a qualidade das cartas.

Revisar:

- espessura;
- bevel;
- normais;
- textura;
- impressão;
- roughness;
- reflexo;
- bordas.

Cartas devem parecer objetos físicos.

==================================================
39. FICHAS
==================================================

Adicionar:

- bevel;
- material;
- microdetalhes;
- relevo;
- sombras.

==================================================
40. CÂMERA
==================================================

A qualidade gráfica depende da composição.

Revisar:

- FOV;
- altura;
- foco;
- clipping;
- enquadramento;
- movimento;
- transições.

Não colocar câmera onde assets ficam feios.

==================================================
41. DEPTH OF FIELD
==================================================

DOF pode ser utilizado somente em momentos cinematográficos.

Evitar durante gameplay se prejudicar leitura.

Exemplos:

- boss intro;
- vitória;
- close-up.

==================================================
42. MOTION BLUR
==================================================

Avaliar com extremo cuidado.

Não implementar se prejudicar clareza.

Em jogo de cartas, provavelmente usar pouco ou nenhum motion blur durante gameplay.

==================================================
43. ANTI-ALIASING
==================================================

Comparar:

- MSAA;
- TAA;
- upscale temporal;
- FSR2.

Escolher conforme perfil.

Evitar serrilhamento em:

- cabelos;
- cartas;
- cadeira;
- silhuetas.

==================================================
44. UPSCALING
==================================================

Avaliar FSR 2 para perfis que precisem de ganho de performance.

Criar opções de:

Native
Quality
Balanced
Performance

se fizer sentido.

Não obrigar upscaling em hardware capaz.

==================================================
45. RESOLUÇÃO INTERNA
==================================================

Manter UI nítida.

Render 3D pode usar resolução dinâmica ou render scale.

Testar:

720p
1080p
1440p
4K.

==================================================
46. PERFIS GRÁFICOS
==================================================

Criar perfis claros:

BAIXO
MÉDIO
ALTO
ULTRA.

Exemplo:

BAIXO
- lightmaps
- shadows reduzidas
- sem SSR
- sem volumetric fog
- SSAO reduzido

MÉDIO
- melhor sombra
- SSAO
- probes

ALTO
- SDFGI/VoxelGI conforme cenário
- SSIL
- SSR seletivo
- volumetric fog

ULTRA
- maior qualidade GI
- melhores sombras
- maior resolução
- SSIL completo
- volumétrico refinado.

Esses são pontos iniciais, não valores fixos.

Benchmark obrigatório.

==================================================
47. LOD
==================================================

Implementar LOD onde for útil.

Personagens e props distantes não precisam usar o mesmo detalhamento.

==================================================
48. VISIBILITY RANGES
==================================================

Usar visibility ranges para pequenos props.

==================================================
49. OCCLUSION CULLING
==================================================

Avaliar occlusion culling nas salas.

Objetos atrás de paredes não precisam ser renderizados.

==================================================
50. INSTANCING
==================================================

Props repetidos devem ser instanciados eficientemente.

==================================================
51. MESH LOD
==================================================

Garantir LOD apropriado quando possível.

==================================================
52. MATERIAIS COMPARTILHADOS
==================================================

Evitar milhares de materiais únicos.

Criar material library.

==================================================
53. TEXTURE ATLASES
==================================================

Avaliar atlas para props menores.

==================================================
54. PARTICLES
==================================================

Use partículas com intenção.

Exemplos:

- poeira;
- fumaça;
- fagulhas;
- lareira;
- hologramas;
- confete raro.

Evitar excesso.

==================================================
55. VFX DE CARTAS
==================================================

Efeitos especiais devem acompanhar gameplay.

Exemplos:

manilha
boss card
combinação rara
vitória
truco.

Evite transformar jogo em cassino mobile genérico.

==================================================
56. AMBIENT VFX
==================================================

Adicionar vida:

- poeira iluminada;
- fumaça;
- vapor;
- faíscas;
- chama;
- telas;
- chuva exterior;
- reflexos animados.

De maneira sutil.

==================================================
57. ANIMAÇÃO AMBIENTAL
==================================================

Ambientes não devem parecer congelados.

Adicionar:

- relógio;
- chama;
- neon;
- cortina;
- ventoinha;
- holograma;
- máquinas;
- partículas.

==================================================
58. AUDIOVISUAL SYNC
==================================================

Sincronizar eventos visuais com:

- som;
- música;
- animação.

Exemplo:

carta toca mesa
→ som
→ shadow/contact
→ bounce
→ reação.

==================================================
59. INTERFACE
==================================================

Elevar a UI junto com os gráficos.

Revisar:

- tipografia;
- sombras;
- ícones;
- botões;
- animações;
- transições.

Não deixar UI parecer de outro jogo.

==================================================
60. HDR-LIKE PRESENTATION
==================================================

Mesmo sem HDR real, preservar contraste e highlights.

Evitar clipping.

Se HDR real for suportado de forma adequada no projeto/plataforma, pesquisar antes de implementar.

==================================================
61. BLENDER
==================================================

Toda melhoria de assets físicos deve acontecer no Blender:

- bevel;
- normals;
- retopologia;
- UV;
- materiais;
- bake;
- LOD;
- modelagem.

Não tentar resolver asset ruim só com shader.

==================================================
62. UVs
==================================================

Auditar:

- overlaps;
- seams;
- escala;
- padding;
- orientação.

==================================================
63. BAKING
==================================================

Quando útil, gerar:

- normal;
- AO;
- curvature;
- roughness support masks.

==================================================
64. EDGE BEVEL
==================================================

Quase nenhum objeto real possui bordas matematicamente infinitas.

Adicionar bevel apropriado aos assets principais.

Principalmente:

- mesa;
- cadeira;
- armários;
- fichas;
- molduras;
- objetos hero.

==================================================
65. WEIGHTED NORMALS / NORMALS
==================================================

Corrigir shading em hard-surface.

Evitar faceting ruim.

==================================================
66. PERSONAGENS
==================================================

Melhorias de personagem devem incluir:

- modelagem;
- materiais;
- olhos;
- roupa;
- cabelo/pelos;
- rig;
- animações.

Personagem bonito com material ruim continua parecendo barato.

==================================================
67. OLHOS
==================================================

Criar olhos com leitura convincente.

Separar quando necessário:

- sclera;
- iris;
- pupil;
- highlight.

Evitar olho totalmente emissivo/plástico.

==================================================
68. CABELO
==================================================

Escolher técnica estilizada coerente.

Evitar hair systems muito pesados sem necessidade.

==================================================
69. PELE
==================================================

Para personagens humanos, avaliar SSS apenas se houver ganho visível e custo aceitável.

Não habilitar só porque existe.

==================================================
70. PÊLOS / PENAS
==================================================

Personagens animais devem possuir leitura convincente de pelos/penas por:

- geometria;
- normal;
- roughness;
- cards;
- shaders estilizados.

Conforme distância.

==================================================
71. QUALITY GATE
==================================================

Para cada grande alteração:

ANTES
VS
DEPOIS.

Comparar:

- screenshot;
- frame time;
- VRAM;
- draw calls;
- triângulos;
- qualidade.

==================================================
72. SCREENSHOT QA
==================================================

Criar screenshots automáticas de:

- cada sala;
- cada boss;
- cada personagem;
- POV;
- overhead;
- 1080p;
- 4K;
- perfis gráficos.

==================================================
73. VISUAL REGRESSION
==================================================

Criar sistema para detectar regressões visuais.

Não confiar só em testes de código.

==================================================
74. GPU PROFILING
==================================================

Use profiler do Godot.

Identificar:

- frame time;
- rendering;
- shadow cost;
- GI;
- particles;
- post-processing.

==================================================
75. CPU PROFILING
==================================================

Não negligenciar CPU.

Animações, scripts, particles e visibility podem afetar CPU.

==================================================
76. PLUGINS
==================================================

Pesquisar plugins que possam ajudar em:

- terrain;
- shader tooling;
- post processing;
- decals;
- VFX;
- profiling;
- asset pipeline;
- procedural generation;
- environment tools.

Somente integrar após auditoria.

==================================================
77. SHADER LIBRARIES
==================================================

Pesquisar shaders Godot open-source e exemplos oficiais.

Não copiar código incompatível ou sem licença.

Adaptar.

==================================================
78. DEMOS GODOT
==================================================

Estudar projetos oficiais de demonstração relevantes.

Particularmente:

- global illumination;
- 3D lighting;
- materials;
- particles;
- shaders;
- post processing.

==================================================
79. INTERNET
==================================================

Não limitar pesquisa ao primeiro resultado.

Cruzar:

- docs oficiais;
- GitHub;
- Asset Library;
- issues;
- discussions;
- benchmarks.

Priorizar documentação oficial quando houver conflito.

==================================================
80. NÃO USAR MODA SEM TESTE
==================================================

Não implementar uma técnica só porque aparece em vídeos.

Cada melhoria deve passar pelo teste:

"o jogador percebe?"

"quanto custa?"

"quebra algo?"

==================================================
81. ORDEM DE EXECUÇÃO
==================================================

PASSO 1 — Auditoria visual

PASSO 2 — Benchmark atual

PASSO 3 — Iluminação

PASSO 4 — Materiais

PASSO 5 — cenário

PASSO 6 — personagens

PASSO 7 — VFX

PASSO 8 — pós-processamento

PASSO 9 — escalabilidade

PASSO 10 — QA

==================================================
82. PRIMEIRO PASSE
==================================================

Primeiramente não refaça tudo.

Escolha UMA sala representativa.

Transforme-a em um "visual target".

Use esta sala para definir:

- luz;
- materiais;
- GI;
- pós;
- props;
- qualidade.

Somente depois aplique ao restante.

==================================================
83. GOLDEN SCENE
==================================================

Crie uma "Golden Scene".

A Golden Scene representa a qualidade final desejada.

Utilize:

- personagem principal;
- mesa;
- cartas;
- cadeira;
- iluminação;
- props;
- ambiente completo.

Se a Golden Scene não estiver convincente, não escale o sistema.

==================================================
84. GOLDEN CHARACTER
==================================================

Escolha um personagem já forte como referência.

Use-o para definir:

- material;
- shading;
- luz;
- qualidade geométrica.

Depois leve os personagens fracos ao mesmo nível.

==================================================
85. QUALITY BUDGET
==================================================

Crie orçamento por categoria:

GPU
CPU
VRAM
draw calls
triangles
lights
shadows
particles.

==================================================
86. ULTRA NÃO SIGNIFICA DESPERDÍCIO
==================================================

Mesmo Ultra deve ser otimizado.

Aumentar qualidade onde a diferença for visível.

==================================================
87. BAIXO DEVE CONTINUAR BONITO
==================================================

O preset baixo deve remover custo, não identidade.

Manter:

- composição;
- materiais principais;
- paleta;
- silhuetas.

==================================================
88. MAPAS DIFERENTES
==================================================

Não aplicar exatamente o mesmo Environment em todas as salas.

Cada sala deve possuir identidade própria.

==================================================
89. LUZ POR PERSONAGEM
==================================================

Se necessário, criar pequenas diferenças de iluminação conforme posição.

Mas evitar aparência artificial de estúdio.

==================================================
90. CARTAS COMO HERO ASSET
==================================================

As cartas ficam constantemente na tela.

Trate-as como hero assets.

==================================================
91. MESA COMO HERO ASSET
==================================================

A mesa é provavelmente o objeto mais visto do jogo.

Investir muito nela.

==================================================
92. POLTRONAS
==================================================

Poltronas devem ter:

- tecido/couro convincente;
- costura;
- bevel;
- normal;
- contato.

==================================================
93. PAREDES E PISO
==================================================

Evitar superfícies completamente planas.

Usar:

- molduras;
- painéis;
- detalhes;
- roughness;
- normals;
- decals.

==================================================
94. COMPOSIÇÃO
==================================================

Use iluminação para direcionar o olho para:

1. cartas;
2. personagem relevante;
3. informações do jogo.

Não deixar props secundários mais brilhantes que o gameplay.

==================================================
95. CINEMÁTICAS
==================================================

Boss intros podem ativar temporariamente:

- DOF;
- fog;
- luz especial;
- animação;
- pós-processamento;

e depois retornar ao gameplay.

==================================================
96. TRANSIÇÕES
==================================================

Mudanças de ambiente devem usar transições suaves.

Evitar:

- pop;
- luz mudando instantaneamente;
- GI pulando;
- música visual desconectada.

==================================================
97. DOCUMENTAÇÃO
==================================================

Documentar:

- técnica;
- motivo;
- custo;
- configuração;
- resultado;
- problemas.

==================================================
98. NÃO INVENTAR RESULTADO
==================================================

Não dizer:

"gráficos AAA"

"ray tracing"

"GI avançado perfeito"

"otimizado"

sem evidência.

==================================================
99. SAÍDA OBRIGATÓRIA DA IA
==================================================

Após auditoria, entregar:

A. problemas encontrados;
B. ranking de impacto visual;
C. tecnologias candidatas;
D. plugins encontrados;
E. riscos;
F. plano por etapas;
G. primeiro visual target;
H. implementação;
I. benchmark antes/depois;
J. screenshots antes/depois.

==================================================
100. OBJETIVO FINAL
==================================================

O MultiGame deve passar da sensação de:

"jogo independente funcional com assets 3D"

para:

"jogo estilizado premium com direção artística consistente".

Quando o jogador olhar a tela, deve perceber:

- profundidade;
- atmosfera;
- materiais convincentes;
- iluminação cuidadosamente dirigida;
- personagens integrados;
- cartas físicas;
- ambientes vivos;
- excelente composição;
- identidade própria.

A melhoria deve ser perceptível sem precisar explicar tecnicamente o que mudou.

Não priorize tecnologia.

Priorize resultado visual.