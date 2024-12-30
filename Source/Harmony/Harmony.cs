using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

// ReSharper disable InconsistentNaming

namespace Ammunition.Harmony
{
	[StaticConstructorOnStartup]
	public class AmmoHarmonyPatches
	{
		static AmmoHarmonyPatches()
		{
			var harmony = new HarmonyLib.Harmony("limetreesnake.ammunition");

			// Patch WorkGiver_HunterHunt.HasHuntingWeapon
			PatchMethod(harmony, typeof(WorkGiver_HunterHunt), "HasHuntingWeapon", postfix: "HasHuntingWeapon_PostFix");

			// Patch VerbTracker.CreateVerbTargetCommand
			PatchMethod(harmony, typeof(VerbTracker), "CreateVerbTargetCommand",
				postfix: "CreateVerbTargetCommand_PostFix");

			// Patch Verb.Available
			PatchMethod(harmony, typeof(Verb), "Available", postfix: "Available_PostFix");

			// Patch Verb.TryCastNextBurstShot
			PatchMethod(harmony, typeof(Verb), "TryCastNextBurstShot", prefix: "TryCastNextBurstShot_PreFix");

			// Patch Verb.WarmupComplete
			PatchMethod(harmony, typeof(Verb), "WarmupComplete", prefix: "WarmupComplete_PreFix");

			// Patch Verb_LaunchProjectile.get_Projectile
			PatchMethod(harmony, typeof(Verb_LaunchProjectile), "get_Projectile", prefix: "Projectile_PreFix");

			// Patch PawnGenerator.RedressPawn
			PatchMethod(harmony, typeof(PawnGenerator), "RedressPawn", postfix: "RedressPawn_PostFix");

			// Patch PawnGenerator.GeneratePawn (specific overload with one parameter)
			PatchMethod(harmony, typeof(PawnGenerator), "GeneratePawn", postfix: "GeneratePawn_PostFix", paramCount: 1);

			// Patch ReverseDesignatorDatabase.InitDesignators
			PatchMethod(harmony, typeof(ReverseDesignatorDatabase), "InitDesignators",
				postfix: "InitDesignators_PostFix");

			// Patch FloatMenuMakerMap.AddHumanlikeOrders
			PatchMethod(harmony, typeof(FloatMenuMakerMap), "AddHumanlikeOrders",
				postfix: "AddHumanlikeOrders_PostFix");

			// Initialize Ammo Logic
			AmmoLogic.Initialize();
		}

		// Helper method to patch methods with prefixes and postfixes
		private static void PatchMethod(HarmonyLib.Harmony harmony, Type type, string methodName, string prefix = null,
			string postfix = null, int paramCount = -1)
		{
			var method = paramCount == -1
				? AccessTools.Method(type, methodName)
				: type.GetMethods().FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == paramCount);

			if (method == null)
			{
				Log.Error($"Could not find method {methodName} in {type} - LTS Ammo Framework Harmony");
				return;
			}

			if (prefix != null)
			{
				var prefixMethod = new HarmonyMethod(typeof(AmmoHarmonyPatches).GetMethod(prefix));
				harmony.Patch(method, prefixMethod);
			}

			if (postfix != null)
			{
				var postfixMethod = new HarmonyMethod(typeof(AmmoHarmonyPatches).GetMethod(postfix));
				harmony.Patch(method, postfix: postfixMethod);
			}
		}

		/// <summary>
		/// Postfix patch for the HasHuntingWeapon method. 
		/// This method checks whether the hunting pawn has the appropriate ammo for their weapon.
		/// If the weapon requires ammo and the pawn does not have any, the method returns false.
		/// </summary>
		/// <param name="p">The pawn being checked for a hunting weapon.</param>
		/// <param name="__result">The result indicating whether the pawn has a hunting weapon.</param>
		public static void HasHuntingWeapon_PostFix(Pawn p, ref bool __result)
		{
			try
			{
				// Only perform AmmoCheck if the result is true
				if (__result)
				{
					__result = AmmoLogic.AmmoCheck(p, p.equipment.Primary, out _, false);
				}
			}
			catch (Exception e)
			{
				Log.Error($"Error in HasHuntingWeapon_PostFix for {p?.Name}: {e.Message}");
			}
		}

