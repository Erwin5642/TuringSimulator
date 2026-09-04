#!/usr/bin/env python3
"""Generates the agentic RAG flowchart (tools, vector/lexical search, embeddings,
request/response, fallback) as a standalone SVG with non-overlapping layout."""

WIDTH = 2000
HEIGHT = 2000

FILL_PROC = "#262629"
FILL_DECISION = "#2a2440"
FILL_TERMINAL = "#1f3a2e"
STROKE = "#4a4a52"
STROKE_DECISION = "#5b4f8a"
TEXT_PRIMARY = "#f2f2f3"
TEXT_SECONDARY = "#c7c7cd"
TEXT_TERTIARY = "#8f8f97"
STROKE_ARROW = "#a3a3ad"
ACCENT_YES = "#5ad18a"
ACCENT_NO = "#e0616b"
ACCENT_LOOP = "#5aa9ff"
ACCENT_DASH = "#c9a15a"
BG = "#151516"

parts: list[str] = []


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def _lines_block(cx, cy, lines, size, weight, fill, lh=15):
    n = len(lines)
    start_y = cy - (n - 1) * lh / 2 + size * 0.32
    out = []
    for i, line in enumerate(lines):
        out.append(
            f'<text x="{cx}" y="{start_y + i * lh:.1f}" text-anchor="middle" '
            f'font-size="{size}" font-weight="{weight}" fill="{fill}">{esc(line)}</text>'
        )
    return "\n".join(out)


def process(cx, cy, w, h, lines, sub=None, fill=FILL_PROC, stroke=STROKE, rx=9):
    parts.append(
        f'<rect x="{cx - w/2:.1f}" y="{cy - h/2:.1f}" width="{w}" height="{h}" '
        f'rx="{rx}" fill="{fill}" stroke="{stroke}" stroke-width="1.3" />'
    )
    body_cy = cy - (6 if sub else 0)
    parts.append(_lines_block(cx, body_cy, lines, 12.3, 600, TEXT_PRIMARY, lh=15))
    if sub:
        sub_y = cy + len(lines) * 15 / 2 + 4
        parts.append(_lines_block(cx, sub_y, sub, 10, 400, TEXT_TERTIARY, lh=12))
    return (cx, cy, w, h)


def diamond(cx, cy, w, h, lines):
    pts = f"{cx},{cy-h/2} {cx+w/2},{cy} {cx},{cy+h/2} {cx-w/2},{cy}"
    parts.append(
        f'<polygon points="{pts}" fill="{FILL_DECISION}" stroke="{STROKE_DECISION}" stroke-width="1.4" />'
    )
    parts.append(_lines_block(cx, cy, lines, 11.8, 600, TEXT_PRIMARY, lh=14))
    return (cx, cy, w, h)


def terminal(cx, cy, w, h, lines, fill=FILL_TERMINAL):
    parts.append(
        f'<rect x="{cx - w/2:.1f}" y="{cy - h/2:.1f}" width="{w}" height="{h}" '
        f'rx="{h/2:.1f}" fill="{fill}" stroke="#3f7a5c" stroke-width="1.3" />'
    )
    parts.append(_lines_block(cx, cy, lines, 11.8, 600, TEXT_PRIMARY, lh=14))
    return (cx, cy, w, h)


def label_box(cx, cy, text_, size=10.3, color=TEXT_SECONDARY):
    w = max(30, len(text_) * size * 0.62 + 12)
    h = size + 8
    parts.append(f'<rect x="{cx - w/2:.1f}" y="{cy - h/2:.1f}" width="{w}" height="{h}" fill="{BG}" />')
    parts.append(f'<text x="{cx}" y="{cy + size*0.35:.1f}" text-anchor="middle" font-size="{size}" fill="{color}">{esc(text_)}</text>')


