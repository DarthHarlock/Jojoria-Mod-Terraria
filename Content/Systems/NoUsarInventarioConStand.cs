using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.DataStructures;
using System.Collections.Generic;

using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_1;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_2;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4;

using Jojo.Content.Projectiles.D4C.D4C_Tier_1;
using Jojo.Content.Projectiles.D4C.D4C_Tier_2;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3;
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;

using Jojo.Content.Projectiles.HGreen.HGreen_Tier_1;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_3;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4;

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

using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_1;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_2;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_4;

using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_2;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_4;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem;

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

using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;

using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_2;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_3;

using Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final;

using Jojo.Content.Projectiles.Anubis.Anubis_Tier_1;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_2;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_3;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_4;

// Scary Monsters: Tier 1, Tier 2, Tier 3 y Tier 4
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_1;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_2;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull;

// Tusk
using Jojo.Content.Projectiles.Tusk.Tusk_Tier_1;

using Jojo.Content.Players;

namespace Jojo.Content.Systems
{
    public class OcultarItemManoAnubis : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (player == null || !player.active || player.dead) return true;

            bool usandoKatanaOGarra = player.ownedProjectileCounts[ModContent.ProjectileType<KatanaSlash_Tier_1>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<KatanaSlash_Tier_2>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<KatanaSlash_Tier_3>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<KatanaSlash_Tier_4>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_1>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_3>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_2>()] > 0 ||
                                     player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_4>()] > 0;

            if (usandoKatanaOGarra)
            {
                return false;
            }
            return true;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) { }
    }

    public class NoUsarInventarioConStand : ModPlayer
    {
        private static HashSet<int> _standProjectileTypes;
        private float _manoAnimTimer = 0f;

        public override void Load()
        {
            _standProjectileTypes = new HashSet<int>
            {
                ModContent.ProjectileType<RIKASTAND>(),

                // Tusk (modo disparo = bloquea armas/inventario; modo libre ai[0]==1 = los devuelve)
                ModContent.ProjectileType<TUSKSTAND_Tier_1>(),

                ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>(),
                ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_2>(),
                ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>(),
                ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>(),
                ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>(),

                ModContent.ProjectileType<WHITESNAKESTAND_Tier_1>(),
                ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>(),
                ModContent.ProjectileType<WHITESNAKESTAND_Tier_3>(),
                ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>(),

                ModContent.ProjectileType<CMOONSTAND_Tier_1>(),
                ModContent.ProjectileType<CMOONSTAND_Tier_2>(),
                ModContent.ProjectileType<CMOONSTAND_Tier_3>(),

                ModContent.ProjectileType<MADEINHEAVENSTAND>(),

                ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_1>(),
                ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_2>(),
                ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_3>(),
                ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_4>(),

                ModContent.ProjectileType<CINDERELLASTAND_Tier_1>(),
                ModContent.ProjectileType<CINDERELLASTAND_Tier_2>(),
                ModContent.ProjectileType<CINDERELLASTAND_Tier_3>(),
                ModContent.ProjectileType<CINDERELLASTAND_Tier_4>(),

                ModContent.ProjectileType<GOLDENSTAND_Tier_1>(),
                ModContent.ProjectileType<GOLDENSTAND_Tier_2>(),
                ModContent.ProjectileType<GOLDENSTAND_Tier_3>(),
                ModContent.ProjectileType<GOLDENSTAND_Tier_4>(),
                ModContent.ProjectileType<GOLDENSTAND_Requiem>(),

                ModContent.ProjectileType<WEATHERSTAND_Tier_1>(),
                ModContent.ProjectileType<WEATHERSTAND_Tier_2>(),
                ModContent.ProjectileType<WEATHERSTAND_Tier_3>(),
                ModContent.ProjectileType<WEATHERSTAND_Tier_4>(),

                ModContent.ProjectileType<HGREENSTAND_Tier_1>(),
                ModContent.ProjectileType<HGREENSTAND_Tier_2>(),
                ModContent.ProjectileType<HGREENSTAND_Tier_3>(),
                ModContent.ProjectileType<HGREENSTAND_Tier_4>(),

                ModContent.ProjectileType<D4CSTAND_Tier_1>(),
                ModContent.ProjectileType<D4CSTAND_Tier_2>(),
                ModContent.ProjectileType<D4CSTAND_Tier_3>(),
                ModContent.ProjectileType<D4CSTAND_Tier_4>(),

                ModContent.ProjectileType<STARPLATINUMSTAND_Tier_1>(),
                ModContent.ProjectileType<STARPLATINUMSTAND_Tier_2>(),
                ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>(),
                ModContent.ProjectileType<STARPLATINUMSTAND_Tier_4>(),

                ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_1>(),
                ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_2>(),
                ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_3>(),
                ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_4>(),

                ModContent.ProjectileType<THEWORLDSTAND_Tier_1>(),
                ModContent.ProjectileType<THEWORLDSTAND_Tier_2>(),
                ModContent.ProjectileType<THEWORLDSTAND_Tier_3>(),
                ModContent.ProjectileType<THEWORLDSTAND_Tier_4>(),

                ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_1>(),
                ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_2>(),
                ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_3>(),
                ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_4>(),

                ModContent.ProjectileType<KILLERQUEENSTAND_Tier_1>(),
                ModContent.ProjectileType<KILLERQUEENSTAND_Tier_2>(),
                ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>(),
                ModContent.ProjectileType<KILLERQUEENSTAND_Tier_4>(),

                ModContent.ProjectileType<ANUBISSTAND_Tier_1>(),
                ModContent.ProjectileType<ANUBISSTAND_Tier_2>(),
                ModContent.ProjectileType<ANUBISSTAND_Tier_3>(),
                ModContent.ProjectileType<ANUBISSTAND_Tier_4>(),

                // Scary Monsters
                ModContent.ProjectileType<MONSTERSTAND_Tier_1>(),
                ModContent.ProjectileType<MONSTERSTAND_Tier_2>(),
                ModContent.ProjectileType<MONSTERSTAND_Tier_3>(),
                ModContent.ProjectileType<MONSTERSTAND_Tier_4>()
            };
        }

        public override void Unload()
        {
            _standProjectileTypes = null;
        }

        private bool HasStandActivo(Player player, out Projectile standProj)
        {
            standProj = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && _standProjectileTypes.Contains(p.type))
                {
                    standProj = p;
                    return true;
                }
            }
            return false;
        }

        private bool AnubisEstaAtacando(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != player.whoAmI) continue;

                if (p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_4>() && p.ModProjectile is ANUBISSTAND_Tier_4 stand4)
                {
                    if (stand4.isAttacking) return true;
                }
                if (p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_2>() && p.ModProjectile is ANUBISSTAND_Tier_2 stand2)
                {
                    if (stand2.isAttacking) return true;
                }
                if (p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_3>() && p.ModProjectile is ANUBISSTAND_Tier_3 stand3)
                {
                    if (stand3.isAttacking) return true;
                }
                if (p.type == ModContent.ProjectileType<ANUBISSTAND_Tier_1>() && p.ModProjectile is ANUBISSTAND_Tier_1 stand1)
                {
                    if (stand1.isAttacking) return true;
                }

                // Scary Monsters Tier 1
                if (p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_1>() && p.ModProjectile is MONSTERSTAND_Tier_1 monster1)
                {
                    if (monster1.isAttacking) return true;
                }

                // Scary Monsters Tier 2
                if (p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_2>() && p.ModProjectile is MONSTERSTAND_Tier_2 monster2)
                {
                    if (monster2.isAttacking) return true;
                }

                // Scary Monsters Tier 3
                if (p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_3>() && p.ModProjectile is MONSTERSTAND_Tier_3 monster3)
                {
                    if (monster3.isAttacking) return true;
                }

                // Scary Monsters Tier 4
                if (p.type == ModContent.ProjectileType<MONSTERSTAND_Tier_4>() && p.ModProjectile is MONSTERSTAND_Tier_4 monster4)
                {
                    if (monster4.isAttacking) return true;
                }
            }
            return false;
        }

        public override bool PreItemCheck()
        {
            if (!HasStandActivo(Player, out Projectile stand))
                return true;

            // RESTRICCIÓN ABSOLUTA: Si está en modo Dinosaurio Full, BLOQUEAR HERRAMIENTAS SIEMPRE
            bool esDino = Player.mount.Active && Player.mount.Type == ModContent.MountType<FromaDino>();
            if (esDino)
            {
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.channel = false;
                Player.itemAnimation = 0;
                Player.itemTime = 0;
                return false;
            }

            bool isAnubisOrMonster = stand.type == ModContent.ProjectileType<ANUBISSTAND_Tier_1>() ||
                                    stand.type == ModContent.ProjectileType<ANUBISSTAND_Tier_2>() ||
                                    stand.type == ModContent.ProjectileType<ANUBISSTAND_Tier_3>() ||
                                    stand.type == ModContent.ProjectileType<ANUBISSTAND_Tier_4>() ||
                                    stand.type == ModContent.ProjectileType<MONSTERSTAND_Tier_1>() ||
                                    stand.type == ModContent.ProjectileType<MONSTERSTAND_Tier_2>() ||
                                    stand.type == ModContent.ProjectileType<MONSTERSTAND_Tier_3>() ||
                                    stand.type == ModContent.ProjectileType<MONSTERSTAND_Tier_4>();

            // Modo Automático / Herramientas activadas mediante ai[0] == 1f (Teclado ToggleAuto)
            // En Tusk, ai[0] == 1f = modo libre: el stand no ataca y recuperas inventario/armas.
            bool isAutoMode = stand.ai[0] == 1f;

            if (isAutoMode)
                return true;

            if (isAnubisOrMonster && Main.mouseLeft && !Player.mouseInterface)
            {
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.channel = false;
                Player.itemAnimation = 0;
                Player.itemTime = 0;
                return false;
            }

            if (isAnubisOrMonster && !Main.mouseLeft)
            {
                _manoAnimTimer = 0f;
            }

            if (Player.controlUseItem && !Player.mouseInterface)
            {
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.channel = false;

                if (Player.itemAnimation > 0 && Player.HeldItem.DamageType != ModContent.GetInstance<Clases.ClaseStand>())
                {
                    Player.itemAnimation = 0;
                    Player.itemTime = 0;
                }
            }

            return true;
        }

        public override void PostUpdate()
        {
            bool atacandoConStand = AnubisEstaAtacando(Player);

            if (atacandoConStand)
            {
                float velocidadStand = Player.GetModPlayer<StandStatsPlayer>().standSpeed;
                float velocidadDeLaMano = 0.25f;

                if (velocidadStand >= 150f)
                {
                    velocidadDeLaMano = 0.40f;
                }
                else if (velocidadStand >= 100f)
                {
                    velocidadDeLaMano = 0.37f;
                }
                else if (velocidadStand >= 50f)
                {
                    velocidadDeLaMano = 0.30f;
                }
                else
                {
                    velocidadDeLaMano = 0.20f;
                }

                _manoAnimTimer += velocidadDeLaMano;

                int frameVisual = (int)_manoAnimTimer % 4;

                switch (frameVisual)
                {
                    case 0: Player.bodyFrame.Y = Player.bodyFrame.Height * 1; break;
                    case 1: Player.bodyFrame.Y = Player.bodyFrame.Height * 2; break;
                    case 2: Player.bodyFrame.Y = Player.bodyFrame.Height * 3; break;
                    case 3: Player.bodyFrame.Y = Player.bodyFrame.Height * 4; break;
                }
            }
            else
            {
                _manoAnimTimer = 0f;
            }
        }
    }
}