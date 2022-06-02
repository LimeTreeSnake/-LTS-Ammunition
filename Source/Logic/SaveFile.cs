using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ammunition.Logic
{
    [Serializable]
    internal class SaveFile
    {
        public Dictionary<string, Dictionary<string, bool>> Categories;
        public Dictionary<string, bool> Exemptions;
    }
}
