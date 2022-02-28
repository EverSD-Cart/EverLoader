using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EverLoader.Enums
{
    public enum RomFeatureFilter
    {
        [Description("Show all ROMs")]
        AllRoms = 0,
        [Description("Show recently added ROMs")]
        RecentlyAdded = 1,
        [Description("Show ROMs without description")]
        RomsWithoutDescription = 2,
        [Description("Show ROMs without boxart images")]
        RomsWithoutBoxart = 3,
        [Description("Show ROMs without banner image")]
        RomsWithoutBanner = 4,
        [Description("Show selected ROMs (for sync)")]
        SelectedForSync = 5,
    }
}
