﻿
using System.Linq;
using Verse;
using HarmonyLib;
using Ammunition.Components;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace Ammunition.Harmony {
    [StaticConstructorOnStartup]
    public class Harmony {
        static Harmony() {
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("limetreesnake.ammunition");
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon", null, null), null, new HarmonyMethod(typeof(Harmony).GetMethod("HasHuntingWeapon_PostFix")), null, null);
            if (Settings.Settings.UseAmmoPerBullet) {
                harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot"), new HarmonyMethod(typeof(Harmony).GetMethod("TryCastShot_PreFix")), null);
            }
            else {
                harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "WarmupComplete"), new HarmonyMethod(typeof(Harmony).GetMethod("WarmupComplete_PreFix")), null);
            }
            harmony.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "get_Projectile"), null, new HarmonyMethod(typeof(Harmony).GetMethod("Projectile_PostFix")));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "RedressPawn"), null, new HarmonyMethod(typeof(Harmony).GetMethod("RedressPawn_PostFix")));
            harmony.Patch(typeof(PawnGenerator).GetMethods().FirstOrDefault(x => x.Name == "GeneratePawn" && x.GetParameters().Count() == 1), null, new HarmonyMethod(typeof(Harmony).GetMethod("GeneratePawn_PostFix")));
            //harmony.Patch(AccessTools.Method(typeof(ReverseDesignatorDatabase), "InitDesignators"), null, new HarmonyMethod(typeof(Harmony).GetMethod("InitDesignators_PostFix")));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(Harmony).GetMethod("AddHumanlikeOrders_PostFix")));
            Logic.AmmoLogic.Initialize();

        }
        [HarmonyPriority(150)]
        public static bool WarmupComplete_PreFix(ref Verb_LaunchProjectile __instance) {
            return Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, true);
        }
        [HarmonyPriority(150)]
        public static bool TryCastShot_PreFix(ref Verb_LaunchProjectile __instance, ref bool __result) {
            __result = Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out _, true);
            return __result;
        }
        [HarmonyPriority(150)]
        public static void Projectile_PostFix(ref Verb_LaunchProjectile __instance, ref ThingDef __result) {
            if (__result == null)
                return;
            if (Logic.AmmoLogic.AmmoCheck(__instance.CasterPawn, __instance.EquipmentSource, out KitComponent comp, false)) {
                if (comp != null && comp.LastUsedBullet != null) {
                    __result = comp.LastUsedBullet;
                }
            }
            else {
                __result = null;
            }
        }
        public static void HasHuntingWeapon_PostFix(ref Pawn p, ref bool __result) {
            if (__result) {
                __result = Logic.AmmoLogic.AmmoCheck(p, p.equipment.Primary, out _, false);
            }
        }
        public static void GeneratePawn_PostFix(ref Pawn __result) {
            if (__result != null && __result.apparel != null) {
                Logic.AmmoLogic.EquipPawn(__result);
            }
        }
        public static void RedressPawn_PostFix(ref Pawn pawn) {
            if (pawn != null && pawn.apparel != null) {
                Logic.AmmoLogic.EquipPawn(pawn);
            }
        }
        public static void InitDesignators_PostFix(ref List<Designator> ___desList) {
            ___desList.Add(new Designators.Designator_LootAmmo());
        }
        public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts) {
            foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true)) {
                LocalTargetInfo stripTarg = item;
                FloatMenuOption floatMenu =
                    pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly) ?
                        ((stripTarg.Pawn == null || !stripTarg.Pawn.HasExtraHomeFaction()) ?
                            ((Logic.AmmoLogic.CanBeLootedByColony(stripTarg.Thing)) ?
                            FloatMenuUtility.DecoratePrioritizedTask(
                                new FloatMenuOption(Language.Translate.DesignatorLootAmmo,
                                delegate {
                                    stripTarg.Thing.SetForbidden(value: false, warnOnFail: false);
                                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Defs.JobDefOf.LTS_LootAmmo, stripTarg), JobTag.Misc);
                                    StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarg.Thing);
                                }), pawn, stripTarg)
                            : new FloatMenuOption(Language.Translate.DesignatorCannotLootAmmo, null))
                        : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null))
                    : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                opts.Add(floatMenu);
            }
        }
    }
}