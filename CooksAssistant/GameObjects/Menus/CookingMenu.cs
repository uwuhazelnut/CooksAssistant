﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceCore;

namespace CooksAssistant.GameObjects.Menus
{
	public class CookingMenu : ItemGrabMenu
	{
		private static Texture2D Texture => ModEntry.SpriteSheet;
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;

		// Layout dimensions (variable with screen size)
		private static Rectangle _cookbookLeftRect = new Rectangle(-1, -1, 240 * 4 / 2, 128 * 4);
		private static Rectangle _cookbookRightRect = new Rectangle(-1, -1, 240 * 4 / 2, 128 * 4);
		private static Point _leftContent;
		private static Point _rightContent;
		private static int _lineWidth;
		private static int _textWidth;

		// Layout definitions
		private const int MarginLeft = 80;
		private const int MarginRight = 32;
		private const int TextMuffinTopOverDivider = 6;
		private const int TextDividerGap = 4;

		private static readonly Color SubtextColour = Game1.textColor * 0.75f;
		private static readonly Color BlockedColour = Game1.textColor * 0.325f;
		private static readonly Color DividerColour = Game1.textColor * 0.325f;

		// Spritesheet source areas
		// Custom spritesheet
		private static readonly Rectangle CookbookSource = new Rectangle(0, 80, 240, 128);
		private static readonly Rectangle CookingSlotOpenSource = new Rectangle(0, 208, 28, 28);
		private static readonly Rectangle CookingSlotLockedSource = new Rectangle(28, 208, 28, 28);
		private static readonly Rectangle CookButtonSource = new Rectangle(128, 0, 16, 22);
		private static readonly Rectangle SearchTabButtonSource = new Rectangle(48, 0, 16, 16);
		private static readonly Rectangle FilterContainerSource = new Rectangle(58, 208, 9, 20);
		private static readonly Rectangle FilterIconSource = new Rectangle(69, 208, 12, 12);
		private static readonly Rectangle FavouriteButtonSource = new Rectangle(140, 208, 12, 12);
		private static readonly Rectangle ToggleViewButtonSource = new Rectangle(80, 224, 16, 16);
		private static readonly Rectangle ToggleFilterButtonSource = new Rectangle(112, 224, 16, 16);
		private static readonly Rectangle ToggleOrderButtonSource = new Rectangle(128, 224, 16, 16);
		private static readonly Rectangle SearchButtonSource = new Rectangle(144, 224, 16, 16);
		// MouseCursors sheet
		private static readonly Rectangle DownButtonSource = new Rectangle(0, 64, 64, 64);
		private static readonly Rectangle UpButtonSource = new Rectangle(64, 64, 64, 64);
		private static readonly Rectangle RightButtonSource = new Rectangle(0, 192, 64, 64);
		private static readonly Rectangle LeftButtonSource = new Rectangle(0, 256, 64, 64);
		private static readonly Rectangle PlusButtonSource = new Rectangle(184, 345, 7, 8);
		private static readonly Rectangle MinusButtonSource = new Rectangle(177, 345, 7, 8);
		private static readonly Rectangle OkButtonSource = new Rectangle(128, 256, 64, 64);
		private static readonly Rectangle NoButtonSource = new Rectangle(192, 256, 64, 64);
		// Other variables
		private static readonly Dictionary<string, Rectangle> CookTextSource = new Dictionary<string, Rectangle>();
		private static readonly Point CookTextSourceOrigin = new Point(0, 240);
		private readonly Dictionary<string, int> CookTextSourceWidths;
		private const int CookTextSourceHeight = 16;
		private const int CookTextSideWidth = 5;
		private int CookTextMiddleWidth;
		private const int FilterContainerSideWidth = 4;
		private int FilterContainerMiddleWidth;
		private const int Scale = 4;
		// TODO: POLISH: add Margin rectangle for use ie. Margin.Bottom, Margin.Left

		// Clickables
		private readonly ClickableTextureComponent NavDownButton;
		private readonly ClickableTextureComponent NavUpButton;
		private readonly ClickableTextureComponent NavRightButton;
		private readonly ClickableTextureComponent NavLeftButton;
		private readonly List<ClickableTextureComponent> CookingSlots = new List<ClickableTextureComponent>();
		private Rectangle CookButtonBounds;
		private readonly ClickableTextureComponent CookQuantityUpButton;
		private readonly ClickableTextureComponent CookQuantityDownButton;
		private readonly ClickableTextureComponent CookConfirmButton;
		private readonly ClickableTextureComponent CookCancelButton;
		private Rectangle CookIconBounds;
		private readonly ClickableTextureComponent SearchTabButton;
		private readonly ClickableTextureComponent ToggleOrderButton;
		private readonly ClickableTextureComponent ToggleFilterButton;
		private readonly ClickableTextureComponent ToggleViewButton;
		private readonly ClickableTextureComponent SearchButton;
		private readonly ClickableTextureComponent FavouriteButton;
		private Rectangle FilterContainerBounds;
		private readonly List<ClickableTextureComponent> FilterButtons = new List<ClickableTextureComponent>();
		private Rectangle SearchResultsArea;

		// Text entry
		private readonly TextBox SearchBarTextBox;
		private readonly TextBox QuantityTextBox;
		private Rectangle QuantityTextBoxBounds;
		private Rectangle SearchBarTextBoxBounds;
		private readonly int SearchBarTextBoxMinWidth;
		private int SearchBarTextBoxMaxWidth;
		private readonly string _textBoxDefaultText = "  1";

		// Menu data
		public enum State
		{
			Opening,
			Search,
			Recipe
		}
		private readonly Stack<State> _stack = new Stack<State>();
		private List<CraftingRecipe> _unlockedCookingRecipes;
		private List<CraftingRecipe> _filteredRecipeList;
		private List<CraftingRecipe> _searchRecipes;
		private int _currentRecipe;
		private Item _recipeItem;
		private string _recipeDescription;
		private Dictionary<int, int> _recipeIngredients;
		private List<int> _recipeBuffs;
		private int _recipesPerPage;
		private int _recipeHeight;
		private readonly int _cookingSlots;
		private List<Item> _cookingSlotsDropIn;
		private bool _showCookingConfirmPopup;
		private bool _showSearchFilters;
		private Filter _lastFilterUsed;
		private bool _lastFilterReversed;
		private double _mouseHeldStartTime;
		private readonly int _mouseHeldDelay;
		private readonly int _mouseHeldLongDelay;
		private string _locale;

		private enum Filter
		{
			None,
			Alphabetical,
			Healing,
			Value,
			Quest,
			New,
			Unknown,
			Favourite,
			Count
		}

		// Animations
		private static readonly int[] _animTextOffsetPerFrame = { 0, 1, 0, -1, -2, -3, -2, -1 };
		private const int _animFrameTime = 100;
		private const int _animFrames = 8;
		private const int _animTimerLimit = _animFrameTime * _animFrames;
		private int _animTimer;
		private int _animFrame;

		// Others
		private readonly IReflectedField<Dictionary<int, double>> _iconShakeTimerField;

