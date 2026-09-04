#!/usr/bin/env python3
"""High-level (conceptual, not code-coupled) architecture diagram of the
agentic RAG flow: semantic search, embeddings, and Gemini usage."""

WIDTH = 2080
HEIGHT = 1300

FILL_PROC = "#262629"
FILL_AGENT = "#2a2440"
FILL_EXTERNAL = "#1f2f42"
FILL_KB = "#243424"
STROKE = "#4a4a52"
STROKE_AGENT = "#5b4f8a"
STROKE_EXTERNAL = "#3a6f9a"
STROKE_KB = "#4f8a52"
TEXT_PRIMARY = "#f2f2f3"
TEXT_SECONDARY = "#c7c7cd"
TEXT_TERTIARY = "#8f8f97"
FWD = "#8b8b93"
REPLY = "#5ad1c8"
FALLBACK = "#e0a13a"
BG = "#141415"

parts: list[str] = []


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def _block(cx, cy, lines, size, weight, fill, lh=15):
    n = len(lines)
    start_y = cy - (n - 1) * lh / 2 + size * 0.32
    return "\n".join(
        f'<text x="{cx}" y="{start_y + i*lh:.1f}" text-anchor="middle" font-size="{size}" '
        f'font-weight="{weight}" fill="{fill}">{esc(l)}</text>'
        for i, l in enumerate(lines)
    )


def box(cx, cy, w, h, title, sub=None, fill=FILL_PROC, stroke=STROKE, dashed=False, rx=12):
    d = ' stroke-dasharray="5 4"' if dashed else ""
    parts.append(
        f'<rect x="{cx-w/2:.1f}" y="{cy-h/2:.1f}" width="{w}" height="{h}" rx="{rx}" '
        f'fill="{fill}" stroke="{stroke}" stroke-width="1.4"{d} />'
    )
    title_cy = cy - (10 if sub else 0)
    parts.append(_block(cx, title_cy, title, 13.5, 650, TEXT_PRIMARY, lh=17))
    if sub:
        sub_cy = cy + len(title) * 17 / 2 + 5
        parts.append(_block(cx, sub_cy, sub, 10.8, 400, TEXT_SECONDARY, lh=13))


def label(cx, cy, text_, size=10.6, color=TEXT_SECONDARY, bg=BG):
    w = max(30, len(text_) * size * 0.6 + 14)
    h = size + 8
    parts.append(f'<rect x="{cx-w/2:.1f}" y="{cy-h/2:.1f}" width="{w}" height="{h}" fill="{bg}" />')
    parts.append(f'<text x="{cx}" y="{cy+size*0.35:.1f}" text-anchor="middle" font-size="{size}" fill="{color}">{esc(text_)}</text>')


def arrow(points, color=FWD, dashed=False, marker="mFwd", width=1.7):
    d = ' stroke-dasharray="6 4"' if dashed else ""
    parts.append(
        f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="{width}"{d} marker-end="url(#{marker})" />'
    )


def pts(*xy):
    return " ".join(f"{x},{y}" for x, y in xy)


def hpair(xa, xb, y_top_box, y_bot_box, y_fwd, y_reply, label_fwd, label_reply):
    """Connects two adjacent boxes with a forward + reply arrow, both routed
    through the open band below the row so labels never sit on a box."""
    arrow(pts((xa, y_top_box), (xa, y_fwd), (xb, y_fwd), (xb, y_top_box)))
    label((xa + xb) / 2, y_fwd, label_fwd)
    arrow(pts((xb, y_bot_box), (xb, y_reply), (xa, y_reply), (xa, y_bot_box)), color=REPLY, marker="mReply")
    label((xa + xb) / 2, y_reply, label_reply, color=REPLY)


parts.append(f'<rect x="0" y="0" width="{WIDTH}" height="{HEIGHT}" fill="{BG}" />')
parts.append(f'<text x="30" y="34" font-size="19" font-weight="600" fill="{TEXT_PRIMARY}">Arquitetura de Alto Nível do RAG Agentic — TuringSimulator</text>')
parts.append(
    f'<text x="30" y="56" font-size="12.5" fill="{TEXT_SECONDARY}">'
    f'Visão conceitual: busca semântica, embeddings e uso do Gemini como modelo de linguagem e de embeddings.</text>'
)

parts.append(
    '<defs>'
    f'<marker id="mFwd" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{FWD}" /></marker>'
    f'<marker id="mReply" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{REPLY}" /></marker>'
    f'<marker id="mFallback" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
    f'<path d="M0,0 L10,5 L0,10 Z" fill="{FALLBACK}" /></marker>'
    '</defs>'
)

# ---------------------------------------------------------------- Row 1: client
ROW1_Y = 110
ROW1_H = 84
TOP_BOX = ROW1_Y - ROW1_H / 2   # 68
BOT_BOX = ROW1_Y + ROW1_H / 2   # 152

P1 = (150, ROW1_Y, 220, ROW1_H)
P2 = (520, ROW1_Y, 300, ROW1_H)
P3 = (890, ROW1_Y, 300, ROW1_H)
box(*P1, ["Aluno"], sub=["pergunta por voz ou gesto,", "no ambiente imersivo"])
box(*P2, ["Cliente Unity / VR"], sub=["voz, gestos, avatar,", "fala sintetizada e legenda"])
box(*P3, ["Servidor Tutor (API)"], sub=["recebe a pergunta e devolve", "a resposta ao cliente"])

