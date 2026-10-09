using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Terraria.DataStructures;

namespace Jojo.Content.Projectiles.CrazyDiamond.Transformacion
{
    public class Roca : ModNPC
    {
        public override string Texture => "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_4/Transformacion/Roca";

        public ref float TrappedNPCIndex => ref NPC.ai[0];
        public ref float DefenseMultiplier => ref NPC.ai[1];
        public ref float Timer => ref NPC.ai[2];

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1000;
            NPC.life = 1000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.aiStyle = -1;
            NPC.friendly = false;
        }

        public override void AI()
        {
            if (NPC.velocity.Y == 0)
            {
                NPC.velocity.X *= 0.5f;
            }

            Timer++;

            int npcIdx = (int)TrappedNPCIndex;
            bool targetValid = npcIdx >= 0 && npcIdx < Main.maxNPCs && Main.npc[npcIdx].active;

            if (targetValid)
            {
                NPC target = Main.npc[npcIdx];

                NPC.lifeMax = target.lifeMax;
                NPC.life = target.life;
                NPC.boss = target.boss;

                if (target.type != NPCID.TargetDummy)
                {
                    target.Center = NPC.Center;
                }
            }
            else
            {
                // El NPC atrapado ya no existe (murió): solo el servidor / singleplayer decide.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ExplodeAndRelease();
                }
                return;
            }

            // Solo el servidor / singleplayer decide cuándo se acaba el tiempo.
            if (Timer >= 300 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ExplodeAndRelease();
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            float mult = DefenseMultiplier > 0f ? DefenseMultiplier : 0.5f;
            modifiers.FinalDamage *= mult;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ForwardDamageToTrappedNPC(damageDone);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            ForwardDamageToTrappedNPC(damageDone);
        }

        private void ForwardDamageToTrappedNPC(int damage)
        {
            // Esto se ejecuta en el cliente que golpea (o en singleplayer).
            if (Main.netMode == NetmodeID.Server) return;

            int npcIdx = (int)TrappedNPCIndex;
            if (npcIdx >= 0 && npcIdx < Main.maxNPCs && Main.npc[npcIdx].active)
            {
                NPC target = Main.npc[npcIdx];

                NPC.HitInfo hit = new NPC.HitInfo
                {
                    Damage = damage,
                    Knockback = 0f,
                    HitDirection = 0,
                    Crit = false
                };

                target.StrikeNPC(hit);
                NPC.life = target.life;

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendStrikeNPC(target, hit);
                }

                // En multijugador el servidor se encarga de liberar/eliminar la roca.
                if ((target.life <= 0 || !target.active) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ExplodeAndRelease();
                }
            }
        }

        // Efectos visuales/sonoros de romperse la roca (no hace nada en el servidor dedicado).
        public static void PlayBreakEffects(Vector2 center, int width, int height, IEntitySource source)
        {
            if (Main.dedServ) return;

            SoundEngine.PlaySound(SoundID.Item14, center);
            SoundEngine.PlaySound(SoundID.Dig, center);

            Vector2 pos = center - new Vector2(width / 2f, height / 2f);

            for (int i = 0; i < 45; i++)
            {
                Dust.NewDust(pos, width, height, DustID.Stone,
                    Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f), Scale: 1.6f);
            }

            for (int i = 0; i < 50; i++)
            {
                Dust d = Dust.NewDustDirect(pos, width, height, DustID.Smoke,
                    Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 100, default, 2.2f);
                d.noGravity = true;
            }

            for (int g = 0; g < 8; g++)
            {
                Gore.NewGore(source, center, new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f)), Main.rand.Next(61, 64), 1.2f);
            }
        }

        private void ExplodeAndRelease()
        {
            // Solo servidor / singleplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int npcIdx = (int)TrappedNPCIndex;
            bool released = false;

            if (npcIdx >= 0 && npcIdx < Main.maxNPCs && Main.npc[npcIdx].active)
            {
                if (Main.npc[npcIdx].TryGetGlobalNPC(out TransformacionGlobalNPC gNPC))
                {
                    // ReleaseFromRock ya reproduce los efectos (singleplayer).
                    gNPC.ReleaseFromRock(Main.npc[npcIdx]);
                    released = true;
                }
            }

            if (!released)
            {
                PlayBreakEffects(NPC.Center, NPC.width, NPC.height, NPC.GetSource_Death());
            }

            NPC.active = false;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }
    }
}