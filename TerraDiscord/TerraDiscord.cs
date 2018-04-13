using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;

namespace TerraDiscord
{
    [ApiVersion(2, 1)]
    public class TerraDiscord : TerrariaPlugin
    {
        public DiscordClient DC;
        public DiscordChannel chan;
        private List<TSPlayer> TMute = new List<TSPlayer>();
        private List<DiscordUser> DMute = new List<DiscordUser>();
        private List<string> SQLTDW = new List<string>();
        private List<string> SQLTDMD = new List<string>();
        private List<string> SQLTDMT = new List<string>();

        public override string Name => "TerraDiscord";
        public override string Description => "An Discord bot plugin";
        public override string Author => "MineBartekSA";
        public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public TerraDiscord(Main game) : base(game)
        {
            // Nothing xD
        }

        public override void Initialize()
        {
            Config.Init();
            if(Config.current.Token == "Please fill me senpai!!~")
            {
                TShock.Log.ConsoleError("[TerraDiscord] Your config just moved in, or you didn't visited it!! Please visit your config!");
                return;
            }
            else if(Config.current.Channel == "Here tooooo!!!~")
            {
                TShock.Log.ConsoleError("[TerraDiscord] How dare you live you config like that? Half filled?! Go to it right now!!");
                return;
            }
            else
            {
                try { ulong chann = ulong.Parse(Config.current.Channel); }
                catch (Exception e)
                {
                    TShock.Log.ConsoleError("[TerraDiscord] Oh no!!!! Config is broken!! GO FIX THAT FAST!!");
                    TShock.Log.ConsoleError("[TerraDiscord] Hint: Your Channel ID is invalid!");
                    TShock.Log.Error("An error occured! Error: " + e.Message);
                    return;
                }

                if(Config.current.TCID)
                {
                    TShock.Log.ConsoleInfo("[TerraDiscord] The 'TerrariaCommandsInDiscord' feature is enabled! Beware who you give the TCIDRole!");
                    if (!Config.current.TCIDFilter)
                        TShock.Log.ConsoleInfo("[TerraDiscord] Command filter is disabled!! Plugin author is not responsible for any damage to the server!");
                    else
                        TShock.Log.ConsoleInfo("[TerraDiscord] Plugin author is not responsible for any damage to the server, if Command Filter is set badly, bad things my occur in wrong hands!");
                    TShock.Log.ConsoleInfo("[TerraDiscord] This is an warning! You use TCID on YOUR OWN RISK!");
                }
            }

            try
            {
                DBInit();
                StartBot().GetAwaiter().GetResult();
            }
            catch(DSharpPlus.Exceptions.UnauthorizedException ue)
            {
                TShock.Log.ConsoleError("[TerraDiscord] Um..... You Token seams invalid!");
                TShock.Log.Error("Error trying to connect. Error: " + ue.Message);
                return;
            }
            catch(Exception exe)
            {
                TShock.Log.ConsoleError("[TerraDiscord] I can't connect! THERE IS NO PATH");
                TShock.Log.Error("Error trying to connect. EName: " + exe.ToString() + " Error: " + exe.Message);
                return;
            }

            Commands.ChatCommands.Add(new Command("terradiscord.admin", TDCom, "td", "terradiscord"));
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
                ServerApi.Hooks.ServerChat.Register(this, OnChat); 
        }

        async Task StartBot()
        {
            DC = new DiscordClient(new DiscordConfiguration { Token = Config.current.Token, TokenType = TokenType.Bot, AutoReconnect = true });

            DC.Ready += DReady;
            DC.ClientErrored += DCError;
            DC.MessageCreated += DMessageRecived;

            await DC.ConnectAsync();
        }

        async Task DReady(ReadyEventArgs args)
        {
            chan = await DC.GetChannelAsync(ulong.Parse(Config.current.Channel));
            TShock.Log.ConsoleInfo("[TerraDiscord] Ready to go!");
            await Task.Delay(0);
        }

