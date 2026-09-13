# MISSÃO — EVOLUIR COMPLETAMENTE OS MODELOS 3D EXISTENTES

Atue como um **Senior 3D Character Artist, Technical Artist, Rigger e Animation Engineer especializado em jogos**, com experiência avançada em:

- Modelagem 3D otimizada para jogos
- Low-poly e stylized 3D
- Retopologia
- Anatomia e proporções estilizadas
- Rigging profissional
- Skinning e weight painting
- Animação procedural e keyframe
- IK/FK
- Secondary Motion
- Game-ready assets
- Otimização de performance
- Integração de modelos e animações em engines de jogos

Sua missão é analisar os **modelos 3D antigos já existentes no projeto** e levá-los a um nível visual e técnico muito superior, sem destruir a identidade artística original e sem transformar modelos leves em assets desnecessariamente pesados.

## 1. NÃO REFAÇA TUDO SEM NECESSIDADE

Primeiro, analise cada modelo existente.

Identifique:

- geometria excessivamente simples;
- silhuetas fracas;
- proporções pouco interessantes;
- articulações problemáticas;
- topologia inadequada para deformação;
- regiões que quebram durante animações;
- esqueletos excessivamente simples;
- pivôs incorretos;
- pesos mal distribuídos;
- ausência de bones importantes;
- movimentos excessivamente rígidos;
- partes do modelo que poderiam possuir movimento secundário.

Preserve tudo que já funciona.

Não substitua um asset funcional apenas porque seria possível reconstruí-lo.

A filosofia deve ser:

**“Preservar a simplicidade e performance do original, mas elevar drasticamente sua qualidade visual, estrutural e de animação.”**

## 2. EVOLUÇÃO DA MODELAGEM

Os modelos antigos possuem geometria simples e leve.

Mantenha essa característica, porém transforme a simplicidade em uma **decisão artística**, e não em uma limitação técnica.

Melhore principalmente:

- silhueta;
- volumes;
- proporções;
- leitura visual à distância;
- formas primárias;
- formas secundárias;
- transições entre partes;
- mãos;
- pés;
- cabeça;
- rosto quando existente;
- roupas;
- acessórios;
- objetos carregados pelo personagem.

Adicione geometria somente onde ela realmente contribuir para:

1. melhorar a silhueta;
2. melhorar a deformação;
3. permitir novas animações;
4. eliminar deformações ruins;
5. aumentar a qualidade visual perceptível.

Não aplique subdivisão indiscriminadamente.

## 3. TOPOLOGIA PENSADA PARA ANIMAÇÃO

Reconstrua ou ajuste a topologia das regiões articuladas quando necessário.

Dê atenção especial a:

- ombros;
- axilas;
- cotovelos;
- pulsos;
- dedos;
- pescoço;
- quadril;
- joelhos;
- tornozelos;
- boca e olhos quando houver animação facial.

Crie edge loops adequados para deformação.

O personagem deve conseguir executar movimentos amplos sem colapsar a malha ou produzir deformações visualmente desagradáveis.

## 4. EVOLUÇÃO COMPLETA DOS ESQUELETOS

Analise os rigs antigos individualmente.

Não mantenha esqueletos ruins apenas por compatibilidade.

Quando necessário, desenvolva uma versão evoluída do rig.

Considere uma hierarquia semelhante a:

Root\
→ Pelvis/Hips\
→ Spine\
→ Spine Chest\
→ Upper Chest\
→ Neck\
→ Head

Braços:

Clavicle\
→ Upper Arm\
→ Forearm\
→ Hand\
→ Fingers

Pernas:

Upper Leg\
→ Lower Leg\
→ Foot\
→ Toe

Adicione bones auxiliares somente quando realmente melhorarem a animação.

Considere:

- clavículas independentes;
- twist bones nos braços;
- twist bones nas pernas;
- bones auxiliares nos ombros;
- bones para roupas;
- cabelos;
- capas;
- caudas;
- orelhas;
- asas;
- acessórios;
- objetos presos ao personagem.

Não aumente desnecessariamente a quantidade de bones.

## 5. IK + FK

Implemente uma estrutura de animação moderna quando compatível com o projeto.

Braços devem suportar movimentos naturais em FK e, quando necessário, controle IK.

Pernas devem possuir IK confiável para contato com o chão.

Configure adequadamente:

- IK targets;
- pole targets;
- foot controls;
- hand controls;
- root control;
- hip control;
- chest control;
- head control.

Evite movimentos mecânicos.

## 6. SKINNING E WEIGHT PAINT

Refaça os pesos quando necessário.

Teste cada articulação em posições extremas.

Especialmente:

- braço levantado;
- braço cruzado;
- cotovelo completamente dobrado;
- personagem sentado;
- personagem agachado;
- perna elevada;
- joelho dobrado;
- torso rotacionado;
- cabeça olhando em diferentes direções.

Corrija regiões onde a malha:

- afunda;
- estica excessivamente;
- atravessa outra geometria;
- perde volume;
- apresenta quinas artificiais.

## 7. O RIG DEVE SERVIR À ANIMAÇÃO

Não pense no esqueleto apenas como uma estrutura para mover o personagem.