hpair(260, 370, TOP_BOX, BOT_BOX, 178, 205, "pergunta (voz / gesto)", "fala sintetizada + legenda")
hpair(670, 740, TOP_BOX, BOT_BOX, 178, 205, "pergunta transcrita + nível", "resposta (texto)")

# ---------------------------------------------------------------- Row 2: agent
P4 = (890, 320, 480, 104)
box(*P4, ["Agente Conversacional"], sub=["persona + contexto do nível ·", "decide se precisa consultar a base de conhecimento"], fill=FILL_AGENT, stroke=STROKE_AGENT)

arrow(pts((820, 152), (820, 268)))
label(755, 210, "pergunta + nível")
arrow(pts((960, 268), (960, 152)), color=REPLY, marker="mReply")
label(1035, 210, "resposta final gerada", color=REPLY)

# ---------------------------------------------------------------- Row 3: RAG trigger
P5 = (1460, 500, 380, 104)
box(*P5, ["Busca Semântica (RAG)"], sub=["ferramenta acionada pelo agente quando", "a pergunta exige contexto do jogo"], fill=FILL_AGENT, stroke=STROKE_AGENT)

arrow(pts((1070, 350), (1290, 448)))
label(1160, 385, "aciona busca")
arrow(pts((1270, 470), (1030, 350)), color=REPLY, marker="mReply")
label(1200, 420, "trechos relevantes / contexto", color=REPLY)

# ---------------------------------------------------------------- Row 4: embeddings + knowledge base
P10 = (950, 690, 320, 96)
P6 = (1460, 690, 320, 96)
box(*P10, ["Base de Conhecimento"], sub=["documentos sobre persona, jogo,", "objetos, níveis e conceitos"], fill=FILL_KB, stroke=STROKE_KB)
box(*P6, ["Gemini API"], sub=["modelo de embeddings —", "transforma texto em vetores"], fill=FILL_EXTERNAL, stroke=STROKE_EXTERNAL)

arrow(pts((1460, 552), (1460, 642)))
label(1390, 595, "pergunta do aluno")
arrow(pts((1110, 690), (1300, 690)), color=FALLBACK, dashed=True, marker="mFallback")
label(1205, 674, "documentos (offline)", color=FALLBACK)

# ---------------------------------------------------------------- Row 5: index + lexical fallback
P7 = (1460, 880, 380, 108)
P8 = (1830, 880, 260, 96)
box(*P7, ["Índice Vetorial +", "Similaridade Semântica"], sub=["compara o vetor da pergunta com vetores", "pré-computados dos documentos (cosseno)"])
box(*P8, ["Busca por", "Palavras-chave"], sub=["fallback quando não há", "embeddings disponíveis"], dashed=True)

arrow(pts((1460, 738), (1460, 826)))
label(1390, 780, "vetores")

# fallback path leaves from P5's right edge (not through its interior)
arrow(
    pts((1460 + 190, 480), (1830, 480), (1830, 832)),
    color=FALLBACK, dashed=True, marker="mFallback",
)
label(1830, 452, "fallback: sem conectividade", color=FALLBACK)
label(1830, 470, "ou erro do provedor", color=FALLBACK)

# ---------------------------------------------------------------- Row 6: retrieved chunks
P9 = (1460, 1060, 400, 88)
box(*P9, ["Trechos Relevantes Recuperados"], sub=["conteúdo priorizado por similaridade", "semântica e pelo nível atual do aluno"])

arrow(pts((1460, 934), (1460, 1016)))
label(1390, 975, "top-k por similaridade")
arrow(pts((1830, 928), (1830, 1050), (1660, 1050)), color=FALLBACK, dashed=True, marker="mFallback")
label(1770, 990, "resultado do fallback", color=FALLBACK)

# context returned to the RAG orchestrator, routed via the clear right margin
# (kept clear of P7/P8 so it never crosses a box)
MARGIN_X = 2010
arrow(
    pts((1660, 1075), (MARGIN_X, 1075), (MARGIN_X, 500), (1650, 500)),
    color=REPLY, marker="mReply",
)
label(MARGIN_X, 780, "contexto", color=REPLY)
label(MARGIN_X, 800, "recuperado", color=REPLY)

# ---------------------------------------------------------------- legend
ly = HEIGHT - 55
arrow(pts((30, ly), (68, ly)), marker="mFwd")
label(150, ly, "fluxo de requisição/consulta", size=11.5)
arrow(pts((330, ly), (368, ly)), color=REPLY, marker="mReply")
label(440, ly, "fluxo de resposta/retorno", size=11.5)
arrow(pts((640, ly), (678, ly)), color=FALLBACK, dashed=True, marker="mFallback")
label(790, ly, "caminho condicional / offline", size=11.5)

parts.append(
    f'<text x="30" y="{HEIGHT-22}" font-size="10.5" fill="{TEXT_TERTIARY}">'
    f'Visão conceitual de alto nível — não corresponde 1:1 a arquivos ou funções do código; ver docs/diagrams/its-rag-agentic-flowchart.png para o fluxo técnico detalhado.</text>'
)

svg = (
    '<?xml version="1.0" encoding="UTF-8"?>\n'
    f'<svg width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}" '
    f'xmlns="http://www.w3.org/2000/svg" font-family="Helvetica, Arial, sans-serif">\n'
    + "\n".join(parts) + "\n</svg>\n"
)

with open("its-rag-highlevel-architecture.svg", "w") as f:
    f.write(svg)

print("wrote its-rag-highlevel-architecture.svg", WIDTH, HEIGHT)
