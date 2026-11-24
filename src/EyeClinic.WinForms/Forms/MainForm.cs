using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.Core.Entities;
using EyeClinic.Core.Enums;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class MainForm : Form
    {
        // Services
        private readonly QueueService _queueService;
        private readonly PatientService _patientService;
        private readonly PaymentService _paymentService;
        private readonly VisitService _visitService;
        private readonly PrintService _printService;
        private readonly DailyClosingService _closingService;
        private readonly SettingsService _settingsService;
        private readonly AuditService _auditService;
        private readonly CatalogService _catalogService;
        private readonly BackupService _backupService;
        private readonly ExcelExportService _exportService;
        private readonly AuthService _authService;

        // Timers
        private Timer _queueRefreshTimer;
        private Timer _inactivityTimer;

        // UI Controls & Right Sidebar
        private TabControl tabMain;
        private Label lblHeaderUser;
        private Label lblHeaderDate;
        private Label lblHeaderClinic;
        private Label lblHeaderDevSig;
        private Panel pnlSidebar;
        private Panel pnlNavList;
        private Panel pnlDevSig;
        private Button btnSidebarBackup;
        private int _navButtonTop = 8;
        private readonly List<Tuple<TabPage, Button>> _navButtons = new List<Tuple<TabPage, Button>>();

        // Tab 1: Queue Controls
        private DataGridView dgvQueue;
        private TextBox txtSearchQueue;
        private DateTimePicker dtpBookingDate;
        private DataGridView dgvSearchResults;
        private Button btnCallExam;
        private Button btnPay;
        private Button btnRefund;
        private Button btnNoShow;
        private Button btnNewPatient;

        // Tab 2: Doctor Consultation Controls
        private TabPage tabDoctorExam;
        private SplitContainer splitDoctorExam;
        private Label lblActiveExamPatient;
        private Panel pnlAllergiesAlert;
        private Label lblAllergiesText;
        private TextBox txtComplaint;
        private TextBox txtDiagnosis;
        private TextBox txtVA_OD;
        private TextBox txtVA_OS;
        private TextBox txtIOP_OD;
        private TextBox txtIOP_OS;
        private TextBox txtDoctorNotes;
        private DateTimePicker dtpFollowUp;
        private DataGridView dgvMedicines;
        private Button btnRepeatRx;
        private Button btnSavePrintRx;
        private Button btnFinishExam;
        private DataGridView dgvHistory;
        private TextBox txtNewMedName;
        private ComboBox cboNewMedFrequency;
        private ComboBox cboNewMedDuration;
        private ComboBox cboNewMedEye;
        private Button btnAddMedItem;
        private Button btnClearRx;
        private int _currentExamPatientId = 0;
        private int _currentExamAppointmentId = 0;

        // Tab 3: Patient Directory
        private DataGridView dgvPatients;
        private TextBox txtSearchPatients;

        // Tab: Daily Archive
        private TabPage tabDayArchive;
        private DateTimePicker dtpArchiveDate;
        private Label lblArchiveCounts;
        private Label lblArchiveRevenue;
        private Label lblArchiveRefunds;
        private Label lblArchiveClosing;
        private DataGridView dgvArchivePatients;
        private DataGridView dgvArchivePrescriptions;

        // Tab 4: Daily Closing
        private Label lblExpectedCash;
        private Label lblVodafone;
        private Label lblInstaPay;
        private Label lblCountsSummary;
        private NumericUpDown numActualCash;
        private Label lblDifference;
        private Button btnCloseDay;
        private Button btnReopenDay;
        private Label lblClosingStatus;

        // Tab 5: Settings & Calibration
        private TabPage tabSettings;
        private TextBox txtClinicName;
        private TextBox txtDoctorName;
        private TextBox txtPhone;
        private TextBox txtAddress;
        private NumericUpDown numPriceExam;
        private NumericUpDown numPriceReExam;
        private NumericUpDown numPriceUrgent;
        private NumericUpDown numReExamDays;
        private NumericUpDown numOffsetX;
        private NumericUpDown numOffsetY;
        private Button btnTestPrint;
        private Button btnSaveSettings;
        private Label lblBackupPath;
        private DataGridView dgvRecentBackups;
        private TextBox txtSecFullName;
        private TextBox txtSecUsername;
        private TextBox txtNewSecPass;
        private Button btnSaveSecAccount;
        private TextBox txtDocCurrentPass;
        private TextBox txtDocNewPass;
        private Button btnChangeDocPass;

        // Tab 6: Audit Log
        private TabPage tabAudit;
        private DataGridView dgvAudit;
        private DateTimePicker dtpAuditFrom;
        private DateTimePicker dtpAuditTo;

        public MainForm()
        {
            _queueService = new QueueService();
            _patientService = new PatientService();
            _paymentService = new PaymentService();
            _visitService = new VisitService();
            _printService = new PrintService();
            _closingService = new DailyClosingService();
            _settingsService = new SettingsService();
            _auditService = new AuditService();
            _catalogService = new CatalogService();
            _backupService = new BackupService(_settingsService);
            _exportService = new ExcelExportService();
            _authService = new AuthService();

            InitializeComponent();
            SetupTimers();
            LoadHeaderInfo();
            RefreshQueue();

            this.Shown += (s, e) =>
            {
                if (_backupService.IsWeeklyFlashReminderDue())
                {
                    var ask = MessageBox.Show(
                        "تنبيه أسبوعي هام (حماية بيانات العيادة):\n" +
                        "لم يتم أخذ نسخة احتياطية على فلاشة خارجية USB منذ أكثر من أسبوع.\n\n" +
                        "هل ترغب في حفظ نسخة احتياطية على فلاشة الآن لحماية بياناتك؟",
                        "تذكير النسخ الاحتياطي الأسبوعي",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (ask == DialogResult.Yes)
                    {
                        if (tabSettings != null) tabMain.SelectedTab = tabSettings;
                        BtnUsbBackup_Click(this, EventArgs.Empty);
                    }
                }
            };
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "نظام إدارة عيادة العيون - Eye Clinic Management System";
            this.Size = new Size(1150, 750);
            this.MinimumSize = new Size(1000, 650);

            // 1. Top Header Bar
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = UITheme.BrandDark
            };
            this.Controls.Add(pnlHeader);

            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 12, 15, 12),
                RightToLeft = RightToLeft.Yes
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlHeader.Controls.Add(tblHeader);

            lblHeaderClinic = new Label
            {
                Text = "عيادة العيون التخصصية",
                Font = UITheme.HeaderFont,
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            tblHeader.Controls.Add(lblHeaderClinic, 0, 0);

            lblHeaderUser = new Label
            {
                Text = string.Format("المستخدم: {0} ({1})", SessionContext.CurrentUser != null ? SessionContext.CurrentUser.FullName : "", SessionContext.CurrentUser != null ? SessionContext.CurrentUser.Role.ToString() : ""),
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandAccent,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            tblHeader.Controls.Add(lblHeaderUser, 1, 0);

            lblHeaderDate = new Label
            {
                Text = string.Format("التاريخ: {0:dd/MM/yyyy}", DateTime.Today),
                Font = UITheme.BodyFont,
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            tblHeader.Controls.Add(lblHeaderDate, 2, 0);

            lblHeaderDevSig = new Label
            {
                Text = "By Eng.Ahmed Adel | 01116887242",
                Font = UITheme.BodyBoldFont,
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            tblHeader.Controls.Add(lblHeaderDevSig, 3, 0);

            var btnLogout = new Button
            {
                Text = "تسجيل خروج",
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            UITheme.StyleDangerButton(btnLogout);
            btnLogout.Click += (s, e) => Logout();
            tblHeader.Controls.Add(btnLogout, 4, 0);

            // 2. Right Sidebar (DockStyle.Left in RTL mirrored form docks on the visual right)
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10, 12, 10, 12)
            };

            // Developer Signature & Quick Backup wrapper at bottom of sidebar
            var pnlBottomWrap = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 118,
                BackColor = Color.Transparent
            };

            btnSidebarBackup = new Button
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "نسخ احتياطي سريع",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Success,
                ForeColor = Color.White,
                Font = UITheme.BodyBoldFont,
                Cursor = Cursors.Hand
            };
            btnSidebarBackup.FlatAppearance.BorderSize = 0;
            btnSidebarBackup.Click += BtnSidebarBackup_Click;
            pnlBottomWrap.Controls.Add(btnSidebarBackup);

            pnlDevSig = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(6)
            };
            var lblDev1 = new Label
            {
                Text = "By Eng.Ahmed Adel",
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblDev2 = new Label
            {
                Text = "01116887242",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlDevSig.Controls.Add(lblDev1);
            pnlDevSig.Controls.Add(lblDev2);
            pnlBottomWrap.Controls.Add(pnlDevSig);

            pnlNavList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            pnlSidebar.Controls.Add(pnlNavList);
            pnlSidebar.Controls.Add(pnlBottomWrap);

            // 3. Main TabControl (Hidden Top Headers, driven exclusively by Right Sidebar)
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed,
                Font = UITheme.BodyBoldFont,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };
            tabMain.SelectedIndexChanged += (s, e) =>
            {
                UpdateNavSelection();
                if (tabMain.SelectedTab == tabDoctorExam && splitDoctorExam != null && splitDoctorExam.Width > 500)
                {
                    try { splitDoctorExam.SplitterDistance = splitDoctorExam.Width / 2; } catch { }
                }
            };

            this.Controls.Add(tabMain);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlHeader);

            // Build Individual Tabs and Add Right Sidebar Navigation Buttons
            var tabQueue = BuildQueueTab();
            var btnQueue = AddNavButton("قائمة الانتظار والحجز", tabQueue);

            var tabExam = BuildDoctorExamTab();
            var btnDoctorExam = AddNavButton("كشف الطبيب والروشتة", tabExam);

            var tabPatients = BuildPatientsTab();
            var btnPatients = AddNavButton("دليل وسجلات المرضى", tabPatients);

            var tabArchive = BuildDayArchiveTab();
            var btnDayArchive = AddNavButton("أرشيف وسجل اليوميات", tabArchive);

            var tabClosing = BuildDailyClosingTab();
            var btnDailyClosing = AddNavButton("التقفيل المالي اليومي", tabClosing);

            var tabSettingsTab = BuildSettingsTab();
            var btnSettings = AddNavButton("إعدادات النظام والطباعة", tabSettingsTab);

            var tabAuditTab = BuildAuditTab();
            var btnAudit = AddNavButton("سجل العمليات والأمان", tabAuditTab);

            // Role-based visibility
            if (!SessionContext.IsDoctor)
            {
                // Secretary cannot access doctor's examination room, archive, settings, or audit log
                tabMain.TabPages.Remove(tabDoctorExam);
                tabMain.TabPages.Remove(tabDayArchive);
                tabMain.TabPages.Remove(tabSettings);
                tabMain.TabPages.Remove(tabAudit);

                RemoveNavButton(btnDoctorExam);
                RemoveNavButton(btnDayArchive);
                RemoveNavButton(btnSettings);
                RemoveNavButton(btnAudit);
            }

            UpdateNavSelection();
            UITheme.ApplyRtlToHierarchy(this);
        }

        private Button AddNavButton(string title, TabPage targetTab)
        {
            var btn = new Button
            {
                Text = title,
                Location = new Point(5, _navButtonTop),
                Size = new Size(200, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                tabMain.SelectedTab = targetTab;
            };

            _navButtons.Add(new Tuple<TabPage, Button>(targetTab, btn));
            pnlNavList.Controls.Add(btn);
            _navButtonTop += 52;
            return btn;
        }

        private void RemoveNavButton(Button btn)
        {
            if (btn == null) return;
            pnlNavList.Controls.Remove(btn);
            _navButtons.RemoveAll(item => item.Item2 == btn);
            btn.Dispose();

            // Re-stack remaining buttons
            int top = 8;
            foreach (var item in _navButtons)
            {
                item.Item2.Location = new Point(5, top);
                top += 52;
            }
            _navButtonTop = top;
        }

        private void UpdateNavSelection()
        {
            foreach (var item in _navButtons)
            {
                bool isSelected = (tabMain.SelectedTab == item.Item1);
                item.Item2.BackColor = isSelected ? UITheme.BrandAccent : Color.FromArgb(30, 41, 59);
                item.Item2.ForeColor = isSelected ? Color.White : Color.FromArgb(226, 232, 240);
            }
        }

        private void BtnSidebarBackup_Click(object sender, EventArgs e)
        {
            var res = _backupService.CreateBackup();
            if (res.IsSuccess)
            {
                LoadBackupList();
                MessageBox.Show(
                    string.Format("تم إنشاء نسخة احتياطية جديدة بنجاح في {0:N1} ثانية.\n\nاسم الملف: {1}\nالمسار: {2}",
                        res.Duration.TotalSeconds, res.FileName, res.BackupFilePath),
                    "نجاح النسخ الاحتياطي السريع",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                MessageBox.Show("حدث خطأ أثناء أخذ النسخة الاحتياطية:\n" + res.ErrorMessage, "خطأ في النسخ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Tab 1: Queue & Quick Booking

        private TabPage BuildQueueTab()
        {
            var tab = new TabPage { Text = "  قائمة الانتظار والحجز اليومي  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tab);

            // 1. Top Search & Fast Action Panel
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10),
                RightToLeft = RightToLeft.Yes
            };
            tab.Controls.Add(pnlTop);

            var flowTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent,
                WrapContents = true
            };
            pnlTop.Controls.Add(flowTop);

            var lblSearch = new Label
            {
                Text = "بحث سريع عن مريض (بالاسم أو الموبايل أو رقم الملف):",
                AutoSize = true,
                Font = UITheme.BodyBoldFont,
                Margin = new Padding(0, 0, 0, 8)
            };
            flowTop.Controls.Add(lblSearch);
            flowTop.SetFlowBreak(lblSearch, true);

            txtSearchQueue = new TextBox
            {
                Width = 260,
                Font = new Font("Segoe UI", 10.5f),
                TextAlign = HorizontalAlignment.Right,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0, 0, 10, 0)
            };
            txtSearchQueue.TextChanged += TxtSearchQueue_TextChanged;
            flowTop.Controls.Add(txtSearchQueue);

            btnNewPatient = new Button
            {
                Text = "+ تسجيل مريض جديد",
                Width = 150,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0)
            };
            UITheme.StylePrimaryButton(btnNewPatient);
            btnNewPatient.Click += BtnNewPatient_Click;
            flowTop.Controls.Add(btnNewPatient);

            var btnQuickBook = new Button
            {
                Text = "حجز دور للمريض المحدد",
                Width = 160,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSecondaryButton(btnQuickBook);
            btnQuickBook.Click += BtnQuickBook_Click;
            flowTop.Controls.Add(btnQuickBook);

            var lblBookDate = new Label
            {
                Text = "تاريخ الحجز:",
                AutoSize = true,
                Font = UITheme.BodyBoldFont,
                Margin = new Padding(8, 8, 4, 0)
            };
            flowTop.Controls.Add(lblBookDate);

            dtpBookingDate = new DateTimePicker
            {
                Width = 120,
                Font = UITheme.BodyBoldFont,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                MinDate = DateTime.Today,
                MaxDate = DateTime.Today.AddDays(3),
                Margin = new Padding(0, 3, 0, 0)
            };
            dtpBookingDate.ValueChanged += (s, e) => RefreshQueue();
            flowTop.Controls.Add(dtpBookingDate);

            // 2. Bottom Actions Toolbar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(12, 12, 12, 12),
                RightToLeft = RightToLeft.Yes
            };
            tab.Controls.Add(pnlBottom);

            var flowBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                WrapContents = false,
                AutoScroll = false
            };
            pnlBottom.Controls.Add(flowBottom);

            btnCallExam = new Button { Text = "◄ استدعاء للكشف", Size = new Size(150, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSuccessButton(btnCallExam);
            btnCallExam.Click += BtnCallExam_Click;
            flowBottom.Controls.Add(btnCallExam);

            btnPay = new Button { Text = "تحصيل وطباعة الإيصال", Size = new Size(175, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StylePrimaryButton(btnPay);
            btnPay.Click += BtnPay_Click;
            flowBottom.Controls.Add(btnPay);

            btnRefund = new Button { Text = "إلغاء واسترداد", Size = new Size(115, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleDangerButton(btnRefund);
            btnRefund.Click += BtnRefund_Click;
            flowBottom.Controls.Add(btnRefund);

            btnNoShow = new Button { Text = "✕ تخطي / غائب", Size = new Size(125, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSecondaryButton(btnNoShow);
            btnNoShow.Click += BtnNoShow_Click;
            flowBottom.Controls.Add(btnNoShow);

            var btnRefresh = new Button { Text = "⟳ تحديث", Size = new Size(95, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSecondaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => RefreshQueue();
            flowBottom.Controls.Add(btnRefresh);

            var btnExportQueue = new Button { Text = "📊 تصدير Excel", Size = new Size(120, 38), Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSecondaryButton(btnExportQueue);
            btnExportQueue.Click += (s, e) => ExportGrid(dgvQueue, "طابور_الانتظار_اليومي");
            flowBottom.Controls.Add(btnExportQueue);

            // 3. Center: Waiting Queue Grid
            dgvQueue = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGridView(dgvQueue);
            dgvQueue.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tab.Controls.Add(dgvQueue);
            dgvQueue.BringToFront(); // Crucial: sets z-order so dgvQueue is between Top and Bottom panels!

            dgvQueue.Columns.Add("AppointmentId", "Id");
            dgvQueue.Columns["AppointmentId"].Visible = false;

            var colTurn = new DataGridViewTextBoxColumn { Name = "TurnNumber", HeaderText = "الدور", FillWeight = 45 };
            var colFile = new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "الملف", FillWeight = 60 };
            var colName = new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "اسم المريض", FillWeight = 160 };
            var colAge = new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "السن", FillWeight = 40 };
            var colType = new DataGridViewTextBoxColumn { Name = "ExamType", HeaderText = "نوع الكشف", FillWeight = 70 };
            var colAmount = new DataGridViewTextBoxColumn { Name = "AmountPaid", HeaderText = "المدفوع", FillWeight = 65 };
            var colMethod = new DataGridViewTextBoxColumn { Name = "PaymentMethod", HeaderText = "طريقة الدفع", FillWeight = 70 };
            var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "حالة المريض", FillWeight = 85 };
            var colUrgent = new DataGridViewTextBoxColumn { Name = "Urgent", HeaderText = "مستعجل", FillWeight = 55 };

            dgvQueue.Columns.AddRange(new DataGridViewColumn[] { colTurn, colFile, colName, colAge, colType, colAmount, colMethod, colStatus, colUrgent });
            dgvQueue.SelectionChanged += DgvQueue_SelectionChanged;

            // 4. Instant Search Results Floating Dropdown Grid
            dgvSearchResults = new DataGridView
            {
                Size = new Size(725, 200),
                Visible = false
            };
            UITheme.StyleDataGridView(dgvSearchResults);
            dgvSearchResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSearchResults.Columns.Add("Id", "Id");
            dgvSearchResults.Columns["Id"].Visible = false;

            var sFile = new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "رقم الملف", FillWeight = 70 };
            var sName = new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "اسم المريض", FillWeight = 180 };
            var sMobile = new DataGridViewTextBoxColumn { Name = "Mobile", HeaderText = "الموبايل", FillWeight = 90 };
            var sElig = new DataGridViewTextBoxColumn { Name = "Eligibility", HeaderText = "حالة الإعادة", FillWeight = 120 };
            dgvSearchResults.Columns.AddRange(new DataGridViewColumn[] { sFile, sName, sMobile, sElig });

            dgvSearchResults.CellDoubleClick += (s, e) =>
            {
                dgvSearchResults.Visible = false;
                BtnQuickBook_Click(s, e);
            };
            tab.Controls.Add(dgvSearchResults);
            dgvSearchResults.BringToFront();
            return tab;
        }

        private void TxtSearchQueue_TextChanged(object sender, EventArgs e)
        {
            string term = txtSearchQueue.Text.Trim();
            if (term.Length >= 2)
            {
                var list = _patientService.SearchPatients(term, 8);
                dgvSearchResults.Rows.Clear();
                foreach (var p in list)
                {
                    string elig = p.IsEligibleForReExam ? string.Format("✓ مستحق إعادة (منذ {0} يوماً)", p.DaysSinceLastExam) : "كشف جديد";
                    dgvSearchResults.Rows.Add(p.Id, p.FileNumber, p.FullName, p.Mobile, elig);
                }

                if (list.Count > 0)
                {
                    var parentTab = tabMain.TabPages[0];
                    var screenPt = txtSearchQueue.Parent.PointToScreen(new Point(txtSearchQueue.Right, txtSearchQueue.Bottom + 2));
                    var tabPt = parentTab.PointToClient(screenPt);
                    int desiredX = tabPt.X - dgvSearchResults.Width;
                    if (desiredX < 15) desiredX = 15;
                    dgvSearchResults.Location = new Point(desiredX, tabPt.Y);
                    dgvSearchResults.BringToFront();
                    dgvSearchResults.Visible = true;
                }
                else
                {
                    dgvSearchResults.Visible = false;
                }
            }
            else
            {
                dgvSearchResults.Visible = false;
            }
        }

        private void BtnNewPatient_Click(object sender, EventArgs e)
        {
            using (var dlg = new PatientEditDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK && dlg.SavedPatient != null)
                {
                    // Directly offer to book for this patient on selected date
                    DateTime targetDate = dtpBookingDate != null ? dtpBookingDate.Value.Date : DateTime.Today;
                    var bookRes = _queueService.BookAppointment(dlg.SavedPatient.Id, targetDate);
                    if (bookRes.IsSuccess)
                    {
                        RefreshQueue();
                        // Prompt payment
                        using (var payDlg = new PaymentDialog(bookRes.Value))
                        {
                            payDlg.ShowDialog();
                            RefreshQueue();
                        }
                    }
                }
            }
        }

        private void BtnQuickBook_Click(object sender, EventArgs e)
        {
            int patientId = 0;
            if (dgvSearchResults.Visible && dgvSearchResults.SelectedRows.Count > 0)
            {
                patientId = Convert.ToInt32(dgvSearchResults.SelectedRows[0].Cells["Id"].Value);
            }
            else if (!string.IsNullOrWhiteSpace(txtSearchQueue.Text))
            {
                var results = _patientService.SearchPatients(txtSearchQueue.Text.Trim(), 1);
                if (results.Count > 0) patientId = results[0].Id;
            }

            if (patientId == 0)
            {
                MessageBox.Show("يرجى البحث واختيار المريض أولاً لحجزه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime targetDate = dtpBookingDate != null ? dtpBookingDate.Value.Date : DateTime.Today;
            var res = _queueService.BookAppointment(patientId, targetDate);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "تنبيه الحجز", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dgvSearchResults.Visible = false;
            txtSearchQueue.Clear();
            RefreshQueue();

            // Ask to pay now
            var dr = MessageBox.Show("تم حجز الدور بنجاح. هل ترغب في تسجيل التحصيل والدفع الآن للدخول لقائمة الانتظار؟", "حجز ناجح", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                using (var payDlg = new PaymentDialog(res.Value))
                {
                    payDlg.ShowDialog();
                    RefreshQueue();
                }
            }
        }

        private void RefreshQueue()
        {
            try
            {
                DateTime targetDate = dtpBookingDate != null ? dtpBookingDate.Value.Date : DateTime.Today;
                var queue = _queueService.GetDailyQueue(targetDate);
                if (btnCallExam != null)
                {
                    btnCallExam.Enabled = SessionContext.IsDoctor && (targetDate == DateTime.Today);
                }
                int selectedApptId = 0;
                if (dgvQueue.SelectedRows.Count > 0)
                {
                    selectedApptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);
                }

                dgvQueue.Rows.Clear();
                foreach (var q in queue)
                {
                    string statusDesc = GetStatusArabic(q.Status);
                    string urgentText = q.IsUrgent ? "★ مستعجل" : "-";
                    string ageText = q.Age.HasValue ? $"{q.Age}" : "-";

                    int rowIdx = dgvQueue.Rows.Add(
                        q.AppointmentId,
                        q.TurnNumber,
                        q.FileNumber,
                        q.PatientName,
                        ageText,
                        q.ExamType,
                        q.AmountPaid > 0 ? $"{q.AmountPaid:N0} ج" : "لم يُدفع",
                        q.PaymentMethod.HasValue ? q.PaymentMethod.Value.ToString() : "-",
                        statusDesc,
                        urgentText
                    );

                    var row = dgvQueue.Rows[rowIdx];
                    if (q.Status == AppointmentStatus.InExam)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(220, 252, 231); // Light emerald
                        row.DefaultCellStyle.Font = UITheme.BodyBoldFont;
                    }
                    else if (q.IsNextInLine)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(224, 242, 254); // Light sky blue
                        row.DefaultCellStyle.Font = UITheme.BodyBoldFont;
                        row.Cells["Status"].Value = "★ التالي في الكشف";
                    }
                    else if (q.IsUrgent && q.Status == AppointmentStatus.Waiting)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199); // Light amber
                    }
                    else if (q.Status == AppointmentStatus.BookedUnpaid)
                    {
                        row.DefaultCellStyle.ForeColor = UITheme.TextMuted;
                    }
                }

                // Restore selection
                if (selectedApptId > 0)
                {
                    foreach (DataGridViewRow r in dgvQueue.Rows)
                    {
                        if (Convert.ToInt32(r.Cells["AppointmentId"].Value) == selectedApptId)
                        {
                            r.Selected = true;
                            break;
                        }
                    }
                }

                DgvQueue_SelectionChanged(null, null);
            }
            catch
            {
                // Suppress background poll errors
            }
        }

        private void DgvQueue_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvQueue.SelectedRows.Count == 0)
            {
                // Doctor can still press call to automatically take the next-in-line patient
                btnCallExam.Enabled = SessionContext.IsDoctor;
                btnPay.Enabled = false;
                btnRefund.Enabled = false;
                btnNoShow.Enabled = false;
                return;
            }

            int apptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);
            string status = dgvQueue.SelectedRows[0].Cells["Status"].Value != null ? dgvQueue.SelectedRows[0].Cells["Status"].Value.ToString() : "";

            bool isWaitingOrNext = (status == "منتظر" || status.Contains("التالي"));
            btnCallExam.Enabled = SessionContext.IsDoctor && isWaitingOrNext;
            btnPay.Enabled = (status == "محجوز - لم يُدفع");
            btnRefund.Enabled = (isWaitingOrNext || (SessionContext.IsDoctor && status != "ملغي"));
            btnNoShow.Enabled = (isWaitingOrNext || status == "محجوز - لم يُدفع");
        }

        private void BtnCallExam_Click(object sender, EventArgs e)
        {
            int apptId = 0;
            if (dgvQueue.SelectedRows.Count > 0)
            {
                apptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);
            }
            else
            {
                int? nextId = _queueService.GetNextInLineAppointmentId(DateTime.Today);
                if (nextId.HasValue)
                {
                    apptId = nextId.Value;
                }
            }

            if (apptId == 0)
            {
                MessageBox.Show("لا يوجد مريض منتظر في قائمة الانتظار حالياً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var res = _queueService.CallPatientToExam(apptId);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshQueue();

            // Load into Doctor Consultation Tab and switch
            LoadActiveExamPatient(apptId);
            tabMain.SelectedTab = tabDoctorExam;
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (dgvQueue.SelectedRows.Count == 0) return;
            int apptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);

            using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
            {
                var appt = db.Appointments.Include("Patient").FirstOrDefault(a => a.Id == apptId);
                if (appt != null)
                {
                    using (var dlg = new PaymentDialog(appt))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshQueue();
                        }
                    }
                }
            }
        }

        private void BtnRefund_Click(object sender, EventArgs e)
        {
            if (dgvQueue.SelectedRows.Count == 0) return;
            int apptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);

            using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
            {
                var pay = db.Payments.Include("Patient").FirstOrDefault(p => p.AppointmentId == apptId && !p.IsRefunded);
                if (pay == null)
                {
                    MessageBox.Show("لا توجد دفعة مسجلة لهذا الحجز للاسترداد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new RefundDialog(pay.Id, pay.Patient.FullName, pay.ActualPrice))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        RefreshQueue();
                    }
                }
            }
        }

        private void BtnNoShow_Click(object sender, EventArgs e)
        {
            if (dgvQueue.SelectedRows.Count == 0) return;
            int apptId = Convert.ToInt32(dgvQueue.SelectedRows[0].Cells["AppointmentId"].Value);

            var dr = MessageBox.Show("هل ترغب في تحديد هذا المريض كـ (ما حضرش) وتخطيه في الدور؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                _queueService.MarkNoShow(apptId);
                RefreshQueue();
            }
        }

        private string GetStatusArabic(AppointmentStatus status)
        {
            switch (status)
            {
                case AppointmentStatus.BookedUnpaid: return "محجوز - لم يُدفع";
                case AppointmentStatus.Waiting: return "منتظر";
                case AppointmentStatus.InExam: return "داخل الكشف";
                case AppointmentStatus.Completed: return "خلّص";
                case AppointmentStatus.NoShow: return "ما حضرش";
                case AppointmentStatus.Cancelled: return "ملغي";
                default: return status.ToString();
            }
        }

        #endregion

        #region Tab 2: Doctor Consultation Workspace (Doctor Only)

        private TabPage BuildDoctorExamTab()
        {
            tabDoctorExam = new TabPage { Text = "  شاشة كشف الطبيب والروشتة  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tabDoctorExam);

            // Top Patient Summary Banner
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White, Padding = new Padding(15, 10, 15, 10), RightToLeft = RightToLeft.Yes };
            tabDoctorExam.Controls.Add(pnlTop);

            lblActiveExamPatient = new Label
            {
                Text = "لا يوجد مريض داخل الكشف حالياً (اختر مريضاً من قائمة الانتظار واضغط استدعاء للكشف).",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleRight,
                Height = 28
            };
            pnlTop.Controls.Add(lblActiveExamPatient);

            pnlAllergiesAlert = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(254, 226, 226), // Soft Red
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                RightToLeft = RightToLeft.Yes,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlTop.Controls.Add(pnlAllergiesAlert);

            lblAllergiesText = new Label
            {
                Text = "⚠ تنبيه طبي:",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Danger,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlAllergiesAlert.Controls.Add(lblAllergiesText);

            // Split into: Right Column (Clinical exam fields) & Left Column (Prescription & History)
            splitDoctorExam = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                RightToLeft = RightToLeft.Yes
            };
            tabDoctorExam.Controls.Add(splitDoctorExam);
            splitDoctorExam.BringToFront();
            tabDoctorExam.Resize += (s, e) =>
            {
                if (splitDoctorExam != null && splitDoctorExam.Width > 600)
                {
                    try { splitDoctorExam.SplitterDistance = splitDoctorExam.Width / 2; } catch { }
                }
            };

            // --- Panel 1: Clinical Examination Details ---
            var pnlExam = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15), BackColor = Color.White };
            splitDoctorExam.Panel1.Controls.Add(pnlExam);

            int y = 10;
            pnlExam.Controls.Add(new Label { Text = "الشكوى (Chief Complaint):", Location = new Point(15, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtComplaint = new TextBox { Location = new Point(15, y + 22), Width = 510, Font = UITheme.BodyFont };
            pnlExam.Controls.Add(txtComplaint);
            y += 55;

            pnlExam.Controls.Add(new Label { Text = "التشخيص (Diagnosis - اقتراحات تلقائية):", Location = new Point(15, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtDiagnosis = new TextBox { Location = new Point(15, y + 22), Width = 510, Font = UITheme.BodyFont };
            SetupDiagnosisAutocomplete(txtDiagnosis);
            pnlExam.Controls.Add(txtDiagnosis);
            y += 55;

            // Eye Measurements Box (VA & IOP)
            var grpEye = new GroupBox
            {
                Text = " قياسات العين (اختيارية - لا تُطبع في الروشتة) ",
                Location = new Point(15, y),
                Size = new Size(510, 115),
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary
            };
            pnlExam.Controls.Add(grpEye);

            // Right Eye (OD)
            grpEye.Controls.Add(new Label { Text = "العين اليمنى (OD):", Location = new Point(380, 25), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextDark });
            grpEye.Controls.Add(new Label { Text = "حدة الإبصار (VA):", Location = new Point(410, 50), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted });
            txtVA_OD = new TextBox { Location = new Point(340, 48), Width = 65 };
            grpEye.Controls.Add(txtVA_OD);

            grpEye.Controls.Add(new Label { Text = "ضغط العين (IOP):", Location = new Point(410, 80), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted });
            txtIOP_OD = new TextBox { Location = new Point(340, 78), Width = 65 };
            grpEye.Controls.Add(txtIOP_OD);

            // Left Eye (OS)
            grpEye.Controls.Add(new Label { Text = "العين اليسرى (OS):", Location = new Point(140, 25), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextDark });
            grpEye.Controls.Add(new Label { Text = "حدة الإبصار (VA):", Location = new Point(170, 50), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted });
            txtVA_OS = new TextBox { Location = new Point(100, 48), Width = 65 };
            grpEye.Controls.Add(txtVA_OS);

            grpEye.Controls.Add(new Label { Text = "ضغط العين (IOP):", Location = new Point(170, 80), AutoSize = true, Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted });
            txtIOP_OS = new TextBox { Location = new Point(100, 78), Width = 65 };
            grpEye.Controls.Add(txtIOP_OS);
            y += 125;

            // Doctor Confidential Notes
            pnlExam.Controls.Add(new Label { Text = "ملاحظات الطبيب السرية (خاصة بك ولا تُطبع):", Location = new Point(15, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtDoctorNotes = new TextBox { Location = new Point(15, y + 22), Width = 510, Height = 60, Multiline = true, Font = UITheme.BodyFont };
            pnlExam.Controls.Add(txtDoctorNotes);
            y += 90;

            // Follow-up Date
            pnlExam.Controls.Add(new Label { Text = "موعد المتابعة المقترح:", Location = new Point(360, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            dtpFollowUp = new DateTimePicker { Location = new Point(180, y), Width = 170, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
            pnlExam.Controls.Add(dtpFollowUp);

            // --- Panel 2: Prescription & Visit History ---
            var pnlRx = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15), BackColor = Color.White };
            splitDoctorExam.Panel2.Controls.Add(pnlRx);

            var lblRxTitle = new Label { Text = "الأدوية والعلاج (تُطبع في صندوق الروشتة 13 × 13 سم):", Location = new Point(15, 12), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandPrimary };
            pnlRx.Controls.Add(lblRxTitle);

            btnRepeatRx = new Button { Text = "⟳ تكرار آخر روشتة", Location = new Point(370, 8), Width = 165, Height = 30 };
            UITheme.StyleSecondaryButton(btnRepeatRx);
            btnRepeatRx.Click += BtnRepeatRx_Click;
            pnlRx.Controls.Add(btnRepeatRx);

            // Stylized Medicine Input Boxes Container (بوكسات إدخال أنيقة وواضحة)
            var pnlMedInput = new Panel
            {
                Location = new Point(15, 44),
                Size = new Size(520, 150),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRx.Controls.Add(pnlMedInput);

            // Row 1 inside pnlMedInput: Medicine Name + Eye Location
            var lblNewMed = new Label { Text = "💊 اسم الدواء (عربي أو إنجليزي):", Location = new Point(200, 8), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandDark };
            pnlMedInput.Controls.Add(lblNewMed);

            txtNewMedName = new TextBox { Location = new Point(200, 30), Width = 305, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
            pnlMedInput.Controls.Add(txtNewMedName);

            var lblEye = new Label { Text = "👁️ موضع الاستعمال:", Location = new Point(15, 8), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandDark };
            pnlMedInput.Controls.Add(lblEye);

            cboNewMedEye = new ComboBox { Location = new Point(15, 30), Width = 175, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyFont };
            cboNewMedEye.Items.AddRange(new object[] { "بالعينين (OD + OS)", "العين اليمنى فقط (OD)", "العين اليسرى فقط (OS)", "مرهم قبل النوم", "(بدون تحديد)" });
            cboNewMedEye.SelectedIndex = 0;
            pnlMedInput.Controls.Add(cboNewMedEye);

            // Row 2 inside pnlMedInput: Frequency + Duration + Add Button
            var lblFreq = new Label { Text = "⏱️ عدد المرات (التكرار):", Location = new Point(340, 68), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandDark };
            pnlMedInput.Controls.Add(lblFreq);

            cboNewMedFrequency = new ComboBox { Location = new Point(340, 92), Width = 165, DropDownStyle = ComboBoxStyle.DropDown, Font = UITheme.BodyFont };
            cboNewMedFrequency.Items.AddRange(new object[] {
                "3 مرات يومياً",
                "مرتين يومياً",
                "مرة واحدة يومياً (صباحاً)",
                "مرة واحدة يومياً (قبل النوم)",
                "4 مرات يومياً",
                "كل ساعتين",
                "كل 4 ساعات",
                "كل 6 ساعات",
                "عند اللزوم / للترطيب",
                "قطرة كل ساعة"
            });
            cboNewMedFrequency.Text = "3 مرات يومياً";
            pnlMedInput.Controls.Add(cboNewMedFrequency);

            var lblDur = new Label { Text = "📅 المدة:", Location = new Point(175, 68), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandDark };
            pnlMedInput.Controls.Add(lblDur);

            cboNewMedDuration = new ComboBox { Location = new Point(175, 92), Width = 155, DropDownStyle = ComboBoxStyle.DropDown, Font = UITheme.BodyFont };
            cboNewMedDuration.Items.AddRange(new object[] {
                "لمدة أسبوع",
                "لمدة 5 أيام",
                "لمدة 3 أيام",
                "لمدة 10 أيام",
                "لمدة أسبوعين",
                "لمدة شهر",
                "حتى الاستشارة القادمة",
                "حتى نفاد العبوة",
                "باستمرار (مزمن)"
            });
            cboNewMedDuration.Text = "لمدة أسبوع";
            pnlMedInput.Controls.Add(cboNewMedDuration);

            btnAddMedItem = new Button { Text = "➕ إضافة الدواء", Location = new Point(15, 88), Width = 150, Height = 34 };
            UITheme.StylePrimaryButton(btnAddMedItem);
            btnAddMedItem.Click += (s, e) => AddMedicineItemToGrid();
            pnlMedInput.Controls.Add(btnAddMedItem);

            // Fast Enter-key navigation
            txtNewMedName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; cboNewMedFrequency.Focus(); } };
            cboNewMedFrequency.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; cboNewMedDuration.Focus(); } };
            cboNewMedDuration.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddMedicineItemToGrid(); } };
            txtNewMedName.Leave += TxtNewMedName_Leave;
            SetupMedicineAutocomplete();

            // Medicines Table (dgvMedicines)
            dgvMedicines = new DataGridView
            {
                Location = new Point(15, 202),
                Size = new Size(520, 160),
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            UITheme.StyleDataGridView(dgvMedicines);
            dgvMedicines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMedicines.RowTemplate.Height = 32;
            dgvMedicines.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgvMedicines.GridColor = Color.FromArgb(203, 213, 225);
            dgvMedicines.ColumnHeadersDefaultCellStyle.BackColor = UITheme.BrandPrimary;
            dgvMedicines.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvMedicines.ColumnHeadersDefaultCellStyle.Font = UITheme.BodyBoldFont;
            dgvMedicines.ColumnHeadersHeight = 34;

            var colNum = new DataGridViewTextBoxColumn { Name = "ColNum", HeaderText = "#", Width = 32, ReadOnly = true };
            colNum.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colNum.DefaultCellStyle.ForeColor = UITheme.TextMuted;

            var medCol = new DataGridViewTextBoxColumn { Name = "MedicineName", HeaderText = "اسم الدواء", FillWeight = 36 };
            var freqCol = new DataGridViewTextBoxColumn { Name = "Frequency", HeaderText = "عدد المرات", FillWeight = 26 };
            var durCol = new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "المدة", FillWeight = 20 };
            var eyeCol = new DataGridViewTextBoxColumn { Name = "EyeLocation", HeaderText = "العين / الملاحظة", FillWeight = 18 };

            var delCol = new DataGridViewButtonColumn
            {
                Name = "ColDelete",
                HeaderText = "حذف",
                Text = "✕",
                UseColumnTextForButtonValue = true,
                Width = 40,
                FlatStyle = FlatStyle.Flat
            };
            delCol.DefaultCellStyle.ForeColor = UITheme.Danger;
            delCol.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            dgvMedicines.Columns.AddRange(new DataGridViewColumn[] { colNum, medCol, freqCol, durCol, eyeCol, delCol });
            dgvMedicines.CellContentClick += DgvMedicines_CellContentClick;
            dgvMedicines.EditingControlShowing += DgvMedicines_EditingControlShowing;
            dgvMedicines.CellEndEdit += DgvMedicines_CellEndEdit;
            pnlRx.Controls.Add(dgvMedicines);

            // Action Buttons
            var flowRxButtons = new FlowLayoutPanel
            {
                Location = new Point(15, 370),
                Size = new Size(520, 48),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlRx.Controls.Add(flowRxButtons);

            btnSavePrintRx = new Button { Text = "🖨 حفظ وطباعة الروشتة المخصوصة", Width = 260, Height = 40, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnSavePrintRx);
            btnSavePrintRx.Click += BtnSavePrintRx_Click;
            flowRxButtons.Controls.Add(btnSavePrintRx);

            btnFinishExam = new Button { Text = "✓ إنهاء الكشف", Width = 140, Height = 40, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSuccessButton(btnFinishExam);
            btnFinishExam.Click += BtnFinishExam_Click;
            flowRxButtons.Controls.Add(btnFinishExam);

            btnClearRx = new Button { Text = "🗑 مسح الأدوية", Width = 100, Height = 40, Margin = new Padding(0, 0, 0, 0) };
            UITheme.StyleSecondaryButton(btnClearRx);
            btnClearRx.Click += (s, e) => { dgvMedicines.Rows.Clear(); };
            flowRxButtons.Controls.Add(btnClearRx);

            // Previous Visits History
            var lblHist = new Label { Text = "تاريخ زيارات المريض السابقة (من الأحدث للأقدم):", Location = new Point(15, 426), AutoSize = true, Font = UITheme.BodyBoldFont };
            pnlRx.Controls.Add(lblHist);

            dgvHistory = new DataGridView
            {
                Location = new Point(15, 452),
                Size = new Size(520, 160)
            };
            UITheme.StyleDataGridView(dgvHistory);
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var hDate = new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "التاريخ", FillWeight = 25 };
            var hDiag = new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "التشخيص", FillWeight = 40 };
            var hComp = new DataGridViewTextBoxColumn { Name = "Complaint", HeaderText = "الشكوى", FillWeight = 35 };
            dgvHistory.Columns.AddRange(new DataGridViewColumn[] { hDate, hDiag, hComp });
            pnlRx.Controls.Add(dgvHistory);
            return tabDoctorExam;
        }

        private void SetupMedicineAutocomplete()
        {
            if (txtNewMedName == null) return;
            var medNames = _catalogService.GetAllMedicineNames();
            var col = new AutoCompleteStringCollection();
            col.AddRange(medNames.ToArray());

            txtNewMedName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtNewMedName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtNewMedName.AutoCompleteCustomSource = col;
        }

        private void TxtNewMedName_Leave(object sender, EventArgs e)
        {
            string medName = txtNewMedName.Text.Trim();
            if (!string.IsNullOrWhiteSpace(medName))
            {
                string defDose = _catalogService.GetDefaultDoseForMedicine(medName);
                if (!string.IsNullOrWhiteSpace(defDose))
                {
                    var parts = defDose.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1 && string.IsNullOrWhiteSpace(cboNewMedFrequency.Text))
                        cboNewMedFrequency.Text = parts[0].Trim();
                    if (parts.Length >= 2 && string.IsNullOrWhiteSpace(cboNewMedDuration.Text))
                        cboNewMedDuration.Text = parts[1].Trim();
                    if (parts.Length >= 3 && cboNewMedEye.SelectedIndex <= 0)
                        cboNewMedEye.Text = parts[2].Trim();
                }
            }
        }

        private void AddMedicineItemToGrid()
        {
            string medName = txtNewMedName.Text.Trim();
            if (string.IsNullOrWhiteSpace(medName))
            {
                MessageBox.Show("يرجى إدخال اسم الدواء أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewMedName.Focus();
                return;
            }

            string freq = cboNewMedFrequency.Text.Trim();
            string dur = cboNewMedDuration.Text.Trim();
            string eye = cboNewMedEye.Text.Trim();

            int num = dgvMedicines.Rows.Count + 1;
            dgvMedicines.Rows.Add(num.ToString(), medName, freq, dur, eye);

            // Auto-save to medicine catalog
            _catalogService.SaveMedicineIfNew(medName, FormatDoseString(freq, dur, eye));
            SetupMedicineAutocomplete();

            // Clear inputs for next medicine
            txtNewMedName.Clear();
            txtNewMedName.Focus();
        }

        private void DgvMedicines_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvMedicines.Columns["ColDelete"].Index)
            {
                dgvMedicines.Rows.RemoveAt(e.RowIndex);
                RenumberPrescriptionGrid();
            }
        }

        private void RenumberPrescriptionGrid()
        {
            for (int i = 0; i < dgvMedicines.Rows.Count; i++)
            {
                dgvMedicines.Rows[i].Cells["ColNum"].Value = (i + 1).ToString();
            }
        }

        private string FormatDoseString(string freq, string dur, string eye)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(freq)) parts.Add(freq.Trim());
            if (!string.IsNullOrWhiteSpace(dur)) parts.Add(dur.Trim());
            if (!string.IsNullOrWhiteSpace(eye) && eye != "(بدون تحديد)") parts.Add(eye.Trim());
            return string.Join(" - ", parts);
        }

        private void SetupDiagnosisAutocomplete(TextBox txt)
        {
            var source = new AutoCompleteStringCollection();
            var suggestions = _catalogService.GetAllDiagnoses();
            source.AddRange(suggestions.ToArray());

            txt.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txt.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txt.AutoCompleteCustomSource = source;
        }

        private void DgvMedicines_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var txtBox = e.Control as TextBox;
            if (txtBox == null) return;

            txtBox.AutoCompleteMode = AutoCompleteMode.None;
            txtBox.AutoCompleteSource = AutoCompleteSource.None;

            int colIdx = dgvMedicines.CurrentCell.ColumnIndex;
            string colName = dgvMedicines.Columns[colIdx].Name;

            if (colName == "MedicineName")
            {
                var medNames = _catalogService.GetAllMedicineNames();
                var col = new AutoCompleteStringCollection();
                col.AddRange(medNames.ToArray());

                txtBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtBox.AutoCompleteCustomSource = col;
            }
            else if (colName == "Frequency")
            {
                var col = new AutoCompleteStringCollection();
                col.AddRange(new string[] { "3 مرات يومياً", "مرتين يومياً", "مرة واحدة يومياً (صباحاً)", "مرة واحدة يومياً (قبل النوم)", "4 مرات يومياً", "كل ساعتين", "كل 4 ساعات", "عند اللزوم / للترطيب" });
                txtBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtBox.AutoCompleteCustomSource = col;
            }
            else if (colName == "Duration")
            {
                var col = new AutoCompleteStringCollection();
                col.AddRange(new string[] { "لمدة أسبوع", "لمدة 5 أيام", "لمدة 3 أيام", "لمدة 10 أيام", "لمدة أسبوعين", "لمدة شهر", "حتى الاستشارة", "حتى نفاد العبوة", "باستمرار (مزمن)" });
                txtBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtBox.AutoCompleteCustomSource = col;
            }
        }

        private void DgvMedicines_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvMedicines.Rows.Count) return;

            string colName = dgvMedicines.Columns[e.ColumnIndex].Name;
            if (colName == "MedicineName")
            {
                var medCell = dgvMedicines.Rows[e.RowIndex].Cells["MedicineName"];
                var freqCell = dgvMedicines.Rows[e.RowIndex].Cells["Frequency"];
                var durCell = dgvMedicines.Rows[e.RowIndex].Cells["Duration"];
                var eyeCell = dgvMedicines.Rows[e.RowIndex].Cells["EyeLocation"];

                string medName = medCell.Value != null ? medCell.Value.ToString().Trim() : "";
                string curFreq = freqCell.Value != null ? freqCell.Value.ToString().Trim() : "";

                if (!string.IsNullOrWhiteSpace(medName) && string.IsNullOrWhiteSpace(curFreq))
                {
                    string defDose = _catalogService.GetDefaultDoseForMedicine(medName);
                    if (!string.IsNullOrWhiteSpace(defDose))
                    {
                        var parts = defDose.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1) freqCell.Value = parts[0].Trim();
                        if (parts.Length >= 2) durCell.Value = parts[1].Trim();
                        if (parts.Length >= 3) eyeCell.Value = parts[2].Trim();
                    }
                }
            }
            RenumberPrescriptionGrid();
        }

        private void LoadActiveExamPatient(int appointmentId)
        {
            _currentExamAppointmentId = appointmentId;

            using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
            {
                var appt = db.Appointments.Include("Patient").FirstOrDefault(a => a.Id == appointmentId);
                if (appt == null) return;

                _currentExamPatientId = appt.PatientId;
                var patient = appt.Patient;

                int age = patient.CalculatedAge ?? 0;
                lblActiveExamPatient.Text = $"المريض الحالي: {patient.FullName}  |  رقم الملف: {patient.FileNumber}  |  السن: {age} سنة  |  رقم الدور: {appt.TurnNumber}";

                // Show Allergies / Chronic diseases banner if present
                bool hasAlert = !string.IsNullOrWhiteSpace(patient.ChronicDiseases) || !string.IsNullOrWhiteSpace(patient.DrugAllergies);
                pnlAllergiesAlert.Visible = hasAlert;
                if (hasAlert)
                {
                    lblAllergiesText.Text = $"⚠ أمراض مزمنة: {patient.ChronicDiseases ?? "لا يوجد"}  |  حساسية الأدوية: {patient.DrugAllergies ?? "لا يوجد"}";
                }

                // Clear previous inputs
                txtComplaint.Clear();
                txtDiagnosis.Clear();
                txtVA_OD.Clear();
                txtVA_OS.Clear();
                txtIOP_OD.Clear();
                txtIOP_OS.Clear();
                txtDoctorNotes.Clear();
                dtpFollowUp.Checked = false;
                dgvMedicines.Rows.Clear();

                // Load Visit History
                var history = _visitService.GetPatientVisitHistory(patient.Id);
                dgvHistory.Rows.Clear();
                foreach (var v in history)
                {
                    dgvHistory.Rows.Add(v.VisitDate.ToString("dd/MM/yyyy"), v.Diagnosis, v.ChiefComplaint);
                }
            }
        }

        private void BtnRepeatRx_Click(object sender, EventArgs e)
        {
            if (_currentExamPatientId == 0) return;

            var lastRx = _visitService.GetLastPrescription(_currentExamPatientId);
            if (lastRx.Count == 0)
            {
                MessageBox.Show("لا توجد روشتة سابقة مسجلة لهذا المريض.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvMedicines.Rows.Clear();
            foreach (var item in lastRx)
            {
                string freq = "";
                string dur = "";
                string eye = "بالعينين (OD + OS)";
                if (!string.IsNullOrWhiteSpace(item.DoseInstructions))
                {
                    var parts = item.DoseInstructions.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) freq = parts[0].Trim();
                    if (parts.Length >= 2) dur = parts[1].Trim();
                    if (parts.Length >= 3) eye = parts[2].Trim();
                }
                int num = dgvMedicines.Rows.Count + 1;
                dgvMedicines.Rows.Add(num.ToString(), item.MedicineName, freq, dur, eye);
            }
        }

        private void BtnSavePrintRx_Click(object sender, EventArgs e)
        {
            if (_currentExamPatientId == 0)
            {
                MessageBox.Show("يرجى استدعاء مريض للكشف أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var visit = new Visit
            {
                PatientId = _currentExamPatientId,
                AppointmentId = _currentExamAppointmentId,
                ChiefComplaint = txtComplaint.Text.Trim(),
                Diagnosis = txtDiagnosis.Text.Trim(),
                VA_OD = txtVA_OD.Text.Trim(),
                VA_OS = txtVA_OS.Text.Trim(),
                IOP_OD = txtIOP_OD.Text.Trim(),
                IOP_OS = txtIOP_OS.Text.Trim(),
                DoctorNotes = txtDoctorNotes.Text.Trim(),
                FollowUpDate = dtpFollowUp.Checked ? (DateTime?)dtpFollowUp.Value.Date : null
            };

            var medicines = new List<PrescriptionPrintItemDto>();
            foreach (DataGridViewRow row in dgvMedicines.Rows)
            {
                if (row.IsNewRow) continue;
                string medName = row.Cells["MedicineName"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(medName)) continue;

                string freq = row.Cells["Frequency"].Value?.ToString() ?? "";
                string dur = row.Cells["Duration"].Value?.ToString() ?? "";
                string eye = row.Cells["EyeLocation"].Value?.ToString() ?? "";

                string dose = FormatDoseString(freq, dur, eye);

                medicines.Add(new PrescriptionPrintItemDto
                {
                    MedicineName = medName.Trim(),
                    DoseInstructions = dose
                });
            }

            var saveRes = _visitService.SaveVisit(visit, medicines);
            if (!saveRes.IsSuccess)
            {
                MessageBox.Show(saveRes.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Print on custom paper
            var patient = _patientService.GetPatientById(_currentExamPatientId);
            var printDto = new PrescriptionPrintDto
            {
                VisitId = visit.Id,
                PatientName = patient?.FullName ?? "",
                Age = patient?.CalculatedAge.HasValue == true ? patient.CalculatedAge.Value.ToString() : "",
                Diagnosis = visit.Diagnosis,
                Date = DateTime.Today,
                Medicines = medicines
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
                MessageBox.Show($"تم حفظ الكشف بنجاح، ولكن تعذرت الطباعة:\n{ex.Message}", "تنبيه الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Auto-save diagnosis and medicines if new
            string enteredDiagnosis = txtDiagnosis.Text.Trim();
            if (!string.IsNullOrWhiteSpace(enteredDiagnosis))
            {
                _catalogService.SaveDiagnosisIfNew(enteredDiagnosis);
                SetupDiagnosisAutocomplete(txtDiagnosis);
            }

            foreach (var m in medicines)
            {
                _catalogService.SaveMedicineIfNew(m.MedicineName, m.DoseInstructions);
            }

            MessageBox.Show("تم حفظ الزيارة والروشتة بنجاح وتحديث تاريخ المريض.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshQueue();
        }

        private void BtnFinishExam_Click(object sender, EventArgs e)
        {
            if (_currentExamAppointmentId > 0)
            {
                _queueService.CompleteExam(_currentExamAppointmentId);
                RefreshQueue();
            }

            _currentExamPatientId = 0;
            _currentExamAppointmentId = 0;
            lblActiveExamPatient.Text = "تم إنهاء الكشف. اختر المريض التالي من قائمة الانتظار.";
            pnlAllergiesAlert.Visible = false;
            tabMain.SelectedIndex = 0; // Back to queue
        }

        #endregion

        #region Tab 3: Patients Directory

        private TabPage BuildPatientsTab()
        {
            var tab = new TabPage { Text = "  سجل ودليل المرضى  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tab);

            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(15, 15, 15, 15),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                WrapContents = false
            };
            tab.Controls.Add(pnlTop);

            pnlTop.Controls.Add(new Label { Text = "بحث في المرضى:", AutoSize = true, Font = UITheme.BodyBoldFont, Margin = new Padding(0, 6, 6, 0) });
            txtSearchPatients = new TextBox { Width = 190, Font = UITheme.BodyFont, TextAlign = HorizontalAlignment.Right, RightToLeft = RightToLeft.Yes, Margin = new Padding(0, 2, 10, 0) };
            txtSearchPatients.TextChanged += (s, e) => LoadPatientsGrid(txtSearchPatients.Text.Trim());
            pnlTop.Controls.Add(txtSearchPatients);

            var btnAdd = new Button { Text = "+ مريض جديد", Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            UITheme.StylePrimaryButton(btnAdd);
            btnAdd.Click += (s, e) =>
            {
                using (var dlg = new PatientEditDialog())
                {
                    if (dlg.ShowDialog() == DialogResult.OK) LoadPatientsGrid(txtSearchPatients.Text.Trim());
                }
            };
            pnlTop.Controls.Add(btnAdd);

            var btnEdit = new Button { Text = "تعديل الملف", Width = 95, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSecondaryButton(btnEdit);
            btnEdit.Click += (s, e) =>
            {
                if (dgvPatients.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgvPatients.SelectedRows[0].Cells["Id"].Value);
                var p = _patientService.GetPatientById(id);
                if (p != null)
                {
                    using (var dlg = new PatientEditDialog(p))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK) LoadPatientsGrid(txtSearchPatients.Text.Trim());
                    }
                }
            };
            pnlTop.Controls.Add(btnEdit);

            var btnProfile = new Button { Text = "الملف الشامل", Width = 115, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSuccessButton(btnProfile);
            btnProfile.Click += (s, e) => OpenSelectedPatientProfile();
            pnlTop.Controls.Add(btnProfile);

            var btnExportPatients = new Button { Text = "📊 تصدير Excel", Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            UITheme.StyleSecondaryButton(btnExportPatients);
            btnExportPatients.Click += (s, e) => ExportGrid(dgvPatients, "سجل_المرضى");
            pnlTop.Controls.Add(btnExportPatients);

            if (SessionContext.IsDoctor)
            {
                var btnImportPatients = new Button { Text = "📥 استيراد Excel", Width = 125, Height = 34, Margin = new Padding(0, 0, 8, 0) };
                UITheme.StyleSecondaryButton(btnImportPatients);
                btnImportPatients.Click += (s, e) =>
                {
                    using (var dlg = new PatientImportForm())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK && dlg.ImportedCount > 0)
                        {
                            LoadPatientsGrid(txtSearchPatients.Text.Trim());
                        }
                    }
                };
                pnlTop.Controls.Add(btnImportPatients);

                var btnArchivePatient = new Button { Text = "📦 أرشفة المريض", Width = 120, Height = 34, Margin = new Padding(0, 0, 8, 0) };
                UITheme.StyleDangerButton(btnArchivePatient);
                btnArchivePatient.Click += (s, e) =>
                {
                    if (dgvPatients.SelectedRows.Count == 0)
                    {
                        MessageBox.Show("يرجى اختيار مريض أولاً لأرشفته.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    int id = Convert.ToInt32(dgvPatients.SelectedRows[0].Cells["Id"].Value);
                    string name = Convert.ToString(dgvPatients.SelectedRows[0].Cells["FullName"].Value);
                    var confirm = MessageBox.Show(
                        string.Format("هل أنت متأكد من رغبتك في أرشفة المريض:\n{0}\n\nسيتم إخفاؤه من البحث وقائمة الانتظار مع الاحتفاظ ببياناته الطبية كاملة في السجل.", name),
                        "تأكيد أرشفة المريض",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        var res = _patientService.ArchivePatient(id);
                        if (res.IsSuccess)
                        {
                            MessageBox.Show("تمت أرشفة المريض بنجاح.", "تمت العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadPatientsGrid(txtSearchPatients.Text.Trim());
                            RefreshQueue();
                        }
                        else
                        {
                            MessageBox.Show(res.ErrorMessage, "خطأ في الأرشفة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };
                pnlTop.Controls.Add(btnArchivePatient);
            }

            dgvPatients = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGridView(dgvPatients);
            dgvPatients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tab.Controls.Add(dgvPatients);
            dgvPatients.BringToFront(); // Crucial: sets z-order so dgvPatients sits below pnlTop!

            dgvPatients.Columns.Add("Id", "Id");
            dgvPatients.Columns["Id"].Visible = false;

            var pFile = new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "رقم الملف", FillWeight = 65 };
            var pName = new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "اسم المريض بالكامل", FillWeight = 180 };
            var pMobile = new DataGridViewTextBoxColumn { Name = "Mobile", HeaderText = "الموبايل", FillWeight = 85 };
            var pAge = new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "السن", FillWeight = 40 };
            var pGen = new DataGridViewTextBoxColumn { Name = "Gender", HeaderText = "النوع", FillWeight = 45 };
            var pVisit = new DataGridViewTextBoxColumn { Name = "LastVisit", HeaderText = "آخر زيارة", FillWeight = 85 };

            dgvPatients.Columns.AddRange(new DataGridViewColumn[] { pFile, pName, pMobile, pAge, pGen, pVisit });

            dgvPatients.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedPatientProfile();
            };

            LoadPatientsGrid("");
            return tab;
        }

        private void OpenSelectedPatientProfile()
        {
            if (dgvPatients.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار مريض أولاً لعرض ملفه الطبي الشامل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id = Convert.ToInt32(dgvPatients.SelectedRows[0].Cells["Id"].Value);
            using (var dlg = new PatientProfileDialog(id))
            {
                dlg.ShowDialog();
            }
        }

        private void LoadPatientsGrid(string term)
        {
            var list = _patientService.SearchPatients(term, 50);
            dgvPatients.Rows.Clear();
            foreach (var p in list)
            {
                string lastV = p.LastVisitDate.HasValue ? p.LastVisitDate.Value.ToString("dd/MM/yyyy") : "جديد";
                dgvPatients.Rows.Add(p.Id, p.FileNumber, p.FullName, p.Mobile, p.Age.HasValue ? $"{p.Age}" : "-", p.Gender ?? "-", lastV);
            }
        }

        #endregion

        #region Tab: Daily Archive & Log (Doctor Only)

        private TabPage BuildDayArchiveTab()
        {
            tabDayArchive = new TabPage { Text = "  أرشيف وسجل يوميات العيادة  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tabDayArchive);

            // 1. Top Filter Panel
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(15, 15, 15, 15),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                WrapContents = false
            };
            tabDayArchive.Controls.Add(pnlTop);

            var lblDate = new Label
            {
                Text = "اختر تاريخ اليومية لعرض التفاصيل:",
                AutoSize = true,
                Font = UITheme.BodyBoldFont,
                Margin = new Padding(0, 6, 8, 0)
            };
            pnlTop.Controls.Add(lblDate);

            dtpArchiveDate = new DateTimePicker
            {
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Margin = new Padding(0, 3, 12, 0)
            };
            dtpArchiveDate.ValueChanged += (s, e) => LoadArchiveData();
            pnlTop.Controls.Add(dtpArchiveDate);

            var btnToday = new Button
            {
                Text = "اليوم الحالي",
                Width = 100,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSecondaryButton(btnToday);
            btnToday.Click += (s, e) => { dtpArchiveDate.Value = DateTime.Today; LoadArchiveData(); };
            pnlTop.Controls.Add(btnToday);

            var btnRefresh = new Button
            {
                Text = "تحديث البيانات",
                Width = 120,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StylePrimaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadArchiveData();
            pnlTop.Controls.Add(btnRefresh);

            var btnViewProfile = new Button
            {
                Text = "فتح ملف المريض المحدد",
                Width = 160,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSuccessButton(btnViewProfile);
            btnViewProfile.Click += BtnArchiveViewProfile_Click;
            pnlTop.Controls.Add(btnViewProfile);

            var btnExportArchive = new Button
            {
                Text = "📊 تصدير Excel",
                Width = 130,
                Height = 34,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSecondaryButton(btnExportArchive);
            btnExportArchive.Click += (s, e) => ExportGrid(dgvArchivePatients, string.Format("أرشيف_يومية_{0:yyyyMMdd}", dtpArchiveDate.Value));
            pnlTop.Controls.Add(btnExportArchive);

            // 2. Summary Cards Panel
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 8, 20, 8),
                RightToLeft = RightToLeft.Yes
            };
            tabDayArchive.Controls.Add(pnlSummary);

            lblArchiveCounts = new Label
            {
                Text = "إجمالي الزيارات: 0 | كشف جديد: 0 | إعادة: 0 | مستعجل: 0 | لم يحضر: 0",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary,
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSummary.Controls.Add(lblArchiveCounts);

            lblArchiveRevenue = new Label
            {
                Text = "إجمالي الإيرادات المحصلة: 0.00 ج  (كاش: 0.00 ج | فودافون كاش: 0.00 ج | إنستا باي: 0.00 ج)",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Success,
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSummary.Controls.Add(lblArchiveRevenue);

            var pnlSummaryRow3 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.Yes
            };
            pnlSummary.Controls.Add(pnlSummaryRow3);

            lblArchiveRefunds = new Label
            {
                Text = "المستردات: 0.00 ج  |  صافي الخزينة: 0.00 ج",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Danger,
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSummaryRow3.Controls.Add(lblArchiveRefunds);

            lblArchiveClosing = new Label
            {
                Text = "حالة الإقفال: لم يتم إقفال هذا اليوم بعد",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandAccent,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSummaryRow3.Controls.Add(lblArchiveClosing);

            // 3. Split Container: Top Patients Table, Bottom Prescriptions
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250
            };
            tabDayArchive.Controls.Add(split);
            split.BringToFront();

            // Split 1: Patients
            var pnlPats = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            split.Panel1.Controls.Add(pnlPats);

            var lblPatsTitle = new Label
            {
                Text = "قائمة المرضى وحالات الكشف في هذا اليوم:",
                Dock = DockStyle.Top,
                Height = 28,
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlPats.Controls.Add(lblPatsTitle);

            dgvArchivePatients = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            UITheme.StyleDataGridView(dgvArchivePatients);
            dgvArchivePatients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            pnlPats.Controls.Add(dgvArchivePatients);
            dgvArchivePatients.BringToFront();

            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientId", HeaderText = "PatientId", Visible = false });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Turn", HeaderText = "الدور", FillWeight = 15 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "الوقت", FillWeight = 22 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "رقم الملف", FillWeight = 22 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "اسم المريض", FillWeight = 50 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "السن", FillWeight = 15 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mobile", HeaderText = "الموبايل", FillWeight = 28 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExamType", HeaderText = "نوع الكشف", FillWeight = 25 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المسدد", FillWeight = 22 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "طريقة الدفع", FillWeight = 25 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "حالة الكشف", FillWeight = 25 });
            dgvArchivePatients.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "التشخيص الطبي", FillWeight = 45 });

            dgvArchivePatients.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnArchiveViewProfile_Click(s, e);
            };

            // Split 2: Prescriptions
            var pnlRx = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            split.Panel2.Controls.Add(pnlRx);

            var lblRxTitle = new Label
            {
                Text = "الأدوية والروشتات المكتوبة لمرضى هذا اليوم:",
                Dock = DockStyle.Top,
                Height = 28,
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlRx.Controls.Add(lblRxTitle);

            dgvArchivePrescriptions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            UITheme.StyleDataGridView(dgvArchivePrescriptions);
            dgvArchivePrescriptions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            pnlRx.Controls.Add(dgvArchivePrescriptions);
            dgvArchivePrescriptions.BringToFront();

            dgvArchivePrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileNumber", HeaderText = "رقم الملف", FillWeight = 20 });
            dgvArchivePrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", HeaderText = "اسم المريض", FillWeight = 45 });
            dgvArchivePrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diagnosis", HeaderText = "التشخيص", FillWeight = 35 });
            dgvArchivePrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedicineName", HeaderText = "اسم الدواء", FillWeight = 45 });
            dgvArchivePrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoseInstructions", HeaderText = "الجرعة وطريقة الاستعمال", FillWeight = 55 });

            // Initial load
            LoadArchiveData();
            return tabDayArchive;
        }

        private void LoadArchiveData()
        {
            if (dtpArchiveDate == null) return;
            DateTime date = dtpArchiveDate.Value.Date;

            var archive = _closingService.GetDailyArchiveDetail(date);
            if (archive == null) return;

            // Summary
            var s = archive.Summary;
            lblArchiveCounts.Text = string.Format("إجمالي المسجلين: {0}  |  كشف جديد: {1}  |  إعادة: {2}  |  مستعجل: {3}  |  ما حضرش: {4}",
                archive.Patients.Count, s.TotalExamCount, s.TotalReExamCount, s.TotalUrgentCount, s.NoShowCount);

            lblArchiveRevenue.Text = string.Format("إجمالي الإيرادات المحصلة: {0:N2} ج  (كاش: {1:N2} ج | فودافون كاش: {2:N2} ج | إنستا باي: {3:N2} ج)",
                s.TotalRevenue, s.TotalCash, s.TotalVodafoneCash, s.TotalInstaPay);

            decimal net = s.TotalRevenue - s.RefundedAmount;
            lblArchiveRefunds.Text = string.Format("المستردات: {0:N2} ج  |  صافي الخزينة: {1:N2} ج",
                s.RefundedAmount, net);

            if (archive.ClosedAt.HasValue)
            {
                lblArchiveClosing.Text = string.Format("✓ تم إقفال هذا اليوم بواسطة: {0} في {1:hh:mm tt}",
                    !string.IsNullOrWhiteSpace(archive.ClosedByName) ? archive.ClosedByName : "المشرف",
                    archive.ClosedAt.Value);
                lblArchiveClosing.ForeColor = UITheme.Success;
            }
            else
            {
                lblArchiveClosing.Text = "حالة الإقفال: لم يتم إقفال هذا اليوم";
                lblArchiveClosing.ForeColor = UITheme.TextMuted;
            }

            // Patients grid
            dgvArchivePatients.Rows.Clear();
            foreach (var p in archive.Patients)
            {
                string statusDesc = GetStatusArabic(p.Status);
                string ageStr = p.Age.HasValue ? p.Age.Value.ToString() : "-";
                string methodStr = p.PaymentMethod.HasValue ? p.PaymentMethod.Value.ToString() : "-";

                int rowIdx = dgvArchivePatients.Rows.Add(
                    p.PatientId,
                    p.TurnNumber,
                    p.Time.ToString("hh:mm tt"),
                    p.FileNumber,
                    p.FullName,
                    ageStr,
                    p.Mobile ?? "-",
                    p.ExamType.ToString(),
                    p.AmountPaid > 0 ? string.Format("{0:N0} ج", p.AmountPaid) : "لم يُدفع",
                    methodStr,
                    statusDesc,
                    !string.IsNullOrWhiteSpace(p.Diagnosis) ? p.Diagnosis : "-"
                );

                if (p.Status == AppointmentStatus.Completed)
                {
                    dgvArchivePatients.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                }
            }

            // Prescriptions grid
            dgvArchivePrescriptions.Rows.Clear();
            foreach (var rx in archive.Prescriptions)
            {
                dgvArchivePrescriptions.Rows.Add(
                    rx.FileNumber,
                    rx.PatientName,
                    rx.Diagnosis,
                    rx.MedicineName,
                    rx.DoseInstructions
                );
            }
        }

        private void BtnArchiveViewProfile_Click(object sender, EventArgs e)
        {
            if (dgvArchivePatients.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار مريض من الجدول لعرض ملفه الطبي الشامل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int patientId = Convert.ToInt32(dgvArchivePatients.SelectedRows[0].Cells["PatientId"].Value);
            using (var dlg = new PatientProfileDialog(patientId))
            {
                dlg.ShowDialog();
            }
        }

        #endregion

        #region Tab 4: Daily Closing & Drawer Reconciliation

        private TabPage BuildDailyClosingTab()
        {
            var tab = new TabPage { Text = "  الإقفال اليومي والخزينة  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tab);

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(25), BackColor = Color.White, RightToLeft = RightToLeft.Yes };
            tab.Controls.Add(pnl);

            var pnlCard = new Panel
            {
                Location = new Point(pnl.Width > 800 ? pnl.Width - 760 : 25, 20),
                Width = 720,
                Height = 580,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(pnlCard);
            pnl.Resize += (s, e) =>
            {
                if (pnl.ClientSize.Width > 780)
                    pnlCard.Left = pnl.ClientSize.Width - pnlCard.Width - 30;
                else
                    pnlCard.Left = 15;
            };

            int y = 0;
            pnlCard.Controls.Add(new Label { Text = "مطابقة وجرد الخزينة اليومية:", Font = UITheme.HeaderFont, ForeColor = UITheme.BrandPrimary, Location = new Point(0, y), AutoSize = true });
            y += 45;

            lblClosingStatus = new Label { Text = "حالة اليوم: مفتوح للتحصيل", Font = UITheme.SubHeaderFont, ForeColor = UITheme.Success, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblClosingStatus);
            y += 40;

            // Stats Cards
            lblExpectedCash = new Label { Text = "الكاش المتوقع في الدرج: 0.00 ج", Font = UITheme.BodyBoldFont, ForeColor = UITheme.BrandPrimary, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblExpectedCash);
            y += 30;

            lblVodafone = new Label { Text = "إجمالي فودافون كاش (للمطابقة اليدوية): 0.00 ج", Font = UITheme.BodyFont, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblVodafone);
            y += 25;

            lblInstaPay = new Label { Text = "إجمالي إنستا باي (للمطابقة اليدوية): 0.00 ج", Font = UITheme.BodyFont, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblInstaPay);
            y += 30;

            lblCountsSummary = new Label { Text = "أعداد الكشوفات: كشف: 0 | إعادة: 0 | مستعجل: 0 | مسترد: 0", Font = UITheme.SmallFont, ForeColor = UITheme.TextMuted, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblCountsSummary);
            y += 50;

            // Actual cash drawer input
            pnlCard.Controls.Add(new Label { Text = "أدخل النقدية الفعلية الموجودة في الدرج الآن (جنيه):", Location = new Point(0, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numActualCash = new NumericUpDown
            {
                Location = new Point(0, y + 24),
                Width = 260,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };
            numActualCash.ValueChanged += (s, e) => CalculateDifference();
            pnlCard.Controls.Add(numActualCash);
            y += 65;

            lblDifference = new Label { Text = "الفرق (عجز / زيادة): 0.00 ج", Font = UITheme.SubHeaderFont, ForeColor = Color.Black, Location = new Point(0, y), AutoSize = true };
            pnlCard.Controls.Add(lblDifference);
            y += 50;

            var flowClosingBtns = new FlowLayoutPanel
            {
                Location = new Point(0, y),
                Size = new Size(550, 48),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlCard.Controls.Add(flowClosingBtns);

            btnCloseDay = new Button { Text = "إقفال اليومية وقفل الحسابات", Width = 230, Height = 40, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnCloseDay);
            btnCloseDay.Click += BtnCloseDay_Click;
            flowClosingBtns.Controls.Add(btnCloseDay);

            btnReopenDay = new Button { Text = "إعادة فتح اليومية (للطبيب فقط)", Width = 230, Height = 40, Margin = new Padding(0, 0, 10, 0), Visible = SessionContext.IsDoctor };
            UITheme.StyleDangerButton(btnReopenDay);
            btnReopenDay.Click += BtnReopenDay_Click;
            flowClosingBtns.Controls.Add(btnReopenDay);

            LoadDailyClosingStats();
            return tab;
        }

        private void LoadDailyClosingStats()
        {
            var summary = _closingService.GetDailySummary(DateTime.Today);
            lblExpectedCash.Text = $"النقدية (الكاش) المتوقعة في الدرج: {summary.TotalCash:N2} جنيه";
            lblVodafone.Text = $"إجمالي فودافون كاش (للمطابقة اليدوية): {summary.TotalVodafoneCash:N2} جنيه";
            lblInstaPay.Text = $"إجمالي إنستا باي (للمطابقة اليدوية): {summary.TotalInstaPay:N2} جنيه";
            lblCountsSummary.Text = $"أعداد الكشوفات: كشف: {summary.TotalExamCount} | إعادة: {summary.TotalReExamCount} | مستعجل: {summary.TotalUrgentCount} | استرداد: {summary.RefundedCount} ({summary.RefundedAmount:N2} ج) | ما حضرش: {summary.NoShowCount}";

            if (summary.IsClosed)
            {
                lblClosingStatus.Text = "✓ تم إقفال حسابات هذا اليوم. لا يمكن تسجيل دفعات جديدة.";
                lblClosingStatus.ForeColor = UITheme.Danger;
                btnCloseDay.Enabled = false;
                numActualCash.Value = summary.ActualCashCounted ?? 0;
            }
            else
            {
                lblClosingStatus.Text = "حالة اليوم: مفتوح للتحصيل والعمل";
                lblClosingStatus.ForeColor = UITheme.Success;
                btnCloseDay.Enabled = true;
            }

            CalculateDifference();
        }

        private void CalculateDifference()
        {
            var summary = _closingService.GetDailySummary(DateTime.Today);
            decimal actual = numActualCash.Value;
            decimal diff = actual - summary.TotalCash;

            if (diff == 0)
            {
                lblDifference.Text = "الفرق: 0.00 ج (مطابق تماماً)";
                lblDifference.ForeColor = UITheme.Success;
            }
            else if (diff > 0)
            {
                lblDifference.Text = $"الفرق: +{diff:N2} ج (زيادة في الدرج)";
                lblDifference.ForeColor = UITheme.Warning;
            }
            else
            {
                lblDifference.Text = $"الفرق: {diff:N2} ج (عجز في الدرج)";
                lblDifference.ForeColor = UITheme.Danger;
            }
        }

        private void BtnCloseDay_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show("هل أنت متأكد من إقفال حسابات اليوم؟ بعد الإقفال لن يمكن تسجيل كشوفات أو دفعات جديدة لهذا اليوم إلا بواسطة الطبيب.", "تأكيد الإقفال", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) return;

            var res = _closingService.CloseDay(DateTime.Today, numActualCash.Value);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Automated Backup upon Daily Closing (PRD BKP-1)
            var backupRes = _backupService.CreateBackup();
            if (backupRes.IsSuccess)
            {
                MessageBox.Show(
                    string.Format("تم إقفال حسابات اليوم وأخذ نسخة احتياطية تلقائية لقاعدة البيانات بنجاح.\n\nاسم النسخة: {0}\nالمسار: {1}", backupRes.FileName, backupRes.BackupFilePath),
                    "تم الإقفال والنسخ الاحتياطي التلقائي",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "تم إقفال حسابات اليوم ولكن تعذر إنشاء النسخة الاحتياطية التلقائية:\n" + backupRes.ErrorMessage,
                    "تنبيه النسخ الاحتياطي",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            LoadDailyClosingStats();
        }

        private void BtnReopenDay_Click(object sender, EventArgs e)
        {
            string reason = PromptInput("يرجى كتابة سبب إعادة فتح اليومية المقفلة:", "إعادة فتح اليومية");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var res = _closingService.ReopenDay(DateTime.Today, reason);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("تمت إعادة فتح اليومية بنجاح وتسجيل الحركة في سجل المراجعة.", "تم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadDailyClosingStats();
        }

        private string PromptInput(string prompt, string title)
        {
            using (var form = new Form())
            {
                UITheme.ApplyFormDefaults(form);
                form.Text = title;
                form.Size = new Size(420, 200);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = prompt, Location = new Point(20, 20), AutoSize = true, Font = UITheme.BodyBoldFont };
                var txt = new TextBox { Location = new Point(20, 50), Width = 360, Font = UITheme.BodyFont };
                var btnOk = new Button { Text = "تأكيد", Location = new Point(20, 100), Width = 100, Height = 34, DialogResult = DialogResult.OK };
                UITheme.StylePrimaryButton(btnOk);
                var btnCancel = new Button { Text = "إلغاء", Location = new Point(130, 100), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
                UITheme.StyleSecondaryButton(btnCancel);

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                return form.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }

        #endregion

        #region Tab 5: Settings & Calibration (Doctor Only)

        private TabPage BuildSettingsTab()
        {
            tabSettings = new TabPage { Text = "  الإعدادات والمعايرة  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tabSettings);

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(25), BackColor = Color.White, RightToLeft = RightToLeft.Yes };
            tabSettings.Controls.Add(pnl);

            var pnlCard = new Panel
            {
                Location = new Point(pnl.Width > 800 ? pnl.Width - 810 : 25, 20),
                Width = 780,
                Height = 500,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(pnlCard);
            pnl.Resize += (s, e) =>
            {
                if (pnl.ClientSize.Width > 820)
                    pnlCard.Left = pnl.ClientSize.Width - pnlCard.Width - 30;
                else
                    pnlCard.Left = 25;
            };

            int y = 0;
            pnlCard.Controls.Add(new Label { Text = "بيانات العيادة والأسعار الافتراضية:", Font = UITheme.SubHeaderFont, ForeColor = UITheme.BrandPrimary, Location = new Point(25, y), AutoSize = true });
            y += 40;

            // Row 1: Clinic Name (Right, X=410) & Doctor Name (Left, X=25)
            pnlCard.Controls.Add(new Label { Text = "اسم العيادة:", Location = new Point(410, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtClinicName = new TextBox { Location = new Point(410, y + 22), Width = 360, Font = UITheme.BodyFont };
            pnlCard.Controls.Add(txtClinicName);

            pnlCard.Controls.Add(new Label { Text = "اسم الطبيب:", Location = new Point(25, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtDoctorName = new TextBox { Location = new Point(25, y + 22), Width = 360, Font = UITheme.BodyFont };
            pnlCard.Controls.Add(txtDoctorName);
            y += 55;

            // Row 2: Phone (Right, X=410) & Address (Left, X=25)
            pnlCard.Controls.Add(new Label { Text = "هاتف العيادة:", Location = new Point(410, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtPhone = new TextBox { Location = new Point(410, y + 22), Width = 360, Font = UITheme.BodyFont };
            pnlCard.Controls.Add(txtPhone);

            pnlCard.Controls.Add(new Label { Text = "عنوان العيادة:", Location = new Point(25, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            txtAddress = new TextBox { Location = new Point(25, y + 22), Width = 360, Font = UITheme.BodyFont };
            pnlCard.Controls.Add(txtAddress);
            y += 65;

            // Pricing: Exam (Right, X=580), ReExam (X=395), Urgent (X=210), Days (Left, X=25)
            pnlCard.Controls.Add(new Label { Text = "سعر الكشف (ج):", Location = new Point(580, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numPriceExam = new NumericUpDown { Location = new Point(580, y + 22), Width = 160, Maximum = 10000 };
            pnlCard.Controls.Add(numPriceExam);

            pnlCard.Controls.Add(new Label { Text = "سعر الإعادة (ج):", Location = new Point(395, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numPriceReExam = new NumericUpDown { Location = new Point(395, y + 22), Width = 160, Maximum = 10000 };
            pnlCard.Controls.Add(numPriceReExam);

            pnlCard.Controls.Add(new Label { Text = "سعر المستعجل (ج):", Location = new Point(210, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numPriceUrgent = new NumericUpDown { Location = new Point(210, y + 22), Width = 160, Maximum = 10000 };
            pnlCard.Controls.Add(numPriceUrgent);

            pnlCard.Controls.Add(new Label { Text = "أيام الإعادة المسموحة:", Location = new Point(25, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numReExamDays = new NumericUpDown { Location = new Point(25, y + 22), Width = 160, Maximum = 365, Value = 14 };
            pnlCard.Controls.Add(numReExamDays);
            y += 75;

            // Print Calibration Section
            pnlCard.Controls.Add(new Label { Text = "معايرة طباعة الروشتة المخصوصة (ورق 16.7 × 24 سم):", Font = UITheme.SubHeaderFont, ForeColor = UITheme.BrandPrimary, Location = new Point(25, y), AutoSize = true });
            y += 35;

            pnlCard.Controls.Add(new Label { Text = "إزاحة أفقية X (مليمتر):", Location = new Point(580, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numOffsetX = new NumericUpDown { Location = new Point(580, y + 22), Width = 160, Minimum = -50, Maximum = 50, DecimalPlaces = 1 };
            pnlCard.Controls.Add(numOffsetX);

            pnlCard.Controls.Add(new Label { Text = "إزاحة رأسية Y (مليمتر):", Location = new Point(395, y), AutoSize = true, Font = UITheme.BodyBoldFont });
            numOffsetY = new NumericUpDown { Location = new Point(395, y + 22), Width = 160, Minimum = -50, Maximum = 50, DecimalPlaces = 1 };
            pnlCard.Controls.Add(numOffsetY);

            btnTestPrint = new Button { Text = "طباعة تجريبية للمعايرة", Location = new Point(25, y + 18), Width = 220, Height = 34 };
            UITheme.StyleSecondaryButton(btnTestPrint);
            btnTestPrint.Click += BtnTestPrint_Click;
            pnlCard.Controls.Add(btnTestPrint);
            y += 85;

            var flowSettingsBtn = new FlowLayoutPanel
            {
                Location = new Point(25, y),
                Size = new Size(720, 50),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlCard.Controls.Add(flowSettingsBtn);

            btnSaveSettings = new Button { Text = "حفظ وتحديث الإعدادات", Width = 220, Height = 38, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnSaveSettings);
            btnSaveSettings.Click += BtnSaveSettings_Click;
            flowSettingsBtn.Controls.Add(btnSaveSettings);

            // Backup & Restore Card
            var pnlBackupCard = new Panel
            {
                Location = new Point(pnl.Width > 800 ? pnl.Width - 810 : 25, 530),
                Width = 780,
                Height = 370,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(pnlBackupCard);

            pnl.Resize += (s, e) =>
            {
                if (pnl.ClientSize.Width > 820)
                {
                    pnlCard.Left = pnl.ClientSize.Width - pnlCard.Width - 30;
                    pnlBackupCard.Left = pnl.ClientSize.Width - pnlBackupCard.Width - 30;
                }
                else
                {
                    pnlCard.Left = 25;
                    pnlBackupCard.Left = 25;
                }
            };

            int by = 15;
            pnlBackupCard.Controls.Add(new Label
            {
                Text = "النسخ الاحتياطي وحماية قاعدة البيانات (SQL Server 2014):",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(25, by),
                AutoSize = true
            });
            by += 35;

            lblBackupPath = new Label
            {
                Text = "مجلد الحفظ: " + _backupService.GetDefaultBackupDirectory(),
                Font = UITheme.SmallFont,
                ForeColor = UITheme.TextMuted,
                Location = new Point(190, by + 6),
                Size = new Size(560, 22),
                AutoEllipsis = true
            };
            pnlBackupCard.Controls.Add(lblBackupPath);

            var btnChangeBackupPath = new Button
            {
                Text = "تغيير المسار",
                Location = new Point(25, by),
                Width = 140,
                Height = 30
            };
            UITheme.StyleSecondaryButton(btnChangeBackupPath);
            btnChangeBackupPath.Click += BtnChangeBackupPath_Click;
            pnlBackupCard.Controls.Add(btnChangeBackupPath);
            by += 45;

            var flowBackupBtns = new FlowLayoutPanel
            {
                Location = new Point(25, by),
                Size = new Size(720, 45),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                BackColor = Color.Transparent
            };
            pnlBackupCard.Controls.Add(flowBackupBtns);

            var btnBackupNow = new Button
            {
                Text = "💾 أخذ نسخة احتياطية الآن",
                Width = 200,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StylePrimaryButton(btnBackupNow);
            btnBackupNow.Click += BtnBackupNow_Click;
            flowBackupBtns.Controls.Add(btnBackupNow);

            var btnUsbBackup = new Button
            {
                Text = "⎘ أخذ نسخة على فلاشة USB",
                Width = 210,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0)
            };
            UITheme.StyleSuccessButton(btnUsbBackup);
            btnUsbBackup.Click += BtnUsbBackup_Click;
            flowBackupBtns.Controls.Add(btnUsbBackup);

            var btnRestoreBackup = new Button
            {
                Text = "⚠ استرجاع نسخة محددة (للطبيب فقط)",
                Width = 240,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0),
                Visible = SessionContext.IsDoctor
            };
            UITheme.StyleDangerButton(btnRestoreBackup);
            btnRestoreBackup.Click += BtnRestoreBackup_Click;
            flowBackupBtns.Controls.Add(btnRestoreBackup);
            by += 50;

            pnlBackupCard.Controls.Add(new Label
            {
                Text = "أحدث النسخ الاحتياطية المتوفرة (الاحتفاظ التلقائي بآخر 30 نسخة):",
                Font = UITheme.BodyBoldFont,
                Location = new Point(25, by),
                AutoSize = true
            });
            by += 25;

            dgvRecentBackups = new DataGridView
            {
                Location = new Point(25, by),
                Size = new Size(720, 160),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            UITheme.StyleDataGridView(dgvRecentBackups);
            dgvRecentBackups.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var bName = new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "اسم الملف", FillWeight = 45 };
            var bDate = new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "تاريخ ووقت النسخ", FillWeight = 30 };
            var bSize = new DataGridViewTextBoxColumn { Name = "SizeDisplay", HeaderText = "حجم الملف", FillWeight = 25 };
            var bPath = new DataGridViewTextBoxColumn { Name = "FilePath", HeaderText = "المسار الكامل", Visible = false };
            dgvRecentBackups.Columns.AddRange(new DataGridViewColumn[] { bName, bDate, bSize, bPath });
            pnlBackupCard.Controls.Add(dgvRecentBackups);

            // User Management & Security Card (Doctor Only)
            var pnlUserCard = new Panel
            {
                Location = new Point(pnl.Width > 800 ? pnl.Width - 810 : 25, 915),
                Width = 780,
                Height = 310,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(pnlUserCard);

            pnl.Resize += (s, e) =>
            {
                if (pnl.ClientSize.Width > 820)
                {
                    pnlUserCard.Left = pnl.ClientSize.Width - pnlUserCard.Width - 30;
                }
                else
                {
                    pnlUserCard.Left = 25;
                }
            };

            pnlUserCard.Controls.Add(new Label
            {
                Text = "إدارة المستخدمين والأمان (بيانات حساب السكرتارية وكلمات المرور):",
                Font = UITheme.SubHeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(25, 15),
                AutoSize = true
            });

            // Row 1: Secretary Name & Username
            pnlUserCard.Controls.Add(new Label
            {
                Text = "اسم موظف/ة الاستقبال (يظهر أعلى البرنامج):",
                Font = UITheme.BodyBoldFont,
                Location = new Point(410, 48),
                AutoSize = true
            });
            txtSecFullName = new TextBox
            {
                Location = new Point(410, 72),
                Width = 350,
                Font = UITheme.BodyFont
            };
            pnlUserCard.Controls.Add(txtSecFullName);

            pnlUserCard.Controls.Add(new Label
            {
                Text = "اسم المستخدم لتسجيل الدخول (Username):",
                Font = UITheme.BodyBoldFont,
                Location = new Point(25, 48),
                AutoSize = true
            });
            txtSecUsername = new TextBox
            {
                Location = new Point(25, 72),
                Width = 350,
                Font = UITheme.BodyFont
            };
            pnlUserCard.Controls.Add(txtSecUsername);

            // Row 2: New Secretary Password & Save Button
            pnlUserCard.Controls.Add(new Label
            {
                Text = "كلمة المرور الجديدة للسكرتارية (اتركه فارغاً للإبقاء على الحالية):",
                Font = UITheme.BodyBoldFont,
                Location = new Point(410, 108),
                AutoSize = true
            });
            txtNewSecPass = new TextBox
            {
                Location = new Point(410, 132),
                Width = 350,
                Font = UITheme.BodyFont,
                PasswordChar = '●'
            };
            pnlUserCard.Controls.Add(txtNewSecPass);

            btnSaveSecAccount = new Button
            {
                Text = "💾 حفظ وتحديث بيانات حساب السكرتارية",
                Location = new Point(25, 128),
                Width = 350,
                Height = 36
            };
            UITheme.StylePrimaryButton(btnSaveSecAccount);
            btnSaveSecAccount.Click += (s, e) =>
            {
                string newName = txtSecFullName.Text.Trim();
                string newUsername = txtSecUsername.Text.Trim();
                string newPass = txtNewSecPass.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("يرجى إدخال اسم موظف/ة الاستقبال.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3)
                {
                    MessageBox.Show("يرجى إدخال اسم مستخدم صحيح لا يقل عن 3 أحرف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(newPass) && newPass.Length < 6)
                {
                    MessageBox.Show("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
                {
                    var secUser = db.Users.FirstOrDefault(u => u.Role == EyeClinic.Core.Enums.UserRole.Secretary);
                    if (secUser == null)
                    {
                        MessageBox.Show("حساب السكرتارية غير مسجل في قاعدة البيانات.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var res = _authService.UpdateUserAccount(secUser.Id, newUsername, newName, string.IsNullOrWhiteSpace(newPass) ? null : newPass);
                    if (res.IsSuccess)
                    {
                        txtNewSecPass.Clear();
                        MessageBox.Show(
                            string.Format("تم حفظ وتحديث بيانات حساب السكرتارية بنجاح!\n\n• الاسم المعروض: {0}\n• اسم المستخدم: {1}{2}",
                                newName,
                                newUsername,
                                string.IsNullOrWhiteSpace(newPass) ? "" : "\n• تم تحديث كلمة المرور بنجاح"),
                            "تم الحفظ بنجاح",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        LoadHeaderInfo();
                    }
                    else
                    {
                        MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            pnlUserCard.Controls.Add(btnSaveSecAccount);

            // Row 3: Doctor Password Change Section
            pnlUserCard.Controls.Add(new Label
            {
                Text = "تغيير كلمة مرور حساب الطبيب / المدير الحالي:",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(25, 180),
                AutoSize = true
            });

            pnlUserCard.Controls.Add(new Label
            {
                Text = "كلمة المرور الحالية:",
                Font = UITheme.SmallFont,
                Location = new Point(595, 206),
                AutoSize = true
            });
            txtDocCurrentPass = new TextBox
            {
                Location = new Point(595, 226),
                Width = 165,
                Font = UITheme.BodyFont,
                PasswordChar = '●'
            };
            pnlUserCard.Controls.Add(txtDocCurrentPass);

            pnlUserCard.Controls.Add(new Label
            {
                Text = "كلمة المرور الجديدة:",
                Font = UITheme.SmallFont,
                Location = new Point(410, 206),
                AutoSize = true
            });
            txtDocNewPass = new TextBox
            {
                Location = new Point(410, 226),
                Width = 175,
                Font = UITheme.BodyFont,
                PasswordChar = '●'
            };
            pnlUserCard.Controls.Add(txtDocNewPass);

            btnChangeDocPass = new Button
            {
                Text = "🔑 تحديث كلمة مرور الطبيب",
                Location = new Point(25, 222),
                Width = 350,
                Height = 34
            };
            UITheme.StyleSecondaryButton(btnChangeDocPass);
            btnChangeDocPass.Click += (s, e) =>
            {
                string currPass = txtDocCurrentPass.Text;
                string newPass = txtDocNewPass.Text;

                if (string.IsNullOrWhiteSpace(currPass))
                {
                    MessageBox.Show("يرجى إدخال كلمة المرور الحالية للطبيب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
                {
                    MessageBox.Show("يجب ألا تقل كلمة المرور الجديدة عن 6 أحرف أو أرقام.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = _authService.ChangePassword(SessionContext.CurrentUserId, currPass, newPass);
                if (res.IsSuccess)
                {
                    txtDocCurrentPass.Clear();
                    txtDocNewPass.Clear();
                    MessageBox.Show("تم تغيير كلمة مرور الطبيب بنجاح.", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            pnlUserCard.Controls.Add(btnChangeDocPass);

            LoadSettingsData();
            LoadBackupList();
            return tabSettings;
        }

        private void LoadSettingsData()
        {
            var dict = _settingsService.GetAllSettings();
            if (dict.TryGetValue("Clinic_Name", out string v1)) txtClinicName.Text = v1;
            if (dict.TryGetValue("Clinic_DoctorName", out string v2)) txtDoctorName.Text = v2;
            if (dict.TryGetValue("Clinic_Phone", out string v3)) txtPhone.Text = v3;
            if (dict.TryGetValue("Clinic_Address", out string v4)) txtAddress.Text = v4;

            if (dict.TryGetValue("Price_Exam", out string p1) && decimal.TryParse(p1, out decimal d1)) numPriceExam.Value = d1;
            if (dict.TryGetValue("Price_ReExam", out string p2) && decimal.TryParse(p2, out decimal d2)) numPriceReExam.Value = d2;
            if (dict.TryGetValue("Price_Urgent", out string p3) && decimal.TryParse(p3, out decimal d3)) numPriceUrgent.Value = d3;
            if (dict.TryGetValue("ReExam_ValidDays", out string p4) && decimal.TryParse(p4, out decimal d4)) numReExamDays.Value = d4;

            if (dict.TryGetValue("Print_Rx_Offset_X_MM", out string ox) && decimal.TryParse(ox, out decimal dox)) numOffsetX.Value = dox;
            if (dict.TryGetValue("Print_Rx_Offset_Y_MM", out string oy) && decimal.TryParse(oy, out decimal doy)) numOffsetY.Value = doy;

            // Load Secretary Account Info
            using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
            {
                var secUser = db.Users.FirstOrDefault(u => u.Role == EyeClinic.Core.Enums.UserRole.Secretary);
                if (secUser != null)
                {
                    if (txtSecFullName != null) txtSecFullName.Text = secUser.FullName;
                    if (txtSecUsername != null) txtSecUsername.Text = secUser.Username;
                }
            }
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            var dict = new Dictionary<string, string>
            {
                ["Clinic_Name"] = txtClinicName.Text.Trim(),
                ["Clinic_DoctorName"] = txtDoctorName.Text.Trim(),
                ["Clinic_Phone"] = txtPhone.Text.Trim(),
                ["Clinic_Address"] = txtAddress.Text.Trim(),
                ["Price_Exam"] = numPriceExam.Value.ToString(),
                ["Price_ReExam"] = numPriceReExam.Value.ToString(),
                ["Price_Urgent"] = numPriceUrgent.Value.ToString(),
                ["ReExam_ValidDays"] = numReExamDays.Value.ToString(),
                ["Print_Rx_Offset_X_MM"] = numOffsetX.Value.ToString(),
                ["Print_Rx_Offset_Y_MM"] = numOffsetY.Value.ToString()
            };

            var res = _settingsService.SaveSettings(dict);
            if (res.IsSuccess)
            {
                lblHeaderClinic.Text = txtClinicName.Text.Trim();
                // Sync Doctor's FullName with Clinic_DoctorName in database
                using (var db = new EyeClinic.Data.Context.EyeClinicDbContext())
                {
                    var docUser = db.Users.FirstOrDefault(u => u.Role == EyeClinic.Core.Enums.UserRole.Doctor);
                    if (docUser != null)
                    {
                        docUser.FullName = txtDoctorName.Text.Trim();
                        db.SaveChanges();
                        if (SessionContext.IsDoctor && SessionContext.CurrentUser != null)
                        {
                            SessionContext.CurrentUser.FullName = docUser.FullName;
                        }
                    }
                }
                LoadHeaderInfo();
                MessageBox.Show("تم حفظ الإعدادات بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.ErrorMessage, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestPrint_Click(object sender, EventArgs e)
        {
            var testDto = new PrescriptionPrintDto
            {
                PatientName = "مريض تجريبي لمعايرة الطباعة",
                Age = "35",
                Diagnosis = "Test Calibration Diagnosis",
                Date = DateTime.Today,
                Medicines = new List<PrescriptionPrintItemDto>
                {
                    new PrescriptionPrintItemDto { MedicineName = "Tobradex Eye Drops", DoseInstructions = "نقطة 3 مرات يومياً لمدة أسبوع" },
                    new PrescriptionPrintItemDto { MedicineName = "Hyfresh Eye Drops", DoseInstructions = "نقطة عند اللزوم للترطيب" }
                }
            };

            var config = _printService.LoadCalibrationConfig();
            config.OffsetX_MM = (float)numOffsetX.Value;
            config.OffsetY_MM = (float)numOffsetY.Value;

            try
            {
                using (var doc = _printService.CreatePrescriptionPrintDocument(testDto, config))
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
                MessageBox.Show($"تعذرت الطباعة التجريبية:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Tab 6: Audit Log (Doctor Only)

        private TabPage BuildAuditTab()
        {
            tabAudit = new TabPage { Text = "  سجل المراجعة والتدقيق  ", BackColor = UITheme.Background };
            tabMain.TabPages.Add(tabAudit);

            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(15, 15, 15, 15),
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.Yes,
                WrapContents = false
            };
            tabAudit.Controls.Add(pnlTop);

            pnlTop.Controls.Add(new Label { Text = "من تاريخ:", AutoSize = true, Font = UITheme.BodyBoldFont, Margin = new Padding(0, 6, 8, 0) });
            dtpAuditFrom = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7), Margin = new Padding(0, 3, 12, 0) };
            pnlTop.Controls.Add(dtpAuditFrom);

            pnlTop.Controls.Add(new Label { Text = "إلى تاريخ:", AutoSize = true, Font = UITheme.BodyBoldFont, Margin = new Padding(0, 6, 8, 0) });
            dtpAuditTo = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 3, 15, 0) };
            pnlTop.Controls.Add(dtpAuditTo);

            var btnFilter = new Button { Text = "تصفية السجل", Width = 130, Height = 34, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StylePrimaryButton(btnFilter);
            btnFilter.Click += (s, e) => LoadAuditGrid();
            pnlTop.Controls.Add(btnFilter);

            var btnExportAudit = new Button { Text = "📊 تصدير Excel", Width = 130, Height = 34, Margin = new Padding(0, 0, 10, 0) };
            UITheme.StyleSecondaryButton(btnExportAudit);
            btnExportAudit.Click += (s, e) => ExportGrid(dgvAudit, "سجل_المراجعة_والتدقيق");
            pnlTop.Controls.Add(btnExportAudit);

            dgvAudit = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGridView(dgvAudit);
            dgvAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabAudit.Controls.Add(dgvAudit);
            dgvAudit.BringToFront(); // Crucial: sets z-order so dgvAudit sits below pnlTop!

            var aTime = new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "الوقت والتاريخ", FillWeight = 80 };
            var aUser = new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "المستخدم", FillWeight = 60 };
            var aType = new DataGridViewTextBoxColumn { Name = "ActionType", HeaderText = "نوع الحركة", FillWeight = 65 };
            var aEntity = new DataGridViewTextBoxColumn { Name = "Entity", HeaderText = "الجدول", FillWeight = 55 };
            var aDesc = new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "تفاصيل التغيير", FillWeight = 240 };

            dgvAudit.Columns.AddRange(new DataGridViewColumn[] { aTime, aUser, aType, aEntity, aDesc });

            LoadAuditGrid();
            return tabAudit;
        }

        private void LoadAuditGrid()
        {
            var logs = _auditService.GetLogs(dtpAuditFrom.Value, dtpAuditTo.Value, null, 150);
            dgvAudit.Rows.Clear();
            foreach (var l in logs)
            {
                dgvAudit.Rows.Add(l.Timestamp.ToString("dd/MM/yyyy hh:mm tt"), l.Username, l.ActionType, l.EntityName, l.Description);
            }
        }

        #endregion

        #region Timers & Inactivity

        private void SetupTimers()
        {
            // Auto-refresh queue every 4 seconds (only for today)
            _queueRefreshTimer = new Timer { Interval = 4000 };
            _queueRefreshTimer.Tick += (s, e) =>
            {
                if (tabMain.SelectedTab == tabMain.TabPages[0] && (dtpBookingDate == null || dtpBookingDate.Value.Date == DateTime.Today))
                {
                    RefreshQueue();
                }
            };
            _queueRefreshTimer.Start();

            // Inactivity monitor
            _inactivityTimer = new Timer { Interval = 30000 }; // check every 30s
            _inactivityTimer.Tick += InactivityTimer_Tick;
            _inactivityTimer.Start();
        }

        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            if (!SessionContext.IsLoggedIn) return;

            int timeoutMinutes = SessionContext.IsDoctor ? 10 : 30;
            var setting = _settingsService.GetSetting(SessionContext.IsDoctor ? "InactivityTimeout_DoctorMinutes" : "InactivityTimeout_SecretaryMinutes");
            if (int.TryParse(setting, out int parsed) && parsed > 0)
            {
                timeoutMinutes = parsed;
            }

            var elapsed = DateTime.Now - SessionContext.LastActivityTime;
            if (elapsed.TotalMinutes >= timeoutMinutes)
            {
                _inactivityTimer.Stop();
                MessageBox.Show("تم تسجيل الخروج تلقائياً لعدم النشاط وحماية البيانات.", "خروج تلقائي", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logout();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_KEYDOWN = 0x0100;
            const int WM_LBUTTONDOWN = 0x0201;

            if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_KEYDOWN || m.Msg == WM_LBUTTONDOWN)
            {
                SessionContext.LastActivityTime = DateTime.Now;
            }

            base.WndProc(ref m);
        }

        private void LoadHeaderInfo()
        {
            lblHeaderClinic.Text = _settingsService.GetSetting("Clinic_Name", "عيادة العيون التخصصية");
            string roleArabic = SessionContext.IsDoctor ? "طبيب" : "استقبال / سكرتارية";
            string displayName = SessionContext.CurrentUser?.FullName ?? SessionContext.CurrentUsername;
            lblHeaderUser.Text = $"المستخدم: {displayName} ({roleArabic})";
        }

        private void Logout()
        {
            _queueRefreshTimer?.Stop();
            _inactivityTimer?.Stop();
            new AuthService().Logout();
            this.Close();
        }

        #region Backup and Export Handlers

        private void LoadBackupList()
        {
            if (dgvRecentBackups == null) return;
            dgvRecentBackups.Rows.Clear();
            var list = _backupService.GetExistingBackups();
            foreach (var b in list)
            {
                dgvRecentBackups.Rows.Add(b.FileName, b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), b.SizeDisplay, b.FilePath);
            }
        }

        private void BtnChangeBackupPath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "اختر المجلد المفضل لحفظ النسخ الاحتياطية:";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var res = _backupService.SetBackupDirectory(fbd.SelectedPath);
                    if (res.IsSuccess)
                    {
                        lblBackupPath.Text = "مجلد الحفظ: " + fbd.SelectedPath;
                        LoadBackupList();
                        MessageBox.Show("تم تحديث مسار النسخ الاحتياطي بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(res.ErrorMessage, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void BtnBackupNow_Click(object sender, EventArgs e)
        {
            var res = _backupService.CreateBackup();
            if (res.IsSuccess)
            {
                LoadBackupList();
                MessageBox.Show(
                    string.Format("تم إنشاء النسخة الاحتياطية لقاعدة البيانات بنجاح في {0:N1} ثانية.\n\nاسم الملف: {1}\nالمسار: {2}", res.Duration.TotalSeconds, res.FileName, res.BackupFilePath),
                    "نجاح النسخ الاحتياطي",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                MessageBox.Show(res.ErrorMessage, "خطأ في النسخ الاحتياطي", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUsbBackup_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "اختر مجلد الفلاشة الخارجية (USB) لحفظ نسخة احتياطية عليها:";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var res = _backupService.CreateBackup(fbd.SelectedPath);
                    if (res.IsSuccess)
                    {
                        _backupService.MarkWeeklyFlashReminderAcknowledged();
                        MessageBox.Show(
                            string.Format("تم حفظ النسخة الاحتياطية بنجاح على الفلاشة الخارجية:\n{0}", res.BackupFilePath),
                            "نجاح النسخ على فلاشة USB",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(res.ErrorMessage, "خطأ في النسخ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            if (!SessionContext.IsDoctor)
            {
                MessageBox.Show("استرجاع قاعدة البيانات مقتصر على الطبيب/المدير فقط.", "صلاحيات غير كافية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvRecentBackups.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار ملف نسخة احتياطية من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filePath = dgvRecentBackups.SelectedRows[0].Cells["FilePath"].Value != null
                ? dgvRecentBackups.SelectedRows[0].Cells["FilePath"].Value.ToString()
                : null;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("الملف المحدد غير موجود على القرص.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileName = Path.GetFileName(filePath);
            var confirm = MessageBox.Show(
                string.Format("تحذير هام:\nهل أنت متأكد تماماً من استرجاع قاعدة البيانات من الملف:\n{0}؟\n\nتنبيه: سيتم استبدال البيانات الحالية بالبيانات الموجودة داخل هذه النسخة الاحتياطية.", fileName),
                "تأكيد استرجاع قاعدة البيانات",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm == DialogResult.Yes)
            {
                var res = _backupService.RestoreBackup(filePath);
                if (res.IsSuccess)
                {
                    MessageBox.Show(
                        "تم استرجاع قاعدة البيانات بنجاح.\nيرجى إعادة تشغيل البرنامج لتحديث البيانات المعروضة في كافة الشاشات.",
                        "استرجاع ناجح",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(res.ErrorMessage, "خطأ في استرجاع قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportGrid(DataGridView dgv, string defaultFileName)
        {
            if (dgv == null || dgv.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات متاحة في الجدول للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "ملف Excel / CSV (*.csv)|*.csv";
                sfd.FileName = string.Format("{0}_{1:yyyyMMdd_HHmm}.csv", defaultFileName, DateTime.Now);
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var res = _exportService.ExportDataGridViewToCsv(dgv, sfd.FileName);
                    if (res.IsSuccess)
                    {
                        var ask = MessageBox.Show(
                            "تم تصدير البيانات بنجاح إلى ملف Excel / CSV.\nهل ترغب في فتح الملف الآن؟",
                            "نجاح التصدير",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );
                        if (ask == DialogResult.Yes)
                        {
                            try { Process.Start(sfd.FileName); } catch { }
                        }
                    }
                    else
                    {
                        MessageBox.Show(res.ErrorMessage, "خطأ في التصدير", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