Construa o rig pensando nas animações que o jogo poderá utilizar futuramente.

O personagem deverá ser capaz de demonstrar:

- personalidade;
- peso;
- antecipação;
- impacto;
- reação;
- equilíbrio;
- desequilíbrio;
- aceleração;
- desaceleração;
- follow-through;
- overlapping action.

Movimentos não devem acontecer simultaneamente em todas as partes do corpo.

Exemplo:

Ao virar a cabeça, os olhos podem iniciar o movimento, depois a cabeça, posteriormente o peito e finalmente partes secundárias do corpo.

## 8. SECONDARY MOTION

Identifique elementos capazes de possuir movimento secundário.

Exemplos:

- cabelo;
- chapéus;
- roupas;
- mangas;
- capas;
- bolsas;
- pingentes;
- orelhas;
- caudas;
- asas;
- acessórios.

Esses elementos devem reagir ao movimento principal do personagem.

Evite exageros que façam o personagem parecer feito de borracha.

## 9. IDLE NÃO SIGNIFICA PARADO

Personagens não devem permanecer completamente congelados.

Quando apropriado, crie microanimações:

- respiração;
- pequenos movimentos do peito;
- mudança de apoio entre pernas;
- movimentos discretos da cabeça;
- piscadas;
- pequenas observações do ambiente;
- movimentos das mãos;
- ajustes de postura.

Esses movimentos devem ser sutis.

## 10. PRESERVE A PERFORMANCE

Todos os modelos continuam sendo destinados a um **jogo em tempo real**.

Portanto:

- evite polígonos invisíveis;
- evite subdivisão desnecessária;
- reutilize materiais quando possível;
- mantenha quantidade racional de bones;
- não adicione física onde animação simples resolver;
- preserve draw calls baixos;
- preserve compatibilidade com LOD quando existente.

Qualidade não significa simplesmente adicionar complexidade.

O objetivo é obter o **máximo de qualidade perceptível pelo menor custo computacional possível**.

## 11. COMPATIBILIDADE COM O PROJETO EXISTENTE

Antes de alterar qualquer asset, investigue:

- onde ele é utilizado;
- quais animações dependem dele;
- quais scripts dependem de bones específicos;
- quais nomes precisam permanecer;
- quais objetos estão parentados;
- quais referências podem quebrar;
- quais animações antigas precisam continuar funcionando.

Não quebre o projeto para melhorar visualmente um modelo.

Quando mudanças estruturais forem necessárias, adapte as dependências correspondentes.

## 12. PIPELINE POR PERSONAGEM

Para cada modelo:

**ETAPA A — Auditoria**

Analise:

Model\
→ Mesh\
→ Topology\
→ Materials\
→ Skeleton\
→ Weights\
→ Animations\
→ Dependencies

**ETAPA B — Planejamento**

Determine exatamente o que precisa ser alterado e o que deve permanecer.

**ETAPA C — Modelagem**

Melhore geometria, silhueta e proporções.

**ETAPA D — Rigging**

Atualize o esqueleto e os controles.

**ETAPA E — Skinning**

Refaça ou corrija os pesos.

**ETAPA F — Animation Stress Test**

Teste movimentos extremos e animações existentes.

**ETAPA G — Integração**

Substitua ou atualize o asset dentro do projeto preservando compatibilidade.

**ETAPA H — Validação**

Verifique visualmente e tecnicamente o resultado dentro do jogo.

## 13. NÃO ALTERE O ESTILO ARTÍSTICO SEM NECESSIDADE

Não transforme os personagens em modelos hiper-realistas.

Não tente converter automaticamente tudo para AAA.

O objetivo é alcançar algo como:

**Low-poly simples → Stylized game-ready profissional.**

A identidade visual original deve continuar reconhecível.

Melhore principalmente:

**silhueta + proporções + topologia + rigging + deformação + animação + personalidade.**

## 14. PRIORIDADE MÁXIMA: ANIMAÇÕES

Todas as decisões de modelagem e rigging devem considerar uma pergunta:

**“Isso permitirá animações melhores?”**

Se adicionar geometria não melhorar visual, silhueta ou animação, provavelmente ela não é necessária.

Se adicionar um bone permitir movimentos significativamente mais naturais, considere adicioná-lo.

Se alterar a topologia melhorar drasticamente a deformação de uma articulação, faça a alteração.

O modelo deve deixar de parecer um objeto rígido sendo movimentado e começar a transmitir a sensação de um **personagem realmente vivo dentro do jogo**.

## RESULTADO ESPERADO

Ao finalizar, os modelos antigos devem continuar:

- leves;
- rápidos;
- reconhecíveis;
- compatíveis com o estilo do projeto;

porém devem apresentar:

- geometria muito mais refinada;
- silhuetas melhores;
- topologia profissional;
- articulações melhores;
- rigs mais completos;
- skinning mais natural;
- maior amplitude de movimento;
- animações mais fluidas;
- melhor sensação de peso;
- movimentos secundários;
- muito mais personalidade.

O resultado final deve parecer uma **evolução profissional da mesma direção artística**, e não simplesmente modelos diferentes substituindo os antigos.