def arrow(points, color=STROKE_ARROW, dashed=False, marker="mArrow", width=1.6):
    d = ' stroke-dasharray="6 4"' if dashed else ""
    parts.append(
        f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{width}"{d} marker-end="url(#{marker})" />'
    )


def pts(*xy):
    return " ".join(f"{x},{y}" for x, y in xy)


# ============================================================ header
parts.append(f'<rect x="0" y="0" width="{WIDTH}" height="{HEIGHT}" fill="{BG}" />')
parts.append(
    f'<text x="30" y="34" font-size="19" font-weight="600" fill="{TEXT_PRIMARY}">'
    f'Fluxograma do Loop Agentic (RAG) — TuringBotAPI</text>'
)
parts.append(
    f'<text x="30" y="56" font-size="12.5" fill="{TEXT_SECONDARY}">'
    f'Requisição /ask, chamadas de ferramenta (search_docs), busca vetorial/lexical, embeddings e fallback offline — agent.py, tutor_provider.py, rag/store.py.</text>'
)

# ============================================================ defs
parts.append(
    '<defs>'
    f'<marker id="mArrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{STROKE_ARROW}" /></marker>'
    f'<marker id="mYes" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{ACCENT_YES}" /></marker>'
    f'<marker id="mNo" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{ACCENT_NO}" /></marker>'
    f'<marker id="mLoop" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{ACCENT_LOOP}" /></marker>'
    f'<marker id="mDash" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{ACCENT_DASH}" /></marker>'
    '</defs>'
)

# ============================================================ init band (top, dashed container)
INIT_TOP = 78
INIT_H = 190
parts.append(
    f'<rect x="640" y="{INIT_TOP}" width="1110" height="{INIT_H}" rx="10" fill="#1c1c2a" '
    f'stroke="#3a3560" stroke-width="1.2" stroke-dasharray="5 4" />'
)
parts.append(f'<text x="660" y="{INIT_TOP+20}" font-size="12" font-weight="600" fill="{TEXT_SECONDARY}">Inicialização do servidor (executa uma única vez, no boot)</text>')

i1 = process(830, INIT_TOP + 80, 300, 70, ["Carrega corpus Markdown", "e calcula hash sha256 do", "conteúdo de cada documento"])
i2 = diamond(1150, INIT_TOP + 90, 230, 95, ["cache SQLite tem", "entrada válida", "(doc_id + hash)?"])
i3a = process(1150, INIT_TOP + 8, 210, 46, ["reaproveita vetor do cache"])
i3b = process(1440, INIT_TOP + 90, 260, 70, ["embed_document(texto)", "via Gemini embeddings →", "grava vetor no cache SQLite"])
i4 = process(1150, INIT_TOP + 168, 320, 40, ["indexa vetores em memória (self._vectors, por doc_id)"])

arrow(pts((980, INIT_TOP+80), (1035, INIT_TOP+90)))
arrow(pts((1150, INIT_TOP+42), (1150, INIT_TOP+31)), color=ACCENT_YES, marker="mYes")
label_box(1200, INIT_TOP+40, "sim", color=ACCENT_YES)
arrow(pts((1265, INIT_TOP+90), (1310, INIT_TOP+90)), color=ACCENT_NO, marker="mNo")
label_box(1290, INIT_TOP+78, "não", color=ACCENT_NO)
arrow(pts((1150, INIT_TOP+31), (1150, INIT_TOP+120)))
arrow(pts((1440, INIT_TOP+125), (1440, INIT_TOP+168), (1310, INIT_TOP+168)))

# ============================================================ MAIN LANE
MX = 380
process_w = 330

m1 = process(MX, 340, process_w, 66, ["Requisição POST /ask recebida"], sub=["student_id, level_id, question"])
m2 = diamond(MX, 460, 260, 90, ["question.strip()", "vazia?"])
eq1 = terminal(90, 460, 190, 56, ["erro 400", "(ValueError)"], fill="#3a2130")
m3 = diamond(MX, 610, 280, 100, ["is_greeting(question)?", "(regex de saudação)"])
sg1 = process(90, 610, 210, 66, ["greeting_reply(question)"], sub=["resposta fixa, sem busca"])
m4 = process(MX, 760, process_w, 66, ["Monta system prompt"], sub=["persona + level_id (build_system_prompt)"])
m5 = process(MX, 880, process_w, 78, ["provider.generate_with_tools()"], sub=["Gemini generate_content_async", "tool_config: function_calling mode=auto"])
m6 = diamond(MX, 1010, 300, 105, ["resposta contém", "function_call", "('search_docs')?"])
m7 = process(MX, 1160, process_w, 60, ["Extrai texto final (response.text)"])
m8 = diamond(MX, 1280, 300, 115, ["texto vazio OU", "TutorProviderUnavailable /", "Exception?"])
m9 = process(MX, 1440, process_w, 66, ["GenerationResult(texto,"], sub=["tokens_in, tokens_out)"])
m10 = terminal(MX, 1600, 380, 78, ["Resposta final ao cliente"], fill="#1f3a2e")
label_box(MX, 1622, "reply, tokens_in, tokens_out (retorno de POST /ask)", size=10.3, color=TEXT_TERTIARY)

arrow(pts((MX, 373), (MX, 415)))
arrow(pts((MX-130, 460), (185, 460)), color=ACCENT_NO, marker="mNo")
label_box(300, 448, "vazia", color=ACCENT_NO)
arrow(pts((MX, 505), (MX, 560)), color=ACCENT_YES, marker="mYes")
label_box(MX+55, 530, "ok", color=ACCENT_YES)
arrow(pts((MX-140, 610), (195, 610)), color=ACCENT_YES, marker="mYes")
label_box(300, 598, "sim", color=ACCENT_YES)
arrow(pts((MX, 660), (MX, 727)), color=ACCENT_NO, marker="mNo")
label_box(MX+55, 690, "não", color=ACCENT_NO)
arrow(pts((MX, 793), (MX, 841)))
arrow(pts((MX, 919), (MX, 957)))
arrow(pts((MX, 1063), (MX, 1130)), color=ACCENT_NO, marker="mNo")
label_box(MX+60, 1096, "não", color=ACCENT_NO)
arrow(pts((MX, 1190), (MX, 1222)))
arrow(pts((MX, 1338), (MX, 1407)), color=ACCENT_NO, marker="mNo")
label_box(MX+60, 1372, "não", color=ACCENT_NO)
arrow(pts((MX, 1473), (MX, 1560)))
arrow(pts((90, 643), (90, 1600), (MX-190, 1600)), color=ACCENT_YES, marker="mYes")
label_box(90, 900, "sem RAG", color=ACCENT_YES)

# ============================================================ RIGHT LANE (search_docs subprocess)
RX = 1230
rw = 340

r1 = process(RX, 1010, rw, 70, ["execute_tool('search_docs', args)"], sub=["chamado a partir do function_call do Gemini"])
r2 = diamond(RX, 1140, 300, 100, ["rounds ≥", "MAX_TOOL_ROUNDS (3)?"])
r2a = process(1560, 1140, 240, 60, ["{error: limite atingido,", "chunks: []}"])
r3 = process(RX, 1260, rw, 60, ["rounds += 1 · store.search(query,"], sub=["category, level_id) — filtra pool por categoria"])
r4 = process(RX, 1360, rw, 60, ["embed_query(query)"], sub=["Gemini embeddings, ou nulo (offline)"])
r5 = diamond(RX, 1480, 320, 110, ["vetores pré-computados", "disponíveis p/ todo o pool", "filtrado?"])
r5a = process(1010, 1610, 280, 74, ["Busca vetorial:", "cosine_similarity(query, doc)"], sub=["+ LEVEL_BOOST (+0.12 se nível bate)"])
r5b = process(1440, 1610, 280, 74, ["Busca lexical:", "overlap de tokens (Jaccard)"], sub=["+ LEVEL_BOOST (+0.12 se nível bate)"])
r6 = process(RX, 1720, rw, 56, ["Ordena por score, filtra > 0,", "top_k=4 → SearchHits"])
r7 = process(RX, 1810, rw, 56, ["format_hits() → {chunks: [...]}", "devolvido como function_response"])

arrow(pts((MX+150, 1010), (1060, 1010)), color=ACCENT_YES, marker="mYes")
label_box((MX+150+1060)/2, 998, "sim", color=ACCENT_YES)
arrow(pts((RX, 1045), (RX, 1090)))
arrow(pts((RX+150, 1140), (1440, 1140)), color=ACCENT_YES, marker="mYes")
label_box(1460, 1128, "sim", color=ACCENT_YES)
arrow(pts((RX, 1190), (RX, 1230)), color=ACCENT_NO, marker="mNo")
label_box(RX+55, 1210, "não", color=ACCENT_NO)
arrow(pts((RX, 1290), (RX, 1330)))
arrow(pts((RX, 1390), (RX, 1425)))
arrow(pts((RX-160, 1480), (1010, 1480), (1010, 1573)), color=ACCENT_YES, marker="mYes")
label_box(1080, 1468, "sim", color=ACCENT_YES)
arrow(pts((RX+160, 1480), (1440, 1480), (1440, 1573)), color=ACCENT_NO, marker="mNo")
label_box(1370, 1468, "não", color=ACCENT_NO)
arrow(pts((1010, 1647), (1010, 1692), (RX, 1692)))
arrow(pts((1440, 1647), (1440, 1692), (RX, 1692)))
arrow(pts((RX, 1748), (RX, 1782)))

TRUNK_X = 1800
arrow(
    pts((RX+170, 1810), (TRUNK_X, 1810), (TRUNK_X, 880), (MX+150, 880)),
    color=ACCENT_LOOP, marker="mLoop",
)
arrow(
    pts((1560+120, 1140), (TRUNK_X, 1140)),
    color=ACCENT_LOOP, marker="mLoop",
)
label_box(TRUNK_X, 1560, "anexa function_response,", size=10.3, color=ACCENT_LOOP)
label_box(TRUNK_X, 1580, "rounds+1, chama Gemini", size=10.3, color=ACCENT_LOOP)
label_box(TRUNK_X, 1600, "de novo (mode=none se", size=10.3, color=ACCENT_LOOP)
label_box(TRUNK_X, 1620, "rounds == 3)", size=10.3, color=ACCENT_LOOP)

# dashed connector: init vectors feed the vector-availability decision (routed
# through the clear right margin so it does not cross r2/r3/r4)
DASH_X = 1900
arrow(
    pts((1150, INIT_TOP + INIT_H), (1150, INIT_TOP + INIT_H + 20), (DASH_X, INIT_TOP + INIT_H + 20),
        (DASH_X, 1440), (RX + 195, 1440)),
    color=ACCENT_DASH, dashed=True, marker="mDash",
)
label_box(DASH_X, 900, "vetores", size=10, color=ACCENT_DASH)
label_box(DASH_X, 920, "pré-computados", size=10, color=ACCENT_DASH)
label_box(DASH_X, 940, "(1x no boot)", size=10, color=ACCENT_DASH)

# ============================================================ FALLBACK subprocess
FX = 800
f1 = process(FX, 1160, 300, 66, ["search_docs(store, question, level_id)"], sub=["chamada única, fora do loop de tools"])
f2 = process(FX, 1300, 300, 78, ["fallback_reply(hits):"], sub=["monta resposta offline com frases", "\u201cdo jogador\u201d extraídas de até 2 hits"])

arrow(pts((MX+150, 1280), (FX-150, 1280), (FX-150, 1160-33)), color=ACCENT_YES, marker="mYes")
label_box(560, 1268, "sim (sem provedor / sem texto)", color=ACCENT_YES)
arrow(pts((FX, 1193), (FX, 1261)))
arrow(pts((FX, 1339), (FX, 1600), (MX+190, 1600)))

# ============================================================ legend
ly = HEIGHT - 60
parts.append(f'<rect x="30" y="30" width="1" height="1" fill="none" />')  # noop guard
arrow(pts((30, ly), (68, ly)), marker="mArrow")
label_box(160, ly+2, "fluxo sequencial", size=11.5, color=TEXT_SECONDARY)
arrow(pts((320, ly), (358, ly)), color=ACCENT_YES, marker="mYes")
label_box(430, ly+2, "ramo \u201csim\u201d", size=11.5, color=TEXT_SECONDARY)
arrow(pts((560, ly), (598, ly)), color=ACCENT_NO, marker="mNo")
label_box(670, ly+2, "ramo \u201cnão\u201d", size=11.5, color=TEXT_SECONDARY)
arrow(pts((800, ly), (838, ly)), color=ACCENT_LOOP, marker="mLoop")
label_box(940, ly+2, "loop de volta ao Gemini", size=11.5, color=TEXT_SECONDARY)
arrow(pts((1160, ly), (1198, ly)), color=ACCENT_DASH, dashed=True, marker="mDash")
label_box(1330, ly+2, "dados pré-computados (boot)", size=11.5, color=TEXT_SECONDARY)

parts.append(f'<text x="30" y="{HEIGHT-24}" font-size="10.5" fill="{TEXT_TERTIARY}">Fonte: TuringBotAPI/agent.py, TuringBotAPI/tutor_provider.py, TuringBotAPI/rag/store.py (estado atual do repositório).</text>')

svg = (
    '<?xml version="1.0" encoding="UTF-8"?>\n'
    f'<svg width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}" '
    f'xmlns="http://www.w3.org/2000/svg" font-family="Helvetica, Arial, sans-serif">\n'
    + "\n".join(parts) + "\n</svg>\n"
)

with open("its-rag-agentic-flowchart.svg", "w") as f:
    f.write(svg)

print("wrote its-rag-agentic-flowchart.svg", WIDTH, HEIGHT)
