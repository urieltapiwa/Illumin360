"""Illumin360 — v1.0 template → v2.0 populated document tool (Word-safe).

Conservative approach: modifies text *inside* existing w:t elements rather than
removing and recreating w:r elements. This preserves all template formatting
(font, size, colour, indentation) AND keeps the XML structure identical to what
Word expects — avoiding Word's strict validation rejecting the file.
"""
from __future__ import annotations
import copy
from pathlib import Path
from typing import Optional

import docx
from docx import Document
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

AUTHOR_NAME = "Uriel Tapiwa Munjanga"
AUTHOR_TITLE = "Software Engineer & Architect"
V2_DATE_LONG = "14 May 2026"


# ─── element helpers ──────────────────────────────────────────────────────

def text_of(el) -> str:
    return "".join(t.text or "" for t in el.iter(qn("w:t")))

def cell_text(cell) -> str:
    return "".join(t.text or "" for t in cell.iter(qn("w:t")))

def is_guidance_table(tbl) -> bool:
    rows = list(tbl.iter(qn("w:tr")))
    if not rows: return False
    t = "".join(x.text or "" for x in rows[0].iter(qn("w:t")))
    return "📋" in t or "Guidance:" in t

def is_placeholder_table(tbl) -> bool:
    rows = list(tbl.iter(qn("w:tr")))
    if not rows: return False
    hdrs = ["".join(x.text or "" for x in tc.iter(qn("w:t"))) for tc in rows[0].iter(qn("w:tc"))]
    return len(hdrs) >= 4 and "Item" in (hdrs[0] or "") and "Detail" in (hdrs[1] or "")

def is_warning_table(tbl) -> bool:
    rows = list(tbl.iter(qn("w:tr")))
    if not rows: return False
    t = "".join(x.text or "" for x in rows[0].iter(qn("w:t")))
    return "TEMPLATE DOCUMENT" in t

def is_metadata_table(tbl) -> bool:
    rows = list(tbl.iter(qn("w:tr")))
    if len(rows) < 6: return False
    labels = []
    for r in rows[:6]:
        cells = list(r.iter(qn("w:tc")))
        if cells:
            labels.append("".join(x.text or "" for x in cells[0].iter(qn("w:t"))))
    return "Project" in labels and ("Organisation" in labels or "Author" in labels)

def is_version_history_table(tbl) -> bool:
    rows = list(tbl.iter(qn("w:tr")))
    if not rows: return False
    hdrs = ["".join(x.text or "" for x in tc.iter(qn("w:t"))) for tc in rows[0].iter(qn("w:tc"))]
    return len(hdrs) >= 4 and "Version" in (hdrs[0] or "") and "Date" in (hdrs[1] or "") and "Change" in (hdrs[3] or "")

def get_heading_text(p):
    pPr = p.find(qn("w:pPr"))
    if pPr is None: return None
    ps = pPr.find(qn("w:pStyle"))
    if ps is None: return None
    style = ps.get(qn("w:val"), "")
    if style.startswith("Heading"):
        return (style, text_of(p).strip())
    return None


# ─── CONSERVATIVE cell text setter ────────────────────────────────────────

def set_cell_text(cell, text: str):
    """Update cell text content WITHOUT removing or recreating runs.
    
    Find the first <w:t> in the cell; set its text to the new value.
    Then clear text in any additional <w:t> elements in the cell.
    This preserves the cell's formatting completely.
    """
    t_elements = list(cell.iter(qn("w:t")))
    if not t_elements:
        # Cell has no text element — find first paragraph and add a run with text
        ps = list(cell.iter(qn("w:p")))
        if not ps:
            return
        p = ps[0]
        # Look for any existing run in this paragraph
        rs = list(p.iter(qn("w:r")))
        if rs:
            # Add a w:t inside the first run
            r = rs[0]
            new_t = OxmlElement("w:t")
            new_t.set(qn("xml:space"), "preserve")
            new_t.text = text
            r.append(new_t)
        else:
            # No runs — create one
            r = OxmlElement("w:r")
            new_t = OxmlElement("w:t")
            new_t.set(qn("xml:space"), "preserve")
            new_t.text = text
            r.append(new_t)
            p.append(r)
        return
    
    # Set first w:t to the new text, clear the rest
    t_elements[0].text = text
    t_elements[0].set(qn("xml:space"), "preserve")
    for t_el in t_elements[1:]:
        t_el.text = ""

def clone_row(template_row):
    return copy.deepcopy(template_row)

