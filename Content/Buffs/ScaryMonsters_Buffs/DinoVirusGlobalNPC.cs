using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;
using Jojo.Content.Buffs.ScaryMonsters_Buffs;
using Jojo.Content.NPCs.ScaryMonsters.MinionDinosaurios;

namespace Jojo.Systems
{
    public class DinoVirusGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool infected = false;
        public int virusTimer = 0;
        public int infectedByPlayer = -1;

        // Tier del stand que infectó a este NPC (3, 4...). Define las estadísticas del dino resultante.
        public int infectedByTier = 0;

        // ================= SINCRONIZACIÓN MULTIJUGADOR =================
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(infectedByPlayer);
            binaryWriter.Write(virusTimer);
            binaryWriter.Write(infectedByTier);
            bitWriter.WriteBit(infected);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            infectedByPlayer = binaryReader.ReadInt32();
            virusTimer = binaryReader.ReadInt32();
            infectedByTier = binaryReader.ReadInt32();
            infected = bitReader.ReadBit();
        }
        // ===============================================================

        public static bool PuedeInfectar(NPC npc)
        {
            if (npc == null || !npc.active) return false;

            if (npc.boss) return false;
            if (NPCID.Sets.ShouldBeCountedAsBoss[npc.type]) return false;
            if (npc.type == NPCID.WyvernHead || npc.type == NPCID.WyvernBody ||
                npc.type == NPCID.WyvernBody2 || npc.type == NPCID.WyvernBody3 ||
                npc.type == NPCID.WyvernLegs || npc.type == NPCID.WyvernTail)
                return false;

            if (npc.type == NPCID.TargetDummy) return false;
            if (npc.townNPC) return false;
            if (npc.dontTakeDamage) return false;
            if (npc.lifeMax <= 0) return false;

            return !npc.friendly || npc.CountsAsACritter;
        }

        /// <summary>
        /// Infecta usando el tier del stand activo del dueño. Los stands y garras existentes
        /// siguen llamando a Infectar(target, owner) sin cambios.
        /// </summary>
        public static void Infectar(NPC npc, int owner)
        {
            int tier = 0;
            if (owner >= 0 && owner < Main.maxPlayers)
                tier = ScaryTiers.GetActiveTier(Main.player[owner]);

            Infectar(npc, owner, tier);
        }

        /// <summary>Infecta indicando explícitamente el tier del stand que lo hace.</summary>
        public static void Infectar(NPC npc, int owner, int tier)
        {
            if (!PuedeInfectar(npc)) return;

            DinoVirusGlobalNPC g = npc.GetGlobalNPC<DinoVirusGlobalNPC>();

            // El dueño (y el tier) son los del PRIMERO que lo infectó
            if (!g.infected)
            {
                g.infectedByPlayer = owner;
                g.infectedByTier = tier;
                g.virusTimer = 0;
                g.infected = true;
                npc.netUpdate = true;
            }

            int buffType = ModContent.BuffType<DinoVirus>();
            if (!npc.HasBuff(buffType))
            {
                ScaryProfile perfil = ScaryTiers.GetProfile(g.infectedByTier);
                int ticksEfecto = (int)(perfil.DinoVirusDuracionEfecto * 60f);
                npc.AddBuff(buffType, ticksEfecto);
            }
        }

        public override void ResetEffects(NPC npc)
        {
            if (!npc.HasBuff(ModContent.BuffType<DinoVirus>()))
            {
                infected = false;
                virusTimer = 0;
                infectedByTier = 0;
            }
        }

        public override void AI(NPC npc)
        {
            int buffType = ModContent.BuffType<DinoVirus>();
            if (!npc.HasBuff(buffType)) return;

            if (!PuedeInfectar(npc))
            {
                int idx = npc.FindBuffIndex(buffType);
                if (idx >= 0) npc.DelBuff(idx);
                infected = false;
                virusTimer = 0;
                infectedByTier = 0;
                return;
            }

            infected = true;
            virusTimer++;

            ScaryProfile perfil = ScaryTiers.GetProfile(infectedByTier);
            int ticksTransformar = (int)(perfil.DinoVirusTiempoTransformar * 60f);
            if (virusTimer >= ticksTransformar)
            {
                TransformarEnDinosaurio(npc);
            }
        }

        public void TransformarEnDinosaurio(NPC npc)
        {
            // --- EXPLOSIÓN VISUAL Y SONIDO (local en cada cliente) ---
            SoundEngine.PlaySound(SoundID.Item14, npc.Center);

            for (int i = 0; i < 40; i++)
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.UltraBrightTorch, 0f, 0f, 100, default, 1.8f);
                d.noGravity = true;
                d.velocity *= 3.5f;
            }
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.UltraBrightTorch, 0f, 0f, 100, default, 1.2f);
                d.velocity *= 1.5f;
            }

            // Solo el servidor / singleplayer crea el dino
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int ownerIndex = (infectedByPlayer >= 0 && infectedByPlayer < Main.maxPlayers) ? infectedByPlayer : 0;
            Player owner = Main.player[ownerIndex];

            // Estadísticas según el tier del stand que infectó
            int tier = infectedByTier != 0 ? infectedByTier : ScaryTiers.GetActiveTier(owner);
            ScaryProfile perfil = ScaryTiers.GetProfile(tier);

            int vidaTotalDino = npc.lifeMax + perfil.TransformedDinoBonusVida;
            int defensaJugador = owner.statDefense;
            int npcType = ModContent.NPCType<TransformedDinoNPC>();

            int newNpcIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, npcType);

            if (newNpcIndex < Main.maxNPCs)
            {
                NPC newNpc = Main.npc[newNpcIndex];
                newNpc.lifeMax = vidaTotalDino;
                newNpc.life = vidaTotalDino;
                newNpc.defense = defensaJugador;
                newNpc.damage = perfil.TransformedDinoDano;
                newNpc.ai[0] = ownerIndex;
                newNpc.ai[1] = tier; // el dino recuerda de qué tier es (ai se sincroniza solo)

                if (newNpc.ModNPC is TransformedDinoNPC dino)
                {
                    dino.ownerName = owner.name;
                }

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newNpcIndex);
            }

            infected = false;
            virusTimer = 0;
            infectedByTier = 0;

            npc.life = 0;
            npc.active = false;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (infected && npc.active)
            {
                int buffType = ModContent.BuffType<DinoVirus>();
                Texture2D buffTexture = TextureAssets.Buff[buffType].Value;

                Vector2 drawPos = new Vector2(
                    npc.Center.X - screenPos.X,
                    npc.Top.Y - screenPos.Y - 20f
                );

                Vector2 origin = new Vector2(buffTexture.Width / 2f, buffTexture.Height / 2f);

                spriteBatch.Draw(buffTexture, drawPos, null, Color.White, 0f, origin, 0.85f, SpriteEffects.None, 0f);
            }
        }
    }
}