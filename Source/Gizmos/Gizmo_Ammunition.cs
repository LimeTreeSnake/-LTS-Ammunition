using Ammunition.Components;
using Ammunition.Defs;
using Ammunition.Logic;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
using RimWorld;
using System.Data.Common;
using Ammunition.Models;
using static HarmonyLib.Code;

namespace Ammunition.Gizmos {
    [StaticConstructorOnStartup]
    public class Gizmo_Ammunition : Gizmo {

        private static readonly Texture2D DropIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss", true);
        public KitComponent kitComp;
        readonly float margin = 2f;

        private static readonly Texture2D FullAmmoBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.5f, 0.6f));
        private static readonly Texture2D BackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.75f));
        public override float GetWidth(float maxWidth) {
            return (float)(GetColumnWidth() * (IsSingle() ? (int)Math.Ceiling((double)(kitComp.Bags.Count)) : (int)Math.Ceiling((double)(kitComp.Bags.Count) / 2)));
        }
        public float GetColumnWidth() {
            return (float)(Settings.Settings.UseWiderGizmo ? 100f : 75f);
        }
        public float GetPocketWidth() {
            return (float)(Settings.Settings.UseWiderGizmo ? 77f : 52f);
        }
        public float GetUnloadWidth() {
            return (float)(Settings.Settings.UseWiderGizmo ? 22f : 22f);
        }
        public bool IsSingle() {
            return Settings.Settings.UseSingleLineAmmo || kitComp.Bags.Count == 1;
        }
        public Gizmo_Ammunition(KitComponent comp) {
            kitComp = comp;
            this.Order = -50f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) {
            try {
                if (kitComp != null && AmmoLogic.HaveAmmo) {
                    Rect borderRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
                    Material material = (this.disabled || parms.lowLight) ? TexUI.GrayscaleGUI : null;
                    GenUI.DrawTextureWithMaterial(borderRect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex, material, default);
                    if (kitComp.Bags.Any()) {
                        for (int i = 0; i < kitComp.Bags.Count; i++) {
                            if (kitComp.Bags[i].ChosenAmmo == null)
                                kitComp.Bags[i].ChosenAmmo = Settings.Settings.InitialAmmoType != null ? Settings.Settings.InitialAmmoType : AmmoLogic.AvailableAmmo.First();
                            Rect innerRect;
                            int heightAdjust = 0;
                            int columns = 0;
                            if (IsSingle()) {
                                columns = (int)Math.Ceiling((double)(i + 1));
                                innerRect = borderRect.ContractedBy(margin);
                                heightAdjust = 16;
                            }
                            else {
                                columns = (int)Math.Ceiling((double)(i + 1) / 2);
                                innerRect = i % 2 == 0 ? borderRect.ContractedBy(margin).TopHalf() : borderRect.ContractedBy(margin).BottomHalf();
                            }
                            int x = (int)(GetColumnWidth() * columns);
                            innerRect.width = GetColumnWidth() - (margin * 2);
                            innerRect.x = borderRect.x + (x - GetColumnWidth());
                            if (i < 2)
                                innerRect.x += margin;
                            Widgets.DrawWindowBackground(innerRect);
                            Rect recLeft = innerRect.LeftPartPixels(GetPocketWidth()).ContractedBy(margin);
                            Rect recLeftTop = recLeft.TopHalf();
                            Rect recLeftBot = recLeft.BottomHalf();
                            Rect recRight = innerRect.RightPartPixels(GetUnloadWidth()).ContractedBy(margin);
                            Rect recRightTop = recRight.TopHalf();
                            recRightTop.height -= heightAdjust;
                            Rect recRightBot = recRight.BottomHalf();
                            recRightBot.height -= heightAdjust;
                            if (Mouse.IsOver(recLeftTop)) {
                                Widgets.DrawHighlight(recLeftTop);
                            }
                            float fillPercent = kitComp.Bags[i].Count / Mathf.Max(1f, kitComp.Bags[i].MaxCount);
                            if (fillPercent > 1f)
                                fillPercent = 1f;
                            Widgets.FillableBar(recLeft, fillPercent, FullAmmoBarTex, TexUI.HighlightTex, false);
                            Widgets.DrawTextureFitted(recLeft, Widgets.GetIconFor(kitComp.Bags[i].ChosenAmmo), 0.95f);
                            GUI.DrawTexture(recLeftTop, BackgroundTex);
                            Widgets.Dropdown(recLeftTop, kitComp.Bags[i], (Models.Bag bag) => kitComp.Bags[i].ChosenAmmo, GenerateAmmoMenu, kitComp.Bags[i].ChosenAmmo?.LabelCap, BaseContent.ClearTex);
                            Text.Font = GameFont.Small;
                            Text.Anchor = TextAnchor.UpperLeft;
                            Widgets.Label(recLeft, (i + 1).ToString());
                            Text.Font = GameFont.Tiny;
                            Text.Anchor = TextAnchor.LowerCenter;
                            kitComp.Bags[i].MaxCount = (int)Widgets.HorizontalSlider_NewTemp(recLeftBot, kitComp.Bags[i].MaxCount, 0f, kitComp.Bags[i].Capacity, true, kitComp.Bags[i].Count + " / " + kitComp.Bags[i].MaxCount);
                            Text.Font = GameFont.Tiny;
                            Text.Anchor = TextAnchor.MiddleCenter;
                            if (Widgets.ButtonImage(recRightTop, DropIcon) && kitComp.Bags[i].Count > 0) {
	                            Thing ammo = ThingMaker.MakeThing(kitComp.Bags[i].ChosenAmmo);
                                ammo.stackCount = kitComp.Bags[i].Count;
                                GenPlace.TryPlaceThing(ammo, kitComp.parent.PositionHeld, Find.CurrentMap,
	                                ThingPlaceMode.Near);

                                kitComp.Bags[i].Count = 0;
                                kitComp.Bags[i].MaxCount = 0;
                            }
                            TooltipHandler.TipRegion(recRightTop, Language.Translate.DropAll);
                            Texture2D image = kitComp.Bags[i].Use ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                            if (Widgets.ButtonImage(recRightBot, image)) {
                                kitComp.Bags[i].Use = !kitComp.Bags[i].Use;
                            }
                            TooltipHandler.TipRegion(recRightBot, Language.Translate.CanUse);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Error("LTS_Ammo framework GizmoOnGUI: " + ex.Message);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);

        }
        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateAmmoMenu(Bag bag) {
            List<ThingDef>.Enumerator enumerator = AmmoLogic.AvailableAmmo.ToList().GetEnumerator();
            while (enumerator.MoveNext()) {
                ThingDef ammoDef = enumerator.Current;
                if (ammoDef != bag.ChosenAmmo)
                    yield return new Widgets.DropdownMenuElement<ThingDef>
                    {
                        option = new FloatMenuOption(ammoDef.label, delegate
                        {
	                        AmmoLogic.EmptyBagAt(bag, kitComp.parent.PositionHeld);
                            bag.MaxCount = bag.Capacity;
                            bag.ChosenAmmo = ammoDef;
                        }, ammoDef.uiIcon, ammoDef.uiIconColor),
                        payload = ammoDef,
                    };
            }
        }



    }
}
