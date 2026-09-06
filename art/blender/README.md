# Elenco do clube — fontes Blender

Oito personagens originais construídos no Blender 5.2.1, com formas arredondadas para câmera ortográfica 2.5D. Corvo e Onça reinterpretam o elenco pixel art da primeira revisão.

Abra qualquer `.blend` desta pasta no Blender. Cada arquivo contém modelo, materiais, grupos articulados, cinco trilhas NLA, câmera e iluminação de retrato. Os grupos animados são corpo, cabeça e braços; não há dependência de um serviço externo ou de plugin no Blender.

- Jogáveis: Nina, Bento, Corvo, Onça, Iara (capivara) e Zeca (raposa).
- Bosses: Barão da Meia-Noite (coruja) e Dama de Copas (serpente).
- Animações exportadas: `entrance`, `truco`, `victory`, `boss_intro`, `flourish`.
- Runtime: `assets/models/club/*.glb`; retratos PNG ao lado dos GLBs.

O gerador reproduzível é `tools/build_blender_cast.py`. Rodá-lo **recria os oito fontes e exports**; preserve edições manuais em outra cópia antes de regenerar. Para editar um único modelo manualmente, exporte só seu GLB com animações por NLA Tracks, mantendo os nomes das trilhas.

O **Patch 8** refina os fontes recebidos pelo servidor Blender MCP. Os arquivos `*_mcp.blend` contêm os oito personagens com mais geometria e acabamento, além dos novos gestos de Corvo, Onça, Barão e Dama; `club_clock.blend` contém o relógio e seu pêndulo. Reaplique `tools/refine_club_mcp.py` e depois `tools/polish_cast_mcp.py` após o gerador base. Na exportação manual, use **Current Frame**, num frame dentro das faixas NLA (por exemplo, 48), para manter a cabeça e os braços em repouso. Consulte o procedimento e histórico no [patch cumulativo](../../AI_DEV_PATCH_NOTES.md).

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python tools/build_blender_cast.py
```

Referência técnica utilizada: [exportador glTF oficial do Blender](https://docs.blender.org/api/main/bpy.ops.export_scene.html). Os `.blend` ficam fora da importação do Godot por `art/.gdignore`; o jogo carrega apenas GLB e PNG.

O **Patch 9** acrescenta `*_fidelity.blend`: fontes atuais dos oito personagens, quatro salas e poltrona. `tools/refine_fidelity_mcp.py` parte dos personagens `_mcp.blend` e dos GLBs preservados em `fidelity-inputs`, refina a geometria de pequenos detalhes dos rostos e arredonda quinas arquitetônicas. O servidor MCP deve estar ativo na porta 9876. Execute com `python tools/blender_bridge.py execute_code --code-file tools/refine_fidelity_mcp.py --timeout 1200`. A cena aberta é restaurada ao terminar. Reexecuções recriam os outputs a partir desses inputs, por isso preserve alterações manuais em cópias separadas antes de regenerar. Medidas em faces Blender no manifesto `assets/models/club/fidelity-pass.json`; não confundir com triângulos efetivamente desenhados pelo LOD do Godot.