		public CookingMenu(bool addDummyState = false, string initialRecipe = null) : base(null)
		{
			ModEntry.RemoveCookingMenuButton();
			Game1.displayHUD = false;
			_locale = LocalizedContentManager.CurrentLanguageCode.ToString();
			initializeUpperRightCloseButton();
			trashCan = null;

			_iconShakeTimerField = ModEntry.Instance.Helper.Reflection
				.GetField<Dictionary<int, double>>(inventory, "_iconShakeTimer");

			// Start off the list of cooking recipes with all those the player has unlocked
			_unlockedCookingRecipes = Utility.GetAllPlayerUnlockedCookingRecipes()
				.Select(str => new CraftingRecipe(str, true))
				.Where(recipe => recipe.name != "Torch").ToList();

			_filteredRecipeList = _unlockedCookingRecipes;
			_searchRecipes = new List<CraftingRecipe>();
			_lastFilterUsed = Filter.None;
			_lastFilterReversed = false;

			// Cooking ingredients item drop-in slots
			//var cookingLevel = SpaceCore.Skills.GetSkillLevel(Game1.player, ModEntry.CookingSkillId);
			_cookingSlots = ModEntry.Instance.CheckForNearbyCookingStation();
			/*
			_cookingSlots = _cookingSlots == ModEntry.GusCookingStationDummyLevel
				? Math.Max(ModEntry.Instance.SaveData.WorldGusCookingRangeLevel,
					ModEntry.Instance.SaveData.ClientCookingEquipmentLevel)
				: Math.Min(_cookingSlots, cookingLevel / 2);
			*/
			// TODO: DEBUG: for cooking menu, remove limiter for _cookingSlots = 3;
			_cookingSlots = 3; // nice
			_cookingSlotsDropIn = new List<Item>(_cookingSlots);
			CookingSlots.Clear();
			for (var i = 0; i < 5; ++i)
			{
				_cookingSlotsDropIn.Add(null);
				CookingSlots.Add(new ClickableTextureComponent(
					"cookingSlot" + i,
					new Rectangle(-1, -1, CookingSlotOpenSource.Width * Scale, CookingSlotOpenSource.Height * Scale),
					null, null, Texture, _cookingSlots <= i ? CookingSlotLockedSource : CookingSlotOpenSource, Scale));
			}

			// Misc bits
			_mouseHeldDelay = 500;
			_mouseHeldLongDelay = 3000;

			// Clickables and elements
			NavDownButton = new ClickableTextureComponent(
				"navDown", new Rectangle(-1, -1, DownButtonSource.Width, DownButtonSource.Height),
				null, null, Game1.mouseCursors, DownButtonSource, 1f, true);
			NavUpButton = new ClickableTextureComponent(
				"navUp", new Rectangle(-1, -1, UpButtonSource.Width, UpButtonSource.Height),
				null, null, Game1.mouseCursors, UpButtonSource, 1f, true);
			NavRightButton = new ClickableTextureComponent(
				"navRight", new Rectangle(-1, -1, RightButtonSource.Width, RightButtonSource.Height),
				null, null, Game1.mouseCursors, RightButtonSource, 1f, true);
			NavLeftButton = new ClickableTextureComponent(
				"navLeft", new Rectangle(-1, -1, LeftButtonSource.Width, LeftButtonSource.Height),
				null, null, Game1.mouseCursors, LeftButtonSource, 1f, true);
			CookQuantityUpButton = new ClickableTextureComponent(
				"quantityUp", new Rectangle(-1, -1, PlusButtonSource.Width * 4, PlusButtonSource.Height * 4),
				null, null, Game1.mouseCursors, PlusButtonSource, 4f, true);
			CookQuantityDownButton = new ClickableTextureComponent(
				"quantityDown", new Rectangle(-1, -1, MinusButtonSource.Width * 4, MinusButtonSource.Height * 4),
				null, null, Game1.mouseCursors, MinusButtonSource, 4f, true);
			CookConfirmButton = new ClickableTextureComponent(
				"confirm", new Rectangle(-1, -1, OkButtonSource.Width, OkButtonSource.Height),
				null, null, Game1.mouseCursors, OkButtonSource, 1f, true);
			CookCancelButton = new ClickableTextureComponent(
				"cancel", new Rectangle(-1, -1, NoButtonSource.Width, NoButtonSource.Height),
				null, null, Game1.mouseCursors, NoButtonSource, 1f, true);
			ToggleOrderButton = new ClickableTextureComponent(
				"toggleOrder", new Rectangle(-1, -1, ToggleOrderButtonSource.Width * 4, ToggleOrderButtonSource.Height * 4),
				null, i18n.Get("menu.cooking_search.order_label"),
				Texture, ToggleOrderButtonSource, 4f, true);
			ToggleFilterButton = new ClickableTextureComponent(
				"toggleFilter", new Rectangle(-1, -1, ToggleFilterButtonSource.Width * 4, ToggleFilterButtonSource.Height * 4),
				null, i18n.Get("menu.cooking_search.filter_label"),
				Texture, ToggleFilterButtonSource, 4f, true);
			ToggleViewButton = new ClickableTextureComponent(
				"toggleView", new Rectangle(-1, -1, ToggleViewButtonSource.Width * 4, ToggleViewButtonSource.Height * 4),
				null, i18n.Get("menu.cooking_search.view."
				               + (ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch ? "grid" : "list")),
				Texture, ToggleViewButtonSource, 4f, true);
			SearchButton = new ClickableTextureComponent(
				"search", new Rectangle(-1, -1, SearchButtonSource.Width * 4, SearchButtonSource.Height * 4),
				null, i18n.Get("menu.cooking_recipe.search_label"),
				Texture, SearchButtonSource, 4f, true);
			FavouriteButton = new ClickableTextureComponent(
				"favourite", new Rectangle(-1, -1, FavouriteButtonSource.Width * 4, FavouriteButtonSource.Height * 4),
				null, i18n.Get("menu.cooking_recipe.favourite_label"),
				Texture, FavouriteButtonSource, 4f, true);
			SearchTabButton = new ClickableTextureComponent(
				"searchTab", new Rectangle(-1, -1, SearchTabButtonSource.Width * 4, SearchTabButtonSource.Height * 4),
				null, null, Texture, SearchTabButtonSource, 4f, true);
			for (var i = (int) Filter.Alphabetical; i < (int) Filter.Count; ++i)
			{
				FilterButtons.Add(new ClickableTextureComponent(
					$"filter{i}", new Rectangle(-1, -1, FilterIconSource.Width * 4, FilterIconSource.Height * 4),
					null, i18n.Get($"menu.cooking_search.filter.{i}"),
					Texture, new Rectangle(
						FilterIconSource.X + (i - 1) * FilterIconSource.Width, FilterIconSource.Y,
						FilterIconSource.Width, FilterIconSource.Height),
					4f));
			}

			SearchBarTextBoxMinWidth = 132;
			SearchBarTextBox = new TextBox(
				Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
				null, Game1.smallFont, Game1.textColor)
			{
				textLimit = 32,
				Selected = false,
				Text = i18n.Get("menu.cooking_recipe.search_label"),
			};
			QuantityTextBox = new TextBox(
				Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
				null, Game1.smallFont, Game1.textColor)
			{
				numbersOnly = true,
				textLimit = 3,
				Selected = false,
				Text = _textBoxDefaultText,
			};

			QuantityTextBox.OnEnterPressed += ValidateNumericalTextBox;
			SearchBarTextBox.OnEnterPressed += sender => { CloseTextBox(sender); };

			CookTextSourceWidths = new Dictionary<string, int>
			{
				{"en", 32},
				{"fr", 45},
				{"es", 42},
				{"pt", 48},
				{"jp", 50},
				{"zh", 36},
			};

			// 'Cook!' button localisations
			var xOffset = 0;
			var yOffset = 0;
			CookTextSource.Clear();
			foreach (var pair in CookTextSourceWidths)
			{
				if (xOffset + pair.Value > Texture.Width)
				{
					xOffset = 0;
					yOffset += CookTextSourceHeight;
				}
				CookTextSource.Add(pair.Key, new Rectangle(
					CookTextSourceOrigin.X + xOffset, CookTextSourceOrigin.Y + yOffset,
					pair.Value, CookTextSourceHeight));
				xOffset += pair.Value;
			}

			// Setup menu elements layout
			RealignElements();
			
			if (addDummyState)
				_stack.Push(State.Opening);
			OpenSearchPage();

			// Open to a starting recipe if needed
			if (!string.IsNullOrEmpty(initialRecipe))
			{
				ChangeCurrentRecipe(initialRecipe);
				OpenRecipePage();
			}
		}

		private void RealignElements()
		{
			var view = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
			var centre = Utility.PointToVector2(view.Center);

			// Menu
			xPositionOnScreen = (int)(centre.X - CookbookSource.Center.X * Scale);
			yPositionOnScreen = (int)(centre.Y - CookbookSource.Center.Y * Scale + 216);

			// Cookbook menu
			_cookbookLeftRect.X = xPositionOnScreen;
			_cookbookRightRect.X = _cookbookLeftRect.X + _cookbookLeftRect.Width;
			_cookbookLeftRect.Y = _cookbookRightRect.Y = yPositionOnScreen;

			_leftContent = new Point(_cookbookLeftRect.X + MarginLeft, _cookbookLeftRect.Y);
			_rightContent = new Point(_cookbookRightRect.X + MarginRight, _cookbookRightRect.Y);

			_lineWidth = _cookbookLeftRect.Width - MarginLeft * 3 / 2; // Actually mostly even for both Left and Right pages
			_textWidth = _lineWidth + TextMuffinTopOverDivider * 2;

			// Extra clickables
			upperRightCloseButton.bounds.X = xPositionOnScreen + CookbookSource.Width * Scale - 12;
			upperRightCloseButton.bounds.Y = yPositionOnScreen + 32;
			if (trashCan != null)
			{
				trashCan.bounds.X = xPositionOnScreen + CookbookSource.Width * Scale + 12;
				trashCan.bounds.Y = yPositionOnScreen + CookbookSource.Height * Scale - trashCan.bounds.Height - 96;
			}

			// Search elements
			SearchTabButton.bounds.X = _cookbookLeftRect.X - 20;
			SearchTabButton.bounds.Y = _cookbookLeftRect.Y + 72;

			var yOffset = 32;
			var xOffset = 40;
			var xOffsetExtra = 8;

			SearchBarTextBox.X = _leftContent.X;
			SearchBarTextBox.Y = _leftContent.Y + yOffset + 10;
			SearchBarTextBox.Width = 160;
			SearchBarTextBox.Selected = false;
			SearchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
			SearchBarTextBox.Update();
			SearchBarTextBoxBounds = new Rectangle(
				SearchBarTextBox.X, SearchBarTextBox.Y, SearchBarTextBox.Width, SearchBarTextBox.Height);

			ToggleViewButton.bounds.X = _cookbookRightRect.X
			                            - ToggleViewButton.bounds.Width
			                            - ToggleFilterButton.bounds.Width
			                            - ToggleOrderButton.bounds.Width
			                            - xOffsetExtra * 3 - 24;
			ToggleFilterButton.bounds.X = ToggleViewButton.bounds.X + ToggleViewButton.bounds.Width + xOffsetExtra;
			ToggleOrderButton.bounds.X = ToggleFilterButton.bounds.X + ToggleFilterButton.bounds.Width + xOffsetExtra;
			ToggleOrderButton.bounds.Y = ToggleFilterButton.bounds.Y = ToggleViewButton.bounds.Y = _leftContent.Y + yOffset;
			
			ToggleViewButton.sourceRect.X = ToggleViewButtonSource.X + (ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch
				? ToggleViewButtonSource.Width : 0);

			SearchButton.bounds = ToggleOrderButton.bounds;
			SearchBarTextBoxMaxWidth = SearchButton.bounds.X - SearchBarTextBox.X - 24;

			NavUpButton.bounds.X = NavDownButton.bounds.X = SearchButton.bounds.X;
			NavUpButton.bounds.Y = SearchButton.bounds.Y + SearchButton.bounds.Height + 16;
			NavDownButton.bounds.Y = _cookbookLeftRect.Bottom - 128;
			
			SearchResultsArea = new Rectangle(
				SearchBarTextBox.X,
				NavUpButton.bounds.Y - 8,
				NavUpButton.bounds.X - SearchBarTextBox.X - 16,
				NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16);

			yOffset = 24;
			for (var i = 0; i < FilterButtons.Count; ++i)
			{
				FilterButtons[i].bounds.X = _cookbookRightRect.X - xOffset - 4 - (FilterButtons.Count - i)
					* FilterButtons[i].bounds.Width;
				FilterButtons[i].bounds.Y = ToggleFilterButton.bounds.Y + ToggleFilterButton.bounds.Height + yOffset;
			}

			FilterContainerMiddleWidth = FilterButtons.Count * FilterIconSource.Width;
			FilterContainerBounds = new Rectangle(
				FilterButtons[0].bounds.X - FilterContainerSideWidth * Scale - 4,
				FilterButtons[0].bounds.Y - (FilterContainerSource.Height - FilterIconSource.Height) * Scale / 2,
				(FilterContainerSideWidth * 2 + FilterContainerMiddleWidth) * Scale,
				FilterContainerSource.Height * Scale);
			
			// Recipe nav buttons
			NavLeftButton.bounds.X = _leftContent.X - 24;
			NavRightButton.bounds.X = NavLeftButton.bounds.X + _lineWidth - 12;
			NavRightButton.bounds.Y = NavLeftButton.bounds.Y = _leftContent.Y + 20;

			// Ingredients item slots
			const int slotsPerRow = 3;
			var w = CookingSlots[0].bounds.Width;
			var h = CookingSlots[0].bounds.Height;
			yOffset = 36;
			xOffset = 0;
			xOffsetExtra = 0;
			var extraSpace = (int)(w / 2f * (CookingSlots.Count % slotsPerRow) / 2f);
			for (var i = 0; i < CookingSlots.Count; ++i)
			{
				xOffset += w;
				if (i % slotsPerRow == 0)
				{
					if (i != 0)
						yOffset += h;
					xOffset = 0;
				}

				if (i == CookingSlots.Count - (CookingSlots.Count % slotsPerRow))
					xOffsetExtra = extraSpace;

				CookingSlots[i].bounds.X = _rightContent.X + xOffset + xOffsetExtra;
				CookingSlots[i].bounds.Y = _rightContent.Y + yOffset;
			}

			// Favourite button
			FavouriteButton.bounds.X = 0;
			FavouriteButton.bounds.Y = 0;

			// Cook! button
			xOffset = _rightContent.X + _cookbookRightRect.Width / 2 - MarginRight;
			yOffset = _rightContent.Y + 344;
			CookTextMiddleWidth = Math.Max(32, CookTextSource[_locale].Width);
			CookButtonBounds = new Rectangle(
				xOffset, yOffset,
				CookTextSideWidth * Scale * 2 + CookTextMiddleWidth * Scale,
				CookButtonSource.Height * Scale);
			CookButtonBounds.X -= (CookTextSourceWidths[_locale] / 2 * Scale - CookTextSideWidth * Scale) + MarginLeft;

			// Cooking confirmation popup buttons
			xOffset -= 160;
			yOffset -= 36;
			CookIconBounds = new Rectangle(xOffset, yOffset + 6, 90, 90);

			xOffset += 48 + CookIconBounds.Width;
			CookQuantityUpButton.bounds.X = CookQuantityDownButton.bounds.X = xOffset;
			CookQuantityUpButton.bounds.Y = yOffset - 12;

			var textSize = QuantityTextBox.Font.MeasureString(
				Game1.parseText("999", QuantityTextBox.Font, 96));
			QuantityTextBox.Text = _textBoxDefaultText;
			QuantityTextBox.limitWidth = false;
			QuantityTextBox.Width = (int)textSize.X + 24;

			extraSpace = (QuantityTextBox.Width - CookQuantityUpButton.bounds.Width) / 2;
			QuantityTextBox.X = CookQuantityUpButton.bounds.X - extraSpace;
			QuantityTextBox.Y = CookQuantityUpButton.bounds.Y + CookQuantityUpButton.bounds.Height + 7;
			QuantityTextBox.Update();
			QuantityTextBoxBounds = new Rectangle(QuantityTextBox.X, QuantityTextBox.Y, QuantityTextBox.Width,
					QuantityTextBox.Height);

			CookQuantityDownButton.bounds.Y = QuantityTextBox.Y + QuantityTextBox.Height + 5;

			CookConfirmButton.bounds.X = CookCancelButton.bounds.X
				= CookQuantityUpButton.bounds.X + CookQuantityUpButton.bounds.Width + extraSpace + 16;
			CookConfirmButton.bounds.Y = yOffset - 16;
			CookCancelButton.bounds.Y = CookConfirmButton.bounds.Y + CookConfirmButton.bounds.Height + 4;

			// Inventory
			inventory.xPositionOnScreen = xPositionOnScreen + CookbookSource.Width / 2 * Scale - inventory.width / 2;
			inventory.yPositionOnScreen = yPositionOnScreen + CookbookSource.Height * Scale + 8 - 20;

			// Inventory items
			yOffset = -4;
			var rowSize = inventory.capacity / inventory.rows;
			for (var i = 0; i < inventory.capacity; ++i)
			{
				if (i % rowSize == 0 && i != 0)
					yOffset += inventory.inventory[i].bounds.Height + 4;
				inventory.inventory[i].bounds.X = inventory.xPositionOnScreen + i % rowSize * inventory.inventory[i].bounds.Width;
				inventory.inventory[i].bounds.Y = inventory.yPositionOnScreen + yOffset;
			}
		}