def replace_placeholder_table(tbl, content_rows):
    """Replace body rows of the placeholder table with content rows.
    Preserves header row, uses first body row as a template for new rows."""
    rows = list(tbl.iter(qn("w:tr")))
    header = rows[0]
    body_rows = rows[1:]
    template = body_rows[0] if body_rows else header
    for r in body_rows:
        r.getparent().remove(r)
    for row_data in content_rows:
        new_row = clone_row(template)
        cells = list(new_row.iter(qn("w:tc")))
        for i, cell in enumerate(cells[:4]):
            val = row_data[i] if i < len(row_data) else ""
            set_cell_text(cell, val)
        tbl.append(new_row)

def replace_guidance_with_narrative(tbl, narrative: str):
    rows = list(tbl.iter(qn("w:tr")))
    if not rows: return
    cells = list(rows[0].iter(qn("w:tc")))
    if not cells: return
    set_cell_text(cells[0], narrative)

def remove_element(el):
    el.getparent().remove(el)

def append_version_history_row(tbl, version, date_str, author, changes):
    rows = list(tbl.iter(qn("w:tr")))
    template = rows[-1]
    new_row = clone_row(template)
    cells = list(new_row.iter(qn("w:tc")))
    set_cell_text(cells[0], version)
    set_cell_text(cells[1], date_str)
    set_cell_text(cells[2], author)
    set_cell_text(cells[3], changes)
    tbl.append(new_row)

def update_metadata_status(tbl, new_status: str):
    for row in tbl.iter(qn("w:tr")):
        cells = list(row.iter(qn("w:tc")))
        for i, c in enumerate(cells[:-1]):
            if cell_text(c).strip() == "Status":
                if i + 1 < len(cells):
                    set_cell_text(cells[i + 1], new_status)


# ─── section iteration ────────────────────────────────────────────────────

def find_sections(doc):
    body = doc.element.body
    children = list(body.iterchildren())
    i = 0
    while i < len(children):
        el = children[i]
        tag = el.tag.split("}")[1] if "}" in el.tag else el.tag
        if tag == "p":
            head = get_heading_text(el)
            if head and head[0] in ("Heading1", "Heading2"):
                heading_text = head[1]
                guidance = None
                placeholder = None
                j = i + 1
                while j < len(children):
                    cel = children[j]
                    ctag = cel.tag.split("}")[1] if "}" in cel.tag else cel.tag
                    if ctag == "p":
                        nh = get_heading_text(cel)
                        if nh and nh[0] in ("Heading1", "Heading2"):
                            break
                    elif ctag == "tbl":
                        if guidance is None and is_guidance_table(cel):
                            guidance = cel
                        elif is_placeholder_table(cel):
                            placeholder = cel
                            break
                    j += 1
                yield heading_text, guidance, placeholder
        i += 1


# ─── main populate ────────────────────────────────────────────────────────

def populate(template_path: Path, content: dict, output_path: Path):
    doc = docx.Document(str(template_path))
    body = doc.element.body
    
    # 1. Remove TEMPLATE warning tables
    to_remove = [tbl for tbl in body.iter(qn("w:tbl")) if is_warning_table(tbl)]
    for tbl in to_remove:
        remove_element(tbl)
    
    # 2. Update metadata Status field (all metadata tables)
    for tbl in body.iter(qn("w:tbl")):
        if is_metadata_table(tbl):
            update_metadata_status(tbl, "Draft for review — v2.0")
    
    # 3. Append v2.0 row to Version History
    for tbl in body.iter(qn("w:tbl")):
        if is_version_history_table(tbl):
            append_version_history_row(
                tbl, "2.0", V2_DATE_LONG, AUTHOR_NAME,
                content.get("v2_change_description", "Populated against v3.7 corrected spec.")
            )
            break
    
    # 4. Populate sections
    sections_content = content.get("sections", {})
    populated_count = 0
    missing = []
    for heading_text, guidance_tbl, placeholder_tbl in find_sections(doc):
        body_text = sections_content.get(heading_text)
        if not body_text:
            stripped = heading_text.split(".", 1)[-1].strip() if "." in heading_text else heading_text
            body_text = sections_content.get(stripped)
        if not body_text:
            missing.append(heading_text)
            continue
        if guidance_tbl is not None and "narrative" in body_text:
            replace_guidance_with_narrative(guidance_tbl, body_text["narrative"])
        if placeholder_tbl is not None:
            rows = body_text.get("rows", [])
            if rows:
                replace_placeholder_table(placeholder_tbl, rows)
            else:
                replace_placeholder_table(placeholder_tbl, [])
        populated_count += 1
    
    doc.save(str(output_path))
    return {"populated": populated_count, "missing": missing}
