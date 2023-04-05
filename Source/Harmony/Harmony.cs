using System.Linq;
using Verse;
using HarmonyLib;
using Ammunition.Components;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment

namespace Ammunition.Harmony
{
	[StaticConstructorOnStartup]
	public class Harmony
	{
		static Harmony()
		{
			var harmony = new HarmonyLib.Harmony("limetreesnake.ammunition");
			harmony.Patch(AccessTools.Method(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("HasHuntingWeapon_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot"),
				new HarmonyMethod(typeof(Harmony).GetMethod("TryCastShot_PreFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "WarmupComplete"),
				new HarmonyMethod(typeof(Harmony).GetMethod("WarmupComplete_PreFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "get_Projectile"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("Projectile_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "RedressPawn"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("RedressPawn_PostFix")));

			harmony.Patch(
				typeof(PawnGenerator).GetMethods()
					.FirstOrDefault(x => x.Name == "GeneratePawn" && x.GetParameters().Count() == 1), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("GeneratePawn_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(ReverseDesignatorDatabase), "InitDesignators"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("InitDesignators_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("AddHumanlikeOrders_PostFix")));

			Logic.AmmoLogic.Initialize();
		}

		[HarmonyPriority(150)]
		public static bool WarmupComplete_PreFix(Verb_LaunchProjectile __instance)
		{
			if (Settings.Settings.UseAmmoPerBullet)
			{
				return Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, true);
			}

			return true;
		}

		[HarmonyPriority(150)]
		public static bool TryCastShot_PreFix(bool __result, Verb_LaunchProjectile __instance)
		{
			__result = Settings.Settings.UseAmmoPerBullet
				? Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, false)
				: Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, true);

			return __result;
		}

		[HarmonyPriority(150)]
		public static void Projectile_PostFix(Verb_LaunchProjectile __instance, ThingDef __result)
		{
			if (__result == null)
			{
				return;
			}

			if (Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out KitComponent comp,
				    false))
			{
				if (comp?.LastUsedBullet != null)
				{
					__result = comp.LastUsedBullet;
				}
			}
			else
			{
				__result = null;
			}
		}

		public static void HasHuntingWeapon_PostFix(Pawn p, bool __result)
		{
			if (__result)
			{
				__result = Logic.AmmoLogic.AmmoCheck(p, p.equipment.Primary, out _, false);
			}
		}

		public static void GeneratePawn_PostFix(Pawn __result)
		{
			if (__result?.apparel != null)
			{
				Logic.AmmoLogic.EquipPawn(__result);
			}
		}

		public static void RedressPawn_PostFix(Pawn pawn)
		{
			if (pawn?.apparel != null)
			{
				Logic.AmmoLogic.EquipPawn(pawn);
			}
		}

		public static void InitDesignators_PostFix(List<Designator> ___desList)
		{
			___desList.Add(new Designators.Designator_LootAmmo());
		}

		public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			opts.AddRange(GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true)
				.Select(stripTarget => pawn.CanReach(stripTarget, PathEndMode.ClosestTouch, Danger.Deadly)
					? ((stripTarget.Pawn == null || !stripTarget.Pawn.HasExtraHomeFaction())
						? ((Logic.AmmoLogic.CanBeLootedByColony(stripTarget.Thing))
							? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
								Language.Translate.DesignatorLootAmmo, delegate
								{
									stripTarget.Thing.SetForbidden(value: false, warnOnFail: false);
									pawn.jobs.TryTakeOrderedJob(
										JobMaker.MakeJob(Defs.JobDefOf.LTS_LootAmmo, stripTarget), JobTag.Misc);

									StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarget.Thing);
								}), pawn, stripTarget)
							: new FloatMenuOption(Language.Translate.DesignatorCannotLootAmmo, null))
						: new FloatMenuOption("CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
						                      ": " +
						                      "QuestRelated".Translate().CapitalizeFirst(), null))
					: new FloatMenuOption("CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
					                      ": " +
					                      "NoPath".Translate().CapitalizeFirst(), null)));
		}
	}
}