		/// <summary>
		/// Checks whether an item can be used in cooking recipes.
		/// Doesn't check for edibility; oil, vinegar, jam, honey, etc are inedible.
		/// </summary>
		public bool CanBeCooked(Item item)
		{
			return !(item == null || item is Tool || item is Furniture || item is Object o
				&& (o.bigCraftable.Value || o.specialItem || o.isLostItem || !o.canBeTrashed()));
		}
		
		/// <summary>
		/// Find an item in a list that works as an ingredient or substitute in a cooking recipe for some given requirement.
		/// </summary>
		/// <param name="target">The required item's identifier for the recipe, given as an index or category.</param>
		/// <param name="items">List of items to seek a match in.</param>
		/// <param name="item">Returns matching item for the identifier, null if not found.</param>
		public void GetMatchingIngredient(int target, List<Item> items, out Item item)
		{
			item = null;
			for (var j = 0; j < items.Count && item == null; ++j)
				if (CanBeCooked(items[j])
				    && (items[j].ParentSheetIndex == target
				        || items[j].Category == target
				        || CraftingRecipe.isThereSpecialIngredientRule((Object) items[j], target)))
					item = items[j];
		}

		/// <summary>
		/// Identify items to consume from the ingredients dropIn.
		/// Abort before consuming if required items are not found.
		/// </summary>
		/// <param name="requiredItems">Map of each target item and their quantities.</param>
		/// <param name="items">List to mark items to be consumed from.</param>
		/// <returns>Items to be crafted and their quantities to be consumed, null if not all required items were found.</returns>
		public Dictionary<int, int> ChooseItemsForCrafting(Dictionary<int, int> requiredItems, List<Item> items)
		{
			var currentItems = items.TakeWhile(_ => true).ToList();
			var craftingItems = new Dictionary<int, int>();
			foreach (var requiredItem in requiredItems)
			{
				var identifier = requiredItem.Key;
				var requiredCount = requiredItem.Value;
				var craftingCount = 0;
				while (craftingCount < requiredCount)
				{
					GetMatchingIngredient(identifier, currentItems, out var item);
					if (item == null)
						return null;
					var consumed = Math.Min(requiredCount, item.Stack);
					craftingCount += consumed;
					var index = items.FindIndex(i =>
						i != null && i.ParentSheetIndex == item.ParentSheetIndex && i.Stack == item.Stack);
					craftingItems.Add(index, consumed);
					currentItems.Remove(item);
				}
			}
			return craftingItems;
		}

		/// <summary>
		/// Determines the number of times the player can craft a cooking recipe by consuming required items.
		/// Returns 0 if any ingredients are missing entirely.
		/// </summary>
		public int GetAmountCraftable(Dictionary<int, int> requiredItems, List<Item> items)
		{
			var craftableCount = -1;
			foreach (var identifier in requiredItems.Keys)
			{
				var localAvailable = 0;
				var requiredCount = requiredItems[identifier];
				GetMatchingIngredient(identifier, items, out var item);
				if (item == null)
					return 0;
				localAvailable += item.Stack;
				var localCount = localAvailable / requiredCount;
				if (localCount < craftableCount || craftableCount == -1)
					craftableCount = localCount;
			}
			return craftableCount;
		}

		public Item CraftItemAndConsumeIngredients(Dictionary<int, int> requiredItems, ref List<Item> items)
		{
			var itemsToConsume = ChooseItemsForCrafting(requiredItems, items);
			
			// Consume items
			foreach (var pair in itemsToConsume)
			{
				if ((items[pair.Key].Stack -= pair.Value) < 1)
					items[pair.Key] = null;
			}

			// Add result item to player's inventory
			var result = _recipeItem.getOne() as Object;
			
			// Consume Oil to improve the recipe
			if (items.Count > 0)
			{
				var i = items.FindIndex(i => i != null && i.Name.EndsWith("Oil"));
				if (i > 0)
				{
					var hasOilPerk =
						ModEntry.Instance.Config.CookingSkill && Game1.player.HasCustomProfession(
							ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.ImprovedOil]);
					switch (items[i].ParentSheetIndex)
					{
						case 247: // Oil
							result.Quality = hasOilPerk ? 2 : 1;
							break;
						case 432: // Truffle Oil
							result.Quality = hasOilPerk ? 4 : 2;
							break;
						default: // Oils not yet discovered by science
							result.Quality = hasOilPerk ? 4 : 2;
							break;
					}

					if (--items[i].Stack < 1)
						items[i] = null;
				}
			}

