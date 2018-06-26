﻿using NetMud.Data.DataIntegrity;
using NetMud.DataAccess;
using NetMud.DataAccess.Cache;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.Base.Supporting;
using NetMud.DataStructure.Behaviors.System;
using NetMud.DataStructure.SupportingClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace NetMud.Data.EntityBackingData
{
    /// <summary>
    /// Backing data for NPCs
    /// </summary>
    [Serializable]
    public class NonPlayerCharacter : EntityBackingDataPartial, INonPlayerCharacter
    {
        /// <summary>
        /// The system type for the entity this attaches to
        /// </summary>
        [JsonIgnore]
        [ScriptIgnore]
        public override Type EntityClass
        {
            get { return typeof(Game.Intelligence); }
        }

        /// <summary>
        /// What type of approval is necessary for this content
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public override ContentApprovalType ApprovalType { get { return ContentApprovalType.Staff; } }

        /// <summary>
        /// keywords this entity is referrable by in the world by the parser
        /// </summary>
        [JsonIgnore]
        [ScriptIgnore]
        public override string[] Keywords
        {
            get
            {
                if (_keywords == null || _keywords.Length == 0)
                    _keywords = new string[] { FullName().ToLower(), Name.ToLower(), SurName.ToLower() };

                return _keywords;
            }
            set { _keywords = value; }
        }

        /// <summary>
        /// Gender data string for NPCs
        /// </summary>
        [StringDataIntegrity("Gender is empty.")]
        public string Gender { get; set; }

        /// <summary>
        /// "family name" for NPCs
        /// </summary>
        [StringDataIntegrity("Gender is empty.")]
        public string SurName { get; set; }

        [JsonProperty("RaceData")]
        private BackingDataCacheKey _raceData { get; set; }

        /// <summary>
        /// NPC's race data
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        [NonNullableDataIntegrity("Invalid racial data.")]
        public IRace RaceData
        {
            get
            {
                return BackingDataCache.Get<IRace>(_raceData);
            }
            set
            {
                _raceData = new BackingDataCacheKey(value);
            }
        }

        /// <summary>
        /// Set of output relevant to this exit. These are essentially single word descriptions to render the path
        /// </summary>
        public HashSet<IOccurrence> Descriptives { get; set; }

        /// <summary>
        /// Base constructor
        /// </summary>
        public NonPlayerCharacter()
        {
        }

        /// <summary>
        /// Full name to refer to this NPC with
        /// </summary>
        /// <returns>the full name string</returns>
        public string FullName()
        {
            return string.Format("{0} {1}", Name, SurName);
        }

        /// <summary>
        /// Get's the entity's model dimensions
        /// </summary>
        /// <returns>height, length, width</returns>
        public override Tuple<int, int, int> GetModelDimensions()
        {
            try
            {
                if (RaceData == null)
                    return new Tuple<int, int, int>(0, 0, 0);

                var height = (RaceData?.Head?.Model != null ? RaceData.Head.Model.Height : 0)
                            + (RaceData?.Torso?.Model != null ? RaceData.Torso.Model.Height : 0)
                            + (RaceData?.Legs?.Item1?.Model != null ? RaceData.Legs.Item1.Model.Height : 0);
                var length = RaceData?.Torso?.Model != null ? RaceData.Torso.Model.Length : 0;
                var width = RaceData?.Torso?.Model != null ? RaceData.Torso.Model.Width : 0;

                return new Tuple<int, int, int>(height, length, width);
            }
            catch(Exception ex)
            {
                LoggingUtility.LogError(ex);
            }

            return new Tuple<int, int, int>(0, 0, 0);
        }

        /// <summary>
        /// Get the significant details of what needs approval
        /// </summary>
        /// <returns>A list of strings</returns>
        public override IDictionary<string, string> SignificantDetails()
        {
            var returnList = base.SignificantDetails();

            returnList.Add("Race", RaceData.Name);
            returnList.Add("SurName", SurName);
            returnList.Add("Gender", Gender);

            foreach (var desc in Descriptives)
                returnList.Add("Descriptives", string.Format("{0} ({1}): {2}", desc.SensoryType, desc.Strength, desc.Event.ToString()));

            return returnList;
        }
    }
}
