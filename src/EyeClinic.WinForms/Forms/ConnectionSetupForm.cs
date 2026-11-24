using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Data.Configuration;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class ConnectionSetupForm : Form
    {
        private TextBox txtServer;
        private TextBox txtDatabase;
        private CheckBox chkIntegrated;
        private TextBox txtUser;
        private TextBox txtPassword;
        private NumericUpDown numPort;
        private Button btnTest;
        private Button btnSave;
        private Label lblStatus;

        public ConnectionSetupForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "إعداد الاتصال بقاعدة البيانات - نظام عيادة العيون";
            this.Size = new Size(520, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.White
            };
            this.Controls.Add(pnlMain);

            var lblTitle = new Label
            {
                Text = "تهيئة الاتصال بـ SQL Server",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.BrandPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            pnlMain.Controls.Add(lblTitle);

            var lblDesc = new Label
            {
                Text = "اختر الاتصال بجهاز السيرفر (السكرتارية) أو محلياً (SQL Server 2014 Express).",
                Font = UITheme.SmallFont,
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Location = new Point(24, 52)
            };
            pnlMain.Controls.Add(lblDesc);

            int y = 90;

            // Server Host
            pnlMain.Controls.Add(new Label { Text = "اسم السيرفر أو عنوان الـ IP:", Location = new Point(24, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtServer = new TextBox { Location = new Point(24, y + 22), Width = 450, Font = UITheme.BodyFont };
            pnlMain.Controls.Add(txtServer);
            y += 60;

            // Database Name
            pnlMain.Controls.Add(new Label { Text = "اسم قاعدة البيانات:", Location = new Point(24, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtDatabase = new TextBox { Location = new Point(24, y + 22), Width = 450, Font = UITheme.BodyFont };
            pnlMain.Controls.Add(txtDatabase);
            y += 60;

            // Port
            pnlMain.Controls.Add(new Label { Text = "رقم المنفذ (Port الافتراضي 1433):", Location = new Point(24, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numPort = new NumericUpDown { Location = new Point(24, y + 22), Width = 160, Minimum = 1, Maximum = 65535, Value = 1433, Font = UITheme.BodyFont };
            pnlMain.Controls.Add(numPort);
            y += 60;

            // Integrated Security
            chkIntegrated = new CheckBox
            {
                Text = "استخدام مصادقة ويندوز (Windows Authentication)",
                Location = new Point(24, y),
                AutoSize = true,
                Font = UITheme.BodyBoldFont,
                Checked = true
            };
            chkIntegrated.CheckedChanged += (s, e) =>
            {
                txtUser.Enabled = !chkIntegrated.Checked;
                txtPassword.Enabled = !chkIntegrated.Checked;
            };
            pnlMain.Controls.Add(chkIntegrated);
            y += 35;

            // User & Password
            pnlMain.Controls.Add(new Label { Text = "اسم المستخدم (SQL User):", Location = new Point(260, y), AutoSize = true, Font = UITheme.SmallFont });
            txtUser = new TextBox { Location = new Point(260, y + 20), Width = 214, Enabled = false, Font = UITheme.BodyFont };
            pnlMain.Controls.Add(txtUser);

            pnlMain.Controls.Add(new Label { Text = "كلمة المرور:", Location = new Point(24, y), AutoSize = true, Font = UITheme.SmallFont });
            txtPassword = new TextBox { Location = new Point(24, y + 20), Width = 214, UseSystemPasswordChar = true, Enabled = false, Font = UITheme.BodyFont };
            pnlMain.Controls.Add(txtPassword);
            y += 65;

            // Status label
            lblStatus = new Label
            {
                Text = "",
                Location = new Point(24, y),
                Width = 450,
                Height = 36,
                Font = UITheme.SmallFont
            };
            pnlMain.Controls.Add(lblStatus);
            y += 42;

            // Buttons
            var flowButtons = new FlowLayoutPanel
            {
                Location = new Point(24, y),
                Size = new Size(450, 45),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlMain.Controls.Add(flowButtons);

            btnSave = new Button
            {
                Text = "حفظ ومتابعة",
                Width = 140,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;
            flowButtons.Controls.Add(btnSave);

            btnTest = new Button
            {
                Text = "اختبار الاتصال",
                Width = 140,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSecondaryButton(btnTest);
            btnTest.Click += BtnTest_Click;
            flowButtons.Controls.Add(btnTest);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void LoadCurrentSettings()
        {
            var cfg = ConnectionConfig.LoadConfig();
            txtServer.Text = cfg.Server;
            txtDatabase.Text = cfg.Database;
            chkIntegrated.Checked = cfg.IntegratedSecurity;
            txtUser.Text = cfg.SqlUser ?? "";
            txtPassword.Text = cfg.SqlPassword ?? "";
            numPort.Value = cfg.Port > 0 ? cfg.Port : 1433;
            txtUser.Enabled = !cfg.IntegratedSecurity;
            txtPassword.Enabled = !cfg.IntegratedSecurity;
        }

        private ConnectionConfigModel GetModelFromUI()
        {
            return new ConnectionConfigModel
            {
                Server = txtServer.Text.Trim(),
                Database = txtDatabase.Text.Trim(),
                Port = (int)numPort.Value,
                IntegratedSecurity = chkIntegrated.Checked,
                SqlUser = txtUser.Text.Trim(),
                SqlPassword = txtPassword.Text
            };
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            lblStatus.ForeColor = Color.Black;
            lblStatus.Text = "جاري اختبار الاتصال بقاعدة البيانات...";
            Application.DoEvents();

            var model = GetModelFromUI();
            string connStr = ConnectionConfig.BuildConnectionString(model);
            var result = ConnectionConfig.TestConnection(connStr);

            if (result.success)
            {
                lblStatus.ForeColor = UITheme.Success;
                lblStatus.Text = "✓ تم الاتصال بالسيرفر بنجاح!";
            }
            else
            {
                lblStatus.ForeColor = UITheme.Danger;
                lblStatus.Text = $"✗ فشل الاتصال: {result.message}";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var model = GetModelFromUI();
            string connStr = ConnectionConfig.BuildConnectionString(model);
            var result = ConnectionConfig.TestConnection(connStr);

            if (!result.success)
            {
                var dr = MessageBox.Show($"فشل اختبار الاتصال:\n{result.message}\n\nهل ترغب في حفظ الإعدادات على أي حال؟", "تحذير", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
            }

            ConnectionConfig.SaveConfig(model);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
