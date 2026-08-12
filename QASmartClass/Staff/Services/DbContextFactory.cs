using QASmartClass.Data;

namespace QASmartClass.Staff.Services
{
    public static class StaffDbFactory
    {
        public static AppDbContext Create() => new AppDbContext();
    }
}

