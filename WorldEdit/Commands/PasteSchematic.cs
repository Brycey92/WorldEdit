﻿using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class PasteSchematic : WECommand
    {
        private readonly WorldSectionData data;
        private readonly int alignment;
        private readonly Expression expression;
        private readonly bool mode_MainBlocks;
        
        public PasteSchematic(int x, int y, TSPlayer plr, WorldSectionData data, int alignment, Expression expression, bool mode_MainBlocks)
            : base(x, y, int.MaxValue, int.MaxValue, plr)
        {
            this.data = data;
            this.alignment = alignment;
            this.expression = expression;
            this.mode_MainBlocks = mode_MainBlocks;
        }

        public override void Execute()
        {
            var width = data.Width - 1;
            var height = data.Height - 1;

            if ((alignment & 1) == 0)
                x2 = x + width;
            else
            {
                x2 = x;
                x -= width;
            }
            if ((alignment & 2) == 0)
                y2 = y + height;
            else
            {
                y2 = y;
                y -= height;
            }

            for (var i = x; i <= x2; i++)
            {
                for (var j = y; j <= y2; j++)
                {
                    var index1 = i - x;
                    var index2 = j - y;

                    if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY ||
                        expression != null && !expression.Evaluate(mode_MainBlocks
                                                ? Main.tile[i, j]
                                                : data.Tiles[index1, index2]))
                    {
                        continue;
                    }

                    Main.tile[i, j] = data.Tiles[index1, index2];
                }
            }

            foreach (var sign in data.Signs)
            {
                var id = Sign.ReadSign(sign.X + x, sign.Y + y);
                if (id == -1)
                {
                    continue;
                }

                Sign.TextSign(id, sign.Text);
            }

            foreach (var itemFrame in data.ItemFrames)
            {
                var ifX = itemFrame.X + x;
                var ifY = itemFrame.Y + y;

                var id = TEItemFrame.Place(ifX, ifY);
                if (id == -1)
                {
                    continue;
                }

                WorldGen.PlaceObject(ifX, ifY, TileID.ItemFrame);
                var frame = (TEItemFrame)TileEntity.ByID[id];

                frame.item = new Item();
                frame.item.netDefaults(itemFrame.Item.NetId);
                frame.item.stack = itemFrame.Item.Stack;
                frame.item.prefix = itemFrame.Item.PrefixId;
            }

            foreach (var chest in data.Chests)
            {
                int chestX = chest.X + x, chestY = chest.Y + y;

                int id;
                if ((id = Chest.FindChest(chestX, chestY)) == -1 &&
                    (id = Chest.CreateChest(chestX, chestY)) == -1)
                {
                    continue;
                }

                WorldGen.PlaceChest(chestX, chestY);
                for (var index = 0; index < chest.Items.Length; index++)
                {
                    var netItem = chest.Items[index];
                    var item = new Item();
                    item.netDefaults(netItem.NetId);
                    item.stack = netItem.Stack;
                    item.prefix = netItem.PrefixId;
                    Main.chest[id].item[index] = item;

                }
            }

            ResetSection();
            plr.SendSuccessMessage("Pasted schematic to selection.");
        }
    }
}