using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class PaymentDialog : Form
    {
        private readonly Appointment _appointment;
        private readonly PaymentService _paymentService;
        private readonly PrintService _printService;

        private Label lblPatientName;
        private Label lblFileTurn;
        private Label lblReExamNotice;

        private ComboBox cmbExamType;
        private NumericUpDown numPrice;
        private ComboBox cmbMethod;
        private TextBox txtReferenceNumber;
        private CheckBox chkPrintReceipt;
        private Button btnConfirm;
        private Button btnCancel;

        public Receipt GeneratedReceipt { get; private set; }

        public PaymentDialog(Appointment appointment)
        {
            _appointment = appointment;
            _paymentService = new PaymentService();
            _printService = new PrintService();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "تحصيل الكشف وإصدار الإيصال";
            this.Size = new Size(500, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(pnl);

            int y = 15;

            // Patient Card Banner
            var pnlHeader = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(440, 70),
                BackColor = Color.FromArgb(240, 249, 255),
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes,
                Padding = new Padding(12, 10, 12, 10)
            };
            pnl.Controls.Add(pnlHeader);

            lblPatientName = new Label
            {
                Text = "المريض: ...",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlHeader.Controls.Add(lblPatientName);

            lblFileTurn = new Label
            {
                Text = "رقم الملف: ... | رقم الدور: ...",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlHeader.Controls.Add(lblFileTurn);
            y += 85;

            // ReExam Notice
            lblReExamNotice = new Label
            {
                Text = "",
                ForeColor = UITheme.Success,
                Font = UITheme.BodyBoldFont,
                Location = new Point(20, y),
                Size = new Size(440, 22),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnl.Controls.Add(lblReExamNotice);
            y += 26;

            // Exam Type
            pnl.Controls.Add(new Label { Text = "نوع الكشف:", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            cmbExamType = new ComboBox
            {
                Location = new Point(20, y + 22),
                Width = 440,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.BodyFont
            };
            cmbExamType.Items.Add(new ComboItem("كشف عادي", ExamType.Exam));
            cmbExamType.Items.Add(new ComboItem("إعادة كشف", ExamType.ReExam));
            cmbExamType.Items.Add(new ComboItem("كشف مستعجل", ExamType.Urgent));
            cmbExamType.SelectedIndexChanged += CmbExamType_SelectedIndexChanged;
            pnl.Controls.Add(cmbExamType);
            y += 60;

            // Price
            pnl.Controls.Add(new Label { Text = "المبلغ المطلوب تحصيله (جنيه):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numPrice = new NumericUpDown
            {
                Location = new Point(20, y + 22),
                Width = 440,
                Maximum = 10000,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UITheme.BrandPrimary,
                TextAlign = HorizontalAlignment.Right,
                UpDownAlign = LeftRightAlignment.Left
            };
            pnl.Controls.Add(numPrice);
            y += 60;

            // Payment Method (Right)
            pnl.Controls.Add(new Label { Text = "طريقة الدفع:", Location = new Point(240, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            cmbMethod = new ComboBox
            {
                Location = new Point(240, y + 22),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.BodyFont
            };
            cmbMethod.Items.Add(new ComboItem("كاش (نقدي)", PaymentMethod.Cash));
            cmbMethod.Items.Add(new ComboItem("فودافون كاش", PaymentMethod.VodafoneCash));
            cmbMethod.Items.Add(new ComboItem("إنستا باي (InstaPay)", PaymentMethod.InstaPay));
            cmbMethod.SelectedIndex = 0;
            cmbMethod.SelectedIndexChanged += (s, e) =>
            {
                var sel = (ComboItem)cmbMethod.SelectedItem;
                bool isElec = (PaymentMethod)sel.Value != PaymentMethod.Cash;
                txtReferenceNumber.Enabled = isElec;
            };
            pnl.Controls.Add(cmbMethod);

            // Reference Number (Left)
            pnl.Controls.Add(new Label { Text = "رقم العملية المرجعي (اختياري):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyFont });
            txtReferenceNumber = new TextBox { Location = new Point(20, y + 22), Width = 200, Enabled = false, Font = UITheme.BodyFont, TextAlign = HorizontalAlignment.Right };
            pnl.Controls.Add(txtReferenceNumber);
            y += 60;

            // Print checkbox
            chkPrintReceipt = new CheckBox
            {
                Text = "طباعة الإيصال الحراري الآن",
                Location = new Point(20, y),
                AutoSize = true,
                Checked = true,
                Font = UITheme.BodyBoldFont
            };
            pnl.Controls.Add(chkPrintReceipt);
            y += 35;

            // Buttons
            var flowButtons = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(440, 48),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(flowButtons);

            btnConfirm = new Button { Text = "✓ تأكيد الدفع وإصدار الإيصال", Width = 230, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSuccessButton(btnConfirm);
            btnConfirm.Click += BtnConfirm_Click;
            flowButtons.Controls.Add(btnConfirm);

            btnCancel = new Button { Text = "إلغاء", Width = 110, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.Close();
            flowButtons.Controls.Add(btnCancel);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void LoadData()
        {
            lblPatientName.Text = $"المريض: {_appointment.Patient.FullName}";
            lblFileTurn.Text = $"رقم الملف: {_appointment.Patient.FileNumber}  |  رقم الدور: {_appointment.TurnNumber}";

            var elig = _paymentService.CheckReExamEligibility(_appointment.PatientId);
            if (elig.isEligible)
            {
                lblReExamNotice.Text = $"✓ المريض مستحق لإعادة كشف (آخر كشف مدفوع منذ {elig.daysSinceLast} يوماً)";
                // Select ReExam
                cmbExamType.SelectedIndex = 1;
            }
            else
            {
                if (_appointment.IsUrgent)
                    cmbExamType.SelectedIndex = 2; // Urgent
                else
                    cmbExamType.SelectedIndex = 0; // Exam
            }
        }

        private void CmbExamType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var pricing = _paymentService.GetPricingConfig();
            var sel = (ComboItem)cmbExamType.SelectedItem;
            var type = (ExamType)sel.Value;

            if (type == ExamType.Exam) numPrice.Value = pricing.examPrice;
            else if (type == ExamType.ReExam) numPrice.Value = pricing.reExamPrice;
            else numPrice.Value = pricing.urgentPrice;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            var examType = (ExamType)((ComboItem)cmbExamType.SelectedItem).Value;
            var method = (PaymentMethod)((ComboItem)cmbMethod.SelectedItem).Value;
            decimal price = numPrice.Value;
            string refNum = txtReferenceNumber.Text.Trim();

            var res = _paymentService.ProcessPayment(_appointment.Id, examType, price, method, refNum);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "خطأ في الدفع", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GeneratedReceipt = res.Value;

            if (chkPrintReceipt.Checked)
            {
                try
                {
                    using (var printDoc = _printService.CreateReceiptPrintDocument(GeneratedReceipt))
                    {
                        var printDlg = new PrintDialog { Document = printDoc };
                        if (printDlg.ShowDialog() == DialogResult.OK)
                        {
                            printDoc.Print();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"تم حفظ الدفعة بنجاح، ولكن تعذرت الطباعة:\n{ex.Message}", "تنبيه الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show($"تم تسجيل الدفع بنجاح برقم إيصال #{GeneratedReceipt.ReceiptNumber} ودخل المريض لقائمة الانتظار.", "تم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private class ComboItem
        {
            public string Text { get; }
            public object Value { get; }
            public ComboItem(string text, object value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
