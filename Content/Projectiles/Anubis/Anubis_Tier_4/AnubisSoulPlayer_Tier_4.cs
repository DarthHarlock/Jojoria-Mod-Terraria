using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using System;
using Jojo.Content.UI;

namespace Jojo.Content.Players
{
    public class AnubisSoulPlayer_Tier_4 : ModPlayer
    {
        public int AnubisSoulsCount { get; private set; } = 0;
        public int HostileKillsCount { get; private set; } = 0;
        public bool AnubisEquipado = false;
        public bool fundaAnubisEquipada = false;
        public bool usandoSkinRed = false;

        public float AnubisDamageMultiplier => 1f + (AnubisSoulsCount * 0.002f);

        public override void Initialize()
        {
            AnubisSoulsCount = 0;
            HostileKillsCount = 0;
            AnubisEquipado = false;
            fundaAnubisEquipada = false;
            usandoSkinRed = false;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["AnubisSoulsCount"] = AnubisSoulsCount;
            tag["HostileKillsCount"] = HostileKillsCount;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("AnubisSoulsCount"))
            {
                AnubisSoulsCount = tag.GetInt("AnubisSoulsCount");
            }
            if (tag.ContainsKey("HostileKillsCount"))
            {
                HostileKillsCount = tag.GetInt("HostileKillsCount");
            }
        }

        public override void ResetEffects()
        {
            fundaAnubisEquipada = false;
            usandoSkinRed = StandSlotSystem.HasRedSkinFor(Player);
            AnubisEquipado = false;
        }

        public void SetProgreso(int almas, int kills)
        {
            AnubisSoulsCount = almas;
            HostileKillsCount = kills;
        }

        public void LimpiarAlmasInterno()
        {
            AnubisSoulsCount = 0;
            HostileKillsCount = 0;
        }

        public void CosecharEnemigo(NPC npc)
        {
            if (npc.friendly || npc.damage <= 0 || npc.SpawnedFromStatue || npc.CountsAsACritter)
                return;

            HostileKillsCount++;
            string idiomaActual = Language.ActiveCulture.Name;

            if (npc.boss)
            {
                AnubisSoulsCount += 7;
                float porcentajeMostrar = AnubisSoulsCount * 0.2f;

                string textoBoss;
                if (idiomaActual.StartsWith("es"))
                    textoBoss = $"(+{porcentajeMostrar:0.0}%) Gran enemigo derrotado (+1.4%)";
                else if (idiomaActual.StartsWith("ja"))
                    textoBoss = $"(+{porcentajeMostrar:0.0}%) 強敵を撃破 (+1.4%)";
                else if (idiomaActual.StartsWith("fr"))
                    textoBoss = $"(+{porcentajeMostrar:0.0}%) Grand ennemi vaincu (+1.4%)";
                else
                    textoBoss = $"(+{porcentajeMostrar:0.0}%) Great enemy defeated (+1.4%)";

                CombatText.NewText(npc.getRect(), new Color(255, 215, 0), textoBoss, true);
            }
            else
            {
                float gananciaPorcentaje = 0.2f;
                Color colorTexto = new Color(186, 85, 211);

                if (npc.lifeMax >= 800)
                {
                    AnubisSoulsCount += 3;
                    gananciaPorcentaje = 0.6f;
                    colorTexto = new Color(255, 105, 180);
                }
                else if (npc.lifeMax >= 300)
                {
                    AnubisSoulsCount += 2;
                    gananciaPorcentaje = 0.4f;
                    colorTexto = new Color(30, 144, 255);
                }
                else
                {
                    AnubisSoulsCount += 1;
                    gananciaPorcentaje = 0.2f;
                    colorTexto = new Color(186, 85, 211);
                }

                float porcentajeMostrar = AnubisSoulsCount * 0.2f;

                string textoEnemigo;
                if (idiomaActual.StartsWith("es"))
                    textoEnemigo = $"(+{porcentajeMostrar:0.0}%) Enemigo derrotado (+{gananciaPorcentaje:0.0}%)";
                else if (idiomaActual.StartsWith("ja"))
                    textoEnemigo = $"(+{porcentajeMostrar:0.0}%) 敵を撃破 (+{gananciaPorcentaje:0.0}%)";
                else if (idiomaActual.StartsWith("fr"))
                    textoEnemigo = $"(+{porcentajeMostrar:0.0}%) Ennemi vaincu (+{gananciaPorcentaje:0.0}%)";
                else
                    textoEnemigo = $"(+{porcentajeMostrar:0.0}%) Enemy defeated (+{gananciaPorcentaje:0.0}%)";

                CombatText.NewText(npc.getRect(), colorTexto, textoEnemigo, false);
            }
        }
    }
}