        async Task DCError(ClientErrorEventArgs args)
        {
            TShock.Log.ConsoleError("[TerraDiscord] Something brok! PLS CONTAC ATHOR");
            TShock.Log.Error("An Error occured! EventName: " + args.EventName + " Error: " + args.Exception.Message);
            await Task.Delay(0);
        }

        async Task DMessageRecived(MessageCreateEventArgs args)
        {
            if (args.Author == DC.CurrentUser)
                return;
            if (args.Channel.Id != ulong.Parse(Config.current.Channel))
                return;
            if (args.Author.IsBot)
                return;

            if((Config.current.Whitelist == Whitelist.Discord || Config.current.Whitelist == Whitelist.Both) && !CheckIfHasRole(args.Author, args.Guild, Config.current.PMRole))
            {
                await args.Message.DeleteAsync(args.Author.Username + " is muted!");
                return;
            }
            else if ((Config.current.Whitelist == Whitelist.None || Config.current.Whitelist == Whitelist.Terraria) && CheckIfHasRole(args.Author, args.Guild, Config.current.PMRole))
            {
                await args.Message.DeleteAsync(args.Author.Username + " is muted!");
                return;
            }

            if (args.Message.Content.StartsWith(Config.current.DCSpecifier + "mute "))
            {
                #region Discord Mute Command
                if (!CheckIfHasRole(args.Author, args.Guild, Config.current.ARole) || !((DiscordMember)args.Author).IsOwner)
                {
                    await DC.SendMessageAsync(args.Channel, args.Author + ", you don't seemes to have the right role to use this command");
                    return;
                }

                string[] paras = args.Message.Content.Replace(Config.current.DCSpecifier + "mute ", "").Split(' ');
                if (paras.Length < 2)
                {
                    await DC.SendMessageAsync(args.Channel, "Invalid paramiters!");
                    await DC.SendMessageAsync(args.Channel, "Usage: " + Config.current.DCSpecifier + "mute <terraria:discord> <username> [<username> ...]");
                    await DC.SendMessageAsync(args.Channel, "Don't use @username");
                    return;
                }

                if (paras.Length == 2)
                {
                    if (paras[0].ToLower() == "terraria")
                    {
                        List<TSPlayer> p = TShock.Utils.FindPlayer(paras[1]);
                        if (p.Count > 1)
                        {
                            await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[1] + "', to many matches!");
                            return;
                        }
                        else if (p.Count == 0)
                        {
                            await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[1] + "', no matches!");
                            return;
                        }

                        if (TMute.Contains(p[0]) || SQLTDMT.Contains(p[0].User.Name))
                        {
                            TMute.Remove(p[0]);
                            RemoveFromDB(p[0].User.Name, "terraria");
                            p[0].SendInfoMessage("You have been unmuted form Discord chat!");
                            await DC.SendMessageAsync(args.Channel, "Unmuted " + p[0].User.Name);
                        }
                        else
                        {
                            TMute.Add(p[0]);
                            AddToDB(p[0].User.Name, "terraria");
                            p[0].SendInfoMessage("You have been muted on Discord chat!");
                            await DC.SendMessageAsync(args.Channel, "Muted " + p[0].User.Name);
                        }
                    }
                    else if (paras[0].ToLower() == "discord")
                    {
                        DiscordUser user = null;
                        foreach (DiscordMember mem in args.Guild.Members)
                            if (mem.Username == paras[1])
                                user = mem;

                        if (user == null)
                        {
                            await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[1] + "'");
                            return;
                        }

                        if (DMute.Contains(user) || SQLTDMD.Contains(user.Username))
                        {
                            DMute.Remove(user);
                            RemoveFromDB(user.Username, "discord");
                            await DC.SendMessageAsync(args.Channel, "Unmuted " + user.Username);
                        }
                        else
                        {
                            DMute.Add(user);
                            AddToDB(user.Username, "discord");
                            await DC.SendMessageAsync(args.Channel, "Muted " + user.Username);
                        }
                    }
                }
                else
                {
                    if (paras[0].ToLower() == "terraria")
                    {
                        List<TSPlayer> playerToMute = new List<TSPlayer>();
                        for (int i = 1; i < paras.Length; i++)
                        {
                            List<TSPlayer> p = TShock.Utils.FindPlayer(paras[i]);
                            if (p.Count > 1)
                            {
                                await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[1] + "', to many matches!");
                                return;
                            }
                            else if (p.Count == 0)
                            {
                                await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[1] + "', no matches!");
                                return;
                            }
                            playerToMute.Add(p[0]);
                        }

