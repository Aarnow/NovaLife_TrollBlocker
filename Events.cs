using Life;
using Life.Network;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrollBlocker.Entities;

namespace TrollBlocker
{
    public class Events : ModKit.Helper.Events
    {
        public Events(IGameAPI api) : base(api)
        {
        }

        public override void OnMinutePassed()
        {
            foreach(var prisoner in TrollBlocker.BlackList)
            {
                if (prisoner.setup.areaId != TrollBlocker.JailConfig.areaId)
                {
                    int randomIndex = TrollBlocker.rand.Next(0, TrollBlocker.Jails.Count);
                    var jail = TrollBlocker.Jails[randomIndex];
                    prisoner.setup.TargetSetPosition(jail.VPosition);
                }
            }           
        }

        public override async void OnPlayerSpawnCharacter(Player player)
        {
            base.OnPlayerSpawnCharacter(player);

            List<TrollBlockerPlayer> prisoners = await TrollBlockerPlayer.Query(p => p.PlayerId == player.account.id && p.IsActive);

            if (prisoners != null && prisoners.Count != 0)
            {
                TrollBlocker.BlackList.Add(player);
                int randomIndex = TrollBlocker.rand.Next(0, TrollBlocker.Jails.Count);
                var jail = TrollBlocker.Jails[randomIndex];
                player.setup.TargetSetPosition(jail.VPosition);
            }
        }

        public override void OnPlayerDisconnect(NetworkConnection conn)
        {
            base.OnPlayerDisconnect(conn);
            var playerToRemove = TrollBlocker.BlackList.FirstOrDefault(p => p.netId == conn.connectionId);
            if (playerToRemove != null) TrollBlocker.BlackList.Remove(playerToRemove);                  
        }
    }
}
