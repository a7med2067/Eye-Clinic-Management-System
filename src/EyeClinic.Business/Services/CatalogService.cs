using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EyeClinic.Core.Entities;
using EyeClinic.Data.Context;

namespace EyeClinic.Business.Services
{
    public class CatalogService
    {
        // In-memory cache for fast responsive autocompletion
        private static List<string> _cachedMedicines;
        private static Dictionary<string, string> _cachedDosesMap;
        private static List<string> _cachedDosageList;
        private static List<string> _cachedDiagnoses;
        private static List<string> _cachedAddresses;

        private static readonly object _syncLock = new object();

        public CatalogService()
        {
            EnsureCatalogsSeeded();
        }

        public void EnsureCatalogsSeeded()
        {
            lock (_syncLock)
            {
                using (var db = new EyeClinicDbContext())
                {
                    // 1. Seed Ophthalmic Medications
                    int medCount = db.MedicineCatalogs.Count();
                    if (medCount < 10)
                    {
                        var standardMeds = GetDefaultOphthalmicMedications();
                        foreach (var m in standardMeds)
                        {
                            if (!db.MedicineCatalogs.Any(c => c.MedicineName == m.MedicineName))
                            {
                                db.MedicineCatalogs.Add(m);
                            }
                        }
                        db.SaveChanges();
                    }

                    // 2. Seed Diagnoses
                    int diagCount = db.DiagnosisCatalogs.Count();
                    if (diagCount < 5)
                    {
                        var standardDiags = GetDefaultOphthalmicDiagnoses();
                        foreach (var d in standardDiags)
                        {
                            if (!db.DiagnosisCatalogs.Any(c => c.DiagnosisName == d.DiagnosisName))
                            {
                                db.DiagnosisCatalogs.Add(d);
                            }
                        }
                        db.SaveChanges();
                    }
                }

                RefreshCache();
            }
        }

