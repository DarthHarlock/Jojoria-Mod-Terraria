using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JusticeMinionPlayer : ModPlayer
    {
        public List<int> savedMinionTypes = new List<int>();

        private int graceTimer = 0;
        private int cooldownSenal = 0;
        private bool estabaEquipado = false;

        private bool EsMio(NPC npc)
        {
            if (!npc.active) return false;
            JusticeGlobalNPC g = npc.GetGlobalNPC<JusticeGlobalNPC>();
            return g.bajoControlMental && g.duenoNombre == Player.name;
        }

        // ¿Tiene el jugador un item de Justice (cualquier tier) en la ranura de Stand?
        // Desinvocar el stand NO cuenta como desequipar.
        private bool StandEquipado()
        {
            StandSlotPlayer slot = Player.GetModPlayer<StandSlotPlayer>();
            Item it = slot.standItem;
            return it != null && !it.IsAir && it.ModItem != null
                && it.ModItem.Name.StartsWith("JusticeItem_Tier_");
        }

        private bool TengoMinions()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (EsMio(Main.npc[i])) return true;
            }
            return false;
        }

        private void LiberarMisMinionsDirecto()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;

                if (EsMio(npc))
                    JusticeGlobalNPC.Liberar(npc);
            }
        }

        private void EnviarSenalLiberar()
        {
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<JusticeSenalLiberar>(),
                0,
                0f,
                Player.whoAmI
            );
        }

        public override void PostUpdateEquips()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            if (graceTimer > 0)
            {
                graceTimer--;
                return;
            }

            if (cooldownSenal > 0) cooldownSenal--;

            if (StandEquipado())
            {
                estabaEquipado = true;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                LiberarMisMinionsDirecto();
                estabaEquipado = false;
                return;
            }

            if (estabaEquipado)
            {
                estabaEquipado = false;
                cooldownSenal = 30;
                EnviarSenalLiberar();
            }
            else if (cooldownSenal <= 0 && TengoMinions())
            {
                cooldownSenal = 30;
                EnviarSenalLiberar();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            List<int> toSave = new List<int>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;

                if (EsMio(npc))
                {
                    toSave.Add(npc.type);
                }
            }
            tag["JusticeMinions"] = toSave;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("JusticeMinions"))
            {
                savedMinionTypes = tag.Get<List<int>>("JusticeMinions");
            }
        }

        public override void OnEnterWorld()
        {
            graceTimer = 120;
            estabaEquipado = false;

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (savedMinionTypes != null && savedMinionTypes.Count > 0)
            {
                foreach (int type in savedMinionTypes)
                {
                    int npcIndex = NPC.NewNPC(
                        Player.GetSource_FromThis(),
                        (int)Player.Center.X,
                        (int)Player.Center.Y - 50,
                        type
                    );

                    if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                    {
                        JusticeGlobalNPC.Controlar(Main.npc[npcIndex], Player.whoAmI);
                    }
                }
                savedMinionTypes.Clear();
            }
        }
    }

    public class JusticeSenalLiberar : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private bool aplicado = false;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Aplicar();
        }

        public override void AI()
        {
            Aplicar();
        }

        private void Aplicar()
        {
            if (aplicado) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            aplicado = true;

            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            string nombre = Main.player[Projectile.owner].name;
            if (string.IsNullOrEmpty(nombre)) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;

                JusticeGlobalNPC g = npc.GetGlobalNPC<JusticeGlobalNPC>();
                if (g.bajoControlMental && g.duenoNombre == nombre)
                {
                    JusticeGlobalNPC.Liberar(npc);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}