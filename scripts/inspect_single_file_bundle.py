#!/usr/bin/env python3
"""Inspect the .NET single-file bundle manifest in an apphost executable."""
from __future__ import annotations

import argparse
import csv
import html
import json
import struct
from collections import defaultdict
from pathlib import Path

SIGNATURE = bytes.fromhex(
    "8b1202b96a612038727b930214d7a03213f5b9e6efa e3318ee3b2dce24b36aae".replace(" ", "")
)
FILE_TYPES = {
    0: "Unknown",
    1: "Assembly",
    2: "NativeBinary",
    3: "DepsJson",
    4: "RuntimeConfigJson",
    5: "Symbols",
}
CATEGORY_COLORS = {
    ".NET runtime": "#64748b",
    "WPF": "#2563eb",
    "Windows Forms": "#7c3aed",
    "Native runtime": "#0f766e",
    "Satellite resources": "#d97706",
    "Workshop/application": "#059669",
    "Configuration/symbols": "#6b7280",
    "Other": "#9ca3af",
}


def read_7bit_int(data: bytes, offset: int) -> tuple[int, int]:
    value = 0
    shift = 0
    while True:
        if offset >= len(data) or shift >= 35:
            raise ValueError("Invalid 7-bit encoded string length in bundle manifest")
        byte = data[offset]
        offset += 1
        value |= (byte & 0x7F) << shift
        if byte < 0x80:
            return value, offset
        shift += 7


def read_string(data: bytes, offset: int) -> tuple[str, int]:
    length, offset = read_7bit_int(data, offset)
    end = offset + length
    if end > len(data):
        raise ValueError("String extends beyond bundle file")
    return data[offset:end].decode("utf-8"), end


def classify(path: str, file_type: int) -> tuple[str, str]:
    normalized = path.replace("\\", "/")
    lower = normalized.lower()
    if lower.endswith(".resources.dll") and "/" in normalized:
        return "Satellite resources", normalized.split("/", 1)[0]
    if file_type == 2:
        return "Native runtime", ""
    if file_type in (3, 4, 5):
        return "Configuration/symbols", ""
    name = normalized.rsplit("/", 1)[-1]
    if name.startswith("TheSingularityWorkshop.") or name.startswith("StoryBuilder.") or name.startswith("FSM_") or name == "AnyApp.dll":
        return "Workshop/application", ""
    if name.startswith("System.Windows.Forms"):
        return "Windows Forms", ""
    if name.startswith(("Presentation", "WindowsBase", "System.Xaml", "ReachFramework", "PenImc")) or name.startswith("wpfgfx"):
        return "WPF", ""
    if name.endswith(".dll"):
        return ".NET runtime", ""
    return "Other", ""


