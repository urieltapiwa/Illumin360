"""Run v2.0 populator across all registered templates."""
from pathlib import Path
import populator
from content_registry import REGISTRY as CORE
from content_registry_batch1 import BATCH1
from content_registry_batch1_fixed import FIXES
from content_registry_batch2 import BATCH2
from content_registry_batch3 import BATCH3
from content_registry_batch3_fixed import FIXES3

ROOT = Path("/sessions/funny-awesome-clarke/mnt/Illumin360")
OUT_ROOT = ROOT / "_v2.0_refresh" / "_populated_v2_0"
OUT_ROOT.mkdir(parents=True, exist_ok=True)

ALL = {**CORE, **BATCH1, **FIXES, **BATCH2, **BATCH3, **FIXES3}

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
ok = 0; err = 0; tot_populated = 0; tot_missing = 0
for rel, summary in results:
    name = rel.split("/")[-1].replace("_v1_0.docx", "")
    if "error" in summary:
        err += 1
        print(f"{name[:69]:<70} ERROR  {summary['error']}")
    else:
        ok += 1
        missing = len(summary.get("missing", []))
        tot_populated += summary['populated']
        tot_missing += missing
        print(f"{name[:69]:<70} {summary['populated']:<11} {missing}")
print(sep)
print(f"OK: {ok}  ERR: {err}  TOTAL: {len(results)}  | sections populated: {tot_populated}  | missing keys: {tot_missing}")
