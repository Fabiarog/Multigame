import math
import os
from PIL import Image, ImageDraw

def create_environment_palette():
    size = 2048
    img = Image.new("RGBA", (size, size), (20, 20, 25, 255))
    draw = ImageDraw.Draw(img)

    # 4 Quadrants of 1024x1024:
    # Q1 (0,0 to 1024,1024): Classic Victorian Club (Mahogany Boiserie + Emerald Damask Wallpaper + Parquet)
    # Q2 (1024,0 to 2048,1024): Barão's Midnight Lounge (Obsidian Wood + Royal Purple/Midnight Damask + Dark Marble)
    # Q3 (0,1024 to 1024,2048): Dama's Crimson Velvet Salon (Rosewood + Ruby Damask/Drapery + Burgundy Gold Marble)
    # Q4 (1024,1024 to 2048,2048): Cyber Casino / Neon Lounge (Brushed Slate + Cyan/Gold Neon Strips + Polished Black Granite)

    # --- Quadrant 1: Classic Club ---
    # Parquet Floor (0..1024, 0..512)
    for y in range(0, 512, 32):
        for x in range(0, 1024, 64):
            shade = 38 + ((x // 64 + y // 32) % 3) * 8
            draw.rectangle([x, y, x + 63, y + 31], fill=(shade, shade - 16, shade - 26, 255), outline=(22, 12, 6, 255))
            # wood grain lines
            for gy in range(y + 4, y + 32, 8):
                draw.line([x + 2, gy, x + 61, gy], fill=(shade - 10, shade - 22, shade - 30, 255))

    # Emerald Damask Wallpaper (0..1024, 512..1024)
    draw.rectangle([0, 512, 1024, 1024], fill=(16, 48, 34, 255))
    for y in range(512, 1024, 48):
        for x in range(0, 1024, 48):
            # Damask diamond pattern
            draw.polygon([(x + 24, y + 6), (x + 42, y + 24), (x + 24, y + 42), (x + 6, y + 24)],
                         fill=(22, 68, 48, 255), outline=(190, 150, 60, 180))
            # inner fleur motif
            draw.ellipse([x + 20, y + 20, x + 28, y + 28], fill=(210, 175, 75, 220))

    # Boiserie Wood Trim Divider (0..1024, 500..512)
    draw.rectangle([0, 500, 1024, 512], fill=(62, 34, 20, 255), outline=(210, 175, 75, 255))

    # --- Quadrant 2: Barão's Midnight Lounge ---
    # Dark Polished Marble Floor (1024..2048, 0..512)
    draw.rectangle([1024, 0, 2048, 512], fill=(18, 16, 26, 255))
    for i in range(1024, 2048, 128):
        draw.line([i, 0, i + 512, 512], fill=(45, 38, 62, 160), width=2)
        draw.line([i, 512, i + 512, 0], fill=(160, 130, 50, 90), width=1)

    # Midnight Purple Damask Wallpaper (1024..2048, 512..1024)
    draw.rectangle([1024, 512, 2048, 1024], fill=(22, 18, 38, 255))
    for y in range(512, 1024, 48):
        for x in range(1024, 2048, 48):
            draw.polygon([(x + 24, y + 6), (x + 42, y + 24), (x + 24, y + 42), (x + 6, y + 24)],
                         fill=(36, 28, 60, 255), outline=(220, 180, 80, 200))
            draw.ellipse([x + 20, y + 20, x + 28, y + 28], fill=(220, 180, 80, 240))
    draw.rectangle([1024, 500, 2048, 512], fill=(30, 24, 48, 255), outline=(220, 180, 80, 255))

    # --- Quadrant 3: Dama's Crimson Salon ---
    # Rosewood Parquet (0..1024, 1024..1536)
    for y in range(1024, 1536, 32):
        for x in range(0, 1024, 64):
            shade_r = 55 + ((x // 64 + y // 32) % 3) * 10
            draw.rectangle([x, y, x + 63, y + 31], fill=(shade_r, 18, 24, 255), outline=(28, 8, 12, 255))
            for gy in range(y + 4, y + 32, 8):
                draw.line([x + 2, gy, x + 61, gy], fill=(shade_r - 12, 12, 18, 255))

    # Ruby Red Velvet & Gold Wallpaper (0..1024, 1536..2048)
    draw.rectangle([0, 1536, 1024, 2048], fill=(58, 14, 22, 255))
    for y in range(1536, 2048, 48):
        for x in range(0, 1024, 48):
            # Heart-like damask motif
            draw.polygon([(x + 24, y + 6), (x + 42, y + 24), (x + 24, y + 42), (x + 6, y + 24)],
                         fill=(78, 18, 30, 255), outline=(220, 175, 70, 210))
            draw.ellipse([x + 18, y + 16, x + 24, y + 22], fill=(220, 175, 70, 240))
            draw.ellipse([x + 24, y + 16, x + 30, y + 22], fill=(220, 175, 70, 240))
    draw.rectangle([0, 1524, 1024, 1536], fill=(42, 12, 18, 255), outline=(220, 175, 70, 255))

    # --- Quadrant 4: Cyber Casino / Neon Lounge ---
    # Black Granite Floor (1024..2048, 1024..1536)
    draw.rectangle([1024, 1024, 2048, 1536], fill=(12, 15, 18, 255))
    for y in range(1024, 1536, 64):
        draw.line([1024, y, 2048, y], fill=(25, 32, 40, 255), width=2)
    for x in range(1024, 2048, 64):
        draw.line([x, 1024, x, 1536], fill=(25, 32, 40, 255), width=2)

    # Dark Slate & Neon Trim (1024..2048, 1536..2048)
    draw.rectangle([1024, 1536, 2048, 2048], fill=(16, 20, 24, 255))
    # Neon Cyan & Gold Strips
    for y in range(1560, 2048, 96):
        draw.rectangle([1024, y, 2048, y + 6], fill=(40, 210, 235, 255))
        draw.rectangle([1024, y + 12, 2048, y + 16], fill=(220, 185, 70, 255))

    # Art & Picture Frames (Top corner boxes to sample for framed art on walls)
    # Frame 1: Ace of Spades / Club Crest
    draw.rectangle([800, 700, 960, 920], fill=(240, 235, 220, 255), outline=(210, 165, 60, 255), width=8)
    draw.text((860, 780), "♠", fill=(20, 20, 20, 255))

    draw.rectangle([1824, 700, 1984, 920], fill=(20, 18, 30, 255), outline=(220, 180, 80, 255), width=8)
    draw.text((1884, 780), "👑", fill=(220, 180, 80, 255))

    out_path = os.path.abspath("assets/models/club/club_environment_palette.png")
    img.save(out_path)
    print(f"[Palette] Saved environment palette to {out_path} ({os.path.getsize(out_path)} bytes)")

if __name__ == "__main__":
    create_environment_palette()
