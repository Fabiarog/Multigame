#!/usr/bin/env python3
"""
Generate custom Truco Call reaction sprites (3, 6, 9, 12) for all 8 club characters.
Extracts high-resolution portraits from club-cast.png, applies character-specific
reactions and progressive stakes escalation aesthetics (Gold, Amber, Crimson, Royal Violet/Cyan),
and composites luxury club call banners.
"""

import os
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageEnhance

BASE_DIR = Path(__file__).resolve().parent.parent
CAST_PATH = BASE_DIR / "assets" / "sprites" / "characters" / "club" / "club-cast.png"
OUTPUT_DIR = BASE_DIR / "assets" / "sprites" / "truco_calls"
FONT_DISPLAY = BASE_DIR / "assets" / "fonts" / "Cormorant-Garamond.ttf"
FONT_SANS = BASE_DIR / "assets" / "fonts" / "DM-Sans.ttf"

CHARACTERS = [
    ("nina", "Nina", 0),
    ("bento", "Bento", 1),
    ("corvo", "Seu Corvo", 2),
    ("onca", "Dona Onça", 3),
    ("iara", "Iara", 4),
    ("zeca", "Zeca", 5),
    ("barao", "Barão da Meia-Noite", 6),
    ("dama", "Dama de Copas", 7),
]

STAKES_CONFIG = {
    3: {
        "title": "TRUCO!",
        "subtitle": "VALE 3 PONTOS",
        "primary_color": (212, 175, 55, 255),       # Luxury Gold
        "secondary_color": (255, 223, 128, 255),
        "glow_color": (180, 140, 30, 90),
        "bg_dark": (15, 22, 18, 255),
        "bg_tint": (30, 42, 34, 255),
        "use_react": False,
        "zoom": 1.02,
        "contrast": 1.05,
        "saturation": 1.05,
        "badge_grad": ((45, 36, 12), (18, 14, 5)),
        "sparks": False,
        "lightning": False,
    },
    6: {
        "title": "SEIS!",
        "subtitle": "VALE 6 PONTOS",
        "primary_color": (230, 126, 34, 255),       # Fiery Amber / Orange
        "secondary_color": (255, 175, 75, 255),
        "glow_color": (220, 90, 20, 110),
        "bg_dark": (26, 16, 12, 255),
        "bg_tint": (48, 24, 16, 255),
        "use_react": True,
        "zoom": 1.08,
        "contrast": 1.15,
        "saturation": 1.20,
        "badge_grad": ((55, 28, 10), (22, 10, 4)),
        "sparks": True,
        "lightning": False,
    },
    9: {
        "title": "NOVE!",
        "subtitle": "VALE 9 PONTOS",
        "primary_color": (225, 45, 65, 255),        # Blood Crimson
        "secondary_color": (255, 95, 110, 255),
        "glow_color": (200, 25, 45, 130),
        "bg_dark": (28, 10, 14, 255),
        "bg_tint": (52, 14, 22, 255),
        "use_react": True,
        "zoom": 1.14,
        "contrast": 1.25,
        "saturation": 1.25,
        "badge_grad": ((60, 14, 20), (24, 5, 8)),
        "sparks": True,
        "lightning": False,
    },
    12: {
        "title": "DOZE!",
        "subtitle": "VALE 12 PONTOS - TUDO OU NADA",
        "primary_color": (165, 80, 240, 255),      # Royal Violet & Electric Gold
        "secondary_color": (255, 215, 0, 255),
        "glow_color": (140, 50, 220, 150),
        "bg_dark": (20, 10, 32, 255),
        "bg_tint": (40, 18, 62, 255),
        "use_react": True,
        "zoom": 1.20,
        "contrast": 1.32,
        "saturation": 1.30,
        "badge_grad": ((50, 18, 70), (18, 6, 30)),
        "sparks": True,
        "lightning": True,
    }
}

def get_font(path, size):
    try:
        return ImageFont.truetype(str(path), size)
    except Exception:
        return ImageFont.load_default()

