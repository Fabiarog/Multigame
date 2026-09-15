"""Assemble unmodified Blender renders into labeled review sheets."""
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'art/blender/patch26';OUT=ROOT/'docs/patch26'
OUT.mkdir(exist_ok=True)
cast=['onca','corvo','nina','bento','iara','zeca','aki','barao','dama','morgana','carnical']
for clip in ('truco','idle','victory'):
    sheet=Image.new('RGB',(1200,960),(20,23,26));draw=ImageDraw.Draw(sheet)
    for i,ident in enumerate(cast):
        pic=Image.open(SRC/f'{ident}_refined_{clip}.png').convert('RGB');pic.thumbnail((300,290))
        x=i%4*300;y=i//4*320;sheet.paste(pic,(x,y+25));draw.text((x+12,y+7),ident+' / '+clip,fill='white')
    sheet.save(OUT/f'elenco-{clip}.jpg',quality=90)
sheet=Image.new('RGB',(1280,1340),(20,23,26));draw=ImageDraw.Draw(sheet)
for i,ident in enumerate(('morgana','carnical')):
    for j,version in enumerate(('original','refined')):
        pic=Image.open(SRC/f'{ident}_{version}_victory.png').convert('RGB')
        x=j*640;y=i*670;sheet.paste(pic,(x,y+30))
        draw.text((x+20,y+10),ident+' / '+('ANTES' if j==0 else 'DEPOIS - pesos corrigidos'),fill='white')
sheet.save(OUT/'correcao-pesos-bosses.jpg',quality=92)
