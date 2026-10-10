Filter symbols for the Weapons panel
====================================

Put one PNG per symbol in this folder, named exactly like this:

  OneHand.png     1-Hand
  TwoHand.png     2-Hand
  Knives.png      Knives
  Swords.png      Swords
  Clubs.png       Clubs / hammers / maces
  Axes.png        Axes
  Spears.png      Spears
  Polearms.png    Polearms (atgeir)
  Bows.png        Bows
  Crossbows.png   Crossbows
  Fists.png       Fists / claws
  Bombs.png       Bombs
  Staff.png       Magic staff (not used by a filter yet)

Each PNG is a finished BOX: wooden frame + symbol, exactly as drawn (parts of the symbol may reach
over the frame, that is kept). 128 x 128 px, transparent outside the frame.
Selected = full brightness with a gold edge, not selected = slightly darker.

The boxes are cut from Assets/Icons/source/sheet1.jpg (4 x 3 tiles):
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\IconCut\IconCut.ps1
Swords.png is built from the 1-Hand tile and its mirror image (crossed swords).
Missing: a key without a PNG uses our drawn symbol on the wood button. You can replace only some.

Two ways to use them
  1. Build:  files in Assets/Icons are compiled into the DLL (dotnet build -c Release).
  2. Test without building: copy them to
       BepInEx/plugins/WorkbenchesPlus/icons/<Key>.png
     (a file there wins over the compiled one; restart the game to reload).
