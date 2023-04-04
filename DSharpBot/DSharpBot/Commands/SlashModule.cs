using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
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
        [SlashCommand("ping", "ping pong!")]
        public async Task PingCommand(InteractionContext ctx)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            embed.WithTitle("pong!");
            embed.WithDescription("ping pong");
            embed.WithColor(DiscordColor.Magenta);

            
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        [SlashCommand("Join", "join your channel")]
        public async Task Join(InteractionContext ctx, [Option("채널", "음성채널")]DiscordChannel channel)
        {
            var lava = ctx.Client.GetLavalink();

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync("Lavalink 연결이 설정되지 않았습니다");
                return; 
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.CreateResponseAsync("올바른 음성 채널이 아닙니다.");
                return;
            }
            await node.ConnectAsync(channel);
            await ctx.CreateResponseAsync($"Joined {channel.Name}!");
        }
        [SlashCommand("Leave", "Leave voice channel")]
        public async Task LeaveCommand(InteractionContext ctx, [Option("채널", "음성채널")]DiscordChannel channel)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync("Lavalink 연결이 설정되지 않았습니다");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.CreateResponseAsync("올바른 음성 채널이 아닙니다.");
                return;
            }

            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync("해당채널에 연결되어 있지 않습니다.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.CreateResponseAsync($"Left {channel.Name}!");
        }
        [SlashCommand("play", "playing the music!")]
        public async Task PlayCommand(InteractionContext ctx, [Option("제목", "노래 제목 혹은 링크를 입력하여주세요")][RemainingText] string search)
        {
            if(ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync("음성채널에 접속하여 주세요!");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if(conn == null)
            {
                await ctx.CreateResponseAsync("저를 음성채널에 접속시켜주세요!");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if(loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync("해당 음악이나 URL을 찾을 수 없습니다!!");
                return;
            }

            var track = loadResult.Tracks.First();

            await conn.PlayAsync(track);
            await ctx.CreateResponseAsync($"{track.Title} 이 노래를 재생할게요!");
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
    }
}