		/// <summary>
		/// Postfix patch for creating a verb target command. If the verb caster does not have the required ammo, 
		/// the command is disabled with a "No Ammo" message.
		/// </summary>
		/// <param name="ownerThing">The object owning the verb.</param>
		/// <param name="verb">The verb being executed.</param>
		/// <param name="__result">The result command to be modified if needed.</param>
		public static void CreateVerbTargetCommand_PostFix(Thing ownerThing, Verb verb, Command_VerbTarget __result)
		{
			try
			{
				// Only proceed if the verb is a projectile verb, and it's cast by a pawn
				if (!(verb is Verb_LaunchProjectile) || !verb.CasterIsPawn)
				{
					return;
				}

				// Check if the caster pawn has the required ammo; disable the command if not
				if (!AmmoLogic.AmmoCheck(verb.CasterPawn, verb.EquipmentSource, out _, false))
				{
					__result.Disable(Translate.NoAmmo);
				}
			}
			catch (Exception e)
			{
				Log.Error($"Error in CreateVerbTargetCommand_PostFix for {verb?.CasterPawn?.Name}: {e.Message}");
			}
		}

		public static void Available_PostFix(ref bool __result, Verb __instance)
		{
			try
			{
				// Early exit if the result is already false or it's a melee attack
				if (!__result || __instance.IsMeleeAttack)
				{
					return;
				}

				// Null checks for CasterPawn and EquipmentSource to avoid potential null references
				Pawn casterPawn = __instance?.CasterPawn;
				Thing equipment = __instance?.EquipmentSource;

				if (casterPawn == null || equipment == null)
				{
					return; // No valid pawn or equipment, exit early
				}

				// Ammo check and result modification
				__result = AmmoLogic.AmmoCheck(casterPawn, equipment, out _, false);

				// If there's no ammo and the pawn is hunting, end the job
				if (!__result && casterPawn.CurJob?.def == RimWorld.JobDefOf.Hunt)
				{
					casterPawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
			}
			catch (Exception e)
			{
				Log.Message($"Error in Available_PostFix: {e.Message}");
			}
		}

		/// <summary>
		/// Prefix patch for the Verb.WarmupComplete method.
		/// This method checks if the caster pawn has the required ammo before completing the warmup phase of a ranged attack.
		/// If the weapon is melee-based, the method skips the ammo check.
		/// </summary>
		/// <param name="__instance">The instance of the verb (attack) being processed.</param>
		/// <returns>
		/// Returns true if the pawn has enough ammo or if the attack is melee-based. 
		/// If the pawn lacks ammo, the original method is skipped by returning false.
		/// </returns>
		[HarmonyPriority(150)]
		public static bool WarmupComplete_PreFix(Verb __instance)
		{
			try
			{
				// If it's a melee attack, we skip the ammo check and allow the method to proceed.
				if (__instance.IsMeleeAttack)
				{
					return true;
				}

				// Perform the ammo check, consuming ammo based on the UseAmmoPerBullet setting.
				return AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _,
					!Settings.Settings.UseAmmoPerBullet);
			}
			catch (Exception e)
			{
				Log.Error(
					$"Error in WarmupComplete_PreFix for {__instance?.CasterPawn?.Name} - {__instance?.EquipmentSource?.def?.label}: {e.Message}");
				return true; // Always return true to allow the original method to proceed in case of an error.
			}
		}

