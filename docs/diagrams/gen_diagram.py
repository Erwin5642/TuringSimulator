#!/usr/bin/env python3
"""Generates the ITS system architecture SVG with guaranteed non-overlapping
layout (columns spaced wide enough for the longest edge label in each gap)."""

WIDTH = 1650
HEIGHT = 800

COL_GAP = 210  # horizontal clearance between node edges of adjacent columns

BAND_A_X = 30
NODE_A_X = BAND_A_X + 20
NODE_A_RIGHT = NODE_A_X + 220

NODE_B_X = NODE_A_RIGHT + COL_GAP
BAND_B_X = NODE_B_X - 20
NODE_B_RIGHT = NODE_B_X + 220

NODE_C_X = NODE_B_RIGHT + COL_GAP
BAND_C_X = NODE_C_X - 20
NODE_C_RIGHT = NODE_C_X + 220

NODE_D_X = NODE_C_RIGHT + COL_GAP
BAND_D_X = NODE_D_X - 20
NODE_D_RIGHT = NODE_D_X + 220

BAND_W = 260
BAND_TOP = 100
BAND_H = 560

FILL_NODE = "#262629"
STROKE_NODE = "#4a4a52"
FILL_BAND = "#232326"
STROKE_BAND = "#33333a"
TEXT_PRIMARY = "#f2f2f3"
TEXT_SECONDARY = "#c2c2c8"
TEXT_TERTIARY = "#8f8f97"
STROKE_ARROW = "#a3a3ad"
ACCENT = "#5aa9ff"
BG = "#18181a"
LABEL_BG = BG

parts = []


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def rect(x, y, w, h, fill, stroke=None, sw=1.2, rx=10, dash=None):
    d = f' stroke-dasharray="{dash}"' if dash else ""
    st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}"{st}{d} />')


def text(x, y, s, size=11, weight=400, fill=TEXT_SECONDARY, anchor="middle"):
    parts.append(
        f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-size="{size}" '
        f'font-weight="{weight}" fill="{fill}">{esc(s)}</text>'
    )


def node(x, y, w, h, title_lines, sub_lines, dashed=False):
    rect(x, y, w, h, FILL_NODE, STROKE_NODE, 1.3, 10, "4 3" if dashed else None)
    cx = x + w / 2
    title_start = y + 24
    for i, line in enumerate(title_lines):
        text(cx, title_start + i * 17, line, size=13, weight=600, fill=TEXT_PRIMARY)
    sub_start = title_start + len(title_lines) * 17 + 8
    for i, (s, size, tone) in enumerate(sub_lines):
        fill = TEXT_TERTIARY if tone == "tertiary" else TEXT_SECONDARY
        text(cx, sub_start + i * 15, s, size=size, fill=fill)


def edge_label(cx, cy, s, size=10.5, pad_x=9, pad_y=4):
    w = max(40, len(s) * size * 0.62 + pad_x * 2)
    h = size + pad_y * 2
    rect(cx - w / 2, cy - h / 2, w, h, LABEL_BG, rx=3)
    text(cx, cy + size * 0.35, s, size=size, fill=TEXT_SECONDARY)
    return w


def arrow(points, color=STROKE_ARROW, dashed=False, both=False, marker="mPrimary"):
    d = ' stroke-dasharray="6 4"' if dashed else ""
    ms = f' marker-start="url(#{marker})"' if both else ""
    parts.append(
        f'<polyline points="{points}" fill="none" stroke="{color}" '
        f'stroke-width="1.7"{d} marker-end="url(#{marker})"{ms} />'
    )


# ---------- document header ----------
parts.append(f'<rect x="0" y="0" width="{WIDTH}" height="{HEIGHT}" fill="{BG}" />')
text(30, 32, "Arquitetura do Sistema Tutor — TuringSimulator", size=19, weight=600, fill=TEXT_PRIMARY, anchor="start")
text(
    30, 54,
    "Cliente Unity/VR, servidor FastAPI (TuringBotAPI) e serviço externo, com o contrato REST "
    "(/session/new, /ask, /health) como fronteira entre cliente e servidor.",
    size=12.5, fill=TEXT_SECONDARY, anchor="start",
)

