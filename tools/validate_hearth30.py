"""Validate the exported room's UV2 and scene structure before admitting it."""
import hashlib,json,struct,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
path=root/'assets/models/club/room_classic_club_premium.glb'
raw=path.read_bytes();assert raw[:4]==b'glTF'
length=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+length]);binary=raw[28+length:]
assert len(doc['meshes'])==6 and not doc.get('skins') and not doc.get('animations')
primitives=[p for m in doc['meshes'] for p in m['primitives']]
rows=[]
for primitive in primitives:
    assert 'TEXCOORD_1' in primitive['attributes'],'Export dropped lightmap UV2'
    acc=doc['accessors'][primitive['attributes']['TEXCOORD_1']]
    assert acc['componentType']==5126 and acc['type']=='VEC2'
    view=doc['bufferViews'][acc['bufferView']];start=view.get('byteOffset',0)+acc.get('byteOffset',0);stride=view.get('byteStride',8)
    values=[struct.unpack_from('<ff',binary,start+i*stride) for i in range(acc['count'])]
    assert all(-.00001<=value<=1.00001 for uv in values for value in uv),'UV2 outside lightmap atlas'
    rows.append(acc['count'])
original=subprocess.check_output(['git','show','87a7b98:assets/models/club/room_classic_club_premium.glb'],cwd=root)
report={'status':'PASS','sha256':hashlib.sha256(raw).hexdigest(),'bytes_before':len(original),'bytes_after':len(raw),'meshes':6,'primitives_with_uv2':len(rows),'uv2_vertices':sum(rows),'lightmap_baked':False}
(root/'art/blender/patch30/export-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
