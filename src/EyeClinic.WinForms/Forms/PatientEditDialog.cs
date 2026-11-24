using System;
using System.Drawing;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.Entities;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class PatientEditDialog : Form
    {
        private TextBox txtFileNumber;
        private TextBox txtFullName;
        private TextBox txtMobile;
        private DateTimePicker dtpBirthDate;
        private Label lblAgeCalc;
        private ComboBox cmbGender;
        private TextBox txtNationalId;
        private ComboBox cmbAddress;
        private TextBox txtGeneralNotes;

        // Confidential Doctor Fields
        private TextBox txtChronic;
        private TextBox txtAllergies;
        private TextBox txtSurgeries;

        private Button btnSave;
        private Button btnCancel;

        // Duplicate Warning Panel
        private Panel pnlDuplicates;
        private DataGridView dgvDuplicates;
        private Label lblDupWarning;

        private readonly PatientService _patientService;
        private readonly CatalogService _catalogService;
        private Patient _patient;
        private bool _isEditMode;

        public Patient SavedPatient { get; private set; }

        public PatientEditDialog() : this(null)
        {
        }

        public PatientEditDialog(Patient patient)
        {
            _patientService = new PatientService();
            _catalogService = new CatalogService();
            _patient = patient ?? new Patient();
            _isEditMode = (patient != null && patient.Id > 0);

            InitializeComponent(_isEditMode);
            LoadPatientData();
        }

        private void InitializeComponent(bool isEdit)
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = isEdit ? "تعديل بيانات مريض" : "تسجيل مريض جديد";
            this.Size = new Size(650, SessionContext.IsDoctor ? 720 : 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(pnl);

            int y = 15;
            pnl.Controls.Add(new Label
            {
                Text = isEdit ? "تعديل الملف الطبي والبيانات الأساسية:" : "تسجيل ملف مريض جديد:",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(20, y),
                AutoSize = true
            });
            y += 35;

            // Row 1: Full Name (Right) & File Number (Left)
            pnl.Controls.Add(new Label { Text = "اسم المريض بالكامل (إجباري):", Location = new Point(180, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtFullName = new TextBox { Location = new Point(180, y + 22), Width = 420 };
            txtFullName.TextChanged += (s, e) => CheckRealtimeDuplicates();
            pnl.Controls.Add(txtFullName);

            pnl.Controls.Add(new Label { Text = "رقم الملف (تلقائي):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtFileNumber = new TextBox { Location = new Point(20, y + 22), Width = 140, ReadOnly = true, BackColor = Color.FromArgb(245, 245, 245) };
            pnl.Controls.Add(txtFileNumber);
            y += 55;

            // Row 2: Mobile (Right) & Gender (Left)
            pnl.Controls.Add(new Label { Text = "رقم الموبايل (إجباري):", Location = new Point(320, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtMobile = new TextBox { Location = new Point(320, y + 22), Width = 280 };
            txtMobile.TextChanged += (s, e) => CheckRealtimeDuplicates();
            pnl.Controls.Add(txtMobile);

            pnl.Controls.Add(new Label { Text = "النوع:", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            cmbGender = new ComboBox { Location = new Point(20, y + 22), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGender.Items.AddRange(new object[] { "ذكر", "أنثى" });
            cmbGender.SelectedIndex = 0;
            pnl.Controls.Add(cmbGender);
            y += 55;

            // Floating Duplicate Suggestion Box
            pnlDuplicates = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(580, 115),
                BackColor = Color.FromArgb(254, 243, 199), // Soft amber
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(pnlDuplicates);

            lblDupWarning = new Label
            {
                Text = "⚠ تنبيه: تم العثور على مرضى مسجلين سابقاً ببيانات مشابهة (انقر مرتين للتحميل):",
                Location = new Point(10, 6),
                AutoSize = true,
                Font = UITheme.SmallFont,
                ForeColor = UITheme.Warning
            };
            pnlDuplicates.Controls.Add(lblDupWarning);

            dgvDuplicates = new DataGridView
            {
                Location = new Point(10, 26),
                Size = new Size(560, 80),
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RightToLeft = RightToLeft.Yes
            };
            dgvDuplicates.Columns.Add("Id", "Id");
            dgvDuplicates.Columns["Id"].Visible = false;
            dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "رقم الملف", FillWeight = 60 });
            dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "اسم المريض", FillWeight = 160 });
            dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mobile", HeaderText = "الموبايل", FillWeight = 90 });
            dgvDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "السن", FillWeight = 40 });

            dgvDuplicates.CellDoubleClick += (s, e) =>
            {
                if (dgvDuplicates.SelectedRows.Count > 0)
                {
                    int selId = Convert.ToInt32(dgvDuplicates.SelectedRows[0].Cells["Id"].Value);
                    var exist = _patientService.GetPatientById(selId);
                    if (exist != null)
                    {
                        var dr = MessageBox.Show(string.Format("هل ترغب في فتح وتعديل ملف المريض ({0}) بدلاً من إضافة ملف جديد؟", exist.FullName), "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.Yes)
                        {
                            _patient = exist;
                            _isEditMode = true;
                            this.Text = "تعديل بيانات مريض";
                            pnlDuplicates.Visible = false;
                            LoadPatientData();
                        }
                    }
                }
            };
            pnlDuplicates.Controls.Add(dgvDuplicates);

            // Row 3: BirthDate (Right) & Age (Left)
            pnl.Controls.Add(new Label { Text = "تاريخ الميلاد (لحساب السن):", Location = new Point(320, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            dtpBirthDate = new DateTimePicker
            {
                Location = new Point(320, y + 22),
                Width = 280,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                RightToLeftLayout = true
            };
            dtpBirthDate.ValueChanged += DtpBirthDate_ValueChanged;
            pnl.Controls.Add(dtpBirthDate);

            lblAgeCalc = new Label { Text = "السن: غير محدد", Location = new Point(20, y + 24), Size = new Size(280, 24), ForeColor = UITheme.BrandAccent, Font = UITheme.BodyBoldFont, TextAlign = ContentAlignment.MiddleRight };
            pnl.Controls.Add(lblAgeCalc);
            y += 55;

            // Row 4: Address (Right) & National ID (Left)
            pnl.Controls.Add(new Label { Text = "العنوان (اختر من القائمة أو اكتب جديداً ليُحفظ تلقائياً):", Location = new Point(320, y), AutoSize = true, Font = UITheme.BodyFont });
            cmbAddress = new ComboBox
            {
                Location = new Point(320, y + 22),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            LoadAddressSuggestions();
            pnl.Controls.Add(cmbAddress);

            pnl.Controls.Add(new Label { Text = "الرقم القومي (اختياري):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyFont });
            txtNationalId = new TextBox { Location = new Point(20, y + 22), Width = 280 };
            pnl.Controls.Add(txtNationalId);
            y += 55;

            // Row 5: General Notes (Full Width)
            pnl.Controls.Add(new Label { Text = "ملاحظات عامة (إدارية):", Location = new Point(20, y), AutoSize = true, Font = UITheme.BodyFont });
            txtGeneralNotes = new TextBox { Location = new Point(20, y + 22), Width = 580 };
            pnl.Controls.Add(txtGeneralNotes);
            y += 55;

            // Doctor confidential section
            if (SessionContext.IsDoctor)
            {
                var grpMedical = new GroupBox
                {
                    Text = " التاريخ الطبي السري (يراه الطبيب فقط) ",
                    Location = new Point(20, y),
                    Size = new Size(580, 150),
                    Font = UITheme.BodyBoldFont,
                    ForeColor = UITheme.BrandPrimary,
                    RightToLeft = RightToLeft.Yes
                };
                pnl.Controls.Add(grpMedical);

                grpMedical.Controls.Add(new Label { Text = "أمراض مزمنة (سكر، ضغط، كبد، قلب...):", Location = new Point(20, 25), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextDark });
                txtChronic = new TextBox { Location = new Point(20, 45), Width = 540, Font = UITheme.BodyFont };
                grpMedical.Controls.Add(txtChronic);

                grpMedical.Controls.Add(new Label { Text = "حساسية من أدوية محددة:", Location = new Point(300, 75), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.Danger });
                txtAllergies = new TextBox { Location = new Point(300, 95), Width = 260, Font = UITheme.BodyFont };
                grpMedical.Controls.Add(txtAllergies);

                grpMedical.Controls.Add(new Label { Text = "عمليات جراحية سابقة:", Location = new Point(20, 75), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextDark });
                txtSurgeries = new TextBox { Location = new Point(20, 95), Width = 260, Font = UITheme.BodyFont };
                grpMedical.Controls.Add(txtSurgeries);

                y += 160;
            }

            y += 10;
            var flowButtons = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                Size = new Size(580, 48),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(flowButtons);

            btnSave = new Button { Text = "حفظ البيانات", Width = 150, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;
            flowButtons.Controls.Add(btnSave);

            btnCancel = new Button { Text = "إلغاء", Width = 110, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.Close();
            flowButtons.Controls.Add(btnCancel);

            UITheme.ApplyRtlToHierarchy(this);
        }

        private void LoadAddressSuggestions()
        {
            cmbAddress.Items.Clear();
            var list = _catalogService.GetDistinctAddresses();
            foreach (var a in list)
            {
                cmbAddress.Items.Add(a);
            }
        }

        private void CheckRealtimeDuplicates()
        {
            if (_isEditMode) return; // Don't annoy user during edit of known patient

            string name = txtFullName.Text.Trim();
            string mobile = txtMobile.Text.Trim();

            if (name.Length < 3 && mobile.Length < 4)
            {
                pnlDuplicates.Visible = false;
                return;
            }

            var matches = _patientService.FindDuplicateMatches(name, mobile, _patient.Id);
            if (matches.Count > 0)
            {
                dgvDuplicates.Rows.Clear();
                foreach (var m in matches)
                {
                    string ageStr = m.Age.HasValue ? m.Age.Value.ToString() : "-";
                    dgvDuplicates.Rows.Add(m.Id, m.FileNumber, m.FullName, m.Mobile, ageStr);
                }
                pnlDuplicates.BringToFront();
                pnlDuplicates.Visible = true;
            }
            else
            {
                pnlDuplicates.Visible = false;
            }
        }

        private void DtpBirthDate_ValueChanged(object sender, EventArgs e)
        {
            if (dtpBirthDate.Checked)
            {
                var today = DateTime.Today;
                var age = today.Year - dtpBirthDate.Value.Year;
                if (dtpBirthDate.Value.Date > today.AddYears(-age)) age--;
                if (age < 0) age = 0;
                lblAgeCalc.Text = string.Format("السن: {0} سنة", age);
            }
            else
            {
                lblAgeCalc.Text = "السن: غير محدد";
            }
        }

        private void LoadPatientData()
        {
            if (_patient.Id > 0)
            {
                txtFileNumber.Text = _patient.FileNumber;
                txtFullName.Text = _patient.FullName;
                txtMobile.Text = _patient.Mobile;
                cmbGender.SelectedItem = _patient.Gender ?? "ذكر";
                txtNationalId.Text = _patient.NationalId ?? "";
                cmbAddress.Text = _patient.Address ?? "";
                txtGeneralNotes.Text = _patient.GeneralNotes ?? "";

                if (_patient.BirthDate.HasValue)
                {
                    dtpBirthDate.Value = _patient.BirthDate.Value;
                    dtpBirthDate.Checked = true;
                    DtpBirthDate_ValueChanged(null, null);
                }

                if (SessionContext.IsDoctor)
                {
                    txtChronic.Text = _patient.ChronicDiseases ?? "";
                    txtAllergies.Text = _patient.DrugAllergies ?? "";
                    txtSurgeries.Text = _patient.PastSurgeries ?? "";
                }
            }
            else
            {
                txtFileNumber.Text = _patientService.GenerateNextFileNumber();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();
            string mobile = txtMobile.Text.Trim();
            string address = cmbAddress.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("يرجى إدخال اسم المريض.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(mobile))
            {
                MessageBox.Show("يرجى إدخال رقم الموبايل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMobile.Focus();
                return;
            }

            // Check duplicate
            var dupCheck = _patientService.CheckDuplicate(name, mobile, _patient.Id);
            if (dupCheck.hasDuplicate)
            {
                string msg = string.Format("تنبيه: تم العثور على مريض مسجل مسبقاً ({0}):\n\nالاسم: {1}\nالموبايل: {2}\nرقم الملف: {3}\n\nهل ترغب في الاستمرار في حفظ هذا المريض كملف جديد؟",
                    dupCheck.matchReason,
                    dupCheck.existingPatient.FullName,
                    dupCheck.existingPatient.Mobile,
                    dupCheck.existingPatient.FileNumber);

                var dr = MessageBox.Show(msg, "تطابق في البيانات", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr != DialogResult.Yes) return;
            }

            _patient.FullName = name;
            _patient.Mobile = mobile;
            _patient.Gender = cmbGender.SelectedItem != null ? cmbGender.SelectedItem.ToString() : "ذكر";
            _patient.BirthDate = dtpBirthDate.Checked ? (DateTime?)dtpBirthDate.Value.Date : null;
            _patient.NationalId = txtNationalId.Text.Trim();
            _patient.Address = address;
            _patient.GeneralNotes = txtGeneralNotes.Text.Trim();

            if (SessionContext.IsDoctor)
            {
                _patient.ChronicDiseases = txtChronic.Text.Trim();
                _patient.DrugAllergies = txtAllergies.Text.Trim();
                _patient.PastSurgeries = txtSurgeries.Text.Trim();
            }

            // Auto-save address for future fast reuse
            if (!string.IsNullOrWhiteSpace(address))
            {
                _catalogService.SaveAddressIfNew(address);
            }

            if (_patient.Id == 0)
            {
                _patient.FileNumber = txtFileNumber.Text.Trim();
                var res = _patientService.RegisterPatient(_patient);
                if (!res.IsSuccess)
                {
                    MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                SavedPatient = res.Value;
            }
            else
            {
                var res = _patientService.UpdatePatient(_patient);
                if (!res.IsSuccess)
                {
                    MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                SavedPatient = _patient;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
