﻿using System;
using System.Collections.Generic;
using Yafc.UI;

#nullable disable warnings // Disabling nullable for legacy code.

namespace Yafc {
    public class WizardPanel : PseudoScreen {
        private readonly List<PageBuilder> pages = [];
        private string? header;
        private Action? finish;
        private int page;

        public delegate void PageBuilder(ImGui gui, ref bool valid);
        public delegate Action WizardBuilder(List<PageBuilder> pages);

        public static void Show(string header, WizardBuilder builder) {
            WizardPanel panel = new() { header = header };
            panel.finish = builder(panel.pages);
            _ = MainScreen.Instance.ShowPseudoScreen(panel);
        }

        public override void Open() {
            page = 0;
            base.Open();
        }
        public override void Build(ImGui gui) {
            BuildHeader(gui, header);
            bool valid = true;
            pages[page](gui, ref valid);
            using (gui.EnterRow(allocator: RectAllocator.RightRow)) {
                if (gui.BuildButton(page >= pages.Count - 1 ? "Finish" : "Next", active: valid)) {
                    if (page < pages.Count - 1) {
                        page++;
                    }
                    else {
                        Close();
                        finish();
                    }
                }
                if (page > 0 && gui.BuildButton("Previous")) {
                    page--;
                }

                gui.BuildText("Step " + (page + 1) + " of " + pages.Count);
            }
        }
    }
}
