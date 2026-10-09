using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KillerQueen_Buffs;


namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust
{
    public class BitesTheDustPlayer : ModPlayer
    {
        public bool btdActive = false;
        public bool isExplodingPhase = false;
        public int explosionTimer = 0;

        public Vector2 savedPosition; // top-left (Player.position), NO Center
        public int savedHealth;
        public int savedDir;

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Player.HasBuff(ModContent.BuffType<BitesTheDustBuff>()) && !isExplodingPhase)
            {
                ActivateBitesTheDust();
                return false;
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
        }

        public void SaveState()
        {
            savedPosition = Player.position;
            savedHealth = Player.statLife;
            savedDir = Player.direction;
            btdActive = true;

            MarkVisibleEnemies();
        }

        // Marca cualquier enemigo activo que todavía no esté marcado, guardando
        // su posición EN ESE INSTANTE. Se llama al activar la habilidad y en
        // cada tick mientras btdActive esté activo (ver PostUpdate), para que
        // los enemigos que aparecen DESPUÉS de activar la habilidad también
        // retrocedan en el tiempo, a la posición donde fueron vistos primero.
        void MarkVisibleEnemies()
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.townNPC)
                {
                    var btdNpc = npc.GetGlobalNPC<BitesTheDustNPC>();
                    if (!btdNpc.markedForBitesTheDust)
                    {
                        btdNpc.markedForBitesTheDust = true;
                        btdNpc.savedPosition = npc.Center;
                        btdNpc.savedFrame = npc.frame;
                        btdNpc.savedSpriteDirection = npc.spriteDirection;
                    }
                }
            }
        }

        public void ActivateBitesTheDust()
        {
            isExplodingPhase = true;
            explosionTimer = 600;

            Player.Teleport(savedPosition, 1);
            Player.position = savedPosition;
            Player.velocity = Vector2.Zero;

            Player.statLife = savedHealth;
            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;
            if (Player.statLife < 1)
                Player.statLife = 1;

            // Reunir TODOS los NPCs marcados (incluidos los que aparecieron
            // después de activar la habilidad) y pedir el rebobinado
            // sincronizado por red: mismo resultado y mismo sonido para todos.
            List<int> npcIds = new();
            List<Vector2> positions = new();
            List<Rectangle> frames = new();
            List<int> spriteDirs = new();

            foreach (NPC npc in Main.npc)
            {
                if (npc.active)
                {
                    var btdNpc = npc.GetGlobalNPC<BitesTheDustNPC>();
                    if (btdNpc.markedForBitesTheDust)
                    {
                        npcIds.Add(npc.whoAmI);
                        positions.Add(btdNpc.savedPosition);
                        frames.Add(btdNpc.savedFrame);
                        spriteDirs.Add(btdNpc.savedSpriteDirection);
                    }
                }
            }

            BitesTheDustNet.RequestRewind(Player.Center, npcIds, positions, frames, spriteDirs);

            Player.ClearBuff(ModContent.BuffType<BitesTheDustBuff>());
        }

        public override void PostUpdate()
        {
            // Solo el cliente dueño de este jugador necesita seguir vigilando
            // enemigos nuevos; el resultado final se sincroniza por red recién
            // al momento de la explosión.
            if (btdActive && Player.whoAmI == Main.myPlayer)
            {
                MarkVisibleEnemies();
            }

            if (isExplodingPhase)
            {
                explosionTimer--;
                if (explosionTimer % 30 == 0)
                {
                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active)
                        {
                            var btdNpc = npc.GetGlobalNPC<BitesTheDustNPC>();
                            if (btdNpc.markedForBitesTheDust)
                            {
                                Projectile.NewProjectile(Player.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BitesTheDust_Explosion>(), 40, 0f, Player.whoAmI);
                            }
                        }
                    }
                }

                if (explosionTimer <= 0)
                {
                    isExplodingPhase = false;
                    btdActive = false;
                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active) npc.GetGlobalNPC<BitesTheDustNPC>().markedForBitesTheDust = false;
                    }
                }
            }
        }
    }
}