﻿using NetMud.Communication.Messaging;
using NetMud.Data.Architectural;
using NetMud.Data.Architectural.DataIntegrity;
using NetMud.Data.Architectural.EntityBase;
using NetMud.DataAccess.Cache;
using NetMud.DataAccess.FileSystem;
using NetMud.DataStructure.Administrative;
using NetMud.DataStructure.Architectural;
using NetMud.DataStructure.Architectural.ActorBase;
using NetMud.DataStructure.Architectural.EntityBase;
using NetMud.DataStructure.Player;
using NetMud.DataStructure.System;
using NetMud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Script.Serialization;

namespace NetMud.Data.Players
{
    /// <summary>
    /// live player character entities
    /// </summary>
    [Serializable]
    [IgnoreAutomatedBackup]
    public class Player : EntityPartial, IPlayer
    {
        #region Template and Framework Values
        public override bool IsPlayer()
        {
            return true;
        }

        /// <summary>
        /// The name of the object in the data template
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public override string TemplateName
        {
            get
            {
                return Template<IPlayerTemplate>()?.Name;
            }
        }

        /// <summary>
        /// The backing data for this entity
        /// </summary>
        public override T Template<T>()
        {
            return (T)PlayerDataCache.Get(new PlayerDataCacheKey(typeof(IPlayerTemplate), AccountHandle, TemplateId));
        }

        [JsonProperty("Gender")]
        private TemplateCacheKey _gender { get; set; }

        /// <summary>
        /// "family name" for player character
        /// </summary>
        public string SurName { get; set; }

        /// <summary>
        /// Has this character "graduated" from the tutorial yet
        /// </summary>
        public bool StillANoob { get; set; }

        /// <summary>
        /// The "user" level for commands and accessibility
        /// </summary>
        public StaffRank GamePermissionsRank { get; set; }

        /// <summary>
        /// Max stamina
        /// </summary>
        public int TotalStamina { get; set; }

        /// <summary>
        /// Max Health
        /// </summary>
        public int TotalHealth { get; set; }

        /// <summary>
        /// Current stamina for this
        /// </summary>
        public int CurrentStamina { get; set; }

        /// <summary>
        /// Current health for this
        /// </summary>
        public int CurrentHealth { get; set; }
        #endregion

        [ScriptIgnore]
        [JsonIgnore]
        private LiveCacheKey _descriptorKey;

        /// <summary>
        /// The connection the player is using to chat with us
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public IDescriptor Descriptor
        {
            get
            {
                if (_descriptorKey == null)
                {
                    return default;
                }

                return LiveCache.Get<IDescriptor>(_descriptorKey);
            }

            set
            {
                _descriptorKey = new LiveCacheKey(value);

                PersistToCache();
            }
        }

        /// <summary>
        /// Type of connection this has, doesn't get saved as it's transitory information
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public override IChannelType ConnectionType
        {
            get
            {
                //All player descriptors should be of ichanneltype too
                return (IChannelType)Descriptor;
            }
        }

        /// <summary>
        /// The account this character belongs to
        /// </summary>
        public string AccountHandle { get; set; }

        /// <summary>
        /// News up an empty entity
        /// </summary>
        public Player()
        {
            Qualities = new HashSet<IQuality>();
        }

        /// <summary>
        /// News up an entity with its backing data
        /// </summary>
        /// <param name="character">the backing data</param>
        public Player(IPlayerTemplate character)
        {
            Qualities = new HashSet<IQuality>();
            TemplateId = character.Id;
            AccountHandle = character.AccountHandle;
            GetFromWorldOrSpawn();
        }

        /// <summary>
        /// Function used to close this connection
        /// </summary>
        public void CloseConnection()
        {
            Descriptor.Disconnect(string.Empty);
        }

        public override bool WriteTo(IEnumerable<string> input)
        {
            IEnumerable<string> strings = MessagingUtility.TranslateColorVariables(input.ToArray(), this);

            return Descriptor.SendOutput(strings);
        }

        public int Exhaust(int exhaustionAmount)
        {
            int stam = Sleep(-1 * exhaustionAmount);

            //TODO: Check for total exhaustion

            return stam;
        }

        public int Harm(int damage)
        {
            int health = Recover(-1 * damage);

            //TODO: Check for DEATH

            return health;
        }

        public int Recover(int recovery)
        {
            CurrentHealth = Math.Max(0, Math.Min(TotalHealth, TotalHealth + recovery));

            return CurrentHealth;
        }

        public int Sleep(int hours)
        {
            CurrentStamina = Math.Max(0, Math.Min(TotalStamina, TotalStamina + hours * 10));

            return CurrentStamina;
        }

