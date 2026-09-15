# Música e noite

## Música por contexto e iluminação noturna — 13/09/2026

Base 87813ad preservada em Backup 52847f4. Música escolhida nas configurações agora afeta apenas o menu. Partidas usam a música do mapa; roguelike usa a do boss, protegida contra chamadas do mapa e da tensão. Ao sair para o menu ou entrar em outra modalidade, o contexto muda explicitamente. Teste tools/audio_night_smoke.gd: 10 verificações de transições aprovadas, incluindo mudar a seleção durante partida e retornar ao menu.

México: GLB recebido tinha 24 materiais cinza padrão (baseColor 0.8), sem os valores PBR definidos no script. Reparo executado no Blender MCP 9876 com importação em cena temporária, materiais reconstruídos e exportação candidata separada; cena original MexicoRecuerdosScene restaurada. 176 malhas antes/depois, 24 cores distintas, 3 materiais emissivos. Restaurados cor, rugosidade, metalicidade e emissão; não foram inventadas texturas bitmap nem alterada a topologia. Corrigida a busca do BSDF por tipo no gerador, evitando depender do nome localizado do nó. Candidato temporário em temp/audio-night; original preservado em Backup. CLI game-dev não estava no PATH; reparo executado pelo bridge local existente, sem geração externa.

Iluminação México: redução da luz direcional e ambiente, preenchimento azul noturno, menor alcance da luminária e menor energia no altar/varanda/luar. Preenchimento local mantém cartas e personagens legíveis. Sem novos efeitos pesados. Capturas runtime OpenGL e verificações em docs/audio-night-qa; build zero erros/avisos. Aviso preexistente de ObjectDB no teardown persiste. Executável local exportado como Windows Debug, pois falta o template Release x86_64 neste PC.


Reprodução: perfil APPDATA isolado e MULTIGAME_QA_APPDATA definidos; Godot .NET 4.7.2 com --path C:/workspace/multigame --rendering-method gl_compatibility --audio-driver Dummy --script res://tools/audio_night_smoke.gd. O teste usa o player de áudio real com saída Dummy: valida seleção/transições, não constitui avaliação auditiva de volume.
