using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace Jojo.Content.Buffs.KingCrimson_Buffs
{
    public class EpitaphVision : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
        }
    }

    public class EpitaphEquipSystem : ModSystem
    {
        public static int EpitaphHeadSlot;
        public override void Load()
        {
            EpitaphHeadSlot = EquipLoader.AddEquipTexture(Mod, "Jojo/Content/Items/Armor/Epitafio", EquipType.Head, name: "Epitafio");
        }
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[EpitaphHeadSlot] = true;
            ArmorIDs.Head.Sets.IsTallHat[EpitaphHeadSlot] = true;
            ArmorIDs.Head.Sets.DrawFullHair[EpitaphHeadSlot] = true;
        }
    }

    public class EpitaphSoundPlayer : ModPlayer
    {
        private bool hadEpitaphLastFrame = false;

        public override void PostUpdateBuffs()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            bool hasEpitaph = Player.HasBuff(ModContent.BuffType<EpitaphVision>());
            if (hasEpitaph && !hadEpitaphLastFrame)
            {
                SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/Epitafio"));
            }
            hadEpitaphLastFrame = hasEpitaph;
        }

        public override void FrameEffects()
        {
            if (Player.HasBuff(ModContent.BuffType<EpitaphVision>()))
            {
                Player.head = EpitaphEquipSystem.EpitaphHeadSlot;
            }
        }
    }
}