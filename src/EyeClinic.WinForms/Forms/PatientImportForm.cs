using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EyeClinic.Business.Services;
using EyeClinic.Core.Common;
using EyeClinic.Core.DTOs;
using EyeClinic.WinForms.Theme;

namespace EyeClinic.WinForms.Forms
{
    public class PatientImportForm : Form
    {
        private readonly PatientImportService _importService;
        private PatientImportPreviewResult _currentPreview;

        private Label lblSelectedFile;
        private CheckBox chkSkipDuplicates;
        private Label lblTotal;
        private Label lblValid;
        private Label lblDuplicates;
        private Label lblErrors;
        private DataGridView dgvPreview;
        private Button btnCommit;
        private Button btnExportErrors;

        public int ImportedCount { get; private set; }

        public PatientImportForm()
        {
            _importService = new PatientImportService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            UITheme.ApplyFormDefaults(this);
            this.Text = "استيراد المرضى من ملف خارجي (Excel / CSV) - للطبيب فقط";
            this.Size = new Size(950, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(850, 550);

            // 1. Top Panel (Header, File Selection, and Summary Counters)
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 144,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlTop);

            var lblHeader = new Label
            {
                Text = "استيراد قائمة المرضى من ملف خارجي (ملف Excel محفوظ بصيغة CSV):",
                Font = UITheme.HeaderFont,
                ForeColor = UITheme.BrandPrimary,
                Location = new Point(16, 12),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblHeader);

            var btnBrowse = new Button
            {
                Text = "📂 اختيار ملف Excel / CSV...",
                Location = new Point(16, 48),
                Width = 210,
                Height = 36
            };
            UITheme.StylePrimaryButton(btnBrowse);
            btnBrowse.Click += BtnBrowse_Click;
            pnlTop.Controls.Add(btnBrowse);

            chkSkipDuplicates = new CheckBox
            {
                Text = "تخطي السجلات الموجودة مسبقاً بنفس رقم الهاتف",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextDark,
                Checked = true,
                AutoSize = true,
                Location = new Point(245, 54)
            };
            pnlTop.Controls.Add(chkSkipDuplicates);

            lblSelectedFile = new Label
            {
                Text = "لم يتم اختيار ملف بعد (يدعم ملفات .csv المحفوظة من Excel)",
                Font = UITheme.SmallFont,
                ForeColor = UITheme.TextMuted,
                Location = new Point(540, 56),
                Size = new Size(380, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTop.Controls.Add(lblSelectedFile);

            // Summary Status Bar nested at bottom of pnlTop
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 8),
                FlowDirection = FlowDirection.RightToLeft,
                RightToLeft = RightToLeft.Yes,
                BorderStyle = BorderStyle.FixedSingle,
                WrapContents = false
            };
            pnlTop.Controls.Add(pnlSummary);

            lblTotal = new Label
            {
                Text = "إجمالي السطور: 0",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };
            pnlSummary.Controls.Add(lblTotal);

            lblValid = new Label
            {
                Text = "جاهز للاستيراد: 0",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Success,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };
            pnlSummary.Controls.Add(lblValid);

            lblDuplicates = new Label
            {
                Text = "مكرر في القاعدة: 0",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.BrandAccent,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };
            pnlSummary.Controls.Add(lblDuplicates);

            lblErrors = new Label
            {
                Text = "أخطاء بيانات: 0",
                Font = UITheme.BodyBoldFont,
                ForeColor = UITheme.Danger,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };
            pnlSummary.Controls.Add(lblErrors);

            // 3. Bottom Action Panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 10, 16, 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlBottom);

            btnCommit = new Button
            {
                Text = "💾 تنفيذ الاستيراد وحفظ المرضى",
                Width = 230,
                Height = 36,
                Dock = DockStyle.Right,
                Enabled = false
            };
            UITheme.StyleSuccessButton(btnCommit);
            btnCommit.Click += BtnCommit_Click;
            pnlBottom.Controls.Add(btnCommit);

            btnExportErrors = new Button
            {
                Text = "📊 تصدير تقرير الأخطاء",
                Width = 180,
                Height = 36,
                Dock = DockStyle.Right,
                Margin = new Padding(0, 0, 12, 0),
                Enabled = false
            };
            UITheme.StyleSecondaryButton(btnExportErrors);
            btnExportErrors.Click += BtnExportErrors_Click;
            pnlBottom.Controls.Add(btnExportErrors);

            var btnClose = new Button
            {
                Text = "إلغاء / إغلاق",
                Width = 120,
                Height = 36,
                Dock = DockStyle.Left
            };
            UITheme.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            // 4. DataGridView Preview
            dgvPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            UITheme.StyleDataGridView(dgvPreview);
            dgvPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.Controls.Add(dgvPreview);
            dgvPreview.BringToFront();

            ConfigureGridColumns();

            this.Load += (s, e) =>
            {
                if (!SessionContext.IsDoctor)
                {
                    MessageBox.Show("عذراً، ميزة استيراد المرضى متاحة فقط لحساب الطبيب / المدير.", "صلاحية غير كافية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                }
            };
        }

        private void ConfigureGridColumns()
        {
            dgvPreview.Columns.Clear();
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNumber", HeaderText = "السطر", FillWeight = 12 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 18 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "اسم المريض", FillWeight = 35 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mobile", HeaderText = "رقم الموبايل", FillWeight = 22 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gender", HeaderText = "النوع", FillWeight = 15 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "السن", FillWeight = 14 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "العنوان", FillWeight = 25 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ErrorMessage", HeaderText = "البيان / سبب الخطأ", FillWeight = 50 });
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "ملفات البيانات (*.csv;*.txt)|*.csv;*.txt|جميع الملفات (*.*)|*.*";
                ofd.Title = "اختر ملف المرضى للاستيراد";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    lblSelectedFile.Text = ofd.FileName;
                    LoadFilePreview(ofd.FileName);
                }
            }
        }

        private void LoadFilePreview(string filePath)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                _currentPreview = _importService.PreviewImport(filePath);

                dgvPreview.Rows.Clear();
                foreach (var r in _currentPreview.Rows)
                {
                    string status;
                    if (!r.IsValid) status = "❌ خطأ";
                    else if (r.IsDuplicateInDb) status = "⚠ مكرر بالهاتف";
                    else status = "✔ جاهز";

                    int rowIdx = dgvPreview.Rows.Add(
                        r.RowNumber,
                        status,
                        r.FullName,
                        r.Mobile,
                        r.Gender,
                        r.AgeYears.HasValue ? r.AgeYears.Value.ToString() : "-",
                        r.Address,
                        r.ErrorMessage
                    );

                    var row = dgvPreview.Rows[rowIdx];
                    if (!r.IsValid)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                        row.DefaultCellStyle.ForeColor = UITheme.Danger;
                    }
                    else if (r.IsDuplicateInDb)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 251, 235);
                        row.DefaultCellStyle.ForeColor = UITheme.BrandAccent;
                    }
                }

                lblTotal.Text = "إجمالي السطور: " + _currentPreview.TotalRows;
                lblValid.Text = "جاهز للاستيراد: " + _currentPreview.ValidRowsCount;
                lblDuplicates.Text = "مكرر في القاعدة: " + _currentPreview.DuplicateRowsCount;
                lblErrors.Text = "أخطاء بيانات: " + _currentPreview.ErrorRowsCount;

                int readyCount = _currentPreview.ValidRowsCount;
                if (!chkSkipDuplicates.Checked) readyCount += _currentPreview.DuplicateRowsCount;

                btnCommit.Enabled = readyCount > 0;
                btnExportErrors.Enabled = _currentPreview.ErrorRowsCount > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء قراءة الملف:\n" + ex.Message, "خطأ قراءة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnCommit_Click(object sender, EventArgs e)
        {
            if (_currentPreview == null || _currentPreview.Rows.Count == 0) return;

            bool skipDuplicates = chkSkipDuplicates.Checked;
            int countToImport = _currentPreview.ValidRowsCount;
            if (!skipDuplicates) countToImport += _currentPreview.DuplicateRowsCount;

            if (countToImport == 0)
            {
                MessageBox.Show("لا توجد سجلات صالحة للاستيراد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("هل أنت متأكد من استيراد {0} مريض إلى قاعدة بيانات العيادة؟\nسيتم توليد أرقام ملفات متسلسلة تلقائياً لكل مريض.", countToImport),
                "تأكيد الاستيراد",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            try
            {
                var res = _importService.CommitImport(_currentPreview.Rows, skipDuplicates, SessionContext.CurrentUserId);
                if (res.Success)
                {
                    ImportedCount = res.ImportedCount;
                    MessageBox.Show(
                        string.Format("تم بنجاح استيراد {0} مريض إلى قاعدة البيانات!", res.ImportedCount),
                        "تم الاستيراد بنجاح",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(res.ErrorMessage, "فشل الاستيراد", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnExportErrors_Click(object sender, EventArgs e)
        {
            if (_currentPreview == null) return;
            var errors = _currentPreview.Rows.Where(r => !r.IsValid).ToList();
            if (errors.Count == 0) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "ملف CSV (*.csv)|*.csv";
                sfd.FileName = string.Format("أخطاء_استيراد_المرضى_{0:yyyyMMdd_HHmm}.csv", DateTime.Now);
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var res = _importService.ExportErrorsToCsv(errors, sfd.FileName);
                    if (res.IsSuccess)
                    {
                        MessageBox.Show("تم تصدير تقرير الأخطاء بنجاح إلى:\n" + sfd.FileName, "نجاح التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(res.ErrorMessage, "خطأ تصدير", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
