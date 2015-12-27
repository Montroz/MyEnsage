using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenziedUrsa {
    class Program
    {
        private static readonly Menu Menu = new Menu("UrsaRage", "ursaRage", true, "npc_dota_hero_ursa", true);

        private const int blinkRadius = 1150;
        private const int mouseToTargetRadius = 300;
        private const int earthShockRadius = 350;

        static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Console.WriteLine("Frenzied Ursa loaded!");
            Menu.AddItem(new MenuItem("comboKey", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Menu.AddToMainMenu();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            Hero me = ObjectMgr.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Ursa)
            {
                return;
            }
                      
            Ability earthshock = me.Spellbook.Spell1;
            Ability overpower = me.Spellbook.SpellW;
            Ability enrage = me.FindSpell("ursa_enrage");

            Item phase = me.FindItem("item_phase_boots");
            Item blink = me.FindItem("item_blink");
            Item abyssal = me.FindItem("item_abyssal_blade");
            

            Hero target = getLowHpTartet(me);
            
            if (target != null && me.Health > 400 && blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink"))
            {
                //autoblink and kill

                if (!useUrsaCombo(me, target, overpower, enrage,
                        blink, earthshock, phase, null))
                {
                    return;
                }
                me.Attack(target);
            } else
            {

                //try to find selected by mouse enemy
                target = me.ClosestToMouseTarget(mouseToTargetRadius);

                bool isPressed = Menu.Item("comboKey").GetValue<KeyBind>().Active;
                if (isPressed && target != null)
                {
                    if (!(blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink")))
                    {
                        //no blink
                        if (calculateDistance(me, target) <= 500)
                        {
                            //come to target and use combo
                            if (!useUrsaCombo(me, target, overpower, enrage,
                                        null, earthshock, phase, abyssal))
                            {
                                return;
                            }
                        }
                        me.Attack(target);
                    } else
                    {
                        //prepare for blink

                        if (calculateDistance(me, target) <= blinkRadius)
                        {
                            //use blink
                            if (!useUrsaCombo(me, target, overpower, enrage,
                                        blink, earthshock, phase, abyssal))
                            {
                                return;
                            }
                            me.Attack(target);
                        }
                    }

                }
            }
        }

        private static bool useUrsaCombo(Hero me, Hero target, Ability overpower, Ability enrage,
            Item blink, Ability earthshock, Item phase, Item abyssal)
        {
            if (!useAbility(overpower, "W"))
            {
                return false;
            }

            if (!useAbility(enrage, "R"))
            {
                return false;
            }

            if (!useAbility(blink, "blink", target.Position))
            {
                return false;
            }

            if (calculateDistance(me, target) <= earthShockRadius)
            {
                if (!useAbility(earthshock, "Q"))
                {
                    return false;
                }
            }

            if (!useAbility(phase, "phase"))
            {
                return false;
            }

            if (!useAbility(abyssal, "abyssal", target))
            {
                return false;
            }

            return true;
        }

        private static bool useAbility(Ability ability, string codeWord, Hero target)
        {
            if (ability == null)
            {
                return true;
            }
            if (ability.CanBeCasted() && Utils.SleepCheck(codeWord))
            {
                ability.UseAbility(target);
                Utils.Sleep(1500, codeWord);
            }
            return ability.CanBeCasted() ? false : true;
        }

        private static bool useAbility(Ability ability, string codeWord, SharpDX.Vector3 position)
        {
            if (ability == null)
            {
                return true;
            }
            if (ability.CanBeCasted() && Utils.SleepCheck(codeWord))
            {
                ability.UseAbility(position);
                Utils.Sleep(1500, codeWord);
            }
            return ability.CanBeCasted() ? false : true;
        }

        private static bool useAbility(Ability ability, string codeWord)
        {
            if (ability == null)
            {
                return true;
            }
            if (ability.CanBeCasted() && Utils.SleepCheck(codeWord))
            {
                ability.UseAbility();
                Utils.Sleep(1500, codeWord);
            }
            return ability.CanBeCasted() ? false : true;
        }  
        
        private static float calculateDistance(Hero me, Hero target)
        {
            var pos = target.Position
                    + target.Vector3FromPolarAngle() * ((Game.Ping / 1000 + 0.3f) * target.MovementSpeed);
            var mePosition = me.Position;
            return mePosition.Distance2D(pos);
        } 
        
        private static Hero getLowHpTartet(Hero me)
        {
            Hero target = null;
            var enemies = ObjectMgr.GetEntities<Hero>()
                    .Where(x => x.IsVisible && x.IsAlive && !x.IsIllusion && x.Team != me.Team && x.Health <= 500).ToList();

            float minDistance = 0;
            foreach (var hero in enemies)
            {
                var distance = me.Distance2D(hero);
                if (distance < 1150 && minDistance < distance)
                {
                    minDistance = distance;
                    target = hero;
                }
            }

            return target;
        } 
        
                
    }
}
