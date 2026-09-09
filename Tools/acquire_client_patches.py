"""Download and inspect the official EP1 patch chain without executing patches.

Requires 7-Zip. CHK MD5s check archive consistency, not publisher identity.
The original reference database is never modified by this tool.
"""
import argparse
import hashlib
import json
import re
import subprocess
import urllib.request
import urllib.error
from pathlib import Path

LIST_URL = "http://launcher.anarchy-online.com/exepatches/index_html"


def digest(path, algorithm="sha256"):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, algorithm).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("Research/ClientReference/Patches"))
    parser.add_argument("--seven-zip", default="C:/Program Files/7-Zip/7z.exe")
    parser.add_argument("--report", type=Path, default=Path("Artifacts/client-patch-acquisition.json"))
    args = parser.parse_args()
    args.root.mkdir(parents=True, exist_ok=True)
    page = urllib.request.urlopen(LIST_URL, timeout=30).read()
    (args.root / "official-patch-list.html").write_bytes(page)
    links = set(re.findall(rb'href="(http://update\.anarchy-online\.com/patches/[^"<>]+)"', page))
    chain = [(f"18.8.{n}", f"18.8.{n+1}") for n in range(50, 62)]
    chain.append(("18.8.62", "18.8.62.0"))
    report = {"source": LIST_URL, "listSha256": hashlib.sha256(page).hexdigest(),
              "scope": "Downloaded and extracted only; no patch applied, no target database verified",
              "start": "18.8.50_EP1", "advertisedEnd": "18.8.62.0_EP1", "patches": [], "unavailable": []}
    for source, target in chain:
        name = f"AOPatch_v{source}_EP1-v{target}_EP1.exe"
        listed_url = f"http://update.anarchy-online.com/patches/{name}"
        if listed_url.encode() not in links:
            raise ValueError(f"Missing chain link in official list: {name}")
        url = listed_url.replace("http:", "https:", 1)
        archive = args.root / name
        if not archive.exists():
            partial = archive.with_suffix(".partial")
            try:
                with urllib.request.urlopen(url, timeout=60) as response, partial.open("wb") as output:
                    while data := response.read(1024 * 1024):
                        output.write(data)
            except urllib.error.HTTPError as error:
                report["unavailable"].append({"from": source, "to": target, "url": url,
                                               "httpStatus": error.code})
                args.report.parent.mkdir(parents=True, exist_ok=True)
                args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
                raise SystemExit(f"Acquisition incomplete: {url} returned HTTP {error.code}; report saved")
            partial.replace(archive)
        destination = args.root / f"{source}-{target}"
        subprocess.run([args.seven_zip, "x", str(archive), f"-o{destination}", "-y"],
                       check=True, stdout=subprocess.DEVNULL)
        checks = []
        for checksum_file in destination.rglob("*.CHK"):
            for name, expected in re.findall(r"^([^#\r\n:]+):([a-fA-F0-9]{32})\s*$",
                                            checksum_file.read_text(), re.MULTILINE):
                if Path(name).name != name:
                    raise ValueError(f"Invalid checksum filename: {name}")
                actual = digest(checksum_file.parent / name, "md5")
                if actual.lower() != expected.lower():
                    raise ValueError(f"Archive checksum mismatch: {name}")
                checks.append({"name": name, "md5": actual, "matched": True})
        if not checks:
            raise ValueError(f"No checksums found in {archive}")
        report["patches"].append({"from": source, "to": target, "url": url,
                                  "bytes": archive.stat().st_size,
                                  "sha256": digest(archive), "checks": checks,
                                  "files": [{"path": str(p.relative_to(destination)),
                                             "bytes": p.stat().st_size, "sha256": digest(p)}
                                            for p in sorted(destination.rglob("*")) if p.is_file()]})
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        print(f"Verified archive {source} -> {target}: {len(checks)} checksums", flush=True)
    print(f"Complete acquisition: {len(chain)} archives. Patches have NOT been applied.")


if __name__ == "__main__":
    main()
