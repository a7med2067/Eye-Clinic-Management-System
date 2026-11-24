using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class MasterRecoveryKeyDialog : Form
    {
        private readonly string _recoveryKey;
        private readonly bool _isFirstTime;
        private readonly AuthService _authService;
        private CheckBox chkConfirm;
        private Button btnProceed;

        public MasterRecoveryKeyDialog(string recoveryKey, bool isFirstTime = true)
        {
            _recoveryKey = recoveryKey;
            _isFirstTime = isFirstTime;
            _authService = new AuthService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "مفتاح الاسترداد الرئيسي للطوارئ - Master Recovery Key";
            this.Size = new Size(620, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var pnlCard = new Panel
            {
                Size = new Size(560, 480),
                Location = new Point(25, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlCard);

            int y = 20;

            var lblTitle = new Label
            {
                Text = "مفتاح الاسترداد الرئيسي لحساب الطبيب (Master Recovery Key)",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(20, y),
                Size = new Size(520, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblTitle);
            y += 38;

            var pnlWarning = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(520, 80),
                BackColor = Color.FromArgb(254, 242, 242), // Light red
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlCard.Controls.Add(pnlWarning);

            var lblWarning = new Label
            {
                Text = "⚠ تنبيه أمني فائق الأهمية (يظهر مرة واحدة فقط):\n" +
                       "هذا المفتاح مخصص للطوارئ فقط لاستعادة حساب الطبيب في حال نسيان كلمة المرور دون الحاجة لأي اتصال بالإنترنت أو إيميل أو وسيط خارجي.\n" +
                       "يرجى طباعته أو كتابته وحفظه في مكان آمن وخزينة خارج جهاز الكمبيوتر.",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(153, 27, 27), // Dark red
                Location = new Point(10, 8),
                Size = new Size(500, 64),
                TextAlign = ContentAlignment.TopRight
            };
            pnlWarning.Controls.Add(lblWarning);
            y += 92;

            // Key display container
            var pnlKeyBox = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(520, 60),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlCard.Controls.Add(pnlKeyBox);

            var txtKey = new TextBox
            {
                Text = _recoveryKey,
                ReadOnly = true,
                Font = new Font("Consolas", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.None,
                Location = new Point(10, 18),
                Size = new Size(495, 28),
                TextAlign = HorizontalAlignment.Center
            };
            pnlKeyBox.Controls.Add(txtKey);
            y += 72;

            // Action Buttons: Copy, Print, Save File
            var flowActions = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(520, 42),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes
            };
            pnlCard.Controls.Add(flowActions);

            var btnCopy = new Button
            {
                Text = "نسخ المفتاح",
                Width = 125,
                Height = 35,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(_recoveryKey);
                    MessageBox.Show("تم نسخ مفتاح الاسترداد إلى الحافظة بنجاح.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("تعذر النسخ للحافظة: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            flowActions.Controls.Add(btnCopy);

            var btnPrint = new Button
            {
                Text = "طباعة الوثيقة",
                Width = 145,
                Height = 35,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StylePrimaryButton(btnPrint);
            btnPrint.Click += (s, e) => PrintRecoveryDocument();
            flowActions.Controls.Add(btnPrint);

            var btnSaveFile = new Button
            {
                Text = "حفظ كملف نصي",
                Width = 155,
                Height = 35,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSuccessButton(btnSaveFile);
            btnSaveFile.Click += (s, e) => SaveToFile();
            flowActions.Controls.Add(btnSaveFile);
            y += 55;

            // Signature badge
            var lblSignature = new Label
            {
                Text = "Eye Clinic Management System  |  By Eng.Ahmed Adel  |  01116887242",
                Font = UITheme.SmallFont,
                ForeColor = UITheme.TextMuted,
                Location = new Point(20, y),
                Size = new Size(520, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCard.Controls.Add(lblSignature);
            y += 28;

            // Checkbox
            chkConfirm = new CheckBox
            {
                Text = "أقر بأنني قمت بطباعة أو كتابة هذا المفتاح وسأحتفظ به في مكان آمن وخزينة خارج هذا الكمبيوتر.",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextDark,
                Location = new Point(20, y),
                Size = new Size(520, 40),
                AutoSize = false
            };
            chkConfirm.CheckedChanged += (s, e) =>
            {
                btnProceed.Enabled = chkConfirm.Checked;
            };
            pnlCard.Controls.Add(chkConfirm);
            y += 50;

            // Proceed Button
            btnProceed = new Button
            {
                Text = "متابعة الدخول إلى النظام",
                Location = new Point(140, y),
                Width = 280,
                Height = 42,
                Enabled = false
            };
            UITheme.StylePrimaryButton(btnProceed);
            btnProceed.Click += (s, e) =>
            {
                var saveRes = _authService.SaveMasterRecoveryKey(_recoveryKey);
                if (!saveRes.IsSuccess)
                {
                    MessageBox.Show(saveRes.ErrorMessage, "خطأ في حفظ المفتاح", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlCard.Controls.Add(btnProceed);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void PrintRecoveryDocument()
        {
            try
            {
                var pd = new PrintDocument();
                pd.DocumentName = "EyeClinic_Master_Recovery_Key";
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics;
                    var titleFont = new Font("Arial", 16f, FontStyle.Bold);
                    var warnFont = new Font("Arial", 11f, FontStyle.Bold);
                    var bodyFont = new Font("Arial", 11f, FontStyle.Regular);
                    var keyFont = new Font("Courier New", 18f, FontStyle.Bold);
                    var subFont = new Font("Arial", 9.5f, FontStyle.Italic);

                    float py = 50;

                    // Header
                    var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString("عيادة العيون التخصصية - وثيقة مفتاح الاسترداد الرئيسي للطوارئ", titleFont, Brushes.Navy, new RectangleF(50, py, 700, 35), sfCenter);
                    py += 40;
                    g.DrawString("CONFIDENTIAL & STRICTLY RESTRICTED - MASTER RECOVERY DOCUMENT", warnFont, Brushes.DarkRed, new RectangleF(50, py, 700, 25), sfCenter);
                    py += 35;

                    g.DrawLine(Pens.Black, 50, py, 750, py);
                    py += 25;

                    // Instructions
                    string instructions = "تنبيه هام للطبيب:\n" +
                        "هذه الوثيقة تحتوي على مفتاح الاسترداد المشفر الوحيد لحساب الطبيب / المدير في نظام إدارة عيادة العيون.\n" +
                        "في حال نسيان كلمة المرور، يمكنك من شاشة تسجيل الدخول الضغط على 'استرداد الطوارئ بمفتاح الأمان'\n" +
                        "وكتابة هذا المفتاح لتعيين كلمة مرور جديدة فوراً دون الحاجة لإنترنت أو وسيط خارجي.\n" +
                        "يجب حفظ هذه الورقة في خزينة أو مكان آمن محمي خارج جهاز الكمبيوتر.";

                    var sfRight = new StringFormat { FormatFlags = StringFormatFlags.DirectionRightToLeft };
                    g.DrawString(instructions, bodyFont, Brushes.Black, new RectangleF(50, py, 700, 100), sfRight);
                    py += 115;

                    // Recovery Key Box
                    g.FillRectangle(new SolidBrush(Color.FromArgb(245, 247, 250)), 70, py, 660, 65);
                    g.DrawRectangle(new Pen(Color.FromArgb(51, 65, 85), 2), 70, py, 660, 65);
                    g.DrawString(_recoveryKey, keyFont, Brushes.DarkBlue, new RectangleF(70, py + 18, 660, 35), sfCenter);
                    py += 85;

                    // Metadata
                    string meta = string.Format("تاريخ ووقت إصدار الوثيقة: {0}  |  صلاحية المفتاح: مفتاح رسمي نشط حتى أول استخدام", DateTime.Now.ToString("dd/MM/yyyy  hh:mm tt"));
                    g.DrawString(meta, bodyFont, Brushes.DarkSlateGray, new RectangleF(50, py, 700, 25), sfCenter);
                    py += 60;

                    // Signatures
                    g.DrawString("توقيع الطبيب المستلم: .......................................", bodyFont, Brushes.Black, 50, py);
                    g.DrawString("اعتماد العيادة: .......................................", bodyFont, Brushes.Black, 480, py);
                    py += 60;

                    g.DrawLine(Pens.Gray, 50, py, 750, py);
                    py += 15;
                    g.DrawString("نظام إدارة عيادة العيون (مكتبي - Offline 100%)  |  برمجة م. أحمد عادل  |  01116887242", subFont, Brushes.Gray, new RectangleF(50, py, 700, 25), sfCenter);
                };

                using (var dlg = new PrintDialog())
                {
                    dlg.Document = pd;
                    dlg.UseEXDialog = true;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        pd.Print();
                        MessageBox.Show("تم إرسال وثيقة مفتاح الاسترداد إلى الطابعة بنجاح.", "تمت الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الطباعة: " + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveToFile()
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "حفظ مفتاح الاسترداد في ملف خارجي (فلاشة / وسيط تخزين)";
                    sfd.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                    sfd.FileName = "EyeClinic_Doctor_Recovery_Key_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("==================================================================");
                        sb.AppendLine("     عيادة العيون التخصصية - وثيقة مفتاح الاسترداد الرئيسي للطوارئ      ");
                        sb.AppendLine("==================================================================");
                        sb.AppendLine();
                        sb.AppendLine("تاريخ الإصدار: " + DateTime.Now.ToString("dd/MM/yyyy  hh:mm:ss tt"));
                        sb.AppendLine();
                        sb.AppendLine("مفتاح الاسترداد الرئيسي (Master Recovery Key):");
                        sb.AppendLine("------------------------------------------------------------------");
                        sb.AppendLine("   " + _recoveryKey);
                        sb.AppendLine("------------------------------------------------------------------");
                        sb.AppendLine();
                        sb.AppendLine("تعليمات هامة:");
                        sb.AppendLine("1. احتفظ بهذا الملف على فلاشة USB أو وسيط تخزين آمن خارج جهاز الكمبيوتر.");
                        sb.AppendLine("2. في حال نسيان كلمة مرور الطبيب، افتح شاشة تسجيل الدخول واضغط على");
                        sb.AppendLine("   'استرداد الطوارئ بمفتاح الأمان' وأدخل المفتاح أعلاه لتعيين كلمة مرور جديدة.");
                        sb.AppendLine("3. لا تشارك هذا المفتاح مع أي شخص غير مصرح له.");
                        sb.AppendLine();
                        sb.AppendLine("نظام إدارة عيادة العيون | By Eng.Ahmed Adel | 01116887242");

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("تم حفظ مفتاح الاسترداد في الملف بنجاح:\n" + sfd.FileName, "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر حفظ الملف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