                        string mMessage = "Muted: ";
                        string dMessage = "Unmuted: ";
                        foreach (TSPlayer play in playerToMute)
                        {
                            if (TMute.Contains(play) || SQLTDMT.Contains(play.User.Name))
                            {
                                TMute.Remove(play);
                                RemoveFromDB(play.User.Name, "terraria");
                                play.SendInfoMessage("You have been unmuted form Discord chat!");
                                dMessage += play.User.Name + ", ";
                            }
                            else
                            {
                                TMute.Add(play);
                                AddToDB(play.User.Name, "terraria");
                                play.SendInfoMessage("You have been muted on Discord chat!");
                                mMessage += play.User.Name + ", ";
                            }
                        }
                        if (dMessage.Length != 9)
                            await DC.SendMessageAsync(args.Channel, dMessage.Remove(dMessage.Length - 2));
                        if (mMessage.Length != 7)
                            await DC.SendMessageAsync(args.Channel, mMessage.Remove(mMessage.Length - 2));
                    }
                    else if (paras[0].ToLower() == "discord")
                    {
                        List<DiscordUser> userToMute = new List<DiscordUser>();
                        for (int i = 1; i < paras.Length; i++)
                        {
                            bool found = false;
                            foreach (DiscordMember mem in args.Guild.Members)
                                if (mem.Username == paras[i])
                                {
                                    userToMute.Add(mem);
                                    found = true;
                                }
                            if (!found)
                            {
                                await DC.SendMessageAsync(args.Channel, "Couldn't find '" + paras[i] + "'!");
                                return;
                            }
                        }

                        string mMessage = "Muted: ";
                        string dMessage = "Unmuted: ";
                        foreach (DiscordUser user in userToMute)
                        {
                            if (DMute.Contains(user) || SQLTDMD.Contains(user.Username))
                            {
                                DMute.Remove(user);
                                RemoveFromDB(user.Username, "discord");
                                dMessage += user.Username + ", ";
                            }
                            else
                            {
                                DMute.Add(user);
                                AddToDB(user.Username, "discord");
                                mMessage += user.Username + ", ";
                            }
                        }
                        if (dMessage.Length != 9)
                            await DC.SendMessageAsync(args.Channel, dMessage.Remove(dMessage.Length - 2));
                        if (mMessage.Length != 7)
                            await DC.SendMessageAsync(args.Channel, mMessage.Remove(mMessage.Length - 2));
                    }
                }
                #endregion
            }
            else if (args.Message.Content == Config.current.DCSpecifier + "mute")
            {
                await DC.SendMessageAsync(args.Channel, "Usage: " + Config.current.DCSpecifier + "mute < terraria:discord> <username> [<username> ...]");
                await DC.SendMessageAsync(args.Channel, "Don't use @username");
                return;
            }
            else if(args.Message.Content == Config.current.DCSpecifier + "muted")
            {
                string message = "";
                message += "Terraria players:\n";
                foreach (TSPlayer p in TMute)
                    message += "- " + p.User.Name + "\n";
                message += "Discord users:\n";
                foreach (DiscordUser d in DMute)
                    message += "- " + d.Username + "\n";
                await DC.SendMessageAsync(args.Channel, message);
                return;
            }
            else if (DMute.Contains(args.Author))
            {
                await args.Message.DeleteAsync(args.Author.Username + " is muted!");
                return;
            }
            else if(SQLTDMD.Contains(args.Author.Username))
            {
                await args.Message.DeleteAsync(args.Author.Username + " is muted!");
                return;
            }
            else if (Config.current.TCID && args.Message.Content.StartsWith(Config.current.DCSpecifier + "command "))
            {
                #region Discord Terraria Commands In Discord Command
                if (!CheckIfHasRole(args.Author, args.Guild, Config.current.TCIDRole))
                {
                    await DC.SendMessageAsync(args.Channel, args.Author + ", you don't seemes to have the right role to use this command");
                    return;
                }

                if (Config.current.TCIDFilter)
                {
                    if (Config.current.TCIDBlackList.Contains(args.Message.Content.Replace(Config.current.DCSpecifier + "command ", "").Split(' ')[0]))
                    {
                        await DC.SendMessageAsync(args.Channel, "Master " + args.Author.Username + ", you wanted to use a forbidden command! I'm sorry but, I can not allow this!!");
                        return;
                    }
                }
                else if (Config.current.TCIDBlackList.Contains(args.Message.Content.Replace(Config.current.DCSpecifier + "command ", "").Split(' ')[0]))
                {
                    await Task.Delay(0);
                    string forbiddenCommand = args.Message.Content.Replace(Config.current.DCSpecifier + "command ", "");
                    if (Commands.HandleCommand(TSPlayer.Server, TShock.Config.CommandSpecifier + forbiddenCommand))
                        await DC.SendMessageAsync(args.Channel, "Master " + args.Author.Username + ", you've used one of forbidden command, and it has succesfully executed. I think you know you are doing...");
                    else
                        await DC.SendMessageAsync(args.Channel, "Master " + args.Author.Username + ", the forbidden command has failed to execute!! Please check if server is ok!");
                    return;
                }

                await Task.Delay(0);
                string command = args.Message.Content.Replace(Config.current.DCSpecifier + "command ", "");
                if (Commands.HandleCommand(TSPlayer.Server, TShock.Config.CommandSpecifier + command))
                    await DC.SendMessageAsync(args.Channel, "Master " + args.Author.Username + ", the command was succesfully executed!");
                else
                    await DC.SendMessageAsync(args.Channel, "Master " + args.Author.Username + ", I'm very sorry to say this but your command failed to execute!");
                #endregion
            }
            else if (args.Message.Content == Config.current.DCSpecifier + "command")
                return;
            
            else
            {
                await Task.Delay(0);
                string formattedMessage = Config.current.TFormat;
                if (((DiscordMember)args.Author).Nickname == "" || ((DiscordMember)args.Author).Nickname == null)
                    formattedMessage = formattedMessage.Replace("{nick}", args.Author.Username);
                else
                    formattedMessage = formattedMessage.Replace("{nick}", ((DiscordMember)args.Author).Nickname);
                formattedMessage = formattedMessage.Replace("{role}", ((DiscordMember)args.Author).Roles.First().Name).Replace("{message}", args.Message.Content).Replace("{user}", args.Author.Username);
                TShock.Utils.Broadcast(formattedMessage, Config.current.ChatColor);
            }
        }

        bool CheckIfHasRole(DiscordUser user, DiscordGuild guild, string roleName)
        {
            bool have = false;
            guild.Roles.ForEach((r) => { if (r.Name == roleName) ((DiscordMember)user).Roles.ForEach((R) => { if (R.Name == r.Name) have = true; }); });
            return have;
        }

        async void OnChat(ServerChatEventArgs args)
        {
            if (args.Text.StartsWith(TShock.Config.CommandSpecifier))
                return;
            if ((Config.current.Whitelist == Whitelist.Terraria || Config.current.Whitelist == Whitelist.Both) && !SQLTDW.Contains(TShock.Players[args.Who].User.Name))
                return;
            if (TShock.Players[args.Who].HasPermission("terradiscord.permmute") && !TShock.Players[args.Who].HasPermission("*"))
                return;
            if (TMute.Contains(TShock.Players[args.Who]))
                return;
            if (SQLTDMT.Contains(TShock.Players[args.Who].User.Name))
                return;
            TShock.Log.Info("Trying to send message");
            await FormatAndSend(chan, args.Text, TShock.Players[args.Who]);
        }

        async Task<DiscordMessage> FormatAndSend(DiscordChannel chan, string message, TSPlayer who)
        {
            string formattedMessage = Config.current.DFormat;

            try
            {
                if (formattedMessage.Contains("{nick}"))
                    formattedMessage = formattedMessage.Replace("{nick}", who.Name);
                if (formattedMessage.Contains("{message}"))
                    formattedMessage = formattedMessage.Replace("{message}", message);

                Group g = TShock.Groups.groups.Where(a => a.Name == who.User.Group).First();

                if (formattedMessage.Contains("{prefix}"))
                    formattedMessage = formattedMessage.Replace("{prefix}", g.Prefix);
                if (formattedMessage.Contains("{suffix}"))
                    formattedMessage = formattedMessage.Replace("{suffix}", g.Suffix);
                if (formattedMessage.Contains("{group}"))
                    formattedMessage = formattedMessage.Replace("{group}", g.Name);
            }
            catch(Exception exe)
            {
                TShock.Log.ConsoleError("[TerraDiscord] Error while formatting message!");
                TShock.Log.Error("Error while 'FormatAndSend'! Error: " + exe.Message);
            }
            return await Send(chan, formattedMessage);
        }

        async Task<DiscordMessage> Send(DiscordChannel chan, string message)
        {
            if (message == Config.current.DFormat)
                return null;
            try
            {
                return await DC.SendMessageAsync(chan, message);
            }
            catch(Exception exe)
            {
                TShock.Log.ConsoleError("[TerraDiscord] Couldn't send message to Discord!");
                TShock.Log.Error("Error while trying to send a message to Discord! Error: " + exe.Message);
                await Task.Delay(500);
                try
                {
                    return await DC.SendMessageAsync(chan, message);
                }
                catch(Exception exee)
                {
                    if(exe == exee)
                    {
                        TShock.Log.ConsoleError("[TerraDiscord] Still coludn't send message to Discord!");
                    }
                    else
                    {
                        TShock.Log.ConsoleError("[TerraDiscord] Still coludn't send message to Discord!");
                        TShock.Log.Error("Error while trying to send a message to Discord! Error: " + exee.Message);
                    }
                }
            }

            return null;
        }

        void TDCom(CommandArgs args)
        {
            if(args.Parameters.Count == 0)
            {
                args.Player.SendSuccessMessage("TerraDiscord v" + Version.ToString());
                args.Player.SendInfoMessage("Commands:");
                args.Player.SendInfoMessage("- td realod - To reload the plugin");
                args.Player.SendInfoMessage("- td mute - To mute/unmute plyers form discord chat");
                args.Player.SendInfoMessage("- td muted - Shows muted player and discord user list");
            }
            else
            {
                if(args.Parameters[0].ToLower() == "reload")
                {
                    DC.DisconnectAsync();
                    DC = null;
                    chan = null;
                    Config.Init();
                    if (Config.current.Token == "Please fill me senpai!!~" || Config.current.Channel == "Here tooooo!!!~")
                    {
                        args.Player.SendErrorMessage("Wrong config!");
                        return;
                    }
                    else
                    {
                        try { ulong chann = ulong.Parse(Config.current.Channel); }
                        catch(Exception e)
                        {
                            TShock.Log.ConsoleError("[TerraDiscord] Oh no!!!! Config is broken!! GO FIX THAT FAST!!");
                            TShock.Log.ConsoleError("[TerraDiscord] Hint: Your Channel ID is invalid!");
                            TShock.Log.Error("An error occured! Error: " + e.Message);
                            args.Player.SendErrorMessage("Wrong Channel ID!");
                            return;
                        }

                        if (Config.current.TCID)
                        {
                            TShock.Log.ConsoleInfo("[TerraDiscord] The 'TerrariaCommandsInDiscord' feature is enabled! Beware who you give the TCIDRole!");
                            if (!Config.current.TCIDFilter)
                                TShock.Log.ConsoleInfo("[TerraDiscord] Command filter is disabled!! Plugin author is not responsible for any damage to the server!");
                            else
                                TShock.Log.ConsoleInfo("[TerraDiscord] Plugin author is not responsible for any damage to the server, if Command Filter is set badly, bad things my occur in wrong hands!");
                            TShock.Log.ConsoleInfo("[TerraDiscord] This is an warning! You use TCID on YOUR OWN RISK!");
                        }
                    }
                    StartBot().GetAwaiter().GetResult();
                    args.Player.SendInfoMessage("Succesfully reloaded!");
                }
                else if(args.Parameters[0].ToLower() == "mute")
                {
                    #region Terraria Mute Command
                    if (args.Parameters.Count == 1)
                        args.Player.SendInfoMessage("Usage: /td mute <terraria:discord> <username> [<username> ...]"); 
                    else if(args.Parameters.Count == 2)
                    {
                        if(args.Parameters[1].ToLower() == "terraria" || args.Parameters[1].ToLower() == "discord")
                            args.Player.SendErrorMessage("No username!");
                        else
                            args.Player.SendErrorMessage("Invalid side!");
                        args.Player.SendErrorMessage("Usage: /td mute <terraria:discord> <username> [<username> ...]");
                    }
                    else
                    {
                        if (args.Parameters[1].ToLower() == "terraria")
                        {
                            List<TSPlayer> playerToMute = new List<TSPlayer>();
                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                List<TSPlayer> p = TShock.Utils.FindPlayer(args.Parameters[i]);
                                if (p.Count > 1)
                                {
                                    args.Player.SendErrorMessage("Couldn't find player '" + args.Parameters[i] + "', to many matches!");
                                    return;
                                }
                                else if (p.Count == 0)
                                {
                                    args.Player.SendErrorMessage("Couldn't find player '" + args.Parameters[i] + "', no matches!");
                                    return;
                                }
                                playerToMute.AddRange(p);
                            }

                            if (playerToMute.Count > 1)
                            {
                                string mMessage = "Muted: ";
                                string dMessage = "Unmuted: ";
                                foreach (TSPlayer play in playerToMute)
                                {
                                    if (TMute.Contains(play) || SQLTDMT.Contains(play.User.Name))
                                    {
                                        TMute.Remove(play);
                                        RemoveFromDB(play.User.Name, "terraria");
                                        play.SendInfoMessage("You have been unmuted form Discord chat!");
                                        dMessage += play.User.Name + ", ";
                                    }
                                    else
                                    {
                                        TMute.Add(play);
                                        AddToDB(play.User.Name, "terraria");
                                        play.SendInfoMessage("You have been muted on Discord chat!");
                                        mMessage += play.User.Name + ", ";
                                    }
                                }
                                if (dMessage.Length != 9)
                                    args.Player.SendInfoMessage(dMessage.Remove(dMessage.Length - 2));
                                if (mMessage.Length != 7)
                                    args.Player.SendInfoMessage(mMessage.Remove(mMessage.Length - 2));
                            }
                            else
                            {
                                if (TMute.Contains(playerToMute[0]) || SQLTDMT.Contains(playerToMute[0].User.Name))
                                {
                                    TMute.Remove(playerToMute[0]);
                                    RemoveFromDB(playerToMute[0].User.Name, "terraria");
                                    playerToMute[0].SendInfoMessage("You have been unmuted form Discord chat!");
                                    args.Player.SendInfoMessage("Unmuted " + playerToMute[0].User.Name);
                                }
                                else
                                {
                                    TMute.Add(playerToMute[0]);
                                    AddToDB(playerToMute[0].User.Name, "terraria");
                                    playerToMute[0].SendInfoMessage("You have been muted on Discord chat!");
                                    args.Player.SendInfoMessage("Muted " + playerToMute[0].User.Name);
                                }
                            }
                        }
                        else if (args.Parameters[1].ToLower() == "discord")
                        {
                            List<DiscordUser> userToMute = new List<DiscordUser>();
                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                bool found = false;
                                try
                                {
                                    DC.Guilds.ForEach(a => { userToMute.Add(a.Value.Members.Where(b => b.Username == args.Parameters[i]).First()); found = true; });
                                }
                                catch(InvalidOperationException exe)
                                {
                                    TShock.Log.Error("Error while searching for Discord User. " + exe.HResult);
                                    found = false;
                                }
                                if(!found)
                                {
                                    args.Player.SendErrorMessage("Couldn't find discord user '" + args.Parameters[i] + "'!");
                                    return;
                                }
                            }
                            
                            if (userToMute.Count > 1)
                            {
                                string mMessage = "Muted: ";
                                string dMessage = "Unmuted: ";
                                foreach (DiscordUser user in userToMute)
                                {
                                    if (DMute.Contains(user) || SQLTDMD.Contains(user.Username))
                                    {
                                        DMute.Remove(user);
                                        RemoveFromDB(user.Username, "discord");
                                        Send(chan, user.Username + " has been unmuted by " + args.Player.User.Name + "!").GetAwaiter();
                                        dMessage += user.Username + ", ";
                                    }
                                    else
                                    {
                                        DMute.Add(user);
                                        AddToDB(user.Username, "discord");
                                        Send(chan, user.Username + " has been muted by " + args.Player.User.Name + "!").GetAwaiter();
                                        mMessage += user.Username + ", ";
                                    }
                                }
                                if (dMessage.Length != 9)
                                    args.Player.SendInfoMessage(dMessage.Remove(dMessage.Length - 2));
                                if (mMessage.Length != 7)
                                    args.Player.SendInfoMessage(mMessage.Remove(mMessage.Length - 2));
                            }
                            else
                            {
                                if (DMute.Contains(userToMute[0]) || SQLTDMD.Contains(userToMute[0].Username))
                                {
                                    DMute.Remove(userToMute[0]);
                                    RemoveFromDB(userToMute[0].Username, "discord");
                                    Send(chan, userToMute[0].Username + " has been unmuted by " + args.Player.User.Name + "!").GetAwaiter();
                                    args.Player.SendInfoMessage("Unmuted " + userToMute[0].Username);
                                }
                                else
                                {
                                    DMute.Add(userToMute[0]);
                                    AddToDB(userToMute[0].Username, "discord");
                                    Send(chan, userToMute[0].Username + " has been muted by " + args.Player.User.Name + "!").GetAwaiter();
                                    args.Player.SendInfoMessage("Muted " + userToMute[0].Username);
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if(args.Parameters[0].ToLower() == "muted")
                {
                    args.Player.SendInfoMessage("Terraria players:");
                    foreach (TSPlayer p in TMute)
                        args.Player.SendInfoMessage("- " + p.User.Name);
                    args.Player.SendInfoMessage("Discord users:");
                    foreach (DiscordUser d in DMute)
                        args.Player.SendInfoMessage("- " + d.Username);
                }
                else if(args.Parameters[0].ToLower() == "whitelist")
                {
                    if(args.Parameters.Count == 1)
                    {
                        args.Player.SendInfoMessage("Usage: td whitelist <username> [<username> ...] - Add or Remove to the whitelist");
                        return;
                    }
                    
                    if(args.Parameters.Count == 2)
                    {
                        List<TSPlayer> p = TShock.Utils.FindPlayer(args.Parameters[1]);
                        if(p.Count == 0)
                        {
                            args.Player.SendErrorMessage("No users found!");
                            return;
                        }
                        else if(p.Count != 1)
                        {
                            args.Player.SendErrorMessage("Too many users found!");
                            return;
                        }

                        if(SQLTDW.Contains(p[0].User.Name))
                        {
                            RemoveFromDB(p[0].User.Name);
                            SQLTDW.Remove(p[0].User.Name);
                            args.Player.SendInfoMessage("Removed form whitelist " + p[0].User.Name);
                        }
                        else
                        {
                            AddToDB(p[0].User.Name);
                            SQLTDW.Add(p[0].User.Name);
                            args.Player.SendInfoMessage("Added to whitelist " + p[0].User.Name);
                        }
                    }
                    else
                    {
                        List<TSPlayer> toaor = new List<TSPlayer>();
                        for(int i = 1; i < args.Parameters.Count; i++)
                        {
                            List<TSPlayer> p = TShock.Utils.FindPlayer(args.Parameters[i]);
                            if (p.Count == 0)
                            {
                                args.Player.SendErrorMessage("No users found for " + args.Parameters[i] + "!");
                                return;
                            }
                            else if (p.Count != 1)
                            {
                                args.Player.SendErrorMessage("Too many users found for " + args.Parameters[i] + "!");
                                return;
                            }
                            toaor.Add(p[0]);
                        }

                        string add = "Added: ", rm = "Removed: ";
                        foreach(TSPlayer pl in toaor)
                        {
                            if (SQLTDW.Contains(pl.User.Name))
                            {
                                RemoveFromDB(pl.User.Name);
                                SQLTDW.Remove(pl.User.Name);
                                rm += pl.User.Name + ", ";
                            }
                            else
                            {
                                AddToDB(pl.User.Name);
                                SQLTDW.Add(pl.User.Name);
                                add += pl.User.Name + ", ";
                            }
                        }
                        if (add.Length > 7)
                            args.Player.SendInfoMessage(add.Remove(add.Length - 2));
                        if (rm.Length > 9)
                            args.Player.SendInfoMessage(rm.Remove(rm.Length - 2));
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid command");
                }
            }
        }

        void DBInit()
        {
            bool TDM = false, TDW = false;
            try
            {
                TShock.DB.Query("CREATE TABLE TDMuted(name TEXT, type TEXT)");
            }
            catch(Exception exe)
            {
                if(exe.HResult != -2147467259)
                    throw new Exception("SQL ERROR: " + exe.HResult);
                TDM = true;
            }

            try
            {
                TShock.DB.Query("CREATE TABLE TDWhitelist(name TEXT)");
            }
            catch(Exception exe)
            {
                if(exe.HResult != -2147467259)
                    throw new Exception("SQL ERROR: " + exe.ToString());
                TDW = true;
            }

            if(TDM)
            {
                QueryResult QR = TShock.DB.QueryReader("SELECT * FROM TDMuted");
                while(QR.Read())
                {
                    if (QR.Get<string>("type") == "discord")
                        SQLTDMD.Add(QR.Get<string>("name"));
                    else if (QR.Get<string>("type") == "terraria")
                        SQLTDMT.Add(QR.Get<string>("name"));
                }
            }
            if(TDW)
            {
                QueryResult QR = TShock.DB.QueryReader("SELECT * FROM TDWhitelist");
                while (QR.Read())
                    SQLTDW.Add(QR.Get<string>("name"));
            }
        }

        void AddToDB(string name, string type)
        {
            if (TShock.DB.Query("INSERT INTO TDMuted(name, type) VALUES ('" + name + "', '" + type + "')") != 1)
                TShock.Log.Error("SQL Error while inserting to TDMuted");
            else
            {
                if (type == "discord")
                    SQLTDMD.Add(name);
                else if (type == "terraria")
                    SQLTDMT.Add(name);
            }
        }

        void AddToDB(string name)
        {
            if (TShock.DB.Query("INSERT INTO TDWhitelist(name) VALUES ('" + name + "')") != 1)
                TShock.Log.Error("SQL Error while inserting to TDWhitelist");
            else
                SQLTDW.Add(name);
        }

        void RemoveFromDB(string name)
        {
            if(TShock.DB.Query("DELETE FROM TDWhitelist WHERE name='" + name + "'") != 1)
                TShock.Log.Error("SQL Error while deleting to TDWhitelist");
            else
                    SQLTDW.Remove(name);
        }

        void RemoveFromDB(string name, string type)
        {
            if (TShock.DB.Query("DELETE FROM TDMuted WHERE name='" + name + "'") != 1)
                TShock.Log.Error("SQL Error while deleting to TDMuted");
            else
            {
                if (type == "discord")
                    SQLTDMD.Remove(name);
                else if (type == "terraria")
                    SQLTDMT.Remove(name);
            }
        }
    }
}
