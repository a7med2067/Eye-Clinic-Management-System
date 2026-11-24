using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class ChangePasswordDialog : Form
    {
        private TextBox txtCurrentPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Button btnSave;
        private Button btnCancel;
        private readonly AuthService _authService;
        private readonly int _userId;

        public ChangePasswordDialog(int userId, bool isForced = false)
        {
            _userId = userId;
            _authService = new AuthService();
            InitializeComponent(isForced);
        }

        private void InitializeComponent(bool isForced)
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = isForced ? "تغيير كلمة المرور (إجباري عند أول تسجيل)" : "تغيير كلمة المرور";
            this.Size = new Size(420, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(pnl);

            int y = 15;
            pnl.Controls.Add(new Label
            {
                Text = isForced ? "يجب تغيير كلمة المرور الافتراضية لأسباب أمنية:" : "تحديث كلمة المرور:",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 40;

            pnl.Controls.Add(new Label { Text = "كلمة المرور الحالية:", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtCurrentPassword = new TextBox { Location = new Point(20, y + 22), Width = 360, UseSystemPasswordChar = true };
            pnl.Controls.Add(txtCurrentPassword);
            y += 55;

            pnl.Controls.Add(new Label { Text = "كلمة المرور الجديدة (6 أحرف على الأقل):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtNewPassword = new TextBox { Location = new Point(20, y + 22), Width = 360, UseSystemPasswordChar = true };
            pnl.Controls.Add(txtNewPassword);
            y += 55;

            pnl.Controls.Add(new Label { Text = "تأكيد كلمة المرور الجديدة:", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtConfirmPassword = new TextBox { Location = new Point(20, y + 22), Width = 360, UseSystemPasswordChar = true };
            pnl.Controls.Add(txtConfirmPassword);
            y += 65;

            var flowButtons = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(360, 45),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(flowButtons);

            btnSave = new Button { Text = "حفظ وتحديث", Width = 150, Height = 36, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;
            flowButtons.Controls.Add(btnSave);

            if (!isForced)
            {
                btnCancel = new Button { Text = "إلغاء", Width = 110, Height = 36, Margin = new Padding(0, 0, 10, 0) };
                UITheme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                flowButtons.Controls.Add(btnCancel);
            }

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("يرجى ملء جميع الحقول.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass.Length < 6)
            {
                MessageBox.Show("كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != confirm)
            {
                MessageBox.Show("كلمة المرور الجديدة وتأكيدها غير متطابقين.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var res = _authService.ChangePassword(_userId, current, newPass);
            if (res.IsSuccess)
            {
                MessageBox.Show("تم تغيير كلمة المرور بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
