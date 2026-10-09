// Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/ReturnZero/ReturnZeroGlobalProjectile.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public class ReturnZeroGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        const int MAX_HISTORIAL = 10 * 60;
        public readonly Queue<Vector2> historialPos = new();
        public readonly Queue<float> historialRot = new();

        public bool rewindeando = false;
        List<Vector2> rutaPos;
        List<float> rutaRot;
        int rewindIndex;
        public int velocidadRebobinado = 3;

        public int npcOwner = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            npcOwner = RastrearDueño(projectile, source);
        }

        private int RastrearDueño(Projectile proj, IEntitySource source)
        {
            if (source != null)
            {
                if (source is EntitySource_Parent parentSource)
                {
                    if (parentSource.Entity is NPC npc) return npc.whoAmI;
                    if (parentSource.Entity is Projectile parentProj)
                        return parentProj.GetGlobalProjectile<ReturnZeroGlobalProjectile>().npcOwner;
                }
            }

            if (proj.hostile || proj.npcProj)
            {
                int bestNpc = -1;
                float minDist = 2000f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && !n.friendly && n.lifeMax > 5)
                    {
                        float dist = Vector2.Distance(n.Center, proj.Center);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestNpc = i;
                        }
                    }
                }
                return bestNpc;
            }

            return -1;
        }

        public override bool PreAI(Projectile projectile)
        {
            if (rewindeando)
            {
                if (rutaPos == null || rewindIndex >= rutaPos.Count)
                {
                    projectile.Kill();
                    return false;
                }

                for (int paso = 0; paso < velocidadRebobinado && rewindIndex < rutaPos.Count; paso++)
                    rewindIndex++;

                int idx = Math.Min(rewindIndex, rutaPos.Count - 1);
                projectile.Center = rutaPos[idx];
                projectile.rotation = rutaRot[idx];
                projectile.velocity = Vector2.Zero;

                Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.GemEmerald, 0, 0, 100, default, 1.2f);

                if (rewindIndex >= rutaPos.Count)
                    projectile.Kill();

                return false;
            }
            return true;
        }

        public override void PostAI(Projectile projectile)
        {
            if (rewindeando || projectile.friendly) return;

            historialPos.Enqueue(projectile.Center);
            historialRot.Enqueue(projectile.rotation);
            if (historialPos.Count > MAX_HISTORIAL)
            {
                historialPos.Dequeue();
                historialRot.Dequeue();
            }
        }

        public void IniciarRewind(Projectile projectile)
        {
            if (rewindeando || historialPos.Count < 2)
            {
                projectile.Kill();
                return;
            }

            rutaPos = new List<Vector2>(historialPos);
            rutaRot = new List<float>(historialRot);
            rutaPos.Reverse();
            rutaRot.Reverse();
            rewindIndex = 0;
            rewindeando = true;

            projectile.hostile = false;
            projectile.friendly = false;
            projectile.damage = 0;
            projectile.netUpdate = true;
        }
    }
}