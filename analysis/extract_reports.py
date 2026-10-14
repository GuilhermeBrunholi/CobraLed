# SPDX-License-Identifier: GPL-3.0-or-later
"""Read-only USBPcap/pcapng extraction; never opens a HID device."""
import json, pathlib, struct, sys
raw = pathlib.Path(sys.argv[1]).read_bytes()
pos = frame = 0
rows = []
while pos < len(raw):
    kind, size = struct.unpack_from('<II', raw, pos)
    if size < 12 or pos + size > len(raw):
        raise ValueError('Invalid pcapng block')
    if kind == 6:
        frame += 1
        interface, hi, lo, captured, original = struct.unpack_from('<IIIII', raw, pos + 8)
        packet = raw[pos + 28:pos + 28 + captured]
        if len(packet) >= 36 and packet[28:30] == bytes.fromhex('2109'):
            value, index, length = struct.unpack_from('<HHH', packet, 30)
            rows.append(dict(frame=frame, seconds=(hi * 2**32 + lo)/1e6,
                device=struct.unpack_from('<H', packet, 19)[0], value=f'{value:04X}',
                index=index, length=length, payload=packet[36:].hex().upper()))
    pos += size
pathlib.Path('analysis/set-reports.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
for row in rows:
    print(row['frame'], f"{row['seconds'] - rows[0]['seconds']:.6f}", row['value'], row['index'], row['length'], row['payload'])
