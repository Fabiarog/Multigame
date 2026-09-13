"""Publish measured fireplace review evidence, keeping full-resolution captures."""
import json,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'docs/patch30';out.mkdir(parents=True,exist_ok=True)
reports={}
for version in ['before','after']:
    source=root/f'temp/patch30-{version}'
    reports[version]=json.loads((source/'report.json').read_text())
    dest=out/version;dest.mkdir(exist_ok=True)
    shutil.copy2(source/'report.json',dest/'report.json')
    for label in ['1920-ultra-mesa','1920-ultra-pov','3840-ultra-pov']:
        shutil.copy2(source/(label+'.png'),dest/(label+'.png'))
previous={r['label']:r for r in reports['before']['measurements']}
lines=['# Lareira do Classic Club — antes/depois','',
       'Base 87a7b98; Backup 18bdc3b. Blender MCP 9876; Godot 4.7.2 .NET, Forward+/Vulkan, RTX 3050. Materiais dos troncos, painel de fundo e bevel refinados; UV2 candidata adicionada ao cenário. Nenhuma luz adicional ou novo efeito de pós-processamento.', '',
       '## Medições desta sessão','',
       'Composição fixa, 90 amostras por configuração. Mediana e P95 são intervalos observados entre quadros, não custo isolado de GPU. Processos externos não são controlados. Não comparar diretamente estes valores com os do Patch 27 nem afirmar ganho de desempenho por pequenas diferenças.', '',
       '| Configuração | Antes mediana/P95 ms | Depois mediana/P95 ms | Draw calls antes → depois |', '|---|---:|---:|---:|']
for row in reports['after']['measurements']:
    old=previous[row['label']]
    lines.append(f"| {row['label']} | {old['median_ms']:.3f} / {old['p95_ms']:.3f} | {row['median_ms']:.3f} / {row['p95_ms']:.3f} | {old['draw_calls']:.0f} → {row['draw_calls']:.0f} |")
lines+=['','## Validação','',
        'Exportação: seis malhas, 37 primitivas com UV2 em 0–1, sem esqueletos/animações na sala. Fonte: 30.692 vértices / 16.560 triângulos, acréscimo de 704 triângulos. UV2 não significa LightmapGI concluído: falta avaliar densidade/margens e realizar o bake no editor.', '',
        'Regras PASS (531 verificações); interface PASS (27 capturas, 25 ações, nenhum erro de layout); regressão das quatro salas/assentos/baralho PASS. Os testes finais não reportaram exceções. Avisos de objetos/texturas retidos ao encerrar continuam presentes; não houve investigação de memória em partida longa.', '',
        'Fontes editáveis, auditoria e hash do export em art/blender/patch30. [Plano e limitações](../PLANO_VISUAL30.md).', '',
        '## Antes — mesa 1080p ultra','', '![Antes](before/1920-ultra-mesa.png)','',
        '## Depois — mesa 1080p ultra','', '![Depois](after/1920-ultra-mesa.png)','',
        '[POV antes](before/1920-ultra-pov.png) · [POV depois](after/1920-ultra-pov.png) · [4K antes](before/3840-ultra-pov.png) · [4K depois](after/3840-ultra-pov.png)','']
(out/'REVISAO.md').write_text('\n'.join(lines),encoding='utf-8');print('HEARTH_REPORT_PASS')
