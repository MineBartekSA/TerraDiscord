using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;
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
            if (CheckIfHasRole(args.Author, args.Guild, Config.current.PMRole))
                return;

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

                        if (TMute.Contains(p[0]))
                        {
                            TMute.Remove(p[0]);
                            p[0].SendInfoMessage("You have been unmuted form Discord chat!");
                            await DC.SendMessageAsync(args.Channel, "Unmuted " + p[0].User.Name);
                        }
                        else
                        {
                            TMute.Add(p[0]);
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

                        if (DMute.Contains(user))
                        {
                            DMute.Remove(user);
                            await DC.SendMessageAsync(args.Channel, "Unmuted " + user.Username);
                        }
                        else
                        {
                            DMute.Add(user);
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
                            if (TMute.Contains(play))
                            {
                                TMute.Remove(play);
                                play.SendInfoMessage("You have been unmuted form Discord chat!");
                                dMessage += play.User.Name + ", ";
                            }
                            else
                            {
                                TMute.Add(play);
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
                            if (DMute.Contains(user))
                            {
                                DMute.Remove(user);
                                dMessage += user.Username + ", ";
                            }
                            else
                            {
                                DMute.Add(user);
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
            if (TShock.Players[args.Who].HasPermission("terradiscord.permmute") && !TShock.Players[args.Who].HasPermission("*"))
                return;
            if (TMute.Contains(TShock.Players[args.Who]))
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
                    formattedMessage = formattedMessage.Replace("{nick}", who.User.Name);
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
                                    if (TMute.Contains(play))
                                    {
                                        TMute.Remove(play);
                                        play.SendInfoMessage("You have been unmuted form Discord chat!");
                                        dMessage += play.User.Name + ", ";
                                    }
                                    else
                                    {
                                        TMute.Add(play);
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
                                if (TMute.Contains(playerToMute[0]))
                                {
                                    TMute.Remove(playerToMute[0]);
                                    playerToMute[0].SendInfoMessage("You have been unmuted form Discord chat!");
                                    args.Player.SendInfoMessage("Unmuted " + playerToMute[0].User.Name);
                                }
                                else
                                {
                                    TMute.Add(playerToMute[0]);
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
                                DC.Guilds.ForEach(a => { userToMute.Add(a.Value.Members.Where(b => b.Username == args.Parameters[i]).First()); found = true; });
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
                                    if (DMute.Contains(user))
                                    {
                                        DMute.Remove(user);
                                        Send(chan, user.Username + " has been unmuted by " + args.Player.User.Name + "!").GetAwaiter();
                                        dMessage += user.Username + ", ";
                                    }
                                    else
                                    {
                                        DMute.Add(user);
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
                                if (DMute.Contains(userToMute[0]))
                                {
                                    DMute.Remove(userToMute[0]);
                                    Send(chan, userToMute[0].Username + " has been unmuted by " + args.Player.User.Name + "!").GetAwaiter();
                                    args.Player.SendInfoMessage("Unmuted " + userToMute[0].Username);
                                }
                                else
                                {
                                    DMute.Add(userToMute[0]);
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
                else
                {
                    args.Player.SendErrorMessage("Invalid command");
                }
            }
        }
    }
}
