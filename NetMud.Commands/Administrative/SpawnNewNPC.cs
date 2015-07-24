﻿using NetMud.DataStructure.Base.System;
using NetMud.DataStructure.Behaviors.Rendering;
using NutMud.Commands.Attributes;
using System.Collections.Generic;

using NetMud.Utility;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.SupportingClasses;
using NetMud.Data.Game;

namespace NutMud.Commands.System
{
    //Really help can be invoked on anything that is helpful, even itself
    [CommandKeyword("SpawnNewNPC", false)]
    [CommandPermission(StaffRank.Admin)]
    [CommandParameter(CommandUsage.Subject, typeof(NetMud.Data.EntityBackingData.NonPlayerCharacter), new CacheReferenceType[] { CacheReferenceType.Data }, "[0-9]+", false)] //for IDs
    [CommandParameter(CommandUsage.Subject, typeof(NetMud.Data.EntityBackingData.NonPlayerCharacter), new CacheReferenceType[] { CacheReferenceType.Data }, "[a-zA-z]+", false)] //for names
    [CommandParameter(CommandUsage.Target, typeof(IContains), new CacheReferenceType[] { CacheReferenceType.Entity }, true)]
    [CommandRange(CommandRangeType.Touch, 0)]
    public class SpawnNewNPC : ICommand, IHelpful
    {
        public IActor Actor { get; set; }
        public object Subject { get; set; }
        public object Target { get; set; }
        public object Supporting { get; set; }
        public ILocation OriginLocation { get; set; }
        public IEnumerable<ILocation> Surroundings { get; set; }

        public SpawnNewNPC()
        {
            //Generic constructor for all IHelpfuls is needed
        }

        public void Execute()
        {
            var newObject = (INonPlayerCharacter)Subject;
            var sb = new List<string>();
            IContains spawnTo;

            //No target = spawn to room you're in
            if (Target != null)
                spawnTo = (IContains)Target;
            else
                spawnTo = OriginLocation;

            var entityObject = new Intelligence(newObject, spawnTo);

            //TODO: keywords is janky, location should have its own identifier name somehow for output purposes
            sb.Add(string.Format("{0} spawned to {1}", entityObject.DataTemplate.Name, spawnTo.Keywords[0]));

            var messagingObject = new MessageCluster(RenderUtility.EncapsulateOutput(sb), "You are ALIVE", "You have been given $S$", "$S$ appears in the $T$.", string.Empty);

            messagingObject.ExecuteMessaging(Actor, entityObject, spawnTo, OriginLocation, null);
        }

        public IEnumerable<string> RenderSyntaxHelp()
        {
            var sb = new List<string>();

            sb.Add(string.Format("Valid Syntax: spawnNewNPC &lt;object name&gt;"));
            sb.Add("spawnNewNPC  &lt;NPC name&gt;  &lt;location name to spawn to&gt;".PadWithString(14, "&nbsp;", true));

            return sb;
        }

        /// <summary>
        /// Renders the help text for the help command itself
        /// </summary>
        /// <returns>string</returns>
        public IEnumerable<string> RenderHelpBody()
        {
            var sb = new List<string>();

            sb.Add(string.Format("spawnNewNPC spawns a new NPC from its data template into the room or into a specified location."));

            return sb;
        }
    }
}