        public void RefreshCache()
        {
            lock (_syncLock)
            {
                using (var db = new EyeClinicDbContext())
                {
                    var meds = db.MedicineCatalogs
                        .OrderByDescending(m => m.UsageCount)
                        .ThenBy(m => m.MedicineName)
                        .ToList();

                    _cachedMedicines = meds.Select(m => m.MedicineName).ToList();
                    _cachedDosesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var m in meds)
                    {
                        if (!string.IsNullOrWhiteSpace(m.DefaultDose) && !_cachedDosesMap.ContainsKey(m.MedicineName))
                        {
                            _cachedDosesMap[m.MedicineName] = m.DefaultDose;
                        }
                    }

                    _cachedDiagnoses = db.DiagnosisCatalogs
                        .OrderByDescending(d => d.UsageCount)
                        .ThenBy(d => d.DiagnosisName)
                        .Select(d => d.DiagnosisName)
                        .ToList();

                    // Distinct addresses from Patients + common towns
                    var patientAddresses = db.Patients
                        .Where(p => !string.IsNullOrEmpty(p.Address))
                        .Select(p => p.Address)
                        .Distinct()
                        .ToList();

                    var combinedAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var a in patientAddresses)
                    {
                        if (!string.IsNullOrWhiteSpace(a)) combinedAddresses.Add(a.Trim());
                    }

                    foreach (var town in GetDefaultTowns())
                    {
                        combinedAddresses.Add(town);
                    }

                    _cachedAddresses = combinedAddresses.OrderBy(a => a).ToList();

                    // Standard dosages list
                    var defaultDoses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var d in GetDefaultDosageInstructions())
                    {
                        defaultDoses.Add(d);
                    }
                    foreach (var d in meds.Select(m => m.DefaultDose).Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        defaultDoses.Add(d.Trim());
                    }
                    _cachedDosageList = defaultDoses.OrderBy(d => d).ToList();
                }
            }
        }

        public List<string> GetAllMedicineNames()
        {
            if (_cachedMedicines == null) RefreshCache();
            return new List<string>(_cachedMedicines);
        }

        public string GetDefaultDoseForMedicine(string medicineName)
        {
            if (string.IsNullOrWhiteSpace(medicineName)) return null;
            if (_cachedDosesMap == null) RefreshCache();

            string dose;
            if (_cachedDosesMap.TryGetValue(medicineName.Trim(), out dose))
            {
                return dose;
            }
            return null;
        }

        public List<string> GetAllDosageInstructions()
        {
            if (_cachedDosageList == null) RefreshCache();
            return new List<string>(_cachedDosageList);
        }

        public List<string> GetAllDiagnoses()
        {
            if (_cachedDiagnoses == null) RefreshCache();
            return new List<string>(_cachedDiagnoses);
        }

        public List<string> GetDistinctAddresses()
        {
            if (_cachedAddresses == null) RefreshCache();
            return new List<string>(_cachedAddresses);
        }

        public void SaveMedicineIfNew(string medicineName, string defaultDose)
        {
            if (string.IsNullOrWhiteSpace(medicineName)) return;
            string med = medicineName.Trim();

            using (var db = new EyeClinicDbContext())
            {
                var existing = db.MedicineCatalogs.FirstOrDefault(m => m.MedicineName == med);
                if (existing == null)
                {
                    db.MedicineCatalogs.Add(new MedicineCatalog
                    {
                        MedicineName = med,
                        DefaultDose = (defaultDose ?? "").Trim(),
                        UsageCount = 1,
                        LastUsedAt = DateTime.Now
                    });
                    db.SaveChanges();
                    RefreshCache();
                }
                else
                {
                    existing.UsageCount++;
                    existing.LastUsedAt = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(defaultDose))
                    {
                        existing.DefaultDose = defaultDose.Trim();
                    }
                    db.SaveChanges();
                }
            }
        }

        public void SaveDiagnosisIfNew(string diagnosisName)
        {
            if (string.IsNullOrWhiteSpace(diagnosisName)) return;
            string diag = diagnosisName.Trim();

            using (var db = new EyeClinicDbContext())
            {
                var existing = db.DiagnosisCatalogs.FirstOrDefault(d => d.DiagnosisName == diag);
                if (existing == null)
                {
                    db.DiagnosisCatalogs.Add(new DiagnosisCatalog
                    {
                        DiagnosisName = diag,
                        UsageCount = 1,
                        LastUsedAt = DateTime.Now
                    });
                    db.SaveChanges();
                    RefreshCache();
                }
                else
                {
                    existing.UsageCount++;
                    existing.LastUsedAt = DateTime.Now;
                    db.SaveChanges();
                }
            }
        }

        public void SaveAddressIfNew(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            string addr = address.Trim();

            lock (_syncLock)
            {
                if (_cachedAddresses != null && !_cachedAddresses.Contains(addr, StringComparer.OrdinalIgnoreCase))
                {
                    _cachedAddresses.Add(addr);
                    _cachedAddresses.Sort();
                }
            }
        }

        private List<MedicineCatalog> GetDefaultOphthalmicMedications()
        {
            return new List<MedicineCatalog>
            {
                new MedicineCatalog { MedicineName = "Tobradex Eye Drops", DefaultDose = "نقطة 4 مرات يومياً لمدة أسبوع", UsageCount = 10 },
                new MedicineCatalog { MedicineName = "Tobradex Eye Ointment", DefaultDose = "مرهم مساءً داخل العين قبل النوم", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Vigamox Eye Drops", DefaultDose = "نقطة 3 مرات يومياً لمدة أسبوع", UsageCount = 9 },
                new MedicineCatalog { MedicineName = "Tobrex Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Pred Forte 1% Eye Drops", DefaultDose = "نقطة 4 مرات يومياً ثم تقليل الجرعة تدريجياً", UsageCount = 9 },
                new MedicineCatalog { MedicineName = "Nevanac 0.1% Eye Drops", DefaultDose = "نقطة 3 مرات يومياً", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Hyfresh Eye Drops", DefaultDose = "نقطة 4 مرات يومياً للترطيب", UsageCount = 10 },
                new MedicineCatalog { MedicineName = "Systane Ultra Eye Drops", DefaultDose = "نقطة عند اللزوم للترطيب", UsageCount = 9 },
                new MedicineCatalog { MedicineName = "Refresh Tears Eye Drops", DefaultDose = "نقطة 3 إلى 4 مرات يومياً", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Polyfresh Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Solofresh Eye Drops", DefaultDose = "نقطة عند اللزوم للترطيب", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Patanol 0.1% Eye Drops", DefaultDose = "نقطة مرتين يومياً للحساسية", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Olopat Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Xalatan 0.005% Eye Drops", DefaultDose = "نقطة واحدة مساءً قبل النوم", UsageCount = 9 },
                new MedicineCatalog { MedicineName = "Lumigan 0.01% Eye Drops", DefaultDose = "نقطة واحدة مساءً", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Alphagan P 0.15% Eye Drops", DefaultDose = "نقطة مرتين يومياً (كل 12 ساعة)", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Cosopt Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 8 },
                new MedicineCatalog { MedicineName = "Timolol 0.5% Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Maxitrol Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Maxitrol Eye Ointment", DefaultDose = "مرهم داخل الجفن السفلي مساءً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Lotemax 0.5% Eye Drops", DefaultDose = "نقطة 3 مرات يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Dexaflox Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Zaditen 0.025% Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Moxiflox 0.5% Eye Drops", DefaultDose = "نقطة 3 مرات يومياً", UsageCount = 7 },
                new MedicineCatalog { MedicineName = "Tymer Eye Drops", DefaultDose = "نقطة 3 مرات يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Ciplox 0.3% Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 5 },
                new MedicineCatalog { MedicineName = "Terramycin Eye Ointment", DefaultDose = "مرهم داخل العين مساءً قبل النوم", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Mydriacyl 1% Eye Drops", DefaultDose = "نقطة مرتين بفارق 10 دقائق لتوسيع الحدقة", UsageCount = 5 },
                new MedicineCatalog { MedicineName = "Cyclopen 1% Eye Drops", DefaultDose = "نقطة 3 مرات يومياً", UsageCount = 5 },
                new MedicineCatalog { MedicineName = "Atropine 1% Eye Drops", DefaultDose = "نقطة واحدة يومياً حسب إرشاد الطبيب", UsageCount = 4 },
                new MedicineCatalog { MedicineName = "Voltarol Ophtha Eye Drops", DefaultDose = "نقطة 4 مرات يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Tears Naturale II", DefaultDose = "نقطة عند اللزوم", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Azopt 1% Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Travatan Eye Drops", DefaultDose = "نقطة واحدة مساءً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Combigan Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 6 },
                new MedicineCatalog { MedicineName = "Fucithalmic Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 5 },
                new MedicineCatalog { MedicineName = "Optive Eye Drops", DefaultDose = "نقطة 3 إلى 4 مرات يومياً", UsageCount = 5 },
                new MedicineCatalog { MedicineName = "Epina 0.05% Eye Drops", DefaultDose = "نقطة مرتين يومياً", UsageCount = 5 }
            };
        }

        private List<DiagnosisCatalog> GetDefaultOphthalmicDiagnoses()
        {
            return new List<DiagnosisCatalog>
            {
                new DiagnosisCatalog { DiagnosisName = "التهاب الملتحمة البكتيري (Bacterial Conjunctivitis)", UsageCount = 10 },
                new DiagnosisCatalog { DiagnosisName = "التهاب الملتحمة التحسسي (Allergic Conjunctivitis)", UsageCount = 10 },
                new DiagnosisCatalog { DiagnosisName = "جفاف العين المزمن (Dry Eye Syndrome)", UsageCount = 10 },
                new DiagnosisCatalog { DiagnosisName = "مياه بيضاء (Cataract)", UsageCount = 9 },
                new DiagnosisCatalog { DiagnosisName = "مياه بيضاء شيخوخية (Senile Cataract)", UsageCount = 8 },
                new DiagnosisCatalog { DiagnosisName = "مياه زرقاء - زرق مفتوح الزاوية (Primary Open Angle Glaucoma)", UsageCount = 8 },
                new DiagnosisCatalog { DiagnosisName = "مياه زرقاء مغلقة الزاوية (Angle Closure Glaucoma)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "قرحة بالقرنية (Corneal Ulcer)", UsageCount = 8 },
                new DiagnosisCatalog { DiagnosisName = "كيس دهني بالجفن (Chalazion)", UsageCount = 9 },
                new DiagnosisCatalog { DiagnosisName = "دمل بالجفن / شحات العين (Hordeolum / Stye)", UsageCount = 8 },
                new DiagnosisCatalog { DiagnosisName = "اعتلال الشبكية السكري (Diabetic Retinopathy)", UsageCount = 9 },
                new DiagnosisCatalog { DiagnosisName = "ارتشاح مركز الإبصار السكري (Diabetic Macular Edema)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "انسداد الوريد الشبكي (Retinal Vein Occlusion)", UsageCount = 6 },
                new DiagnosisCatalog { DiagnosisName = "انفصال شبكي (Retinal Detachment)", UsageCount = 6 },
                new DiagnosisCatalog { DiagnosisName = "خطأ انكساري - قصر نظر (Myopia)", UsageCount = 10 },
                new DiagnosisCatalog { DiagnosisName = "خطأ انكساري - طول نظر (Hyperopia)", UsageCount = 9 },
                new DiagnosisCatalog { DiagnosisName = "خطأ انكساري - استجماتيزم (Astigmatism)", UsageCount = 9 },
                new DiagnosisCatalog { DiagnosisName = "طول النظر الشيخوخي (Presbyopia)", UsageCount = 8 },
                new DiagnosisCatalog { DiagnosisName = "ظفرة العين (Pterygium)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "نزيف تحت الملتحمة (Subconjunctival Hemorrhage)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "خدش بالقرنية (Corneal Abrasion)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "جسم غريب بالقرنية (Corneal Foreign Body)", UsageCount = 7 },
                new DiagnosisCatalog { DiagnosisName = "التهاب القزحية (Anterior Uveitis)", UsageCount = 6 },
                new DiagnosisCatalog { DiagnosisName = "كسل العين (Amblyopia)", UsageCount = 6 },
                new DiagnosisCatalog { DiagnosisName = "حول إنسي (Esotropia)", UsageCount = 5 },
                new DiagnosisCatalog { DiagnosisName = "حول وحشي (Exotropia)", UsageCount = 5 },
                new DiagnosisCatalog { DiagnosisName = "شحيمة العين (Pinguecula)", UsageCount = 6 },
                new DiagnosisCatalog { DiagnosisName = "التهاب الجفون المزمن (Blepharitis)", UsageCount = 8 }
            };
        }

        private List<string> GetDefaultDosageInstructions()
        {
            return new List<string>
            {
                "نقطة 4 مرات يومياً لمدة أسبوع",
                "نقطة 3 مرات يومياً",
                "نقطة كل ساعتين أول يومين ثم 4 مرات يومياً",
                "نقطة مرتين يومياً (صباحاً ومساءً)",
                "نقطة واحدة مساءً قبل النوم",
                "نقطة عند اللزوم للترطيب",
                "مرهم مساءً داخل العين قبل النوم",
                "مرهم 3 مرات يومياً داخل الجفن السفلي",
                "نقطة 5 مرات يومياً ثم تقليل الجرعة تدريجياً",
                "كمادات ماء دافئ لمدة 10 دقائق 3 مرات يومياً",
                "نقطة كل 4 ساعات",
                "نقطة بالتناوب مع القطرة الأخرى بفارق 10 دقائق",
                "قرص بعد الأكل مرة واحدة يومياً"
            };
        }

        private List<string> GetDefaultTowns()
        {
            return new List<string>
            {
                "طنطا",
                "المحلة الكبرى",
                "كفر الزيات",
                "زفتى",
                "السنطة",
                "بسيون",
                "قطور",
                "سمنود",
                "المنصورة",
                "شبين الكوم",
                "القاهرة",
                "الإسكندرية",
                "دمنهور",
                "بنها"
            };
        }
    }
}
