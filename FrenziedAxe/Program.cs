using Ensage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenziedAxe
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            Hero me = ObjectMgr.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Axe)
            {
                return;
            }
            else {
                Game.OnUpdate += Game_OnUpdate;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            Hero me = ObjectMgr.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Axe)
            {
                return;
            }
        }
    }
}
