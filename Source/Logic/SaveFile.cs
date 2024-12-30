using System;
using System.Collections.Generic;
using Ammunition.Settings;

namespace Ammunition.Logic
{
    [Serializable]
    internal class SaveFile
    {
        public Dictionary<string, Dictionary<string, bool>> Categories;
        public Dictionary<string, bool> Exemptions;
        public Dictionary<string, BagSettings>  Bags;
    }
}
