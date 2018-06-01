﻿using NetMud.DataStructure.Base.System;
using System.Collections.Generic;

namespace NetMud.DataStructure.Behaviors.Rendering
{
    /// <summary>
    /// For when something that can be held is looked at as being held
    /// </summary>
    public interface IRenderAsHeld
    {
        /// <summary>
        /// Renders output for this entity when it is held by something they are looking at
        /// </summary>
        /// <param name="viewer">entity initiating the command</param>
        /// <param name="holder">entity holding the thing</param>
        /// <returns>the output</returns>
        IEnumerable<string> RenderAsHeld(IEntity viewer, IEntity holder);
    }
}