# ---------- bands ----------
bands = [
    (BAND_A_X, "Cliente Unity / VR", "Ambiente imersivo (Meta Quest)"),
    (BAND_B_X, "Servidor FastAPI", "TuringBotAPI · Python"),
    (BAND_C_X, "RAG / Conhecimento", "Recuperação aumentada por geração"),
    (BAND_D_X, "Serviço externo", "Google Gemini (LLM + embeddings)"),
]
for bx, title, sub in bands:
    rect(bx, BAND_TOP, BAND_W, BAND_H, FILL_BAND, STROKE_BAND, 1, 12)
for bx, title, sub in bands:
    cx = bx + BAND_W / 2
    text(cx, BAND_TOP + 22, title, size=14, weight=600, fill=TEXT_PRIMARY)
    text(cx, BAND_TOP + 39, sub, size=11, fill=TEXT_TERTIARY)

# ---------- nodes ----------
A1 = (NODE_A_X, 120, 220, 76)
A2 = (NODE_A_X, 226, 220, 76)
A3 = (NODE_A_X, 332, 220, 76)
A4 = (NODE_A_X, 438, 220, 76)
A5 = (NODE_A_X, 544, 220, 76)

node(*A1, ["Gestos (Hand Tracking)"], [("Meta XR Hands · gesto de fala", 11, "sec")])
node(*A2, ["STT — Wit.ai (pt-BR)"], [("Transcrição de voz do jogador", 11, "sec")])
node(*A3, ["Cliente REST (ITSClient)"], [("UnityWebRequest · snake_case", 11, "sec")])
node(*A4, ["TTS — Wit.ai (EN)"], [("Síntese de fala do tutor", 11, "sec")])
node(*A5, ["Avatar / NPC Tutor"], [("Legenda pt-BR + animação", 11, "sec")])

B1 = (NODE_B_X, 120, 220, 76)
B2 = (NODE_B_X, 332, 220, 92)
B3 = (NODE_B_X, 448, 220, 92)

node(*B1, ["Web Tester"], [("Servido em /web-tester/", 11, "sec")], dashed=True)
node(*B2, ["API FastAPI"], [("/ask · /session/new · /health", 11, "sec"), ("main.py", 9, "tertiary")])
node(*B3, ["Loop Agentic (RAG)"], [("orquestra busca e geração", 11, "sec"), ("agent.py", 9, "tertiary")])

C1 = (NODE_C_X, 448, 220, 92)
C2 = (NODE_C_X, 564, 105, 76)
C3 = (NODE_C_X + 115, 564, 105, 76)

node(*C1, ["KnowledgeStore"], [("índice em memória + cache", 11, "sec"), ("rag/store.py", 9, "tertiary")])
node(*C2, ["Corpus", "Markdown"], [("knowledge/*.md", 10, "sec")])
node(*C3, ["Cache", "SQLite"], [("embeddings.sqlite", 10, "sec")])

D1 = (NODE_D_X, 448, 220, 92)
node(*D1, ["Gemini API"], [("Geração de texto", 11, "sec"), ("+ embeddings (externo)", 10, "tertiary")])

# ---------- edges ----------
parts.append(
    '<defs>'
    '<marker id="mPrimary" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{STROKE_ARROW}" /></marker>'
    '<marker id="mAccent" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{ACCENT}" /></marker>'
    '</defs>'
)

Acx = NODE_A_X + 110

# client-side vertical chain
arrow(f"{Acx},196 {Acx},226")
arrow(f"{Acx},302 {Acx},332")
arrow(f"{Acx},408 {Acx},438")
arrow(f"{Acx},514 {Acx},544")

# REST boundary (request / reply)
a3_right = A3[0] + A3[2]
b2_left = B2[0]
mid_ab = (a3_right + b2_left) / 2
req_y = 356
rep_y = 396
arrow(f"{a3_right},{req_y} {b2_left},{req_y}")
edge_label(mid_ab, req_y - 14, "POST /ask, /session/new")
arrow(f"{b2_left},{rep_y} {a3_right},{rep_y}", color=ACCENT, dashed=True, marker="mAccent")
edge_label(mid_ab, rep_y + 14, "reply, tokens_in/out")

