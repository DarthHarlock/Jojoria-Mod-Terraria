using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ID;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariot_Buffs;

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

using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_1;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_2;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_4;

using Jojo.Content.Projectiles.HGreen.HGreen_Tier_1;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_3;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4;

using Jojo.Content.Projectiles.D4C.D4C_Tier_1;
using Jojo.Content.Projectiles.D4C.D4C_Tier_2;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3;
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;

using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;

using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_2;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_3;

using Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final;

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

namespace Jojo.Content.UI
{
    public static class JojoPackets
    {
        public const byte SkinSync = 1;
    }

    public class StandSlotPlayer : ModPlayer
    {
        public Item standItem = new Item();
        public Item skinItem = new Item();

        public override void Initialize()
        {
            standItem = new Item();
            standItem.TurnToAir();
            skinItem = new Item();
            skinItem.TurnToAir();
        }

        public override void SaveData(TagCompound tag)
        {
            if (standItem != null && !standItem.IsAir)
                tag["standSlot"] = standItem;
            if (skinItem != null && !skinItem.IsAir)
                tag["skinSlot"] = skinItem;
        }

        public override void LoadData(TagCompound tag)
        {
            standItem = new Item();
            if (tag.ContainsKey("standSlot"))
            {
                Item loaded = tag.Get<Item>("standSlot");
                if (StandSlotSystem.IsStandItem(loaded))
                    standItem = loaded;
                else
                    standItem.TurnToAir();
            }
            else
            {
                standItem.TurnToAir();
            }

            skinItem = new Item();
            if (tag.ContainsKey("skinSlot"))
            {
                Item loadedSkin = tag.Get<Item>("skinSlot");
                if (StandSlotSystem.IsSkinItem(loadedSkin))
                    skinItem = loadedSkin;
                else
                    skinItem.TurnToAir();
            }
            else
            {
                skinItem.TurnToAir();
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            StandSlotSystem.SendSkinSync(Player.whoAmI, toWho);
        }
    }

    public class StandSlotSystem : ModSystem
    {
        private static StandSlotPlayer GetSlotPlayer(Player p) =>
            p.GetModPlayer<StandSlotPlayer>();

        private static StandSlotPlayer LocalSlotPlayer =>
            Main.LocalPlayer.GetModPlayer<StandSlotPlayer>();

        public static bool HasStand => HasStandFor(Main.LocalPlayer);
        public static bool HasSkin => HasSkinFor(Main.LocalPlayer);
        public static bool HasMeguminSkin => HasMeguminSkinFor(Main.LocalPlayer);
        public static bool HasOVASkin => HasOVASkinFor(Main.LocalPlayer);
        public static bool HasRedSkin => HasRedSkinFor(Main.LocalPlayer);
        public static bool HasGreenSkin => HasGreenSkinFor(Main.LocalPlayer);
        public static bool HasBlueSkin => HasBlueSkinFor(Main.LocalPlayer);
        public static bool HasGoldenSkin => HasGoldenSkinFor(Main.LocalPlayer);
        public static bool HasBlackCrimsonSkin => HasBlackCrimsonSkinFor(Main.LocalPlayer);

        // ── skins de The World ──
        public static bool HasTheWorldBlueSkin => HasTheWorldBlueSkinFor(Main.LocalPlayer);
        public static bool HasTheWorldRedSkin => HasTheWorldRedSkinFor(Main.LocalPlayer);
        public static bool HasTheWorldGreenSkin => HasTheWorldGreenSkinFor(Main.LocalPlayer);

        // ── skins de Star Platinum ──
        public static bool HasStarPlatinumRedSkin => HasStarPlatinumRedSkinFor(Main.LocalPlayer);
        public static bool HasStarPlatinumGreenSkin => HasStarPlatinumGreenSkinFor(Main.LocalPlayer);
        public static bool HasStarPlatinumBlueSkin => HasStarPlatinumBlueSkinFor(Main.LocalPlayer);

        // ── skins de Killer Queen ──
        public static bool HasKillerQueenRedSkin => HasKillerQueenRedSkinFor(Main.LocalPlayer);
        public static bool HasKillerQueenGreenSkin => HasKillerQueenGreenSkinFor(Main.LocalPlayer);
        public static bool HasKillerQueenBlueSkin => HasKillerQueenBlueSkinFor(Main.LocalPlayer);

