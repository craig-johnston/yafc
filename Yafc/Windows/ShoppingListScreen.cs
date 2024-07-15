﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Yafc.Blueprints;
using Yafc.Model;
using Yafc.UI;

namespace Yafc {
    public class ShoppingListScreen : PseudoScreen {
        private readonly VirtualScrollList<(FactorioObject, float)> list;
        private float shoppingCost, totalBuildings, totalModules;
        private bool decomposed = false;

        private ShoppingListScreen() => list = new VirtualScrollList<(FactorioObject, float)>(30f, new Vector2(float.PositiveInfinity, 2), ElementDrawer, MainScreen.Instance.InputSystem);

        private void ElementDrawer(ImGui gui, (FactorioObject obj, float count) element, int index) {
            using (gui.EnterRow()) {
                gui.BuildFactorioObjectIcon(element.obj, MilestoneDisplay.Contained);
                gui.RemainingRow().BuildText(DataUtils.FormatAmount(element.count, UnitOfMeasure.None, "x") + ": " + element.obj.locName);
            }
            _ = gui.BuildFactorioObjectButton(gui.lastRect, element.obj);
        }

        public static void Show(Dictionary<FactorioObject, int> counts) {
            float cost = 0f, buildings = 0f, modules = 0f;
            var data = counts.Select(x => (x.Key, Value: (float)x.Value)).OrderByDescending(x => x.Value).ToArray();
            foreach ((FactorioObject obj, float count) in data) {
                if (obj is Entity) {
                    buildings += count;
                }
                else if (obj is Module module) {
                    modules += count;
                }

                cost += obj.Cost() * count;
            }

            ShoppingListScreen screen = new() {
                shoppingCost = cost,
                totalBuildings = buildings,
                totalModules = modules,
                list = { data = data }
            };
            _ = MainScreen.Instance.ShowPseudoScreen(screen);
        }

        public override void Build(ImGui gui) {
            BuildHeader(gui, "Shopping list");
            gui.BuildText(
                "Total cost of all objects: " + DataUtils.FormatAmount(shoppingCost, UnitOfMeasure.None, "¥") + ", buildings: " +
                DataUtils.FormatAmount(totalBuildings, UnitOfMeasure.None) + ", modules: " + DataUtils.FormatAmount(totalModules, UnitOfMeasure.None), align: RectAlignment.Middle);
            gui.AllocateSpacing(1f);
            list.Build(gui);
            using (gui.EnterRow(allocator: RectAllocator.RightRow)) {
                if (gui.BuildButton("Done")) {
                    Close();
                }

                if (gui.BuildButton("Decompose", active: !decomposed)) {
                    Decompose();
                }

                if (gui.BuildButton("Export to blueprint", SchemeColor.Grey)) {
                    gui.ShowDropDown(ExportBlueprintDropdown);
                }
            }
        }

        private List<(T, int)> ExportGoods<T>() where T : Goods {
            List<(T, int)> items = [];
            foreach (var (element, amount) in list.data) {
                int rounded = MathUtils.Round(amount);
                if (rounded == 0) {
                    continue;
                }

                if (element is T g) {
                    items.Add((g, rounded));
                }
                else if (element is Entity e && e.itemsToPlace.Length > 0) {
                    items.Add(((T)(object)e.itemsToPlace[0], rounded));
                }
            }

            return items;
        }

        private void ExportBlueprintDropdown(ImGui gui) {
            gui.BuildText("Blueprint string will be copied to clipboard", wrap: true);
            if (Database.objectsByTypeName.TryGetValue("Entity.constant-combinator", out var combinator) && gui.BuildFactorioObjectButtonWithText(combinator) == Click.Left && gui.CloseDropdown()) {
                _ = BlueprintUtilities.ExportConstantCombinators("Shopping list", ExportGoods<Goods>());
            }

            foreach (var container in Database.allContainers) {
                if (container.logisticMode == "requester" && gui.BuildFactorioObjectButtonWithText(container) == Click.Left && gui.CloseDropdown()) {
                    _ = BlueprintUtilities.ExportRequesterChests("Shopping list", ExportGoods<Item>(), container);
                }
            }
        }

        private Recipe? FindSingleProduction(Recipe[] production) {
            Recipe? current = null;
            foreach (var recipe in production) {
                if (recipe.IsAccessible()) {
                    if (current != null) {
                        return null;
                    }

                    current = recipe;
                }
            }

            return current;
        }

        private void Decompose() {
            decomposed = true;
            Queue<FactorioObject> decompositionQueue = new Queue<FactorioObject>();
            Dictionary<FactorioObject, float> decomposeResult = [];

            void AddDecomposition(FactorioObject obj, float amount) {
                if (!decomposeResult.TryGetValue(obj, out float prev)) {
                    decompositionQueue.Enqueue(obj);
                }

                decomposeResult[obj] = prev + amount;
            }

            foreach (var (item, count) in list.data) {
                AddDecomposition(item, count);
            }

            int steps = 0;
            while (decompositionQueue.Count > 0) {
                var elem = decompositionQueue.Dequeue();
                float amount = decomposeResult[elem];
                if (elem is Entity e && e.itemsToPlace.Length == 1) {
                    AddDecomposition(e.itemsToPlace[0], amount);
                }
                else if (elem is Recipe rec) {
                    if (rec.HasIngredientVariants()) {
                        continue;
                    }

                    foreach (var ingredient in rec.ingredients) {
                        AddDecomposition(ingredient.goods, ingredient.amount * amount);
                    }
                }
                else if (elem is Goods g && (g.usages.Length <= 5 || (g is Item item && (item.factorioType != "item" || item.placeResult != null))) && (rec = FindSingleProduction(g.production)!) != null) {
                    AddDecomposition(g.production[0], amount / rec.GetProduction(g));
                }
                else {
                    continue;
                }

                _ = decomposeResult.Remove(elem);
                if (steps++ > 1000) {
                    break;
                }
            }

            list.data = decomposeResult.Select(x => (x.Key, x.Value)).OrderByDescending(x => x.Value).ToArray();
        }
    }
}