		/// <summary>
		/// Prefix patch for the Verb.TryCastNextBurstShot method.
		/// This method checks if the caster pawn has enough ammo to fire the next shot in a burst.
		/// If the weapon supports burst firing (with a burst count greater than 1), it triggers additional shots.
		/// </summary>
		/// <param name="__instance">The instance of the verb responsible for casting the next shot.</param>
		/// <returns>
		/// Returns true if the shot can be fired (i.e., the pawn has enough ammo or the attack is melee-based).
		/// If the ammo check fails, the method returns false, preventing the original method from proceeding.
		/// </returns>
		[HarmonyPriority(150)]
		public static bool TryCastNextBurstShot_PreFix(Verb __instance)
		{
			try
			{
				// Skip ammo checks for melee attacks
				if (__instance.IsMeleeAttack)
				{
					return true;
				}

				// Check if the pawn has enough ammo; consume ammo per bullet if necessary
				var canFire = AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out var comp,
					Settings.Settings.UseAmmoPerBullet);

				// If the pawn can't fire or no kit component is available, return the result of the ammo check
				if (!canFire || comp == null)
				{
					return canFire;
				}

				// Check if the ammo has burst count and fire additional shots if required
				var burstCount = comp.LastUsedAmmo.GetModExtension<AmmunitionExtension>()?.burstCount ?? 1;
				if (burstCount > 1)
				{
					// Fire additional shots based on burst count
					for (var i = 1; i < burstCount; i++)
					{
						Traverse.Create(__instance).Method("TryCastShot").GetValue();
					}
				}

				return true;
			}
			catch (Exception e)
			{
				Log.Error(
					$"Error in TryCastNextBurstShot_PreFix for {__instance?.CasterPawn?.Name} - {__instance?.EquipmentSource?.def?.label}: {e.Message}");
				return true; // Always return true to allow the original method to proceed in case of error
			}
		}


		/// <summary>
		/// Replaces the projectile with the one provided by the last used ammo if available.
		/// </summary>
		/// <param name="__result">The result projectile that will be modified.</param>
		/// <param name="__instance">The instance of the verb.</param>
		/// <returns>Returns false if the result has been replaced, otherwise true to continue original method.</returns>
		[HarmonyPriority(150)]
		public static bool Projectile_PreFix(ref ThingDef __result, Verb_LaunchProjectile __instance)
		{
			try
			{
				// Check if the caster pawn has ammo for the weapon
				if (!AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out var comp, false))
				{
					// If no ammo is found, allow the original method to run (return true)
					return true;
				}

				// If the kit component or the last used bullet is null, allow the original method to run
				if (comp?.LastUsedBullet == null)
				{
					return true;
				}

				// Replace the projectile with the last used bullet
				__result = comp.LastUsedBullet;
				return false; // Skip the original method since we've provided a new result
			}
			catch (Exception e)
			{
				Log.Message(e.Message);
				return true; // Continue with the original method in case of any exception
			}
		}

		/// <summary>
		/// Postfix method to equip generated pawns with appropriate ammo gear if they have apparel.
		/// </summary>
		/// <param name="__result">The generated pawn.</param>
		public static void GeneratePawn_PostFix(Pawn __result)
		{
			try
			{
				if (__result?.apparel != null)
				{
					AmmoLogic.EquipPawn(__result);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Error equipping pawn after generation: {ex.Message}\n{ex.StackTrace}");
			}
		}

		/// <summary>
		/// Postfix method to re-equip pawns with appropriate ammo gear after redressing.
		/// </summary>
		/// <param name="pawn">The pawn being redressed.</param>
		public static void RedressPawn_PostFix(Pawn pawn)
		{
			try
			{
				if (pawn?.apparel != null)
				{
					AmmoLogic.EquipPawn(pawn);
				}
			}
			catch (Exception e)
			{
				Log.Error($"Error equipping pawn after redress: {e.Message}");
			}
		}

		/// <summary>
		/// Postfix method to add a custom ammo looting designator to the designator list.
		/// </summary>
		/// <param name="___desList">The list of designators to which the custom designator is added.</param>
		public static void InitDesignators_PostFix(List<Designator> ___desList)
		{
			___desList.Add(new Designator_LootAmmo());
		}

		/// <summary>
		/// Adds looting and stripping options to the right-click menu when interacting with humanlike pawns. 
		/// Ensures that the pawn can reach the target, and handles special cases such as quest-related pawns or 
		/// targets that cannot be looted.
		/// </summary>
		/// <param name="clickPos">The position where the user clicked.</param>
		/// <param name="pawn">The pawn who is trying to interact with the target.</param>
		/// <param name="opts">The list of FloatMenuOption objects representing the right-click menu options.</param>
		public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			// Get the targets at the click position
			var targets = GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true);

			foreach (var stripTarget in targets)
			{
				// Null check for the target and the thing itself
				if (stripTarget == null || stripTarget.Thing == null)
				{
					continue; // Skip invalid targets
				}

				// Check if the pawn can reach the target
				if (!pawn.CanReach(stripTarget, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption(
						"CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
						": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}

				// Check if the target pawn is quest-related (HasExtraHomeFaction)
				if (stripTarget.Pawn != null && stripTarget.Pawn.HasExtraHomeFaction())
				{
					opts.Add(new FloatMenuOption(
						"CannotStrip".Translate(stripTarget.Thing.LabelCap, stripTarget.Thing) +
						": " + "QuestRelated".Translate().CapitalizeFirst(), null));
					continue;
				}

				// Check if the thing can be looted by the colony
				if (AmmoLogic.CanBeLootedByColony(stripTarget.Thing))
				{
					// Add a looting option
					var menuOption = new FloatMenuOption(Translate.DesignatorLootAmmo, delegate
					{
						stripTarget.Thing.SetForbidden(value: false, warnOnFail: false);
						var job = JobMaker.MakeJob(JobDefOf.LTS_LootAmmo, stripTarget);
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarget.Thing);
					});

					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(menuOption, pawn, stripTarget));
				}
				else
				{
					// Add an option indicating the pawn cannot loot ammo
					opts.Add(new FloatMenuOption(Translate.DesignatorCannotLootAmmo, null));
				}
			}
		}
	}
}