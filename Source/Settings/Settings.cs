using Ammunition.Defs;
using Ammunition.Language;
using Ammunition.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ammunition.Components;

namespace Ammunition.Settings
{
	public class AmmunitionSettings : Mod
	{

		private readonly Settings _settings;

		public AmmunitionSettings(ModContentPack content) : base(content)
		{
			_settings = this.GetSettings<Settings>();
			Settings.Initialize();
		}

		public override string SettingsCategory()
		{
			return "LTS Ammunition";
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			_settings.DoWindowContents(inRect);
		}
	}


	public class Settings : ModSettings
	{

		private static Texture2D AmmoDisabled => ContentFinder<Texture2D>.Get("Icons/Ammo_disabled");
		private static Texture2D AmmoEnabled => ContentFinder<Texture2D>.Get("Icons/Ammo_enabled");

		#region Fields

		private static bool _hideMultipleGizmos = true;
		private static float _pawnAmmoSpawnRate = 1f;
		private static float _pawnAmmoMinSpawnRate = 0.5f;

		private static bool _useAmmoPerBullet;

		//private static bool useAnimalAmmo = false;
		//private static bool useMechanoidAmmo = false;
		private static bool _useSingleLineAmmo;
		private static bool _useWiderGizmo;
		private static bool _npcUseAmmo = true;
		private static string _initialAmmoString;
		private static AmmoCategoryDef _chosenCategory;
		private static ThingDef _chosenKit;
		private static bool _enable, _disable, _enableMod, _disableMod, _changesMade;

		private static bool
			_neolithic = true,
			_medieval = true,
			_industrial = true,
			_spacer = true,
			_ultra = true,
			_archotech = true;

		private static string _searchFilter = "";
		private Vector2 _scrollWeaponsPos;
		private const float Margin = 4f;
		private static readonly float _lineHeight = Text.LineHeight;
		private static bool _firstPage = true;

		#endregion Fields


		public static bool HideMultipleGizmos => _hideMultipleGizmos;
		public static float PawnAmmoSpawnRate => _pawnAmmoSpawnRate;
		public static float PawnAmmoMinSpawnRate => _pawnAmmoMinSpawnRate;

		public static bool UseAmmoPerBullet => _useAmmoPerBullet;

		//public static bool UseAnimalAmmo => useAnimalAmmo;
		//public static bool UseMechanoidAmmo => useMechanoidAmmo;
		public static bool UseSingleLineAmmo => _useSingleLineAmmo;
		public static bool UseWiderGizmo => _useWiderGizmo;
		public static bool NpcUseAmmo => _npcUseAmmo;
		public static ThingDef InitialAmmoType { get; private set; }

