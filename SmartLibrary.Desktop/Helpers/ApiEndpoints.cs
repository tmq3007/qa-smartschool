namespace SmartLibrary.Desktop.Helpers
{
    public static class ApiEndpoints
    {
        public const string Books = "/Books";
        public const string BookLookup = "/Books/lookup/";
        public const string PendingReviews = "/Reviews/pending";
        public const string CirculationStudentStatus = "/Circulation/student-status/";
        public const string CirculationBookStatus = "/Circulation/book-status/";
        public const string CheckoutReserved = "/Circulation/checkout-reserved";
        public const string LibraryConfig = "/Config";
        
        public const string CirculationReturn = "/Circulation/return";
        public const string CirculationLoan = "/Circulation/loan";
        public const string CirculationExtend = "/Circulation/extend";
        public const string CirculationPayFine = "/Circulation/pay-fine";
        public const string CirculationReportLost = "/Circulation/report-lost";
        public const string BookCopy = "/Books/copy/";
        public const string CirculationUnpaidFines = "/Circulation/unpaid-fines";
        public const string Reviews = "/Reviews";
        public const string Proposals = "/Proposals";
        public const string XpShopPendingCollections = "/XpShop/pending-collections";
        public const string XpShopCollect = "/XpShop/collect/";

        // Audiobook Approval
        public const string AudiobookPending = "/Audiobook/pending";
        public const string AudiobookApprove = "/Audiobook/approve/";

        // Usage & ESG Reports
        public const string ReportsDashboard = "/Reports/dashboard";
        public const string ReportsEsgMetrics = "/Reports/esg-metrics";
        public const string CirculationActiveLoans = "/Circulation/active-loans";
        public const string ReportsStreak = "/Reports/StudyAnalytics/streak";

        public const string FeaturedReviews = "/Reviews/featured";
        
        // Review helper methods
        public static string GetApproveReviewUrl(int reviewId) => $"{Reviews}/{reviewId}/approve";
        public static string GetApproveFeaturedReviewUrl(int reviewId) => $"{Reviews}/{reviewId}/approve-featured";
        public static string GetDeleteReviewUrl(int reviewId) => $"{Reviews}/{reviewId}";
    }
}
