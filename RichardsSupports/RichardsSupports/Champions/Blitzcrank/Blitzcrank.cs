namespace RichardsSupports
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;

    using Abs;
    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Spell = Aimtec.SDK.Spell;
    using Aimtec.SDK.Prediction.Skillshots;

     class Blitzcrank : Spells
    {

        public static Menu Menu = new Menu("R_Support", "R_" + Player.ChampionName, true);
        public static Orbwalker Orbwalker = new Orbwalker();
        public static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        public static Spell Flash;
        public static Spell Ignite;


        private MenuKeyBind FhKey { get; set; }
        public Blitzcrank()
        {
            Q = new Spell(SpellSlot.Q, 1425);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 150);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.Line, false, HitChance.High);
            R.SetSkillshot(0.25f, 600f, float.MaxValue, false, SkillshotType.Circle);


            Orbwalker.Attach(Menu);
            //Combo Menu
            var ComboMenu = new Menu("combo", "Combo");
            {
                ComboMenu.Add(new MenuBool("useq", "Use Q"));
                ComboMenu.Add(new MenuBool("usee", "Use E "));
                ComboMenu.Add(new MenuBool("user", "Use R"));
                ComboMenu.Add(new MenuList("rAOE", "Min Enimies for R", new string[] { "1", "2", "3", "4", "5" }, 1));
                ComboMenu.Add(new MenuBool("userKS", "Use R to Killsteal"));
                ComboMenu.Add(new MenuBool("userIKS", "Use Ignite to Killsteal"));


                ComboMenu.Add(new MenuSeperator("sep1", "Use Q on: "));

                foreach (Obj_AI_Hero enemies in GameObjects.EnemyHeroes)
                ComboMenu.Add(new MenuBool("useqon" + enemies.ChampionName.ToLower(), enemies.ChampionName));
            }


            //Adjust Hitchance Menu
            var HitChanceMenu = new Menu("hc", "HitChance");
            {
                HitChanceMenu.Add(new MenuList("hcq", "Q", new string[] { "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 1));
            }


            //Flash + Hook Menu
            var FlashHookMenu = new Menu("fh", "FlashHook");
            {
                FhKey = new MenuKeyBind("FhKey", "Flash Hook Key", Aimtec.SDK.Util.KeyCode.T, KeybindType.Press);
                FlashHookMenu.Add(FhKey);
                FlashHookMenu.Add(new MenuSeperator("sep1", "Use Q on: "));
                foreach (Obj_AI_Hero enemies in GameObjects.EnemyHeroes)
                FlashHookMenu.Add(new MenuBool("FH" + enemies.ChampionName.ToLower(), enemies.ChampionName));
            }

            //Draw Menu
            var DrawingsMenu = new Menu("draw", "Drawings");
            {
                DrawingsMenu.Add(new MenuBool("dfhr", "Flash + Hook"));
                DrawingsMenu.Add(new MenuBool("dhr", "Hook Range"));
            }

            FhKey.OnValueChanged += FhKey_ValueChanged;
            HitChanceMenu.OnValueChanged += HitChanceMenu_ValueChanged;
            Menu.Add(ComboMenu);
            Menu.Add(HitChanceMenu);
            Menu.Add(FlashHookMenu);
            Menu.Add(DrawingsMenu);
            Menu.Attach();

            GetUnitSummonerSpellFixedName();
            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
            Console.WriteLine("R_" + Player.ChampionName + " loaded.");
        }


        private void Game_OnUpdate()
        {
            if (Player.IsDead || MenuGUI.IsChatOpen())
            {
                return;
            }

            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
            }

            Killsteal();
        }

        private void Render_OnPresent()
        {
            //Basic Q range indicator
            if (Menu["draw"]["dhr"].Enabled)
            {
                Render.Circle(Player.Position, Q.Range - 425, 30, Color.White);
            }
            //Q+Flash indicator
            if (Menu["draw"]["dfhr"].Enabled)
            {
                Render.Circle(Player.Position, Q.Range, 30, Color.Yellow);
            }

        }



        private void FhKey_ValueChanged(MenuComponent sender, ValueChangedArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range);
            if (Flash.Ready && args.GetNewValue<MenuKeyBind>().Value && target != null)
            {
                FHook();
                Console.WriteLine("trigged" + Game.TickCount);
            }
        }

        private void HitChanceMenu_ValueChanged(MenuComponent sender, ValueChangedArgs args)
        {
            if (args.InternalName == "hcq")
            {
                this.Q.HitChance = (HitChance)args.GetNewValue<MenuList>().Value + 3;
            }
        }

        public static SpellSlot[] SummonerSpellSlots =
            {
                SpellSlot.Summoner1,
                SpellSlot.Summoner2

            };

        public static void GetUnitSummonerSpellFixedName()
        {
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SummonerSpellSlots[0]).Name.ToLower() == "summonerflash")
            {
                Flash = new Spell(SpellSlot.Summoner1, 425);
                Console.WriteLine("Flash Detected");
            }
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SummonerSpellSlots[1]).Name.ToLower() == "summonerflash")
            {
                Flash = new Spell(SpellSlot.Summoner2, 425);
                Console.WriteLine("Flash Detected");
            }
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SummonerSpellSlots[0]).Name.ToLower() == "summonerdot")
            {
                Ignite = new Spell(SpellSlot.Summoner1, 600);
                Console.WriteLine("Ignite Detected");
            }
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SummonerSpellSlots[1]).Name.ToLower() == "summonerdot")
            {
               Ignite = new Spell(SpellSlot.Summoner2, 600);
                Console.WriteLine("Ignite Detected");
            }
            return;
        }

        public void FHook()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            if (Menu["fh"]["FH" + target.ChampionName.ToLower()].Enabled && Q.Ready && target.IsValidTarget(Q.Range) && target != null)
            {
                var prediction = Q.GetPrediction(target);
                if (prediction.HitChance >= Q.HitChance)
                { 
                    Flash.Cast(target);
                    Task.Delay(250);
                    Q.Cast(target);

                }
            }
        }
        private void Combo()
        {
            //Store boolean value of menu items
            bool useQ = Menu["combo"]["useq"].Enabled;
            bool useE = Menu["combo"]["usee"].Enabled;
            bool useR = Menu["combo"]["user"].Enabled;
            var rAOE = Menu["combo"]["rAOE"].As<MenuList>().Value;

            var target = TargetSelector.GetTarget(Q.Range);

            //Q logic
            if (target != null)
            {
                if (useQ && Q.Ready && Menu["combo"]["useqon" + target.ChampionName.ToLower()].Enabled && target.IsValidTarget(Q.Range - 425))
                {
                    //var prediction = Q.GetPrediction(target);
                    //if (prediction.HitChance >= Q.HitChance)
                   // {
                        Q.Cast(target);
                    Console.WriteLine(Q.HitChance);
                    //}

                }
            }

            //E logic - Avoid using E on already knocked up target
            if (useE && E.Ready && target.IsValidTarget(120) && !target.HasBuffOfType(BuffType.Knockup) && !target.HasBuffOfType(BuffType.Stun))
            {
                E.Cast();
            }

            //R logic - Use R for AOE
            if (useR && R.Ready && target.IsValidTarget(R.Range) && Player.CountEnemyHeroesInRange(R.Range) >= rAOE)
            {
                R.Cast();
            }
        }

        private void Killsteal()
        {
            var ks = Menu["combo"]["userKS"].Enabled;
            var Iks = Menu["combo"]["userIKS"].Enabled;
            var target = TargetSelector.GetTarget(R.Range);

            if (ks && R.Ready && target.IsValidTarget(R.Range) && Player.GetSpellDamage(target, SpellSlot.R) >= target.Health)
            {
                R.Cast();
            }
            if (Iks && target.IsValidTarget(600) && ((!target.IsValidTarget(R.Range) || !R.Ready || !ks))&& Ignite.Ready && (50+(20*Player.Level)) > target.Health)
            {
                Ignite.Cast(target);
            }
        }
    }

}