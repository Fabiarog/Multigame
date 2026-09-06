"""Composite 8-character club cast sprite sheet:
Indices 0..3: Nina, Bento, Corvo, Onça (from existing club-cast.png)
Indices 4..7: Iara, Zeca, Barão, Dama (from AI generated expansion sheet)
Output: assets/sprites/characters/club/club-cast.png (8 columns x 2 rows, 3548 x 887)
"""
import os
from pathlib import Path
from PIL import Image

ROOT = Path(r"c:\workspace\multigame")
EXPANSION_IMG = Path(r"C:\Users\Lucas\.gemini\antigravity-ide\brain\eabdfecc-ffb3-4c81-afb0-7677193c59e8\club_expansion_cast_1788657759646.jpg")
DAMA_REACT_IMG = Path(r"C:\Users\Lucas\.gemini\antigravity-ide\brain\eabdfecc-ffb3-4c81-afb0-7677193c59e8\dama_victory_pose_1788658617521.jpg")
ORIGINAL_SHEET = Path(r"C:\Users\Lucas\.gemini\antigravity-ide\brain\eabdfecc-ffb3-4c81-afb0-7677193c59e8\existing_club_cast.png")
OUTPUT_SHEET = ROOT / "assets/sprites/characters/club/club-cast.png"
MODELS_DIR = ROOT / "assets/models/club"

def remove_white_bg(img, threshold=240, feather=15):
    """Cleanly converts white background to transparent while preserving anti-aliasing."""
    img = img.convert("RGBA")
    r, g, b, a = img.split()
    
    bg_mask = Image.new("L", img.size, 255)
    pixels_r = r.load()
    pixels_g = g.load()
    pixels_b = b.load()
    mask_load = bg_mask.load()
    
    w, h = img.size
    for y in range(h):
        for x in range(w):
            pr = pixels_r[x, y]
            pg = pixels_g[x, y]
            pb = pixels_b[x, y]
            min_val = min(pr, pg, pb)
            if min_val >= threshold:
                mask_load[x, y] = 0
            elif min_val > threshold - feather:
                alpha = int(255 * (threshold - min_val) / feather)
                mask_load[x, y] = alpha
            else:
                mask_load[x, y] = 255
                
    img.putalpha(bg_mask)
    return img

def main():
    orig = Image.open(ORIGINAL_SHEET).convert("RGBA")
    orig_w, orig_h = orig.size
    cell_w = orig_w // 4  # ~443
    cell_h = orig_h // 2  # ~443
    
    print(f"Original sheet size: {orig_w}x{orig_h}, cell: {cell_w}x{cell_h}")
    
    exp = Image.open(EXPANSION_IMG).convert("RGB")
    exp_w, exp_h = exp.size
    exp_cell_w = exp_w // 4  # 256
    exp_cell_h = exp_h // 2  # 512
    
    master_w = cell_w * 8
    master_h = cell_h * 2
    master = Image.new("RGBA", (master_w, master_h), (0, 0, 0, 0))
    
    for row in range(2):
        for col in range(4):
            src_box = (col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h)
            cell = orig.crop(src_box)
            dest_pos = (col * cell_w, row * cell_h)
            master.paste(cell, dest_pos)
            
    names = ["iara", "zeca", "barao", "dama"]
    for col in range(4):
        char_name = names[col]
        for row in range(2):
            if char_name == "dama" and row == 1:
                cell = Image.open(DAMA_REACT_IMG).convert("RGB")
            else:
                src_box = (col * exp_cell_w, row * exp_cell_h, (col + 1) * exp_cell_w, (row + 1) * exp_cell_h)
                cell = exp.crop(src_box)
            cell_trans = remove_white_bg(cell, threshold=245, feather=12)
            
            bbox = cell_trans.getbbox()
            if bbox:
                cropped_content = cell_trans.crop(bbox)
                cw, ch = cropped_content.size
                
                target_h = int(cell_h * 0.94)
                target_w = int(cw * (target_h / ch))
                if target_w > cell_w:
                    target_w = cell_w
                    target_h = int(ch * (target_w / cw))
                    
                scaled = cropped_content.resize((target_w, target_h), Image.Resampling.LANCZOS)
                
                paste_x = (4 + col) * cell_w + (cell_w - target_w) // 2
                paste_y = row * cell_h + (cell_h - target_h)
                master.paste(scaled, (paste_x, paste_y), scaled)
                
                if row == 0:
                    portrait_path = MODELS_DIR / f"{char_name}.png"
                    indiv = Image.new("RGBA", (320, 400), (0, 0, 0, 0))
                    scale_factor = min(300 / cw, 380 / ch)
                    pw = int(cw * scale_factor)
                    ph = int(ch * scale_factor)
                    scaled_indiv = cropped_content.resize((pw, ph), Image.Resampling.LANCZOS)
                    indiv.paste(scaled_indiv, ((320 - pw) // 2, 400 - ph), scaled_indiv)
                    indiv.save(portrait_path, "PNG")
                    print(f"Saved portrait: {portrait_path}")

    master.save(OUTPUT_SHEET, "PNG")
    print(f"Master 8-character sheet saved to {OUTPUT_SHEET} ({master_w}x{master_h})")

if __name__ == "__main__":
    main()
