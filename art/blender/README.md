# Elenco do clube — fontes Blender

Oito personagens originais construídos no Blender 5.2.1, em linguagem de figuras poligonais para câmera ortográfica 2.5D. Corvo e Onça reinterpretam o elenco pixel art da primeira revisão.

Abra qualquer `.blend` desta pasta no Blender. Cada arquivo contém modelo, materiais, grupos articulados, cinco trilhas NLA, câmera e iluminação de retrato. Os grupos animados são corpo, cabeça e braços; não há dependência de um serviço externo ou de plugin no Blender.

- Jogáveis: Nina, Bento, Corvo, Onça, Iara (capivara) e Zeca (raposa).
- Bosses: Barão da Meia-Noite (coruja) e Dama de Copas (serpente).
- Animações exportadas: `entrance`, `truco`, `victory`, `boss_intro`, `flourish`.
- Runtime: `assets/models/club/*.glb`; retratos PNG ao lado dos GLBs.

O gerador reproduzível é `tools/build_blender_cast.py`. Rodá-lo **recria os oito fontes e exports**; preserve edições manuais em outra cópia antes de regenerar. Para editar um único modelo manualmente, exporte só seu GLB com animações por NLA Tracks, mantendo os nomes das trilhas.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python tools/build_blender_cast.py
```

Referência técnica utilizada: [exportador glTF oficial do Blender](https://docs.blender.org/api/main/bpy.ops.export_scene.html). Os `.blend` ficam fora da importação do Godot por `art/.gdignore`; o jogo carrega apenas GLB e PNG.
