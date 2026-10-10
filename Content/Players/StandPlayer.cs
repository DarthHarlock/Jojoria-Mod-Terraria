using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System.Reflection; // Necesario para leer y aplicar el modo sin modificar los Stands

using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;

using Jojo.Content.Projectiles.Justice.Justice_Tier_1;
using Jojo.Content.Projectiles.Justice.Justice_Tier_2;
using Jojo.Content.Projectiles.Justice.Justice_Tier_3;
using Jojo.Content.Projectiles.Justice.Justice_Tier_4;

using Jojo.Content.Projectiles.Tusk.Tusk_Tier_1;

using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_1;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_2;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;

using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_1;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_2;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_3;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_4;

using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_1;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_2;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_3;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_4;

using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_2;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem;

using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4;

using Jojo.Content.Projectiles.HGreen.HGreen_Tier_1;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_3;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4;

using Jojo.Content.Projectiles.D4C.D4C_Tier_1;
using Jojo.Content.Projectiles.D4C.D4C_Tier_2;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3;
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;

using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_2;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_3;

using Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final;

using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_1;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_2;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_4;

using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_1;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_2;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4;

using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_2;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_4;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem;

using Jojo.Content.Projectiles.Anubis.Anubis_Tier_1;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_2;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_3;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_4;

using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_1;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_2;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_3;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_4;

using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_1;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_2;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_3;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4;

using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_1;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_2;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4;

using Jojo.Content.Projectiles.Rika_Stand;
using Jojo.Content.UI;

namespace Jojo.Content.Players
{
    public class StandPlayer : ModPlayer
    {
        // 💾 Guarda el último modo del Stand (false = Manual, true = Automático)
        public bool lastStandAutoState = false;

        // =========================
        // 🔥 COOLDOWN GLOBAL F
        // =========================
        public int fCooldownTimer = 0;

        public override void ResetEffects()
        {
            if (fCooldownTimer > 0)
                fCooldownTimer--;
        }

        public bool CanUseF()
        {
            return fCooldownTimer <= 0;
        }

        public void StartFCooldown(int time)
        {
            fCooldownTimer = time;
        }

        // =========================
        // 🔍 DETECCIÓN DE STANDS
        // =========================
        private bool IsStandProjectile(Projectile p)
        {
            return p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_4>()

                || p.type == ModContent.ProjectileType<JUSTICESTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<JUSTICESTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<JUSTICESTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<JUSTICESTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<WEATHERSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<WEATHERSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<WEATHERSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<WEATHERSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<HGREENSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<HGREENSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<HGREENSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<HGREENSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<GOLDENSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<GOLDENSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<GOLDENSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<GOLDENSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<GOLDENSTAND_Requiem>()
                || p.type == ModContent.ProjectileType<D4CSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<D4CSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<D4CSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<D4CSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<CMOONSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<CMOONSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<CMOONSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<MADEINHEAVENSTAND>()
                || p.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_4>()

                || p.type == ModContent.ProjectileType<TUSKSTAND_Tier_1>()

                || p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_4>()

                || p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>()
                || p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_4>()
                || p.type == ModContent.ProjectileType<RIKASTAND>()
                || p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_1>()
                || p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_2>()
                || p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>()
                || p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_4>();
        }

        private bool HasStandProjectile()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (!p.active) continue;
                if (p.owner != Player.whoAmI) continue;

                if (IsStandProjectile(p))
                    return true;
            }

