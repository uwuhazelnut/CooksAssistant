﻿using System;
using System.Collections.Generic;
using System.Linq;

using StardewModdingAPI;
using StardewValley;
using xTile;

namespace CooksAssistant
{
	public class AssetManager : IAssetEditor
	{
		private static Config Config => ModEntry.Instance.Config;
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;

		public AssetManager() {}
		
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return Game1.player != null 
			       && (asset.AssetNameEquals(@"Data/CookingRecipes")
			           || asset.AssetNameEquals(@"Data/ObjectInformation")
			           || asset.AssetNameEquals(@"Data/Events/Saloon")
			           || asset.AssetNameEquals(@"Data/Events/Mountain")
			           || asset.AssetNameEquals(@"Data/Events/JoshHouse")
			           || asset.AssetNameEquals(@"Maps/Beach")
			           || asset.AssetNameEquals(@"Maps/Saloon"));
		}

		public void Edit<T>(IAssetData asset)
		{
			if (asset.AssetNameEquals(@"Data/CookingRecipes"))
			{
				// Edit fields of vanilla recipes to use new ingredients

				if (ModEntry.JsonAssets == null)
					return;
				if (!Config.MakeChangesToRecipes)
				{
					Log.D($"Did not patch {asset.AssetName}: Recipe patches are disabled in config file.",
						Config.DebugMode);
					return;
				}

				try
				{
					var data = asset.AsDictionary<string, string>().Data;
					var recipeData = new Dictionary<string, string>
					{
						// Maki Roll: // Sashimi 1 Seaweed 1 Rice 1
						{
							"Maki Roll",
							"227 1 152 1 423 1"
						},
						// Coleslaw: Vinegar 1 Mayonnaise 1
						{
							"Coleslaw",
							$"{ModEntry.JsonAssets.GetObjectId("Cabbage")} 1" + " 419 1 306 1"
						},
						// Pink Cake: Cake 1 Melon 1
						{
							"Pink Cake",
							$"{ModEntry.JsonAssets.GetObjectId("Cake")} 1" + " 254 1"
						},
						// Chocolate Cake: Cake 1 Chocolate Bar 1
						{
							"Chocolate Cake",
							$"{ModEntry.JsonAssets.GetObjectId("Cake")} 1" 
							+ $" {ModEntry.JsonAssets.GetObjectId("Chocolate Bar")} 1"
						},
						// Cookies: Flour 1 Category:Egg 1 Chocolate Bar 1
						{
							"Cookies",
							"246 1 -5 1" + $" {ModEntry.JsonAssets.GetObjectId("Chocolate Bar")} 1"
						},
						// Pizza: Flour 2 Tomato 2 Cheese 2
						{
							"Pizza",
							"246 2 256 2 424 2"
						},
					};
					foreach (var recipe in recipeData)
						data[recipe.Key] = ModEntry.UpdateEntry(data[recipe.Key], new [] {recipe.Value});
					
					// Remove Oil from all cooking recipes in the game
					foreach (var recipe in data.ToDictionary(pair => pair.Key, pair => pair.Value))
					{
						var recipeSplit = data[recipe.Key].Split('/');
						var ingredients = recipeSplit[0].Split(' ');
						if (!ingredients.Contains("247"))
							continue;

						recipeSplit[0] = ModEntry.UpdateEntry(recipeSplit[0],
							ingredients.Where((ingredient, i) => 
								ingredient != "247" && (i <= 0 || ingredients[i - 1] != "247")).ToArray(), 
							false, true, 0, ' ');
						data[recipe.Key] = ModEntry.UpdateEntry(data[recipe.Key], recipeSplit, false, true);
					}

					asset.AsDictionary<string, string>().ReplaceWith(data);

					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => recipeData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.AssetNameEquals(@"Data/ObjectInformation"))
			{
				// Edit fields of vanilla objects to revalue and recategorise some produce
				if (!Config.MakeChangesToIngredients)
				{
					Log.D($"Did not patch {asset.AssetName}: Ingredients patches are disabled in config file.",
						Config.DebugMode);
					return;
				}

				try
				{
					var data = asset.AsDictionary<int, string>().Data;
					var objectData = new Dictionary<int, string[]>
					{
						{206, new[] {null, null, "45"}}, // Pizza
						{220, new[] {null, null, "60"}}, // Chocolate Cake
						{221, new[] {null, null, "75"}}, // Pink Cake
						{419, new[] {null, "220", "-300", "Basic -26"}}, // Vinegar
						{247, new[] {null, null, "-300", "Basic -26", null, i18n.Get("item.oil.description")}}, // Oil
						{432, new[] {null, null, "-300", null, null, i18n.Get("item.truffleoil.description")}}, // Truffle Oil
						{ModEntry.JsonAssets.GetObjectId("Sugar Cane"), new[] {null, null, null, "Basic"}},
					};
					foreach (var obj in objectData.Where(o =>
						Config.GiveLeftoversFromBigFoods || !Config.FoodsThatGiveLeftovers.Contains(o.Value[0])))
						data[obj.Key] = ModEntry.UpdateEntry(data[obj.Key], obj.Value);

					asset.AsDictionary<int, string>().ReplaceWith(data);

					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => objectData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.AssetNameEquals(@"Data/Monsters"))
			{
				try
				{
					var data = asset.AsDictionary<string, string>().Data;
					var monsterData = new Dictionary<string, string[]>
					{
						{"Shadow Shaman", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0035"
						                         + $" {ModEntry.JsonAssets.GetObjectId("Nettles")} .05"}},
						{"Wilderness Golem", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0065"}},
						{"Mummy", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0022"}},
						{"Pepper Rex", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .02"}},
					};
					foreach (var monster in monsterData)
						data[monster.Key] = ModEntry.UpdateEntry(data[monster.Key], monster.Value, true);

					asset.AsDictionary<string, string>().ReplaceWith(data);
					
					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => monsterData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.DataType == typeof(IDictionary<string, string>) && !Config.PlayWithQuestline)
			{
				Log.D($"Did not patch {asset.AssetName}: Quest patches are disabled in config file.",
					Config.DebugMode);
				return;
			}
			
			if (asset.DataType == typeof(Map) && !Config.MakeChangesToMaps)
			{
				Log.D($"Did not patch {asset.AssetName}: Map patches are disabled in config file.",
					Config.DebugMode);
				return;
			}

			switch (asset.AssetName)
			{
				case @"Maps/Beach":
					// Add dock wares to the secret beach

					break;

				case @"Maps/Saloon":
					// Add a cooking range to Gus' saloon
					
					var saloonCooktop = Config.WhereToPutTheSaloonCookingStation.ConvertAll(int.Parse);
					for (var x = saloonCooktop[0]; x < saloonCooktop[0] + 1; ++x)
						asset.AsMap().Data.GetLayer("Buildings").Tiles[saloonCooktop[0], saloonCooktop[1]]
							.Properties.Add("Action", ModEntry.ActionRange);

					break;

				case @"Maps/FarmHouse":
					// Add a cooking range to the farmhouse

					break;
			}
		}
	}
}
