using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Jojo.Content.UI;

namespace Jojo.Content.Players
{
    public class AnubisSoulManager : ModPlayer
    {
        private int tiempoDeGracia = 0;
        // 5 ticks (0.08 segundos): Imperceptible para el ojo humano al desequipar, pero suficiente para evitar borrados por micro-cortes al cambiar de Tier.
        private const int MAX_TIEMPO_GRACIA = 5;
        private bool habiaAnubisEquipado = false;

        public override void Initialize()
        {
            tiempoDeGracia = 0;
            habiaAnubisEquipado = false;
        }

        private bool EsItemAnubis(Item item)
        {
            if (item == null || item.IsAir || item.ModItem == null)
                return false;

            // Detecta automáticamente cualquier ítem que empiece por "AnubisItem" (Tier_1, Tier_2, Tier_3, Tier_4, etc.)
            return item.ModItem.Name.StartsWith("AnubisItem");
        }

        public override void ResetEffects()
        {
            bool tieneAnubisEnSlot = false;

            if (Player.TryGetModPlayer(out StandSlotPlayer slotPlayer))
            {
                tieneAnubisEnSlot = EsItemAnubis(slotPlayer.standItem);
            }

            if (tieneAnubisEnSlot)
            {
                habiaAnubisEquipado = true;
                tiempoDeGracia = MAX_TIEMPO_GRACIA;

                // Sincroniza el progreso entre todas las Tiers al instante
                SincronizarProgresoTodasLasTiers();
            }
            else if (habiaAnubisEquipado)
            {
                if (tiempoDeGracia > 0)
                {
                    tiempoDeGracia--;
                }
                else
                {
                    // Se quitó el ítem y pasaron los 0.08 segundos de margen: ¡Se borra todo al instante en todas las Tiers!
                    BorrarProgresoDeTodasLasTiers(mostrarMensaje: true);
                    habiaAnubisEquipado = false;
                }
            }
            else
            {
                // Seguridad extra: si no hay Anubis equipado ni en tiempo de gracia, nos aseguramos de que el progreso sea 0
                if (TieneAlmasAcumuladas())
                {
                    BorrarProgresoDeTodasLasTiers(mostrarMensaje: false);
                }
            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (habiaAnubisEquipado || TieneAlmasAcumuladas())
            {
                BorrarProgresoDeTodasLasTiers(mostrarMensaje: false);
                habiaAnubisEquipado = false;
                tiempoDeGracia = 0;

                string textoMuerte;
                string idiomaActual = Language.ActiveCulture.Name;

                if (idiomaActual.StartsWith("es"))
                    textoMuerte = "¡Anubis ha perdido su racha!";
                else if (idiomaActual.StartsWith("ja"))
                    textoMuerte = "アヌビスは連撃と魂を失った！";
                else if (idiomaActual.StartsWith("fr"))
                    textoMuerte = "Anubis a perdu sa série et ses âmes !";
                else
                    textoMuerte = "Anubis has lost his streak and souls!";

                CombatText.NewText(Player.getRect(), new Color(138, 43, 226), textoMuerte, true);
            }
        }

        private bool TieneAlmasAcumuladas()
        {
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 t1) && (t1.AnubisSoulsCount > 0 || t1.HostileKillsCount > 0)) return true;
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_2 t2) && (t2.AnubisSoulsCount > 0 || t2.HostileKillsCount > 0)) return true;
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_3 t3) && (t3.AnubisSoulsCount > 0 || t3.HostileKillsCount > 0)) return true;
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_4 t4) && (t4.AnubisSoulsCount > 0 || t4.HostileKillsCount > 0)) return true;
            return false;
        }

        private void SincronizarProgresoTodasLasTiers()
        {
            int maxSouls = 0;
            int maxKills = 0;

            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 t1))
            {
                maxSouls = Math.Max(maxSouls, t1.AnubisSoulsCount);
                maxKills = Math.Max(maxKills, t1.HostileKillsCount);
            }
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_2 t2))
            {
                maxSouls = Math.Max(maxSouls, t2.AnubisSoulsCount);
                maxKills = Math.Max(maxKills, t2.HostileKillsCount);
            }
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_3 t3))
            {
                maxSouls = Math.Max(maxSouls, t3.AnubisSoulsCount);
                maxKills = Math.Max(maxKills, t3.HostileKillsCount);
            }
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_4 t4))
            {
                maxSouls = Math.Max(maxSouls, t4.AnubisSoulsCount);
                maxKills = Math.Max(maxKills, t4.HostileKillsCount);
            }

            if (Player.TryGetModPlayer(out t1)) t1.SetProgreso(maxSouls, maxKills);
            if (Player.TryGetModPlayer(out t2)) t2.SetProgreso(maxSouls, maxKills);
            if (Player.TryGetModPlayer(out t3)) t3.SetProgreso(maxSouls, maxKills);
            if (Player.TryGetModPlayer(out t4)) t4.SetProgreso(maxSouls, maxKills);
        }

        public void BorrarProgresoDeTodasLasTiers(bool mostrarMensaje)
        {
            bool habiaProgreso = TieneAlmasAcumuladas();

            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 t1)) t1.LimpiarAlmasInterno();
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_2 t2)) t2.LimpiarAlmasInterno();
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_3 t3)) t3.LimpiarAlmasInterno();
            if (Player.TryGetModPlayer(out AnubisSoulPlayer_Tier_4 t4)) t4.LimpiarAlmasInterno();

            if (habiaProgreso && mostrarMensaje)
            {
                string textoDesequipar;
                string idiomaActual = Language.ActiveCulture.Name;

                if (idiomaActual.StartsWith("es"))
                    textoDesequipar = "¡Has perdido todas tus almas!";
                else if (idiomaActual.StartsWith("ja"))
                    textoDesequipar = "すべての魂を失ってしまった！";
                else if (idiomaActual.StartsWith("fr"))
                    textoDesequipar = "Tu as perdu toutes tes âmes !";
                else
                    textoDesequipar = "You have lost all your souls!";

                CombatText.NewText(Player.getRect(), new Color(138, 43, 226), textoDesequipar, true);
            }
        }
    }
}