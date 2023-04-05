using DSharpBot.Service;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSharpBot.Commands
{
    public class SlashModule : ApplicationCommandModule
    {
        private Queue<LavaSong> q = new Queue<LavaSong>();
        private DiscordChannel lastChannel = null;

        private async Task Conn_PlaybackFinished(LavalinkGuildConnection sender, DSharpPlus.Lavalink.EventArgs.TrackFinishEventArgs e)
        {
            if(q.Count != 0)
            {
                var track = q.Dequeue();
                await sender.PlayAsync(track.lavaTrack);
                DiscordEmbedBuilder embed = new();
                embed.WithTitle("노래를 재생할게!");
                embed.WithDescription($"{Formatter.Bold(Formatter.Sanitize(track.lavaTrack.Title))}");
                embed.WithColor(DiscordColor.Magenta);
                await track.requestChannel.SendMessageAsync(embed: embed);
                lastChannel = track.requestChannel;
            }
        }
        
        [SlashCommand("ping", "ping pong!")]
        public async Task PingCommand(InteractionContext ctx)
        {
            DiscordEmbedBuilder embed = new();

            embed.WithTitle("pong!");
            embed.WithDescription($"{ctx.Client.Ping}ms");
            embed.WithColor(DiscordColor.Magenta);
            
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        [SlashCommand("Leave", "Leave voice channel")]
        public async Task LeaveCommand(InteractionContext ctx)
        {
            var channel = ctx.Member.VoiceState.Channel;

            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed1 = new();

                embed1.WithTitle("Error!");
                embed1.WithDescription("Lavalink 연결이 설정되지 않았습니다");
                embed1.WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
                return;
            }

            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed2 = new();

                embed2.WithTitle("Error!");
                embed2.WithDescription("이미 채널에 제가 존재하지 않아요!");
                embed2.WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed2));
                return;
            }
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            DiscordEmbedBuilder embed = new();

            embed.WithTitle("잘있어...");
            embed.WithDescription("난 채널에서 나가볼게...");
            embed.WithColor(DiscordColor.Magenta);

            await conn.DisconnectAsync();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        [SlashCommand("play", "playing the music!")]
        public async Task PlayCommand(InteractionContext ctx, [Option("제목", "노래 제목 혹은 링크를 입력하여주세요")][RemainingText] string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            LavalinkGuildConnection conn;
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed1 = new();

                embed1.WithTitle("Error!");
                embed1.WithDescription("너 음성채널에 들어가 있지 않은거 같은데?");
                embed1.WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
                return;
               
            }
            conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                conn = await node.ConnectAsync(ctx.Member.VoiceState?.Channel);
            } else conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            conn.PlaybackFinished += Conn_PlaybackFinished;

            var loadResult = await node.Rest.GetTracksAsync(search);
            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed1 = new();

                embed1.WithTitle("Error!");
                embed1.WithDescription("노래를 찾을 수 없어.. 다른 방법으로 다시 검색해줘!");
                embed1.WithColor(DiscordColor.Red);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
                return;
            }
            var track = loadResult.Tracks.First();
            

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed1 = new();
                embed1.WithTitle(track.Title);
                embed1.WithDescription($"노래를 재생할게!");
                embed1.WithUrl(track.Uri);
                embed1.WithColor(DiscordColor.Magenta);

                await conn.PlayAsync(track);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
            }
            else
            {
                LavaSong s = new LavaSong(track, ctx.Channel);
                q.Enqueue(s);
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed1 = new();
                embed1.WithTitle("노래를 대기열에 추가할게!");
                embed1.WithDescription($"{Formatter.Bold(Formatter.Sanitize(track.Title))}");
                embed1.WithColor(DiscordColor.Magenta);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
            }
        }
        [SlashCommand("pause", "pause the music")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync("음성채널에 접속하여 주세요!");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync("음성채널에 연결되어 있지 않아요!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync("노래를 재생하고있지 않습니다!");
                return;
            }

            await conn.PauseAsync();
        }
        [SlashCommand("Skip", "노래를 넘깁니다.")]
        public async Task SkipCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.Guild);

            if( conn != null)
            {
                try
                {
                    await conn.SeekAsync(conn.CurrentState.CurrentTrack.Length);
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    DiscordEmbedBuilder embed = new();
                    embed.WithTitle("노래를 스킵할게!");
                    embed.WithDescription($"뿅!");
                    embed.WithColor(DiscordColor.Magenta);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                } 
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    DiscordEmbedBuilder embed = new();
                    embed.WithTitle("스킵을 못했어..");
                    embed.WithDescription($"재생중이 아닌거 같은데??");
                    embed.WithColor(DiscordColor.Magenta);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                DiscordEmbedBuilder embed = new();
                embed.WithTitle("스킵을 못했어..");
                embed.WithDescription($"재생중이 아닌거 같은데?");
                embed.WithColor(DiscordColor.Magenta);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
        }
    }
}
