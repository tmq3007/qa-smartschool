using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows.Controls;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentTuitionPage : Page
    {
        private readonly AppDbContext _db;
        private readonly Student _student;

        public ParentTuitionPage(AppDbContext db, Student student)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                var tuition = _db.TuitionRecords.Where(t => t.StudentId == _student.Id).ToList();
                DgTuition.ItemsSource = tuition.Select(t => new
                {
                    t.Id,
                    t.StudentId,
                    t.StudentName,
                    t.ClassName,
                    t.Amount,
                    t.DueDate,
                    t.PaidDate,
                    Status = t.Status?.Trim()?.ToLower() switch
                    {
                        "paid" => "Đã thanh toán",
                        "unpaid" => "Chưa thanh toán",
                        "overdue" => "Quá hạn",
                        _ => t.Status
                    },
                    PaymentMethod = t.PaymentMethod?.Trim()?.ToLower() switch
                    {
                        "cash" => "Tiền mặt",
                        "transfer" => "Chuyển khoản",
                        _ => t.PaymentMethod
                    },
                    t.PaidAmount,
                    t.IsLocked
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("ParentTuition Load error: {Err}", ex.Message); }
        }
    }
}