def parse_bundle(path: Path) -> tuple[dict, list[dict]]:
    data = path.read_bytes()
    signature_at = data.find(SIGNATURE)
    if signature_at < 8:
        raise ValueError(f"Bundle signature not found in {path}")
    header_offset = struct.unpack_from("<q", data, signature_at - 8)[0]
    if not 0 < header_offset < len(data):
        raise ValueError(f"Invalid bundle header offset: {header_offset}")

    offset = header_offset
    major, minor, count = struct.unpack_from("<IIi", data, offset)
    offset += 12
    if major not in (1, 2, 3, 4, 5, 6):
        raise ValueError(f"Unsupported bundle major version: {major}")
    bundle_id, offset = read_string(data, offset)

    deps_location = runtime_config_location = None
    flags = 0
    if major >= 2:
        deps_location = struct.unpack_from("<qq", data, offset)
        offset += 16
        runtime_config_location = struct.unpack_from("<qq", data, offset)
        offset += 16
        flags = struct.unpack_from("<Q", data, offset)[0]
        offset += 8

    entries = []
    for _ in range(count):
        file_offset, size = struct.unpack_from("<qq", data, offset)
        offset += 16
        compressed_size = 0
        if major >= 6:
            compressed_size = struct.unpack_from("<q", data, offset)[0]
            offset += 8
        file_type = data[offset]
        offset += 1
        relative_path, offset = read_string(data, offset)
        category, culture = classify(relative_path, file_type)
        stored_size = compressed_size if compressed_size > 0 else size
        entries.append({
            "Path": relative_path,
            "Type": FILE_TYPES.get(file_type, f"Unknown({file_type})"),
            "TypeCode": file_type,
            "Category": category,
            "Culture": culture,
            "Offset": file_offset,
            "UncompressedBytes": size,
            "StoredBytes": stored_size,
            "CompressedBytes": compressed_size,
            "SavedBytes": size - stored_size,
        })

    if offset != len(data):
        raise ValueError(f"Manifest ended at {offset}, executable ends at {len(data)}")
    if any(e["Offset"] < 0 or e["UncompressedBytes"] < 0 or e["StoredBytes"] < 0 for e in entries):
        raise ValueError("Negative bundle entry metadata encountered")

    summary = {
        "Executable": path.name,
        "ExecutableBytes": len(data),
        "BundleHeaderOffset": header_offset,
        "BundleMetadataBytes": len(data) - header_offset,
        "BundleMajorVersion": major,
        "BundleMinorVersion": minor,
        "BundleId": bundle_id,
        "EntryCount": count,
        "CompressedEntryCount": sum(e["CompressedBytes"] > 0 for e in entries),
        "UncompressedPayloadBytes": sum(e["UncompressedBytes"] for e in entries),
        "StoredPayloadBytes": sum(e["StoredBytes"] for e in entries),
        "ExecutableOverheadBytes": len(data) - sum(e["StoredBytes"] for e in entries),
        "DepsJsonLocation": {"Offset": deps_location[0], "Bytes": deps_location[1]} if deps_location else None,
        "RuntimeConfigLocation": {"Offset": runtime_config_location[0], "Bytes": runtime_config_location[1]} if runtime_config_location else None,
        "Flags": flags,
    }
    return summary, entries


def aggregate(entries: list[dict]) -> list[dict]:
    groups = defaultdict(lambda: {"Category": "", "Count": 0, "UncompressedBytes": 0, "StoredBytes": 0, "SavedBytes": 0})
    for entry in entries:
        group = groups[entry["Category"]]
        group["Category"] = entry["Category"]
        group["Count"] += 1
        for key in ("UncompressedBytes", "StoredBytes", "SavedBytes"):
            group[key] += entry[key]
    return sorted(groups.values(), key=lambda item: item["StoredBytes"], reverse=True)


def slice_layout(items: list[dict], x: float, y: float, width: float, height: float) -> list[tuple[dict, float, float, float, float]]:
    """Binary slice-and-dice treemap layout, weighted by stored bytes."""
    items = [item for item in items if item["StoredBytes"] > 0]
    if not items or width <= 0 or height <= 0:
        return []
    if len(items) == 1:
        return [(items[0], x, y, width, height)]
    total = sum(item["StoredBytes"] for item in items)
    running = 0
    split = 1
    best = float("inf")
    for index in range(1, len(items)):
        running += items[index - 1]["StoredBytes"]
        difference = abs(total / 2 - running)
        if difference < best:
            best, split = difference, index
    left, right = items[:split], items[split:]
    left_weight = sum(item["StoredBytes"] for item in left)
    if width >= height:
        left_width = width * left_weight / total
        return slice_layout(left, x, y, left_width, height) + slice_layout(right, x + left_width, y, width - left_width, height)
    top_height = height * left_weight / total
    return slice_layout(left, x, y, width, top_height) + slice_layout(right, x, y + top_height, width, height - top_height)


