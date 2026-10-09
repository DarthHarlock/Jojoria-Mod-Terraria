// Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/ReturnZero/ReturnZeroPlayer.cs
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.GoldenExperienceRequiem_Buffs;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public class ReturnZeroPlayer : ModPlayer
    {
        bool shaderActivo = false;

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (!Player.HasBuff(ModContent.BuffType<ReturnToZero>()))
                return false;

            bool esquivado = false;
            int? npcIndex = info.DamageSource.SourceNPCIndex;
            int projIndex = info.DamageSource.SourceProjectileLocalIndex;

            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                if (proj.active)
                {
                    var globalProj = proj.GetGlobalProjectile<ReturnZeroGlobalProjectile>();
                    globalProj.IniciarRewind(proj);
                    esquivado = true;

                    if ((npcIndex == null || npcIndex < 0) && globalProj.npcOwner >= 0 && globalProj.npcOwner < Main.maxNPCs)
                    {
                        npcIndex = globalProj.npcOwner;
                    }
                }
            }

            if (npcIndex != null && npcIndex.Value >= 0 && npcIndex.Value < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcIndex.Value];
                if (npc.active)
                {
                    List<NPC> segmentos = ReturnZeroGlobalNPC.ObtenerSegmentosActivosDelGrupo(npc);
                    bool puedeRebobinar = true;

                    foreach (NPC segmento in segmentos)
                    {
                        var d = segmento.GetGlobalNPC<ReturnZeroGlobalNPC>();
                        if (d.rewindeando || d.rewindCooldown > 0)
                        {
                            puedeRebobinar = false;
                            break;
                        }
                    }

                    if (puedeRebobinar)
                    {
                        ReturnZeroEffects.EjecutarEfecto(Player, segmentos);
                    }
                    esquivado = true;
                }
            }

            return esquivado;
        }

        public override void PostUpdateBuffs()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            bool activo = Player.HasBuff(ModContent.BuffType<ReturnToZero>());

            if (activo && !shaderActivo)
            {
                global::Jojo.Jojo.ActivarShaderVerde();
                shaderActivo = true;
            }
            else if (!activo && shaderActivo)
            {
                global::Jojo.Jojo.DesactivarShaderVerde();
                shaderActivo = false;
            }
        }

        public override void OnRespawn()
        {
            if (shaderActivo)
            {
                global::Jojo.Jojo.DesactivarShaderVerde();
                shaderActivo = false;
            }
        }
    }
}