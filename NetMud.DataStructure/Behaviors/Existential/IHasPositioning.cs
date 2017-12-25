﻿using NetMud.DataStructure.Base.Place;
using NetMud.DataStructure.Behaviors.Rendering;

namespace NetMud.DataStructure.Behaviors.Existential
{
    /// <summary>
    /// Var collection for IExist
    /// </summary>
    public interface IHasPositioning
    {
        /// <summary>
        /// position in the worlds
        /// </summary>
        IGlobalPosition Position { get; }

        /// <summary>
        /// Change the position of this
        /// </summary>
        /// <param name="direction">the 0-360 direction we're moving</param>
        /// <param name="newPosition">The new position the thing is in, will return with the original one if nothing moved</param>
        /// <returns>was this thing moved?</returns>
        bool TryMoveDirection(int direction, IGlobalPosition newPosition);

        /// <summary>
        /// Move this inside of something
        /// </summary>
        /// <param name="container">The container to move into</param>
        /// <returns>was this thing moved?</returns>
        bool TryMoveInto(IContains container);

        /// <summary>
        /// Change the position of this without physical movement
        /// </summary>
        /// <param name="newPosition">The new position the thing is in, will return with the original one if nothing moved</param>
        /// <returns>was this thing moved?</returns>
        bool TryTeleport(IGlobalPosition newPosition);
    }

    /// <summary>
    /// Coords + world designator
    /// </summary>
    public interface IGlobalPosition
    {
        /// <summary>
        /// The world this is in
        /// </summary>
        IZone CurrentZone { get; set;  }

        /// <summary>
        /// Current location this entity is in
        /// </summary>
        IContains CurrentLocation { get; set; }
    }
}