			if (ModEntry.Instance.Config.CookingSkill) {
				if (Game1.player.HasCustomProfession(
					ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.SaleValue]))
					result.Price += result.Price / CookingSkill.SaleValue;
				if (Game1.random.NextDouble() * 10 < CookingSkill.ExtraPortionChance && Game1.player.HasCustomProfession(
					ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.ExtraPortion]))
					++result.Stack;
			}
			
			return result;
		}

		private bool CookRecipe(Dictionary<int, int> requiredItems, ref List<Item> items, int quantity)
		{
			var craftableCount = Math.Min(quantity, GetAmountCraftable(requiredItems, items));
			Item result = null;
			for (var i = 0; i < craftableCount; ++i)
				result = CraftItemAndConsumeIngredients(requiredItems, ref items);
			result.Stack = craftableCount;
			Game1.player.addItemByMenuIfNecessary(result);
			return true;
		}

		private bool ShouldDrawCookButton()
		{
			//return _cookingSlotsDropIn.Any(item => item != null) && !_showCookingConfirmPopup;
			return _filteredRecipeList.Count > _currentRecipe
				   && _recipeItem != null
			       && GetAmountCraftable(_recipeIngredients, _cookingSlotsDropIn) > 0
			       && !_showCookingConfirmPopup;
		}

		private void OpenRecipePage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Recipe)
				_stack.Pop();
			_stack.Push(State.Recipe);

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X;
		}

		private void CloseRecipePage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Recipe)
				_stack.Pop();

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X + SearchTabButtonSource.Width;
		}

		private void OpenSearchPage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Search)
				_stack.Pop();
			_stack.Push(State.Search);

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X + SearchTabButtonSource.Width;
			_filteredRecipeList = FilterRecipes();
			_showSearchFilters = false;
			SearchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
		}

		private void CloseTextBox(TextBox textBox)
		{
			textBox.Selected = false;
			Game1.keyboardDispatcher.Subscriber = null;
		}

		private List<CraftingRecipe> ReverseRecipeList(List<CraftingRecipe> recipes)
		{
			recipes.Reverse();
			_currentRecipe = 0;
			//_currentRecipe = recipes.Count - 1 - _currentRecipe;
			_lastFilterReversed = true;
			return recipes;
		}

		private List<CraftingRecipe> FilterRecipes(Filter which = Filter.Alphabetical,
			string substr = null)
		{
			Func<CraftingRecipe, object> order = null;
			Func<CraftingRecipe, bool> filter = null;
			Log.D($"Which filter: {which.ToString()}");
			switch (which)
			{
				case Filter.Healing:
					order = recipe => recipe.createItem().staminaRecoveredOnConsumption();
					break;
				case Filter.Value:
					order = recipe => recipe.createItem().salePrice();
					break;
				case Filter.Quest:
					filter = recipe => Game1.player.questLog.Any(
						quest => quest.questDescription.Contains(recipe.DisplayName));
					break;
				case Filter.New:
					filter = recipe =>
						!Game1.player.recipesCooked.ContainsKey(
							recipe.createItem().ParentSheetIndex);
					break;
				case Filter.Unknown:
					filter = recipe => !Game1.player.knowsRecipe(recipe.name);
					break;
				case Filter.Favourite:
					filter = recipe =>
						ModEntry.Instance.SaveData.FavouriteRecipes.Contains(recipe.name);
					break;
				default:
					Log.D("Using default filter");
					order = recipe => recipe.DisplayName;
					break;
			}

			_currentRecipe = 0;

			var recipes = (order != null
				? _unlockedCookingRecipes.OrderBy(order)
				: _unlockedCookingRecipes.Where(filter)).ToList();
			if (!string.IsNullOrEmpty(substr) && substr != i18n.Get("menu.cooking_recipe.search_label"))
			{
				substr = substr.ToLower();
				recipes = recipes.Where(recipe => recipe.DisplayName.ToLower().Contains(substr)).ToList();
			}

			if (recipes.Count < 1)
				recipes.Add(new CraftingRecipe("none", true));
			if (_lastFilterUsed == which && !_lastFilterReversed)
				recipes = ReverseRecipeList(recipes);
			else
				_lastFilterReversed = false;
			_lastFilterUsed = which;

			return recipes;
		}

		// TODO: POLISH: find a very suitable position for UpdateSearchRecipes() call, rather than in draw()
		private void UpdateSearchRecipes()
		{
			int minRecipe, maxRecipe;

			_searchRecipes.Clear();
			SearchResultsArea.Y = NavUpButton.bounds.Y - 8;
			SearchResultsArea.Height = NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16;

			var isGridView = ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch;
			_recipeHeight = isGridView
				? 64 + 8
				: 64;
			_recipesPerPage = isGridView
				? (SearchResultsArea.Width / _recipeHeight) * (SearchResultsArea.Height / _recipeHeight)
				: SearchResultsArea.Height / _recipeHeight;
			minRecipe = Math.Max(0, _currentRecipe - _recipesPerPage / 2);
			maxRecipe = Math.Min(_filteredRecipeList.Count, minRecipe + _recipesPerPage);

			for (var i = minRecipe; i < maxRecipe; ++i)
				_searchRecipes.Add(_filteredRecipeList[i]);
		}

		private void ToggleCookingConfirmPopup(bool playSound)
		{
			_showCookingConfirmPopup = !_showCookingConfirmPopup;
			QuantityTextBox.Text = _textBoxDefaultText;
			if (playSound)
				Game1.playSound(_showCookingConfirmPopup ? "bigSelect" : "bigDeSelect");
		}

		private void ValidateNumericalTextBox(TextBox sender)
		{
			sender.Text = Math.Max(1, Math.Min(
				int.Parse(sender.Text), GetAmountCraftable(_recipeIngredients, _cookingSlotsDropIn))).ToString();
			for (var i = 3 - sender.Text.Length; i >= 0; --i)
				sender.Text = $" {sender.Text}";
			sender.Selected = false;
		}

		private void ChangeCurrentRecipe(int index)
		{
			index = Math.Max(0, Math.Min(_filteredRecipeList.Count - 1, index));
			ChangeCurrentRecipe(_filteredRecipeList[index].name);
		}

		private void ChangeCurrentRecipe(string name)
		{
			_currentRecipe = _filteredRecipeList.FindIndex(recipe => recipe.name == name);
			_recipeItem = new Object(Game1.objectInformation.FirstOrDefault(
				pair => pair.Value.Split('/')[0] == name).Key, 1);
			_recipeIngredients = ModEntry.Instance.Helper.Reflection.GetField<Dictionary<int, int>>(
				_filteredRecipeList[_currentRecipe], "recipeList").GetValue();
			_recipeDescription = ModEntry.Instance.Helper.Reflection.GetField<string>(
				_filteredRecipeList[_currentRecipe], "description").GetValue();
			var info = Game1.objectInformation[_recipeItem.ParentSheetIndex]
				.Split('/');
			_recipeBuffs = info.Length < 7
				? null
				: info[7].Split(' ').ToList().ConvertAll(int.Parse);
		}

		private void ReturnIngredientsToInventory()
		{
			if (_cookingSlotsDropIn.All(item => item == null))
				return;

			Log.W($"Trying to add {_cookingSlotsDropIn.Count(item => item != null)} ingredients from dropin slots");
			foreach (var item in _cookingSlotsDropIn)
				inventory.tryToAddItem(item);
			_cookingSlotsDropIn = new List<Item> { null, null, null, null, null };
		}

		private void TryClickNavButton(int x, int y, bool playSound)
		{
			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();
			var isGridView = ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch;
			var max = _filteredRecipeList.Count - 1;
			var delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] {new InputButton(Keys.LeftShift)})
				? _recipesPerPage
				: isGridView && state == State.Search ? SearchResultsArea.Width / _recipeHeight : 1;
			switch (state)
			{
				case State.Search:
					// Search up/down nav buttons
					if (NavUpButton.containsPoint(x, y) && _currentRecipe > 0)
						_currentRecipe = Math.Max(0, _currentRecipe - delta);
					else if (NavDownButton.containsPoint(x, y) && _currentRecipe < max)
						_currentRecipe = Math.Min(max, _currentRecipe + delta);
					else
						return;
					break;

				case State.Recipe:
					// Recipe next/prev nav buttons
					if (NavLeftButton.containsPoint(x, y) && _currentRecipe > 0)
						ChangeCurrentRecipe(_currentRecipe - delta);
					else if (NavRightButton.containsPoint(x, y) && _currentRecipe < max)
						ChangeCurrentRecipe(_currentRecipe + delta);
					else
						return;
					break;

				default:
					return;
			}
			Log.D($"Current recipe: ({_currentRecipe}/{max}) {_filteredRecipeList[_currentRecipe].name}");
			if (playSound)
				Game1.playSound(state == State.Search ? "coin" : "newRecipe");
		}

		private void TryClickQuantityButton(int x, int y)
		{
			var value = int.Parse(QuantityTextBox.Text);
			var delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] {new InputButton(Keys.LeftShift)})
				? 10 : 1;

			if (CookQuantityUpButton.containsPoint(x, y))
				value += delta;
			else if (CookQuantityDownButton.containsPoint(x, y))
				value -= delta;
			else
				return;

			QuantityTextBox.Text = value.ToString();
			ValidateNumericalTextBox(QuantityTextBox);
			Game1.playSound(int.Parse(QuantityTextBox.Text) == value ? "coin" : "cancel");
			QuantityTextBox.Update();
		}

		public bool TryClickItem(int x, int y, bool moveEntireStack)
		{
			const string sound = "coin";
			var clickedAnItem = true;
			var inventoryItem = inventory.getItemAt(x, y);
			var inventoryIndex = inventory.getInventoryPositionOfClick(x, y);

			if (inventoryItem != null && !CanBeCooked(inventoryItem))
			{
				inventory.ShakeItem(inventoryItem);
				Game1.playSound("cancel");
				return false;
			}

			var dropInIsFull = _cookingSlotsDropIn.GetRange(0, _cookingSlots).TrueForAll(i => i != null);

			// Add an inventory item to the ingredients dropIn slots in the best available position
			for (var i = 0; i < _cookingSlots && inventoryItem != null && clickedAnItem; ++i)
			{
				if (_cookingSlotsDropIn[i] == null || !_cookingSlotsDropIn[i].canStackWith(inventoryItem))
					continue;

				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, i, moveEntireStack, false, sound) == 0;
			}
			// Try add inventory item to a new slot if it couldn't be stacked with any elements in dropIn ingredients slots
			if (inventoryItem != null && clickedAnItem)
			{
				// Ignore dropIn actions from inventory when ingredients slots are full
				var index = _cookingSlotsDropIn.FindIndex(i => i == null);
				if (dropInIsFull || index < 0)
				{
					//Game1.showRedMessage(i18n.Get("menu.cooking_recipe.locked"));
					inventory.ShakeItem(inventoryItem);
					Game1.playSound("cancel");
					return false;
				}
				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, index, moveEntireStack, false, sound) == 0;
			}

			// Return a dropIn ingredient item to the inventory
			for (var i = 0; i < _cookingSlotsDropIn.Count && clickedAnItem; ++i)
			{
				if (!CookingSlots[i].containsPoint(x, y))
					continue;
				if (i >= _cookingSlots)
				{
					//Game1.showRedMessage(i18n.Get("menu.cooking_recipe.locked"));
					return false;
				}
				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, i, moveEntireStack, true, sound) == 0;
			}

			return clickedAnItem;
		}
		
		private int TryGetIndexForSearchResult(int x, int y)
		{
			var index = -1;
			if (!SearchResultsArea.Contains(x, y) || _recipeHeight == 0)
				return index;
			var yIndex = (y - SearchResultsArea.Y - (SearchResultsArea.Height % _recipeHeight) / 2) / _recipeHeight;
			var xIndex = (x - SearchResultsArea.X) / _recipeHeight;
			if (ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch)
				index = yIndex * (SearchResultsArea.Width / _recipeHeight) + xIndex;
			else
				index = yIndex;
			return index;
		}

		/// <summary>
		/// Move quantities of stacks of two items, one in the inventory, and one in the ingredients dropIn.
		/// </summary>
		/// <param name="inventoryIndex">Index of item slot in the inventory to draw from.</param>
		/// <param name="ingredientsIndex">Index of item slot in the ingredients dropIn to add to.</param>
		/// <param name="moveEntireStack">If true, the quantity moved will be as large as possible.</param>
		/// <param name="reverse">If true, stack size from the ingredients dropIn is reduced, and added to the inventory.</param>
		/// <param name="sound">Name of sound effect to play when items are moved.</param>
		/// <returns>Quantity moved from one item stack to another. May return a negative number, affected by reverse.</returns>
		private int AddToIngredientsDropIn(int inventoryIndex, int ingredientsIndex,
			bool moveEntireStack, bool reverse, string sound = null)
		{
			// Add items to fill in empty slots at our indexes
			if (_cookingSlotsDropIn[ingredientsIndex] == null)
			{
				if (inventoryIndex == -1)
					return 0;

				_cookingSlotsDropIn[ingredientsIndex] = inventory.actualInventory[inventoryIndex].getOne();
				_cookingSlotsDropIn[ingredientsIndex].Stack = 0;
			}
			if (inventoryIndex == -1)
			{
				var dropOut = _cookingSlotsDropIn[ingredientsIndex].getOne();
				dropOut.Stack = 0;
				var item = inventory.actualInventory.FirstOrDefault(i => dropOut.canStackWith(i));
				inventoryIndex = inventory.actualInventory.IndexOf(item);
				if (item == null)
					inventory.actualInventory[inventoryIndex] = dropOut;
			}

			var addTo = !reverse
				? _cookingSlotsDropIn[ingredientsIndex]
				: inventory.actualInventory[inventoryIndex];
			var takeFrom = !reverse
				? inventory.actualInventory[inventoryIndex]
				: _cookingSlotsDropIn[ingredientsIndex];

			// Contextual goal quantity mimics the usual vanilla inventory dropIn interactions
			// (left-click moves entire stack, right-click moves one from stack, shift-right-click moves half the stack)
			var quantity = 0;
			if (addTo != null && takeFrom != null)
			{
				var max = addTo.maximumStackSize();
				quantity = moveEntireStack
					? takeFrom.Stack
					: Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) })
						? (int)Math.Ceiling(takeFrom.Stack / 2.0)
						: 1;
				// Actual quantity is limited by the dest stack limit and source stack quantity
				quantity = Math.Min(quantity, max - addTo.Stack);
			}
			// If quantity is 0, we've probably reached these limits
			if (quantity == 0)
			{
				inventory.ShakeItem(inventory.actualInventory[inventoryIndex]);
				Game1.playSound("cancel");
			}
			// Add/subtract quantities from each stack, and remove items with empty stacks
			else
			{
				if (reverse)
					quantity *= -1;

				if ((_cookingSlotsDropIn[ingredientsIndex].Stack += quantity) < 1)
					_cookingSlotsDropIn[ingredientsIndex] = null;
				if ((inventory.actualInventory[inventoryIndex].Stack -= quantity) < 1)
					inventory.actualInventory[inventoryIndex] = null;

				if (!string.IsNullOrEmpty(sound))
					Game1.playSound(sound);
			}

			return quantity;
		}

		internal bool PopMenuStack(bool playSound, bool tryToQuit = false)
		{
			if (_stack.Count < 1)
				return false;

			if (_showCookingConfirmPopup)
			{
				ToggleCookingConfirmPopup(true);
				if (!tryToQuit)
					return false;
			}

			ReturnIngredientsToInventory();

			var state = _stack.Peek();
			if (state == State.Search)
				_stack.Pop();
			else if (state == State.Recipe)
				CloseRecipePage();

			while (tryToQuit && _stack.Count > 0)
				_stack.Pop();

			if (playSound)
				Game1.playSound("bigDeSelect");

			if (!readyToClose() || _stack.Count > 0)
				return false;
			Game1.exitActiveMenu();
			cleanupBeforeExit();
			return true;
		}

		protected override void cleanupBeforeExit()
		{
			ReturnIngredientsToInventory();

			Game1.displayHUD = true;
			base.cleanupBeforeExit();
		}

		public override void snapToDefaultClickableComponent()
		{
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}

		public override void performHoverAction(int x, int y)
		{
			if (_stack.Count < 1)
				return;

			hoverText = null;
			hoveredItem = null;
			var obj = inventory.hover(x, y, heldItem);
			if (obj != null)
				hoveredItem = obj;
			for (var i = 0; i < _cookingSlotsDropIn.Count && hoveredItem == null; ++i)
				if (CookingSlots[i].containsPoint(x, y))
					hoveredItem = _cookingSlotsDropIn[i];
			
			upperRightCloseButton.tryHover(x, y, 0.5f);

			if (trashCan != null)
			{
				if (trashCan.containsPoint(x, y))
				{
					if (trashCanLidRotation <= 0.0)
						Game1.playSound("trashcanlid");
					trashCanLidRotation = Math.Min(trashCanLidRotation + (float)Math.PI / 48f, 1.570796f);
					if (heldItem == null || Utility.getTrashReclamationPrice(heldItem, Game1.player) <= 0)
						return;
					hoverText = Game1.content.LoadString("Strings\\UI:TrashCanSale");
					hoverAmount = Utility.getTrashReclamationPrice(heldItem, Game1.player);
				}
				else
				{
					trashCanLidRotation = Math.Max(trashCanLidRotation - (float)Math.PI / 48f, 0.0f);
				}
			}

			var state = _stack.Peek();
			switch (state)
			{
				case State.Opening:
					break;

				case State.Recipe:
					NavRightButton.tryHover(x, y);
					NavLeftButton.tryHover(x, y);
					break;

				case State.Search:
					NavDownButton.tryHover(x, y);
					NavUpButton.tryHover(x, y);

					if (SearchBarTextBox.Selected)
					{
						SearchButton.tryHover(x, y);
						if (SearchButton.containsPoint(x,y ))
							hoverText = SearchButton.hoverText;
					}
					else
					{
						foreach (var clickable in new[] { ToggleOrderButton, ToggleViewButton, ToggleFilterButton })
						{
							clickable.tryHover(x, y, 0.2f);
							if (clickable.bounds.Contains(x, y))
								hoverText = clickable.hoverText;
						}

						if (_showSearchFilters)
						{
							foreach (var clickable in FilterButtons)
							{
								clickable.tryHover(x, y, 0.4f);
								if (clickable.bounds.Contains(x, y))
									hoverText = clickable.hoverText;
							}
						}
					}

					if (!ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch)
						break;

					var index = TryGetIndexForSearchResult(x, y);
					if (index >= 0 && index < _searchRecipes.Count && _searchRecipes[index].name != "Torch")
						hoverText = Game1.player.knowsRecipe(_searchRecipes[index].name)
							? _searchRecipes[index].DisplayName
							: i18n.Get("menu.cooking_recipe.title_unknown");

					break;
			}

			SearchTabButton.tryHover(x, y, state != State.Search ? 0.5f : 0f);
			if (SearchTabButton.bounds.Contains(x, y))
				hoverText = SearchTabButton.hoverText;

			if (_showCookingConfirmPopup)
			{
				CookQuantityUpButton.tryHover(x, y, 0.5f);
				QuantityTextBox.Hover(x, y);
				CookQuantityDownButton.tryHover(x, y, 0.5f);

				CookConfirmButton.tryHover(x, y);
				CookCancelButton.tryHover(x, y);
			}
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_stack.Count < 1 || Game1.activeClickableMenu == null)
				return;
			if (upperRightCloseButton.containsPoint(x, y))
			{
				PopMenuStack(false, true);
				return;
			}

			var state = _stack.Peek();
			switch (state)
			{
				case State.Opening:
					break;

				case State.Search:
					// Search text
					if (SearchBarTextBoxBounds.Contains(x, y))
					{
						SearchBarTextBox.Text = "";
						Game1.keyboardDispatcher.Subscriber = SearchBarTextBox;
						SearchBarTextBox.SelectMe();
						_showSearchFilters = false;
					}
					else if (SearchBarTextBox.Selected)
					{
						if (SearchButton.containsPoint(x, y))
						{
							Game1.playSound("coin");
						}
						if (string.IsNullOrEmpty(SearchBarTextBox.Text))
							SearchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
						CloseTextBox(SearchBarTextBox);
					}
					else
					{
						// Filter buttons
						if (_showSearchFilters)
						{
							var clickable = FilterButtons.FirstOrDefault(c => c.containsPoint(x, y));
							if (clickable != null)
							{
								_filteredRecipeList = FilterRecipes(
									(Filter) int.Parse(clickable.name[clickable.name.Length - 1].ToString()),
									SearchBarTextBox.Text);
								Game1.playSound("coin");
							}
						}
					
						var index = TryGetIndexForSearchResult(x, y);
						if (index >= 0 && index < _searchRecipes.Count && _searchRecipes[index].name != "Torch")
						{
							Game1.playSound("shwip");
							ChangeCurrentRecipe(_searchRecipes[index].name);
							OpenRecipePage();
						}

						// Search filter toggles
						if (ToggleFilterButton.containsPoint(x, y))
						{
							_showSearchFilters = !_showSearchFilters;
							Game1.playSound("shwip");
						}
						else if (ToggleViewButton.containsPoint(x, y))
						{
							var isGridView = ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch;
							ToggleViewButton.sourceRect.X = ToggleViewButtonSource.X
							                                + (isGridView ? 0 : ToggleViewButtonSource.Width);
							ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch = !isGridView;
							Game1.playSound("shwip");
							ToggleViewButton.hoverText =
								i18n.Get($"menu.cooking_search.view.{(isGridView ? "grid" : "list")}");
						}
						else if (ToggleOrderButton.containsPoint(x, y))
						{
							_filteredRecipeList = ReverseRecipeList(_filteredRecipeList);
							Game1.playSound("shwip");
						}
					}
					break;

				case State.Recipe:
					
					break;
			}

			if (state != State.Search && SearchTabButton.containsPoint(x, y))
			{
				_stack.Pop();
				OpenSearchPage();
				Game1.playSound("bigSelect");
			}
			else if (ShouldDrawCookButton() && CookButtonBounds.Contains(x, y))
			{
				ToggleCookingConfirmPopup(true);
			}
			else if (_showCookingConfirmPopup)
			{
				TryClickQuantityButton(x, y);

				if (QuantityTextBoxBounds.Contains(x, y))
				{
					Game1.keyboardDispatcher.Subscriber = QuantityTextBox;
					QuantityTextBox.SelectMe();
				}
				else if (QuantityTextBox.Selected)
				{
					CloseTextBox(QuantityTextBox);
				}

				if (CookConfirmButton.containsPoint(x, y))
				{
					if (CookRecipe(_recipeIngredients, ref _cookingSlotsDropIn, int.Parse(QuantityTextBox.Text)))
					{
						Game1.playSound("reward");
						PopMenuStack(true);
					}
					else
					{
						Game1.playSound("cancel");
					}
				}
				else if (CookCancelButton.containsPoint(x, y))
				{
					PopMenuStack(true);
				}
			}
			
			TryClickNavButton(x, y, true);
			TryClickItem(x, y, true);

			_mouseHeldStartTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			base.receiveRightClick(x, y, playSound);

			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();
			var shouldPop = TryClickItem(x, y, false);
			
			if (_showCookingConfirmPopup && shouldPop)
			{
				ToggleCookingConfirmPopup(true);
				QuantityTextBox.Update();
				shouldPop = false;
			}
			else if (SearchBarTextBox.Selected)
			{
				SearchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
				CloseTextBox(SearchBarTextBox);
				shouldPop = false;
			}
			else if (state == State.Search
			         && !string.IsNullOrEmpty(SearchBarTextBox.Text)
			         && SearchBarTextBox.Text != i18n.Get("menu.cooking_recipe.search_label"))
			{
				SearchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
				_filteredRecipeList = FilterRecipes();
				shouldPop = false;
			}

			if (shouldPop)
				PopMenuStack(playSound);
		}

		public override void leftClickHeld(int x, int y)
		{
			base.leftClickHeld(x, y);

			// Start mouse-held behaviours after a delay, and accelerate a stage after a longer delay
			if (_mouseHeldStartTime < 0
			    || Game1.currentGameTime.TotalGameTime.TotalMilliseconds - _mouseHeldStartTime < _mouseHeldDelay
			    || Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 
			    (Game1.currentGameTime.TotalGameTime.TotalMilliseconds - _mouseHeldStartTime < _mouseHeldLongDelay ? 30 : 15) > 1)
				return;
			
			// Use mouse-held behaviours on navigation and quantity buttons
			TryClickNavButton(x, y, true);
			if (_showCookingConfirmPopup)
				TryClickQuantityButton(x, y);
		}

		public override void releaseLeftClick(int x, int y)
		{
			base.releaseLeftClick(x, y);

			_mouseHeldStartTime = -1;
		}

		public override void receiveGamePadButton(Buttons b)
		{
			Log.D($"receiveGamePadButton: {b.ToString()}");

			// TODO: SYSTEM: keep GamePadButtons inputs up-to-date with KeyPress and Click behaviours

			if (b == Buttons.RightTrigger)
				return;
			else if (b == Buttons.LeftTrigger)
				return;
			else if (b == Buttons.B)
				PopMenuStack(true);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			base.receiveScrollWheelAction(direction);

			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();
			var clickable = direction < 0
				? state == State.Search ? NavDownButton : NavRightButton
				: state == State.Search ? NavUpButton : NavLeftButton;
			TryClickNavButton(clickable.bounds.X, clickable.bounds.Y, state == State.Recipe);
		}

		public override void receiveKeyPress(Keys key)
		{
			if (_stack.Count < 1)
				return;

			base.receiveKeyPress(key);

			var state = _stack.Peek();
			switch (state)
			{
				case State.Search:
				{
					// Navigate left/right buttons
					if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
						break;
					break;
				}
			}

			if (SearchBarTextBox.Selected)
			{
				switch (key)
				{
					case Keys.Enter:
						break;
					case Keys.Escape:
						CloseTextBox(SearchBarTextBox);
						break;
					default:
						_filteredRecipeList = FilterRecipes(_lastFilterUsed, SearchBarTextBox.Text);
						break;
				}
				return;
			}

			if (key == Keys.K)
			{
				Log.D($"Toggling cooking confirm popup to {!_showCookingConfirmPopup}");
				ToggleCookingConfirmPopup(true);
			}
			else if (key == Keys.L)
			{
				var locales = CookTextSource.Keys.ToList();
				_locale = locales[(locales.IndexOf(_locale) + 1) % locales.Count];
				Log.D($"Changed to locale {_locale} and realigning elements");
				RealignElements();
			}
			else if (key == Keys.M)
			{
				var message = "asdadas";
				Log.D($"Opening new numberSelectionMenu as '{message}'");
				Game1.activeClickableMenu = new NumberSelectionMenu(
					message, null, 300, 1, 5, 2);
			}
			else if (key == Keys.N)
			{
				var next = state == State.Search ? State.Recipe : State.Search;
				Log.D($"Swapping to state {next}");
				if (state == State.Recipe)
					CloseRecipePage();
				else
					_stack.Pop();
				if (next == State.Recipe)
					OpenRecipePage();
				else
					_stack.Push(next);
			}

			if (Game1.options.doesInputListContain(Game1.options.menuButton, key)
				|| Game1.options.doesInputListContain(Game1.options.journalButton, key))
			{
				Log.D($"PopMenuStack from menu button press");
				PopMenuStack(true);
			}

			if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && canExitOnKey)
			{
				Log.D($"PopMenuStack from menu button press + EXIT");
				PopMenuStack(true);
				if (Game1.currentLocation.currentEvent != null && Game1.currentLocation.currentEvent.CurrentCommand > 0)
					Game1.currentLocation.currentEvent.CurrentCommand++;
			}
			else if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && heldItem != null && trashCan != null)
			{
				Game1.setMousePosition(trashCan.bounds.Center);
			}
			if (key == Keys.Delete && heldItem != null && heldItem.canBeTrashed())
			{
				Utility.trashItem(heldItem);
				heldItem = null;
			}
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			RealignElements();
			base.gameWindowSizeChanged(oldBounds, newBounds);
		}

		public override void update(GameTime time)
		{
			_animTimer += time.ElapsedGameTime.Milliseconds;
			if (_animTimer >= _animTimerLimit)
			{
				_animTimer = 0;
				if (false)
					_locale = CookTextSourceWidths.Keys.ToList()[
							(int)((time.TotalGameTime.TotalMilliseconds / _animTimerLimit / 3) % CookTextSourceWidths.Count)];
			}
			_animFrame = (int)((float)_animTimer / _animTimerLimit * _animFrames);

			// Expand search bar on selected, contract on deselected
			var delta = 256f / time.ElapsedGameTime.Milliseconds;
			if (SearchBarTextBox.Selected && SearchBarTextBox.Width < SearchBarTextBoxMaxWidth)
				SearchBarTextBox.Width = (int)Math.Min(SearchBarTextBoxMaxWidth, SearchBarTextBox.Width + delta);
			else if (!SearchBarTextBox.Selected && SearchBarTextBox.Width > SearchBarTextBoxMinWidth)
				SearchBarTextBox.Width = (int)Math.Max(SearchBarTextBoxMinWidth, SearchBarTextBox.Width - delta);
			SearchBarTextBoxBounds.Width = SearchBarTextBox.Width;

			base.update(time);
		}

		public override void draw(SpriteBatch b)
		{
			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();

			// Blackout
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea(),
				Color.Black * 0.5f);
			// Cookbook
			b.Draw(
				Texture,
				new Vector2(_cookbookLeftRect.X, _cookbookLeftRect.Y),
				CookbookSource,
				Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 1f);

			if (state == State.Recipe)
				DrawRecipePage(b);
			else if (state == State.Search)
				DrawSearchPage(b);
			DrawCraftingPage(b);
			//b.Draw(Game1.fadeToBlackRect, CookButtonBounds, Color.Red);
			DrawInventoryMenu(b);
			DrawActualInventory(b);
			DrawExtraStuff(b);
		}

		private void DrawSearchPage(SpriteBatch b)
		{
			// Search nav buttons
			NavUpButton.bounds.Y = _showSearchFilters
				? SearchButton.bounds.Y + SearchButton.bounds.Height + 16 + FilterContainerBounds.Height
				: SearchButton.bounds.Y + SearchButton.bounds.Height + 16;
			if (_currentRecipe > 0)
				NavUpButton.draw(b);
			if (_currentRecipe < _filteredRecipeList.Count - 1)
				NavDownButton.draw(b);

			// Recipe entries
			UpdateSearchRecipes();
			int textSpacing = 80, yOffset, x, y, w;
			string text;
			CraftingRecipe r;

			SearchResultsArea.Y = NavUpButton.bounds.Y - 8;
			SearchResultsArea.Height = NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16;
			x = SearchResultsArea.X;
			yOffset = (SearchResultsArea.Height % _recipeHeight) / 2;
			var rows = SearchResultsArea.Height / _recipeHeight;

			if (ModEntry.Instance.SaveData.IsUsingGridViewInRecipeSearch)
			{
				var columns = SearchResultsArea.Width / _recipeHeight;
				_recipesPerPage = columns * rows;

				for (var i = 0; i < _searchRecipes.Count; ++i)
				{
					y = SearchResultsArea.Y + yOffset + (i / columns) * _recipeHeight + (_recipeHeight - 64) / 2;
					r = _searchRecipes[i];
					if (r.name == "Torch")
					{
						text = i18n.Get("menu.cooking_search.none_label");
						DrawText(b, text, 1f,
							_leftContent.X - SearchResultsArea.X + textSpacing - 16,
							SearchResultsArea.Y + 64,
							SearchResultsArea.Width - textSpacing, true);
						break;
					}
					x = SearchResultsArea.X + (i % columns) * _recipeHeight;
					r.drawMenuView(b, x, y);
				}
			}
			else
			{
				_recipesPerPage = rows;
				w = SearchResultsArea.Width - textSpacing;

				for (var i = 0; i < _searchRecipes.Count; ++i)
				{
					y = SearchResultsArea.Y + yOffset + i * _recipeHeight + (_recipeHeight - 64) / 2;
					r = _searchRecipes[i];
					if (r.name == "Torch")
					{
						text = i18n.Get("menu.cooking_search.none_label");
						DrawText(b, text, 1f,
							_leftContent.X - SearchResultsArea.X + textSpacing - 16,
							SearchResultsArea.Y + 64,
							SearchResultsArea.Width - textSpacing, true);
						break;
					}

					text = Game1.player.knowsRecipe(r.name)
						? r.DisplayName
						: i18n.Get("menu.cooking_recipe.title_unknown");
					r.drawMenuView(b, x, y);
					//b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, _recipeHeight, _recipeHeight), Color.Red);
					y -= (int)(Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, w)).Y / 2  - _recipeHeight / 2);
					DrawText(b, text, 1f, _leftContent.X - x + textSpacing, y, w, true);
				}
			}

			// Search bar
			SearchBarTextBox.Draw(b);
			if (SearchBarTextBox.Selected)
			{
				SearchButton.draw(b);
				return;
			}

			// Search filter toggles
			foreach (var clickable in new[] {ToggleOrderButton, ToggleFilterButton, ToggleViewButton})
				if (SearchBarTextBox.X + SearchBarTextBox.Width < clickable.bounds.X)
					clickable.draw(b);
			
			if (_showSearchFilters)
			{
				// Filter clickable icons container
				// left
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X, FilterContainerBounds.Y,
						FilterContainerSideWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X, FilterContainerSource.Y,
						FilterContainerSideWidth, FilterContainerSource.Height),
					Color.White);
				// middle
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X + FilterContainerSideWidth * Scale, FilterContainerBounds.Y,
						FilterContainerMiddleWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X + FilterContainerSideWidth, FilterContainerSource.Y,
						1, FilterContainerSource.Height),
					Color.White);
				// right
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X + FilterContainerSideWidth * Scale + FilterContainerMiddleWidth * Scale,
						FilterContainerBounds.Y,
						FilterContainerSideWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X + FilterContainerSideWidth + 1, FilterContainerSource.Y,
						FilterContainerSideWidth, FilterContainerSource.Height),
					Color.White);

				// Filter clickable icons
				foreach (var clickable in FilterButtons)
					clickable.draw(b);
			}
		}

		private void DrawRecipePage(SpriteBatch b)
		{
			var duration = "30";

			var textPosition = Vector2.Zero;
			var textWidth = _textWidth;
			string text;

			// Clickables
			if (_currentRecipe > 0)
				NavLeftButton.draw(b);
			if (_currentRecipe < _filteredRecipeList.Count - 1)
				NavRightButton.draw(b);

			// Recipe icon and title
			textPosition.Y = NavLeftButton.bounds.Y + 4;
			textPosition.X = NavLeftButton.bounds.X + NavLeftButton.bounds.Width;
			_filteredRecipeList[_currentRecipe].drawMenuView(b, (int)textPosition.X, (int)textPosition.Y);
			textWidth = 142;
			text = Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name)
				? _filteredRecipeList[_currentRecipe].DisplayName
				: i18n.Get("menu.cooking_recipe.title_unknown");
			textPosition.X = NavLeftButton.bounds.Width + 56;
			textPosition.Y -= Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y / 2 - 24;
			DrawText(b, text, 1.5f, textPosition.X, textPosition.Y, textWidth, true);

			// Recipe description
			textPosition.X = 0;
			textPosition.Y = NavLeftButton.bounds.Y + NavLeftButton.bounds.Height + 20;
			textWidth = _textWidth;
			text = Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name)
				? _recipeDescription
				: i18n.Get("menu.cooking_recipe.title_unknown");
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, textWidth, true);
			textPosition.Y += TextDividerGap * 2;

			// Recipe ingredients
			textPosition.Y += TextDividerGap + Game1.smallFont.MeasureString(
				Game1.parseText("Hoplite!\nHoplite!\nHoplite!", Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap;
			text = i18n.Get("menu.cooking_recipe.ingredients_label");
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true, SubtextColour);
			textPosition.Y += Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap - 64 / 2 + 4;

			if (Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name))
			{
				for (var i = 0; i < _recipeIngredients?.Count; ++i)
				{
					textPosition.Y += 64 / 2 + (_recipeIngredients.Count < 5 ? 4 : 0);

					var id = _recipeIngredients.Keys.ElementAt(i);
					var requiredCount = _recipeIngredients.Values.ElementAt(i);
					var requiredItem = id;
					var bagCount = Game1.player.getItemCount(requiredItem, 8);
					var dropInCount = _cookingSlotsDropIn.Where(item => item?.ParentSheetIndex == id)
						.Aggregate(0, (current, item) => current + item.Stack);
					requiredCount -= bagCount + dropInCount;
					var ingredientNameText = _filteredRecipeList[_currentRecipe].getNameFromIndex(id);
					var drawColour = requiredCount <= 0 ? Game1.textColor : BlockedColour;

					// Ingredient icon
					b.Draw(
						Game1.objectSpriteSheet,
						new Vector2(_leftContent.X, textPosition.Y - 2f),
						Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet,
							_filteredRecipeList[_currentRecipe].getSpriteIndexFromRawIndex(id), 16, 16),
						Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.86f);
					// Ingredient quantity
					Utility.drawTinyDigits(
						_recipeIngredients.Values.ElementAt(i),
						b,
						new Vector2(
							_leftContent.X + 32 - Game1.tinyFont.MeasureString(
								string.Concat(_recipeIngredients.Values.ElementAt(i))).X,
							textPosition.Y + 21 - 2f),
						2f,
						0.87f,
						Color.AntiqueWhite);
					// Ingredient name
					DrawText(b, ingredientNameText, 1f, 48, textPosition.Y, null, true, drawColour);

					// Ingredient stock
					if (!Game1.options.showAdvancedCraftingInformation)
						continue;
					var position = new Point(_lineWidth - 64, (int)(textPosition.Y + 2));
					b.Draw(
						Game1.mouseCursors,
						new Rectangle(_leftContent.X + position.X, position.Y, 22, 26),
						new Rectangle(268, 1436, 11, 13),
						Color.White);
					DrawText(b, string.Concat(bagCount + dropInCount), 1f, position.X + 32, position.Y, 64, true, drawColour);
				}
			}
			else
			{
				textPosition.Y += 64 / 2 + 4;
				text = i18n.Get("menu.cooking_recipe.title_unknown");
				DrawText(b, text, 1f, 40, textPosition.Y, textWidth, true, SubtextColour);
			}

			// Recipe cooking duration and clock icon
			text = i18n.Get("menu.cooking_recipe.time_label");
			textPosition.Y = _cookbookLeftRect.Y + _cookbookLeftRect.Height - 56 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true);
			text = _filteredRecipeList[_currentRecipe].timesCrafted > 0
				? i18n.Get("menu.cooking_recipe.time_value", new { duration })
				: i18n.Get("menu.cooking_recipe.title_unknown");
			textPosition.X = _lineWidth - 16 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).X;
			Utility.drawWithShadow(b,
				Game1.mouseCursors,
				new Vector2(_leftContent.X + textPosition.X, textPosition.Y + 6),
				new Rectangle(434, 475, 9, 9),
				Color.White, 0f, Vector2.Zero, 2f, false, 1f,
				-2, 2);
			textPosition.X += 24;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true);

		}

		private void DrawCraftingPage(SpriteBatch b)
		{
			// Cooking slots
			foreach (var clickable in CookingSlots)
				clickable.draw(b);

			for (var i = 0; i < _cookingSlotsDropIn.Count; ++i)
				_cookingSlotsDropIn[i]?.drawInMenu(b,
					new Vector2(
						CookingSlots[i].bounds.Location.X + CookingSlots[i].bounds.Width / 2 - 64 / 2,
						CookingSlots[i].bounds.Location.Y + CookingSlots[i].bounds.Height / 2 - 64 / 2),
					1f, 1f, 1f,
					StackDrawType.Draw, Color.White, true);

			var textPosition = Vector2.Zero;
			var textWidth = _textWidth;
			string text;

			// Recipe notes
			text = i18n.Get("menu.cooking_recipe.notes_label");
			textPosition.Y = _cookbookRightRect.Y + _cookbookRightRect.Height - 196 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;

			if (_showCookingConfirmPopup)
			{
				textPosition.Y += 16;
				textPosition.X += 64;

				// Contextual cooking popup
				Game1.DrawBox(CookIconBounds.X, CookIconBounds.Y, CookIconBounds.Width, CookIconBounds.Height);
				_filteredRecipeList[_currentRecipe].drawMenuView(b, CookIconBounds.X + 14, CookIconBounds.Y + 14);

				CookQuantityUpButton.draw(b);
				QuantityTextBox.Draw(b);
				CookQuantityDownButton.draw(b);

				CookConfirmButton.draw(b);
				CookCancelButton.draw(b);

				return;
			}

			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, false);
			textPosition.Y += TextDividerGap;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, SubtextColour);
			textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, false);
			textPosition.Y += TextDividerGap * 2;

			if (_recipeItem == null || _stack.Count < 1 || _stack.Peek() == State.Search)
				return;

			if (ShouldDrawCookButton())
			{
				textPosition.Y += 16;
				textPosition.X = _rightContent.X + _cookbookRightRect.Width / 2 - MarginRight;

				// Cook! button
				var source = CookButtonSource;
				source.X += _animFrame * CookButtonSource.Width;
				var dest = new Rectangle(
					(int)textPosition.X, (int)textPosition.Y,
					source.Width * Scale, source.Height * Scale);
				dest.X -= (CookTextSourceWidths[_locale] / 2 * Scale - CookTextSideWidth * Scale) + MarginLeft;
				var clickableArea = new Rectangle(dest.X, dest.Y, CookTextSideWidth * Scale * 2 + CookTextMiddleWidth * Scale, dest.Height);
				if (clickableArea.Contains(Game1.getMouseX(), Game1.getMouseY()))
					source.Y += source.Height;
				// left
				source.Width = CookTextSideWidth;
				dest.Width = source.Width * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				// middle and text
				source.X = _animFrame * CookButtonSource.Width + CookButtonSource.X + CookTextSideWidth;
				source.Width = 1;
				dest.Width = CookTextMiddleWidth * Scale;
				dest.X += CookTextSideWidth * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				b.Draw(
					Texture,
					new Rectangle(
						dest.X, dest.Y + (dest.Height - CookTextSource[_locale].Height * Scale) / 2
									   + _animTextOffsetPerFrame[_animFrame] * Scale,
						CookTextSource[_locale].Width * Scale, CookTextSource[_locale].Height * Scale),
					CookTextSource[_locale],
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				// right
				source.X = _animFrame * CookButtonSource.Width + CookButtonSource.X + CookButtonSource.Width - CookTextSideWidth;
				source.Width = CookTextSideWidth;
				dest.Width = source.Width * Scale;
				dest.X += CookTextMiddleWidth * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);

				// DANCING FORKS
				/*var flipped = _animFrame >= 4 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				for (var i = 0; i < 2; ++i)
				{
					var xSourceOffset = i == 1 ? 32 : 0;
					var ySourceOffset = _animFrame % 2 == 0 ? 32 : 0;
					var xDestOffset = i == 1 ? FilterContainerSideWidth * 2 * Scale + FilterContainerMiddleWidth * Scale + 96 : 0;
					b.Draw(
						Texture,
						new Vector2(_rightContent.X + xDestOffset - 8, dest.Y - 32),
						new Rectangle(128 + xSourceOffset, 16 + ySourceOffset, 32, 32),
						Color.White, 0f, Vector2.Zero, Scale, flipped, 1f);
				}*/
			}
			else if (!ModEntry.Instance.SaveData.FoodsEaten.ContainsKey(_recipeItem.Name)
			         || ModEntry.Instance.SaveData.FoodsEaten[_recipeItem.Name] < 1)
			{
				text = i18n.Get("menu.cooking_recipe.notes_unknown");
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, textWidth, false, SubtextColour);
			}
			else
			{
				// Energy
				textPosition.X = -8f;
				text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3116",
					_recipeItem.staminaRecoveredOnConsumption());
				Utility.drawWithShadow(b,
					Game1.mouseCursors,
					new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
					new Rectangle(0, 428, 10, 10),
					Color.White, 0f, Vector2.Zero, 3f);
				textPosition.X += 34f;
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
				textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
				// Health
				text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3118",
					_recipeItem.healthRecoveredOnConsumption());
				textPosition.X -= 34f;
				Utility.drawWithShadow(b,
					Game1.mouseCursors,
					new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
					new Rectangle(0, 428 + 10, 10, 10),
					Color.White, 0f, Vector2.Zero, 3f);
				textPosition.X += 34f;
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
				textPosition.Y -= Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
				textPosition.X += -34f + _lineWidth / 2f + 16f;

				// Buffs
				for (var i = 0; i < Math.Min(4, _recipeBuffs.Count); ++i)
				{
					if (_recipeBuffs[i] == 0)
						continue;

					Utility.drawWithShadow(b,
						Game1.mouseCursors,
						new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
						new Rectangle(10 + 10 * i, 428, 10, 10),
						Color.White, 0f, Vector2.Zero, 3f);
					textPosition.X += 34f;
					text = (_recipeBuffs[i] > 0 ? "+" : "")
						   + _recipeBuffs[i]
						   + Game1.content.LoadString($"Strings\\StringsFromCSFiles:Buff.cs.{480 + i * 3}");
					DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
					textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
					textPosition.X -= 34f;
				}
			}
		}

		private void DrawInventoryMenu(SpriteBatch b)
		{
			// Card
			Game1.drawDialogueBox(
				inventory.xPositionOnScreen - borderWidth / 2 - 32,
				inventory.yPositionOnScreen - borderWidth - spaceToClearTopBorder + 28,
				width,
				height - (borderWidth + spaceToClearTopBorder + 192) - 12,
				false, true);

			// Items
			//inventory.draw(b);

			var iconShakeTimer = _iconShakeTimerField.GetValue();
			for (var key = 0; key < inventory.inventory.Count; ++key)
				if (iconShakeTimer.ContainsKey(key)
					&& Game1.currentGameTime.TotalGameTime.TotalSeconds >= iconShakeTimer[key])
					iconShakeTimer.Remove(key);
		}

		/// <summary>
		/// Mostly a copy of InventoryMenu.draw(SpriteBatch b, int red, int blue, int green),
		/// though items considered unable to be cooked will be greyed out.
		/// </summary>
		private void DrawActualInventory(SpriteBatch b)
		{
			var iconShakeTimer = _iconShakeTimerField.GetValue();
			for (var key = 0; key < inventory.inventory.Count; ++key)
			{
				if (iconShakeTimer.ContainsKey(key)
					&& Game1.currentGameTime.TotalGameTime.TotalSeconds >= iconShakeTimer[key])
					iconShakeTimer.Remove(key);
			}
			for (var i = 0; i < inventory.capacity; ++i)
			{
				var position = new Vector2(
					inventory.xPositionOnScreen
					 + i % (inventory.capacity / inventory.rows) * 64
					 + inventory.horizontalGap * (i % (inventory.capacity / inventory.rows)),
					inventory.yPositionOnScreen
						+ i / (inventory.capacity / inventory.rows) * (64 + inventory.verticalGap)
						+ (i / (inventory.capacity / inventory.rows) - 1) * 4
						- (i >= inventory.capacity / inventory.rows
						   || !inventory.playerInventory || inventory.verticalGap != 0 ? 0 : 12));

				b.Draw(
					Game1.menuTexture,
					position,
					Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10),
					Color.White, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);

				if ((inventory.playerInventory || inventory.showGrayedOutSlots) && i >= Game1.player.maxItems.Value)
					b.Draw(
						Game1.menuTexture,
						position,
						Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57),
						Color.White * 0.5f, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);

				if (i >= 12 || !inventory.playerInventory)
					continue;

				var text = "";
				switch (i)
				{
					case 9:
						text = "0";
						break;
					case 10:
						text = "-";
						break;
					case 11:
						text = "=";
						break;
					default:
						text = string.Concat(i + 1);
						break;
				}
				var vector2 = Game1.tinyFont.MeasureString(text);
				b.DrawString(
					Game1.tinyFont,
					text,
					position + new Vector2((float)(32.0 - vector2.X / 2.0), -vector2.Y),
					i == Game1.player.CurrentToolIndex ? Color.Red : Color.DimGray);
			}
			for (var i = 0; i < inventory.capacity; ++i)
			{
				var colour = CanBeCooked(inventory.actualInventory[i]) ? Color.White : Color.DarkGray;

				var location = new Vector2(
					inventory.xPositionOnScreen
					 + i % (inventory.capacity / inventory.rows) * 64
					 + inventory.horizontalGap * (i % (inventory.capacity / inventory.rows)),
					inventory.yPositionOnScreen
						+ i / (inventory.capacity / inventory.rows) * (64 + inventory.verticalGap)
						+ (i / (inventory.capacity / inventory.rows) - 1) * 4
						- (i >= inventory.capacity / inventory.rows
						   || !inventory.playerInventory || inventory.verticalGap != 0 ? 0 : 12));

				if (inventory.actualInventory.Count <= i || inventory.actualInventory.ElementAt(i) == null)
					continue;

				var drawShadow = inventory.highlightMethod(inventory.actualInventory[i]);
				if (iconShakeTimer.ContainsKey(i))
					location += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				inventory.actualInventory[i].drawInMenu(
					b,
					location,
					inventory.inventory.Count > i ? inventory.inventory[i].scale : 1f,
					!inventory.highlightMethod(inventory.actualInventory[i]) ? 0.25f : 1f,
					0.865f,
					StackDrawType.Draw,
					colour,
					drawShadow);
			}
		}

		private void DrawExtraStuff(SpriteBatch b)
		{
			/*
			if (message != null)
			{
				Game1.drawDialogueBox(
					Game1.viewport.Width / 2, ItemsToGrabMenu.yPositionOnScreen + ItemsToGrabMenu.height / 2,
					false, false, message);
			}
			if (poof != null)
			{
				poof.draw(b, true);
			}
			*/

			foreach (var transferredItemSprite in _transferredItemSprites)
				transferredItemSprite.Draw(b);

			specialButton?.draw(b);
			upperRightCloseButton.draw(b);

			// Hover text
			if (hoverText != null)
			{
				if (hoverAmount > 0)
					drawToolTip(b, hoverText, "", null, true, -1, 0,
						-1, -1, null, hoverAmount);
				else
					drawHoverText(b, hoverText, Game1.smallFont);
			}

			// Trashcan
			if (trashCan != null)
			{
				trashCan.draw(b);
				b.Draw(Game1.mouseCursors,
					new Vector2(trashCan.bounds.X + 60, trashCan.bounds.Y + 40),
					new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10),
					Color.White, trashCanLidRotation, new Vector2(16f, 10f), Scale, SpriteEffects.None, 0.86f);
			}

			// Hover elements
			if (hoveredItem != null)
				drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
			else if (hoveredItem != null && ItemsToGrabMenu != null)
				drawToolTip(b, ItemsToGrabMenu.descriptionText, ItemsToGrabMenu.descriptionTitle, hoveredItem, heldItem != null);
			heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

			// Search button
			SearchTabButton.draw(b);

			// Cursor
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
		}

		private void DrawText(SpriteBatch b, string text, float scale, float x, float y, float? w, bool isLeftSide, Color? colour = null)
		{
			var position = isLeftSide ? _leftContent : _rightContent;
			position.Y -= yPositionOnScreen;
			Utility.drawTextWithShadow(b, Game1.parseText(text, Game1.smallFont,
				w != null ? (int)w : (int)Game1.smallFont.MeasureString(text).X), Game1.smallFont,
				new Vector2(position.X + x, position.Y + y), colour ?? Game1.textColor, scale);
		}

		private void DrawHorizontalDivider(SpriteBatch b, float x, float y, int w, bool isLeftSide)
		{
			var position = isLeftSide ? _leftContent : _rightContent;
			position.Y -= yPositionOnScreen;
			Utility.drawLineWithScreenCoordinates(
				position.X + TextMuffinTopOverDivider, (int)(position.Y + y),
				position.X + w + TextMuffinTopOverDivider, (int)(position.Y + y),
				b, DividerColour);
		}
	}
}
