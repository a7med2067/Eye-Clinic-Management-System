using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class EmergencyRecoveryDialog : Form
    {
        private TextBox txtRecoveryKey;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Label lblError;
        private Button btnRecover;
        private Button btnCancel;
        private readonly AuthService _authService;

        public EmergencyRecoveryDialog()
        {
            _authService = new AuthService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "استرداد الحساب بمفتاح الطوارئ الرئيسي - Doctor Recovery";
            this.Size = new Size(540, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlCard = new Panel
            {
                Size = new Size(480, 440),
                Location = new Point(25, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlCard);

            int y = 20;

            var lblTitle = new Label
            {
                Text = "استرداد حساب الطبيب / المدير (Master Recovery)",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(15, y),
                Size = new Size(450, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblTitle);
            y += 35;

            var lblSubtitle = new Label
            {
                Text = "أدخل مفتاح الاسترداد الرئيسي المدون في وثيقة الطوارئ التي قمت بحفظها لتعيين كلمة مرور جديدة فوراً دون الحاجة لإنترنت أو وسيط.",
                Font = UITheme.SmallFont,
                ForeColor = UITheme.TextMuted,
                Location = new Point(15, y),
                Size = new Size(450, 38),
                TextAlign = ContentAlignment.TopRight
            };
            pnlCard.Controls.Add(lblSubtitle);
            y += 45;

            // Recovery Key
            pnlCard.Controls.Add(new Label
            {
                Text = "مفتاح الاسترداد الرئيسي (Master Recovery Key):",
                Location = new Point(25, y),
                AutoSize = true,
                Font = UITheme.BodyBoldFont
            });
            txtRecoveryKey = new TextBox
            {
                Location = new Point(25, y + 24),
                Width = 430,
                Font = new Font("Consolas", 12f, FontStyle.Bold)
            };
            pnlCard.Controls.Add(txtRecoveryKey);
            y += 65;

            // New Password
            pnlCard.Controls.Add(new Label
            {
                Text = "كلمة المرور الجديدة (6 أحرف أو أرقام على الأقل):",
                Location = new Point(25, y),
                AutoSize = true,
                Font = UITheme.BodyBoldFont
            });
            txtNewPassword = new TextBox
            {
                Location = new Point(25, y + 24),
                Width = 430,
                Font = new Font("Segoe UI", 10.5f),
                UseSystemPasswordChar = true
            };
            pnlCard.Controls.Add(txtNewPassword);
            y += 60;

            // Confirm Password
            pnlCard.Controls.Add(new Label
            {
                Text = "تأكيد كلمة المرور الجديدة:",
                Location = new Point(25, y),
                AutoSize = true,
                Font = UITheme.BodyBoldFont
            });
            txtConfirmPassword = new TextBox
            {
                Location = new Point(25, y + 24),
                Width = 430,
                Font = new Font("Segoe UI", 10.5f),
                UseSystemPasswordChar = true
            };
            pnlCard.Controls.Add(txtConfirmPassword);
            y += 55;

            // Error Label
            lblError = new Label
            {
                Text = "",
                ForeColor = UITheme.Danger,
                Font = UITheme.SmallFont,
                Location = new Point(25, y),
                Size = new Size(430, 32)
            };
            pnlCard.Controls.Add(lblError);
            y += 35;

            // Buttons
            btnRecover = new Button
            {
                Text = "استعادة الحساب وتعيين كلمة المرور",
                Location = new Point(25, y),
                Width = 280,
                Height = 38
            };
            UITheme.StylePrimaryButton(btnRecover);
            btnRecover.Click += (s, e) => PerformRecovery();
            pnlCard.Controls.Add(btnRecover);

            btnCancel = new Button
            {
                Text = "إلغاء",
                Location = new Point(315, y),
                Width = 140,
                Height = 38
            };
            UITheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.Close();
            pnlCard.Controls.Add(btnCancel);
            y += 45;

            var lblSignature = new Label
            {
                Text = "By Eng.Ahmed Adel  |  01116887242",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UITheme.BrandAccent,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(430, 20),
                Location = new Point(25, y)
            };
            pnlCard.Controls.Add(lblSignature);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void PerformRecovery()
        {
            lblError.Text = "";
            string key = txtRecoveryKey.Text.Trim();
            string pass1 = txtNewPassword.Text;
            string pass2 = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(key))
            {
                lblError.Text = "يرجى إدخال مفتاح الاسترداد الرئيسي.";
                return;
            }

            if (string.IsNullOrWhiteSpace(pass1) || pass1.Length < 6)
            {
                lblError.Text = "يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.";
                return;
            }

            if (pass1 != pass2)
            {
                lblError.Text = "كلمتا المرور غير متطابقتين.";
                return;
            }

            var res = _authService.RecoverDoctorPassword(key, pass1);
            if (!res.IsSuccess)
            {
                lblError.Text = res.ErrorMessage;
                return;
            }

            // Successfully recovered! Generate replacement recovery key and prompt doctor to save it
            string replacementKey = AuthService.GenerateRecoveryKey();
            using (var keyDlg = new MasterRecoveryKeyDialog(replacementKey, isFirstTime: false))
            {
                keyDlg.ShowDialog();
            }

            MessageBox.Show(
                "تمت استعادة حساب الطبيب بنجاح وتعيين كلمة المرور الجديدة ومفتاح الاسترداد البديل!\nيمكنك الآن تسجيل الدخول مباشرة بكلمة المرور الجديدة.",
                "نجاح استعادة الحساب",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
