using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenziedAxe
{
    class Program
    {
        private static readonly Menu Menu = new Menu("FrenziedAxe", "frenziedAxe", true, "npc_dota_hero_axe", true);

        private const int agroDistance = 300;
        private const int blinkRadius = 1180;
        private const double agroDelay = 0.4; 

        static void Main(string[] args)
        {
            Menu.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            Console.WriteLine("Frenzied Axe loaded!");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            Hero me = ObjectMgr.LocalHero;

            if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Axe)
            {
                return;
            }

            Ability agro = me.Spellbook.SpellQ;
            Item bladeMail = me.FindItem("item_blade_mail");
        
            Item blink = me.FindItem("item_blink");
            Ability ult = me.Spellbook.SpellR;

            Hero target = null;

            if (ult != null && ult.Level > 0)
            {
                target = GetLowHpHeroInDistance(me, blinkRadius);

                //check for blink
                if (target != null && me.Health > 400 && blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink"))
                {
                    if (!useAbilityAndGetResult(blink, "blink", target, true, me))
                    {
                        return;
                    }
                }
                
                target = GetLowHpHeroInDistance(me, 300);

                //check for ult
                if (target != null && ult != null && (ult.Level > 0) && ult.CanBeCasted() && Utils.SleepCheck("ult"))
                {
                    if (!useAbilityAndGetResult(ult, "ult", target, false, me))
                    {
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            Hero killedTarget = target;

            target = GetHeroInAgro(me);

            if (target != null && !target.Equals(killedTarget))
            {
                //agro+blade mail combo

                if (!useAbilityAndGetResult(agro, "agro", null, false, me))
                {
                    return;
                }
                

                if (agro != null)
                {
                    if (!agro.CanBeCasted())
                    {
                        if ((agro.CooldownLength - agro.Cooldown) < 3.2)
                        {
                            if (!useAbilityAndGetResult(bladeMail, "bladeMail", null, false, me))
                            {
                                return;
                            }
                        }
                    }
                }
            }

        }

        private static bool useAbilityAndGetResult(Ability ability, string codeWord, Hero target, bool isPos, Hero me)
        {
            if (ability == null)
            {
                return true;
            }

            if (ability.CanBeCasted() && !ability.IsInAbilityPhase && Utils.SleepCheck(codeWord))
            {
                if (target != null)
                {
                    if (isPos)
                    {

                        ability.UseAbility(target.Position);
                    }
                    else
                    {
                        ability.UseAbility(target);
                    }
                }
                else
                {
                    ability.UseAbility();
                }

                Utils.Sleep(ability.GetCastDelay(me, target, true) * 1000, codeWord);

                if (ability.CanBeCasted())
                {
                    return false;
                }
            }
            return true;
        }

        private static Hero GetLowHpHeroInDistance(Hero me, float maxDistance)
        {
            var enemies = ObjectMgr.GetEntities<Hero>()
                    .Where(x => x.IsAlive && !x.IsIllusion && x.Team != me.Team && (getULtDamage(me) > (x.Health - 5)
                    && NotDieFromBladeMail(x, me, getULtDamage(me)))).ToList();

            Hero target = getHeroInDistance(me, enemies, maxDistance);

            return target;
        }

        private static bool NotDieFromBladeMail(Unit enemy, Unit me, double damageDone)
        {
            return !(enemy.Modifiers.FirstOrDefault(modifier => modifier.Name == "modifier_item_blade_mail_reflect") != null 
                && me.Health < damageDone);
        }

        private static int getULtDamage(Hero me)
        {
            Item aghanim = me.FindItem("item_ultimate_scepter");

            int[] ultDamage;
            if (aghanim != null)
            {
                ultDamage = new int[3] { 300, 425, 550 };
            } else
            {
                ultDamage = new int[3] { 250, 325, 400 };
            }
            var ultLevel = me.Spellbook.SpellR.Level;
            var damage = ultDamage[ultLevel - 1];

            return damage;
        }

        private static Hero GetHeroInAgro(Hero me)
        {
            
            var enemies = ObjectMgr.GetEntities<Hero>()
                    .Where(x => x.IsAlive && !x.IsIllusion && x.Team != me.Team).ToList();

            List<Hero> enemiesForAgro = new List<Hero>();

            foreach (var hero in enemies)
            {
                int targetSpeed = hero.MovementSpeed;

                float distanceBefore = calculateDistance(me, hero);

                double distanceAfter = distanceBefore + targetSpeed * (agroDelay - getTimeToTurn(me, hero));

                //Console.WriteLine("distanceBefore" + distanceBefore);
                //Console.WriteLine("getTimeToTurn(me, hero)" + getTimeToTurn(me, hero));
                //Console.WriteLine("distanceAfter" + distanceAfter);
                if (distanceAfter <= agroDistance && hero.IsAlive)
                {
                    enemiesForAgro.Add(hero);
                }
            }

            Hero target = null;
            if (enemiesForAgro.Count > 0)
            {
                target = getHeroInDistance(me, enemiesForAgro, agroDistance);
            }

            return target;
        }

        private static float calculateDistance(Hero me, Hero target)
        {
            var pos = target.Position;
            var mePosition = me.Position;
            return mePosition.Distance2D(pos);
        }

        private static double getTimeToTurn(Hero me, Hero enemy) {
            Vector3 myPos = me.Position;
            Vector3 enemyPos = enemy.Position;

            var difX = myPos.X - enemyPos.X;
            var difY = myPos.Y - enemyPos.Y;
            var degree = Math.Atan2(difY, difX);

            var enemyDirection = Math.Atan2(enemy.Vector2FromPolarAngle().Y, enemy.Vector2FromPolarAngle().X);

            var difDegree = Math.Abs(enemyDirection - degree);
            var turnRate = Game.FindKeyValues(enemy.Name + "/MovementTurnRate", KeyValueSource.Hero).FloatValue;
            var timeToTurn = 0.03 * (Math.PI - difDegree) / turnRate;
            return timeToTurn;
        }

        private static Hero getHeroInDistance(Hero me, List<Hero> enemies, float maxDistance)
        {
            Hero target = null;
            float minDistance = maxDistance;
            foreach (var hero in enemies)
            {
                var distance = me.Distance2D(hero);
                if (distance <= maxDistance && distance <= minDistance)
                {
                    minDistance = distance;
                    target = hero;
                }
            }

            return target;
        }
    }
}
