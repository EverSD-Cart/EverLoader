using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EverLoader.Enums
{
    public enum RomListFilter
    {
        [Description("Show all ROMs")]
        AllRoms = 0,
        [Description("Show selected ROMs (for sync)")]
        SelectedForSync = 1,
        [Description("Show recently added ROMs")]
        RecentlyAdded = 2,
        [Description("Show ROMs without description")]
        RomsWithoutDescription = 3,
        [Description("Show ROMs without boxart images")]
        RomsWithoutBoxart = 4,
        [Description("Show ROMs without banner image")]
        RomsWithoutBanner = 5
    }
}
