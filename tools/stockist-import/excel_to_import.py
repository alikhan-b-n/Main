#!/usr/bin/env python3
"""
Turns Nadine's "Noahs Adventures - customer list.xlsx" into the JSON that
POST /api/stockists/import accepts. Standard library only, so it runs anywhere:

    python3 -I excel_to_import.py "customer list.xlsx" stockists.json

The sheet is hand-kept, so anything that is not a clean number or date (e.g.
"called June 30th - still 5 in stc", "11 - invoiced") is kept as a note on the
store instead of being guessed at. Review the notes after the import.
"""
import datetime
import json
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

NS = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'

# (book code, date column, quantity column) as laid out in the sheet
TITLES = [
    ('cork', 'C', 'D'),
    ('galway', 'E', 'F'),
    ('kerry', 'G', 'H'),
    ('berlin', 'I', 'J'),
    ('waterford', 'K', 'L'),
]
NAME, REGION, CONTACT, INVOICE, NOTES = 'A', 'B', 'M', 'N', 'O'
FIRST_ROW = 3


def read_rows(path):
    book = zipfile.ZipFile(path)
    shared = [
        ''.join(t.text or '' for t in si.iter(NS + 't'))
        for si in ET.fromstring(book.read('xl/sharedStrings.xml')).findall(NS + 'si')
    ]
    rows = {}
    for row in ET.fromstring(book.read('xl/worksheets/sheet1.xml')).iter(NS + 'row'):
        cells = {}
        for cell in row.findall(NS + 'c'):
            value = cell.find(NS + 'v')
            if value is None:
                continue
            column = re.match(r'[A-Z]+', cell.get('r')).group()
            cells[column] = shared[int(value.text)] if cell.get('t') == 's' else value.text
        rows[int(row.get('r'))] = cells
    return rows


def clean(text):
    return ' '.join((text or '').replace('​', '').split())


def excel_date(value):
    """Excel serial day (46272) or a written date (14/06/2025, 2025-06-06); the last one wins."""
    if value is None:
        return None
    value = value.strip()
    if re.fullmatch(r'\d{5}(\.0+)?', value):
        return datetime.date(1899, 12, 30) + datetime.timedelta(days=int(float(value)))
    found = None
    for d, m, y in re.findall(r'\b(\d{1,2})/(\d{1,2})/(\d{4})\b', value):
        try:
            found = datetime.date(int(y), int(m), int(d))
        except ValueError:
            pass
    for y, m, d in re.findall(r'\b(\d{4})-(\d{2})-(\d{2})\b', value):
        found = datetime.date(int(y), int(m), int(d))
    return found


def is_plain_number(value):
    return value is not None and re.fullmatch(r'\s*\d+\s*', value) is not None


def first_number(value):
    match = re.match(r'\s*(\d+)', value or '')
    return int(match.group(1)) if match else None


def convert(rows):
    stores = []
    for r in sorted(rows):
        cells = rows[r]
        if r < FIRST_ROW or not clean(cells.get(NAME)) or clean(cells.get(NAME)).lower() == 'total':
            continue

        store = {
            'name': clean(cells[NAME]),
            'region': clean(cells.get(REGION)) or None,
            'stage': 'Active',
            'stock': [],
            'notes': [],
            'invoices': [],
        }

        for code, date_col, qty_col in TITLES:
            raw_date, raw_qty = cells.get(date_col), cells.get(qty_col)
            quantity = first_number(raw_qty)
            on = excel_date(raw_date)
            if quantity and quantity > 9999:
                # A date typed into the quantity column (Excel shows it as 45822)
                on, quantity = on or excel_date(raw_qty), None
            label = code.capitalize()

            if quantity:
                entry = {'title': code, 'quantity': quantity}
                if on:
                    entry['deliveredOn'] = on.isoformat()
                store['stock'].append(entry)

            # Whatever was more than a plain number or date is kept word for word
            odd = [clean(v) for v in (raw_date, raw_qty)
                   if v and not is_plain_number(v) and not re.fullmatch(r'\d{5}(\.0+)?', v.strip())
                   and clean(v) != '-']
            if odd:
                store['notes'].append(f'From Excel, {label}: ' + ' / '.join(odd))
            elif on and not quantity:
                store['notes'].append(f'From Excel, {label}: dated {on.isoformat()} with no quantity')

        contact = [line.strip() for line in (cells.get(CONTACT) or '').split('\n') if line.strip()]
        emails = [line for line in contact if '@' in line]
        others = [line for line in contact if '@' not in line]
        if emails:
            store['email'] = emails[0]
        if others:
            name, _, rest = others[0].partition(' - ')
            store['contactName'] = name.strip()
            extra = ([rest.strip()] if rest.strip() else []) + others[1:]
            if extra:
                store['notes'].append('From Excel, contact: ' + ' / '.join(extra))

        invoice = clean(cells.get(INVOICE))
        if invoice:
            sent = excel_date(cells.get(INVOICE))
            if sent and 'unpaid' in invoice.lower():
                store['invoices'].append({'sentOn': sent.isoformat(), 'reference': 'From Excel'})
            else:
                store['notes'].append(f'From Excel, invoices: {invoice}')

        if clean(cells.get(NOTES)):
            store['notes'].append(f'From Excel: {clean(cells[NOTES])}')

        if not any(s['quantity'] > 0 for s in store['stock']):
            store['stage'] = 'Lead'

        stores.append(store)
    return stores


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    stores = convert(read_rows(sys.argv[1]))
    with open(sys.argv[2], 'w', encoding='utf-8') as out:
        json.dump(stores, out, ensure_ascii=False, indent=2)
    leads = sum(1 for s in stores if s['stage'] == 'Lead')
    print(f'{len(stores)} rows: {len(stores) - leads} stores, {leads} leads -> {sys.argv[2]}')


if __name__ == '__main__':
    main()
