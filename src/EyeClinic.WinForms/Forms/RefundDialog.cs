using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.DTOs;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class RefundDialog : Form
    {
        private readonly int _paymentId;
        private readonly string _patientName;
        private readonly decimal _amount;
        private readonly PaymentService _paymentService;

        private TextBox txtReason;
        private Button btnConfirm;
        private Button btnCancel;

        public RefundDialog(int paymentId, string patientName, decimal amount)
        {
            _paymentId = paymentId;
            _patientName = patientName;
            _amount = amount;
            _paymentService = new PaymentService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "إلغاء واسترداد الدفعة";
            this.Size = new Size(460, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(pnl);

            int y = 15;
            pnl.Controls.Add(new Label
            {
                Text = "تأكيد استرداد كشف وإلغاء الحجز:",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.Danger,
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 35;

            var lblDetails = new Label
            {
                Text = $"المريض: {_patientName}\nالمبلغ المسترد: {_amount:N2} جنيه (استرداد كامل)",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextDark,
                Location = new Point(20, y),
                Size = new Size(400, 42),
                TextAlign = ContentAlignment.TopRight
            };
            pnl.Controls.Add(lblDetails);
            y += 50;

            pnl.Controls.Add(new Label { Text = "سبب الاسترداد (إجباري ولا يمكن المتابعة بدونه):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.Danger });
            txtReason = new TextBox { Location = new Point(20, y + 22), Width = 400, Font = UITheme.BodyFont, TextAlign = HorizontalAlignment.Right };
            pnl.Controls.Add(txtReason);
            y += 65;

            var flowButtons = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(400, 45),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(flowButtons);

            btnConfirm = new Button { Text = "تأكيد الاسترداد والإلغاء", Width = 180, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleDangerButton(btnConfirm);
            btnConfirm.Click += BtnConfirm_Click;
            flowButtons.Controls.Add(btnConfirm);

            btnCancel = new Button { Text = "تراجع", Width = 110, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.Close();
            flowButtons.Controls.Add(btnCancel);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string reason = txtReason.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("يرجى كتابة سبب الاسترداد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReason.Focus();
                return;
            }

            var dr = MessageBox.Show($"هل أنت متأكد من استرداد مبلغ {_amount:N2} جنيه للمريض {_patientName} وإلغاء الحجز؟", "تأكيد نهائي", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) return;

            var res = _paymentService.RefundPayment(_paymentId, reason);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "خطأ في الاسترداد", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("تم الاسترداد بنجاح وتسجيل الحركة في سجل المراجعة.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
