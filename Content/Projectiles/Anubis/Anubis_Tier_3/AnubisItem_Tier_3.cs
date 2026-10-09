using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_3;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_2;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Items;
using Terraria.Localization;

namespace Jojo.Content.Projectiles.Anubis.Anubis_Tier_3
{
    public class AnubisItem_Tier_3 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 20; // Daño base inmóvil
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.MoradoAnubis>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<ANUBISSTAND_Tier_3>();

            if (player.ownedProjectileCounts[type] == 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_Accessory(Item),
                    player.Center,
                    Vector2.Zero,
                    type,
                    Item.damage,
                    2f,
                    player.whoAmI
                );
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Obtenemos la textura del ítem
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Item.type].Value;

            // Multiplicamos la escala por 1.5 para mantener el tamaño personalizado sin encogerse al arrastrarlo o en el Hero's Mod
            float inventoryScale = scale * 1.5f;

            spriteBatch.Draw(
                texture,
                position,
                frame,
                drawColor,
                0f,
                origin,
                inventoryScale,
                SpriteEffects.None,
                0f
            );

            // Retornamos false para evitar que Terraria lo dibuje con la escala pequeña por defecto
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            int realCrit = StandCritSystem.GetFinalCritChance(player, baseCrit);
            int bajasHostiles = 0;
            float multiplicadorAlmas = 1f;
            float bonusPorcentajeEtiqueta = 0f;

            if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_3 anubisPlayer))
            {
                bajasHostiles = anubisPlayer.HostileKillsCount;
                multiplicadorAlmas = anubisPlayer.AnubisDamageMultiplier;
                bonusPorcentajeEtiqueta = anubisPlayer.AnubisSoulsCount * 0.2f;
            }

            string idiomaActual = Language.ActiveCulture.Name;

            foreach (var line in tooltips)
            {
                if (line.Mod == "Terraria" && line.Name == "ItemName")
                {
                    if (line.Text.Contains("Tier 3"))
                    {
                        line.Text = line.Text.Replace("Tier 3", "[c/F2F2F2:Tier 3]");
                    }
                }

                if (line.Mod == "Terraria" && line.Name == "Damage")
                {
                    float dañoConAlmas = Item.damage * multiplicadorAlmas;

                    string textoDaño;
                    if (idiomaActual.StartsWith("es")) textoDaño = "daño de Stand";
                    else if (idiomaActual.StartsWith("ja")) textoDaño = "スタンドダメージ";
                    else if (idiomaActual.StartsWith("fr")) textoDaño = "dégâts de Stand";
                    else textoDaño = "Stand damage";

                    line.Text = $"{player.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(dañoConAlmas):0} {textoDaño}";
                }

                if (line.Name == "CritChance" && line.Mod == "Terraria")
                {
                    string critText = Terraria.Localization.Language.GetTextValue("LegacyTooltip.5");
                    line.Text = realCrit + critText;
                }
            }

            if (bonusPorcentajeEtiqueta > 0f)
            {
                string textoTooltip;

                if (idiomaActual.StartsWith("es"))
                {
                    textoTooltip = $"[c/BA55D3:Combates victoriosos: {bajasHostiles} (+{bonusPorcentajeEtiqueta:0.0}% daño total)]";
                }
                else if (idiomaActual.StartsWith("ja"))
                {
                    textoTooltip = $"[c/BA55D3:撃破した敵の数: {bajasHostiles} (+{bonusPorcentajeEtiqueta:0.0}% 総ダメージ)]";
                }
                else if (idiomaActual.StartsWith("fr"))
                {
                    textoTooltip = $"[c/BA55D3:Créatures hostiles vaincues : {bajasHostiles} (+{bonusPorcentajeEtiqueta:0.0}% dégâts totaux)]";
                }
                else
                {
                    textoTooltip = $"[c/BA55D3:Combates victoriosos: {bajasHostiles} (+{bonusPorcentajeEtiqueta:0.0}% total damage)]";
                }

                TooltipLine lineaContador = new TooltipLine(
                    Mod,
                    "AnubisKillsYPorcentaje",
                    textoTooltip
                );
                tooltips.Add(lineaContador);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<AnubisItem_Tier_2>(), 1)
                .AddIngredient(ModContent.ItemType<Voluntad_Tenebrosa>(), 10)
                .AddRecipeGroup("Jojo:TitaniumOrAdamantite", 10)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddIngredient(ItemID.DesertFossil, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}