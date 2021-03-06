﻿using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using System;
using System.Linq;
using UBAddons.General;
using UBAddons.Libs;
using UBAddons.Libs.Dictionary;
using UBAddons.Log;
using Color = System.Drawing.Color;

namespace UBAddons.Champions.Velkoz
{
    internal class Velkoz : ChampionPlugin
    {
        internal static AIHeroClient player = Player.Instance;
        protected static Spell.Skillshot Q { get; set; }
        protected static Spell.Skillshot Q2 { get; set; }
        //private static Spell.Skillshot QFake { get; set; }
        protected static MissileClient QMissile { get; set; }
        protected static Spell.Skillshot W { get; set; }
        protected static Spell.Skillshot E { get; set; }
        internal static Spell.Skillshot R { get; set; }
        protected static int LastQTick { get; set; }

        protected static Menu Menu { get; set; }
        protected static Menu ComboMenu { get; set; }
        protected static Menu HarassMenu { get; set; }
        protected static Menu LaneClearMenu { get; set; }
        protected static Menu JungleClearMenu { get; set; }
        protected static Menu LastHitMenu { get; set; }
        protected static Menu MiscMenu { get; set; }
        protected static Menu DrawMenu { get; set; }

        static Velkoz()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, DamageType.Magical);

            Q2 = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 800, Q.Speed, Q.Width, DamageType.Magical);
            
            //QFake = new Spell.Skillshot(SpellSlot.Q, DamageType.Magical);
           
            W = new Spell.Skillshot(SpellSlot.W, DamageType.Magical)
            {
                AllowedCollisionCount = int.MaxValue,
            };

            E = new Spell.Skillshot(SpellSlot.E, DamageType.Magical)
            {
                AllowedCollisionCount = int.MaxValue,
            };

            R = new Spell.Skillshot(SpellSlot.R, 1550, SkillShotType.Linear, 250, int.MaxValue, 60, DamageType.Magical)
            {
                AllowedCollisionCount = int.MaxValue,
            };

            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            MissileClient.OnCreate += delegate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (sender != null && missile.SpellCaster.IsMe)
                {
                    if (missile.SData.Name.Equals("VelkozQMissile"))
                    {
                        QMissile = missile;
                    }
                    if (missile.SData.Name.Equals("VelkozQMissileSplit"))
                    {
                        QMissile = null;
                    }
                }
            };
            MissileClient.OnDelete += delegate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (sender != null && missile.SpellCaster.IsMe)
                {
                    if (missile.SData.Name.Equals("VelkozQMissile"))
                    {
                        QMissile = null;
                    }
                }
            };
            Game.OnUpdate += delegate(EventArgs args)
            {
                if (QMissile == null) return;
                Q2.SourcePosition = QMissile.Position;
                Q2.RangeCheckSource = QMissile.Position;
                if (Q.ToggleState == 2 || Q.Name.Equals("VelkozQSplitActivate"))
                {
                    Vector2 now = QMissile.Position.To2D();
                    Vector2 start = QMissile.StartPosition.To2D();
                    Vector2 end = QMissile.EndPosition.To2D();
                    var pos1 = now + (start - end - 50).Rotated((float)Math.PI / 2);
                    var pos2 = now + (start - end - 50).Rotated(-(float)Math.PI / 2);
                    Geometry.Polygon.Rectangle[] perpendicular = new Geometry.Polygon.Rectangle[] 
                    {
                        new Geometry.Polygon.Rectangle(QMissile.Position.To2D(), pos1, Q.Width),
                        new Geometry.Polygon.Rectangle(QMissile.Position.To2D(), pos2, Q.Width),
                    };
                    foreach (var rectangle in perpendicular)
                    {
                        var target = Q2.GetTarget();
                        if (target != null)
                        {
                            var pred = Q2.GetPrediction(target);
                            if (rectangle.IsInside(pred.CastPosition) && pred.CanNext(Q2, MenuValue.General.QHitChance, false) && Q.IsReady())
                            {
                                Player.CastSpell(SpellSlot.Q);
                            }
                        }
                    }
                }
            };
            AIHeroClient.OnProcessSpellCast += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (!sender.IsMe) return;
                if (args.Slot.Equals(SpellSlot.Q))
                {
                    LastQTick = Core.GameTickCount;
                }
            };
        }

        #region Creat Menu
        protected override void CreateMenu()
        {
            try
            {
                #region Mainmenu
                Menu = MainMenu.AddMenu("UB" + player.Hero, "UBAddons.MainMenu" + player.Hero, "UB" + player.Hero + " - UBAddons - by U.Boruto");
                Menu.AddGroupLabel("General Setting");
                Menu.CreatSlotHitChance(SpellSlot.Q);
                Menu.Add("UBAddons.Velkoz.Q.Split.Enable", new CheckBox("Use Q split [Beta]", false));
                Menu.Add("UBAddons.Velkoz.Q.Split.OnlyOutRange", new CheckBox("Only Out Range", true));
                Menu.CreatSlotHitChance(SpellSlot.W);
                Menu.CreatSlotHitChance(SpellSlot.E);
                Menu.CreatSlotHitChance(SpellSlot.R);

                #endregion

                #region Combo
                ComboMenu = Menu.AddSubMenu("Combo", "UBAddons.ComboMenu" + player.Hero, "Settings your combo below");
                {
                    ComboMenu.CreatSlotCheckBox(SpellSlot.Q);
                    ComboMenu.CreatSlotCheckBox(SpellSlot.W);
                    ComboMenu.CreatSlotCheckBox(SpellSlot.E);
                    ComboMenu.CreatSlotCheckBox(SpellSlot.R);
                }
                #endregion

                #region Harass
                HarassMenu = Menu.AddSubMenu("Harass", "UBAddons.HarassMenu" + player.Hero, "Settings your harass below");
                {
                    HarassMenu.CreatSlotCheckBox(SpellSlot.Q);
                    HarassMenu.CreatSlotCheckBox(SpellSlot.W);
                    HarassMenu.CreatSlotCheckBox(SpellSlot.E);
                    HarassMenu.CreatManaLimit();
                    HarassMenu.CreatHarassKeyBind();
                }
                #endregion

                #region LaneClear
                LaneClearMenu = Menu.AddSubMenu("LaneClear", "UBAddons.LaneClear" + player.Hero, "Settings your laneclear below");
                {
                    LaneClearMenu.CreatLaneClearOpening();
                    LaneClearMenu.CreatSlotCheckBox(SpellSlot.Q, null, false);
                    LaneClearMenu.CreatSlotCheckBox(SpellSlot.W, null, false);
                    LaneClearMenu.CreatSlotHitSlider(SpellSlot.W, 5, 1, 10);
                    LaneClearMenu.CreatSlotCheckBox(SpellSlot.E, null, false);
                    LaneClearMenu.CreatManaLimit();
                }
                #endregion

                #region JungleClear
                JungleClearMenu = Menu.AddSubMenu("JungleClear", "UBAddons.JungleClear" + player.Hero, "Settings your jungleclear below");
                {
                    JungleClearMenu.CreatSlotCheckBox(SpellSlot.Q);
                    JungleClearMenu.CreatSlotCheckBox(SpellSlot.W);
                    JungleClearMenu.CreatSlotCheckBox(SpellSlot.E, null, false);
                    JungleClearMenu.CreatManaLimit();
                }
                #endregion

                #region Lasthit
                LastHitMenu = Menu.AddSubMenu("Lasthit", "UBAddons.Lasthit" + player.Hero, "UB" + player.Hero + " - Settings your unkillable minion below");
                {
                    LastHitMenu.CreatLasthitOpening();
                    LastHitMenu.CreatSlotCheckBox(SpellSlot.Q);
                    LastHitMenu.CreatSlotCheckBox(SpellSlot.W);
                    LastHitMenu.CreatSlotCheckBox(SpellSlot.E, null, false);
                    LastHitMenu.CreatManaLimit();
                }
                #endregion

                #region Misc
                MiscMenu = Menu.AddSubMenu("Misc", "UBAddons.Misc" + player.Hero, "Settings your misc below");
                {
                    MiscMenu.AddGroupLabel("Anti Gapcloser settings");
                    MiscMenu.CreatMiscGapCloser();
                    MiscMenu.CreatSlotCheckBox(SpellSlot.E, "GapCloser");
                    MiscMenu.AddGroupLabel("Interrupter settings");
                    MiscMenu.CreatDangerValueBox();
                    MiscMenu.CreatSlotCheckBox(SpellSlot.E, "Interrupter");
                    MiscMenu.AddGroupLabel("Killsteal settings");
                    MiscMenu.CreatSlotCheckBox(SpellSlot.Q, "KillSteal");
                    MiscMenu.CreatSlotCheckBox(SpellSlot.W, "KillSteal");
                    MiscMenu.CreatSlotCheckBox(SpellSlot.E, "KillSteal");
                    MiscMenu.CreatSlotCheckBox(SpellSlot.R, "KillSteal", false);
                }
                #endregion

                #region Drawings
                DrawMenu = Menu.AddSubMenu("Drawings", "UBAddons.Drawings" + player.Hero, "Settings your drawings below");
                {
                    DrawMenu.CreatDrawingOpening();
                    DrawMenu.CreatColorPicker(SpellSlot.Q);
                    DrawMenu.CreatColorPicker(SpellSlot.W);
                    DrawMenu.CreatColorPicker(SpellSlot.E);
                    DrawMenu.CreatColorPicker(SpellSlot.R);
                    DrawMenu.CreatColorPicker(SpellSlot.Unknown);
                }
                #endregion

                DamageIndicator.Initalize(MenuValue.Drawings.ColorDmg);
            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString(), Console_Message.Error);
            }
        }
        #endregion

        #region Boolean
        protected override bool EnableDraw
        {
            get { return MenuValue.Drawings.EnableDraw; }
        }

        protected override bool EnableDamageIndicator
        {
            get { return MenuValue.Drawings.DrawDamageIndicator; }
        }

        protected override bool IsAutoHarass
        {
            get { return MenuValue.Harass.IsAuto; }
        }
        #endregion

        #region Misc
        protected override void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender == null || !sender.IsValidTarget() || !sender.IsEnemy) return;
            if (MenuValue.Misc.EGap && E.IsReady() && (MenuValue.Misc.Idiot ? player.Distance(args.End) <= 250 : E.IsInRange(args.End) || sender.IsAttackingPlayer))
            {
                E.Cast(sender);
            }
        }

        protected override void OnInterruptable(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (sender == null || !sender.IsEnemy || !sender.IsValidTarget() || !MenuValue.Misc.dangerValue.Contains(args.DangerLevel)) return;
            if (E.IsReady() && MenuValue.Misc.EI && E.IsInRange(sender))
            {
                E.Cast(sender);
            }
        }

        protected override void OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (target == null || target.IsInvulnerable || !target.IsValidTarget()) return;
            if (MenuValue.LastHit.PreventCombo && Orbwalker.ActiveModes.Combo.IsOrb()) return;
            if (MenuValue.LastHit.OnlyFarmMode && !Variables.IsFarm) return;
            if (player.ManaPercent < MenuValue.LastHit.ManaLimit) return;
            if (args.RemainingHealth <= DamageIndicator.DamageDelegate(target, SpellSlot.Q) && MenuValue.LastHit.UseQ && Q.IsReady() && Q.IsInRange(target))
            {
                var predHealth = Q.GetHealthPrediction(target);
                if (predHealth < float.Epsilon) return;
                Q.Cast(target);
            }
            if (args.RemainingHealth <= DamageIndicator.DamageDelegate(target, SpellSlot.W) && MenuValue.LastHit.UseW && W.IsReady() && W.IsInRange(target))
            {
                var predHealth = W.GetHealthPrediction(target);
                if (predHealth < float.Epsilon) return;
                W.Cast(target);
            }
            if (args.RemainingHealth <= DamageIndicator.DamageDelegate(target, SpellSlot.E) && MenuValue.LastHit.UseE && E.IsReady() && E.IsInRange(target))
            {
                var predHealth = E.GetHealthPrediction(target);
                if (predHealth < float.Epsilon) return;
                E.Cast(target);
            }
        }
        #endregion

        #region Damage

        #region DamageRaw
        public static float PassiveDamage(Obj_AI_Base target)
        {
            if (target.HasBuff("velkozresearchstack"))
            {
                return player.CalculateDamageOnUnit(target, DamageType.True, 25 + 8 * player.Level + 0.5f * player.TotalMagicalDamage);
            }
            else
            {
                return 0;
            }
        }
        public static float QDamage(Obj_AI_Base target)
        {
            return player.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 80f, 120f, 160f, 200f, 240f }[Q.Level] + 0.6f * player.TotalMagicalDamage);
        }
        public static float WDamage(Obj_AI_Base target)
        {
            if (target.GetMovementBlockedDebuffDuration() < 1.2f)
            {
                return player.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 30f, 50f, 70f, 90f, 110f }[W.Level] + 0.15f * player.TotalMagicalDamage);
            }
            else
            {
                return player.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 75f, 125f, 175f, 255f, 275f }[W.Level] + 0.40f * player.TotalMagicalDamage);
            }
        }
        public static float EDamage(Obj_AI_Base target)
        {
            return player.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 70f, 100f, 130f, 160f, 190f }[E.Level] + 0.3f * player.TotalMagicalDamage);
        }
        public static float RDamage(Obj_AI_Base target)
        {
            if (R.IsInRange(target))
            {
                var raw = new[] { 0f, 450f, 625f, 800f }[R.Level] + 1.25f * player.TotalMagicalDamage;
                var timeToOut = (R.Range - player.Distance(target)) / target.MoveSpeed + target.GetMovementBlockedDebuffDuration();
                var percentRHit = timeToOut / 2.5f >= 1 ? 1 : timeToOut / 2.5f;
                return player.CalculateDamageOnUnit(target, target.HasBuff("velkozreseachedstack") ? DamageType.True : DamageType.Magical, raw * percentRHit);
            }
            else
            {
                return player.GetSpellDamage(target, SpellSlot.R);
            }
        }
        #endregion

        internal static float HandleDamageIndicator(Obj_AI_Base target, SpellSlot? slot = null)
        {
            if (target == null)
            {
                return 0;
            }
            switch (slot)
            {
                case SpellSlot.Q:
                    return QDamage(target);
                case SpellSlot.W:
                    return WDamage(target);
                case SpellSlot.E:
                    return EDamage(target);
                case SpellSlot.R:
                    return RDamage(target);
                default:
                    {
                        float damage = 0f;
                        if (target.HasBuff("velkozresearchstack"))
                        {
                            damage = damage + PassiveDamage(target);
                        }
                        if (Q.IsReady())
                        {
                            damage = damage + QDamage(target);
                        }
                        if (W.IsReady())
                        {
                            damage = damage + WDamage(target);
                        }
                        if (E.IsReady())
                        {
                            damage = damage + EDamage(target);
                        }
                        if (R.IsReady())
                        {
                            damage = damage + RDamage(target);
                        }
                        if (Orbwalker.CanAutoAttack)
                        {
                            damage = damage + player.GetAutoAttackDamage(target, true);
                        }
                        return damage;
                    }
            }
        }
        #endregion

        #region Modes
        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void Combo()
        {
            Modes.Combo.Execute();
        }

        protected override void Harass()
        {
            if (player.IsUnderEnemyturret()) return;
            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }
        #endregion

        #region Drawings
        protected override void OnDraw(EventArgs args)
        {
            if (!MenuValue.Drawings.EnableDraw) return;
            if (MenuValue.Drawings.DrawQ && (!MenuValue.Drawings.ReadyQ || Q.IsReady()))
            {
                Q.DrawRange(MenuValue.Drawings.ColorQ);
            }
            if (MenuValue.Drawings.DrawW && (!MenuValue.Drawings.ReadyW || W.IsReady()))
            {
                W.DrawRange(MenuValue.Drawings.ColorW);
            }
            if (MenuValue.Drawings.DrawE && (!MenuValue.Drawings.ReadyE || E.IsReady()))
            {
                E.DrawRange(MenuValue.Drawings.ColorE);
            }
            if (MenuValue.Drawings.DrawR && (!MenuValue.Drawings.ReadyR || R.IsReady()))
            {
                R.DrawRange(MenuValue.Drawings.ColorR);
            }
        }
        #endregion

        #region QLogic
        protected static void QCast(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget()) return;
            if (Q.IsInRange(target))
            {
                var pred = Q.GetPrediction(target);
                if (pred.CanNext(Q, MenuValue.General.QHitChance, false))
                {
                    Q.Cast(pred.CastPosition);
                }
                else
                {
                    if (MenuValue.General.UseQSplit && !MenuValue.General.OnlyOutRange)
                    {
                        for (int i = 1; i <= 45; i++)
                        {
                            Vector3[] Position = new Vector3[] 
                            {
                                player.Position.Extend(target, Q.Range).RotateAroundPoint(player.Position.To2D(), (float)Math.PI / (180 / i)).To3DWorld(),
                                player.Position.Extend(target, Q.Range).RotateAroundPoint(player.Position.To2D(), (float)-Math.PI / (180 / i)).To3DWorld()
                            };
                            Vector3 position = (from pos in Position
                                                let sourcePos = player.Position.Extend(pos, player.Distance(pred.CastPosition) * (float)Math.Cos(i)).To3DWorld()
                                                where VectorHelper.Linear_Collision_Point(player.Position, sourcePos, Q.Range, Q.Width, Q.Speed, Q.CastDelay) == Vector2.Zero
                                                where Prediction.Position.PredictLinearMissile(target, Q2.Range, Q2.Radius, Q2.CastDelay, Q2.Speed, 0, sourcePos).CanNext(Q2, MenuValue.General.QHitChance, false)
                                                select pos).FirstOrDefault();
                            if (position != null)
                            {
                                Q.Cast(position);
                            }
                        }
                    }
                }
            }
            else
            {
                if (MenuValue.General.UseQSplit && player.IsInRange(target, (float)Math.Sqrt(Math.Pow(1050, 2) + Math.Pow(1100, 2))))
                {
                    Vector3[] Position = new Vector3[] 
                            {
                                player.Position.Extend(target, Q.Range).RotateAroundPoint(player.Position.To2D(), (float)Math.PI / (180 / 45)).To3DWorld(),
                                player.Position.Extend(target, Q.Range).RotateAroundPoint(player.Position.To2D(), (float)-Math.PI / (180 / 45)).To3DWorld()
                            };
                    Vector3 position = (from pos in Position
                                        let sourcePos = player.Position.Extend(pos, player.Distance(Prediction.Position.PredictUnitPosition(target, 800)) * (float)Math.Cos(45)).To3DWorld()
                                        where VectorHelper.Linear_Collision_Point(player.Position, sourcePos, Q.Range, Q.Width, Q.Speed, Q.CastDelay) == Vector2.Zero
                                        where Prediction.Position.PredictLinearMissile(target, Q2.Range, Q2.Radius, Q2.CastDelay, Q2.Speed, 0, sourcePos).CanNext(Q2, MenuValue.General.QHitChance, false)
                                        select pos).FirstOrDefault();
                    if (position != null)
                    {
                        Q.Cast(position);
                    }
                }
            }
        }
        #endregion

        #region Menu Value
        protected internal static class MenuValue
        {
            internal static class General
            {
                public static int QHitChance { get { return Menu.GetSlotHitChance(SpellSlot.Q); } }

                public static bool UseQSplit { get { return Menu.VChecked("UBAddons.Velkoz.Q.Split.Enable"); } }

                public static bool OnlyOutRange { get { return Menu.VChecked("UBAddons.Velkoz.Q.Split.OnlyOutRange"); } }

                public static int WHitChance { get { return Menu.GetSlotHitChance(SpellSlot.W); } }

                public static int EHitChance { get { return Menu.GetSlotHitChance(SpellSlot.E); } }

                public static int RHitChance { get { return Menu.GetSlotHitChance(SpellSlot.R); } }

            }

            internal static class Combo
            {
                public static bool UseQ { get { return ComboMenu.GetSlotCheckBox(SpellSlot.Q); } }

                public static bool UseW { get { return ComboMenu.GetSlotCheckBox(SpellSlot.W); } }

                public static bool UseE { get { return ComboMenu.GetSlotCheckBox(SpellSlot.E); } }

                public static bool UseR { get { return ComboMenu.GetSlotCheckBox(SpellSlot.R); } }

            }

            internal static class Harass
            {
                public static bool UseQ { get { return HarassMenu.GetSlotCheckBox(SpellSlot.Q); } }

                public static bool UseW { get { return HarassMenu.GetSlotCheckBox(SpellSlot.W); } }

                public static bool UseE { get { return HarassMenu.GetSlotCheckBox(SpellSlot.E); } }

                public static int ManaLimit { get { return HarassMenu.GetManaLimit(); } }

                public static bool IsAuto { get { return HarassMenu.GetHarassKeyBind(); } }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies { get { return LaneClearMenu.GetNoEnemyOnly(); } }

                public static int ScanRange { get { return LaneClearMenu.GetDetectRange(); } }

                public static bool OnlyKillable { get { return LaneClearMenu.GetKillableOnly(); } }

                public static bool UseQ { get { return LaneClearMenu.GetSlotCheckBox(SpellSlot.Q); } }

                public static bool UseW { get { return LaneClearMenu.GetSlotCheckBox(SpellSlot.W); } }

                public static int Whit { get { return LaneClearMenu.GetSlotHitSlider(SpellSlot.W); } }

                public static bool UseE { get { return LaneClearMenu.GetSlotCheckBox(SpellSlot.E); } }

                public static int ManaLimit { get { return LaneClearMenu.GetManaLimit(); } }
            }

            internal static class JungleClear
            {

                public static bool UseQ { get { return JungleClearMenu.GetSlotCheckBox(SpellSlot.Q); } }

                public static bool UseW { get { return JungleClearMenu.GetSlotCheckBox(SpellSlot.W); } }

                public static bool UseE { get { return JungleClearMenu.GetSlotCheckBox(SpellSlot.E); } }

                public static int ManaLimit { get { return JungleClearMenu.GetManaLimit(); } }
            }

            internal static class LastHit
            {
                public static bool OnlyFarmMode { get { return LastHitMenu.OnlyFarmMode(); } }

                public static bool PreventCombo { get { return LastHitMenu.PreventCombo(); } }

                public static bool UseQ { get { return LastHitMenu.GetSlotCheckBox(SpellSlot.Q); } }

                public static bool UseW { get { return LastHitMenu.GetSlotCheckBox(SpellSlot.W); } }

                public static bool UseE { get { return LastHitMenu.GetSlotCheckBox(SpellSlot.E); } }

                public static int ManaLimit { get { return LastHitMenu.GetManaLimit(); } }
            }

            internal static class Misc
            {
                public static bool QKS { get { return MiscMenu.GetSlotCheckBox(SpellSlot.Q, Misc_Menu_Value.KillSteal.ToString()); } }

                public static bool WKS { get { return MiscMenu.GetSlotCheckBox(SpellSlot.W, Misc_Menu_Value.KillSteal.ToString()); } }

                public static bool EKS { get { return MiscMenu.GetSlotCheckBox(SpellSlot.E, Misc_Menu_Value.KillSteal.ToString()); } }

                public static bool RKS { get { return MiscMenu.GetSlotCheckBox(SpellSlot.R, Misc_Menu_Value.KillSteal.ToString()); } }

                public static bool Idiot { get { return MiscMenu.PreventIdiotAntiGap(); } }

                public static bool EGap { get { return MiscMenu.GetSlotCheckBox(SpellSlot.E, Misc_Menu_Value.GapCloser.ToString()); } }

                public static DangerLevel[] dangerValue { get { return MiscMenu.GetDangerValue(); } }

                public static bool EI { get { return MiscMenu.GetSlotCheckBox(SpellSlot.E, Misc_Menu_Value.Interrupter.ToString()); } }
            }

            internal static class Drawings
            {

                public static bool EnableDraw { get { return DrawMenu.VChecked(Variables.AddonName + "." + player.Hero + ".EnableDraw"); } }

                public static bool DrawQ { get { return DrawMenu.GetDrawCheckValue(SpellSlot.Q); } }

                public static bool ReadyQ { get { return DrawMenu.GetOnlyReady(SpellSlot.Q); } }

                public static SharpDX.Color ColorQ { get { return DrawMenu.GetColorPicker(SpellSlot.Q).ToSharpDX(); } }

                public static bool DrawW { get { return DrawMenu.GetDrawCheckValue(SpellSlot.W); } }

                public static bool ReadyW { get { return DrawMenu.GetOnlyReady(SpellSlot.W); } }

                public static SharpDX.Color ColorW { get { return DrawMenu.GetColorPicker(SpellSlot.W).ToSharpDX(); } }

                public static bool DrawE { get { return DrawMenu.GetDrawCheckValue(SpellSlot.E); } }

                public static bool ReadyE { get { return DrawMenu.GetOnlyReady(SpellSlot.E); } }

                public static SharpDX.Color ColorE { get { return DrawMenu.GetColorPicker(SpellSlot.E).ToSharpDX(); } }

                public static bool DrawR { get { return DrawMenu.GetDrawCheckValue(SpellSlot.R); } }

                public static bool ReadyR { get { return DrawMenu.GetOnlyReady(SpellSlot.R); } }

                public static SharpDX.Color ColorR { get { return DrawMenu.GetColorPicker(SpellSlot.R).ToSharpDX(); } }

                public static bool DrawDamageIndicator { get { return DrawMenu.GetDrawCheckValue(SpellSlot.Unknown); } }

                public static Color ColorDmg { get { return DrawMenu.GetColorPicker(SpellSlot.Unknown); } }

            }
        }
        #endregion
    }
}
