"""Format measured evidence; never infer unmeasured CPU/GPU performance."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / 'docs/patch27'
before = json.loads((out/'before/report.json').read_text())
after = json.loads((out/'after/report.json').read_text())
previous = {r['label']: r for r in before['measurements']}
lines = ['# Comparação medida — Classic Club', '',
         'RTX 3050; Godot 4.7.2 .NET; Forward+/Vulkan. Menor tempo é melhor. Mediana/P95 em milissegundos; memória é estimativa do renderer.', '',
         '| Configuração | Antes mediana / P95 | Depois mediana / P95 | Variação mediana | Desenhos antes → depois | Memória MiB antes → depois |',
         '|---|---:|---:|---:|---:|---:|']
for row in after['measurements']:
    old = previous[row['label']]
    change = (row['median_ms']/old['median_ms']-1)*100
    lines.append(f"| {row['label']} | {old['median_ms']:.3f} / {old['p95_ms']:.3f} | {row['median_ms']:.3f} / {row['p95_ms']:.3f} | {change:+.1f}% | {old['draw_calls']:.0f} → {row['draw_calls']:.0f} | {old['render_memory_bytes']/2**20:.1f} → {row['render_memory_bytes']/2**20:.1f} |")
lines += ['', 'O perfil leve melhorou neste teste. Em ultra 1080p o custo ficou próximo do anterior; em 4K ultra subiu cerca de 6%. A mediana atende à meta provisória de 33 ms, mas o P95 do POV chegou a 42,167 ms: a estabilidade de 30 FPS não está demonstrada. Contagens são monitores amostrados, não capturas completas de todas as passagens de sombra.', '',
          '## Variantes de iluminação — 1080p ultra', '',
          '| Variante incremental | Mediana ms | P95 ms | Memória MiB |', '|---|---:|---:|---:|']
for row in json.loads((out/'gi/report.json').read_text()):
    if 'median_ms' in row:
        lines.append(f"| {row['mode']} | {row['median_ms']:.3f} | {row['p95_ms']:.3f} | {row['render_memory_bytes']/2**20:.1f} |")
    else:
        lines.append('| LightmapGI | Não medido | UV2/bake pendentes | — |')
lines += ['', 'Variantes sequenciais podem reter alocações. Não comparar a memória como picos isolados. Configurações e limites: [relatório](../PATCH27_VALIDACAO.md).', '', '## Imagens nativas', '']
for row in after['measurements']:
    label = row['label']
    lines.append(f'- {label}: [antes](before/{label}.png) · [depois](after/{label}.png)')
lines += ['', '### Antes — 1080p ultra, mesa', '', '![Antes](before/1920-ultra-mesa.png)', '', '### Depois — 1080p ultra, mesa', '', '![Depois](after/1920-ultra-mesa.png)', '']
(out/'COMPARACAO.md').write_text('\n'.join(lines), encoding='utf-8')
print('REPORT_27_WRITTEN')
