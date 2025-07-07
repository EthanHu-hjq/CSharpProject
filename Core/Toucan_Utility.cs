using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore.Configuration;
using TestCore.Ctrls;

namespace ToucanCore
{
    public static class Toucan_Utility
    {
        public static BarcodeFunction IsFunction(string context)
        {
            try
            {
                return (BarcodeFunction)Enum.Parse(typeof(BarcodeFunction), context);
            }
            catch
            {
                return BarcodeFunction.ILLEGAL_CMD;
            }
        }

        public static int IsSocketNumber(string context)
        {
            var rs = GlobalConfiguration.Default.General.RE_SocketNumber.Match(context);

            if (rs.Success)
            {
                return int.Parse(rs.Groups[1].Value);
            }
            else
            {
                return -1;
            }
        }

        public static int IsSerialNumber(string context, bool promptmsg = true)
        {
            var rs = GlobalConfiguration.Default.General.RE_SerialNumber.Match(context);

            if (promptmsg && !rs.Success)
            {
                MessageBox.Show(string.Format("Warning!!! Illegal Serial Number {0}, Should be match {1}", context, GlobalConfiguration.Default.General.RE_SerialNumber.ToString()));
            }

            return rs.Success ? 0 : -1;
        }

        public static void AutoResizePanelCtrl(Panel panel, SlotInfo[] slots, int col = 4, int border = 5, int column_count = 0)
        {
            panel.Parent.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            panel.Parent.Refresh();

            panel.Controls.Clear();

            if (slots?.FirstOrDefault() is null) return;

            if (column_count == 0)
            {
                col = Math.Min((panel.Size.Width - border) / (slots[0].MinimumSize.Width + border), slots.Length);
            }
            else
            {
                col = Math.Min(column_count, slots.Length);
            }

            if (col == 0) return;

            int row = (slots.Length / col) + (slots.Length % col == 0 ? 0 : 1);

            if (row > 1)
            {
                col = slots.Length / row + (slots.Length % row == 0 ? 0 : 1);
            }

            int width = (panel.Size.Width - border) / col;

            int height;

            if (row > 1)
            {
                height = Math.Max((panel.Size.Height - border) / row, slots[0].MinimumSize.Height + border);
            }
            else
            {
                height = panel.Height - border;
            }

            System.Threading.Thread.Sleep(20);

            for (int i = 0; i < slots.Length; i++)
            {
                var temp = slots[i];

                temp.Location = new Point(width * (i % col) + border, height * (i / col) + border);

                temp.Width = width - border;
                temp.Height = height - border;

                temp.Show();

                panel.Controls.Add(temp);
            }

            if (height * row > panel.Height)
            {
                panel.VerticalScroll.Enabled = true;
            }
        }
    }

    public enum BarcodeFunction
    {
        ILLEGAL_CMD = 0x8000, //Avoid the mistake converting from number 0
        TERMINATE,
        ABORT,
        TERMINATEALL,
        ABORTALL,
        TESTSTART,
        TESTFINISH,
        ENTER_VERIFICATION = 0x8100,
        QUIT_VERIFICATION,
    }
}
