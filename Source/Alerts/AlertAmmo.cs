using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Ammunition.Alerts
{
    class AlertAmmo : Alert
    {
        private readonly List<Pawn> shootersWithoutAmmo = new List<Pawn>();

        private List<Pawn> ShootersWithoutAmmo
        {
            get
            {
                this.shootersWithoutAmmo.Clear();
                foreach (Pawn current in PawnsFinder.AllMaps_FreeColonists)
                {                    
                    if ((current.Spawned || current.BrieflyDespawned()) && (!HealthAIUtility.ShouldSeekMedicalRest(current) || !current.InBed()) && !current.Downed && !Logic.AmmoLogic.AmmoCheck(current, current?.equipment?.Primary, out _, false))
                    {
                        this.shootersWithoutAmmo.Add(current);
                    }
                }
                return this.shootersWithoutAmmo;
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
        //public override AlertReport GetReport() {
        //	return AlertReport.CulpritsAre(Logic.AmmoLogic.AmmoAlertCheck);
        //}
    }
}
