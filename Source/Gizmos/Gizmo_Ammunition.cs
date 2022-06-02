using Ammunition.Components;
using Ammunition.Defs;
using Ammunition.Logic;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
using RimWorld;

namespace Ammunition.Gizmos {
    [StaticConstructorOnStartup]
    public class Gizmo_Ammunition : Gizmo {

        public KitComponent Component;

        private static readonly Texture2D FullAmmoBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.4f, 0.5f));
        private static readonly Texture2D BackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));
        public override float GetWidth(float maxWidth) {
            return 150f;
        }
        public Gizmo_Ammunition(KitComponent comp) {
            Component = comp;
            this.order = -50f;
        }
        public override bool Visible {
            get {
                return (base.Visible && !(Find.Selector.SelectedPawns.Count != 1 && Settings.Settings.HideMultipleGizmos));
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) {
            if (AmmoLogic.AmmoTypesList(Component.Props.kitCategory).Count > 0) {
                Text.Anchor = TextAnchor.MiddleCenter;
                ThingDef current = Component.ChosenAmmo;
                Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
                Rect rect2 = rect.ContractedBy(6f);
                Rect recLeft = rect2.LeftPartPixels(rect2.width * 0.6f);
                Rect recLeftTop = recLeft;
                recLeftTop.height = (recLeftTop.height / 3f) * 2;
                recLeftTop.width = rect2.width * 0.6f;
                Rect recLeftBot = recLeft;
                recLeftBot.y = recLeftTop.y + recLeftTop.height;
                recLeftBot.height = recLeftTop.height / 2;
                Rect recRight = rect2.RightPartPixels(rect2.width * 0.4f).ContractedBy(2, 0f);
                recRight.x += 2f;
                Rect recRightTop = recRight.TopPartPixels(recRight.height * 0.66f);
                Rect recRightBot = recRight.BottomPartPixels(recRight.height * 0.33f);
                Widgets.DrawWindowBackground(rect);
                Widgets.Dropdown(recLeftTop, Component.ChosenAmmo, (ThingDef selectedThing) => Component.ChosenAmmo = selectedThing, GenerateAmmoMenu, Component.ChosenAmmo?.LabelCap, BaseContent.ClearTex);
                if (Component.ChosenAmmo != null) {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    float fillPercent = Component.Count / Mathf.Max(1f, Component.MaxCount);
                    if (fillPercent > 1f)
                        fillPercent = 1f;
                    Widgets.FillableBar(recLeft, fillPercent, FullAmmoBarTex, TexUI.HighlightTex, false);
                    Widgets.DrawTextureFitted(recLeft.ContractedBy(2f), Component.ChosenAmmo != null ? Widgets.GetIconFor(Component.ChosenAmmo) : Widgets.GetIconFor(Component.parent.def), 1);
                    GUI.DrawTexture(recLeftTop.TopHalf(), BackgroundTex);
                    Component.MaxCount = (int)Widgets.HorizontalSlider(recLeftBot.ContractedBy(2), Component.MaxCount, 0f, Component.Props.ammoCapacity, true, Component?.Count + " / " + Component.MaxCount);
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(recLeftTop.TopHalf(), Component.ChosenAmmo.LabelCap);
                    if (Widgets.ButtonText(recRightTop.TopPartPixels(recRightTop.height * 0.9f), "Unload") && Component?.Count > 0) {
                        DebugThingPlaceHelper.DebugSpawn(Component.ChosenAmmo, Component.parent.PositionHeld, Component.Count);
                        Component.Count = 0;
                    }
                    if (Widgets.ButtonText(recRightBot.RightPartPixels(recRightBot.height), "+")) {

                        if (Event.current.shift) {
                            Component.MaxCount += 5;
                        }
                        else if (Event.current.control) {
                            Component.MaxCount += 10;
                        }
                        else if (Event.current.shift && Event.current.control) {
                            Component.MaxCount = Component.MaxCount;
                        }
                        else {
                            Component.MaxCount++;
                        }
                        if (Component.MaxCount > Component.Props.ammoCapacity)
                            Component.MaxCount = Component.Props.ammoCapacity;
                    }
                    if (Widgets.ButtonText(recRightBot.LeftPartPixels(recRightBot.height), "-")) {
                        if (Event.current.shift) {
                            Component.MaxCount -= 5;
                        }
                        else if (Event.current.control) {
                            Component.MaxCount -= 10;
                        }
                        else if (Event.current.shift && Event.current.control) {
                            Component.MaxCount = 0;
                        }
                        else {
                            Component.MaxCount--;
                        }
                        if (Component.MaxCount < 0)
                            Component.MaxCount = 0;
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }
                else {
                    Component.ChosenAmmo = AmmoLogic.AmmoTypesList(Component.Props.kitCategory).FirstOrDefault();
                }
            }
            else {
                Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            }

            return new GizmoResult(GizmoState.Clear);
        }
        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateAmmoMenu(ThingDef ammo) {
            List<ThingDef>.Enumerator enumerator = AmmoLogic.AmmoTypesList(Component.Props.kitCategory).GetEnumerator();
            while (enumerator.MoveNext()) {
                ThingDef ammoDef = enumerator.Current;
                yield return new Widgets.DropdownMenuElement<ThingDef> {
                    option = new FloatMenuOption(ammoDef.label, delegate () {
                        Component.ChosenAmmo = ammoDef;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = ammoDef,
                };
            }
            yield break;
        }
    }
}
