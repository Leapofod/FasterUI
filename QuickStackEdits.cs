using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace FasterUI;

internal static class QuickStackEdits
{
	private static int stackSplitMultiplier => (int)Math.Max(1, 60 * TimeKeeper.DoDrawDeltaTime);

	internal static int stackDelayTempVar;

	internal static void ApplyQuickStackEdits()
	{
		IL.Terraria.Main.DoDraw += (il) =>
		{
			var c = new ILCursor(il);

			Mono.Cecil.FieldReference stackCounterField = null;
			Mono.Cecil.FieldReference stackDelayField = null;
			#region Unused
			//int local_i_index = -1;

			//// Gets the index for the i local variable for for loops
			//if(!c.TryGotoNext(
			//	x => x.MatchLdcI4(0) &&
			//	x.Next.MatchStloc(out local_i_index) &&
			//	x.Next.Next.MatchBr(out _)) && 
			//	local_i_index == -1)
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}
			#endregion

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
			 *		(A) QuickStackEdits::stackDelayTempVar = 1 - stackDelay;
			 *		stackDelay = 2;
			 *		(M) superFastStack += 1 + QuickStackEdits::stackDelayTempVar;
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
			 *	QuickStackEdits::stackDelayTempVar = 1 - stackDelay;
			 */
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldsfld, stackDelayField);
			c.EmitDelegate<Action<int>>(stackDelay => stackDelayTempVar = 1 - stackDelay);

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
				x.Next.Next.MatchLdsfld(sFSfld)))
			{
				LogIL("Main::DoDraw (4)");
				return;
			}

			/* 
			 * Changes
			 *		superFastStack++;
			 * to
			 *		superFastStack += 1 + QuickStackEdits::stackDelayTempVar;
			 */
			c.EmitDelegate<Func<int, int>>(one => one + stackDelayTempVar);


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

			#region Unused
			//// Goes to before stackCounter++;
			//if (!c.TryGotoPrev(MoveType.Before,
			//	x => x.MatchLdsfld(out var Main_stackCounter) &&
			//	x.Next.MatchLdcI4(1) && 
			//	x.Next.Next.MatchAdd() &&
			//	x.Next.Next.Next.MatchStsfld(Main_stackCounter)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}

			//var loopBeginningLabel = c.DefineLabel();
			//var loopSkipLabel = c.DefineLabel();
			//var loopEndLabel = c.DefineLabel();

			//// Define a fod loop -> (int i = 0; i < stackSplitModifier - 1; i++)
			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, local_i_index);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Br_S, loopSkipLabel);
			//c.MarkLabel(loopBeginningLabel);

			//if (!c.TryGotoNext(
			//	x => x.Previous.Previous.MatchLdsfld(out _) &&
			//	x.Previous.MatchLdloc(out _) &&
			//	x.MatchBlt(out _)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}
			//c.Emit(Mono.Cecil.Cil.OpCodes.Blt_S, loopEndLabel);
			//c.Remove();

			//Mono.Cecil.FieldReference stackCounter = null;
			//if(!c.TryGotoNext(
			//	x => x.Previous.MatchLdcI4(0) &&
			//	x.MatchStsfld(out stackCounter)))
			//{
			//	ModContent.GetInstance<FasterUI>().Logger.Error("IL: Can't find correct DoDraw Index");
			//	return;
			//}

			//c.Emit(Mono.Cecil.Cil.OpCodes.Stsfld, stackCounter);
			//c.MarkLabel(loopEndLabel);

			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, local_i_index);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Add);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, local_i_index);
			//c.MarkLabel(loopSkipLabel);

			//c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, local_i_index);
			//c.EmitDelegate(() => stackSplitMultiplier);
			//c.Emit(Mono.Cecil.Cil.OpCodes.Blt_S, loopBeginningLabel);

			//c.Remove();
			#endregion

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

			// Multiplies by delta time correction, doesn't go below 0, doesn't subtract to 0 immediately
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

			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, numIndex);
			c.EmitDelegate<Func<int, int>>(num => num == 1 ? num : num * stackSplitMultiplier);
			c.Emit(Mono.Cecil.Cil.OpCodes.Stloc, numIndex);
		};
	}

	private static void LogIL(string msg)
	{
		ModContent.GetInstance<FasterUI>().Logger.Error(msg);
	}
}
