# HayDay-Project reference integration

The original repository is a Unity 3D early-alpha project. Its scripts are kept
here as reference only and are intentionally outside `Assets`, so Unity will
not compile them or change the current scene.

Useful mechanics identified for the existing 2D project:

- animal purchase, feeding, health/happiness and production;
- market buy/sell flow;
- farm/land expansion;
- menu and save/load flow.

The selected 2D textures under `Assets/Art/Imported/HayDayReference` are not
assigned to any scene or prefab. They can be evaluated later as optional UI or
background art without replacing the current 2D art direction.

Before shipping any imported art or code, verify the permission supplied by the
asset author and any separate third-party package terms. The public repository
contains third-party folders (including NGUI and asset packs), so permission
for the repository author may not cover every file.
