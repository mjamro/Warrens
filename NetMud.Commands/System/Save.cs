﻿using NetMud.Commands.Attributes;
using NetMud.Communication.Messaging;
using NetMud.DataAccess.FileSystem;
using NetMud.DataStructure.Administrative;
using NetMud.DataStructure.Architectural;
using NetMud.DataStructure.Player;
using System.Collections.Generic;

namespace NetMud.Commands.System
{
    /// <summary>
    /// Invokes the current container's RenderToVisible
    /// </summary>
    [CommandKeyword("save", false)]
    [CommandPermission(StaffRank.Player)]
    [CommandRange(CommandRangeType.Touch, 0)]
    public class Save : CommandPartial
    {
        /// <summary>
        /// All Commands require a generic constructor
        /// </summary>
        public Save()
        {
            //Generic constructor for all IHelpfuls is needed
        }

        /// <summary>
        /// Executes this command
        /// </summary>
        public override void Execute()
        {
            IPlayer player = (IPlayer)Actor;

            Message messagingObject = new Message("You save your life.");

            messagingObject.ExecuteMessaging(Actor, null, null, OriginLocation, null, 0);

            PlayerData playerDataWrapper = new PlayerData();

            //Save the player out
            playerDataWrapper.WriteOnePlayer(player);
        }

        /// <summary>
        /// Renders syntactical help for the command, invokes automatically when syntax is bungled
        /// </summary>
        /// <returns>string</returns>
        public override IEnumerable<string> RenderSyntaxHelp()
        {
            List<string> sb = new List<string>
            {
                "Valid Syntax: save"
            };

            return sb;
        }

        /// <summary>
        /// The custom body of help text
        /// </summary>
        public override MarkdownString HelpText
        {
            get
            {
                return string.Format("Save writes your character to the backup set. This also happens automatically behind the scenes quite often.");
            }
            set {  }
        }
    }
}
