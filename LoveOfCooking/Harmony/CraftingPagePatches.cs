﻿using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace LoveOfCooking.HarmonyPatches
{
	public static class CraftingPagePatches
	{
		public static void Patch(Harmony harmony)
		{
			Log.D($"Applying patches to CraftingPage.clickCraftingRecipe",
				ModEntry.Config.DebugMode);
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
				prefix: new HarmonyMethod(typeof(CraftingPagePatches), nameof(CraftItem_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
				postfix: new HarmonyMethod(typeof(CraftingPagePatches), nameof(CraftItem_Postfix)));
		}

        public static void CraftItem_Prefix()
		{
			ModEntry.Instance.States.Value.ItemsCooked = Game1.stats.ItemsCooked;
		}

		/// <summary>
		/// Unique crafting behaviours for when the Cooking Menu is disabled.
		/// </summary>
		public static void CraftItem_Postfix(CraftingPage __instance,
			Item ___lastCookingHover,
			ClickableTextureComponent c)
		{
			// Do nothing if the Cooking Menu is enabled
			if (ModEntry.Config.AddCookingMenu)
				return;

			if (Game1.stats.ItemsCooked <= ModEntry.Instance.States.Value.ItemsCooked)
				return;

			Item item = ___lastCookingHover;
			CraftingRecipe recipe = new CraftingRecipe(item.Name, isCookingRecipe: true);

			// Apply burn chance to destroy cooked food at random
			/*
			if (GameObjects.CookingMenu.GetBurnChance(recipe) > Game1.random.NextDouble())
			{
				// Add burnt food to inventory if possible
				var burntItem = new StardewValley.Object(Interface.Interfaces.JsonAssets.GetObjectId(ModEntry.ObjectPrefix + "burntfood"), 1);
				ModEntry.AddOrDropItem(burntItem);

				// Hunt down the usual food to be incinerated
				if (Game1.player.hasItemInInventoryNamed(item.Name))
				{
					var items = Game1.player.Items.Where(i => i.Name == item.Name).ToArray();
					var index = Game1.player.Items.IndexOf(items[Game1.random.Next(items.Length)]);
					--Game1.player.Items[index].Stack;
					if (Game1.player.Items[index].Stack < 1)
						Game1.player.removeItemFromInventory(index);
				}
				else if (Game1.player.isInventoryFull())
				{
					var items = Game1.currentLocation.debris.Where(d => d.item.Name == item.Name).ToArray();
					// . . .
				}
			}
			*/

			if (ModEntry.Config.AddCookingSkillAndRecipes)
			{
				// TODO: SYSTEM: Finish integrating cooking skill profession bonuses into cooking minus new menu

				// Apply extra portion bonuses to the amount cooked
				if (ModEntry.CookingSkillApi.HasProfession(Objects.ICookingSkillAPI.Profession.ExtraPortion) && ModEntry.CookingSkillApi.RollForExtraPortion())
				{
					//qualityStacks[0] += numPerCraft;
				}

				// Update tracked stats
				if (!ModEntry.Instance.States.Value.FoodCookedToday.ContainsKey(item.Name))
					ModEntry.Instance.States.Value.FoodCookedToday[item.Name] = 0;
				ModEntry.Instance.States.Value.FoodCookedToday[item.Name] += item.Stack;

				// Add cooking skill experience
				ModEntry.CookingSkillApi.CalculateExperienceGainedFromCookingItem(
					item, numIngredients: recipe.getNumberOfIngredients(), item.Stack, applyExperience: true);
			}
		}
	}
}
