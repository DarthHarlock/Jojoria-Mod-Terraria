using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria.Localization;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_3
{
    // 1. EL MANEJADOR DE LA HABILIDAD
    public static class CMoonSkillHandler_Tier_3
    {
        public static readonly HashSet<int> TiposMoonLord = new HashSet<int>
        {
            NPCID.MoonLordCore,
            NPCID.MoonLordHead,
            NPCID.MoonLordHand
        };

        public static void CancelDash(NPC npc)
        {
            if (!npc.active || !npc.boss) return;

            if (npc.type == NPCID.QueenBee)
            {
                if (npc.ai[0] == 0f || npc.ai[0] == 4f)
                {
                    npc.ai[0] = 1f; npc.ai[1] = 0f; npc.ai[2] = 0f; npc.ai[3] = 0f;
                    if (Main.netMode == NetmodeID.Server) npc.netUpdate = true;
                }
            }
            else if (npc.type == NPCID.EyeofCthulhu)
            {
                if (npc.ai[0] == 1f || npc.ai[0] == 5f)
                {
                    npc.ai[0] = 0f; npc.ai[1] = 0f; npc.ai[2] = 0f;
                    if (Main.netMode == NetmodeID.Server) npc.netUpdate = true;
                }
            }
            else if (npc.type == NPCID.DukeFishron)
            {
                if (npc.ai[0] == 1f || npc.ai[0] == 3f || npc.ai[0] == 9f)
                {
                    npc.ai[0] = 0f; npc.ai[1] = 0f; npc.ai[2] = 0f;
                    if (Main.netMode == NetmodeID.Server) npc.netUpdate = true;
                }
            }
            else if (npc.type == NPCID.Spazmatism)
            {
                if (npc.ai[0] == 1f || npc.ai[0] == 3f)
                {
                    npc.ai[0] = 0f; npc.ai[1] = 0f;
                    if (Main.netMode == NetmodeID.Server) npc.netUpdate = true;
                }
            }
        }

        // ===================== FIX BUG: HELPERS DE GUSANOS =====================
        // Reconocen si un NPC es cabeza/cuerpo/cola de un enemigo tipo gusano.
        // Se usan para: (1) no redirigir el daño de la Habilidad F a cada segmento
        // individual, sino siempre a la cabeza; y (2) no rotar el sprite de los
        // gusanos al levantarlos con la Habilidad F.
        public static bool EsCuerpoDeGusano(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) return true;
            return npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail ||
                   npc.type == NPCID.DevourerBody || npc.type == NPCID.DevourerTail ||
                   npc.type == NPCID.DiggerBody || npc.type == NPCID.DiggerTail ||
                   npc.type == NPCID.TombCrawlerBody || npc.type == NPCID.TombCrawlerTail ||
                   npc.type == NPCID.BoneSerpentBody || npc.type == NPCID.BoneSerpentTail ||
                   npc.type == NPCID.WyvernBody || npc.type == NPCID.WyvernBody2 || npc.type == NPCID.WyvernBody3 || npc.type == NPCID.WyvernLegs || npc.type == NPCID.WyvernTail ||
                   npc.type == NPCID.StardustWormBody || npc.type == NPCID.StardustWormTail ||
                   npc.type == NPCID.LeechBody || npc.type == NPCID.LeechTail ||
                   npc.type == NPCID.SolarCrawltipedeBody || npc.type == NPCID.SolarCrawltipedeTail ||
                   npc.type == NPCID.DuneSplicerBody || npc.type == NPCID.DuneSplicerTail ||
                   npc.type == NPCID.BloodEelBody || npc.type == NPCID.BloodEelTail;
        }

        public static bool EsCabezaDeGusano(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife == npc.whoAmI) return true;
            return npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.DevourerHead ||
                   npc.type == NPCID.DiggerHead || npc.type == NPCID.TombCrawlerHead ||
                   npc.type == NPCID.BoneSerpentHead || npc.type == NPCID.WyvernHead ||
                   npc.type == NPCID.StardustWormHead || npc.type == NPCID.LeechHead ||
                   npc.type == NPCID.SolarCrawltipedeHead || npc.type == NPCID.DuneSplicerHead ||
                   npc.type == NPCID.BloodEelHead;
        }

        public static bool EsSegmentoDeGusano(NPC npc) => EsCabezaDeGusano(npc) || EsCuerpoDeGusano(npc);

        /// <summary>
        /// Dado cualquier segmento de un gusano (cabeza, cuerpo o cola), devuelve la
        /// entidad "cabeza" — la que debe recibir todo el daño del estampado, como si
        /// fuera un único jefe. Si no se encuentra cabeza (no debería pasar con los
        /// tipos soportados), devuelve el propio segmento como respaldo de seguridad.
        /// </summary>
        public static NPC EncontrarCabezaDeGusano(NPC npc)
        {
            if (EsCabezaDeGusano(npc)) return npc;

            foreach (NPC segmento in ObtenerSegmentos(npc))
            {
                if (EsCabezaDeGusano(segmento)) return segmento;
            }

            return npc;
        }

        public static void DeteccionContinuaAura(Player player, Vector2 center, float range)
        {
            // A. Detectar NPCs (SOLO EL SERVIDOR) - Evita que un cliente altere un NPC localmente.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];

                    if (npc.active && !npc.friendly && npc.type != NPCID.TargetDummy)
                    {
                        if (Vector2.Distance(center, npc.Center) <= range)
                        {
                            var modNPC = npc.GetGlobalNPC<CMoonGravityNPC_Tier_3>();

                            // FIX BUG: ya NO exigimos "zeroG.zeroGravityTimer <= 0" para poder
                            // empezar la Habilidad F. Mientras el campo de Gravedad Cero (H)
                            // estaba activo, ese timer se mantenía siempre > 0 (el campo lo
                            // refresca cada tick sobre todo lo que hay en su radio), así que la
                            // F jamás lograba activarse sobre un enemigo atrapado por H. Ahora
                            // es StartGravityEffect() quien se encarga de cancelar la Gravedad
                            // Cero de forma segura al tomar el control.
                            if (!modNPC.affectedByCMoon)
                            {
                                List<NPC> todosLosSegmentos = ObtenerSegmentos(npc);
                                foreach (NPC segmento in todosLosSegmentos)
                                {
                                    var segGlobal = segmento.GetGlobalNPC<CMoonGravityNPC_Tier_3>();

                                    if (!segGlobal.affectedByCMoon)
                                    {
                                        segGlobal.StartGravityEffect(player.whoAmI, segmento);
                                    }
                                }
                            }
                        }
                    }
                }

                // B. Detectar Proyectiles Enemigos (SOLO EL SERVIDOR)
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.hostile && !proj.friendly)
                    {
                        if (Vector2.Distance(center, proj.Center) <= range)
                        {
                            var modProj = proj.GetGlobalProjectile<CMoonGravityProjectile_Tier_3>();

                            // FIX BUG: mismo criterio que con los NPCs.
                            if (!modProj.affectedByCMoon)
                            {
                                modProj.StartGravityEffect(proj);
                            }
                        }
                    }
                }
            }

            // C. FIX PVP: Detectar Jugadores Enemigos (CADA CLIENTE SE DETECTA A SÍ MISMO)
            // Esto asegura que tu pantalla procese tu propia caída sin depender del lag del jugador 1.
            if (Main.netMode != NetmodeID.Server)
            {
                Player myPlayer = Main.LocalPlayer;
                if (myPlayer.active && !myPlayer.dead && myPlayer.whoAmI != player.whoAmI)
                {
                    if (player.hostile && myPlayer.hostile && (player.team == 0 || player.team != myPlayer.team))
                    {
                        if (Vector2.Distance(center, myPlayer.Center) <= range)
                        {
                            var modPlayer = myPlayer.GetModPlayer<CMoonGravityPlayer_Tier_3>();

                            // FIX BUG: mismo criterio que con los NPCs.
                            if (!modPlayer.affectedByCMoon)
                            {
                                modPlayer.StartGravityEffect(player.whoAmI);
                            }
                        }
                    }
                }
            }
        }

        public static List<NPC> ObtenerSegmentos(NPC npc)
        {
            List<NPC> lista = new List<NPC>();
            if (npc == null || !npc.active) return lista;

            if (TiposMoonLord.Contains(npc.type))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && TiposMoonLord.Contains(o.type)) lista.Add(o);
                }
                return lista;
            }

            if (npc.realLife >= 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && (o.realLife == npc.realLife || o.whoAmI == npc.realLife)) lista.Add(o);
                }
                return lista;
            }

            bool esNucleo = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC o = Main.npc[i];
                if (o.active && o.whoAmI != npc.whoAmI && o.realLife == npc.whoAmI)
                {
                    esNucleo = true;
                    break;
                }
            }

            if (esNucleo)
            {
                lista.Add(npc);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && o.whoAmI != npc.whoAmI && o.realLife == npc.whoAmI) lista.Add(o);
                }
                return lista;
            }

            if (npc.aiStyle == 6)
            {
                HashSet<int> visitados = new HashSet<int>();
                Queue<NPC> cola = new Queue<NPC>();
                cola.Enqueue(npc);
                visitados.Add(npc.whoAmI);

                while (cola.Count > 0)
                {
                    NPC actual = cola.Dequeue();
                    lista.Add(actual);

                    int adelanteID = (int)actual.ai[0];
                    if (adelanteID >= 0 && adelanteID < Main.maxNPCs)
                    {
                        NPC ahead = Main.npc[adelanteID];
                        if (ahead.active && ahead.aiStyle == 6 && !visitados.Contains(adelanteID))
                        {
                            visitados.Add(adelanteID);
                            cola.Enqueue(ahead);
                        }
                    }

                    int atrasID = (int)actual.ai[1];
                    if (atrasID >= 0 && atrasID < Main.maxNPCs)
                    {
                        NPC behind = Main.npc[atrasID];
                        if (behind.active && behind.aiStyle == 6 && !visitados.Contains(atrasID))
                        {
                            visitados.Add(atrasID);
                            cola.Enqueue(behind);
                        }
                    }
                }
                return lista;
            }

            lista.Add(npc);
            return lista;
        }
    }

    // 2. COMPORTAMIENTO DE GRAVEDAD PARA NPCs
    public class CMoonGravityNPC_Tier_3 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool affectedByCMoon = false;
        public int gravityTimer = 0;
        public int attackerWhoAmI = -1;

        // Variables de cache blindadas
        public bool hasCachedState = false;
        private bool oldNoTileCollide;
        private bool oldNoGravity;
        private float oldKnockBackResist;

        // FIX BUG (daño de gusanos): marca si la cabeza de ESTE gusano ya recibió el
        // golpe de "estampado" durante la activación actual de la Habilidad F. Solo
        // tiene sentido en la instancia de la CABEZA; los demás segmentos consultan
        // esta bandera EN la cabeza, nunca en sí mismos.
        public bool wormDamageAppliedThisActivation = false;

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(affectedByCMoon);
            binaryWriter.Write(gravityTimer);
            binaryWriter.Write(attackerWhoAmI);

            // Sincronizamos el estado original para que cuando el efecto termine, 
            // el cliente sepa exactamente qué valores restaurar.
            binaryWriter.Write(hasCachedState);
            if (hasCachedState)
            {
                binaryWriter.Write(oldNoTileCollide);
                binaryWriter.Write(oldNoGravity);
                binaryWriter.Write(oldKnockBackResist);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            affectedByCMoon = binaryReader.ReadBoolean();
            gravityTimer = binaryReader.ReadInt32();
            attackerWhoAmI = binaryReader.ReadInt32();

            hasCachedState = binaryReader.ReadBoolean();
            if (hasCachedState)
            {
                oldNoTileCollide = binaryReader.ReadBoolean();
                oldNoGravity = binaryReader.ReadBoolean();
                oldKnockBackResist = binaryReader.ReadSingle();
            }
        }

        public void StartGravityEffect(int playerID, NPC npc)
        {
            // FIX BUG: si Gravedad Cero (H) tiene el control de este NPC, se lo quitamos
            // de forma segura (restaurando su noGravity original) ANTES de que la
            // Habilidad F empiece a cachear/modificar su estado. Esto es justo lo que
            // permite usar la F sobre un enemigo aunque el campo H siga activo.
            npc.GetGlobalNPC<CMoonZeroGravityNPC_Tier3>().CancelSilently(npc);

            if (!affectedByCMoon)
            {
                CMoonSkillHandler_Tier_3.CancelDash(npc);

                // Solo guardamos el estado original UNA vez. 
                // Esto evita que si es golpeado por otra habilidad (ej. Gravedad Cero) guarde un estado corrupto.
                if (!hasCachedState)
                {
                    oldNoTileCollide = npc.noTileCollide;
                    oldNoGravity = npc.noGravity;
                    oldKnockBackResist = npc.knockBackResist;
                    hasCachedState = true;
                }

                // FIX BUG (daño de gusanos): si esta entidad es la cabeza de un gusano,
                // reiniciamos su marca de "ya recibió el golpe" al comenzar una nueva
                // activación, para que la PRÓXIMA vez que uses la habilidad sobre este
                // mismo gusano el golpe sí vuelva a aplicarse (una sola vez).
                if (CMoonSkillHandler_Tier_3.EsCabezaDeGusano(npc))
                {
                    wormDamageAppliedThisActivation = false;
                }
            }

            affectedByCMoon = true;
            gravityTimer = 0;
            attackerWhoAmI = playerID;
            npc.knockBackResist = 0f;

            if (Main.netMode == NetmodeID.Server)
                npc.netUpdate = true;
        }

        /// <summary>
        /// FIX BUG: Cancela el efecto de Gravedad Invertida (F) de forma "silenciosa",
        /// es decir, SIN aplicar el golpe de "estampado" ni la lógica de segmentos.
        /// Se usa cuando otro sistema (Gravedad Cero / Habilidad H) necesita tomar
        /// control exclusivo del NPC, para que ambos efectos nunca escriban sobre
        /// npc.velocity/noGravity/rotation en el mismo tick.
        /// </summary>
        public void CancelSilently(NPC npc)
        {
            if (!affectedByCMoon) return;

            affectedByCMoon = false;
            gravityTimer = 0;
            attackerWhoAmI = -1;

            if (hasCachedState)
            {
                npc.noTileCollide = oldNoTileCollide;
                npc.noGravity = oldNoGravity;
                npc.knockBackResist = oldKnockBackResist;
                hasCachedState = false;
            }

            npc.velocity.X = 0f;

            if (Main.netMode == NetmodeID.Server)
                npc.netUpdate = true;
        }

        public override bool PreAI(NPC npc)
        {
            if (!affectedByCMoon) return base.PreAI(npc);

            // Safety net: si por cualquier motivo Gravedad Cero (H) ya tomó control de
            // este NPC en este mismo tick, cedemos en vez de seguir forzando velocidad/
            // gravedad en paralelo.
            if (npc.GetGlobalNPC<CMoonZeroGravityNPC_Tier3>().zeroGravityTimer > 0)
            {
                CancelSilently(npc);
                return base.PreAI(npc);
            }

            // FIX BUG (rotación de gusanos): los segmentos de gusano quedan excluidos
            // de todo el manejo de rotación de abajo, igual que ya pasaba con Moon Lord.
            bool esGusano = CMoonSkillHandler_Tier_3.EsSegmentoDeGusano(npc);

            gravityTimer++;

            npc.noTileCollide = false;
            npc.noGravity = false;
            npc.velocity.X = 0f;

            if (gravityTimer <= 60)
            {
                npc.velocity.Y = -7f;

                if (!CMoonSkillHandler_Tier_3.TiposMoonLord.Contains(npc.type) && !esGusano)
                {
                    npc.rotation = MathHelper.Pi;
                }

                npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.ChlorophyteWeapon);
                }
            }
            else
            {
                if (gravityTimer == 61 && !CMoonSkillHandler_Tier_3.TiposMoonLord.Contains(npc.type) && !esGusano)
                {
                    npc.rotation = 0f;
                }

                if (npc.velocity.Y < 25f) npc.velocity.Y += 2f;

                float velYAntes = npc.velocity.Y;
                npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                if (gravityTimer > 65 && velYAntes > 1f && npc.velocity.Y <= 0.15f)
                {
                    SlamIntoGround(npc);
                }
                else if (gravityTimer > 180)
                {
                    SlamIntoGround(npc);
                }
            }

            return false;
        }

        public void SlamIntoGround(NPC npc, bool syncSegments = true)
        {
            affectedByCMoon = false;

            // FIX BUG (rotación de gusanos): nunca les tocamos la rotación al empezar
            // (ver PreAI), así que tampoco la reseteamos aquí — se queda exactamente
            // como estaba antes de iniciar la habilidad.
            if (!CMoonSkillHandler_Tier_3.TiposMoonLord.Contains(npc.type) && !CMoonSkillHandler_Tier_3.EsSegmentoDeGusano(npc))
            {
                npc.rotation = 0f;
            }

            // Restaurar con seguridad usando los valores verdaderos cacheados
            if (hasCachedState)
            {
                npc.noTileCollide = oldNoTileCollide;
                npc.noGravity = oldNoGravity;
                npc.knockBackResist = oldKnockBackResist;

                // ¡VITAL! Reiniciamos el caché para que no se corrompa en usos futuros
                hasCachedState = false;
            }

            // Frenar cualquier inercia horizontal residual que haya dejado la habilidad 'H'
            npc.velocity.X = 0f;

            if (syncSegments)
            {
                List<NPC> segmentos = CMoonSkillHandler_Tier_3.ObtenerSegmentos(npc);
                foreach (NPC segmento in segmentos)
                {
                    if (segmento.whoAmI != npc.whoAmI && segmento.active)
                    {
                        var segGlobal = segmento.GetGlobalNPC<CMoonGravityNPC_Tier_3>();
                        if (segGlobal.affectedByCMoon)
                        {
                            segGlobal.SlamIntoGround(segmento, false);
                        }
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server)
                npc.netUpdate = true;

            for (int i = 0; i < 15; i++)
            {
                Dust.NewDust(npc.Bottom - new Vector2(0, 10), npc.width, 10, DustID.Smoke);
            }

            if (attackerWhoAmI == -1) return;

            // FIX BUG (daño de gusanos): antes, CADA fragmento del gusano que detectaba
            // su propio aterrizaje llamaba a StrikeNPC sobre SÍ MISMO (y encima cada
            // llamada recursiva de sincronización de segmentos repetía el mismo golpe),
            // así que un gusano largo podía recibir el daño base multiplicado por 8-10
            // veces en un solo estampado. Ahora, si la entidad golpeada es parte de un
            // gusano, el daño se redirige SIEMPRE a la cabeza (la entidad "jefe") y solo
            // se aplica UNA vez por activación, sin importar cuántos segmentos toquen
            // el suelo ni en qué orden lo hagan.
            NPC entidadQueRecibeDano = npc;

            if (CMoonSkillHandler_Tier_3.EsSegmentoDeGusano(npc))
            {
                NPC cabeza = CMoonSkillHandler_Tier_3.EncontrarCabezaDeGusano(npc);
                var cabezaGlobal = cabeza.GetGlobalNPC<CMoonGravityNPC_Tier_3>();

                if (cabezaGlobal.wormDamageAppliedThisActivation)
                {
                    return; // Este gusano ya recibió su golpe de daño en esta activación.
                }

                cabezaGlobal.wormDamageAppliedThisActivation = true;
                entidadQueRecibeDano = cabeza;
            }

            Player player = Main.player[attackerWhoAmI];

            int baseDamage = 140;
            float standDamageFloat = player.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
            int finalVaryingDamage = (int)Main.DamageVar(standDamageFloat);

            // Calculamos una dirección real (1 o -1) en lugar de 0 para evitar bugs físicos en Terraria
            int direccionGolpe = entidadQueRecibeDano.Center.X < player.Center.X ? -1 : 1;

            NPC.HitInfo hitInfo = new NPC.HitInfo
            {
                Damage = finalVaryingDamage,
                Knockback = 5f,
                HitDirection = direccionGolpe,
                Crit = false
            };

            // Solo el servidor o el Singleplayer pueden ejecutar el daño
            if (!entidadQueRecibeDano.dontTakeDamage && Main.netMode != NetmodeID.MultiplayerClient)
            {
                entidadQueRecibeDano.StrikeNPC(hitInfo);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendStrikeNPC(entidadQueRecibeDano, hitInfo);
                }
            }
        }
    }

    // 3. COMPORTAMIENTO DE GRAVEDAD PARA PROYECTILES
    public class CMoonGravityProjectile_Tier_3 : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool affectedByCMoon = false;
        public int gravityTimer = 0;

        public bool hasCachedState = false;
        private bool oldTileCollide;

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(affectedByCMoon);
            binaryWriter.Write(gravityTimer);
            binaryWriter.Write(hasCachedState);
            if (hasCachedState) binaryWriter.Write(oldTileCollide);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            affectedByCMoon = binaryReader.ReadBoolean();
            gravityTimer = binaryReader.ReadInt32();
            hasCachedState = binaryReader.ReadBoolean();
            if (hasCachedState) oldTileCollide = binaryReader.ReadBoolean();
        }

        public void StartGravityEffect(Projectile proj)
        {
            // FIX BUG: mismo criterio que en CMoonGravityNPC_Tier_3 — cedemos el control
            // de Gravedad Cero antes de que la Habilidad F tome el proyectil.
            proj.GetGlobalProjectile<CMoonZeroGravityProjectile_Tier3>().CancelSilently(proj);

            if (!affectedByCMoon)
            {
                if (!hasCachedState)
                {
                    oldTileCollide = proj.tileCollide;
                    hasCachedState = true;
                }
            }
            affectedByCMoon = true;
            gravityTimer = 0;

            if (Main.netMode == NetmodeID.Server)
                proj.netUpdate = true;
        }

        /// <summary>
        /// FIX BUG: cancela el efecto de Gravedad Invertida (F) sobre un proyectil
        /// sin destruirlo, para cederle el control a Gravedad Cero (H) sin conflictos.
        /// </summary>
        public void CancelSilently(Projectile proj)
        {
            if (!affectedByCMoon) return;

            affectedByCMoon = false;
            gravityTimer = 0;

            if (hasCachedState)
            {
                proj.tileCollide = oldTileCollide;
                hasCachedState = false;
            }

            proj.velocity.X = 0f;

            if (Main.netMode == NetmodeID.Server)
                proj.netUpdate = true;
        }

        public override bool PreAI(Projectile proj)
        {
            if (!affectedByCMoon) return base.PreAI(proj);

            // Safety net: mismo criterio que en CMoonGravityNPC_Tier_3.
            if (proj.GetGlobalProjectile<CMoonZeroGravityProjectile_Tier3>().zeroGravityTimer > 0)
            {
                CancelSilently(proj);
                return base.PreAI(proj);
            }

            gravityTimer++;

            proj.velocity.X = 0f;
            proj.tileCollide = false;

            if (gravityTimer <= 60)
            {
                proj.velocity.Y = -7f;
                proj.rotation += 0.4f;

                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(proj.position, proj.width, proj.height, DustID.ChlorophyteWeapon);
                }
            }
            else
            {
                if (proj.velocity.Y < 25f) proj.velocity.Y += 2f;
                proj.rotation += 0.6f;

                if (gravityTimer > 65)
                {
                    proj.tileCollide = true;
                }

                if (gravityTimer > 180)
                {
                    SlamIntoGround(proj);
                }
            }

            return false;
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (affectedByCMoon && gravityTimer > 60)
            {
                SlamIntoGround(projectile);
                return false;
            }
            return base.OnTileCollide(projectile, oldVelocity);
        }

        private void SlamIntoGround(Projectile proj)
        {
            affectedByCMoon = false;

            if (hasCachedState)
            {
                proj.tileCollide = oldTileCollide;

                // Reiniciar el caché aquí también
                hasCachedState = false;
            }

            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(proj.Bottom - new Vector2(0, 10), proj.width, 10, DustID.Smoke);
            }

            // Si eres el dueño del proyectil o el servidor, mátalo
            if (Main.netMode != NetmodeID.MultiplayerClient || proj.owner == Main.myPlayer)
            {
                proj.Kill();
            }
        }
    }

    // 4. FIX PVP: COMPORTAMIENTO DE GRAVEDAD PARA JUGADORES (HABILIDAD F)
    public class CMoonGravityPlayer_Tier_3 : ModPlayer
    {
        public bool affectedByCMoon = false;
        public int gravityTimer = 0;
        public int attackerWhoAmI = -1;

        public void StartGravityEffect(int playerID)
        {
            // FIX BUG: mismo criterio que en NPCs/proyectiles — cedemos el control de
            // Gravedad Cero antes de que la Habilidad F tome al jugador.
            Player.GetModPlayer<CMoonZeroGravityPlayer_Tier3>().CancelSilently();

            affectedByCMoon = true;
            gravityTimer = 0;
            attackerWhoAmI = playerID;
        }

        /// <summary>
        /// FIX BUG: cancela el efecto de Gravedad Invertida (F) sobre el jugador
        /// sin aplicar el golpe final, para cederle el control a Gravedad Cero (H).
        /// </summary>
        public void CancelSilently()
        {
            affectedByCMoon = false;
            gravityTimer = 0;
            attackerWhoAmI = -1;
        }

        public override void PreUpdate()
        {
            if (!affectedByCMoon) return;

            // Safety net: mismo criterio de exclusión mutua que en NPCs/proyectiles.
            if (Player.GetModPlayer<CMoonZeroGravityPlayer_Tier3>().zeroGravityTimer > 0)
            {
                CancelSilently();
                return;
            }

            // Este código ahora solo lo corre de forma prioritaria el cliente de la víctima
            gravityTimer++;

            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlJump = false;
            Player.controlDown = false;
            Player.controlUp = false;
            Player.velocity.X = 0f;

            if (gravityTimer <= 60)
            {
                Player.velocity.Y = -7f;
                if (Main.rand.NextBool(3)) Dust.NewDust(Player.position, Player.width, Player.height, DustID.ChlorophyteWeapon);
            }
            else
            {
                if (Player.velocity.Y < 25f) Player.velocity.Y += 2f;

                if (gravityTimer > 65 && Player.velocity.Y == 0f)
                {
                    SlamIntoGround();
                }
                else if (gravityTimer > 180)
                {
                    SlamIntoGround();
                }
            }
        }

        private void SlamIntoGround()
        {
            affectedByCMoon = false;
            for (int i = 0; i < 15; i++)
                Dust.NewDust(Player.Bottom - new Vector2(0, 10), Player.width, 10, DustID.Smoke);

            if (attackerWhoAmI == -1) return;
            Player attacker = Main.player[attackerWhoAmI];

            int baseDamage = 150;
            float standDamageFloat = attacker.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
            int finalVaryingDamage = (int)Main.DamageVar(standDamageFloat);

            // FIX ADVERTENCIA CS0618: PlayerDeathReason.ByCustomReason(string) está obsoleto.
            // Se reemplaza por la sobrecarga que recibe NetworkText, usando FromLiteral()
            // para que el texto se muestre exactamente igual que antes (sin traducción).
            var deathReason = PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(Player.name + " fue aplastado por la gravedad de " + attacker.name));

            // Solo el cliente del propio jugador (la víctima) decide lastimarse a sí mismo
            if (Player.whoAmI == Main.myPlayer)
            {
                Player.Hurt(deathReason, finalVaryingDamage, 0, pvp: true);
            }
        }
    }
}