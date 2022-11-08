using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace FasterUI;

internal class TimeKeeper : ModSystem
{
	/// <summary>
	/// Amount of time in seconds between the last frame and current frame (DrawInventory call)
	/// </summary>
	internal static double InventoryDeltaTime => m_inventoryDeltaTime / 1000.0;

	/// <summary>
	/// Previous value of Inventory Delta Time
	/// </summary>
	internal static double LastInventoryDeltaTime => m_lastInventoryDeltaTime / 1000.0;

	private static long m_lastInventoryDeltaTime;
	private static long m_inventoryDeltaTime;

	private static long m_lastInventoryUnixTimeMilli = DateTimeOffset.Now.ToUnixTimeMilliseconds();


	/// <summary>
	/// Amount of time in seconds between the last frame and current frame (DoDraw call)
	/// </summary>
	internal static double DoDrawDeltaTime => m_doDrawDeltaTime / 1000.0;

	private static long m_doDrawDeltaTime;
	private static long m_lastDoDrawUnixTimeMilli = DateTimeOffset.Now.ToUnixTimeMilliseconds();

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int invIdx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
		if (invIdx > -1)
		{
			layers.Insert(invIdx, new LegacyGameInterfaceLayer("FasterUI: Inventory Time Keeper", delegate
			{
				m_lastInventoryDeltaTime = m_inventoryDeltaTime;

				var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				m_inventoryDeltaTime = now - m_lastInventoryUnixTimeMilli;
				m_lastInventoryUnixTimeMilli = now;
				
				return true;
			}, InterfaceScaleType.None));
		}
	}

	public override void PostDrawInterface(SpriteBatch spriteBatch)
	{
		var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		m_doDrawDeltaTime = now - m_lastDoDrawUnixTimeMilli;
		m_lastDoDrawUnixTimeMilli = now;
	}
}
