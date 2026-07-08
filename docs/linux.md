# Linux & WSL printing

On Linux (including WSL), **winprint** ships the `wp` terminal UI and headless `wp print` — not the MAUI GUI. Printing goes through **CUPS**: `wp` renders pages to a PDF in-process (Skia), then submits that PDF with `lpr`. There is no Windows-style “pick a printer” dialog in the headless path; `--printer` is the CUPS **queue name**.

Install `wp` first (see [Install](install.md#linux)). This page covers getting **something useful to happen** when you print.

## Prefer `--pdf` when you only need a file

```bash
wp print mermaid.md --pdf mermaid.pdf --sheet "Proportional 1-Up"
```

No CUPS queue, no driver, no dialog. Works the same on bare metal Linux and WSL. Use this instead of “print to PDF” virtual printers when you just want a file on disk.

`--pdf` and `--printer` are mutually exclusive.

## How Linux printing works

| Piece | Role |
| --- | --- |
| `wp print …` | Renders via Skia → PDF, then either writes a file (`--pdf`) or calls `lpr` (`--printer` / system default) |
| **CUPS** | Spooler: queues, filters, backends |
| `lpr` / `lp` | Submit a job to a named queue |
| `lpstat` | List queues, default destination, jobs |

Bare `wp print file.md` with **no** `--pdf` and **no** `--printer` uses the **system default** CUPS destination. If none is configured, `wp` fails with an actionable error (install a queue, pass `--printer`, or use `--pdf`). It will not silently report success while the job sits nowhere useful.

## List printers (CUPS)

```bash
lpstat -p          # printers and state
lpstat -a          # queues accepting jobs (name is the first word)
lpstat -d          # system default destination
lpstat -t          # full status dump
lpstat -o          # outstanding jobs
```

The name you pass to `wp print … --printer NAME` is that first word, e.g. `PDF` or `Brother-HL-L3230CDW`.

Set a default (optional):

```bash
lpoptions -d Brother-HL-L3230CDW   # per-user
# sudo lpadmin -d Brother-HL-L3230CDW   # system-wide
```

## Install CUPS

```bash
# Debian / Ubuntu / WSL
sudo apt update
sudo apt install -y cups cups-client cups-bsd
sudo service cups start   # or: sudo systemctl start cups

lpstat -r                 # expect: scheduler is running
```

```bash
# Fedora / RHEL
sudo dnf install -y cups
sudo systemctl enable --now cups
```

## Print to a PDF file via a virtual queue (cups-pdf)

When you want a CUPS queue (preview of the real `--printer` path) rather than `--pdf`:

```bash
# Debian / Ubuntu
sudo apt install -y printer-driver-cups-pdf
# Fedora
# sudo dnf install -y cups-pdf

lpstat -p                 # queue is usually "PDF" (Debian/Ubuntu) or "Cups-PDF" (Fedora)
```

```bash
wp print mermaid.md --printer PDF --sheet "Proportional 1-Up"
# PDFs land in ~/PDF/ by default (see Out in /etc/cups/cups-pdf.conf)
ls ~/PDF/
```

**Fidelity:** stock cups-pdf with its PPD may run a PDF→PS→PDF filter chain (Ghostscript). Mermaid diagrams are embedded rasters and survive. For **bit-identical** Skia output, use `wp print … --pdf out.pdf` instead.

## Install a network printer (IPP)

Most modern LAN printers (Brother, HP, Epson, …) speak **IPP**. CUPS can drive them with the generic *everywhere* driver — no vendor Windows driver needed inside Linux.

Example: **Brother HL-L3230CDW** at `192.168.1.104`:

```bash
# Reachability from this machine / WSL
ping -c 2 192.168.1.104

sudo lpadmin -p Brother-HL-L3230CDW -E \
  -m everywhere \
  -v ipp://192.168.1.104/ipp/print \
  -D "Brother HL-L3230CDW" \
  -L "LAN"

lpoptions -d Brother-HL-L3230CDW    # optional default
lpstat -p -a -d
```

Test:

```bash
echo "hello from CUPS" | lpr -P Brother-HL-L3230CDW
wp print README.md --printer Brother-HL-L3230CDW --sheet "Proportional 1-Up"
```

### If that URI fails

Brother (and others) vary by firmware. Remove and retry:

```bash
sudo lpadmin -x Brother-HL-L3230CDW

sudo lpadmin -p Brother-HL-L3230CDW -E -m everywhere \
  -v ipp://192.168.1.104/ipp/port1
# or:
#   -v http://192.168.1.104:631/ipp/print
```

Optional discovery:

```bash
sudo apt install -y avahi-utils cups-ipp-utils
ippfind
# printer web UI is often http://192.168.1.104/
```

Vendor Linux `.deb`/`.rpm` drivers are only worth installing if IPP Everywhere misprints (margins, duplex, color). Prefer IPP first.

## WSL notes

WSL2 is a **separate Linux** environment. Windows and WSL do **not** share printer lists.

| Expectation | Reality |
| --- | --- |
| `lpstat` shows “Microsoft Print to PDF” | No — Windows-only virtual printer |
| Printers added in Windows Settings appear in WSL | No — not until you add a **CUPS** queue in Linux |
| Bare `wp print file` with no queues | Error (or a dead spool) — configure CUPS or use `--pdf` |

**Recommended on WSL**

1. `wp print … --pdf out.pdf`, then open on Windows (`explorer.exe out.pdf`) if you only need a file.
2. Or add a real network IPP queue (above) so `lpr` / `--printer` hit the device on your LAN.

**Windows-shared printers from WSL** (optional): share the printer on Windows, then add an SMB queue in CUPS (`smb://<windows-host>/ShareName`). The Windows host IP from WSL2 is often the `nameserver` in `/etc/resolv.conf`. This is more brittle than IPP to the printer’s own address — prefer talking to the printer directly when it has a LAN IP.

**WSL networking:** the distro must reach `192.168.x.x` printers. If `ping` fails, fix Windows/WSL networking (firewall, mirrored mode, VPN) before debugging CUPS.

## Quick recipes

```bash
# File only — no CUPS required
wp print demo.md --pdf ./demo.pdf --sheet "Proportional 1-Up"

# Count sheets without printing
wp print demo.md --what-if --sheet "Proportional 1-Up"

# cups-pdf virtual queue
wp print demo.md --printer PDF

# Physical network printer (queue name from lpstat -a)
wp print demo.md --printer Brother-HL-L3230CDW --sheet "Proportional 1-Up"
```

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| `No print destination is configured` | `lpstat -a` empty → install cups-pdf or an IPP queue, or use `--pdf` |
| `No default printer is set` | Queues exist but none is default → `wp … --printer NAME` or `lpoptions -d NAME` |
| `Unknown printer '…'` | Typo or wrong name → `lpstat -a` |
| `Unable to launch 'lpr'` | Install `cups-client` / `cups-bsd` |
| `lpstat: scheduler is not running` | `sudo service cups start` |
| Job accepted, nothing prints | `lpstat -o`, printer power/network, wrong IPP URI; try `lpr -P NAME /path/to.pdf` alone |
| WSL can’t ping the printer | Windows/WSL network path, not winprint |

Debug logging: run `wp` with `--debug`. On Linux, portable mode keeps logs next to the `wp` executable (see [Support](support.md)).

## See also

* [Install](install.md#linux) — Homebrew `wp` formula
* [User’s Guide](users-guide.md) — CLI options (`--printer`, `--pdf`, sheets)
* [Overview](index.md#how-to-turn-markdown-into-a-pdf) — Markdown → PDF one-liners per platform
