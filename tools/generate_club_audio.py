"""Reproducible original instrumental loops and card sounds; Python standard library only."""
from array import array
from pathlib import Path
import math
import random
import wave

ROOT = Path(__file__).resolve().parents[1] / "assets" / "audio"
RATE = 22050
TAU = math.tau

def note(buffer, start, duration, midi, gain, kind="keys", pan=0):
    frequency = 440 * 2 ** ((midi - 69) / 12)
    first = int(start * RATE)
    length = int(duration * RATE)
    for j in range(length):
        t = j / RATE
        attack = min(1, t / .012)
        release = min(1, (length - j) / (RATE * .09))
        if kind == "bass":
            signal = math.sin(TAU * frequency * t) + .16 * math.sin(TAU * frequency * 2 * t)
            envelope = math.exp(-t * 3)
        elif kind == "pluck":
            signal = math.sin(TAU * frequency * t) + .32 * math.sin(TAU * frequency * 2 * t) + .13 * math.sin(TAU * frequency * 3 * t)
            envelope = math.exp(-t * 4.5)
        else:
            signal = math.sin(TAU * frequency * t + .6 * math.sin(TAU * frequency * 2 * t) * math.exp(-t * 4))
            signal += .12 * math.sin(TAU * frequency * 3 * t) * math.exp(-t * 5)
            envelope = math.exp(-t * 2.1)
        value = signal * envelope * attack * release * gain
        # Wrapping tails makes each loop join without an artificial silence.
        i = ((first + j) % (len(buffer) // 2)) * 2
        buffer[i] += value * (1 - pan * .35)
        buffer[i + 1] += value * (1 + pan * .35)

def percussion(buffer, start, gain, rng, low=False):
    length = int(RATE * (.2 if low else .09))
    last = 0
    for j in range(length):
        t = j / RATE
        noise = rng.uniform(-1, 1)
        value = (math.sin(TAU * (70 - 100 * t) * t) if low else noise - last) * math.exp(-t * (22 if low else 65)) * gain
        last = noise
        i = ((int(start * RATE) + j) % (len(buffer) // 2)) * 2
        buffer[i] += value; buffer[i + 1] += value

def write(name, buffer):
    ROOT.mkdir(parents=True, exist_ok=True)
    peak = max(abs(value) for value in buffer) or 1
    gain = .68 / max(1, peak)
    pcm = array("h", (int(max(-1, min(1, value * gain)) * 32767) for value in buffer))
    with wave.open(str(ROOT / (name + ".wav")), "wb") as out:
        out.setnchannels(2); out.setsampwidth(2); out.setframerate(RATE); out.writeframes(pcm.tobytes())
    print(name, round(len(buffer) / 2 / RATE, 2), "seconds", "peak", round(peak * gain, 3))

def track(name, bpm, chords, seed, pluck=False):
    rng = random.Random(seed)
    beat = 60 / bpm
    buffer = array("f", [0]) * (int(beat * 32 * RATE) * 2)
    for bar in range(8):
        chord = chords[bar % len(chords)]
        start = bar * 4 * beat
        for b in (0, 1.5, 2.5):
            for n, midi in enumerate(chord):
                note(buffer, start + b * beat + n * .009, beat * 1.8, midi, .038, "pluck" if pluck else "keys", (n - 1.5) / 2)
        for b, interval in ((0, -24), (2, -17)):
            note(buffer, start + b * beat, beat * 1.5, chord[0] + interval, .095, "bass")
        melody = [chord[2] + 12, chord[1] + 12, chord[3] + 12, chord[2] + 12]
        for n, midi in enumerate(melody):
            if (n + bar) % 3:
                note(buffer, start + (n + .5) * beat, beat * .85, midi, .033, "pluck", .3)
        for b in range(8):
            percussion(buffer, start + b * beat / 2, .012 if b % 2 else .008, rng)
        for b in (0, 2): percussion(buffer, start + b * beat, .055, rng, True)
    write(name, buffer)

if __name__ == "__main__":
    track("midnight-club", 80, [[57, 60, 64, 67], [53, 57, 60, 64], [55, 59, 62, 65], [52, 56, 59, 62]], 41)
    track("velvet-table", 92, [[60, 64, 67, 71], [57, 60, 64, 67], [62, 65, 69, 72], [55, 59, 62, 65]], 42)
    track("last-manilha", 108, [[62, 65, 69, 72], [58, 62, 65, 69], [60, 64, 67, 70], [57, 61, 64, 67]], 43, True)
    track("copper-steps", 96, [[65,69,72,76],[62,65,69,72],[67,71,74,77],[60,64,67,70]], 61, True)
    track("midnight-baron", 72, [[48,51,55,59],[44,48,51,55],[46,50,53,57],[43,47,50,54]], 62)
    for name, offsets in {"shuffle": [0, .07, .14, .21, .28], "cut": [0, .13], "deal": [0, .11, .22, .33], "play": [0], "select": [0]}.items():
        buffer = array("f", [0]) * int((max(offsets) + .24) * RATE * 2)
        rng = random.Random(7)
        for at in offsets: percussion(buffer, at, .12, rng)
        write(name, buffer)
    for name, notes in {"score": [72, 76, 79], "win": [60, 64, 67, 72], "truco": [50, 57, 62], "buy": [76, 79], "arrival": [60,67,72,76], "boss-arrival": [36,43,48,51,59]}.items():
        buffer = array("f", [0]) * int(1.2 * RATE * 2)
        for n, midi in enumerate(notes): note(buffer, n * .065, .7, midi, .12, "pluck")
        write(name, buffer)
