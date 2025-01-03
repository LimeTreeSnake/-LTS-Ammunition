﻿using Ammunition.Components;
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
			return GetColumnWidth() * (int)Math.Ceiling((double)(_kitComp?.Bags?.Count ?? 1));
		}

		private static float GetColumnWidth()
		{
			return !Settings.Settings.UseCompactGizmo ? 225f : 150f;
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
					var material = (this.disabled || parms.lowLight) ? TexUI.GrayscaleGUI : null;
					//GenUI.DrawTextureWithMaterial(borderRect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex,	material);
					Widgets.DrawMenuSection(borderRect);

					for (var i = 0; i < _kitComp.Bags.Count; i++)
					{
						var slot = _kitComp.Bags[i];
						if (slot.ChosenAmmo == null)
						{
							slot.ChosenAmmo = Settings.Settings.AvailableAmmo.First();
						}

						var columns = (int)Math.Ceiling((double)(i + 1));
						var innerRect = borderRect.ContractedBy(Margin);

						var x = (int)(GetColumnWidth() * columns);
						innerRect.width = GetColumnWidth() - (Margin * 2);
						innerRect.x = borderRect.x + (x - GetColumnWidth());
						if (i < 2)
						{
							innerRect.x += Margin;
						}


						innerRect.SplitVerticallyWithMargin(out var recImage, out var recAmmoGizmo, out _,
							Margin, null, ControlsWidth);

						if (!Settings.Settings.UseCompactGizmo)
						{
							GenUI.DrawTextureWithMaterial(recImage, parms.shrunk ? Command.BGTexShrunk : Command.BGTex,
								material);
							Widgets.DrawTextureFitted(recImage, slot.ChosenAmmo.uiIcon, 0.95f);
							var infoRect = new Rect(recImage.xMax - 22f, recImage.y + 2f, 20, 20);
							if (Widgets.ButtonImage(infoRect, Settings.Settings.InfoIcon))
							{
								Find.WindowStack.Add(new Dialog_InfoCard(slot.ChosenAmmo));
							}
						}

						recAmmoGizmo.SplitVerticallyWithMargin(out var recLeft, out var recRight, out _,
							Margin, null, UtilityWidth);

						recLeft.SplitHorizontallyWithMargin(out var recLeftTop, out var recLeftBot, out _,
							Margin, recAmmoGizmo.height / 2);

						Widgets.DrawBoxSolid(recLeftTop, Widgets.WindowBGFillColor);
						Widgets.DrawBoxSolid(recLeftBot, Widgets.WindowBGFillColor);
						recRight.SplitHorizontallyWithMargin(out var recRightTop, out var recRightBot, out _,
							Margin, recAmmoGizmo.height / 2);

						Widgets.DrawBoxSolid(recRightTop, Widgets.WindowBGFillColor);
						Widgets.DrawBoxSolid(recRightBot, Widgets.WindowBGFillColor);

						if (Mouse.IsOver(recLeftTop))
						{
							Widgets.DrawHighlight(recLeftTop);
						}

						var fillPercent = slot.Count / Mathf.Max(1f, slot.MaxCount);
						if (fillPercent > 1f)
						{
							fillPercent = 1f;
						}

						var max = (float)slot.MaxCount / slot.Capacity;
						Widgets.DraggableBar(recLeftBot,
							_ammoBarTex,
							_ammoBarTexHighlightTex,
							_emptyBarTex,
							_targetAmmoBarTex,
							ref _draggingBar,
							fillPercent,
							ref max,
							_ammoBandPercentages, 10);

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
							var ammo = ThingMaker.MakeThing(slot.ChosenAmmo);
							ammo.stackCount = slot.Count;
							GenPlace.TryPlaceThing(ammo, _kitComp.parent.PositionHeld, Find.CurrentMap,
								ThingPlaceMode.Near);

							slot.Count = 0;
							slot.MaxCount = 0;
						}

						TooltipHandler.TipRegion(recRightTop, Language.Translate.DropAll);
						var image = slot.Use ? Settings.Settings.TrueIcon : Settings.Settings.FalseIcon;
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
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("LTS_Ammo framework GizmoOnGUI: " + ex.Message);
				}
			}

			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}

		private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateAmmoMenu(AmmoSlot ammoSlot)
		{
			using (var enumerator = Settings.Settings.AvailableAmmo.ToList().GetEnumerator())
			{
				var yielded = false;
				while (enumerator.MoveNext())
				{
					var ammoDef = enumerator.Current;

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
						}, 
							ammoDef.uiIcon, 
							ammoDef.uiIconColor),
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