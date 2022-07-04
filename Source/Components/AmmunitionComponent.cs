using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Ammunition.Defs;

namespace Ammunition.Components {
	public class AmmunitionComponent : ThingComp {
		public CompProps_Ammunition Props => (CompProps_Ammunition)props;
	}
	public class CompProps_Ammunition  :CompProperties {
        public ThingDef bulletDef;

		public CompProps_Ammunition() {
			this.compClass = typeof(AmmunitionComponent);
		}
	}
}
