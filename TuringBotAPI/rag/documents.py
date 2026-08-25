"""Load classic RAG markdown files with YAML-like frontmatter."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

CATEGORIES = frozenset({"persona", "gameplay", "objects", "goals", "concepts"})
REQUIRED_FIELDS = ("id", "category", "title", "level_id")


@dataclass(frozen=True)
class KnowledgeDocument:
    id: str
    category: str
    title: str
    level_id: str
    text: str
    path: str


def parse_frontmatter_markdown(raw: str, path: str = "") -> KnowledgeDocument:
    text = raw.lstrip("\ufeff")
    if not text.startswith("---"):
        raise ValueError(f"Missing frontmatter in {path or 'document'}")

    rest = text[3:]
    end = rest.find("\n---")
    if end < 0:
        raise ValueError(f"Unclosed frontmatter in {path or 'document'}")

    fm_block = rest[:end].strip()
    body = rest[end + 4 :].strip()
    if not body:
        raise ValueError(f"Empty body in {path or 'document'}")

    fields: dict[str, str] = {key: "" for key in REQUIRED_FIELDS}
    for line in fm_block.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if ":" not in stripped:
            raise ValueError(f"Invalid frontmatter line in {path}: {stripped}")
        key, value = stripped.split(":", 1)
        fields[key.strip()] = value.strip().strip('"').strip("'")

    missing = [key for key in REQUIRED_FIELDS if key not in fields]
    if missing:
        raise ValueError(f"Missing frontmatter {missing} in {path}")

    category = fields["category"]
    if category not in CATEGORIES:
        raise ValueError(f"Unknown category '{category}' in {path}")

    doc_id = fields["id"].strip()
    if not doc_id:
        raise ValueError(f"Empty id in {path}")

    return KnowledgeDocument(
        id=doc_id,
        category=category,
        title=fields["title"].strip(),
        level_id=fields["level_id"].strip(),
        text=body,
        path=path,
    )


def load_knowledge_dir(root: Path) -> list[KnowledgeDocument]:
    if not root.is_dir():
        raise FileNotFoundError(f"Knowledge directory not found: {root}")

    docs: list[KnowledgeDocument] = []
    seen: set[str] = set()
    for path in sorted(root.rglob("*.md")):
        parsed = parse_frontmatter_markdown(path.read_text(encoding="utf-8"), str(path))
        if parsed.id in seen:
            raise ValueError(f"Duplicate document id '{parsed.id}' ({path})")
        seen.add(parsed.id)
        docs.append(parsed)

    if not docs:
        raise ValueError(f"No markdown documents in {root}")
    return docs