		public static Dictionary<string, Dictionary<string, bool>> CategoryWeaponDictionary;
		public static Dictionary<string, bool> ExemptionWeaponDictionary;
		public static Dictionary<string, List<int>> BagSettingsDictionary;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref _hideMultipleGizmos, "HideMultipleGizmos", true);
			Scribe_Values.Look(ref _pawnAmmoSpawnRate, "PawnAmmoSpawnRate", 1f);
			Scribe_Values.Look(ref _pawnAmmoMinSpawnRate, "PawnAmmoMinSpawnRate", 0.5f);
			Scribe_Values.Look(ref _useAmmoPerBullet, "UseAmmoPerBullet", true);
			//Scribe_Values.Look(ref useMechanoidAmmo, "UseMechanoidAmmo", false, false);
			Scribe_Values.Look(ref _useSingleLineAmmo, "UseSingleLineAmmo");
			Scribe_Values.Look(ref _useWiderGizmo, "UseWiderGizmo");
			Scribe_Values.Look(ref _npcUseAmmo, "NpcUseAmmo", true);
			//Scribe_Values.Look(ref useAnimalAmmo, "UseAnimalAmmo", false, false);
			Scribe_Values.Look(ref _initialAmmoString, "InitialAmmoString", "");
			base.ExposeData();
		}

		public static void Initialize()
		{
			if (CategoryWeaponDictionary == null)
			{
				CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
			}

			if (ExemptionWeaponDictionary == null)
			{
				ExemptionWeaponDictionary = new Dictionary<string, bool>();
			}

			if (BagSettingsDictionary == null)
			{
				BagSettingsDictionary = new Dictionary<string, List<int>>();
			}
		}

		public static ThingDef GetDefaultAmmo()
		{
			if (_initialAmmoString.NullOrEmpty())
			{
				InitialAmmoType = AmmoLogic.AvailableAmmo.FirstOrDefault();
				if (InitialAmmoType != null)
				{
					_initialAmmoString = InitialAmmoType.defName;
				}
			}
			else
			{
				InitialAmmoType = AmmoLogic.AvailableAmmo.FirstOrDefault(x => x.defName == _initialAmmoString);
			}

			return InitialAmmoType;
		}

		private static void Reset()
		{
			_hideMultipleGizmos = true;
			_useAmmoPerBullet = false;
			//useAnimalAmmo = false;
			//useMechanoidAmmo = false;
			_useSingleLineAmmo = false;
			_useWiderGizmo = false;
			_npcUseAmmo = true;
			_pawnAmmoSpawnRate = 1f;
			_pawnAmmoMinSpawnRate = 0.5f;
			InitialAmmoType = AmmoLogic.AvailableAmmo?.FirstOrDefault();
			if (InitialAmmoType != null)
			{
				_initialAmmoString = InitialAmmoType.defName;
			}

			AmmoLogic.ResetInitialize();
		}

		public void DoWindowContents(Rect inRect)
		{
			try
			{
				var list = new Listing_Standard();
				list.Begin(inRect);
				if (list.ButtonText(Translate.DefaultSettings))
				{
					Reset();
					_changesMade = true;
				}

				list.Gap(2);
				if (list.ButtonText(Translate.ChangePage))
				{
					_firstPage = !_firstPage;
				}

				if (_firstPage)
				{
					Rect rect1 = list.GetRect(_lineHeight);
					Widgets.CheckboxLabeled(rect1, Translate.PerBullet, ref _useAmmoPerBullet);
					if (Mouse.IsOver(rect1))
					{
						Widgets.DrawHighlight(rect1);
					}

					TooltipHandler.TipRegion(rect1, Translate.PerBulletDesc);

					Rect rect2 = list.GetRect(_lineHeight);
					Widgets.CheckboxLabeled(rect2, Translate.HideMultipleGizmos, ref _hideMultipleGizmos);
					if (Mouse.IsOver(rect2))
					{
						Widgets.DrawHighlight(rect2);
					}

					TooltipHandler.TipRegion(rect2, Translate.HideMultipleGizmosDesc);

					//Rect rect3 = list.GetRect(lineHeight);
					//Widgets.CheckboxLabeled(rect3, Translate.MechanoidAmmo, ref useMechanoidAmmo);
					//if (Mouse.IsOver(rect3)) {
					//    Widgets.DrawHighlight(rect3);
					//}
					//TooltipHandler.TipRegion(rect3, Translate.MechanoidAmmoDesc);

					//Rect rect4 = list.GetRect(lineHeight);
					//Widgets.CheckboxLabeled(rect4, Translate.AnimalAmmo, ref useAnimalAmmo);
					//if (Mouse.IsOver(rect4)) {
					//    Widgets.DrawHighlight(rect4);
					//}
					//TooltipHandler.TipRegion(rect4, Translate.AnimalAmmoDesc);

					Rect rect5 = list.GetRect(_lineHeight);
					Widgets.CheckboxLabeled(rect5, Translate.LtsUseSingleLineAmmo, ref _useSingleLineAmmo);
					if (Mouse.IsOver(rect5))
					{
						Widgets.DrawHighlight(rect5);
					}

					TooltipHandler.TipRegion(rect5, Translate.LtsUseSingleLineAmmoDesc);

					Rect rect6 = list.GetRect(_lineHeight);
					Widgets.CheckboxLabeled(rect6, Translate.LtsUseWiderGizmo, ref _useWiderGizmo);
					if (Mouse.IsOver(rect6))
					{
						Widgets.DrawHighlight(rect6);
					}

					TooltipHandler.TipRegion(rect6, Translate.LtsUseWiderGizmoDesc);
					
					Rect rect7 = list.GetRect(_lineHeight);
					Widgets.CheckboxLabeled(rect7, Translate.NpcUseAmmo, ref _npcUseAmmo);
					if (Mouse.IsOver(rect7))
					{
						Widgets.DrawHighlight(rect7);
					}

					TooltipHandler.TipRegion(rect7, Translate.NpcUseAmmoDesc);


					Rect rect8 = list.GetRect(_lineHeight);
					Rect rect9 = list.GetRect(_lineHeight);
					Widgets.Label(rect8, Translate.NpcMaxAmmo((int)(_pawnAmmoSpawnRate * 100)));
					TooltipHandler.TipRegion(rect8, Translate.NpcMaxAmmoDesc);
					_pawnAmmoSpawnRate = Widgets.HorizontalSlider_NewTemp(rect9.ContractedBy(Margin),
						_pawnAmmoSpawnRate,
						_pawnAmmoMinSpawnRate, 1f, true, "");
					
					Rect rect10 = list.GetRect(_lineHeight);
					Rect rect11 = list.GetRect(_lineHeight);
					Widgets.Label(rect10, Translate.NpcMinAmmo((int)(_pawnAmmoMinSpawnRate * 100)));
					TooltipHandler.TipRegion(rect10, Translate.NpcMinAmmoDesc);
					_pawnAmmoMinSpawnRate = Widgets.HorizontalSlider_NewTemp(rect11.ContractedBy(Margin),
						_pawnAmmoMinSpawnRate,
						0.1f, _pawnAmmoSpawnRate, true, "");
					

					list.GapLine(2);
					// Kit settings
					if (_chosenKit == null)
					{
						_chosenKit = AmmoLogic.AvailableKits.First();
					}

					if (BagSettingsDictionary.TryGetValue(_chosenKit.defName, out List<int> bags))
					{
						Widgets.Label(list.GetRect(_lineHeight), Translate.AvailableKits);
						//Add image right half
						Widgets.Label(list.GetRect(_lineHeight), Translate.ChangeBagDesc);
						Rect kitDropDown = list.GetRect(_lineHeight);
						Widgets.Dropdown(kitDropDown.LeftHalf().LeftHalf(), _chosenKit,
							(selectedThing) => _ = selectedThing, GenerateKitCategoryMenu,
							Translate.AmmunitionCategory + _chosenKit?.label);

						if (Widgets.ButtonText(kitDropDown.LeftHalf().RightHalf(), Translate.AddBag))
						{
							if (_chosenKit?.comps.FirstOrDefault(x => x is CompProps_Kit) is CompProps_Kit kitProp)
							{
								bags.Add(kitProp.ammoCapacity.First());
								_changesMade = true;
							}
						}

						list.Gap(2);
						for (int i = 0; i < bags.Count; i++)
						{
							int val = bags[i];
							string buffer = val.ToString();
							Rect kitRect = list.GetRect(_lineHeight).LeftHalf();
							Rect leftPart = kitRect.LeftPartPixels(kitRect.width - 24);
							Rect rightPart = kitRect.RightPartPixels(24);
							Widgets.TextFieldNumericLabeled(leftPart, Translate.BagNumber(i + 1), ref val, ref buffer,
								1, 999);

							if (val != bags[i])
							{
								_changesMade = true;
								bags[i] = val;
							}

							if (Widgets.ButtonImage(rightPart, Widgets.CheckboxOffTex) && bags.Count > 1)
							{
								bags.RemoveAt(i);
								_changesMade = true;
							}
						}
					}
				}
				//Second page
				else
				{
					if (AmmoLogic.AmmoCategoryDefs.Any())
					{
						if (_chosenCategory == null)
						{
							_chosenCategory = AmmoLogic.AmmoCategoryDefs.First();
						}

						Rect innerRect = list.GetRect(_lineHeight);
						Widgets.Dropdown(innerRect.LeftHalf(), _chosenCategory,
							selectedThing => _ = selectedThing, GenerateAmmoCategoryMenu,
							Translate.AmmunitionCategory + _chosenCategory?.label);

						Widgets.Label(innerRect.RightHalf().ContractedBy(Margin, 0f), Translate.AvailableAmmoCategory);
						Rect ammoRect = list.GetRect(_lineHeight * 2f);
						Rect textRect = list.GetRect(_lineHeight * 1f);
						for (int i = 0; i < _chosenCategory.ammoDefs.Count; i++)
						{
							ThingDef tempDef = DefDatabase<ThingDef>.GetNamed(_chosenCategory.ammoDefs[i]);
							var tempRect = new Rect((ammoRect.x + i * _lineHeight * 2f), ammoRect.y,
								_lineHeight * 2, _lineHeight * 2);

							if (Widgets.ButtonImage(tempRect, (Texture2D)tempDef.graphic.MatSingle.mainTexture))
							{
								InitialAmmoType = tempDef;
								_initialAmmoString = tempDef.defName;
							}

							if (InitialAmmoType == tempDef)
							{
								Widgets.DrawTextureFitted(tempRect, Widgets.CheckboxOnTex, 1);
								Widgets.LabelFit(tempRect, Translate.SelectedStandardAmmo);
							}

							if (!Mouse.IsOver(tempRect))
							{
								continue;
							}

							float tempY = textRect.y;
							Widgets.DrawHighlight(tempRect);
							Widgets.Label(textRect.x + i * _lineHeight, ref tempY, _lineHeight * 10,
								tempDef.label);
						}

						list.GapLine();
						//Filter buttons
						Rect settings1 = list.GetRect(_lineHeight);
						if (Widgets.ButtonText(settings1.LeftHalf(), Translate.DefaultAssociation))
						{
							AmmoLogic.ResetInitialize();
							_changesMade = true;
						}

						Rect settings2 = list.GetRect(_lineHeight);
						if (Widgets.ButtonText(settings2.LeftHalf(), Translate.Enable))
						{
							_enable = true;
						}

						if (Widgets.ButtonText(settings2.RightHalf(), Translate.Disable))
						{
							_disable = true;
						}

						Rect settings3 = list.GetRect(_lineHeight);
						if (Widgets.ButtonText(settings3.LeftHalf(), Translate.EnableMod))
						{
							_enableMod = true;
						}

						if (Widgets.ButtonText(settings3.RightHalf(), Translate.DisableMod))
						{
							_disableMod = true;
						}

						//Main
						Rect optionsRect = list.GetRect((_lineHeight * 12f) - Margin * 2);
						Widgets.DrawMenuSection(optionsRect);
						Rect filterRect = optionsRect.LeftPartPixels(250);
						Rect weaponsInRect = optionsRect.RightPartPixels(optionsRect.width - 250);
						TextAnchor tempAnchor = Text.Anchor;
						Text.Anchor = TextAnchor.MiddleCenter;
						Widgets.Label(filterRect.TopHalf().TopHalf().TopHalf().ContractedBy(4), Translate.Filter);
						Text.Anchor = tempAnchor;
						//Search
						_searchFilter = Widgets.TextField(filterRect.TopHalf().TopHalf().BottomHalf().ContractedBy(2),
							_searchFilter);

						Widgets.CheckboxLabeled(filterRect.TopHalf().BottomHalf().TopHalf().ContractedBy(2),
							((TechLevel)2).ToStringHuman(), ref _neolithic);

						Widgets.CheckboxLabeled(filterRect.TopHalf().BottomHalf().BottomHalf().ContractedBy(2),
							((TechLevel)3).ToStringHuman(), ref _medieval);

						Widgets.CheckboxLabeled(filterRect.BottomHalf().TopHalf().TopHalf().ContractedBy(2),
							((TechLevel)4).ToStringHuman(), ref _industrial);

						Widgets.CheckboxLabeled(filterRect.BottomHalf().TopHalf().BottomHalf().ContractedBy(2),
							((TechLevel)5).ToStringHuman(), ref _spacer);

						Widgets.CheckboxLabeled(filterRect.BottomHalf().BottomHalf().TopHalf().ContractedBy(2),
							((TechLevel)6).ToStringHuman(), ref _ultra);

						Widgets.CheckboxLabeled(filterRect.BottomHalf().BottomHalf().BottomHalf().ContractedBy(2),
							((TechLevel)7).ToStringHuman(), ref _archotech);

						Widgets.DrawMenuSection(weaponsInRect);
						var weaponsOutRect = new Rect(weaponsInRect.ContractedBy(Margin));
						float num2 = ((AmmoLogic.AvailableProjectileWeapons.Count(x =>
							               (x.techLevel == TechLevel.Neolithic && _neolithic) ||
							               (x.techLevel == TechLevel.Medieval && _medieval) ||
							               (x.techLevel == TechLevel.Industrial && _industrial) ||
							               (x.techLevel == TechLevel.Spacer && _spacer) ||
							               (x.techLevel == TechLevel.Ultra && _ultra) ||
							               (x.techLevel == TechLevel.Archotech && _archotech)) *
						               (_lineHeight)));

						if (num2 < weaponsOutRect.height)
						{
							num2 = weaponsOutRect.height;
						}

						//Weapons
						var weaponsViewRect = new Rect(0f, 0f, weaponsOutRect.width - Margin * 4, num2);
						Widgets.BeginScrollView(weaponsOutRect, ref _scrollWeaponsPos, weaponsViewRect);
						var weaponsList = new Listing_Standard(weaponsInRect, () => _scrollWeaponsPos);
						weaponsList.Begin(weaponsViewRect);
						foreach (ThingDef thingDef in AmmoLogic.AvailableProjectileWeapons.Where(thingDef =>
							         thingDef.label.ToLower().Contains(_searchFilter.ToLower())))
						{
							switch (thingDef.techLevel)
							{
								case TechLevel.Neolithic:
									if (!_neolithic)
									{
										continue;
									}

									break;


								case TechLevel.Medieval:
									if (!_medieval)
									{
										continue;
									}

									break;


								case TechLevel.Industrial:
									if (!_industrial)
									{
										continue;
									}

									break;


								case TechLevel.Spacer:
									if (!_spacer)
									{
										continue;
									}

									break;


								case TechLevel.Ultra:
									if (!_ultra)
									{
										continue;
									}

									break;


								case TechLevel.Archotech:
									if (!_archotech)
									{
										continue;
									}

									break;


								case TechLevel.Undefined:
								case TechLevel.Animal:
								default:
									break;
							}

							Rect weaponRect = weaponsList.GetRect(_lineHeight);
							Rect iconRect = weaponRect.LeftPartPixels(25);
							Rect mainWeaponRect = weaponRect.LeftPartPixels(weaponRect.width - 50);
							mainWeaponRect.x += 25;
							Rect subWeaponRect = weaponRect.RightPartPixels(25f);
							Widgets.DrawTextureFitted(iconRect, Widgets.GetIconFor(thingDef), 0.95f);
							CategoryWeaponDictionary.TryGetValue(_chosenCategory.defName,
								out Dictionary<string, bool> dic);

							dic.TryGetValue(thingDef.defName, out bool res);
							ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out bool exemption);
							bool val = res;
							bool val2 = exemption;
							if (_enable)
							{
								dic.SetOrAdd(thingDef.defName, true);
								_changesMade = true;
							}

							if (_disable)
							{
								dic.SetOrAdd(thingDef.defName, false);
								_changesMade = true;
							}

							if (_enableMod)
							{
								ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
								_changesMade = true;
							}

							if (_disableMod)
							{
								ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, true);
								_changesMade = true;
							}

							Widgets.DrawHighlightIfMouseover(weaponRect);
							TooltipHandler.TipRegion(mainWeaponRect,
								thingDef.description + "\n\n" + Translate.UseAmmo(_chosenCategory.label));

							Widgets.CheckboxLabeled(mainWeaponRect, " " + thingDef.LabelCap, ref val);
							if (val != res)
							{
								dic.SetOrAdd(thingDef.defName, val);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
								_changesMade = true;
							}

							TooltipHandler.TipRegion(subWeaponRect, Translate.ExemptWeapon);
							Widgets.Checkbox(subWeaponRect.x, subWeaponRect.y, ref val2, 24, false, false,
								AmmoDisabled, AmmoEnabled);

							if (val2 == exemption)
							{
								continue;
							}

							ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, val2);
							AmmoLogic.ResetHyperLinksForWeapon(thingDef);
							_changesMade = true;
						}

						weaponsList.End();
						Widgets.EndScrollView();
						_disable = false;
						_enable = false;
						_disableMod = false;
						_enableMod = false;
					}
					else
					{
						list.Label(Translate.NoAmmoCategories);
					}
				}

				if (_changesMade)
				{
					AmmoLogic.Save();
					_changesMade = false;
				}

				list.End();
				this.Write();
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
			}
		}

		private static IEnumerable<Widgets.DropdownMenuElement<AmmoCategoryDef>> GenerateAmmoCategoryMenu(
			AmmoCategoryDef def)
		{
			using (List<AmmoCategoryDef>.Enumerator enumerator = AmmoLogic.AmmoCategoryDefs.ToList().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					AmmoCategoryDef ammo = enumerator.Current;
					yield return new Widgets.DropdownMenuElement<AmmoCategoryDef>
					{
						option = new FloatMenuOption(ammo?.label, delegate { _chosenCategory = ammo; }), payload = ammo
					};
				}
			}
		}

		private static IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateKitCategoryMenu(ThingDef def)
		{
			using (List<ThingDef>.Enumerator enumerator = AmmoLogic.AvailableKits.ToList().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ThingDef kit = enumerator.Current;
					yield return new Widgets.DropdownMenuElement<ThingDef>
					{
						option = new FloatMenuOption(kit?.label, delegate { _chosenKit = kit; }), payload = kit
					};
				}
			}
		}
	}
}