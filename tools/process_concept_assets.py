import os
from pathlib import Path
from PIL import Image

ARTIFACT_DIR = Path(r"C:\Users\Lucas\.gemini\antigravity-ide\brain\afaa2c0b-95ab-491c-87c3-49abc587caa4")
ROOT = Path(r"c:\workspace\multigame")
CONCEPT_DIR = ROOT / "assets/sprites/concept"
UI_DIR = ROOT / "assets/sprites/ui"
CONCEPT_DIR.mkdir(parents=True, exist_ok=True)
UI_DIR.mkdir(parents=True, exist_ok=True)

CHAR_FILES = {
    "nina": ARTIFACT_DIR / "nina_concept_360_1788749492040.jpg",
    "bento": ARTIFACT_DIR / "bento_concept_360_1788749556561.jpg",
    "corvo": ARTIFACT_DIR / "corvo_concept_360_1788749621280.jpg",
    "onca": ARTIFACT_DIR / "onca_concept_360_1788749735486.jpg",
    "iara": ARTIFACT_DIR / "iara_concept_360_1788749806503.jpg",
    "zeca": ARTIFACT_DIR / "zeca_concept_360_1788749882611.jpg",
    "barao": ARTIFACT_DIR / "barao_concept_360_1788749959526.jpg",
    "dama": ARTIFACT_DIR / "dama_concept_360_1788750049021.jpg",
}

POV_FILE = ARTIFACT_DIR / "pov_player_hands_1788750129757.jpg"

def remove_white_bg(img, threshold=245, feather=15):
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
            min_val = min(pixels_r[x, y], pixels_g[x, y], pixels_b[x, y])
            if min_val >= threshold:
                mask_load[x, y] = 0
            elif min_val > threshold - feather:
                mask_load[x, y] = int(255 * (threshold - min_val) / feather)
            else:
                mask_load[x, y] = 255
    img.putalpha(bg_mask)
    return img

def process_characters():
    for name, path in CHAR_FILES.items():
        if not path.exists():
            print(f"Warning: {path} not found!")
            continue
        img = Image.open(path).convert("RGB")
        sheet_dest = CONCEPT_DIR / f"{name}_sheet.png"
        img.save(sheet_dest)
        print(f"Saved sheet: {sheet_dest}")

        char_sub = CONCEPT_DIR / name
        char_sub.mkdir(parents=True, exist_ok=True)
        w, h = img.size
        # The sheet has 4 columns: Front, 3/4, Side, Back
        col_w = w // 4
        labels = ["0_front", "1_three_quarter", "2_side", "3_back"]
        for idx, label in enumerate(labels):
            crop_box = (idx * col_w, 0, (idx + 1) * col_w, h)
            angle_img = img.crop(crop_box)
            angle_dest = char_sub / f"{label}.png"
            angle_img.save(angle_dest)
            print(f"  Saved angle {label}: {angle_dest}")

def process_pov_hands():
    if not POV_FILE.exists():
        print("Warning: POV file not found!")
        return
    img = Image.open(POV_FILE)
    trans = remove_white_bg(img, threshold=245, feather=12)
    dest = UI_DIR / "pov_hands.png"
    trans.save(dest)
    print(f"Saved POV hands with transparency: {dest}")

if __name__ == "__main__":
    process_characters()
    process_pov_hands()
    print("Concept asset processing complete!")
