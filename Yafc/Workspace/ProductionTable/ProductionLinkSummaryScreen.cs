﻿using System;
using System.Collections.Generic;
using Yafc.Model;
using Yafc.UI;

namespace Yafc {
    public class ProductionLinkSummaryScreen : PseudoScreen, IComparer<(RecipeRow row, float flow)> {
        private readonly ProductionLink link;
        private readonly List<(RecipeRow row, float flow)> input = [];
        private readonly List<(RecipeRow row, float flow)> output = [];
        private float totalInput, totalOutput;
        private readonly ScrollArea scrollArea;

        private ProductionLinkSummaryScreen(ProductionLink link) {
            scrollArea = new ScrollArea(30, BuildScrollArea, MainScreen.Instance.InputSystem);
            this.link = link;
            CalculateFlow(link);
        }

        private void BuildScrollArea(ImGui gui) {
            gui.BuildText("Production: " + DataUtils.FormatAmount(totalInput, link.goods.flowUnitOfMeasure), Font.subheader);
            BuildFlow(gui, input, totalInput);
            gui.spacing = 0.5f;
            gui.BuildText("Consumption: " + DataUtils.FormatAmount(totalOutput, link.goods.flowUnitOfMeasure), Font.subheader);
            BuildFlow(gui, output, totalOutput);
            if (link.flags.HasFlags(ProductionLink.Flags.LinkNotMatched) && totalInput != totalOutput) {
                gui.BuildText((totalInput > totalOutput ? "Overproduction: " : "Overconsumption: ") + DataUtils.FormatAmount(MathF.Abs(totalInput - totalOutput), link.goods.flowUnitOfMeasure), Font.subheader, color: SchemeColor.Error);
            }
        }

        public override void Build(ImGui gui) {
            BuildHeader(gui, "Link summary");
            scrollArea.Build(gui);
            if (gui.BuildButton("Done")) {
                Close();
            }
        }

        protected override void ReturnPressed() => Close();

        private void BuildFlow(ImGui gui, List<(RecipeRow row, float flow)> list, float total) {
            gui.spacing = 0f;
            foreach (var (row, flow) in list) {
                _ = gui.BuildFactorioObjectButtonWithText(row.recipe, DataUtils.FormatAmount(flow, link.goods.flowUnitOfMeasure));
                if (gui.isBuilding) {
                    var lastRect = gui.lastRect;
                    lastRect.Width *= (flow / total);
                    gui.DrawRectangle(lastRect, SchemeColor.Primary);
                }
            }

        }

        private void CalculateFlow(ProductionLink link) {
            totalInput = 0;
            totalOutput = 0;
            foreach (var recipe in link.capturedRecipes) {
                float production = recipe.recipe.GetProduction(link.goods, recipe.parameters.productivity);
                if (recipe.fuel is not null && recipe.fuel.HasSpentFuel(out Item? spent) && spent == link.goods) {
                    production += recipe.parameters.fuelUsagePerSecondPerRecipe;
                }
                float consumption = recipe.recipe.GetConsumption(link.goods);
                float fuelUsage = recipe.fuel == link.goods ? recipe.parameters.fuelUsagePerSecondPerRecipe : 0;
                float localFlow = (float)((production - consumption - fuelUsage) * recipe.recipesPerSecond);
                if (localFlow > 0) {
                    input.Add((recipe, localFlow));
                    totalInput += localFlow;
                }
                else if (localFlow < 0) {
                    output.Add((recipe, -localFlow));
                    totalOutput -= localFlow;
                }
            }
            input.Sort(this);
            output.Sort(this);
            Rebuild();
            scrollArea.RebuildContents();
        }

        public static void Show(ProductionLink link) {
            _ = MainScreen.Instance.ShowPseudoScreen(new ProductionLinkSummaryScreen(link));
        }

        public int Compare((RecipeRow row, float flow) x, (RecipeRow row, float flow) y) => y.flow.CompareTo(x.flow);
    }
}
