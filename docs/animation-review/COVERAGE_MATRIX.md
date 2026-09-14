# MultiGame — Animation Coverage Matrix

> Baseline Audit: Patch 26–32 | Godot 4.7.2 .NET / Blender 5.2.1
> Total Characters: 11 | Total Existing Clips: 333 | Target Unique Coverage: 100%

---

## 1. Character Rig & System Classification

| ID | Name | Role | Rig Architecture | Bone Count | Active Clips | Fallback Dependency |
|---|---|---|---|---:|---:|---|
| `corvo` | Seu Corvo | Playable | Full Armature + Wings (Wing.L/R) | 23 | 39 | Low (pilot character) |
| `iara` | Iara | Playable | Full Armature (Humanoid) | 21 | 39 | Low |
| `zeca` | Zeca | Playable | Full Armature + Tail | 25 | 39 | Low |
| `aki` | Aki | Playable | Full Armature (Humanoid) | 21 | 39 | Low |
| `barao` | Barão da Meia-Noite | Boss | Full Armature + Coat | 23 | 39 | Low |
| `dama` | Dama de Copas | Boss | Full Armature + Gown | 20 | 39 | Low |
| `morgana` | Madame Morgana | Boss | Full Armature + Cape | 21 | 39 | Low (repaired shoulder) |
| `carnical` | Lorde Carniçal | Boss | Full Armature (Hunched) | 21 | 39 | Low (repaired body) |
| `onca` | Dona Onça | Playable | Full Armature (Tail.01-05, Ears) | 26 | 9 | High (missing 30 clips) |
| `nina` | Nina | Playable | Rigid-Body Pivot Hierarchy | 0 | 7 | High (uses core 7 only) |
| `bento` | Bento | Playable | Rigid-Body Pivot Hierarchy | 0 | 7 | High (uses core 7 only) |

---

## 2. Complete Animation Coverage Matrix

| Category | Animation Clip | Nina | Bento | Corvo | Onça | Iara | Zeca | Aki | Barão | Dama | Morgana | Carniçal |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **IDLE** | `idle` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `idle_table_01` | ❌ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `idle_table_02` | ❌ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `idle_impatient` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `idle_relaxed` *(New)* | ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `idle_nervous` *(New)* | ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| **CARD** | `play_card` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `play_card_fast` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `play_card_dramatic` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `hold_cards` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `inspect_hand` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **DEALER** | `deal` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `shuffle` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `cut_deck` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **TRUCO** | `truco` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `accept_truco` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `decline_truco` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **VICTORY** | `victory` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `big_win` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `small_win` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `trick_win` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **DEFEAT** | `lose` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `trick_lose` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `bad_beat` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `life_lost` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **STAGING** | `entrance` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `boss_intro` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `flourish` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **EXPRESSION**| `think` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `taunt` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `laugh` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `surprised` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `suspicious` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **BETTING** | `check` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `bet` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `fold` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `all_in` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `showdown` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `bid_confident` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `bid_uncertain` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| | `bid_zero` | ❌ | ❌ | ✔ | ❌ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| **MICRO** | `nod` *(New)* | ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `shake_head` *(New)*| ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `lean_forward` *(New)*| ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `lean_back` *(New)* | ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `micro_sigh` *(New)* | ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| | `seat_adjust` *(New)*| ⏳ | ⏳ | 🎯 | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |

*Legend: ✔ = Implemented & verified | ❌ = Missing / using GestureFallback | 🎯 = Pilot implementation target (Corvo) | ⏳ = Planned next phase*

---

## 3. Fallback Impact Analysis

When a clip is missing, `GestureFallback` redirects the request:
- `trick_win` $\rightarrow$ `victory` (Causes full-match celebration for a single trick round!)
- `trick_lose` $\rightarrow$ `idle` (Character looks completely indifferent after losing a round!)
- `think` $\rightarrow$ `idle` (Character freezes while AI computes turn!)
- `hold_cards` $\rightarrow$ `idle`
- `inspect_hand` $\rightarrow$ `idle`
- `bid_*` $\rightarrow$ `idle`

### Primary Goal of Phase 1:
Break the dependency on `idle` fallbacks for micro-reactions and round resolution (`win_trick`, `lose_trick`), making rounds feel reactive and emotionally paced.
