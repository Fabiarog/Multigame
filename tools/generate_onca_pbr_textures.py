"""Generate high-resolution (2048x2048) PBR texture maps for Dona Onça.
Produces:
- assets/models/club/onca_base_color.jpg: Rich amber fur with authentic jaguar rosettes, white muzzle, burgundy velvet, gold filigree, charcoal trousers, ruby gem.
- assets/models/club/onca_metallic_roughness.jpg: Green=Roughness, Blue=Metallic.
- assets/models/club/onca_normal.jpg: Tangent-space normal map with embroidery, seams, fur relief, and button bevels.
"""
import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(r"c:\workspace\multigame")
OUT_DIR = ROOT / "assets/models/club"
OUT_DIR.mkdir(parents=True, exist_ok=True)

SIZE = 2048

def create_pbr_maps():
    print("[TEXTURE] Generating Dona Onça 2K PBR texture maps...")
    random.seed(42) # Deterministic authentic rosettes
    
    # 1. BASE COLOR MAP (RGB)
    albedo = Image.new("RGB", (SIZE, SIZE), (200, 137, 56)) # Warm amber jaguar base
    draw = ImageDraw.Draw(albedo)
    
    # -------------------------------------------------------------------------
    # Region 1: Head & Face & Ears (UV: [0, 0] to [1024, 1024])
    # -------------------------------------------------------------------------
    # Amber fur base: (203, 137, 56)
    # White muzzle & chin area: (248, 244, 236)
    draw.rectangle([0, 0, 1024, 1024], fill=(205, 138, 58))
    
    # Muzzle white patch (center-bottom of head UV)
    for r in range(260, 0, -10):
        alpha = int(255 * (1.0 - r / 260.0))
        c = (int(205 + (248 - 205) * (1 - r/260)), int(138 + (244 - 138) * (1 - r/260)), int(58 + (236 - 58) * (1 - r/260)))
        draw.ellipse([512 - r, 720 - int(r * 0.7), 512 + r, 720 + int(r * 0.7)], fill=c)
        
    # Black nose pad at bottom center
    draw.rounded_rectangle([472, 600, 552, 670], radius=18, fill=(24, 20, 22))
    draw.ellipse([484, 626, 506, 646], fill=(12, 10, 12)) # nostril L
    draw.ellipse([518, 626, 540, 646], fill=(12, 10, 12)) # nostril R
    draw.line([512, 668, 512, 720], fill=(24, 20, 22), width=4) # philtrum
    
    # Eyes (L at 320, 480; R at 704, 480)
    for eye_x in (320, 704):
        # Dark eyeliner / mascara outline
        draw.ellipse([eye_x - 70, 480 - 45, eye_x + 70, 480 + 45], fill=(22, 18, 20))
        # Almond amber-gold iris
        draw.ellipse([eye_x - 55, 480 - 35, eye_x + 55, 480 + 35], fill=(228, 168, 40))
        # Inner golden ring
        draw.ellipse([eye_x - 38, 480 - 28, eye_x + 38, 480 + 28], fill=(245, 195, 65))
        # Vertical feline pupil
        draw.ellipse([eye_x - 14, 480 - 32, eye_x + 14, 480 + 32], fill=(12, 10, 14))
        # Specular glint
        draw.ellipse([eye_x - 22, 480 - 22, eye_x - 10, 480 - 10], fill=(255, 255, 255))
        draw.ellipse([eye_x + 10, 480 + 6, eye_x + 16, 480 + 12], fill=(230, 230, 240))
        
    # Ears: left at (160, 200), right at (864, 200)
    for ear_x, is_left in ((200, True), (824, False)):
        # Inner ear pinkish white
        draw.polygon([(ear_x - 100, 300), (ear_x, 100), (ear_x + 100, 300)], fill=(240, 230, 225))
        draw.polygon([(ear_x - 60, 280), (ear_x, 140), (ear_x + 60, 280)], fill=(248, 215, 205))
        # Ear tuft white lines
        for i in range(-4, 5):
            draw.line([(ear_x + i * 12, 280), (ear_x + i * 8, 200)], fill=(255, 255, 255), width=3)
            
    # Authentic Jaguar Rosettes on Cranium, Cheeks and Forehead
    # A jaguar rosette is a cluster of 3-5 dark spots encircling an amber center
    def draw_rosette(cx, cy, scale=1.0):
        center_color = (222, 152, 64) # Warmer amber inside
        border_color = (36, 24, 18)    # Dark brown/black spots
        r_inner = int(18 * scale)
        draw.ellipse([cx - r_inner, cy - r_inner, cx + r_inner, cy + r_inner], fill=center_color)
        num_spots = random.randint(3, 6)
        for s in range(num_spots):
            ang = s * (2 * math.pi / num_spots) + random.uniform(-0.3, 0.3)
            dist = 22 * scale + random.uniform(-3, 3)
            sx = cx + int(math.cos(ang) * dist)
            sy = cy + int(math.sin(ang) * dist)
            spot_rx = int(random.uniform(7, 12) * scale)
            spot_ry = int(random.uniform(6, 10) * scale)
            draw.ellipse([sx - spot_rx, sy - spot_ry, sx + spot_rx, sy + spot_ry], fill=border_color)

    # Scatter rosettes across head UV
    rosette_positions = [
        (340, 280), (440, 240), (512, 220), (584, 240), (684, 280),
        (260, 380), (380, 360), (512, 340), (644, 360), (764, 380),
        (220, 520), (280, 600), (744, 600), (804, 520),
        (320, 720), (704, 720), (400, 840), (512, 860), (624, 840),
        (180, 880), (844, 880), (512, 140)
    ]
    for rx, ry in rosette_positions:
        draw_rosette(rx, ry, scale=random.uniform(0.85, 1.25))
        
    # Small single spots on whisker pads
    for wx, wy in [(420, 640), (400, 660), (430, 675), (410, 695),
                   (604, 640), (624, 660), (594, 675), (614, 695)]:
        draw.ellipse([wx - 6, wy - 5, wx + 6, wy + 5], fill=(36, 24, 18))

    # -------------------------------------------------------------------------
    # Region 2: Burgundy Velvet Jacket & Gold Embroidery (UV: [1024, 0] to [2048, 1024])
    # -------------------------------------------------------------------------
    jacket_base = (104, 29, 51) # #681d33 Rich maroon velvet
    draw.rectangle([1024, 0, 2048, 1024], fill=jacket_base)
    
    # Add velvet fabric micro-texture (subtle grain)
    for _ in range(3000):
        gx = random.randint(1024, 2047)
        gy = random.randint(0, 1023)
        v_offset = random.randint(-12, 12)
        draw.point((gx, gy), fill=(max(0, min(255, 104 + v_offset)), max(0, min(255, 29 + v_offset // 2)), max(0, min(255, 51 + v_offset // 2))))
        
    # Gold filigree arabesque borders on lapels and hem
    gold_fill = (223, 177, 91)  # #dfb15b Aristocratic gold
    gold_shade = (180, 135, 55)
    
    # Lapel trim lines with braided embroidery pattern
    for border_y in (120, 240, 560, 720, 920):
        draw.rectangle([1060, border_y - 6, 2012, border_y + 6], fill=gold_shade)
        draw.rectangle([1060, border_y - 3, 2012, border_y + 3], fill=gold_fill)
        # Baroque filigree wave / scallops
        for fx in range(1080, 2000, 40):
            draw.arc([fx - 18, border_y - 18, fx + 18, border_y + 18], 0, 180, fill=gold_fill, width=3)
            draw.ellipse([fx - 4, border_y - 4, fx + 4, border_y + 4], fill=(245, 210, 120))
            
    # Ornate coat buttons (double-breasted gold lion head buttons)
    button_coords = [(1300, 380), (1300, 500), (1300, 620), (1750, 380), (1750, 500), (1750, 620)]
    for bx, by in button_coords:
        draw.ellipse([bx - 26, by - 26, bx + 26, by + 26], fill=gold_shade)
        draw.ellipse([bx - 22, by - 22, bx + 22, by + 22], fill=gold_fill)
        draw.ellipse([bx - 14, by - 14, bx + 14, by + 14], fill=(245, 220, 130))
        draw.ellipse([bx - 7, by - 7, bx + 7, by + 7], fill=gold_shade)

    # -------------------------------------------------------------------------
    # Region 3: Blouse, Corset & Ruby Necklace (UV: [0, 1024] to [1024, 2048])
    # -------------------------------------------------------------------------
    # Blouse cream silk: (245, 238, 220)
    draw.rectangle([0, 1024, 512, 2048], fill=(245, 238, 220))
    # Delicate lace ruffles
    for ry in range(1060, 2000, 60):
        draw.rectangle([20, ry - 3, 492, ry + 3], fill=(235, 225, 205))
        for rx in range(30, 490, 25):
            draw.ellipse([rx - 8, ry - 8, rx + 8, ry + 8], fill=(255, 252, 245))
            
    # Corset / Cummerbund: Charcoal/dark leather with gold eyelets
    draw.rectangle([512, 1024, 1024, 2048], fill=(38, 32, 36))
    for ey in range(1100, 1980, 55):
        # Left eyelet + gold rivet
        draw.ellipse([640 - 12, ey - 12, 640 + 12, ey + 12], fill=gold_fill)
        draw.ellipse([640 - 6, ey - 6, 640 + 6, ey + 6], fill=(18, 14, 16))
        # Right eyelet + gold rivet
        draw.ellipse([880 - 12, ey - 12, 880 + 12, ey + 12], fill=gold_fill)
        draw.ellipse([880 - 6, ey - 6, 880 + 6, ey + 6], fill=(18, 14, 16))
        # Gold criss-cross lacing
        draw.line([640, ey, 880, ey + 40], fill=gold_shade, width=5)
        draw.line([880, ey, 640, ey + 40], fill=gold_shade, width=5)
        
    # Signature Gold Necklace with Oval Ruby Pendant
    neck_center_x, neck_center_y = 256, 1750
    # Gold chain
    draw.ellipse([neck_center_x - 140, neck_center_y - 90, neck_center_x + 140, neck_center_y + 90], outline=gold_fill, width=8)
    # Filigree pendant setting
    draw.ellipse([neck_center_x - 45, neck_center_y + 40, neck_center_x + 45, neck_center_y + 110], fill=gold_shade)
    draw.ellipse([neck_center_x - 38, neck_center_y + 46, neck_center_x + 38, neck_center_y + 104], fill=gold_fill)
    # Glowing faceted Ruby gemstone
    draw.ellipse([neck_center_x - 28, neck_center_y + 54, neck_center_x + 28, neck_center_y + 96], fill=(195, 24, 46))
    # Facet reflection
    draw.polygon([(neck_center_x - 15, neck_center_y + 60), (neck_center_x + 8, neck_center_y + 60), (neck_center_x - 8, neck_center_y + 82)], fill=(255, 90, 110))
    draw.ellipse([neck_center_x - 12, neck_center_y + 62, neck_center_x - 4, neck_center_y + 70], fill=(255, 255, 255))

    # -------------------------------------------------------------------------
    # Region 4: Trousers, Boots & Articulated Tail (UV: [1024, 1024] to [2048, 2048])
    # -------------------------------------------------------------------------
    # Charcoal trousers with subtle pinstripes
    draw.rectangle([1024, 1024, 1600, 2048], fill=(42, 43, 48))
    for stripe_x in range(1040, 1600, 24):
        draw.line([stripe_x, 1024, stripe_x, 2048], fill=(55, 57, 64), width=2)
        
    # Black leather boots: (22, 22, 26)
    draw.rectangle([1600, 1024, 2048, 1536], fill=(22, 22, 26))
    # Boot sole and welt stitching
    draw.rectangle([1600, 1500, 2048, 1536], fill=(14, 14, 16))
    for stitch_x in range(1610, 2040, 14):
        draw.line([stitch_x, 1504, stitch_x + 6, 1504], fill=(180, 150, 100), width=2)
    # Boot polish highlight
    draw.ellipse([1750, 1200, 1920, 1380], fill=(45, 45, 52))

    # Jaguar Tail: amber fur with dark rosettes, fading to solid black tip
    draw.rectangle([1600, 1536, 2048, 2048], fill=(205, 138, 58))
    # Tail rosettes
    for ty in range(1560, 1900, 70):
        for tx in (1680, 1820, 1960):
            draw_rosette(tx, ty, scale=0.85)
    # Solid black tail tip
    draw.rectangle([1600, 1940, 2048, 2048], fill=(18, 14, 16))
    for b_y in range(1900, 1940):
        b_fac = (b_y - 1900) / 40.0
        c_val = int(205 * (1 - b_fac) + 18 * b_fac)
        draw.line([1600, b_y, 2048, b_y], fill=(c_val, int(138 * (1 - b_fac) + 14 * b_fac), int(58 * (1 - b_fac) + 16 * b_fac)))

    # Save Albedo
    albedo_path = OUT_DIR / "onca_base_color.jpg"
    albedo.save(str(albedo_path), "JPEG", quality=95)
    print(f"[TEXTURE] Saved Albedo -> {albedo_path}")

    # -------------------------------------------------------------------------
    # 2. METALLIC-ROUGHNESS MAP (RGB: R=unused/AO, G=Roughness, B=Metallic)
    # -------------------------------------------------------------------------
    # Default: non-metal (B=0), medium fur roughness (G=170 -> 0.66)
    mr = Image.new("RGB", (SIZE, SIZE), (255, 170, 0))
    draw_mr = ImageDraw.Draw(mr)
    
    # Head & Face: Roughness = 0.68 (G=173), Non-metal (B=0)
    draw_mr.rectangle([0, 0, 1024, 1024], fill=(255, 173, 0))
    # Nose pad: slightly glossy leather (G=90 -> 0.35)
    draw_mr.rounded_rectangle([472, 600, 552, 670], radius=18, fill=(255, 90, 0))
    # Eyes: glossy cornea (G=25 -> 0.10)
    for eye_x in (320, 704):
        draw_mr.ellipse([eye_x - 70, 480 - 45, eye_x + 70, 480 + 45], fill=(255, 25, 0))
    # Left Ear Gold Hoop Earring: Metallic 0.95 (B=242), Roughness 0.22 (G=56)
    draw_mr.ellipse([140, 260, 200, 320], fill=(255, 56, 242))

    # Jacket: Velvet Roughness = 0.82 (G=210), Non-metal (B=0)
    draw_mr.rectangle([1024, 0, 2048, 1024], fill=(255, 210, 0))
    # Gold Embroidery & Buttons on Jacket: Metallic 0.95 (B=242), Roughness 0.24 (G=61)
    for border_y in (120, 240, 560, 720, 920):
        draw_mr.rectangle([1060, border_y - 6, 2012, border_y + 6], fill=(255, 61, 242))
    for bx, by in button_coords:
        draw_mr.ellipse([bx - 26, by - 26, bx + 26, by + 26], fill=(255, 50, 245))

    # Blouse: Silk Roughness = 0.55 (G=140), Non-metal (B=0)
    draw_mr.rectangle([0, 1024, 512, 2048], fill=(255, 140, 0))
    # Corset: Leather Roughness = 0.40 (G=102)
    draw_mr.rectangle([512, 1024, 1024, 2048], fill=(255, 102, 0))
    # Gold eyelets & lacing: Metallic (B=242), Roughness (G=60)
    for ey in range(1100, 1980, 55):
        draw_mr.ellipse([640 - 12, ey - 12, 640 + 12, ey + 12], fill=(255, 60, 242))
        draw_mr.ellipse([880 - 12, ey - 12, 880 + 12, ey + 12], fill=(255, 60, 242))
        
    # Gold Necklace & Ruby Pendant:
    # Gold chain: Metallic (B=245), Roughness (G=50)
    draw_mr.ellipse([neck_center_x - 140, neck_center_y - 90, neck_center_x + 140, neck_center_y + 90], outline=(255, 50, 245), width=8)
    draw_mr.ellipse([neck_center_x - 45, neck_center_y + 40, neck_center_x + 45, neck_center_y + 110], fill=(255, 50, 245))
    # Ruby: Dielectric (B=0), High Gloss Roughness = 0.12 (G=30)
    draw_mr.ellipse([neck_center_x - 28, neck_center_y + 54, neck_center_x + 28, neck_center_y + 96], fill=(255, 30, 0))

    # Trousers: Wool Roughness = 0.75 (G=191), Non-metal (B=0)
    draw_mr.rectangle([1024, 1024, 1600, 2048], fill=(255, 191, 0))
    # Boots: Polished Leather Roughness = 0.32 (G=82), Non-metal (B=0)
    draw_mr.rectangle([1600, 1024, 2048, 1536], fill=(255, 82, 0))
    # Tail: Fur Roughness = 0.66 (G=170), Non-metal (B=0)
    draw_mr.rectangle([1600, 1536, 2048, 2048], fill=(255, 170, 0))

    mr_path = OUT_DIR / "onca_metallic_roughness.jpg"
    mr.save(str(mr_path), "JPEG", quality=95)
    print(f"[TEXTURE] Saved MetallicRoughness -> {mr_path}")

    # -------------------------------------------------------------------------
    # 3. NORMAL MAP (Tangent space: neutral flat is (128, 128, 255))
    # -------------------------------------------------------------------------
    normal = Image.new("RGB", (SIZE, SIZE), (128, 128, 255))
    draw_n = ImageDraw.Draw(normal)
    
    # Embroidered relief on jacket (simulated normal bevel)
    for border_y in (120, 240, 560, 720, 920):
        # Beveled top (+Y = green light) and bottom (-Y = green shadow)
        draw_n.line([1060, border_y - 6, 2012, border_y - 6], fill=(128, 185, 220), width=3)
        draw_n.line([1060, border_y + 6, 2012, border_y + 6], fill=(128, 70, 220), width=3)
        for fx in range(1080, 2000, 40):
            # Convex button/scallop normal hemisphere
            for rad in range(16, 0, -2):
                nx = int(128 + (rad / 16.0) * 45)
                ny = int(128 + (rad / 16.0) * 45)
                draw_n.ellipse([fx - rad, border_y - rad, fx + rad, border_y + rad], outline=(nx, ny, 235))
                
    # Buttons: 3D embossed dome normals
    for bx, by in button_coords:
        for r_dome in range(26, 0, -3):
            # Spherical normal gradient
            fac = r_dome / 26.0
            draw_n.ellipse([bx - r_dome, by - r_dome, bx + r_dome, by + r_dome], fill=(int(128 - fac * 35), int(128 + fac * 45), 235))
            
    # Eye sockets: beveled depth
    for eye_x in (320, 704):
        draw_n.ellipse([eye_x - 70, 480 - 45, eye_x + 70, 480 + 45], outline=(110, 160, 230), width=4)
        
    # Nose pad: beveled edge & nostril depressions
    draw_n.rounded_rectangle([472, 600, 552, 670], radius=18, outline=(128, 175, 230), width=3)
    draw_n.ellipse([484, 626, 506, 646], fill=(128, 90, 240)) # inward normal
    draw_n.ellipse([518, 626, 540, 646], fill=(128, 90, 240))
    
    # Necklace Ruby Pendant: faceted normal bevel
    draw_n.ellipse([neck_center_x - 45, neck_center_y + 40, neck_center_x + 45, neck_center_y + 110], outline=(145, 175, 220), width=4)
    draw_n.ellipse([neck_center_x - 28, neck_center_y + 54, neck_center_x + 28, neck_center_y + 96], outline=(115, 185, 210), width=4)
    
    # Boot soles & welt: sharp normal step
    draw_n.line([1600, 1500, 2048, 1500], fill=(128, 195, 215), width=4)
    draw_n.line([1600, 1536, 2048, 1536], fill=(128, 65, 215), width=4)

    # Slight gaussian blur on normal map to ensure smooth organic gradients
    normal_smooth = normal.filter(ImageFilter.GaussianBlur(radius=1.2))
    
    normal_path = OUT_DIR / "onca_normal.jpg"
    normal_smooth.save(str(normal_path), "JPEG", quality=95)
    print(f"[TEXTURE] Saved Normal -> {normal_path}")
    print("[TEXTURE] All 3 PBR texture maps created successfully!")

if __name__ == "__main__":
    create_pbr_maps()
