﻿using System;
using System.Linq;

using NetMud.DataStructure.Behaviors.Rendering;
using NetMud.Utility;

namespace NetMud.Interp
{
    public static class Interpret
    {
        public static string Render(string commandString, IActor actor)
        {
            try
            {
                //Need some way to build a context object to work off of
                //TODO: Actually care about actor details somehow, off of ICommand likely
                var commandContext = new Context(commandString, actor);

                //Derp, we had an error with accessing the command somehow, usually to do with parameter collection or access permissions
                if (commandContext.AccessErrors.Count() > 0)
                    return RenderUtility.EncapsulateOutput(commandContext.AccessErrors);

                commandContext.Command.Execute();
            }
            catch(Exception ex)
            {
                //TODO: Dont return this sort of thing, testing phase only
                return ex.Message;
            }

            return string.Empty;
        }
    }
}
