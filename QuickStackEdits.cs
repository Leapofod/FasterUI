using MonoMod.Cil;
using System;
using Terraria.ModLoader;

namespace FasterUI;

internal static class QuickStackEdits
{
	private static int stackSplitMultiplier => (int)Math.Max(1, 60 * TimeKeeper.DoDrawDeltaTime);

	private static int stackSplitDifference => stackSplitMultiplier - Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), stackSplitMultiplier);
	private static int rightClickLoopIndex;

	internal static void ApplyQuickStackEdits()
	{
		IL.Terraria.Main.DoDraw += (il) =>
		{
			var c = new ILCursor(il);

			Mono.Cecil.FieldReference stackCounterField = null;
			Mono.Cecil.FieldReference stackDelayField = null;
			int local_i_index = -1;

			// Gets the index for the i local variable for for loops
			if (!c.TryFindNext(out _,
				x => x.MatchLdcI4(0) &&
				x.Next.MatchStloc(out local_i_index) &&
				x.Next.Next.MatchBr(out _)) &&
				local_i_index == -1)
			{
				LogIL("Main::DoDraw (0)");
				return;
			}

			// Goes to Main.stackDelay--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out stackDelayField) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(stackDelayField)))
			{
				LogIL("Main::DoDraw (1)");
				return;
			}

			// Changes stackDelay--; -> stackDelay -= stackCounter / num;
			// Local variable num computed right before this
			int numIndex = -1;

			if (!c.TryFindPrev(out _,
				x => x.MatchBlt(out _) &&
				x.Previous.MatchLdloc(out numIndex) &&
				x.Previous.Previous.MatchLdsfld(out stackCounterField)) &&
				numIndex == -1)
			{
				LogIL("Main::DoDraw (2)");
				return;
			}

			c.Emit(Mono.Cecil.Cil.OpCodes.Ldsfld, stackCounterField);

			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)numIndex);
			c.Emit(Mono.Cecil.Cil.OpCodes.Div);

			// Consumes the previously loaded 1
			c.Emit(Mono.Cecil.Cil.OpCodes.Mul);

			// >	sub
			// >	stsfld	int32 Terraria.Main::stackDelay


			/* 
			 * Changes
			 *	stackDelay -= stackCounter / num;
			 *	if (stackDelay < 2)
			 *	{
			 *		stackDelay = 2;
			 *		superFastStack++;
			 *	}
			 *	to
			 *	stackDelay -= stackCounter / num;
			 *	if (stackDelay < 2)
			 *	{
			 *		(A) int i = 1 - stackDelay;
			 *		stackDelay = 2;
			 *		(M) superFastStack += 1 + i;
			 *	}
			 */

			/*
			 * Moves to 
			 *	if (stackDelay < 2)
			 *	{
			 *		-->
			 *		stackDelay = 2;
			 *		...
			 */
			if (!c.TryGotoNext(MoveType.Before, x => x.MatchLdcI4(2) && x.Next.MatchStsfld(stackDelayField)))
			{
				LogIL("Main::DoDraw (3)");
				return;
			}

			/*
			 * Adds
			 *	int i = 1 - stackDelay;
			 */
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldsfld, stackDelayField);
			c.Emit(Mono.Cecil.Cil.OpCodes.Sub);
			c.Emit(Mono.Cecil.Cil.OpCodes.Stloc_S, (byte)local_i_index);

			/*
			 * Moves to
			 *		...
			 *		-> superFastStack++;
			 *	}
			 */
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var sFSfld) &&
				x.Next.MatchAdd() &&
				x.Next.Next.MatchStsfld(sFSfld)))
			{
				LogIL("Main::DoDraw (4)");
				return;
			}

			/* 
			 * Changes
			 *		superFastStack++;
			 * to
			 *		superFastStack += 1 + i;
			 */
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)local_i_index);
			c.Emit(Mono.Cecil.Cil.OpCodes.Add);


			/* Changes
			 *		...
			 *	}
			 *	stackCounter = 0;
			 * to
			 *		...
			 *	}
			 *	stackCounter %= num;
			 */
			
			// Goes to stackCounter = 0;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(0) &&
				x.Next.MatchStsfld(stackCounterField)))
			{
				LogIL("Main::DoDraw (5)");
				return;
			}

			// Computes stackCounter % num;
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldsfld, stackCounterField);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)numIndex);
			c.Emit(Mono.Cecil.Cil.OpCodes.Rem);

			// Basically consumes the 0; adds the computed remainder to the loaded 0
			c.Emit(Mono.Cecil.Cil.OpCodes.Add); 

			// >	stsfld	int32 Terraria.Main::stackCounter


			// Goes to Main.stackCounter++;
			if (!c.TryGotoPrev(MoveType.After,
				x => x.Previous.MatchLdsfld(stackCounterField) &&
				x.MatchLdcI4(1) &&
				x.Next.MatchAdd() &&
				x.Next.Next.MatchStsfld(stackCounterField)))
			{
				LogIL("Main::DoDraw (6)");
				return;
			}

			// Multiplies by multiplier
			// Should speed up acceleration??
			//c.EmitDelegate<Func<int, int>>(one => Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), one * stackSplitMultiplier));
			c.EmitDelegate<Func<int, int>>(one => one * stackSplitMultiplier);

			// Reposition for next jumps
			if (!c.TryGotoNext(x => x.MatchAdd()))
			{
				LogIL("Main::DoDraw (7)");
				return;
			}

			// Goes to Main.mapTime--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var Main_mapTime) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(Main_mapTime)))
			{
				LogIL("Main::DoDraw (8)");
				return;
			}


			// goes to Main.stackSplit--;
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchLdcI4(1) &&
				x.Previous.MatchLdsfld(out var Main_stackSplit) &&
				x.Next.MatchSub() &&
				x.Next.Next.MatchStsfld(Main_stackSplit)))
			{
				LogIL("Main::DoDraw (9)");
				return;
			}

			// Multiplies by delta time correction, doesn't go below 1
			c.EmitDelegate<Func<int, int>>(one => Math.Min(Math.Max(1, Terraria.Main.stackSplit - 1), one * stackSplitMultiplier));
		};

		IL.Terraria.UI.ItemSlot.HandleShopSlot += il =>
		{
			var c = new ILCursor(il);
			int numIndex = -1;

			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchStloc(out numIndex) && 
				x.Previous.MatchAdd() &&
				x.Previous.Previous.MatchLdcI4(1) &&
				x.Previous.Previous.Previous.MatchLdsfld(out _)) &&	
				numIndex == -1)
			{
				ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct HandleShopSlot Index");
				return;
			}

			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)numIndex);
			c.EmitDelegate<Func<int, int>>(num => num == 1 ? num : num * stackSplitMultiplier);
			c.Emit(Mono.Cecil.Cil.OpCodes.Stloc_S, (byte)numIndex);
		};

		IL.Terraria.UI.ItemSlot.RightClick_ItemArray_int_int += il =>
		{
			var c = new ILCursor(il);

			int locPlayerIndex = -1;
			int locFlagIndex = -1;
			var loopSkipLabel = c.DefineLabel();

			/*
			 * Moves cursor to after
			 *	{
			 *		bool flag = true;
			 *		...
			 */
			if (!c.TryGotoNext(x => x.MatchLdloc(out locPlayerIndex)) || locPlayerIndex == -1)
			{
				LogIL("ItemSlot::RightClick (0)");
				return;
			}

			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchStloc(out locFlagIndex) &&
				x.Previous.MatchLdcI4(1) &&
				locFlagIndex != locPlayerIndex))
			{
				LogIL("ItemSlot::RightClick (1)");
				return;
			}

			c.EmitDelegate(() => { rightClickLoopIndex = 0; });

			/*
			 * Moves cursor to before first condition in
			 *	if (flag && (Main.mouseItem.IsTheSameAs(inv[slot]) && 
			 *		ItemLoader.CanStack(Main.mouseItem, inv[slot]) || 
			 *		Main.mouseItem.type == 0) && 
			 *		(Main.mouseItem.stack < Main.mouseItem.maxStack || 
			 *		Main.mouseItem.type == 0))
			 */
			if (!c.TryGotoNext(MoveType.Before, 
				x => x.MatchLdloc(locFlagIndex) && 
				x.Next.MatchBrfalse(out loopSkipLabel)))
			{
				LogIL("ItemSlot::RightClick (2)");
				return;
			}
			var loopStartLabel = c.MarkLabel();

			/*
			 * Goes to after the call to
			 *	{
			 *		PickupItemIntoMouse(inv, context, slot, player);
			 *		...
			 */
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchCall(out _) &&
				x.Previous.MatchLdloc(locPlayerIndex) &&
				x.Previous.Previous.MatchLdarg(out _) &&
				x.Previous.Previous.Previous.MatchLdarg(out _) &&
				x.Previous.Previous.Previous.Previous.MatchLdarg(out _)))
			{
				LogIL("ItemSlot::RightClick (3)");
				return;
			}

			/*
			 * Jumps to start of the loop if not on first iteration and conditions are met
			 */
			c.EmitDelegate(() =>
			{
				rightClickLoopIndex++;
				return rightClickLoopIndex > 1 &&
					Terraria.Main.superFastStack > 0 &&
					rightClickLoopIndex <= stackSplitDifference;
			});
			c.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, loopStartLabel);

			/*
			 * Breaks out of loop if on last iteration and isn't on first iteration 
			 */
			c.EmitDelegate(() =>
				rightClickLoopIndex > 1 &&
				rightClickLoopIndex > stackSplitDifference &&
				Terraria.Main.superFastStack > 0);
			c.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, loopSkipLabel);

			/* Goes to after the call to
			 *		...
			 *		RefreshStackSplitCooldown();
			 *	}
			 * 
			 */
			if (!c.TryGotoNext(MoveType.After, 
				x => x.MatchCall(out _) && x.Previous.MatchPop()))
			{
				LogIL("ItemSlot::RightClick (4)");
				return;
			}

			/*
			 * Jumps to start of loop if it's the first iteration and conditions are met
			 */
			c.EmitDelegate(()
				=> Terraria.Main.superFastStack > 0 &&
				rightClickLoopIndex <= stackSplitDifference);
			c.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, loopStartLabel);

			//c.MarkLabel(loopSkipLabel);
		};

		On.Terraria.Main.HoverOverCraftingItemButton += (On.Terraria.Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) =>
		{
			orig(recipeIndex);
			for (int i = 0; i < stackSplitDifference && Terraria.Main.superFastStack > 0; i++)
			{
				var prevRec = Terraria.Main.availableRecipe[recipeIndex];
				Terraria.Recipe.FindRecipes();
				if (Terraria.Main.availableRecipe.Length < recipeIndex ||
					Terraria.Main.availableRecipe[recipeIndex] != prevRec)
					break;
				Terraria.Main.stackSplit = 1;
				orig(recipeIndex);
			}
		};
	}

	private static void LogIL(string msg)
	{
		ModContent.GetInstance<FasterUI>().Logger.Error(msg);
#if DEBUG
		throw new Exception("Debug Exception: " + msg);
#endif
	}
}
