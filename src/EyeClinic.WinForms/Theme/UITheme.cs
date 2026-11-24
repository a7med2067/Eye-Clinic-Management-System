using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace EyeClinic.WinForms.Theme
{
    public static class UITheme
    {
        // Color Palette
        public static readonly Color BrandPrimary = Color.FromArgb(15, 76, 129);   // Deep Medical Navy
        public static readonly Color BrandAccent = Color.FromArgb(2, 132, 199);    // Clear Sky Blue
        public static readonly Color BrandDark = Color.FromArgb(11, 37, 69);       // Midnight Navy
        public static readonly Color Success = Color.FromArgb(22, 163, 74);        // Emerald Green
        public static readonly Color Warning = Color.FromArgb(217, 119, 6);        // Amber Orange
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);         // Crimson Red
        public static readonly Color Background = Color.FromArgb(241, 245, 249);   // Soft Slate
        public static readonly Color CardBg = Color.White;
        public static readonly Color BorderColor = Color.FromArgb(203, 213, 225);  // Slate 300
        public static readonly Color TextDark = Color.FromArgb(15, 23, 42);        // Slate 900
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);    // Slate 500

        // Fonts
        public static readonly Font HeaderFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        public static readonly Font SubHeaderFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        private static Icon _appIcon;
        public static Icon AppIcon
        {
            get
            {
                if (_appIcon != null) return _appIcon;
                try
                {
                    string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        _appIcon = new Icon(iconPath);
                        return _appIcon;
                    }
                    _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    return _appIcon;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static void ApplyFormDefaults(Form form)
        {
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.Font = BodyFont;
            form.BackColor = Background;
            form.ForeColor = TextDark;
            form.StartPosition = FormStartPosition.CenterScreen;
            if (AppIcon != null)
            {
                form.Icon = AppIcon;
            }
        }

        public static void StyleButton(Button btn, Color bg, Color fg, bool isBold = true)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = isBold ? BodyBoldFont : BodyFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = Math.Max(btn.Height, 34);
        }

        public static void StylePrimaryButton(Button btn)
        {
            StyleButton(btn, BrandPrimary, Color.White, true);
        }

        public static void StyleSuccessButton(Button btn)
        {
            StyleButton(btn, Success, Color.White, true);
        }

        public static void StyleDangerButton(Button btn)
        {
            StyleButton(btn, Danger, Color.White, true);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = Color.White;
            btn.ForeColor = TextDark;
            btn.Font = BodyBoldFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = Math.Max(btn.Height, 34);
        }

        public static void StyleDataGridView(DataGridView dgv)
        {
            SetDoubleBuffered(dgv);
            dgv.RightToLeft = RightToLeft.Yes;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoGenerateColumns = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowTemplate.Height = 36;

            // Column Header - MiddleRight for Arabic RTL
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgv.ColumnHeadersDefaultCellStyle.Font = BodyBoldFont;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Rows - MiddleRight for Arabic RTL
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = TextDark;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254); // Light blue selection
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(12, 74, 110);
            dgv.DefaultCellStyle.Font = BodyFont;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.AlternatingRowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        public static void ApplyRtlToHierarchy(Control parent)
        {
            if (parent == null) return;

            parent.RightToLeft = RightToLeft.Yes;

            if (parent is Form form)
            {
                form.RightToLeftLayout = true;
            }
            else if (parent is TabControl tab)
            {
                tab.RightToLeftLayout = true;
            }
            else if (parent is DateTimePicker dtp)
            {
                dtp.RightToLeftLayout = true;
            }
            else if (parent is DataGridView dgv)
            {
                StyleDataGridView(dgv);
            }
            else if (parent is TextBox txt)
            {
                txt.TextAlign = HorizontalAlignment.Right;
            }
            else if (parent is NumericUpDown num)
            {
                num.TextAlign = HorizontalAlignment.Right;
                num.UpDownAlign = LeftRightAlignment.Left;
            }
            else if (parent is Label lbl)
            {
                if (lbl.TextAlign == ContentAlignment.TopLeft)
                    lbl.TextAlign = ContentAlignment.TopRight;
                else if (lbl.TextAlign == ContentAlignment.MiddleLeft)
                    lbl.TextAlign = ContentAlignment.MiddleRight;
                else if (lbl.TextAlign == ContentAlignment.BottomLeft)
                    lbl.TextAlign = ContentAlignment.BottomRight;
            }
            else if (parent is FlowLayoutPanel flow)
            {
                flow.FlowDirection = FlowDirection.LeftToRight; // In WinForms RTL, LeftToRight flows visually from right to left
            }

            foreach (Control child in parent.Controls)
            {
                ApplyRtlToHierarchy(child);
            }
        }

        public static void StyleCard(Panel panel)
        {
            panel.BackColor = CardBg;
            panel.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void SetDoubleBuffered(Control control)
        {
            if (SystemInformation.TerminalServerSession) return;
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            prop?.SetValue(control, true, null);
        }
    }
}
