using Life;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrollBlocker
{
    public class Events : ModKit.Helper.Events
    {
        public Events(IGameAPI api) : base(api)
        {
        }

        public async override void OnMinutePassed()
        {
            //vérifier si le joueur est dans la liste noir
            //si oui, vérifier s'il est toujours présent dans sa cellule
            //s'il n'est pas dans sa cellule, le re-téléporter + strike
            //si la personne récidive, bannir perm le joueur
        }
    }
}
