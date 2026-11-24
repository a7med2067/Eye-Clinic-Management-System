using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnConfig;
        private Label lblError;
        private readonly AuthService _authService;

        public User AuthenticatedUser { get; private set; }

        public LoginForm()
        {
            _authService = new AuthService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "تسجيل الدخول - نظام إدارة عيادة العيون";
            this.Size = new Size(460, 565);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            var pnlCard = new Panel
            {
                Size = new Size(390, 485),
                Location = new Point(35, 18),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlCard);

            int y = 18;

            // Logo / Header
            var lblTitle = new Label
            {
                Text = "عيادة العيون التخصصية",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.BrandPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(370, 32),
                Location = new Point(10, y)
            };
            pnlCard.Controls.Add(lblTitle);
            y += 35;

            var lblSubtitle = new Label
            {
                Text = "تسجيل دخول المستخدمين",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(370, 22),
                Location = new Point(10, y)
            };
            pnlCard.Controls.Add(lblSubtitle);
            y += 42;

            // Username
            pnlCard.Controls.Add(new Label { Text = "اسم المستخدم:", Location = new Point(30, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtUsername = new TextBox { Location = new Point(30, y + 24), Width = 328, Font = new Font("Segoe UI", 10.5f) };
            txtUsername.Text = "";
            pnlCard.Controls.Add(txtUsername);
            y += 62;

            // Password
            pnlCard.Controls.Add(new Label { Text = "كلمة المرور:", Location = new Point(30, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtPassword = new TextBox { Location = new Point(30, y + 24), Width = 328, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10.5f) };
            txtPassword.Text = "";
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformLogin(); };
            pnlCard.Controls.Add(txtPassword);
            y += 62;

            // Error Label
            lblError = new Label
            {
                Text = "",
                ForeColor = UITheme.Danger,
                Font = UITheme.SmallFont,
                Location = new Point(30, y),
                Size = new Size(328, 30)
            };
            pnlCard.Controls.Add(lblError);
            y += 32;

            // Login Button
            btnLogin = new Button
            {
                Text = "تسجيل الدخول",
                Location = new Point(30, y),
                Width = 328,
                Height = 38
            };
            UITheme.StylePrimaryButton(btnLogin);
            btnLogin.Click += (s, e) => PerformLogin();
            pnlCard.Controls.Add(btnLogin);
            y += 44;

            // Forgot Password / Emergency Recovery Button
            var btnForgotPass = new Button
            {
                Text = "استرداد كلمة المرور بمفتاح الطوارئ",
                Location = new Point(30, y),
                Width = 328,
                Height = 32
            };
            UITheme.StyleSecondaryButton(btnForgotPass);
            btnForgotPass.Click += (s, e) =>
            {
                using (var recDlg = new EmergencyRecoveryDialog())
                {
                    if (recDlg.ShowDialog() == DialogResult.OK)
                    {
                        txtUsername.Text = "admin";
                        txtPassword.Clear();
                        txtPassword.Focus();
                    }
                }
            };
            pnlCard.Controls.Add(btnForgotPass);
            y += 38;

            // Server Config Button
            btnConfig = new Button
            {
                Text = "⚙ إعدادات الاتصال بالسيرفر",
                Location = new Point(30, y),
                Width = 328,
                Height = 30
            };
            UITheme.StyleSecondaryButton(btnConfig);
            btnConfig.Click += (s, e) =>
            {
                using (var setup = new ConnectionSetupForm())
                {
                    setup.ShowDialog();
                }
            };
            pnlCard.Controls.Add(btnConfig);
            y += 38;

            var lblSignature = new Label
            {
                Text = "By Eng.Ahmed Adel  |  01116887242",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.BrandAccent,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(328, 24),
                Location = new Point(30, y)
            };
            pnlCard.Controls.Add(lblSignature);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void PerformLogin()
        {
            lblError.Text = "";
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            var result = _authService.Login(username, password);
            if (!result.IsSuccess)
            {
                lblError.Text = result.ErrorMessage;
                return;
            }

            var user = result.Value;
            AuthenticatedUser = user;

            // Check if user must change password
            if (user.MustChangePassword)
            {
                using (var chg = new ChangePasswordDialog(user.Id, isForced: true))
                {
                    if (chg.ShowDialog() != DialogResult.OK)
                    {
                        lblError.Text = "يجب تغيير كلمة المرور للمتابعة.";
                        _authService.Logout();
                        return;
                    }
                }
            }

            // Check if Doctor needs Master Recovery Key generation (First-time / One-time)
            if (user.Role == UserRole.Doctor && _authService.NeedsRecoveryKeyPrompt())
            {
                string key = AuthService.GenerateRecoveryKey();
                using (var keyDlg = new MasterRecoveryKeyDialog(key, isFirstTime: true))
                {
                    keyDlg.ShowDialog();
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
