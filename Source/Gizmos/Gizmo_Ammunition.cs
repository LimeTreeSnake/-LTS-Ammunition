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

        private static readonly Texture2D DropIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss", true);
        public KitComponent kitComp;
        readonly float pocketWidth = 50;
        readonly float columnWidth = 75;
        readonly float unloadWidth = 20f;
        readonly float margin = 2f;

        private static readonly Texture2D FullAmmoBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.5f, 0.6f));
        private static readonly Texture2D BackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.75f));
        public override float GetWidth(float maxWidth) {
            return (float)(columnWidth * Math.Ceiling((double)kitComp?.Props.bags / 2));
        }
        public Gizmo_Ammunition(ref KitComponent comp) {
            kitComp = comp;
            this.Order = -50f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) {
            if (kitComp != null) {
                try {
                    Rect borderRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
                    Material material = (this.disabled || parms.lowLight) ? TexUI.GrayscaleGUI : null;
                    GenUI.DrawTextureWithMaterial(borderRect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex, material, default);
                    for (int i = 0; i < kitComp.Props.bags; i++) {
                        if (kitComp.Bags[i].ChosenAmmo == null)
                            kitComp.Bags[i].ChosenAmmo = AmmoLogic.AvailableAmmo.First();
                        int column = (int)Math.Ceiling((double)(i + 1) / 2);
                        int x = (int)(columnWidth * column);
                        Rect innerRect;
                        innerRect = i % 2 == 0 ? borderRect.ContractedBy(margin).TopHalf() : borderRect.ContractedBy(margin).BottomHalf();
                        innerRect.width = columnWidth - margin;
                        innerRect.x = borderRect.x + ((x - columnWidth)) + (margin / 2);
                        if (i < 2)
                            innerRect.x += margin / 2;
                        Widgets.DrawWindowBackground(innerRect.LeftPartPixels(columnWidth - (margin*2)));
                        Rect recLeft = innerRect.LeftPartPixels(pocketWidth).ContractedBy(1f);
                        recLeft.x += margin;
                        Rect recRightTop = innerRect.RightPartPixels(unloadWidth + margin / 2).ContractedBy(1f).TopHalf();
                        recRightTop.x -= margin;
                        Rect recRightBot = innerRect.RightPartPixels(unloadWidth + margin / 2).ContractedBy(1f).BottomHalf();
                        recRightTop.x -= margin;
                        if (Mouse.IsOver(recLeft.TopHalf())) {
                            Widgets.DrawHighlight(recLeft.TopHalf());
                        }
                        float fillPercent = kitComp.Bags[i].Count / Mathf.Max(1f, kitComp.Bags[i].MaxCount);
                        if (fillPercent > 1f)
                            fillPercent = 1f;
                        Widgets.FillableBar(recLeft, fillPercent, FullAmmoBarTex, TexUI.HighlightTex, false);
                        Widgets.DrawTextureFitted(recLeft.ExpandedBy(margin / 2), kitComp.Bags[i].ChosenAmmo != null ? Widgets.GetIconFor(kitComp.Bags[i].ChosenAmmo) : Widgets.GetIconFor(kitComp.parent.def), 1);
                        GUI.DrawTexture(recLeft.TopHalf(), BackgroundTex);
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                        Widgets.Label(recLeft, (i + 1).ToString());
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.LowerCenter;
                        kitComp.Bags[i].MaxCount = (int)Widgets.HorizontalSlider(recLeft.BottomHalf(), kitComp.Bags[i].MaxCount, 0f, kitComp.Props.ammoCapacity[i], true, kitComp.Bags[i].Count + " / " + kitComp.Bags[i].MaxCount);
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Dropdown(recLeft.TopHalf(), kitComp.Bags[i], (Models.Bag bag) => kitComp.Bags[i].ChosenAmmo, GenerateAmmoMenu, kitComp.Bags[i].ChosenAmmo?.LabelCap, BaseContent.ClearTex);
                        if (Widgets.ButtonImage(recRightTop.ContractedBy(1), DropIcon) && kitComp.Bags[i].Count > 0) {
                            DebugThingPlaceHelper.DebugSpawn(kitComp.Bags[i].ChosenAmmo, kitComp.parent.PositionHeld, kitComp.Bags[i].Count);
                            kitComp.Bags[i].Count = 0;
                            kitComp.Bags[i].MaxCount = 0;
                        }
                        Texture2D image = kitComp.Bags[i].Use ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                        if (Widgets.ButtonImage(recRightBot.ContractedBy(1), image)) {
                            kitComp.Bags[i].Use = !kitComp.Bags[i].Use;
                        }
                    }
                }
                catch (Exception ex) {
                    Log.Error(ex.Message);
                }
            }
            else {
                Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);

        }
        public IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateAmmoMenu(Models.Bag bag) {
            List<ThingDef>.Enumerator enumerator = AmmoLogic.AvailableAmmo.ToList().GetEnumerator();
            while (enumerator.MoveNext()) {
                ThingDef ammoDef = enumerator.Current;
                if (ammoDef != bag.ChosenAmmo)
                    yield return new Widgets.DropdownMenuElement<ThingDef>() {
                        option = new FloatMenuOption(ammoDef.label, delegate () {
                            if (bag.Count > 0) {
                                {
                                    DebugThingPlaceHelper.DebugSpawn(bag.ChosenAmmo, kitComp.parent.PositionHeld, bag.Count);
                                }
                            }
                            bag.Count = 0;
                            bag.MaxCount = bag.Capacity;
                            bag.ChosenAmmo = ammoDef;
                        }, ammoDef.uiIcon, ammoDef.uiIconColor
                        , MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                        payload = ammoDef,
                    };
            }
            yield break;
        }



    }
}
