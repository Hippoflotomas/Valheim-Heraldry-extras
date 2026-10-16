# Valheim Heraldry Extras

Companion mod to Valheim Heraldry Mod ([Heraldry-Mod](https://github.com/Hippoflotomas/Heraldry-Mod)): extra models that the main mod can paint with heraldry.

## How it works
This mod adds no painting code. Every paintable surface in its models is tagged with the main mod's convention
(`ValheimHeraldryMod_Face`, `ValheimHeraldryMod_Face_1` … `_4`, `ValheimHeraldryMod_CentrePoint`), and Valheim Heraldry Mod finds and paints it.
Soft dependency: without the main mod the pieces are plain.

## Layout
- `ValheimHeraldryExtras.sln`
- `ValheimHeraldryExtras/` plugin (net48, Jötunn 2.30.2)
- `ValheimHeraldryExtrasUnity/` asset-bundle project (open in Unity 6000.0.75f1)
- `scripts/publish.ps1` post-build deploy/package

## Setup
Copy `Environment.props` from the Heraldry-Mod repo into this folder (it's gitignored). Built from the JotunnModStub template; see the [Jötunn docs](https://valheim-modding.github.io/Jotunn/guides/overview.html).
