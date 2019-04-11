﻿using NetMud.DataStructure.Administrative;
using NetMud.DataStructure.Architectural.ActorBase;
using NetMud.DataStructure.Combat;
using System.Collections.Generic;

namespace NetMud.DataStructure.Player
{
    /// <summary>
    /// Backing data for player characters
    /// </summary>
    public interface IPlayerFramework : IHaveHealth, IHaveStamina
    {
        /// <summary>
        /// Account this player belongs to
        /// </summary>
        string AccountHandle { get; set; }

        /// <summary>
        /// Command permissions for player character
        /// </summary>
        StaffRank GamePermissionsRank { get; set; }

        /// <summary>
        /// Family name for character
        /// </summary>     
        string SurName { get; set; }

        /// <summary>
        /// Is this character not graduated from the tutorial
        /// </summary>
        bool StillANoob { get; set; }

        /// <summary>
        /// fArt Combos
        /// </summary>
        HashSet<IFightingArtCombination> Combos { get; set; }
    }
}
