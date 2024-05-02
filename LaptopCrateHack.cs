using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Previous maintenance and contributions by Arainrr
 * Originally created by TheSurgeon
 */

namespace Oxide.Plugins
{
    [Info("Laptop Crate Hack", "VisEntities", "2.0.2")]
    [Description("Hack locked crates using targeting computers.")]
    public class LaptopCrateHack : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin Clans, Friends;

        #endregion 3rd Party Dependencies

        #region Fields

        private static LaptopCrateHack _plugin;
        private static Configuration _config;
        
        private const int ITEM_ID_COMPUTER = 1523195708;
        private const string FX_CODE_LOCK_SHOCK = "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab";

        private Dictionary<ulong, DateTime> _playerLastHackTimes = new Dictionary<ulong, DateTime>();
        
        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Required Targeting Computers For Hack")]
            public int RequiredTargetingComputersForHack { get; set; }
            
            [JsonProperty("Consume Targeting Computer On Hack")]
            public bool ConsumeTargetingComputerOnHack { get; set; }

            [JsonProperty("Crate Unlock Time Seconds")]
            public float CrateUnlockTimeSeconds { get; set; }

            [JsonProperty("Cooldown Between Hacks Seconds")]
            public float CooldownBetweenHacksSeconds { get; set; }

            [JsonProperty("Crate Lootable By Hacker Only")]
            public bool CrateLootableByHackerOnly { get; set; }

            [JsonProperty("Can Be Looted By Hacker Teammates")]
            public bool CanBeLootedByHackerTeammates { get; set; }

            [JsonProperty("Can Be Looted By Hacker Friends")]
            public bool CanBeLootedByHackerFriends { get; set; }

            [JsonProperty("Can Be Looted By Hacker Clanmates")]
            public bool CanBeLootedByHackerClanmates { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                RequiredTargetingComputersForHack = 1,
                ConsumeTargetingComputerOnHack = true,
                CrateUnlockTimeSeconds = 900f,
                CooldownBetweenHacksSeconds = 300f,
                CrateLootableByHackerOnly = true,
                CanBeLootedByHackerTeammates = true,
                CanBeLootedByHackerFriends = false,
                CanBeLootedByHackerClanmates = false
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null)
                return null;

            if (_config.RequiredTargetingComputersForHack > 0)
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != ITEM_ID_COMPUTER || activeItem.amount < _config.RequiredTargetingComputersForHack)
                {
                    SendReplyToPlayer(player, Lang.NeedTargetingComputer, _config.RequiredTargetingComputersForHack);
                    OnCrateHackFailed(crate);
                    return true;
                }
            }

            if (_playerLastHackTimes.TryGetValue(player.userID, out DateTime lastHackTime))
            {
                var timeSinceLastHack = DateTime.UtcNow - lastHackTime;
                if (timeSinceLastHack.TotalSeconds < _config.CooldownBetweenHacksSeconds)
                {
                    var timeLeft = _config.CooldownBetweenHacksSeconds - timeSinceLastHack.TotalSeconds;
                    SendReplyToPlayer(player, Lang.CooldownBeforeNextHack, timeLeft);
                    OnCrateHackFailed(crate);
                    return true;
                }
            }

            crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _config.CrateUnlockTimeSeconds;
            return null;
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null || crate.originalHackerPlayerId == 0)
                return;

            BasePlayer player = BasePlayer.FindByID(crate.originalHackerPlayerId);
            if (player != null)
            {
                _playerLastHackTimes[player.userID] = DateTime.UtcNow;

                if (_config.ConsumeTargetingComputerOnHack)
                {
                    Item activeItem = player.GetActiveItem();
                    if (activeItem != null && activeItem.info.itemid == ITEM_ID_COMPUTER)
                    {
                        activeItem.UseItem(_config.RequiredTargetingComputersForHack);
                    }
                }
            }
        }

        private object CanLootEntity(BasePlayer player, HackableLockedCrate crate)
        {
            if (crate == null || player == null)
                return null;

            if (_config.CrateLootableByHackerOnly && crate.originalHackerPlayerId != player.userID)
            {
                bool isTeammate = _config.CanBeLootedByHackerTeammates && AreTeammates(player, crate.originalHackerPlayerId);
                bool isFriend = _config.CanBeLootedByHackerFriends && FriendUtil.AreFriends(player.userID, crate.originalHackerPlayerId);
                bool isClanmate = _config.CanBeLootedByHackerClanmates && ClanUtil.AreClanmates(player.userID, crate.originalHackerPlayerId);

                if (isTeammate || isFriend || isClanmate)
                    return null;
                else
                {
                    SendReplyToPlayer(player, Lang.CrateLootDenied);
                    return true;
                }
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Clan Integration

        private static class ClanUtil
        {
            public static bool AreClanmates(ulong firstPlayerId, ulong secondPlayerId)
            {
                if (!VerifyPluginBeingLoaded(_plugin.Clans))
                    return false;

                return (bool)_plugin.Call("IsClanMember", firstPlayerId, secondPlayerId);
            }
        }

        #endregion Clan Integration

        #region Friend Integration
        
        private static class FriendUtil
        {
            public static bool AreFriends(ulong firstPlayerId, ulong secondPlayerId)
            {
                if (!VerifyPluginBeingLoaded(_plugin.Friends))
                    return false;

                return (bool)_plugin.Friends.Call("HasFriend", firstPlayerId, secondPlayerId);
            }
        }

        #endregion Friend Integration

        #region Functions
        
        private void OnCrateHackFailed(HackableLockedCrate crate)
        {
            RunEffect(FX_CODE_LOCK_SHOCK, crate.transform.TransformPoint(new Vector3(0.02f, 1.10f, 0.56f)), Vector3.up);
        }

        #endregion Functions

        #region Helper Functions

        public static bool VerifyPluginBeingLoaded(Plugin plugin)
        {
            if (plugin != null && plugin.IsLoaded)
                return true;
            else
                return false;
        }

        public static bool AreTeammates(BasePlayer firstPlayer, ulong secondPlayerId)
        {
            if (firstPlayer.Team != null && firstPlayer.Team.members.Contains(secondPlayerId))
                return true;

            return false;
        }

        private static void RunEffect(string prefab, Vector3 worldPosition = default(Vector3), Vector3 worldDirection = default(Vector3), Connection effectRecipient = null, bool sendToAll = false)
        {
            Effect.server.Run(prefab, worldPosition, worldDirection, effectRecipient, sendToAll);
        }

        #endregion Helper Functions

        #region Localization

        private class Lang
        {
            public const string NeedTargetingComputer = "NeedTargetingComputer";
            public const string CooldownBeforeNextHack = "CooldownBeforeNextHack";
            public const string CrateLootDenied = "CrateLootDenied";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NeedTargetingComputer] = "You need to hold <color=#FABE28>{0}</color> targeting computers in your hand to hack this crate.",
                [Lang.CooldownBeforeNextHack] = "You must wait <color=#FABE28>{0:N0}</color> more seconds before hacking another crate.",
                [Lang.CrateLootDenied] = "This crate is reserved for the original hacker and cannot be looted."
            }, this, "en");
        }

        public void SendReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}