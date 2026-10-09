using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Personalities;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using Jojo.Content.Items;
using Jojo.Content.Items.Cosmeticos;

namespace Jojo.Content.NPCs.TownNPCs
{
    [AutoloadHead]
    public class Jotaro : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;
            NPCID.Sets.AttackType[NPC.type] = 0;
            NPCID.Sets.AttackTime[NPC.type] = 90;
            NPCID.Sets.AttackAverageChance[NPC.type] = 30;

            NPC.Happiness
                .SetBiomeAffection<OceanBiome>(AffectionLevel.Love)
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Hate);
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;

            AnimationType = NPCID.Guide;
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string>() { "Jotaro" };
        }

        // --- EFECTO DE GOLPE Y MUERTE (SANGRE Y GORE) ---
        public override void HitEffect(NPC.HitInfo hit)
        {
            // 1. Genera partículas de sangre con cada impacto
            int dustAmount = NPC.life <= 0 ? 30 : 5;
            for (int i = 0; i < dustAmount; i++)
            {
                Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.Blood,
                    hit.HitDirection * 2f,
                    -1f,
                    0,
                    default,
                    1f
                );
            }

            // 2. Al morir, suelta las piezas de gore (Evitando que se ejecute en el servidor dedicado)
            if (Main.netMode != NetmodeID.Server && NPC.life <= 0)
            {
                // Ahora usamos ModContent.GoreType referenciando las clases creadas abajo.
                // Esto es 100% seguro y evita los errores de diccionario.
                int gore1 = ModContent.GoreType<JotaroGore_1>();
                int gore2 = ModContent.GoreType<JotaroGore_2>();
                int gore3 = ModContent.GoreType<JotaroGore_3>();

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, gore1);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, gore2);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, gore3);
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,
                new FlavorTextBestiaryInfoElement("Mods.Jojo.Bestiary.Jotaro")
            });
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StarPlatinum_Blue>(), 1));
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return true;
        }

        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            JojoPlayer modPlayer = player.GetModPlayer<JojoPlayer>();
            string lang = Language.ActiveCulture.Name;

            if (!modPlayer.haRecibidoFlecha)
            {
                if (lang == "es-ES") return "He viajado desde muy lejos para encontrar a alguien digno, ¿quieres probar?";
                if (lang == "fr-FR") return "J'ai voyagé de très loin pour trouver quelqu'un de digne, tu veux essayer ?";
                return "I have traveled from afar to find someone worthy, want to try?";
            }
            else
            {
                WeightedRandom<string> chat = new WeightedRandom<string>();

                if (lang == "es-ES")
                {
                    chat.Add("Yare yare daze...");
                    chat.Add("Un Stand es el reflejo de tu propia alma. Úsalo con sabiduría.");
                    chat.Add("Esos delfines en la costa... son criaturas fascinantes. Mucho más interesantes que la mayoría de humanos.");
                    chat.Add("Tsk... mi hija. Supongo que debería llamarla y pasar más tiempo con ella. No es que me importe demasiado, pero mi esposa se enfadará si no lo hago.");
                }
                else if (lang == "fr-FR")
                {
                    chat.Add("Yare yare daze...");
                    chat.Add("Un Stand est le reflet de ton âme. Utilise-le bien.");
                    chat.Add("Ces dauphins sur la côte... ce sont des créatures fascinantes. Bien plus intéressants que la plupart des humains.");
                    chat.Add("Tsk... ma fille. Je suppose que je devrais l'appeler et passer plus de temps avec elle. Ce n'est pas que ça m'importe beaucoup, mais ma femme va s'énerver sinon.");
                }
                else
                {
                    chat.Add("Yare yare daze...");
                    chat.Add("A Stand is the reflection of your soul. Use it wisely.");
                    chat.Add("Those dolphins on the coast... they are fascinating creatures. Much more interesting than most humans.");
                    chat.Add("Tsk... my daughter. I suppose I should call her and spend more time with her. Not that I care too much, but my wife will get mad if I don't.");
                }

                return chat.Get();
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            Player player = Main.LocalPlayer;
            JojoPlayer modPlayer = player.GetModPlayer<JojoPlayer>();
            string lang = Language.ActiveCulture.Name;

            if (!modPlayer.haRecibidoFlecha)
            {
                if (lang == "es-ES") button = "Recibir fragmento";
                else if (lang == "fr-FR") button = "Recevoir un fragment";
                else button = "Receive fragment";
            }
            else
            {
                if (player.ZoneBeach && player.currentShoppingSettings.PriceAdjustment <= 1f)
                {
                    if (lang == "es-ES") button = "Tienda";
                    else if (lang == "fr-FR") button = "Boutique";
                    else button = "Shop";
                }
            }
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            Player player = Main.LocalPlayer;
            JojoPlayer modPlayer = player.GetModPlayer<JojoPlayer>();

            if (firstButton)
            {
                if (!modPlayer.haRecibidoFlecha)
                {
                    // Cambiado aquí: Ahora da Flecha_Stand_Fragmento
                    player.QuickSpawnItem(NPC.GetSource_GiftOrReward(), ModContent.ItemType<Flecha_Stand_Fragmento>());
                    modPlayer.haRecibidoFlecha = true;

                    string lang = Language.ActiveCulture.Name;
                    if (lang == "es-ES") Main.npcChatText = "Toma esto. Sobrevive y despierta tu poder.";
                    else if (lang == "fr-FR") Main.npcChatText = "Prends ça. Survis et éveille ton pouvoir.";
                    else Main.npcChatText = "Take this. Survive and awaken your power.";
                }
                else if (player.ZoneBeach && player.currentShoppingSettings.PriceAdjustment <= 1f)
                {
                    shopName = "Shop";
                }
            }
        }

        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, "Shop");
            // Cambiado aquí: Ahora también vende el fragmento en lugar de la flecha entera
            var flechaVenta = new Item(ModContent.ItemType<Flecha_Stand_Fragmento>());
            flechaVenta.shopCustomPrice = Item.buyPrice(0, 50, 0, 0);
            npcShop.Add(flechaVenta);
            npcShop.Register();
        }
    }

    // --- REGISTRO MANUAL DEL GORE ---
    public class JotaroGore_1 : ModGore
    {
        public override string Texture => "Jojo/Content/NPCs/TownNPCs/JotaroGore/Gore_1";
    }

    public class JotaroGore_2 : ModGore
    {
        public override string Texture => "Jojo/Content/NPCs/TownNPCs/JotaroGore/Gore_2";
    }

    public class JotaroGore_3 : ModGore
    {
        public override string Texture => "Jojo/Content/NPCs/TownNPCs/JotaroGore/Gore_3";
    }
}