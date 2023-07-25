using Ammunition.Components;
using Ammunition.Defs;
using Ammunition.Logic;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
using Ammunition.Models;

namespace Ammunition.Gizmos
{
	[StaticConstructorOnStartup]
	public sealed class GizmoAmmunition : Gizmo
	{
		private readonly KitComponent _kitComp;
		private Map CurrentMap => _kitComp?.parent.MapHeld;
		private const float Margin = 2f;
		private const float UtilityWidth = 34f;
		private const float ControlsWidth = 150;
		private static bool _draggingBar;

		private static readonly List<float> _ammoBandPercentages = new List<float>()
		{
			0.0f,
			0.1f,
			0.2f,
			0.3f,
			0.4f,
			0.5f,
			0.6f,
			0.7f,
			0.8f,
			0.9f,
			1.0f
		};

		private static readonly Texture2D _targetAmmoBarTex =
			SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.8f, 0.8f));

		private static readonly Texture2D _ammoBarTex =
			SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

		private static readonly Texture2D _ammoBarTexHighlightTex =
			SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

		private static readonly Texture2D _emptyBarTex =
			SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

		public override float GetWidth(float maxWidth)
		{
			return (GetColumnWidth() * (int)Math.Ceiling((double)(_kitComp.Bags.Count)));
		}

		private static float GetColumnWidth()
		{
			return (float)(!Settings.Settings.UseCompactGizmo ? 225f : 150f);
		}

		public GizmoAmmunition(KitComponent comp)
		{
			_kitComp = comp;
			this.Order = -50f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			try
			{
				if (_kitComp != null && AmmoLogic.HaveAmmo && _kitComp.Bags.Any())
				{
					var borderRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
					Material material = (this.disabled || parms.lowLight) ? TexUI.GrayscaleGUI : null;
					//GenUI.DrawTextureWithMaterial(borderRect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex,	material);
					Widgets.DrawMenuSection(borderRect);

					for (int i = 0; i < _kitComp.Bags.Count; i++)
					{
						AmmoSlot slot = _kitComp.Bags[i];
						if (slot.ChosenAmmo == null)
						{
							slot.ChosenAmmo = AmmoLogic.AvailableAmmo.First();
						}

						int columns = (int)Math.Ceiling((double)(i + 1));
						Rect innerRect = borderRect.ContractedBy(Margin);

						int x = (int)(GetColumnWidth() * columns);
						innerRect.width = GetColumnWidth() - (Margin * 2);
						innerRect.x = borderRect.x + (x - GetColumnWidth());
						if (i < 2)
						{
							innerRect.x += Margin;
						}


						innerRect.SplitVerticallyWithMargin(out Rect recImage, out Rect recAmmoGizmo, out _,
							Margin, null, ControlsWidth);

						if (!Settings.Settings.UseCompactGizmo)
						{
							GenUI.DrawTextureWithMaterial(recImage, parms.shrunk ? Command.BGTexShrunk : Command.BGTex,
								material);
							Widgets.DrawTextureFitted(recImage, slot.ChosenAmmo.uiIcon, 0.95f);
						}

						recAmmoGizmo.SplitVerticallyWithMargin(out Rect recLeft, out Rect recRight, out _,
							Margin, null, UtilityWidth);

						recLeft.SplitHorizontallyWithMargin(out Rect recLeftTop, out Rect recLeftBot, out _,
							Margin, recAmmoGizmo.height / 2);

						Widgets.DrawBoxSolid(recLeftTop, Widgets.WindowBGFillColor);
						Widgets.DrawBoxSolid(recLeftBot, Widgets.WindowBGFillColor);
						recRight.SplitHorizontallyWithMargin(out Rect recRightTop, out Rect recRightBot, out _,
							Margin, recAmmoGizmo.height / 2);

						Widgets.DrawBoxSolid(recRightTop, Widgets.WindowBGFillColor);
						Widgets.DrawBoxSolid(recRightBot, Widgets.WindowBGFillColor);

						if (Mouse.IsOver(recLeftTop))
						{
							Widgets.DrawHighlight(recLeftTop);
						}

						float fillPercent = slot.Count / Mathf.Max(1f, slot.MaxCount);
						if (fillPercent > 1f)
						{
							fillPercent = 1f;
						}

						float max = (float)slot.MaxCount / slot.Capacity;
						Widgets.DraggableBar(recLeftBot,
							_ammoBarTex,
							_ammoBarTexHighlightTex,
							_emptyBarTex,
							_targetAmmoBarTex,
							ref _draggingBar,
							fillPercent,
							ref max,
							_ammoBandPercentages, 16);

						if (Math.Abs(slot.MaxCount - max) > 0.01f)
						{
							slot.MaxCount = (int)(max * slot.Capacity);
						}
						
						Widgets.Dropdown(recLeftTop, slot,
							(ammoSlot) => slot.ChosenAmmo, GenerateAmmoMenu,
							slot.ChosenAmmo?.LabelCap, BaseContent.ClearTex);

						Text.Anchor = TextAnchor.MiddleCenter;
						Widgets.Label(recLeftTop, slot.ChosenAmmo?.label);
						Widgets.Label(recLeftBot, slot.Count + " / " + slot.MaxCount);
						if (Widgets.ButtonImage(recRightTop, Settings.Settings.DropIcon) && slot.Count > 0)
						{
							Thing ammo = ThingMaker.MakeThing(slot.ChosenAmmo);
							ammo.stackCount = slot.Count;
							GenPlace.TryPlaceThing(ammo, _kitComp.parent.PositionHeld, Find.CurrentMap,
								ThingPlaceMode.Near);

							slot.Count = 0;
							slot.MaxCount = 0;
						}

						TooltipHandler.TipRegion(recRightTop, Language.Translate.DropAll);
						Texture2D image = slot.Use ? Settings.Settings.TrueIcon : Settings.Settings.FalseIcon;
						if (Widgets.ButtonImage(recRightBot, image))
						{
							slot.Use = !slot.Use;
						}

						TooltipHandler.TipRegion(recRightBot, Language.Translate.CanUse);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("LTS_Ammo framework GizmoOnGUI: " + ex.Message);
			}

			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}

		private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateAmmoMenu(AmmoSlot ammoSlot)
		{
			using (List<ThingDef>.Enumerator enumerator = AmmoLogic.AvailableAmmo.ToList().GetEnumerator())
			{
				bool yielded = false;
				while (enumerator.MoveNext())
				{
					ThingDef ammoDef = enumerator.Current;

					if (ammoDef == null)
					{
						continue;
					}

					if (ammoDef == ammoSlot.ChosenAmmo)
					{
						continue;
					}

					if (_kitComp == null || _kitComp.parent.MapHeld.listerThings.ThingsOfDef(ammoDef).NullOrEmpty())
					{
						continue;
					}


					yielded = true;
					yield return new Widgets.DropdownMenuElement<ThingDef>
					{
						option = new FloatMenuOption(ammoDef.label, delegate
						{
							AmmoLogic.EmptyBagAt(ammoSlot, _kitComp.parent.PositionHeld);
							ammoSlot.MaxCount = ammoSlot.Capacity;
							ammoSlot.ChosenAmmo = ammoDef;
						}, ammoDef.uiIcon, ammoDef.uiIconColor),
						payload = ammoDef,
					};
				}

				if (!yielded)
				{
					yield return new Widgets.DropdownMenuElement<ThingDef>
					{
						option = new FloatMenuOption(Language.Translate.NoAmmo, null)
					};
				}
			}
		}


	}
}