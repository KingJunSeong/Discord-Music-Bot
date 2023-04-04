using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSharpBot.Commands
{
    public class PrefixModule : BaseCommandModule
    {
        [Command("ping")]
        public async Task GreetCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("pong!");
        }
    }
}
