﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using NetMud.Authentication;
using NetMud.Data.Players;
using NetMud.DataAccess;
using NetMud.DataAccess.Cache;
using NetMud.DataAccess.FileSystem;
using NetMud.DataStructure.Architectural;
using NetMud.DataStructure.Architectural.EntityBase;
using NetMud.DataStructure.Gossip;
using NetMud.DataStructure.Player;
using NetMud.DataStructure.System;
using NetMud.Gossip;
using NetMud.Interp;
using NetMud.Utility;
using NetMud.Websock.OutputFormatting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NetMud.Websock
{
    /// <summary>
    /// Main handler of descriptor connections for websockets
    /// </summary>
    public class Descriptor : Channel, IDescriptor
    {
        /// <summary>
        /// The user manager for the application, handles authentication from the web
        /// </summary>
        public ApplicationUserManager UserManager { get; private set; }

        /// <summary>
        /// Unique string for this live entity
        /// </summary>
        public string BirthMark { get; set; }

        /// <summary>
        /// When this entity was born to the world
        /// </summary>
        public DateTime Birthdate { get; set; }

        /// <summary>
        /// User id of connected player
        /// </summary>
        public string _userId { get; set; }

        /// <summary>
        /// The player connected
        /// </summary>
        private IPlayer _currentPlayer;

        /// <summary>
        /// Creates an instance of the command negotiator
        /// </summary>
        public Descriptor() : this(new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        /// <summary>
        /// Creates an instance of the command negotiator with a specified user manager
        /// </summary>
        /// <param name="userManager">the authentication manager from the web</param>
        public Descriptor(ApplicationUserManager userManager)
        {
            UserManager = userManager;

            Birthdate = DateTime.Now;
        }

        #region Caching
        /// <summary>
        /// What type of cache is this using
        /// </summary>
        public virtual CacheType CachingType => CacheType.Live;

        /// <summary>
        /// Put it in the cache
        /// </summary>
        /// <returns>success status</returns>
        public virtual bool PersistToCache()
        {
            try
            {
                LiveCache.Add<IDescriptor>(this);
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex, LogChannels.SystemWarnings);
                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Send a sound file to play
        /// </summary>
        /// <param name="soundUri"></param>
        /// <returns></returns>
        public bool SendSound(string soundUri)
        {
            OutputStatus outputFormat = new OutputStatus
            {
                SoundToPlay = soundUri
            };

            Send(Utility.SerializationUtility.Serialize(outputFormat));

            return true;
        }

        /// <summary>
        /// Wraps sending messages to the connected descriptor
        /// </summary>
        /// <param name="strings">the output</param>
        /// <returns>success status</returns>
        public bool SendOutput(IEnumerable<string> strings)
        {
            //TODO: Stop hardcoding this but we have literally no sense of injury/self status yet
            SelfStatus self = new SelfStatus
            {
                Body = new BodyStatus
                {
                    Health = _currentPlayer.CurrentHealth == 0 ? 100 : 100 / (2M * _currentPlayer.CurrentHealth),
                    Stamina = _currentPlayer.CurrentStamina
                },
                CurrentActivity = _currentPlayer.CurrentAction,
                Balance = _currentPlayer.Balance.ToString(),
                CurrentArt = _currentPlayer.LastAttack?.Name ?? "",
                CurrentCombo = _currentPlayer.LastCombo?.Name ?? "",
                CurrentTarget = _currentPlayer.GetTarget() == null ? "" 
                                                                   : _currentPlayer.GetTarget() == _currentPlayer 
                                                                        ? "Your shadow" 
                                                                        : _currentPlayer.GetTarget().GetDescribableName(_currentPlayer),
                CurrentTargetHealth = _currentPlayer.GetTarget() == null || _currentPlayer.GetTarget() == _currentPlayer ? double.PositiveInfinity
                                                                        : _currentPlayer.GetTarget().CurrentHealth == 0 ? 100 : 100 / (2 * _currentPlayer.CurrentHealth),
                Position = _currentPlayer.StancePosition.ToString(),
                Stance = _currentPlayer.Stance,
                Stagger = _currentPlayer.Stagger.ToString(),
                Qualities = string.Join("", _currentPlayer.Qualities.Where(quality => quality.Visible).Select(quality => string.Format("<div class='qualityRow'><span>{0}</span><span>{1}</span></div>", quality.Name, quality.Value))),
                CurrentTargetQualities = _currentPlayer.GetTarget() == null || _currentPlayer.GetTarget() == _currentPlayer ? "" 
                                                                            : string.Join("", _currentPlayer.GetTarget().Qualities.Where(quality => quality.Visible).Select(quality => string.Format("<div class='qualityRow'><span>{0}</span><span>{1}</span></div>", quality.Name, quality.Value)))
            };

            IGlobalPosition currentLocation = _currentPlayer.CurrentLocation;
            IEnumerable<string> populace = Enumerable.Empty<string>();
            string locationDescription = string.Empty;

            decimal distance = currentLocation.CurrentSection == 0M ? 20M : 20M / (2M * currentLocation.CurrentSection);
            populace = LiveCache.GetAll<IPlayer>().Where(player => player.Descriptor != null && player != _currentPlayer).Select(data => data.GetDescribableName(_currentPlayer));
            locationDescription = string.Format("You are {0} meters from the exit.", distance);

            LocalStatus local = new LocalStatus
            {
                Populace = populace.ToArray(),
                LocationDescriptive = locationDescription
            };

            OutputStatus outputFormat = new OutputStatus
            {
                Occurrence = strings.Count() > 0 ? EncapsulateOutput(strings) : "",
                Self = self,
                Local = local
            };

            Send(Utility.SerializationUtility.Serialize(outputFormat));

            return true;
        }

        /// <summary>
        /// Wraps sending UI Updates to the connected descriptor
        /// </summary>
        public bool SendWrapper()
        {
            return SendOutput(new string[0]);
        }

        /// <summary>
        /// Wraps sending messages to the connected descriptor
        /// </summary>
        /// <param name="str">the output</param>
        /// <returns>success status</returns>
        public bool SendOutput(string str)
        {
            //Easier just to handle it in one place
            return SendOutput(new List<string>() { str });
        }

        /// <summary>
        /// Disconnects the client socket
        /// </summary>
        /// <param name="finalMessage">the final string data to send the socket before closing it</param>
        public void Disconnect(string finalMessage)
        {
            SendOutput(finalMessage);

            Close();
        }

        /// <summary>
        /// Open the socket, the client gets set up in the constructor
        /// </summary>
        public void Open()
        {
            new Task(OnOpen).Start();
        }

        #region "Socket Management"
        public override void OnClose()
        {
            IEnumerable<IPlayer> validPlayers = LiveCache.GetAll<IPlayer>().Where(player => player.Descriptor != null
                        && player.Template<IPlayerTemplate>().Account.Config.WantsNotification(_currentPlayer.AccountHandle, false, AcquaintenceNotifications.LeaveGame));

            foreach (IPlayer player in validPlayers)
            {
                player.WriteTo(new string[] { string.Format("{0} has left the game.", _currentPlayer.AccountHandle) });
            }

            if (_currentPlayer != null && _currentPlayer.Template<IPlayerTemplate>().Account.Config.GossipSubscriber)
            {
                GossipClient gossipClient = LiveCache.Get<GossipClient>("GossipWebClient");

                if (gossipClient != null)
                {
                    gossipClient.SendNotification(_currentPlayer.AccountHandle, Notifications.LeaveGame);
                }
            }

            base.OnClose();
        }

        /// <summary>
        /// Handles initial connection
        /// </summary>
        public override void OnOpen()
        {
            base.OnOpen();

            BirthMark = LiveCache.GetUniqueIdentifier(string.Format(cacheKeyFormat, WebSocketContext.AnonymousID));
            PersistToCache();

            UserManager = new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            ValidateUser(WebSocketContext.CookieCollection[".AspNet.ApplicationCookie"]);

            LoggingUtility.Log(content: "Socket client accepted", channel: LogChannels.SocketCommunication);
        }

        /// <summary>
        /// Handles when the connected descriptor sends input
        /// </summary>
        /// <param name="e">the events of the message</param>
        public override void OnMessage(string message)
        {
            if (_currentPlayer == null)
            {
                OnError(new Exception("Invalid character; please reload the client and try again."));
                return;
            }

            IEnumerable<string> errors = Interpret.Render(message, _currentPlayer);

            //It only sends the errors
            if (errors.Any(str => !string.IsNullOrWhiteSpace(str)))
            {
                SendOutput(errors);
            }
        }

        /// <summary>
        /// Handles when the connection faults
        /// </summary>
        /// <param name="err">the error</param>
        private void OnError(Exception ex)
        {
            //Log it
            LoggingUtility.LogError(ex, false);
        }

        /// <summary>
        /// Ping the client for keepalive
        /// </summary>
        private void SendPing()
        {
            byte[] ping = new byte[2];

            ping[0] = 9 | 0x80;
            ping[1] = 0;

            Send(ping);
        }
        #endregion

        #region "Helpers"
        /// <summary>
        /// Validates the game account from the aspnet cookie
        /// </summary>
        /// <param name="handshake">the headers from the http request</param>
        private void ValidateUser(Cookie cookie)
        {
            //Grab the user
            GetUserIDFromCookie(cookie.Value);

            ApplicationUser authedUser = UserManager.FindById(_userId);

            IPlayerTemplate currentCharacter = authedUser.GameAccount.Characters.FirstOrDefault(ch => ch.Id.Equals(authedUser.GameAccount.CurrentlySelectedCharacter));

            if (currentCharacter == null)
            {
                Send("<p>No character selected</p>");
                return;
            }

            //Try to see if they are already live
            _currentPlayer = LiveCache.GetAll<IPlayer>().FirstOrDefault(player => player.Descriptor != null && player.Descriptor._userId == _userId);

            //Check the backup
            if (_currentPlayer == null)
            {
                PlayerData playerDataWrapper = new PlayerData();
                _currentPlayer = playerDataWrapper.RestorePlayer(currentCharacter.AccountHandle, currentCharacter);
            }

            //else new them up
            if (_currentPlayer == null)
            {
                _currentPlayer = new Player(currentCharacter);
            }

            _currentPlayer.Descriptor = this;

            //We need to barf out to the connected client the welcome message. The client will only indicate connection has been established.
            List<string> welcomeMessage = new List<string>
            {
                string.Format("Welcome to Zeno's Fight Club, {0}.", currentCharacter.FullName()),
                "You're going to want to start moving FORWARD to get to that exit."
            };

            _currentPlayer.WriteTo(welcomeMessage);

            //Send the look command in
            Interpret.Render("look", _currentPlayer);

            try
            {
                IEnumerable<IPlayer> validPlayers = LiveCache.GetAll<IPlayer>().Where(player => player.Descriptor != null
                && player.Template<IPlayerTemplate>().Account.Config.WantsNotification(_currentPlayer.AccountHandle, false, AcquaintenceNotifications.EnterGame));

                foreach (IPlayer player in validPlayers)
                {
                    player.WriteTo(new string[] { string.Format("{0} has entered the game.", _currentPlayer.AccountHandle) });
                }

                if (authedUser.GameAccount.Config.GossipSubscriber)
                {
                    GossipClient gossipClient = LiveCache.Get<GossipClient>("GossipWebClient");

                    if (gossipClient != null)
                    {
                        gossipClient.SendNotification(authedUser.GlobalIdentityHandle, Notifications.EnterGame);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex, LogChannels.SocketCommunication);
            }
        }

        /// <summary>
        /// Gets the user Id from the web from the aspnet cookie
        /// </summary>
        /// <param name="authTicketValue">the cookie's value</param>
        private void GetUserIDFromCookie(string authTicketValue)
        {
            authTicketValue = authTicketValue.Replace('-', '+').Replace('_', '/');

            int padding = 3 - ((authTicketValue.Length + 3) % 4);
            if (padding != 0)
            {
                authTicketValue += new string('=', padding);
            }

            byte[] bytes = Convert.FromBase64String(authTicketValue);

            bytes = System.Web.Security.MachineKey.Unprotect(bytes,
                "Microsoft.Owin.Security.Cookies.CookieAuthenticationMiddleware",
                        "ApplicationCookie", "v1");

            using (MemoryStream memory = new MemoryStream(bytes))
            {
                using (GZipStream compression = new GZipStream(memory, CompressionMode.Decompress))
                {
                    using (BinaryReader reader = new BinaryReader(compression))
                    {
                        reader.ReadInt32(); // Ignoring version here
                        string authenticationType = reader.ReadString();
                        reader.ReadString(); // Ignoring the default name claim type
                        reader.ReadString(); // Ignoring the default role claim type

                        int count = reader.ReadInt32(); // count of claims in the ticket

                        Claim[] claims = new Claim[count];
                        for (int index = 0; index != count; ++index)
                        {
                            string type = reader.ReadString();
                            type = type == "\0" ? ClaimTypes.Name : type;

                            string value = reader.ReadString();

                            string valueType = reader.ReadString();
                            valueType = valueType == "\0" ?
                                            "http://www.w3.org/2001/XMLSchema#string" : valueType;

                            string issuer = reader.ReadString();
                            issuer = issuer == "\0" ? "LOCAL AUTHORITY" : issuer;

                            string originalIssuer = reader.ReadString();
                            originalIssuer = originalIssuer == "\0" ? issuer : originalIssuer;

                            claims[index] = new Claim(type, value, valueType, issuer, originalIssuer);
                        }

                        ClaimsIdentity identity = new ClaimsIdentity(claims, authenticationType,
                                                              ClaimTypes.Name, ClaimTypes.Role);

                        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                        _userId = principal.Identity.GetUserId();
                    }
                }
            }
        }
        #endregion

        #region Equality Functions
        /// <summary>
        /// -99 = null input
        /// -1 = wrong type
        /// 0 = same type, wrong id
        /// 1 = same reference (same id, same type)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(ILiveData other)
        {
            if (other != null)
            {
                try
                {
                    if (other.GetType() != GetType())
                    {
                        return -1;
                    }

                    if (other.BirthMark.Equals(BirthMark))
                    {
                        return 1;
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    LoggingUtility.LogError(ex);
                }
            }

            return -99;
        }

        /// <summary>
        /// Compares this object to another one to see if they are the same object
        /// </summary>
        /// <param name="other">the object to compare to</param>
        /// <returns>true if the same object</returns>
        public bool Equals(ILiveData other)
        {
            if (other != default(ILiveData))
            {
                try
                {
                    return other.GetType() == GetType() && other.BirthMark.Equals(BirthMark);
                }
                catch (Exception ex)
                {
                    LoggingUtility.LogError(ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Compares an object to another one to see if they are the same object
        /// </summary>
        /// <param name="x">the object to compare to</param>
        /// <param name="y">the object to compare to</param>
        /// <returns>true if the same object</returns>
        public bool Equals(ILiveData x, ILiveData y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Get the hash code for comparison purposes
        /// </summary>
        /// <param name="obj">the thing to get the hashcode for</param>
        /// <returns>the hash code</returns>
        public int GetHashCode(ILiveData obj)
        {
            return obj.GetType().GetHashCode() + obj.BirthMark.GetHashCode();
        }

        /// <summary>
        /// Get the hash code for comparison purposes
        /// </summary>
        /// <returns>the hash code</returns>
        public override int GetHashCode()
        {
            return GetType().GetHashCode() + BirthMark.GetHashCode();
        }
        #endregion

        public object Clone()
        {
            throw new NotImplementedException("Not much point cloning descriptors.");
        }
    }
}