        // ── skins de Magicians Red ──          
        public static bool HasMagiciansRedPinkSkin => HasMagiciansRedPinkSkinFor(Main.LocalPlayer);
        public static bool HasMagiciansRedGreenSkin => HasMagiciansRedGreenSkinFor(Main.LocalPlayer);
        public static bool HasMagiciansRedBlueSkin => HasMagiciansRedBlueSkinFor(Main.LocalPlayer);



        public static bool HasStandFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.standItem != null && !sp.standItem.IsAir && IsStandItem(sp.standItem);
        }

        public static bool HasSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir && IsSkinItem(sp.skinItem);
        }

        public static bool HasMeguminSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "MeguminSkin";
        }

        public static bool HasOVASkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "OVASkin";
        }

        public static bool HasRedSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "RedSkin";
        }

        public static bool HasGreenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "GreenSkin";
        }

        public static bool HasBlueSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "BlueSkin";
        }

        public static bool HasGoldenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "GoldenSkin";
        }

        public static bool HasBlackCrimsonSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "BlackCrimsonSkin";
        }

        // ── skins de The World ──
        public static bool HasTheWorldBlueSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "TheWorld_Blue";
        }

        public static bool HasTheWorldRedSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "TheWorld_Red";
        }

        public static bool HasTheWorldGreenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "TheWorld_Green";
        }

        // ── skins de Star Platinum ──
        public static bool HasStarPlatinumRedSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "StarPlatinum_Red";
        }

        public static bool HasStarPlatinumGreenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "StarPlatinum_Green";
        }

        public static bool HasStarPlatinumBlueSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "StarPlatinum_Blue";
        }

        // ── skins de Killer Queen ──
        public static bool HasKillerQueenRedSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "KillerQueen_Red";
        }

        public static bool HasKillerQueenGreenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "KillerQueen_Green";
        }

        public static bool HasKillerQueenBlueSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "KillerQueen_Blue";
        }

        // ── skins de Magicians Red 
        public static bool HasMagiciansRedPinkSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "MagiciansRed_Pink";
        }

        public static bool HasMagiciansRedGreenSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "MagiciansRed_Green";
        }

        public static bool HasMagiciansRedBlueSkinFor(Player p)
        {
            var sp = GetSlotPlayer(p);
            return sp.skinItem != null && !sp.skinItem.IsAir &&
                   sp.skinItem.ModItem != null && sp.skinItem.ModItem.Name == "MagiciansRed_Blue";
        }

        public static bool IsStandItem(Item item)
        {
            if (item == null || item.IsAir || item.ModItem == null) return false;
            string name = item.ModItem.Name;
            return name == "PLATINUM" ||
                   name == "RikaItem" ||

                   name == "JusticeItem_Tier_4" ||

                   name == "CinderellaItem_Tier_1" ||
                   name == "CinderellaItem_Tier_2" ||
                   name == "CinderellaItem_Tier_3" ||
                   name == "CinderellaItem_Tier_4" ||

                   name == "WeatherReportItem_Tier_1" ||
                   name == "WeatherReportItem_Tier_2" ||
                   name == "WeatherReportItem_Tier_3" ||
                   name == "WeatherReportItem_Tier_4" ||

                   name == "CrazyDiamondItem_Tier_1" ||
                   name == "CrazyDiamondItem_Tier_2" ||
                   name == "CrazyDiamondItem_Tier_3" ||
                   name == "CrazyDiamondItem_Tier_4" ||

                   name == "MRedItem_Tier_1" ||
                   name == "MRedItem_Tier_2" ||
                   name == "MRedItem_Tier_3" ||
                   name == "MRedItem_Tier_4" ||

                   name == "GoldenItem_Tier_1" ||
                   name == "GoldenItem_Tier_2" ||
                   name == "GoldenItem_Tier_3" ||
                   name == "GoldenItem_Tier_4" ||

                   name == "GoldenItem_Requiem" ||

                   name == "TuskItem_Tier_1" ||

                   name == "ScaryMonstersItem_Tier_1" ||
                   name == "ScaryMonstersItem_Tier_2" ||
                   name == "ScaryMonstersItem_Tier_3" ||
                   name == "ScaryMonstersItem_Tier_4" ||

                   name == "HGreenItem_Tier_1" ||
                   name == "HGreenItem_Tier_2" ||
                   name == "HGreenItem_Tier_3" ||
                   name == "HGreenItem_Tier_4" ||

                   name == "D4CItem_Tier_1" ||
                   name == "D4CItem_Tier_2" ||
                   name == "D4CItem_Tier_3" ||
                   name == "D4CItem_Tier_4" ||

                   name == "KingCrimsonItem_Tier_1" ||
                   name == "KingCrimsonItem_Tier_2" ||
                   name == "KingCrimsonItem_Tier_3" ||
                   name == "KingCrimsonItem_Tier_4" ||

                   name == "SilverChariotItem_Tier_1" ||
                   name == "SilverChariotItem_Tier_2" ||
                   name == "SilverChariotItem_Tier_3" ||
                   name == "SilverChariotItem_Tier_4" ||
                   name == "SilverChariotRequiemItem_Requiem" ||

                   name == "AnubisItem_Tier_1" ||
                   name == "AnubisItem_Tier_2" ||
                   name == "AnubisItem_Tier_3" ||
                   name == "AnubisItem_Tier_4" ||

                   name == "StarPlatinumItem_Tier_1" ||
                   name == "StarPlatinumItem_Tier_2" ||
                   name == "StarPlatinumItem_Tier_3" ||
                   name == "StarPlatinumItem_Tier_4" ||

                   name == "TheWorldItem_Tier_1" ||
                   name == "TheWorldItem_Tier_2" ||
                   name == "TheWorldItem_Tier_3" ||
                   name == "TheWorldItem_Tier_4" ||

                   name == "WhiteSnakeItem_Tier_1" ||
                   name == "WhiteSnakeItem_Tier_2" ||
                   name == "WhiteSnakeItem_Tier_3" ||
                   name == "WhiteSnakeItem_Tier_4" ||

                   name == "CMoonItem_Tier_1" ||
                   name == "CMoonItem_Tier_2" ||
                   name == "CMoonItem_Tier_3" ||

                   name == "MadeInHeavenItem" ||

                   name == "KillerQueenItem_Tier_1" ||
                   name == "KillerQueenItem_Tier_2" ||
                   name == "KillerQueenItem_Tier_3" ||
                   name == "KillerQueenItem_Tier_4";
        }

        public static bool IsSkinItem(Item item)
        {
            if (item == null || item.IsAir || item.ModItem == null) return false;
            string name = item.ModItem.Name;
            return name == "MeguminSkin" ||
                   name == "OVASkin" ||
                   name == "RedSkin" ||
                   name == "GreenSkin" ||
                   name == "BlueSkin" ||
                   name == "GoldenSkin" ||
                   name == "BlackCrimsonSkin" ||
                   name == "TheWorld_Blue" ||
                   name == "TheWorld_Red" ||
                   name == "TheWorld_Green" ||
                   name == "StarPlatinum_Red" ||
                   name == "StarPlatinum_Green" ||
                   name == "StarPlatinum_Blue" ||
                   name == "KillerQueen_Red" ||
                   name == "KillerQueen_Green" ||
                   name == "KillerQueen_Blue" ||
                   name == "MagiciansRed_Pink" ||      
                   name == "MagiciansRed_Green" ||     
                   name == "MagiciansRed_Blue";       
        }

        public static int GetStandProjectileType() =>
            GetStandProjectileTypeFor(Main.LocalPlayer);

        public static int GetStandProjectileTypeFor(Player p)
        {
            Item storedItem = GetSlotPlayer(p).standItem;
            if (storedItem == null || storedItem.IsAir || storedItem.ModItem == null) return 0;
            string name = storedItem.ModItem.Name;
            if (name == "RikaItem") return ModContent.ProjectileType<RIKASTAND>();

            if (name == "SilverChariotItem_Tier_1") return ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>();
            if (name == "SilverChariotItem_Tier_2") return ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_2>();
            if (name == "SilverChariotItem_Tier_3") return ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>();
            if (name == "SilverChariotItem_Tier_4") return ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>();
            if (name == "SilverChariotRequiemItem_Requiem") return ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>();

            if (name == "KingCrimsonItem_Tier_1") return ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_1>();
            if (name == "KingCrimsonItem_Tier_2") return ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_2>();
            if (name == "KingCrimsonItem_Tier_3") return ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_3>();
            if (name == "KingCrimsonItem_Tier_4") return ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_4>();

            if (name == "JusticeItem_Tier_4") return ModContent.ProjectileType<JUSTICESTAND_Tier_4>();

            if (name == "TuskItem_Tier_1") return ModContent.ProjectileType<TUSKSTAND_Tier_1>();

            if (name == "ScaryMonstersItem_Tier_1") return ModContent.ProjectileType<MONSTERSTAND_Tier_1>();
            if (name == "ScaryMonstersItem_Tier_2") return ModContent.ProjectileType<MONSTERSTAND_Tier_2>();
            if (name == "ScaryMonstersItem_Tier_3") return ModContent.ProjectileType<MONSTERSTAND_Tier_3>();
            if (name == "ScaryMonstersItem_Tier_4") return ModContent.ProjectileType<MONSTERSTAND_Tier_4>();

            if (name == "CrazyDiamondItem_Tier_1") return ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_1>();
            if (name == "CrazyDiamondItem_Tier_2") return ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_2>();
            if (name == "CrazyDiamondItem_Tier_3") return ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_3>();
            if (name == "CrazyDiamondItem_Tier_4") return ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_4>();

            if (name == "CinderellaItem_Tier_1") return ModContent.ProjectileType<CINDERELLASTAND_Tier_1>();
            if (name == "CinderellaItem_Tier_2") return ModContent.ProjectileType<CINDERELLASTAND_Tier_2>();
            if (name == "CinderellaItem_Tier_3") return ModContent.ProjectileType<CINDERELLASTAND_Tier_3>();
            if (name == "CinderellaItem_Tier_4") return ModContent.ProjectileType<CINDERELLASTAND_Tier_4>();

            if (name == "GoldenItem_Tier_1") return ModContent.ProjectileType<GOLDENSTAND_Tier_1>();
            if (name == "GoldenItem_Tier_2") return ModContent.ProjectileType<GOLDENSTAND_Tier_2>();
            if (name == "GoldenItem_Tier_3") return ModContent.ProjectileType<GOLDENSTAND_Tier_3>();
            if (name == "GoldenItem_Tier_4") return ModContent.ProjectileType<GOLDENSTAND_Tier_4>();

            if (name == "GoldenItem_Requiem") return ModContent.ProjectileType<GOLDENSTAND_Requiem>();

            if (name == "WeatherReportItem_Tier_1") return ModContent.ProjectileType<WEATHERSTAND_Tier_1>();
            if (name == "WeatherReportItem_Tier_2") return ModContent.ProjectileType<WEATHERSTAND_Tier_2>();
            if (name == "WeatherReportItem_Tier_3") return ModContent.ProjectileType<WEATHERSTAND_Tier_3>();
            if (name == "WeatherReportItem_Tier_4") return ModContent.ProjectileType<WEATHERSTAND_Tier_4>();

            if (name == "HGreenItem_Tier_1") return ModContent.ProjectileType<HGREENSTAND_Tier_1>();
            if (name == "HGreenItem_Tier_2") return ModContent.ProjectileType<HGREENSTAND_Tier_2>();
            if (name == "HGreenItem_Tier_3") return ModContent.ProjectileType<HGREENSTAND_Tier_3>();
            if (name == "HGreenItem_Tier_4") return ModContent.ProjectileType<HGREENSTAND_Tier_4>();

            if (name == "D4CItem_Tier_1") return ModContent.ProjectileType<D4CSTAND_Tier_1>();
            if (name == "D4CItem_Tier_2") return ModContent.ProjectileType<D4CSTAND_Tier_2>();
            if (name == "D4CItem_Tier_3") return ModContent.ProjectileType<D4CSTAND_Tier_3>();
            if (name == "D4CItem_Tier_4") return ModContent.ProjectileType<D4CSTAND_Tier_4>();

            if (name == "MRedItem_Tier_1") return ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_1>();
            if (name == "MRedItem_Tier_2") return ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_2>();
            if (name == "MRedItem_Tier_3") return ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_3>();
            if (name == "MRedItem_Tier_4") return ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_4>();

            if (name == "WhiteSnakeItem_Tier_1") return ModContent.ProjectileType<WHITESNAKESTAND_Tier_1>();
            if (name == "WhiteSnakeItem_Tier_2") return ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>();
            if (name == "WhiteSnakeItem_Tier_3") return ModContent.ProjectileType<WHITESNAKESTAND_Tier_3>();
            if (name == "WhiteSnakeItem_Tier_4") return ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>();

            if (name == "CMoonItem_Tier_1") return ModContent.ProjectileType<CMOONSTAND_Tier_1>();
            if (name == "CMoonItem_Tier_2") return ModContent.ProjectileType<CMOONSTAND_Tier_2>();
            if (name == "CMoonItem_Tier_3") return ModContent.ProjectileType<CMOONSTAND_Tier_3>();

            if (name == "MadeInHeavenItem") return ModContent.ProjectileType<MADEINHEAVENSTAND>();

            if (name == "AnubisItem_Tier_1") return ModContent.ProjectileType<ANUBISSTAND_Tier_1>();
            if (name == "AnubisItem_Tier_2") return ModContent.ProjectileType<ANUBISSTAND_Tier_2>();
            if (name == "AnubisItem_Tier_3") return ModContent.ProjectileType<ANUBISSTAND_Tier_3>();
            if (name == "AnubisItem_Tier_4") return ModContent.ProjectileType<ANUBISSTAND_Tier_4>();

            if (name == "StarPlatinumItem_Tier_1") return ModContent.ProjectileType<STARPLATINUMSTAND_Tier_1>();
            if (name == "StarPlatinumItem_Tier_2") return ModContent.ProjectileType<STARPLATINUMSTAND_Tier_2>();
            if (name == "StarPlatinumItem_Tier_3") return ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>();
            if (name == "StarPlatinumItem_Tier_4") return ModContent.ProjectileType<STARPLATINUMSTAND_Tier_4>();

            if (name == "TheWorldItem_Tier_1") return ModContent.ProjectileType<THEWORLDSTAND_Tier_1>();
            if (name == "TheWorldItem_Tier_2") return ModContent.ProjectileType<THEWORLDSTAND_Tier_2>();
            if (name == "TheWorldItem_Tier_3") return ModContent.ProjectileType<THEWORLDSTAND_Tier_3>();
            if (name == "TheWorldItem_Tier_4") return ModContent.ProjectileType<THEWORLDSTAND_Tier_4>();

            if (name == "KillerQueenItem_Tier_1") return ModContent.ProjectileType<KILLERQUEENSTAND_Tier_1>();
            if (name == "KillerQueenItem_Tier_2") return ModContent.ProjectileType<KILLERQUEENSTAND_Tier_2>();
            if (name == "KillerQueenItem_Tier_3") return ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>();
            if (name == "KillerQueenItem_Tier_4") return ModContent.ProjectileType<KILLERQUEENSTAND_Tier_4>();
            return 0;
        }

        public static void SendSkinSync(int playerWho, int toWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;
            Player p = Main.player[playerWho];
            var sp = p.GetModPlayer<StandSlotPlayer>();
            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(JojoPackets.SkinSync);
            packet.Write((byte)playerWho);
            bool hasSkin = sp.skinItem != null && !sp.skinItem.IsAir && sp.skinItem.ModItem != null;
            packet.Write(hasSkin);
            if (hasSkin) packet.Write(sp.skinItem.ModItem.Name);
            if (Main.netMode == NetmodeID.MultiplayerClient) packet.Send();
            else packet.Send(toWho);
        }

        public static void HandlePacketAfterType(byte packetId, System.IO.BinaryReader reader, int fromWho)
        {
            if (packetId == JojoPackets.SkinSync)
            {
                int playerWho = reader.ReadByte();
                bool hasSkin = reader.ReadBoolean();
                string skinName = hasSkin ? reader.ReadString() : "";
                if (Main.netMode == NetmodeID.Server)
                {
                    Player p2 = Main.player[playerWho];
                    var sp2 = p2.GetModPlayer<StandSlotPlayer>();
                    ApplySkinName(sp2, skinName);
                    ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                    packet.Write(JojoPackets.SkinSync);
                    packet.Write((byte)playerWho);
                    packet.Write(hasSkin);
                    if (hasSkin) packet.Write(skinName);
                    packet.Send(-1, fromWho);
                    return;
                }
                if (playerWho >= 0 && playerWho < Main.maxPlayers)
                {
                    Player p3 = Main.player[playerWho];
                    var sp3 = p3.GetModPlayer<StandSlotPlayer>();
                    ApplySkinName(sp3, skinName);
                }
            }
        }

        private static void ApplySkinName(StandSlotPlayer sp, string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                sp.skinItem = new Item();
                sp.skinItem.TurnToAir();
                return;
            }

            // OPTIMIZACIÓN: Se reemplazó el bucle foreach masivo por un TryFind directo libre de lag.
            if (ModContent.TryFind<ModItem>("Jojo", skinName, out var modItem))
            {
                sp.skinItem = new Item();
                sp.skinItem.SetDefaults(modItem.Type);
                return;
            }

            sp.skinItem = new Item();
            sp.skinItem.TurnToAir();
        }

        public override void PreUpdatePlayers()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;
            int currentExpectedStandProj = GetStandProjectileTypeFor(p);
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == p.whoAmI &&
                    (proj.type == ModContent.ProjectileType<RIKASTAND>() ||

                    proj.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_1>() ||
                    proj.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_2>() ||
                    proj.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<KINGCRIMSONSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<JUSTICESTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<TUSKSTAND_Tier_1>() ||

                     proj.type == ModContent.ProjectileType<MONSTERSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<CRAZYDIAMONDSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<CINDERELLASTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<GOLDENSTAND_Requiem>() ||

                     proj.type == ModContent.ProjectileType<WEATHERSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<WEATHERSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<WEATHERSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<WEATHERSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<HGREENSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<HGREENSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<HGREENSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<HGREENSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<D4CSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<D4CSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<D4CSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<D4CSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<MAGICIANSREDSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<CMOONSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<CMOONSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<CMOONSTAND_Tier_3>() ||

                     proj.type == ModContent.ProjectileType<MADEINHEAVENSTAND>() ||

                     proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>() ||
                     proj.type == ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>() ||

                     proj.type == ModContent.ProjectileType<ANUBISSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<ANUBISSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<ANUBISSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<ANUBISSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_4>() ||

                     proj.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_1>() ||
                     proj.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_2>() ||
                     proj.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>() ||
                     proj.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_4>()))
                {
                    if (proj.type != currentExpectedStandProj)
                        proj.Kill();
                }
            }
        }

        private static Vector2 standSlotOffset = new Vector2(-380f, -20f);
        private static Vector2 skinSlotOffset = new Vector2(-320f, -20f);

        public override void Load() { }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (index != -1)
                layers.Insert(index + 1, new LegacyGameInterfaceLayer("Jojo: Stand Slot", DrawSlots, InterfaceScaleType.UI));
        }

        private bool DrawSlots()
        {
            if (!Main.playerInventory) return true;

            Item storedItem = LocalSlotPlayer.standItem;
            Item storedSkinItem = LocalSlotPlayer.skinItem;
            Texture2D slotTex = TextureAssets.InventoryBack.Value;

            Vector2 standPosition = new Vector2(Main.screenWidth / 2f + standSlotOffset.X, Main.screenHeight / 2f + standSlotOffset.Y);
            Vector2 skinPosition = new Vector2(Main.screenWidth / 2f + skinSlotOffset.X, Main.screenHeight / 2f + skinSlotOffset.Y);

            Rectangle standRect = new Rectangle((int)standPosition.X, (int)standPosition.Y, 52, 52);
            Rectangle skinRect = new Rectangle((int)skinPosition.X, (int)skinPosition.Y, 52, 52);

            // ───────────────────────────────────────────────────────
            //  ¿El slot de Stand está bloqueado por Time Erase?
            // ───────────────────────────────────────────────────────
            bool standLockedByTimeErase = KingCrimsonTimeTracker_Tier_4.IsStandLockedFor(Main.myPlayer);

            Color standSlotColor = standLockedByTimeErase ? Color.Red * 0.6f : Color.Yellow * 0.4f;
            Main.spriteBatch.Draw(slotTex, standPosition, standSlotColor);
            Main.spriteBatch.Draw(slotTex, skinPosition, Color.HotPink * 0.55f);

            Item mouseItem = Main.mouseItem;
            bool timeStopped = Main.LocalPlayer.HasBuff(ModContent.BuffType<TimeStoped>());
            bool hasSilverClones = Main.LocalPlayer.HasBuff(ModContent.BuffType<SilverClones>());

            bool hoveringStand = standRect.Contains(Main.MouseScreen.ToPoint());
            if (hoveringStand && !storedItem.IsAir)
            {
                Main.hoverItemName = storedItem.Name;
                Main.HoverItem = storedItem.Clone();
            }
            if (hoveringStand)
            {
                Main.LocalPlayer.mouseInterface = true;

                if (standLockedByTimeErase && !storedItem.IsAir)
                {
                    Main.instance.MouseText("¡No puedes cambiar de Stand mientras Time Erase esté activo!");
                }

                if (!timeStopped && !hasSilverClones && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    bool slotHasStand = !storedItem.IsAir;
                    bool mouseHasStand = !mouseItem.IsAir && IsStandItem(mouseItem);

                    // Solo bloqueamos si YA hay un stand puesto y el jugador intenta
                    // sacarlo (mano vacía) o cambiarlo por otro (mano con stand distinto)
                    bool intentaQuitarOCambiar = slotHasStand && (mouseItem.IsAir || mouseHasStand);
                    bool bloqueado = intentaQuitarOCambiar && standLockedByTimeErase;

                    if (bloqueado)
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose, Main.LocalPlayer.position);
                        Main.NewText("¡No puedes cambiar de Stand mientras Time Erase esté activo!", 255, 90, 90);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick, Main.LocalPlayer.position);
                        if (!slotHasStand)
                        {
                            if (mouseHasStand)
                            {
                                LocalSlotPlayer.standItem = mouseItem.Clone();
                                mouseItem.TurnToAir();
                                SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                            }
                        }
                        else if (mouseItem.IsAir)
                        {
                            mouseItem.SetDefaults(storedItem.type);
                            mouseItem.stack = storedItem.stack;
                            LocalSlotPlayer.standItem.TurnToAir();
                            SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                        }
                        else if (mouseHasStand)
                        {
                            Item temp = storedItem.Clone();
                            LocalSlotPlayer.standItem = mouseItem.Clone();
                            mouseItem = temp;
                            Main.mouseItem = mouseItem;
                            SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                        }
                    }
                }
            }

            bool hoveringSkin = skinRect.Contains(Main.MouseScreen.ToPoint());
            if (hoveringSkin && !storedSkinItem.IsAir)
            {
                Main.hoverItemName = storedSkinItem.Name;
                Main.HoverItem = storedSkinItem.Clone();
            }
            if (hoveringSkin)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!timeStopped && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick, Main.LocalPlayer.position);
                    bool mouseHasSkin = !mouseItem.IsAir && IsSkinItem(mouseItem);
                    bool slotHasSkin = !storedSkinItem.IsAir;
                    bool changed = false;
                    if (!slotHasSkin)
                    {
                        if (mouseHasSkin)
                        {
                            LocalSlotPlayer.skinItem = mouseItem.Clone();
                            mouseItem.TurnToAir();
                            SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                            changed = true;
                        }
                    }
                    else if (mouseItem.IsAir)
                    {
                        mouseItem.SetDefaults(storedSkinItem.type);
                        mouseItem.stack = storedSkinItem.stack;
                        LocalSlotPlayer.skinItem.TurnToAir();
                        SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                        changed = true;
                    }
                    else if (mouseHasSkin)
                    {
                        Item temp = storedSkinItem.Clone();
                        LocalSlotPlayer.skinItem = mouseItem.Clone();
                        mouseItem = temp;
                        Main.mouseItem = mouseItem;
                        SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.position);
                        changed = true;
                    }
                    if (changed) SendSkinSync(Main.myPlayer);
                }
            }

            if (!LocalSlotPlayer.standItem.IsAir)
            {
                Main.instance.LoadItem(LocalSlotPlayer.standItem.type);
                Texture2D itemTex = TextureAssets.Item[LocalSlotPlayer.standItem.type].Value;
                Vector2 origin = itemTex.Size() / 2f;
                Vector2 slotCenter = standPosition + new Vector2(26f, 26f);
                float scale = 2f;
                if (itemTex.Width > 32 || itemTex.Height > 32)
                    scale = (itemTex.Width > itemTex.Height ? 32f / itemTex.Width : 32f / itemTex.Height) * 2f;
                Main.spriteBatch.Draw(itemTex, slotCenter, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            if (!LocalSlotPlayer.skinItem.IsAir)
            {
                Main.instance.LoadItem(LocalSlotPlayer.skinItem.type);
                Texture2D itemTex = TextureAssets.Item[LocalSlotPlayer.skinItem.type].Value;
                Vector2 origin = itemTex.Size() / 2f;
                Vector2 slotCenter = skinPosition + new Vector2(26f, 26f);
                float scale = 2f;
                if (itemTex.Width > 32 || itemTex.Height > 32)
                    scale = (itemTex.Width > itemTex.Height ? 32f / itemTex.Width : 32f / itemTex.Height) * 2f;
                Main.spriteBatch.Draw(itemTex, slotCenter, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            return true;
        }

        public override void OnWorldUnload() { }
 
   }


}

