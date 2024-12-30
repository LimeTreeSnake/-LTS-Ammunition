using System;
using System.Collections.Generic;

namespace Ammunition.Settings
{
	[Serializable]
	public class BagSettings
	{
		public List<int> AmmoCapacities { get; set; } = new List<int>(); // Custom settings for each bag
	}
}