            return false;
        }

        // =========================
        // 💾 GUARDAR ESTADO ACTUAL
        // =========================
        private void SaveStandAutoState()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (!p.active) continue;
                if (p.owner != Player.whoAmI) continue;

                if (IsStandProjectile(p))
                {
                    // Lee el estado actual por ai[0]
                    bool state = p.ai[0] == 1f;

                    // O lee directamente la variable 'auto' / 'AutoMode' en el ModProjectile
                    if (p.ModProjectile != null)
                    {
                        var type = p.ModProjectile.GetType();

                        var fieldAuto = type.GetField("auto", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldAuto != null && fieldAuto.FieldType == typeof(bool))
                        {
                            state = (bool)fieldAuto.GetValue(p.ModProjectile);
                        }
                        else
                        {
                            var fieldAutoMode = type.GetField("AutoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                ?? type.GetField("autoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fieldAutoMode != null && fieldAutoMode.FieldType == typeof(bool))
                            {
                                state = (bool)fieldAutoMode.GetValue(p.ModProjectile);
                            }
                        }
                    }

                    lastStandAutoState = state;
                    break;
                }
            }
        }

        // =========================
        // 🚀 INVOCAR RESTAURANDO MODO
        // =========================
        private void SpawnStandWithSavedState(string sourceTag)
        {
            int projType = StandSlotSystem.GetStandProjectileTypeFor(Player);

            if (projType != 0)
            {
                float ai0Value = lastStandAutoState ? 1f : 0f;

                int pIdx = Projectile.NewProjectile(
                    Player.GetSource_Misc(sourceTag),
                    Player.Center,
                    Vector2.Zero,
                    projType,
                    10,
                    2f,
                    Player.whoAmI,
                    ai0Value
                );

                if (pIdx >= 0 && pIdx < Main.maxProjectiles)
                {
                    Projectile spawnedStand = Main.projectile[pIdx];
                    spawnedStand.ai[0] = ai0Value;

                    if (spawnedStand.ModProjectile != null)
                    {
                        var type = spawnedStand.ModProjectile.GetType();

                        var fieldAuto = type.GetField("auto", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldAuto != null && fieldAuto.FieldType == typeof(bool))
                        {
                            fieldAuto.SetValue(spawnedStand.ModProjectile, lastStandAutoState);
                        }

                        var fieldAutoMode = type.GetField("AutoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? type.GetField("autoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldAutoMode != null && fieldAutoMode.FieldType == typeof(bool))
                        {
                            fieldAutoMode.SetValue(spawnedStand.ModProjectile, lastStandAutoState);
                        }

                        var propAuto = type.GetProperty("AutoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? type.GetProperty("autoMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (propAuto != null && propAuto.CanWrite)
                        {
                            propAuto.SetValue(spawnedStand.ModProjectile, lastStandAutoState);
                        }
                    }
                }
            }
        }

        // =========================
        // 💀 DESPAWN CONTROLADO
        // =========================
        private void KillStandProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (!p.active) continue;
                if (p.owner != Player.whoAmI) continue;
                if (!IsStandProjectile(p)) continue;

                if (p.ModProjectile is RIKASTAND Rika1) Rika1.StartDying();
                else if (p.ModProjectile is SLIVERCHARIOTSTAND_Tier_1 SilverChariot1) SilverChariot1.StartDying();
                else if (p.ModProjectile is SLIVERCHARIOTSTAND_Tier_2 SilverChariot2) SilverChariot2.StartDying();
                else if (p.ModProjectile is SLIVERCHARIOTSTAND_Tier_3 SilverChariot3) SilverChariot3.StartDying();
                else if (p.ModProjectile is SLIVERCHARIOTSTAND_Tier_4 SilverChariot4) SilverChariot4.StartDying();
                else if (p.ModProjectile is CRAZYDIAMONDSTAND_Tier_1 CrazyDiamond1) CrazyDiamond1.StartDying();
                else if (p.ModProjectile is CRAZYDIAMONDSTAND_Tier_2 CrazyDiamond2) CrazyDiamond2.StartDying();
                else if (p.ModProjectile is CRAZYDIAMONDSTAND_Tier_3 CrazyDiamond3) CrazyDiamond3.StartDying();
                else if (p.ModProjectile is CRAZYDIAMONDSTAND_Tier_4 CrazyDiamond4) CrazyDiamond4.StartDying();
                else if (p.ModProjectile is GOLDENSTAND_Tier_1 Golden1) Golden1.StartDying();
                else if (p.ModProjectile is GOLDENSTAND_Tier_2 Golden2) Golden2.StartDying();
                else if (p.ModProjectile is GOLDENSTAND_Tier_3 Golden3) Golden3.StartDying();
                else if (p.ModProjectile is GOLDENSTAND_Tier_4 Golden4) Golden4.StartDying();
                else if (p.ModProjectile is GOLDENSTAND_Requiem GoldenRequiem) GoldenRequiem.StartDying();
                else if (p.ModProjectile is WEATHERSTAND_Tier_1 Report1) Report1.StartDying();
                else if (p.ModProjectile is WEATHERSTAND_Tier_2 Report2) Report2.StartDying();
                else if (p.ModProjectile is WEATHERSTAND_Tier_3 Report3) Report3.StartDying();
                else if (p.ModProjectile is WEATHERSTAND_Tier_4 Report4) Report4.StartDying();
                else if (p.ModProjectile is HGREENSTAND_Tier_1 HGreen1) HGreen1.StartDying();
                else if (p.ModProjectile is HGREENSTAND_Tier_2 HGreen2) HGreen2.StartDying();
                else if (p.ModProjectile is HGREENSTAND_Tier_3 HGreen3) HGreen3.StartDying();
                else if (p.ModProjectile is HGREENSTAND_Tier_4 HGreen4) HGreen4.StartDying();
                else if (p.ModProjectile is CINDERELLASTAND_Tier_1 Cinderella1) Cinderella1.StartDying();
                else if (p.ModProjectile is CINDERELLASTAND_Tier_2 Cinderella2) Cinderella2.StartDying();
                else if (p.ModProjectile is CINDERELLASTAND_Tier_3 Cinderella3) Cinderella3.StartDying();
                else if (p.ModProjectile is CINDERELLASTAND_Tier_4 Cinderella4) Cinderella4.StartDying();
                else if (p.ModProjectile is D4CSTAND_Tier_1 D4C1) D4C1.StartDying();
                else if (p.ModProjectile is D4CSTAND_Tier_2 D4C2) D4C2.StartDying();
                else if (p.ModProjectile is D4CSTAND_Tier_3 D4C3) D4C3.StartDying();
                else if (p.ModProjectile is D4CSTAND_Tier_4 D4C4) D4C4.StartDying();
                else if (p.ModProjectile is SILVERCHARIOTSTAND_Requiem SilverChariotRequiem) SilverChariotRequiem.StartDying();
                else if (p.ModProjectile is MAGICIANSREDSTAND_Tier_1 MagiciansRed1) MagiciansRed1.StartDying();
                else if (p.ModProjectile is MAGICIANSREDSTAND_Tier_2 MagiciansRed2) MagiciansRed2.StartDying();
                else if (p.ModProjectile is MAGICIANSREDSTAND_Tier_3 MagiciansRed3) MagiciansRed3.StartDying();
                else if (p.ModProjectile is MAGICIANSREDSTAND_Tier_4 MagiciansRed4) MagiciansRed4.StartDying();

                else if (p.ModProjectile is JUSTICESTAND_Tier_1 Justice1) Justice1.StartDying();

                else if (p.ModProjectile is JUSTICESTAND_Tier_2 Justice2) Justice2.StartDying();
                else if (p.ModProjectile is JUSTICESTAND_Tier_3 Justice3) Justice3.StartDying();
                else if (p.ModProjectile is JUSTICESTAND_Tier_4 Justice4) Justice4.StartDying();
                else if (p.ModProjectile is KINGCRIMSONSTAND_Tier_1 KingCrimson1) KingCrimson1.StartDying();
                else if (p.ModProjectile is KINGCRIMSONSTAND_Tier_2 KingCrimson2) KingCrimson2.StartDying();
                else if (p.ModProjectile is KINGCRIMSONSTAND_Tier_3 KingCrimson3) KingCrimson3.StartDying();
                else if (p.ModProjectile is KINGCRIMSONSTAND_Tier_4 KingCrimson4) KingCrimson4.StartDying();
                else if (p.ModProjectile is WHITESNAKESTAND_Tier_1 WhiteSnake1) WhiteSnake1.StartDying();
                else if (p.ModProjectile is WHITESNAKESTAND_Tier_2 WhiteSnake2) WhiteSnake2.StartDying();
                else if (p.ModProjectile is WHITESNAKESTAND_Tier_3 WhiteSnake3) WhiteSnake3.StartDying();
                else if (p.ModProjectile is WHITESNAKESTAND_Tier_4 WhiteSnake4) WhiteSnake4.StartDying();
                else if (p.ModProjectile is CMOONSTAND_Tier_1 CMoon1) CMoon1.StartDying();
                else if (p.ModProjectile is CMOONSTAND_Tier_2 CMoon2) CMoon2.StartDying();
                else if (p.ModProjectile is CMOONSTAND_Tier_3 CMoon) CMoon.StartDying();
                else if (p.ModProjectile is MADEINHEAVENSTAND MadeInHeaven1) MadeInHeaven1.StartDying();
                else if (p.ModProjectile is ANUBISSTAND_Tier_1 Anubis1) Anubis1.StartDying();
                else if (p.ModProjectile is ANUBISSTAND_Tier_2 Anubis2) Anubis2.StartDying();
                else if (p.ModProjectile is ANUBISSTAND_Tier_3 Anubis3) Anubis3.StartDying();
                else if (p.ModProjectile is ANUBISSTAND_Tier_4 Anubis4) Anubis4.StartDying();

                else if (p.ModProjectile is TUSKSTAND_Tier_1 Tusk1) Tusk1.StartDying();

                else if (p.ModProjectile is MONSTERSTAND_Tier_1 ScaryMonsters1) ScaryMonsters1.StartDying();
                else if (p.ModProjectile is MONSTERSTAND_Tier_2 ScaryMonsters2) ScaryMonsters2.StartDying();
                else if (p.ModProjectile is MONSTERSTAND_Tier_3 ScaryMonsters3) ScaryMonsters3.StartDying();
                else if (p.ModProjectile is MONSTERSTAND_Tier_4 ScaryMonsters4) ScaryMonsters4.StartDying();

                else if (p.ModProjectile is STARPLATINUMSTAND_Tier_1 StarPlatinumTier1) StarPlatinumTier1.StartDying();
                else if (p.ModProjectile is STARPLATINUMSTAND_Tier_2 StarPlatinumTier2) StarPlatinumTier2.StartDying();
                else if (p.ModProjectile is STARPLATINUMSTAND_Tier_3 StarPlatinumTier3) StarPlatinumTier3.StartDying();
                else if (p.ModProjectile is STARPLATINUMSTAND_Tier_4 StarPlatinumTier4) StarPlatinumTier4.StartDying();
                else if (p.ModProjectile is THEWORLDSTAND_Tier_1 theWorld1) theWorld1.StartDying();
                else if (p.ModProjectile is THEWORLDSTAND_Tier_2 theWorld2) theWorld2.StartDying();
                else if (p.ModProjectile is THEWORLDSTAND_Tier_3 theWorld3) theWorld3.StartDying();
                else if (p.ModProjectile is THEWORLDSTAND_Tier_4 theWorld4) theWorld4.StartDying();
                else if (p.ModProjectile is KILLERQUEENSTAND_Tier_1 killerQueen1) killerQueen1.StartDying();
                else if (p.ModProjectile is KILLERQUEENSTAND_Tier_2 killerQueen2) killerQueen2.StartDying();
                else if (p.ModProjectile is KILLERQUEENSTAND_Tier_3 killerQueen3) killerQueen3.StartDying();
                else if (p.ModProjectile is KILLERQUEENSTAND_Tier_4 killerQueen4) killerQueen4.StartDying();
                else p.Kill();
            }
        }

        // =========================
        // 💀 STAND SYSTEM
        // =========================
        public override void PostUpdate()
        {
            // ── GUARD MULTIPLAYER ──────────────────────────
            if (Player.whoAmI != Main.myPlayer)
                return;
            // ───────────────────────────────────────────────

            // Guardar constantemente el estado mientras el Stand existe en el mundo
            if (HasStandProjectile())
            {
                SaveStandAutoState();
            }

            // ❌ Si no hay stand equipado → eliminar
            if (!StandSlotSystem.HasStandFor(Player))
            {
                if (HasStandProjectile())
                    KillStandProjectiles();

                return;
            }

            if (JojoKeybinds.SummonStand.JustPressed)
            {
                if (!HasStandProjectile())
                {
                    SpawnStandWithSavedState("Stand");
                }
                else
                {
                    SaveStandAutoState();
                    KillStandProjectiles();
                }
            }
        }

        public override void UpdateDead()
        {
            // ── GUARD MULTIPLAYER ──────────────────────────
            if (Player.whoAmI != Main.myPlayer)
                return;
            // ───────────────────────────────────────────────

            // Guarda el modo antes de eliminar el Stand al morir
            SaveStandAutoState();
            KillStandProjectiles();
        }

        public override void OnRespawn()
        {
            // ── GUARD MULTIPLAYER ──────────────────────────
            if (Player.whoAmI != Main.myPlayer)
                return;
            // ───────────────────────────────────────────────

            if (!StandSlotSystem.HasStandFor(Player))
                return;

            if (!HasStandProjectile())
            {
                // Reaparece el Stand con el modo que tenía en el instante de morir
                SpawnStandWithSavedState("StandRespawn");
            }
        }
    }
}