using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Clases;

namespace Jojo.Content.Buffs.StarPlatinum_Buffs
{
    public class PlatinumRage_Tier_2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            Main.buffNoSave[Type] = false;
        }

        public override bool RightClick(int buffIndex)
        {
            return false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(ModContent.GetInstance<ClaseStand>()) += 0.1f;
        }
    }

    public class PlatinumRage_Tier_2_Player : ModPlayer
    {
        private bool soundPlayed;
        private bool wasActive;

        private bool musicStored;
        private float oldMusicVolume;

        public override void ResetEffects()
        {
            if (!Player.HasBuff(ModContent.BuffType<PlatinumRage_Tier_2>()))
            {
                soundPlayed = false;
            }
        }

        public override void PostUpdateBuffs()
        {
            bool active = Player.HasBuff(ModContent.BuffType<PlatinumRage_Tier_2>());

            // ===== SONIDO (solo una vez al activar) =====
            if (active && !wasActive)
            {
                if (!soundPlayed)
                {
                    soundPlayed = true;

                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/YareYareDaze_10Seconds")
                    {
                        Volume = 1f,
                        MaxInstances = 1,
                        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                    });
                }
            }

            // ===== MÚSICA (mute mientras el buff está activo) =====
            if (active)
            {
                if (!musicStored)
                {
                    oldMusicVolume = Main.musicVolume;
                    musicStored = true;
                }

                Main.musicVolume = 0f;
            }
            else
            {
                if (musicStored)
                {
                    Main.musicVolume = oldMusicVolume;
                    musicStored = false;
                }
            }

            wasActive = active;
        }
    }
}