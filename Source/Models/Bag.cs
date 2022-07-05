﻿using System;
using Verse;

namespace Ammunition.Models {
    [Serializable]
    public class Bag : IExposable {
        private ThingDef chosenAmmo;
        private int count;
        private int maxCount;
        private bool use;
        private int capacity;

        public ThingDef ChosenAmmo {
            get => chosenAmmo;
            set => chosenAmmo = value;
        }
        public int Count {
            get => count;
            set => count = value < 0 ? 0 : value;
        }
        public int MaxCount {
            get => maxCount;
            set => maxCount = value > capacity ? capacity : value;
        }
        public bool Use {
            get => use;
            set => use = value;
        }
        public int Capacity {
            get => capacity;
            set => capacity = value;
        }
        public void ExposeData() {
            Scribe_Defs.Look(ref chosenAmmo, "ChosenAmmo");
            Scribe_Values.Look(ref count, "Count");
            Scribe_Values.Look(ref maxCount, "MaxCount");
            Scribe_Values.Look(ref use, "Use");
            Scribe_Values.Look(ref capacity, "Capacity");
        }
    }
}