using System.Collections.Generic;
using System.Linq;
using YAFC.Model;
using YAFC.UI;

namespace YAFC
{
    public class ModuleCustomisation : PseudoScreen
    {
        private static readonly ModuleCustomisation Instance = new ModuleCustomisation();

        private RecipeRow recipe;

        public static void Show(RecipeRow recipe)
        {
            Instance.recipe = recipe;
            MainScreen.Instance.ShowPseudoScreen(Instance);
        }
        
        public override void Build(ImGui gui)
        {
            BuildHeader(gui, "Module customisation");
            if (recipe.modules == null)
            {
                if (gui.BuildButton("Enable custom modules"))
                    recipe.RecordUndo().modules = new CustomModules(recipe);
            }
            else
            {
                gui.BuildText("Internal modules:", Font.subheader);
                gui.BuildText("Leave zero amount to fill the remainings slots");
                DrawRecipeModules(gui, null);
                gui.BuildText("Beacon modules:", Font.subheader);
                if (recipe.modules.beacon == null)
                {
                    gui.BuildText("Use default parameters");
                    if (gui.BuildButton("Override beacons as well"))
                        SelectBeacon(gui);
                }
                else
                {
                    if (gui.BuildFactorioObjectButtonWithText(recipe.modules.beacon))
                        SelectBeacon(gui);
                    gui.BuildText("Input the amount of modules, not the amount of beacons. Single beacon can hold "+recipe.modules.beacon.moduleSlots+" modules.", wrap:true);
                    DrawRecipeModules(gui, recipe.modules.beacon);
                }
            }
            
            gui.AllocateSpacing(3f);
            using (gui.EnterRow(allocator:RectAllocator.RightRow))
            {
                if (gui.BuildButton("Done"))
                    Close();
                gui.allocator = RectAllocator.LeftRow;
                if (recipe.modules != null && gui.BuildRedButton("Remove module customisation") == ImGuiUtils.Event.Click)
                {
                    recipe.RecordUndo().modules = null;
                    Close();
                }
            }
        }

        private void SelectBeacon(ImGui gui)
        {
            gui.BuildObjectSelectDropDown<Entity>(Database.allBeacons, DataUtils.DefaultOrdering, sel =>
            {
                if (recipe.modules != null)
                    recipe.modules.RecordUndo().beacon = sel;
                contents.Rebuild();
            }, "Select beacon", allowNone:recipe.modules.beacon != null);
        }

        private IReadOnlyList<Item> GetModules(Entity beacon)
        {
            IEnumerable<Item> modules = beacon == null ? recipe.recipe.modules : Database.allModules;
            var filter = beacon ?? recipe.entity;
            return modules.Where(x => filter.CanAcceptModule(x.module)).ToArray();
        }

        private void DrawRecipeModules(ImGui gui, Entity beacon)
        {
            using (var grid = gui.EnterInlineGrid(3f, 1f))
            {
                var list = beacon != null ? recipe.modules.beaconList : recipe.modules.list;
                foreach (var module in list)
                {
                    grid.Next();
                    var evt = gui.BuildFactorioGoodsWithEditableAmount(module.module, module.fixedCount, UnitOfMeasure.None, out var newAmount);
                    if (evt == GoodsWithAmountEvent.ButtonClick)
                    {
                        SelectObjectPanel.Select(GetModules(beacon), "Select module", sel =>
                        {
                            if (sel == null)
                                recipe.modules.RecordUndo().list.Remove(module);
                            else module.module = sel;
                            gui.Rebuild();
                        }, DataUtils.FavouriteModule, true);
                    } 
                    else if (evt == GoodsWithAmountEvent.TextEditing)
                    {
                        var amountInt = MathUtils.Floor(newAmount);
                        if (amountInt < 0)
                            amountInt = 0;
                        module.RecordUndo().fixedCount = amountInt;
                    }
                }
                
                grid.Next();
                if (gui.BuildButton(Icon.Plus, SchemeColor.Primary, SchemeColor.PrimalyAlt, size:2.5f))
                {
                    gui.BuildObjectSelectDropDown(GetModules(beacon), DataUtils.FavouriteModule, sel =>
                    {
                        recipe.modules.RecordUndo();
                        list.Add(new RecipeRowCustomModule(recipe.modules, sel));
                        gui.Rebuild();
                    }, "Select module");
                }
            }
        }
    }
}