def create_sprite(sheet, char_idx, char_id, char_name, stakes, cfg):
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    # 1. Background roundel / luxury card shape
    margin = 12
    rect = [margin, margin, size - margin, size - margin]
    corner_r = 38

    # Radial background gradient on opaque canvas
    bg_dark = (cfg["bg_dark"][0], cfg["bg_dark"][1], cfg["bg_dark"][2], 255)
    bg_img = Image.new("RGBA", (size, size), bg_dark)
    
    # Radial aura from center
    glow_surf = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_surf)
    cx, cy = size // 2, size // 2 - 10
    max_r = size // 2 - 20
    for r in range(max_r, 0, -4):
        t = 1.0 - (r / max_r)
        glow_alpha = int(cfg["glow_color"][3] * (t ** 1.4))
        p_col = cfg["primary_color"]
        color = (p_col[0], p_col[1], p_col[2], glow_alpha)
        glow_draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color)
    glow_surf = glow_surf.filter(ImageFilter.GaussianBlur(8))
    bg_img.alpha_composite(glow_surf)

    # Cut background into rounded rectangle
    mask = Image.new("L", (size, size), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle(rect, radius=corner_r, fill=255)
    img.paste(bg_img, (0, 0), mask)

    # Add lightning / electric starburst if stakes 12
    if cfg["lightning"]:
        rays = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        r_draw = ImageDraw.Draw(rays)
        for deg in range(0, 360, 24):
            rad = math.radians(deg)
            x2 = cx + math.cos(rad) * 260
            y2 = cy + math.sin(rad) * 260
            r_draw.line([(cx, cy), (x2, y2)], fill=(255, 230, 100, 35), width=3)
            # cyan arc
            rad_cyan = math.radians(deg + 12)
            xc = cx + math.cos(rad_cyan) * 250
            yc = cy + math.sin(rad_cyan) * 250
            r_draw.line([(cx, cy), (xc, yc)], fill=(0, 230, 255, 30), width=2)
        rays = rays.filter(ImageFilter.GaussianBlur(2))
        img.alpha_composite(rays)

    # 2. Extract Character Crop
    frame_w = sheet.width // 8
    frame_h = sheet.height // 2
    row = 1 if cfg["use_react"] else 0
    crop_x = char_idx * frame_w
    crop_y = row * frame_h
    char_crop = sheet.crop((crop_x, crop_y, crop_x + frame_w, crop_y + frame_h))

    # Apply zoom & enhancements
    zoom = cfg["zoom"]
    target_w = int(frame_w * zoom * 1.15)
    target_h = int(frame_h * zoom * 1.15)
    char_crop = char_crop.resize((target_w, target_h), Image.Resampling.LANCZOS)

    enh_contrast = ImageEnhance.Contrast(char_crop)
    char_crop = enh_contrast.enhance(cfg["contrast"])
    enh_sat = ImageEnhance.Color(char_crop)
    char_crop = enh_sat.enhance(cfg["saturation"])

    # Center character inside roundel
    paste_x = (size - target_w) // 2
    paste_y = (size - target_h) // 2 - 14

    char_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    char_layer.paste(char_crop, (paste_x, paste_y), char_crop)
    bg_img.alpha_composite(char_layer)

    # 3. Add Sparks / Embers if enabled
    if cfg["sparks"]:
        sparks_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        s_draw = ImageDraw.Draw(sparks_layer)
        import random
        rng = random.Random(char_idx * 100 + stakes)
        spark_color = cfg["secondary_color"]
        for _ in range(24 if stakes >= 9 else 16):
            sx = rng.randint(margin + 20, size - margin - 20)
            sy = rng.randint(margin + 20, size - margin - 80)
            sr = rng.randint(2, 5)
            s_alpha = rng.randint(120, 240)
            s_col = (spark_color[0], spark_color[1], spark_color[2], s_alpha)
            s_draw.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], fill=s_col)
        sparks_layer = sparks_layer.filter(ImageFilter.GaussianBlur(1))
        bg_img.alpha_composite(sparks_layer)

    # 4. Vignette / Dark gradient at bottom behind banner
    vignette = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    v_draw = ImageDraw.Draw(vignette)
    for y in range(size - 180, size - margin - 4):
        progress = (y - (size - 180)) / 176.0
        v_alpha = int(230 * (progress ** 1.3))
        v_draw.line([(margin + 6, y), (size - margin - 6, y)], fill=(8, 10, 12, v_alpha))
    bg_img.alpha_composite(vignette)

    # 5. Mask the entire composited background into the rounded card
    mask = Image.new("L", (size, size), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle(rect, radius=corner_r, fill=255)
    img.paste(bg_img, (0, 0), mask)

    # 5. Metallic Border with Multiple Ribs & Corner Accents
    p_col = cfg["primary_color"]
    s_col = cfg["secondary_color"]
    draw = ImageDraw.Draw(img)

    # Outer metallic border
    draw.rounded_rectangle(rect, radius=corner_r, outline=p_col, width=4)
    # Inner subtle rim
    inner_rect = [margin + 6, margin + 6, size - margin - 6, size - margin - 6]
    draw.rounded_rectangle(inner_rect, radius=corner_r - 4, outline=(p_col[0], p_col[1], p_col[2], 90), width=1)

    # Luxury Corner Rivets / Gems
    corner_offsets = [
        (margin + 18, margin + 18),
        (size - margin - 18, margin + 18),
        (margin + 18, size - margin - 18),
        (size - margin - 18, size - margin - 18),
    ]
    for kx, ky in corner_offsets:
        draw.ellipse([kx - 5, ky - 5, kx + 5, ky + 5], fill=s_col, outline=p_col, width=1)
        draw.ellipse([kx - 2, ky - 2, kx + 2, ky + 2], fill=(255, 255, 255, 230))

    # Top Character Name Badge
    name_font = get_font(FONT_SANS, 18)
    name_bbox = name_font.getbbox(char_name.upper())
    name_w = name_bbox[2] - name_bbox[0]
    badge_x1 = cx - name_w // 2 - 16
    badge_x2 = cx + name_w // 2 + 16
    draw.rounded_rectangle([badge_x1, margin - 2, badge_x2, margin + 26], radius=8, fill=(12, 16, 20, 240), outline=p_col, width=2)
    draw.text((cx - name_w // 2, margin + 3), char_name.upper(), font=name_font, fill=s_col)

    # 6. Lower Call Banner ("TRUCO!", "SEIS!", etc.)
    banner_w = size - 56
    banner_h = 96
    bx1 = (size - banner_w) // 2
    by1 = size - banner_h - margin - 8
    bx2 = bx1 + banner_w
    by2 = by1 + banner_h

    # Banner background with gradient and gold trim
    b_surf = Image.new("RGBA", (banner_w, banner_h), (0, 0, 0, 0))
    b_draw = ImageDraw.Draw(b_surf)
    c_top, c_bot = cfg["badge_grad"]
    for y in range(banner_h):
        t = y / float(banner_h)
        col = (
            int(c_top[0] * (1 - t) + c_bot[0] * t),
            int(c_top[1] * (1 - t) + c_bot[1] * t),
            int(c_top[2] * (1 - t) + c_bot[2] * t),
            245
        )
        b_draw.line([(0, y), (banner_w, y)], fill=col)

    b_mask = Image.new("L", (banner_w, banner_h), 0)
    b_mask_draw = ImageDraw.Draw(b_mask)
    b_mask_draw.rounded_rectangle([0, 0, banner_w, banner_h], radius=16, fill=255)

    img.paste(b_surf, (bx1, by1), b_mask)

    # Banner outline
    draw.rounded_rectangle([bx1, by1, bx2, by2], radius=16, outline=p_col, width=3)
    draw.rounded_rectangle([bx1 + 3, by1 + 3, bx2 - 3, by2 - 3], radius=13, outline=(s_col[0], s_col[1], s_col[2], 120), width=1)

    # Banner Typography
    call_font = get_font(FONT_DISPLAY, 46)
    sub_font = get_font(FONT_SANS, 16)

    title_text = cfg["title"]
    t_bbox = call_font.getbbox(title_text)
    t_w = t_bbox[2] - t_bbox[0]
    t_x = cx - t_w // 2
    t_y = by1 + 6

    # Text drop shadow
    draw.text((t_x + 2, t_y + 2), title_text, font=call_font, fill=(0, 0, 0, 240))
    draw.text((t_x, t_y), title_text, font=call_font, fill=s_col)

    # Subtitle ("VALE 3 PONTOS", etc.)
    sub_text = cfg["subtitle"]
    s_bbox = sub_font.getbbox(sub_text)
    s_w = s_bbox[2] - s_bbox[0]
    s_x = cx - s_w // 2
    s_y = by1 + 58

    draw.text((s_x + 1, s_y + 1), sub_text, font=sub_font, fill=(0, 0, 0, 220))
    draw.text((s_x, s_y), sub_text, font=sub_font, fill=(235, 235, 235, 255))

    return img

def main():
    if not CAST_PATH.exists():
        raise FileNotFoundError(f"Missing {CAST_PATH}")

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(CAST_PATH).convert("RGBA")
    print(f"Loaded cast sheet: {sheet.size}")

    generated_files = []
    # Contact sheet for QA verification (8 columns x 4 rows)
    contact_w = 8 * 256
    contact_h = 4 * 256
    contact = Image.new("RGBA", (contact_w, contact_h), (12, 14, 16, 255))

    for char_idx, (char_id, char_name, _) in enumerate(CHARACTERS):
        for stakes in [3, 6, 9, 12]:
            cfg = STAKES_CONFIG[stakes]
            sprite = create_sprite(sheet, char_idx, char_id, char_name, stakes, cfg)
            out_name = f"{char_id}_{stakes}.png"
            out_path = OUTPUT_DIR / out_name
            sprite.save(out_path, "PNG", optimize=True)
            generated_files.append(out_path)

            # Paste into contact sheet (row 0=3, row 1=6, row 2=9, row 3=12)
            row_idx = [3, 6, 9, 12].index(stakes)
            thumb = sprite.resize((256, 256), Image.Resampling.LANCZOS)
            contact.paste(thumb, (char_idx * 256, row_idx * 256), thumb)

    contact_path = OUTPUT_DIR / "all_truco_calls_preview.png"
    contact.save(contact_path, "PNG", optimize=True)
    print(f"Successfully generated {len(generated_files)} Truco Call sprites in {OUTPUT_DIR}")
    print(f"Saved contact preview: {contact_path}")

if __name__ == "__main__":
    main()
