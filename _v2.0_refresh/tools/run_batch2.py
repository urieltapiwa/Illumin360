"""Run v2.0 populator across batch-1 + batch-2 templates."""
from pathlib import Path
import populator
from content_registry import REGISTRY as CORE
from content_registry_batch1 import BATCH1
from content_registry_batch1_fixed import FIXES
from content_registry_batch2 import BATCH2

ROOT = Path("/sessions/funny-awesome-clarke/mnt/Illumin360")
OUT_ROOT = ROOT / "_v2.0_refresh" / "_populated_v2_0"
OUT_ROOT.mkdir(parents=True, exist_ok=True)

ALL = {**CORE, **BATCH1, **FIXES, **BATCH2}

print(f"Populating {len(ALL)} documents...\n")
results = []
for rel_path, content in ALL.items():
    src = ROOT / rel_path
    if not src.exists():
        results.append((rel_path, {"error": "MISSING_TEMPLATE"}))
        continue
    out_rel = rel_path.replace("_v1_0.docx", "_v2_0.docx")
    out_path = OUT_ROOT / out_rel
    out_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        summary = populator.populate(src, content, out_path)
        results.append((rel_path, summary))
    except Exception as e:
        import traceback
        results.append((rel_path, {"error": str(e)[:80], "tb": traceback.format_exc()[:200]}))

sep = "=" * 100
print(sep)
print(f"{'DOCUMENT':<70} POPULATED   MISSING")
print(sep)
for rel, summary in results:
    name = rel.split("/")[-1].replace("_v1_0.docx", "")
    if "error" in summary:
        print(f"{name:<70} ERROR  {summary['error']}")
    else:
        missing = len(summary.get("missing", []))
        print(f"{name:<70} {summary['populated']:<11} {missing}")
print(sep)
print(f"Output: {OUT_ROOT}")
