using System;
using System.Collections.Generic;
using System.Linq;
using Ammunition.Components;
using Ammunition.DefModExtensions;
using Ammunition.Designators;
using Ammunition.Language;
using Ammunition.Logic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using JobDefOf = Ammunition.Defs.JobDefOf;

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

			harmony.Patch(AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("CreateVerbTargetCommand_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "Available"), null,
				new HarmonyMethod(typeof(Harmony).GetMethod("Available_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot"),
				new HarmonyMethod(typeof(Harmony).GetMethod("TryCastShot_PreFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "WarmupComplete"),
				new HarmonyMethod(typeof(Harmony).GetMethod("WarmupComplete_PreFix")),
				new HarmonyMethod(typeof(Harmony).GetMethod("WarmupComplete_PostFix")));

			harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "get_Projectile"),
				new HarmonyMethod(typeof(Harmony).GetMethod("Projectile_PreFix")));

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

			AmmoLogic.Initialize();
		}

		public static void HasHuntingWeapon_PostFix(Pawn p, ref bool __result)
		{
			try
			{
				if (__result)
				{
					__result = AmmoLogic.AmmoCheck(p, p.equipment.Primary, out _, false);
				}
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
			}
		}

		public static void Available_PostFix(ref bool __result, Verb_LaunchProjectile __instance)
		{
			try
			{
				if (!__result)
				{
					return;
				}

				__result = AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, false);
				if (!__result && __instance.CasterPawn.CurJob.def == RimWorld.JobDefOf.Hunt)
				{
					__instance.CasterPawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
			}
		}

		public static void CreateVerbTargetCommand_PostFix(Thing ownerThing,
			Verb verb,
			Command_VerbTarget __result)
		{
			try
			{
				if (!(verb is Verb_LaunchProjectile && verb.CasterIsPawn))
				{
					return;
				}

				if (!AmmoLogic.AmmoCheck(verb.CasterPawn, verb.EquipmentSource, out _, false))
				{
					__result.Disable(Translate.NoAmmo);
				}
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
			}
		}

		[HarmonyPriority(150)]
		public static bool WarmupComplete_PreFix(Verb_LaunchProjectile __instance, out KitComponent __state)
		{
			__state = null;
			try
			{
				return !Settings.Settings.UseAmmoPerBullet ||
				       AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out __state, true);
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
				return true;
			}
		}

		[HarmonyPriority(150)]
		public static bool TryCastShot_PreFix(ref bool __result, Verb_LaunchProjectile __instance)
		{
			try
			{
				__result = Settings.Settings.UseAmmoPerBullet
					? AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, false)
					: AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, true);

				return __result;
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
				return true;
			}
		}

		[HarmonyPriority(150)]
		public static void WarmupComplete_PostFix(Verb_LaunchProjectile __instance, KitComponent __state)
		{
			if (__state == null)
			{
				return;
			}

			int burst = __state.LastUsedAmmo.GetModExtension<AmmunitionExtension>().burstCount;
			if (burst <= 1)
			{
				return;
			}

			for (int i = 1; i < burst; i++)
			{
				Traverse.Create(__instance).Method("TryCastShot").GetValue();
			}
		}

		[HarmonyPriority(150)]
		public static bool Projectile_PreFix(ref ThingDef __result, Verb_LaunchProjectile __instance)
		{
			try
			{
				if (!AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out KitComponent comp,
					    false))
				{
					return true;
				}

				if (comp?.LastUsedBullet == null)
				{
					return true;
				}

				__result = comp.LastUsedBullet;
				return false;
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
				return true;
			}
		}
		
		public static void GeneratePawn_PostFix(Pawn __result)
		{
			if (__result?.apparel != null)
			{
				AmmoLogic.EquipPawn(__result);
			}
		}

		public static void RedressPawn_PostFix(Pawn pawn)
		{
			if (pawn?.apparel != null)
			{
				AmmoLogic.EquipPawn(pawn);
			}
		}

		public static void InitDesignators_PostFix(List<Designator> ___desList)
		{
			___desList.Add(new Designator_LootAmmo());
		}

		public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			opts.AddRange(GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true)
				.Select(stripTarget => pawn.CanReach(stripTarget, PathEndMode.ClosestTouch, Danger.Deadly)
					? ((stripTarget.Pawn == null || !stripTarget.Pawn.HasExtraHomeFaction())
						? ((AmmoLogic.CanBeLootedByColony(stripTarget.Thing))
							? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
								Translate.DesignatorLootAmmo, delegate
								{
									stripTarget.Thing.SetForbidden(value: false, warnOnFail: false);
									pawn.jobs.TryTakeOrderedJob(
										JobMaker.MakeJob(JobDefOf.LTS_LootAmmo, stripTarget), JobTag.Misc);

									StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarget.Thing);
								}), pawn, stripTarget)
							: new FloatMenuOption(Translate.DesignatorCannotLootAmmo, null))
						: new FloatMenuOption("CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
						                      ": " +
						                      "QuestRelated".Translate().CapitalizeFirst(), null))
					: new FloatMenuOption("CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
					                      ": " +
					                      "NoPath".Translate().CapitalizeFirst(), null)));
		}
	}
}