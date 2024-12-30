using RimWorld;
using Verse;
using System.Collections.Generic;
using Ammunition.Logic;

namespace Ammunition.Alerts
{
	class AlertAmmo : Alert
	{
		private readonly List<Pawn> _shootersWithoutAmmo = new List<Pawn>();

		private List<Pawn> ShootersWithoutAmmo
		{
			get
			{
				this._shootersWithoutAmmo.Clear();

				foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
				{
					// Skip invalid pawns: not spawned, needs medical rest, or is downed
					if (!pawn.Spawned && !pawn.BrieflyDespawned() ||
					    (HealthAIUtility.ShouldSeekMedicalRest(pawn) && pawn.InBed()) ||
					    pawn.Downed)
					{
						continue;
					}

					// Check if the pawn has ammo
					if (!AmmoLogic.AmmoCheck(pawn, pawn.equipment?.Primary, out _, false))
					{
						this._shootersWithoutAmmo.Add(pawn);
					}
				}

				return this._shootersWithoutAmmo;
			}
		}


		public AlertAmmo()
		{
			defaultPriority = AlertPriority.High;
		}

		public override string GetLabel()
		{
			return Language.Translate.NoAmmo;
		}

		public override TaggedString GetExplanation()
		{
			return Language.Translate.NoAmmoDesc;
		}

		public override AlertReport GetReport()
		{
			return AlertReport.CulpritsAre(this.ShootersWithoutAmmo);
		}
	}
}