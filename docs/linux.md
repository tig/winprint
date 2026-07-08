# Linux & WSL printing

On Linux (including WSL), **winprint** provides the `wp` terminal UI and headless `wp print` — not the graphical app. Printing uses the system’s **CUPS** setup: `--printer` is a CUPS **queue name**. There is no Windows-style printer picker in the headless path.

Install `wp` first (see [Install](install.md#linux)). This page is about getting printers (or a PDF file) working so `wp print` has somewhere useful to send output.

## Prefer `--pdf` when you only need a file

```bash
wp print mermaid.md --pdf mermaid.pdf --sheet "Proportional 1-Up"
```

No printer queue, no driver, no dialog. Works the same on bare-metal Linux and WSL. Use this when you want a PDF on disk rather than paper (or a virtual “print to PDF” queue).

`--pdf` and `--printer` cannot be combined.

## List printers

```bash
lpstat -p          # printers and state
lpstat -a          # queues accepting jobs (name is the first word)
lpstat -d          # system default destination
lpstat -t          # full status dump
lpstat -o          # outstanding jobs
```

The name for `wp print … --printer NAME` is that first word, e.g. `PDF` or `Brother-HL-L3230CDW`.

Set a default (optional):

```bash
lpoptions -d Brother-HL-L3230CDW   # per-user
# sudo lpadmin -d Brother-HL-L3230CDW   # system-wide
```

With no `--pdf` and no `--printer`, `wp print` uses the **system default** destination. If nothing is configured, it errors and tells you to add a queue, pass `--printer`, or use `--pdf`.

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

## Print to PDF via a virtual queue (cups-pdf)

If you want a CUPS “print to PDF” queue (instead of `--pdf`):

```bash
# Debian / Ubuntu
sudo apt install -y printer-driver-cups-pdf
# Fedora
# sudo dnf install -y cups-pdf

lpstat -p                 # usually "PDF" (Debian/Ubuntu) or "Cups-PDF" (Fedora)
```

```bash
wp print mermaid.md --printer PDF --sheet "Proportional 1-Up"
# Output directory is ~/PDF/ by default (see Out in /etc/cups/cups-pdf.conf)
ls ~/PDF/
```

For a simple file on disk with no queue, prefer `wp print … --pdf out.pdf`.

## Install a network printer (IPP)

Most modern LAN printers (Brother, HP, Epson, …) speak **IPP**. CUPS can use the generic *everywhere* driver — you usually do not need a Windows or vendor driver inside Linux.

Example: **Brother HL-L3230CDW** at `192.168.1.104`:

```bash
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

Firmware varies. Remove and retry:

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

Install vendor Linux packages only if the generic driver misprints (margins, duplex, color). Prefer IPP first.

## WSL notes

WSL2 is a **separate Linux** environment. Windows and WSL do **not** share printer lists.

| Expectation | Reality |
| --- | --- |
| `lpstat` shows “Microsoft Print to PDF” | No — that is Windows-only |
| Printers added in Windows Settings appear in WSL | No — add a **CUPS** queue in Linux |
| `wp print file` with no queues configured | Errors — set up CUPS or use `--pdf` |

**Recommended on WSL**

1. `wp print … --pdf out.pdf`, then open on Windows (`explorer.exe out.pdf`) if you only need a file.
2. Or add a network IPP queue (above) so `--printer` reaches the device on your LAN.

**Windows-shared printers from WSL** (optional): share the printer on Windows, then add an SMB queue in CUPS (`smb://<windows-host>/ShareName`). The Windows host from WSL2 is often the `nameserver` in `/etc/resolv.conf`. Prefer talking to the printer’s own LAN IP when it has one.

**WSL networking:** the distro must reach `192.168.x.x` printers. If `ping` fails, fix Windows/WSL networking (firewall, mirrored mode, VPN) before debugging CUPS.

## Quick recipes

```bash
# File only — no printer required
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
| WSL can’t ping the printer | Windows/WSL network path |

More detail: `wp --debug`. Logs on Linux live next to the `wp` executable (see [Support](support.md)).

## See also

* [Install](install.md#linux) — Homebrew `wp` formula
* [User’s Guide](users-guide.md) — CLI options (`--printer`, `--pdf`, sheets)
* [Overview](index.md#how-to-turn-markdown-into-a-pdf) — Markdown → PDF one-liners per platform
