"""Static compatibility gate for Blender candidates. Does not replace visual QA."""
import json,struct,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch26'
def inspect(path):
    raw=path.read_bytes();assert raw[:4]==b'glTF'
    doc=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
    return {'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),
        'meshes':len(doc.get('meshes',[])),
        'vertices':sum(doc['accessors'][p['attributes']['POSITION']]['count'] for m in doc.get('meshes',[]) for p in m['primitives']),
        'joints':sum(len(s['joints']) for s in doc.get('skins',[])),
        'clips':{a['name']:max(doc['accessors'][s['input']]['max'][0] for s in a['samplers']) for a in doc.get('animations',[])}}
report=[]
expected={'nina','bento','corvo','onca','iara','zeca','aki','barao','dama','morgana','carnical'}
assert {p.stem.removesuffix('_original') for p in OUT.glob('*_original.glb')}==expected,'Missing or unexpected baseline models'
for path in sorted(OUT.glob('*_original.glb')):
    ident=path.stem.removesuffix('_original');before=inspect(path);after=inspect(OUT/(ident+'_refined.glb'))
    assert before['clips'].keys() <= after['clips'].keys(),ident
    assert before['meshes']==after['meshes'],ident
    assert before['joints']==after['joints'],(ident,before['joints'],after['joints'])
    assert after['vertices']<=before['vertices']*1.01,(ident,'unexpected geometry growth')
    assert all(.3<=duration<=5.1 for duration in after['clips'].values()),ident
    report.append({'id':ident,'before':before,'after':after})
(OUT/'compatibility-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CAST_COMPATIBILITY_PASS',len(report),'characters;',sum(len(r['after']['clips']) for r in report),'clips')
