# Fontes Blender — Patch 26

Onze fontes editáveis com cena isolada e imagens dos GLBs existentes. Base imutável: `10c4b4d18ece647bd52cc42e3f50cad658ec8f5c`. Não sobrescrever edições manuais ao executar o gerador novamente; ele recria os arquivos `_refined.blend`.

Consulte `docs/PATCH26_VALIDACAO.md` para auditoria, evidências e limites. Controles IK são de autoria, com influência zero, e precisam de ajuste antes de animar. O jogo recebe somente os clipes exportados e ossos deformadores.

Os arquivos `_original.glb`, `_refined.glb` e PNGs individuais são staging local ignorado. Originais são recuperados pelo gerador do commit fixo; resultados de execução ficam em `assets/models/club`. Relatórios JSON registram hashes, contagens, durações e amostras de limites. As fontes foram produzidas com Blender 5.2.1 via MCP local, preservando geometria e identidade dos assets recebidos.
