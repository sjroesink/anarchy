"""Capture explicitly selected public AO Index item tables; never crawl the catalogue.

Keeps raw operators/unsigned values as strings. This is reference evidence, not
an executable item importer: unknown actions must still be decoded separately.
"""
import argparse
from datetime import datetime, timezone
import hashlib
from html.parser import HTMLParser
import json
from pathlib import Path
import re
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1]

class Tables(HTMLParser):
    def __init__(self):
        super().__init__()
        self.tables = []
        self.table = self.row = self.cell = None

    def handle_starttag(self, tag, attrs):
        if tag == 'table':
            if self.table is not None:
                raise ValueError('Nested table requires parser review')
            self.table = []
        elif tag == 'tr' and self.table is not None:
            self.row = []
        elif tag in ('td', 'th') and self.row is not None:
            self.cell = []

    def handle_data(self, data):
        if self.cell is not None:
            self.cell.append(data)

    def handle_endtag(self, tag):
        if tag in ('td', 'th') and self.cell is not None:
            self.row.append(re.sub(r'\s+', ' ', ''.join(self.cell)).strip())
            self.cell = None
        elif tag == 'tr' and self.row is not None:
            self.table.append(self.row)
            self.row = None
        elif tag == 'table' and self.table is not None:
            self.tables.append(self.table)
            self.table = None

def capture(aoid, html_dir=None):
    url = f'https://www.aoindex.com/item/{aoid}'
    if html_dir is not None:
        raw = (html_dir / f'aoindex-{aoid}.html').read_bytes()
    else:
        with urlopen(url, timeout=30) as response:
            raw = response.read()
    text = raw.decode('utf-8')
    parser = Tables()
    parser.feed(text)
    raw_tables = []
    for table in parser.tables:
        if table and table[0] in (
            ['Key', 'Value'], ['Stat ID', 'Value', 'Stat Name'],
            ['Type', 'Sort', 'Stat ID', 'Op ID', 'Value', 'Stat Name', 'Op', 'Label'],
            ['Sort', 'Event ID', 'Fn', 'Target', 'Stat ID', 'Amount', 'Tick Count', 'Tick Delay', 'Parameters', 'Event', 'Effect', 'Target Label', 'Stat Name'],
            ['Sort', 'Key Type', 'Skill ID', 'Amount', 'Skill Name'],
            ['Attribute ID', 'Amount'],
        ):
            raw_tables.append({'columns': table[0], 'rows': table[1:]})
    stats = next((t for t in raw_tables if t['columns'] == ['Stat ID', 'Value', 'Stat Name']), None)
    base = next((t for t in raw_tables if t['columns'] == ['Key', 'Value']), None)
    if len(raw_tables) != 6 or any(len(row) != len(t['columns']) and row != ['No rows.'] for t in raw_tables for row in t['rows']):
        raise ValueError(f'{aoid}: raw table layout changed; no output written')
    if not stats or not base or not any(r == ['lowId', str(aoid)] or r == ['highId', str(aoid)] for r in base['rows']):
        raise ValueError(f'{aoid}: expected raw item tables missing; no output written')
    if '18.08.62' not in text:
        raise ValueError(f'{aoid}: source version changed; review before importing')
    return {'aoid': aoid, 'sourceUrl': url, 'sourceVersion': '18.08.62',
            'capturedUtc': datetime.now(timezone.utc).isoformat(),
            'htmlSha256': hashlib.sha256(raw).hexdigest(),
            'scope': 'Published raw table evidence; not complete executable semantics.', 'tables': raw_tables}

if __name__ == '__main__':
    args = argparse.ArgumentParser(description=__doc__)
    args.add_argument('aoids', type=int, nargs='+')
    args.add_argument('--html-dir', type=Path, help='Parse previously downloaded source pages offline')
    options = args.parse_args()
    selected = options.aoids
    if any(i <= 0 for i in selected):
        args.error('AOIDs must be positive')
    dest = ROOT / 'Unity/Assets/Resources/AO/RawItems'
    dest.mkdir(parents=True, exist_ok=True)
    for aoid in dict.fromkeys(selected):
        evidence = capture(aoid, options.html_dir)
        (dest / f'{aoid}.json').write_text(json.dumps(evidence, indent=2) + '\n', encoding='utf-8')
        print(f'{aoid}: captured {len(evidence["tables"])} raw tables')
