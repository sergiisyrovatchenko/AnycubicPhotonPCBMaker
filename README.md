# Anycubic Photon PCB Maker

Transforms PCB Gerber files into exposure masks for Anycubic Photon resin printers: the printer's LCD exposes a
photoresist-coated PCB instead of curing resin.

Windows desktop app (WinForms, .NET 8), based on [photonic-etcher](https://github.com/Andrew-Dickinson/photonic-etcher)
with support for **Photon Mono 4 Ultra** and **Photon Mono 4**.

![Anycubic Photon PCB Maker](https://i.postimg.cc/rz6cgRzF/Untitled.png)

## Features

- **Any Gerber set** - files, a folder or a ZIP; layers recognized automatically for KiCad, Altium, Eagle, OrCAD,
  gEDA, DipTrace and EasyEDA, plus the Gerber X2 `.FileFunction` attribute.
- **Board and layer previews** - realistic top / bottom render of the board and a preview of every layer, showing
  exactly what gets exposed.
- **Positive and negative photoresist** - exposes the background or the tracks, including dry film.
- **Holes as on the board** - all drill files (plated, non plated, vias) are cut out of the copper and solder-mask.
- **Drill guides** - a 0.1 mm copper ring marks holes in bare areas (mounting holes) so they can be drilled.
- **Board outline** - drawn into the copper for cutting the board out.
- **Free placement** - any corner or the center of the screen, with an offset in mm.
- **Flips per board side** - horizontal/vertical, right in the layer list.
- **90° rotation** - when the board only fits the other way round; the app tells you when it does.
- **Several copies** - up to a 3 × 3 matrix of boards on one exposure, with an adjustable gap.

## Latest Version

You can download .zip file with the latest build of the master branch from [Releases](https://github.com/sergiisyrovatchenko/AnycubicPhotonPCBMaker/releases)

## Supported printers

| Printer | File |
|---|---|
| Photon Mono 4 Ultra | `.pm4u` |
| Photon Mono 4 | `.pm4n` |
| Photon Ultra | `.dlp` |
| Photon M3 / M3 Max | `.pm3` / `.pm3m` |
| Photon Mono SQ | `.pmsq` |
| Photon Zero | `.pw0` |
| Photon Mono 4K | `.pwma` |
| Photon Mono X 6K, M3 Plus | `.pwmb` |
| Photon Mono / Mono SE / Mono X | `.pwmo` / `.pmsq` / `.pwmx` |
| Photon, Photon S | `.pws` |
| Photon X | `.pwx` |
