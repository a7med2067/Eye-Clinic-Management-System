using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class PatientProfileDialog : Form
    {
        private readonly int _patientId;
        private readonly PatientService _patientService;
        private readonly PrintService _printService;
        private readonly QueueService _queueService;

        private PatientProfileDto _profile;

        // UI Controls
        private Label lblFileBadge;
        private Label lblPatientName;
        private Label lblPatientDetails;
        private Label lblStatsSummary;
        private Panel pnlMedicalAlert;
        private Label lblMedicalAlert;

        private TabControl tabProfile;

        // Tab 1: Visits & Prescriptions
        private DataGridView dgvVisits;
        private Label lblVisitMeasurements;
        private TextBox txtVisitNotes;
        private DataGridView dgvVisitMedicines;
        private Button btnReprintRx;

        // Tab 2: All Prescribed Medicines
        private DataGridView dgvAllMedicines;

        // Tab 3: Financial History
        private DataGridView dgvPayments;

        public PatientProfileDialog(int patientId)
        {
            _patientId = patientId;
            _patientService = new PatientService();
            _printService = new PrintService();
            _queueService = new QueueService();

            InitializeComponent();
            LoadProfileData();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "الملف الطبي الشامل للمريض - بروفايل المريض";
            this.Size = new Size(960, 680);
            this.MinimumSize = new Size(880, 580);
            this.StartPosition = FormStartPosition.CenterParent;

            // 1. Top Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15),
                RightToLeft = RightToLeft.Yes
            };
            this.Controls.Add(pnlHeader);

            var flowHeaderTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(flowHeaderTop);

            lblFileBadge = new Label
            {
                Text = "ملف # ...",
                Font = UITheme.HeaderFont,
                BackColor = UITheme.BrandPrimary,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(130, 38),
                Margin = new Padding(0, 0, 12, 0)
            };
            flowHeaderTop.Controls.Add(lblFileBadge);

            lblPatientName = new Label
            {
                Text = "...",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 6, 20, 0)
            };
            flowHeaderTop.Controls.Add(lblPatientName);

            lblStatsSummary = new Label
            {
                Text = "إجمالي الزيارات: 0  |  إجمالي المدفوعات: 0 ج",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandAccent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(20, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblStatsSummary);

            var flowHeaderDetails = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };
            pnlHeader.Controls.Add(flowHeaderDetails);

            lblPatientDetails = new Label
            {
                Text = "السن: -  |  الموبايل: -  |  العنوان: -  |  الرقم القومي: -",
                Font = UITheme.BodyFont,
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            flowHeaderDetails.Controls.Add(lblPatientDetails);

            // Medical Alert Strip
            pnlMedicalAlert = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(254, 226, 226), // Light Red
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                RightToLeft = RightToLeft.Yes,
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlHeader.Controls.Add(pnlMedicalAlert);

            lblMedicalAlert = new Label
            {
                Text = "⚠ تنبيه طبي: ...",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Danger,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlMedicalAlert.Controls.Add(lblMedicalAlert);

            // 2. Bottom Actions Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10),
                RightToLeft = RightToLeft.Yes
            };
            this.Controls.Add(pnlBottom);

            var flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlBottom.Controls.Add(flowBottom);

            var btnEdit = new Button { Text = "✏ تعديل بيانات المريض", Size = new Size(160, 38), Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnEdit);
            btnEdit.Click += (s, e) =>
            {
                if (_profile != null && _profile.Patient != null)
                {
                    using (var dlg = new PatientEditDialog(_profile.Patient))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            LoadProfileData();
                        }
                    }
                }
            };
            flowBottom.Controls.Add(btnEdit);

            var btnBookToday = new Button { Text = "📅 حجز دور جديد اليوم", Size = new Size(160, 38), Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSuccessButton(btnBookToday);
            btnBookToday.Click += (s, e) =>
            {
                var res = _queueService.BookAppointment(_patientId, DateTime.Today);
                if (res.IsSuccess)
                {
                    MessageBox.Show("تم حجز دور للمريض في قائمة انتظار اليوم بنجاح.", "حجز ناجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(res.ErrorMessage, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            flowBottom.Controls.Add(btnBookToday);

            if (SessionContext.IsDoctor)
            {
                var btnArchive = new Button { Text = "📦 أرشفة المريض", Size = new Size(140, 38), Margin = new Padding(0, 0, 10, 0) };
                UITheme.StyleDangerButton(btnArchive);
                btnArchive.Click += (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        string.Format("هل أنت متأكد من أرشفة وإخفاء المريض ({0})؟\n\nتنبيه: ستبقى جميع كشوفاته ومدفوعاته في السجلات ولكن سيتم إخفاؤه من قوائم البحث العادية.", _profile?.Patient?.FullName),
                        "تأكيد أرشفة المريض",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        var res = _patientService.ArchivePatient(_patientId);
                        if (res.IsSuccess)
                        {
                            MessageBox.Show("تمت أرشفة المريض بنجاح.", "تمت الأرشفة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };
                flowBottom.Controls.Add(btnArchive);
            }

            var btnClose = new Button { Text = "إغلاق", Size = new Size(100, 38), Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => this.Close();
            flowBottom.Controls.Add(btnClose);

            var lblSig = new Label
            {
                Text = "By Eng.Ahmed Adel  |  01116887242",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UITheme.BrandAccent,
                AutoSize = true,
                Margin = new Padding(20, 10, 0, 0)
            };
            flowBottom.Controls.Add(lblSig);

            // 3. TabControl for 360 profile
            tabProfile = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = UITheme.BodyBoldFont,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };
            this.Controls.Add(tabProfile);
            tabProfile.BringToFront();

            BuildVisitsTab();
            BuildAllMedicationsTab();
            BuildPaymentsTab();

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void BuildVisitsTab()
        {
            var tab = new TabPage { Text = "  سجل الكشوفات والروشتات  ", BackColor = UITheme.Background };
            tabProfile.TabPages.Add(tab);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210,
                RightToLeft = RightToLeft.Yes
            };
            tab.Controls.Add(split);

            // Top: Visits Grid
            dgvVisits = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGridView(dgvVisits);
            dgvVisits.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvVisits.Columns.Add("VisitId", "Id");
            dgvVisits.Columns["VisitId"].Visible = false;
            dgvVisits.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "تاريخ الكشف", FillWeight = 30 });
            dgvVisits.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "التشخيص الطبي", FillWeight = 45 });
            dgvVisits.Columns.Add(new DataGridViewTextBoxColumn { Name = "Complaint", HeaderText = "الشكوى", FillWeight = 40 });
            dgvVisits.Columns.Add(new DataGridViewTextBoxColumn { Name = "FollowUp", HeaderText = "موعد المتابعة", FillWeight = 25 });
            dgvVisits.SelectionChanged += DgvVisits_SelectionChanged;
            split.Panel1.Controls.Add(dgvVisits);

            // Bottom: Visit Detail Panel
            var pnlDetail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15), AutoScroll = true, RightToLeft = RightToLeft.Yes };
            split.Panel2.Controls.Add(pnlDetail);

            lblVisitMeasurements = new Label
            {
                Text = "قياسات العين: -",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(430, 10),
                AutoSize = true
            };
            pnlDetail.Controls.Add(lblVisitMeasurements);

            btnReprintRx = new Button
            {
                Text = "🖨 إعادة طباعة روشتة هذا الكشف",
                Size = new Size(220, 32),
                Location = new Point(15, 8)
            };
            UITheme.StylePrimaryButton(btnReprintRx);
            btnReprintRx.Click += BtnReprintRx_Click;
            pnlDetail.Controls.Add(btnReprintRx);

            var lblMeds = new Label { Text = "الأدوية المقررة في هذا الكشف:", Location = new Point(430, 38), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted };
            pnlDetail.Controls.Add(lblMeds);

            dgvVisitMedicines = new DataGridView
            {
                Location = new Point(430, 58),
                Size = new Size(460, 110),
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            UITheme.StyleDataGridView(dgvVisitMedicines);
            dgvVisitMedicines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvVisitMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedName", HeaderText = "اسم الدواء", FillWeight = 50 });
            dgvVisitMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dose", HeaderText = "الجرعة وطريقة الاستعمال", FillWeight = 50 });
            pnlDetail.Controls.Add(dgvVisitMedicines);

            var lblNotes = new Label { Text = "ملاحظات الطبيب السرية:", Location = new Point(15, 38), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted };
            pnlDetail.Controls.Add(lblNotes);

            txtVisitNotes = new TextBox
            {
                Location = new Point(15, 58),
                Size = new Size(400, 110),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = UITheme.BodyFont
            };
            pnlDetail.Controls.Add(txtVisitNotes);
        }

        private void BuildAllMedicationsTab()
        {
            var tab = new TabPage { Text = "  مجمع الأدوية المصروفة  ", BackColor = UITheme.Background };
            tabProfile.TabPages.Add(tab);

            dgvAllMedicines = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };
            UITheme.StyleDataGridView(dgvAllMedicines);
            dgvAllMedicines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tab.Controls.Add(dgvAllMedicines);

            dgvAllMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "تاريخ الوصف", FillWeight = 25 });
            dgvAllMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedName", HeaderText = "اسم الدواء", FillWeight = 35 });
            dgvAllMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dose", HeaderText = "الجرعة وطريقة الاستعمال", FillWeight = 40 });
            dgvAllMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "التشخيص المصاحب", FillWeight = 35 });
        }

        private void BuildPaymentsTab()
        {
            var tab = new TabPage { Text = "  سجل المدفوعات والإيصالات  ", BackColor = UITheme.Background };
            tabProfile.TabPages.Add(tab);

            var pnlPayBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };
            tab.Controls.Add(pnlPayBottom);

            var btnReprintReceipt = new Button
            {
                Text = "🖨 إعادة طباعة الإيصال الحراري المحدد",
                Width = 240,
                Height = 34,
                Dock = DockStyle.Right
            };
            UITheme.StylePrimaryButton(btnReprintReceipt);
            btnReprintReceipt.Click += BtnReprintReceipt_Click;
            pnlPayBottom.Controls.Add(btnReprintReceipt);

            dgvPayments = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };
            UITheme.StyleDataGridView(dgvPayments);
            dgvPayments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tab.Controls.Add(dgvPayments);
            dgvPayments.BringToFront();

            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentId", HeaderText = "Id", Visible = false });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "التاريخ والوقت", FillWeight = 25 });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExamType", HeaderText = "نوع الكشف", FillWeight = 20 });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ المدفوع", FillWeight = 20 });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "طريقة الدفع", FillWeight = 20 });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Receipt", HeaderText = "رقم الإيصال", FillWeight = 20 });
            dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "حالة الدفعة", FillWeight = 25 });
        }

        private void LoadProfileData()
        {
            _profile = _patientService.GetPatientProfile(_patientId);
            if (_profile == null || _profile.Patient == null)
            {
                MessageBox.Show("تعذر تحميل ملف المريض.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            var p = _profile.Patient;
            lblFileBadge.Text = string.Format("ملف # {0}", p.FileNumber);
            lblPatientName.Text = p.FullName;

            int age = p.CalculatedAge ?? 0;
            lblPatientDetails.Text = string.Format("السن: {0} سنة  |  النوع: {1}  |  الموبايل: {2}  |  العنوان: {3}  |  الرقم القومي: {4}",
                age > 0 ? age.ToString() : "-",
                p.Gender ?? "-",
                p.Mobile ?? "-",
                !string.IsNullOrWhiteSpace(p.Address) ? p.Address : "غير مسجل",
                !string.IsNullOrWhiteSpace(p.NationalId) ? p.NationalId : "-");

            lblStatsSummary.Text = string.Format("إجمالي الزيارات: {0}  |  إجمالي المدفوعات: {1:N0} ج", _profile.TotalVisits, _profile.TotalSpent);

            // Medical Alert Banner
            bool hasAlert = !string.IsNullOrWhiteSpace(p.ChronicDiseases) || !string.IsNullOrWhiteSpace(p.DrugAllergies) || !string.IsNullOrWhiteSpace(p.PastSurgeries);
            pnlMedicalAlert.Visible = hasAlert;
            if (hasAlert)
            {
                lblMedicalAlert.Text = string.Format("⚠ تنبيه طبي: أمراض مزمنة: {0}  |  حساسية أدوية: {1}  |  عمليات سابقة: {2}",
                    !string.IsNullOrWhiteSpace(p.ChronicDiseases) ? p.ChronicDiseases : "لا يوجد",
                    !string.IsNullOrWhiteSpace(p.DrugAllergies) ? p.DrugAllergies : "لا يوجد",
                    !string.IsNullOrWhiteSpace(p.PastSurgeries) ? p.PastSurgeries : "لا يوجد");
            }

            // Load Visits Grid
            dgvVisits.Rows.Clear();
            foreach (var v in _profile.Visits)
            {
                string followUp = v.FollowUpDate.HasValue ? v.FollowUpDate.Value.ToString("dd/MM/yyyy") : "-";
                dgvVisits.Rows.Add(v.VisitId, v.VisitDate.ToString("dd/MM/yyyy hh:mm tt"), v.Diagnosis, v.ChiefComplaint, followUp);
            }

            // Load All Medicines Grid
            dgvAllMedicines.Rows.Clear();
            foreach (var m in _profile.AllMedications)
            {
                dgvAllMedicines.Rows.Add(m.PrescribedDate.ToString("dd/MM/yyyy"), m.MedicineName, m.DoseInstructions, m.Diagnosis);
            }

            // Load Payments Grid
            dgvPayments.Rows.Clear();
            foreach (var pay in _profile.Payments)
            {
                string statusText = pay.IsRefunded ? string.Format("مسترد ({0})", pay.RefundReason) : "مكتمل";
                dgvPayments.Rows.Add(pay.PaymentId, pay.Date.ToString("dd/MM/yyyy hh:mm tt"), pay.ExamType.ToString(), string.Format("{0:N0} ج", pay.Amount), pay.Method.ToString(), pay.ReceiptNumber, statusText);
            }

            if (dgvVisits.Rows.Count > 0)
            {
                dgvVisits.Rows[0].Selected = true;
                DgvVisits_SelectionChanged(null, null);
            }
        }

        private void DgvVisits_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvVisits.SelectedRows.Count == 0 || _profile == null) return;

            int visitId = Convert.ToInt32(dgvVisits.SelectedRows[0].Cells["VisitId"].Value);
            var v = _profile.Visits.FirstOrDefault(x => x.VisitId == visitId);
            if (v == null) return;

            lblVisitMeasurements.Text = string.Format("قياسات العين: اليمنى (OD): حدة: {0} | ضغط: {1}  ---  اليسرى (OS): حدة: {2} | ضغط: {3}",
                !string.IsNullOrWhiteSpace(v.VA_OD) ? v.VA_OD : "-",
                !string.IsNullOrWhiteSpace(v.IOP_OD) ? v.IOP_OD : "-",
                !string.IsNullOrWhiteSpace(v.VA_OS) ? v.VA_OS : "-",
                !string.IsNullOrWhiteSpace(v.IOP_OS) ? v.IOP_OS : "-");

            txtVisitNotes.Text = v.DoctorNotes ?? "";

            dgvVisitMedicines.Rows.Clear();
            foreach (var m in v.Medicines)
            {
                dgvVisitMedicines.Rows.Add(m.MedicineName, m.DoseInstructions);
            }
        }

        private void BtnReprintRx_Click(object sender, EventArgs e)
        {
            if (dgvVisits.SelectedRows.Count == 0 || _profile == null) return;

            int visitId = Convert.ToInt32(dgvVisits.SelectedRows[0].Cells["VisitId"].Value);
            var v = _profile.Visits.FirstOrDefault(x => x.VisitId == visitId);
            if (v == null || v.Medicines.Count == 0)
            {
                MessageBox.Show("لا توجد أدوية مقررة في هذا الكشف للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printDto = new PrescriptionPrintDto
            {
                VisitId = v.VisitId,
                PatientName = _profile.Patient.FullName,
                Age = _profile.Patient.CalculatedAge.HasValue ? _profile.Patient.CalculatedAge.Value.ToString() : "",
                Diagnosis = v.Diagnosis,
                Date = v.VisitDate,
                Medicines = v.Medicines
            };

            try
            {
                using (var doc = _printService.CreatePrescriptionPrintDocument(printDto))
                {
                    var printDlg = new PrintDialog { Document = doc };
                    if (printDlg.ShowDialog() == DialogResult.OK)
                    {
                        doc.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("تعذرت الطباعة:\n{0}", ex.Message), "خطأ الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnReprintReceipt_Click(object sender, EventArgs e)
        {
            if (dgvPayments.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار دفعة من الجدول أولاً لإعادة طباعة إيصالها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int paymentId = Convert.ToInt32(dgvPayments.SelectedRows[0].Cells["PaymentId"].Value);
            using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
            {
                var receipt = db.Receipts.FirstOrDefault(r => r.PaymentId == paymentId);
                if (receipt == null)
                {
                    MessageBox.Show("لم يتم العثور على إيصال مسجل لهذه الدفعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (var printDoc = _printService.CreateReceiptPrintDocument(receipt))
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
                    MessageBox.Show(string.Format("تعذرت إعادة طباعة الإيصال:\n{0}", ex.Message), "خطأ الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
