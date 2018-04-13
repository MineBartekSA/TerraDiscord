using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace TerraDiscord
{
    public static class Config
    {
        private static string _path = Path.Combine(TShockAPI.TShock.SavePath, "TerraDiscord.json");
        public static ConfJson current;

        public static void Init()
        {
            if(File.Exists(_path))
            {
                try
                {
                    current = JsonConvert.DeserializeObject<ConfJson>(File.ReadAllText(_path));
                }
                catch(Exception exe)
                {
                    TShockAPI.TShock.Log.Error("[TerraDiscord] Failed tring to read config, overwriteing...");
                    TShockAPI.TShock.Log.Error("[TerraDiscord] Error: " + exe.Message);

                    current = new ConfJson
                    {
                        Token = "Please fill me senpai!!~",
                        Channel = "Here tooooo!!!~",
                        ARole = "TAdmin",
                        PMRole = "PermMute",
                        Whitelist = Whitelist.None,
                        ChatColor = Color.LawnGreen,
                        TCC = new TerrariaChatColor { A = Color.LawnGreen.A, R = Color.LawnGreen.R, G = Color.LawnGreen.G, B = Color.LawnGreen.B },
                        DFormat = "{nick}: {message}", //{nick} - Player username, {prefix} - Group prefix, {suffix} - Group suffix, {group} - Group name, {message} - Message
                        DCSpecifier = "!",
                        TFormat = "[Discord]{nick}: {message}", //{nick} - Username, {role} - First role, {message} - Message
                        TCID = false,
                        TCIDFilter = true,
                        TCIDBlackList = new List<string> { "user", "group", "stop", "restart" },
                        TCIDRole = "TAdmin"
                    };

                    File.WriteAllText(_path, JsonConvert.SerializeObject(current, Formatting.Indented));
                }
                

                bool isOverrided = false;
                if (current.Token == null || current.Token == string.Empty)
                {
                    current.Token = "Please fill me senpai!!~";
                    isOverrided = true;
                }
                if (current.Channel == null || current.Channel == string.Empty)
                {
                    current.Channel = "Here tooooo!!!~";
                    isOverrided = true;
                }
                if (current.ARole == null || current.ARole == string.Empty)
                {
                    current.ARole = "TAdmin";
                    isOverrided = true;
                }
                if(current.PMRole == null || current.PMRole == string.Empty)
                {
                    current.PMRole = "PermMute";
                    isOverrided = true;
                }
                if(current.Whitelist == null)
                {
                    current.Whitelist = Whitelist.None;
                    isOverrided = true;
                }
                if (current.TCC == null)
                {
                    current.ChatColor = Color.LawnGreen;
                    current.TCC = new TerrariaChatColor { A = current.ChatColor.A, R = current.ChatColor.R, G = current.ChatColor.G, B = current.ChatColor.A };
                    isOverrided = true;
                }
                if (current.DFormat == null || current.DFormat == string.Empty)
                {
                    current.DFormat = "{nick}: {message}";
                    isOverrided = true;
                }
                if (current.DCSpecifier == null || current.DCSpecifier == string.Empty)
                {
                    current.DCSpecifier = "!";
                    isOverrided = true;
                }
                if (current.TFormat == null || current.TFormat == string.Empty)
                {
                    current.TFormat = "[Discord]{nick}: {message}";
                    isOverrided = true;
                }
                if (current.TCIDRole == null || current.TCIDRole == string.Empty)
                {
                    current.TCIDRole = "TAdmin";
                    isOverrided = true;
                }

                if (isOverrided)
                    File.WriteAllText(_path, JsonConvert.SerializeObject(current, Formatting.Indented));

                current.ChatColor = new Color(current.TCC.R, current.TCC.G, current.TCC.B, current.TCC.A);
            }
            else
            {
                current = new ConfJson
                {
                    Token = "Please fill me senpai!!~",
                    Channel = "Here tooooo!!!~",
                    ARole = "TAdmin",
                    PMRole = "PermMute",
                    Whitelist = Whitelist.None, // 0 - None, 1 - Discord, 2 - Terraria, 3 - Both
                    ChatColor = Color.LawnGreen,
                    TCC = new TerrariaChatColor { A = Color.LawnGreen.A, R = Color.LawnGreen.R, G = Color.LawnGreen.G, B = Color.LawnGreen.B },
                    DFormat = "{nick}: {message}", //{nick} - Player username, {prefix} - Group prefix, {suffix} - Group suffix, {group} - Group name, {message} - Message
                    DCSpecifier = "!",
                    TFormat = "[Discord]{nick}: {message}", //{nick} - Username, {role} - First role, {message} - Message
                    TCID = false,
                    TCIDFilter = true,
                    TCIDBlackList = new List<string> { "user", "group", "stop", "restart" },
                    TCIDRole = "TAdmin"
                };

                File.WriteAllText(_path, JsonConvert.SerializeObject(current, Formatting.Indented));
            }
        }
    }

    public class ConfJson
    {
        [JsonProperty("Token")]
        public string Token { get; set; }
        [JsonProperty("ChannelID")]
        public string Channel { get; set; }
        [JsonProperty("AdminRole")]
        public string ARole { get; set; }
        [JsonProperty("White/BlacklistRole")]
        public string PMRole { get; set; }
        [JsonProperty("Whitelist")]
        public Whitelist? Whitelist { get; set; }
        [JsonProperty("TerrariaChatColor")]
        public TerrariaChatColor TCC { get; set; }
        [JsonIgnore]
        public Color ChatColor { get; set; }
        [JsonProperty("DiscordMessagesFormat")]
        public string DFormat { get; set; }
        [JsonProperty("DiscordCommandsSpecifier")]
        public string DCSpecifier { get; set; }
        [JsonProperty("TerrariaChatMessagesFormat")]
        public string TFormat { get; set; }
        [JsonProperty("TerrariaCommandsInDiscord")]
        public bool TCID { get; set; }
        [JsonProperty("TCIDCommandFilter")]
        public bool TCIDFilter { get; set; }
        [JsonProperty("TCIDCommandFilterBlackList")]
        public List<string> TCIDBlackList { get; set; }
        [JsonProperty("TCIDRoleName")]
        public string TCIDRole { get; set; }
    }

    public class TerrariaChatColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
    }

    public enum Whitelist
    {
        None = 0,
        Discord = 1,
        Terraria = 2,
        Both = 3
    }
}
