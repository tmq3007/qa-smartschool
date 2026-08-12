using System;

namespace QASmartClass.Helpers
{
    public static class BmiCalculator
    {
        public static double CalculateBmi(double heightCm, double weightKg)
        {
            if (heightCm <= 0) return 0;
            return weightKg / Math.Pow(heightCm / 100.0, 2);
        }

        public static string GetBmiCategory(double bmi, int age, string gender, bool useChildStandard)
        {
            if (bmi <= 0) return "Không xác định";

            if (useChildStandard)
            {
                bool isBoy = string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase);
                double p5 = 13.0;
                double p85 = 17.0;
                double p95 = 18.5;

                if (isBoy)
                {
                    if (age <= 7)  { p5 = 13.0; p85 = 17.0; p95 = 18.5; }
                    else if (age <= 9)  { p5 = 13.5; p85 = 18.0; p95 = 20.0; }
                    else if (age <= 11) { p5 = 14.0; p85 = 19.5; p95 = 22.0; }
                    else if (age <= 13) { p5 = 15.0; p85 = 21.0; p95 = 24.0; }
                    else if (age <= 15) { p5 = 16.5; p85 = 23.0; p95 = 26.0; }
                    else { p5 = 17.5; p85 = 25.0; p95 = 28.0; }
                }
                else
                {
                    if (age <= 7)  { p5 = 12.7; p85 = 16.8; p95 = 18.0; }
                    else if (age <= 9)  { p5 = 13.2; p85 = 17.8; p95 = 19.8; }
                    else if (age <= 11) { p5 = 13.8; p85 = 19.2; p95 = 21.8; }
                    else if (age <= 13) { p5 = 14.8; p85 = 20.8; p95 = 23.8; }
                    else if (age <= 15) { p5 = 16.0; p85 = 22.8; p95 = 25.8; }
                    else { p5 = 17.2; p85 = 24.8; p95 = 27.8; }
                }

                if (bmi < p5)
                    return "Gầy";
                if (bmi < p85)
                    return "Bình thường";
                if (bmi < p95)
                    return "Thừa cân";
                return "Béo phì";
            }
            else
            {
                if (bmi < 18.5) return "Gầy";
                if (bmi <= 25.0) return "Bình thường";
                if (bmi <= 30.0) return "Thừa cân";
                return "Béo phì";
            }
        }
    }
}
