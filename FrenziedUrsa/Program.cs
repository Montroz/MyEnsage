using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenziedUrsa {
    class Program
    {
        private static readonly Menu Menu = new Menu("FrenziedUrsa", "frenziedUrsa", true, "npc_dota_hero_ursa", true);
        private static Font text;
        private const int blinkRadius = 1150;
        private const int mouseToTargetRadius = 300;
        private const int earthShockRadius = 350;

        private const int sleepTime = 2000; //ms

        static void Main(string[] args)
        {
            Console.WriteLine("Frenzied Ursa loaded!");
            Menu.AddItem(new MenuItem("comboKey", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));

            var hotkey = new MenuItem("bkbKey", "Toggle hotkey for BKB").SetValue(
               new KeyBind('P', KeyBindType.Toggle));
            Menu.AddItem(hotkey);

            Menu.AddItem(new MenuItem("positionX", "X-Position").SetValue(
                    new Slider(75, 0, Drawing.Width)));

            Menu.AddItem(new MenuItem("positionY", "Y-Position").SetValue(
                    new Slider(50, 0, Drawing.Height)));

            Menu.AddToMainMenu();

            text = new Font( Drawing.Direct3DDevice9, new FontDescription
                    {
                        FaceName = "Calibri",
                        Height = 16,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

            if (!Game.IsInGame)
            {
                return;
            }

            Hero me = ObjectMgr.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Ursa)
            {
                return;
            } else { 
                Game.OnUpdate += Game_OnUpdate;
                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
                Drawing.OnEndScene += Drawing_OnEndScene;
            }
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
                return;

            var player = ObjectMgr.LocalPlayer;
            if (player == null || player.Team == Team.Observer)
                return;

            int x = Menu.Item("positionX").GetValue<Slider>().Value, y = Menu.Item("positionY").GetValue<Slider>().Value;
            var keyBind = Menu.Item("bkbKey").GetValue<KeyBind>();

            if (!keyBind.Active)
            {
                text.DrawText(null, "Frenzied Ursa: Bkb WILL NOT be used in combo." +
                        "Press \"" + (char)keyBind.Key + "\" to toogle it.", x, y, Color.White);
            } else
            {
                text.DrawText(null, "Frenzied Ursa: Bkb WILL BE used in combo." +
                        "Press \"" + (char)keyBind.Key + "\" to toogle it.", x, y, Color.White);
            }
            
            

        }

        static void Drawing_OnPostReset(EventArgs args)
        {
            text.OnResetDevice();
        }

        static void Drawing_OnPreReset(EventArgs args)
        {
            text.OnLostDevice();
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
            Item sheepstick = me.FindItem("item_sheepstick");
            Item bkb = me.FindItem("item_black_king_bar");

            Hero target = getLowHpTartet(me);
            
            if (target != null && me.Health > 400 && blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink"))
            {
                //autoblink and kill

                if (!useUrsaCombo(me, target, overpower, enrage,
                        blink, earthshock, phase, null, null, null, false))
                {
                    return;
                }
                me.Attack(target);
            } else
            {

                //try to find selected by mouse enemy
                target = me.ClosestToMouseTarget(mouseToTargetRadius);

                bool isPressed = Menu.Item("comboKey").GetValue<KeyBind>().Active;

                bool isBkbToogled = Menu.Item("bkbKey").GetValue<KeyBind>().Active;

                if (isPressed && target != null)
                {
                    if (!(blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink")))
                    {
                        //no blink
                        if (calculateDistance(me, target) <= 500)
                        {
                            //use combo W/O blink
                            if (!useUrsaCombo(me, target, overpower, enrage,
                                        null, earthshock, phase, abyssal, sheepstick, bkb, isBkbToogled))
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
                                        blink, earthshock, phase, abyssal, sheepstick, bkb, isBkbToogled))
                            {
                                return;
                            }
                            
                        }
                        me.Attack(target);
                    }

                }
            }
        }

        private static bool useUrsaCombo(Hero me, Hero target, Ability overpower, Ability enrage,
            Item blink, Ability earthshock, Item phase, Item abyssal, Item sheepstick, Item bkb, bool isBkbToogled)
        {
            if (!useAbility(overpower, "W", null, false, me))
            {
                return false;
            }

            if (!useAbility(enrage, "R", null, false, me))
            {
                return false;
            }

            if (!useAbility(blink, "blink", target, true, me))
            {
                return false;
            }
            if (isBkbToogled)
            {
                if (!useAbility(bkb, "bkb", null, false, me))
                {
                    return false;
                }
            }

            if (calculateDistance(me, target) <= earthShockRadius)
            {
                if (!useAbility(earthshock, "Q", null, false, me))
                {
                    return false;
                }
            }

            if (!useAbility(phase, "phase", null, false, me))
            {
                return false;
            }

            if (!useAbility(abyssal, "abyssal", target, false, me))
            {
                return false;
            } 

            if (abyssal != null)
            {
                if (!abyssal.CanBeCasted())
                {
                    if ((abyssal.GetCooldown(0) - abyssal.Cooldown) > 1.95)
                    {
                        if (!useAbility(sheepstick, "sheepstick", target, false, me)) { 

                            return false;
                        }
                    }
                }
            } else
            {
                if (!useAbility(sheepstick, "sheepstick", target, false, me))
                {

                    return false;
                }
            }
                
            return true;
        }

        private static bool useAbility(Ability ability, string codeWord, Hero target, bool isPos, Hero me)
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
                } else
                {
                    ability.UseAbility();
                }
                
                Utils.Sleep(ability.GetCastDelay(me, target, true)*1000, codeWord);
                
                if (ability.CanBeCasted())
                {
                    return false;
                }               
            }
            return true;
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