# web tester -> API (vertical, within column B)
Bcx = NODE_B_X + 110
arrow(f"{Bcx},196 {Bcx},332")
edge_label(Bcx, 264, "mesmo contrato REST")

# API -> agentic loop
arrow(f"{Bcx},424 {Bcx},448")

# agentic <-> knowledgestore
b3_right = B3[0] + B3[2]
c1_left = C1[0]
mid_bc = (b3_right + c1_left) / 2
sync_y = 494
arrow(f"{b3_right},{sync_y} {c1_left},{sync_y}", both=True)
edge_label(mid_bc, sync_y - 20, "search_docs (≤ 3x)")

# knowledgestore -> corpus / cache
c2_cx = C2[0] + C2[2] / 2
c3_cx = C3[0] + C3[2] / 2
arrow(f"{c2_cx},{C1[1]+C1[3]} {c2_cx},{C2[1]}")
edge_label(c2_cx - 55, (C1[1] + C1[3] + C2[1]) / 2, "carga inicial", size=10)
arrow(f"{c3_cx},{C1[1]+C1[3]} {c3_cx},{C3[1]}")
edge_label(c3_cx + 60, (C1[1] + C1[3] + C2[1]) / 2, "cache por hash", size=10)

# agentic <-> gemini, routed above the RAG column
route_y = B2[1] - 22  # comfortably above B2's top edge, in the empty band header area is avoided
route_y = 404  # between B2 bottom (424) is below; must sit above nodes -> recompute
top_clear_y = min(B2[1] - 8, C1[1] - 8)  # highest safe y above C1/D1 tops (448-8=440) and clear of B2 (332..424)
# route strictly between B2 bottom (424) and C1/D1 top (448)
route_y = (424 + 448) / 2
d1_left = D1[0]
arrow(f"{b3_right},{sync_y} {b3_right},{route_y} {d1_left},{route_y} {d1_left},{sync_y}", both=True)
edge_label((b3_right + d1_left) / 2, route_y - 16, "generate_with_tools + embeddings")

# ---------- legend ----------
ly = HEIGHT - 90
lx = 30
arrow(f"{lx},{ly} {lx+40},{ly}")
text(lx + 50, ly + 4, "Fluxo de execução / requisição", size=11.5, anchor="start")

lx2 = 340
arrow(f"{lx2},{ly} {lx2+40},{ly}", color=ACCENT, dashed=True, marker="mAccent")
text(lx2 + 50, ly + 4, "Fluxo de resposta (reply)", size=11.5, anchor="start")

lx3 = 620
arrow(f"{lx3},{ly} {lx3+44},{ly}", both=True)
text(lx3 + 54, ly + 4, "Chamada síncrona (ida e volta)", size=11.5, anchor="start")

lx4 = 940
rect(lx4, ly - 7, 40, 14, "none", STROKE_NODE, 1.2, 3, dash="4 3")
text(lx4 + 50, ly + 4, "Consumidor alternativo (sem VR)", size=11.5, anchor="start")

text(30, HEIGHT - 40,
     "Fonte: reconstrução a partir de docs/client/README.md, docs/server/README.md, TuringBotAPI/main.py,",
     size=10.5, fill=TEXT_TERTIARY, anchor="start")
text(30, HEIGHT - 24,
     "TuringBotAPI/agent.py, TuringBotAPI/rag/store.py e Assets/TuringSimulator/ITS/ (estado atual do repositório).",
     size=10.5, fill=TEXT_TERTIARY, anchor="start")

svg = (
    f'<?xml version="1.0" encoding="UTF-8"?>\n'
    f'<svg width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}" '
    f'xmlns="http://www.w3.org/2000/svg" font-family="Helvetica, Arial, sans-serif">\n'
    + "\n".join(parts) + "\n</svg>\n"
)

with open("its-system-architecture.svg", "w") as f:
    f.write(svg)

print("wrote its-system-architecture.svg", WIDTH, HEIGHT)
print("NODE_A_X..RIGHT", NODE_A_X, NODE_A_RIGHT)
print("NODE_B_X..RIGHT", NODE_B_X, NODE_B_RIGHT)
print("NODE_C_X..RIGHT", NODE_C_X, NODE_C_RIGHT)
print("NODE_D_X..RIGHT", NODE_D_X, NODE_D_RIGHT)