def write_treemap(path: Path, profile: str, summary: dict, entries: list[dict]) -> None:
    width, height, margin, header = 1400, 900, 8, 34
    groups = aggregate(entries)
    rectangles = []
    group_rectangles = slice_layout(groups, margin, margin, width - 2 * margin, height - 2 * margin)
    for group, gx, gy, gw, gh in group_rectangles:
        category = group["Category"]
        members = sorted((entry for entry in entries if entry["Category"] == category), key=lambda entry: entry["StoredBytes"], reverse=True)
        content_y = gy + min(header, gh * 0.18)
        content_height = max(0, gh - (content_y - gy))
        color = CATEGORY_COLORS.get(category, "#9ca3af")
        rectangles.append(f'<rect x="{gx:.2f}" y="{gy:.2f}" width="{gw:.2f}" height="{gh:.2f}" fill="{color}" opacity="0.16" stroke="#ffffff" stroke-width="2"><title>{html.escape(category)} — {group["Count"]} files — {group["StoredBytes"]:,} stored bytes</title></rect>')
        rectangles.append(f'<text x="{gx + 7:.2f}" y="{gy + 22:.2f}" font-size="15" font-weight="700" fill="#111827">{html.escape(category)} · {group["StoredBytes"] / 1048576:.2f} MiB</text>')
        for entry, x, y, w, h in slice_layout(members, gx + 2, content_y, max(0, gw - 4), max(0, content_height - 2)):
            title = f'{entry["Path"]} | {entry["Type"]} | uncompressed {entry["UncompressedBytes"]:,} B | stored {entry["StoredBytes"]:,} B'
            rectangles.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{max(0, w):.2f}" height="{max(0, h):.2f}" fill="{color}" stroke="#ffffff" stroke-width="1"><title>{html.escape(title)}</title></rect>')
            if w > 95 and h > 25:
                rectangles.append(f'<text x="{x + 5:.2f}" y="{y + 17:.2f}" font-size="11" fill="#ffffff">{html.escape(entry["Path"][:42])}</text>')
    legend = "".join(
        f'<span class="key"><i class="swatch" style="background:{CATEGORY_COLORS.get(group["Category"], "#9ca3af")}"></i>{html.escape(group["Category"])} ({group["StoredBytes"] / 1048576:.2f} MiB)</span>'
        for group in groups
    )
    document = f"""<!doctype html><html><head><meta charset="utf-8"><title>AnyApp bundle treemap — {html.escape(profile)}</title>
<style>body{{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#111827}}h1{{margin-bottom:4px}}.meta{{color:#4b5563;margin:0 0 16px}}svg{{width:100%;height:auto;border:1px solid #d1d5db;background:#f9fafb}}.legend{{display:flex;flex-wrap:wrap;gap:14px;margin:14px 0}}.key{{display:flex;align-items:center;gap:6px;font-size:13px}}.swatch{{width:12px;height:12px;display:inline-block}}small{{color:#6b7280}}</style></head><body>
<h1>AnyApp embedded bundle treemap — {html.escape(profile)}</h1>
<p class="meta">{summary["EntryCount"]} entries · {summary["ExecutableBytes"]:,} executable bytes · {summary["StoredPayloadBytes"]:,} stored payload bytes · bundle {summary["BundleMajorVersion"]}.{summary["BundleMinorVersion"]}</p>
<div class="legend">{legend}</div>
<svg viewBox="0 0 {width} {height}" role="img" aria-label="Treemap of embedded .NET bundle entries">{''.join(rectangles)}</svg>
<p><small>Rectangle area is proportional to stored bytes. Hover a rectangle for path, type, uncompressed size and stored size. This visualizes the embedded manifest, not loose sidecar files in the publish directory. Assembly-name categories are heuristic groupings.</small></p>
</body></html>"""
    path.write_text(document, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("--output-directory", type=Path, required=True)
    parser.add_argument("--profile", default="unspecified")
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=True)
    summary, entries = parse_bundle(args.executable)
    summary["Profile"] = args.profile
    summary["Categories"] = aggregate(entries)
    with (args.output_directory / "bundle-inventory.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(entries[0].keys()))
        writer.writeheader()
        writer.writerows(sorted(entries, key=lambda entry: entry["StoredBytes"], reverse=True))
    (args.output_directory / "bundle-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    write_treemap(args.output_directory / "bundle-treemap.html", args.profile, summary, entries)
    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
