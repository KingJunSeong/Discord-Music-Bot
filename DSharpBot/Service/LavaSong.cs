using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSharpBot.Service
{
    class LavaSong
    {
        public LavaSong(LavalinkTrack track, DiscordChannel requestChannel)
        {
            this.lavaTrack = track;
            this.requestChannel = requestChannel;
        }

        public LavalinkTrack lavaTrack;
        public DiscordChannel requestChannel;
    }
}