        /// <summary>
        /// Get the live version of this in the world
        /// </summary>
        /// <returns>The live data</returns>
        public IPlayer GetLiveInstance()
        {
            return this;
        }

        #region Rendering
        #endregion

        #region SpawnBehavior
        /// <summary>
        /// Tries to find this entity in the world based on its Id or gets a new one from the db and puts it in the world
        /// </summary>
        public void GetFromWorldOrSpawn()
        {
            //Try to see if they are already there
            IPlayer me = LiveCache.Get<IPlayer>(TemplateId);

            //Isn't in the world currently
            if (me == default(IPlayer))
            {
                SpawnNewInWorld();
            }
            else
            {
                IPlayerTemplate ch = me.Template<IPlayerTemplate>();
                BirthMark = me.BirthMark;
                Birthdate = me.Birthdate;
                TemplateId = ch.Id;
                Keywords = me.Keywords;
                CurrentHealth = me.CurrentHealth;
                CurrentStamina = me.CurrentStamina;

                Qualities = me.Qualities;

                TotalHealth = me.TotalHealth;
                TotalStamina = me.TotalStamina;
                SurName = me.SurName;
                StillANoob = me.StillANoob;
                GamePermissionsRank = me.GamePermissionsRank;

                if (CurrentHealth == 0)
                {
                    CurrentHealth = ch.TotalHealth;
                }

                if (CurrentStamina == 0)
                {
                    CurrentStamina = ch.TotalStamina;
                }

                if (me.CurrentLocation == null)
                {
                    TryMoveTo(GetBaseSpawn());
                }
                else
                {
                    TryMoveTo((IGlobalPosition)me.CurrentLocation.Clone());
                }
            }
        }


        /// <summary>
        /// Spawn this new into the live world
        /// </summary>
        public override void SpawnNewInWorld()
        {
            IPlayerTemplate ch = Template<IPlayerTemplate>();

            SpawnNewInWorld(new GlobalPosition(ch.CurrentSlice));
        }

        /// <summary>
        /// Spawn this new into the live world into a specified container
        /// </summary>
        /// <param name="spawnTo">the location/container this should spawn into</param>
        public override void SpawnNewInWorld(IGlobalPosition position)
        {
            //We can't even try this until we know if the data is there
            IPlayerTemplate ch = Template<IPlayerTemplate>() ?? throw new InvalidOperationException("Missing backing data store on player spawn event.");

            Keywords = ch.Keywords;

            if (string.IsNullOrWhiteSpace(BirthMark))
            {
                BirthMark = LiveCache.GetUniqueIdentifier(ch);
                Birthdate = DateTime.Now;
            }

            Qualities = ch.Qualities;
            CurrentHealth = ch.TotalHealth;
            CurrentStamina = ch.TotalStamina;
            TotalHealth = ch.TotalHealth;
            TotalStamina = ch.TotalStamina;
            SurName = ch.SurName;
            StillANoob = ch.StillANoob;
            GamePermissionsRank = ch.GamePermissionsRank;

            IGlobalPosition spawnTo = position ?? GetBaseSpawn();

            //Set the data context's stuff too so we don't have to do this over again
            ch.Save(ch.Account, StaffRank.Player); //characters/players dont actually need approval

            TryMoveTo(spawnTo);

            UpsertToLiveWorldCache(true);

            KickoffProcesses();

            Save();
        }

        public override string TryMoveTo(IGlobalPosition newPosition)
        {
            string error = string.Empty;
            IPlayerTemplate ch = Template<IPlayerTemplate>();

            //validate position
            if (newPosition != null)
            {
                CurrentLocation = newPosition;
                UpsertToLiveWorldCache();

                ch.CurrentSlice = newPosition.CurrentSection;
                ch.SystemSave();
                ch.PersistToCache();
            }
            else
            {
                error = "Cannot move to an invalid location";
            }

            return error;
        }

        /// <summary>
        /// Save this to the filesystem in Current
        /// </summary>
        /// <returns>Success</returns>
        public override bool Save()
        {
            try
            {
                PlayerData dataAccessor = new PlayerData();
                dataAccessor.WriteOnePlayer(this);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Find the emergency we dont know where to spawn this guy spawn location
        /// </summary>
        /// <returns>The emergency spawn location</returns>
        private IGlobalPosition GetBaseSpawn()
        {
            return new GlobalPosition(0);
        }

        public override object Clone()
        {
            throw new NotImplementedException("Can't clone player objects.");
        }
        #endregion
    }
}
