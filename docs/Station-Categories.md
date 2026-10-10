# Station categories

There is no per-station chip table any more. The chip bar shows the same set on every station
(`CraftCategories.ChipOrder`) and hides chips that have no recipe at the open station.
Which chip a recipe belongs to is decided by `RecipeClassifier`: see [Sorting.md](Sorting.md).
