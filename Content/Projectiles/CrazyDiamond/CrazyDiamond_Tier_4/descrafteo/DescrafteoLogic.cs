using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Jojo.Content.Projectiles.CrazyDiamond.descrafteo
{
    public class DescrafteoLogic
    {
        // Receta por objeto resultante (se construye una sola vez)
        private static Dictionary<int, Recipe> recipeCache;

        // Objetos que SOLO existen por el Fulgor (resultado de transformacion y sin receta)
        private static HashSet<int> shimmerExclusive;

        private static void BuildCache()
        {
            // 1. Todos los objetos que tienen receta
            HashSet<int> hasRecipe = new HashSet<int>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.Disabled) continue;
                if (r.createItem == null || r.createItem.type == ItemID.None) continue;
                hasRecipe.Add(r.createItem.type);
            }

            // 2. Exclusivos del Fulgor = resultado de transformacion del Fulgor y sin receta
            shimmerExclusive = new HashSet<int>();
            for (int i = 0; i < ItemID.Sets.ShimmerTransformToItem.Length; i++)
            {
                int result = ItemID.Sets.ShimmerTransformToItem[i];
                if (result > 0 && !hasRecipe.Contains(result))
                    shimmerExclusive.Add(result);
            }

            // 3. Cache de recetas validas (ignorando las que usen ingredientes exclusivos del Fulgor)
            recipeCache = new Dictionary<int, Recipe>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.Disabled) continue;
                if (r.createItem == null || r.createItem.type == ItemID.None) continue;
                if (recipeCache.ContainsKey(r.createItem.type)) continue;
                if (UsaIngredienteDelFulgor(r)) continue;

                recipeCache[r.createItem.type] = r;
            }
        }

        private static bool UsaIngredienteDelFulgor(Recipe r)
        {
            foreach (Item ing in r.requiredItem)
            {
                if (ing == null || ing.type == ItemID.None) continue;
                if (shimmerExclusive.Contains(ing.type)) return true;
            }
            return false;
        }

        // Llamar si otros mods cambian recetas tras la carga (opcional)
        public static void ResetCache()
        {
            recipeCache = null;
            shimmerExclusive = null;
        }

        // Devuelve true si el objeto fue descrafteado con exito.
        // No usa el Fulgor y no depende de ningun jefe derrotado.
        public static bool IntentarDescraftear(Item item, Vector2 hitPosition)
        {
            if (item == null || !item.active || item.type == ItemID.None) return false;

            if (recipeCache == null) BuildCache();

            // Los objetos exclusivos del Fulgor no se tocan
            if (shimmerExclusive.Contains(item.type)) return false;

            if (!recipeCache.TryGetValue(item.type, out Recipe foundRecipe)) return false;
            if (item.stack < foundRecipe.createItem.stack) return false;

            // Quitamos del suelo lo que produce la receta
            item.stack -= foundRecipe.createItem.stack;
            if (item.stack <= 0)
                item.TurnToAir();

            // Soltamos los materiales originales
            foreach (Item ingredient in foundRecipe.requiredItem)
            {
                if (ingredient == null || ingredient.type == ItemID.None) continue;

                int dropped = Item.NewItem(
                    item.GetSource_Misc("CrazyDiamondDecraft"),
                    (int)hitPosition.X, (int)hitPosition.Y, 0, 0,
                    ingredient.type, ingredient.stack);

                if (Main.netMode == NetmodeID.Server && dropped >= 0 && dropped < Main.maxItems)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, dropped, 1f);
            }

            // Sincronizamos el objeto original
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI, 1f);

            GenerarEfectos(hitPosition);
            return true;
        }

        private static void GenerarEfectos(Vector2 posicion)
        {
            SoundEngine.PlaySound(SoundID.Item29, posicion);

            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(posicion, DustID.PinkFairy, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.5f);
                d.noGravity = true;
            }
        }
    }
}