using System;
using System.IO;
using Xunit;

namespace QASmartClass.Tests
{
    public class AssetCopyTests
    {
        [Fact]
        public void CopyGeneratedAssets()
        {
            string srcDir = @"C:\Users\DELL\.gemini\antigravity\brain\09a419b8-020a-4c09-933c-21f3344463e0";
            string destDir = @"d:\JOB\QA SmartClass -062026\QASmartClass\Assets\Images";

            // Define mapping of generated images to target images
            var mappings = new (string SrcFile, string DestFile)[]
            {
                ("app_conic_7_new_1782057179615.png", "app_conic_7_VN.png"),
                ("app_conic_7_new_1782057179615.png", "app_conic_7_EN.png"),
                ("app_conic_8_new_1782057258506.png", "app_conic_8_VN.png"),
                ("app_conic_8_new_1782057258506.png", "app_conic_8_EN.png"),
                ("app_derivative_7_new_1782057298718.png", "app_derivative_7_VN.png"),
                ("app_derivative_7_new_1782057298718.png", "app_derivative_7_EN.png"),
                ("app_derivative_8_new_1782057312816.png", "app_derivative_8_VN.png"),
                ("app_derivative_8_new_1782057312816.png", "app_derivative_8_EN.png"),
                ("app_complex_7_new_1782057377113.png", "app_complex_7_VN.png"),
                ("app_complex_7_new_1782057377113.png", "app_complex_7_EN.png"),
                ("app_complex_8_new_1782057389812.png", "app_complex_8_VN.png"),
                ("app_complex_8_new_1782057389812.png", "app_complex_8_EN.png"),
                ("app_quadratic_7_new_1782057428980.png", "app_quadratic_7_VN.png"),
                ("app_quadratic_7_new_1782057428980.png", "app_quadratic_7_EN.png"),
                ("app_quadratic_8_new_1782057440641.png", "app_quadratic_8_VN.png"),
                ("app_quadratic_8_new_1782057440641.png", "app_quadratic_8_EN.png"),
                ("app_linearsystem_7_new_1782057482777.png", "app_linearsystem_7_VN.png"),
                ("app_linearsystem_7_new_1782057482777.png", "app_linearsystem_7_EN.png"),
                ("app_linearsystem_8_new_1782057497547.png", "app_linearsystem_8_VN.png"),
                ("app_linearsystem_8_new_1782057497547.png", "app_linearsystem_8_EN.png"),
                ("app_trig_7_new_1782057537683.png", "app_trig_7_VN.png"),
                ("app_trig_7_new_1782057537683.png", "app_trig_7_EN.png"),
                ("app_trig_8_new_1782057553552.png", "app_trig_8_VN.png"),
                ("app_trig_8_new_1782057553552.png", "app_trig_8_EN.png"),
                ("app_cubic_7_new_1782057610317.png", "app_cubic_7_VN.png"),
                ("app_cubic_7_new_1782057610317.png", "app_cubic_7_EN.png"),
                ("app_cubic_8_new_1782057624238.png", "app_cubic_8_VN.png"),
                ("app_cubic_8_new_1782057624238.png", "app_cubic_8_EN.png"),
                ("app_coordinate_7_new_1782057698578.png", "app_coordinate_7_VN.png"),
                ("app_coordinate_7_new_1782057698578.png", "app_coordinate_7_EN.png"),
                ("app_coordinate_8_new_1782057711895.png", "app_coordinate_8_VN.png"),
                ("app_coordinate_8_new_1782057711895.png", "app_coordinate_8_EN.png"),
                ("app_inequality_7_new_1782057760262.png", "app_inequality_7_VN.png"),
                ("app_inequality_7_new_1782057760262.png", "app_inequality_7_EN.png"),
                ("app_inequality_8_new_1782057776189.png", "app_inequality_8_VN.png"),
                ("app_inequality_8_new_1782057776189.png", "app_inequality_8_EN.png"),
                ("app_unitconverter_7_1782057885208.png", "app_unitconverter_7_VN.png"),
                ("app_unitconverter_7_1782057885208.png", "app_unitconverter_7_EN.png"),
                ("app_unitconverter_8_1782057900748.png", "app_unitconverter_8_VN.png"),
                ("app_unitconverter_8_1782057900748.png", "app_unitconverter_8_EN.png"),
                ("app_integral_7_1782058233940.png", "app_integral_7_VN.png"),
                ("app_integral_7_1782058233940.png", "app_integral_7_EN.png"),
                ("app_integral_8_1782058248028.png", "app_integral_8_VN.png"),
                ("app_integral_8_1782058248028.png", "app_integral_8_EN.png"),
                ("app_matrix_7_1782058338737.png", "app_matrix_7_VN.png"),
                ("app_matrix_7_1782058338737.png", "app_matrix_7_EN.png"),
                ("app_matrix_8_1782058353902.png", "app_matrix_8_VN.png"),
                ("app_matrix_8_1782058353902.png", "app_matrix_8_EN.png"),
                ("app_vector_7_1782058434982.png", "app_vector_7_VN.png"),
                ("app_vector_7_1782058434982.png", "app_vector_7_EN.png"),
                ("app_vector_8_1782058446964.png", "app_vector_8_VN.png"),
                ("app_vector_8_1782058446964.png", "app_vector_8_EN.png"),
                ("app_genetics_7_1782058478816.png", "app_genetics_7_VN.png"),
                ("app_genetics_7_1782058478816.png", "app_genetics_7_EN.png"),
                ("app_genetics_8_1782058492670.png", "app_genetics_8_VN.png"),
                ("app_genetics_8_1782058492670.png", "app_genetics_8_EN.png"),
                ("app_molecular_genetics_7_1782058526230.png", "app_molecular_genetics_7_VN.png"),
                ("app_molecular_genetics_7_1782058526230.png", "app_molecular_genetics_7_EN.png"),
                ("app_molecular_genetics_8_1782058542242.png", "app_molecular_genetics_8_VN.png"),
                ("app_molecular_genetics_8_1782058542242.png", "app_molecular_genetics_8_EN.png"),
                ("app_circuit_7_1782058570977.png", "app_circuit_7_VN.png"),
                ("app_circuit_7_1782058570977.png", "app_circuit_7_EN.png"),
                ("app_circuit_8_1782058585894.png", "app_circuit_8_VN.png"),
                ("app_circuit_8_1782058585894.png", "app_circuit_8_EN.png"),
                ("app_density_7_1782058613921.png", "app_density_7_VN.png"),
                ("app_density_7_1782058613921.png", "app_density_7_EN.png"),
                ("app_density_8_1782058627229.png", "app_density_8_VN.png"),
                ("app_density_8_1782058627229.png", "app_density_8_EN.png")
            };

            foreach (var mapping in mappings)
            {
                string srcPath = Path.Combine(srcDir, mapping.SrcFile);
                string destPath = Path.Combine(destDir, mapping.DestFile);

                if (File.Exists(srcPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.Copy(srcPath, destPath, true);
                    Console.WriteLine($"Successfully copied {mapping.SrcFile} to {mapping.DestFile}");
                }
                else
                {
                    Console.WriteLine($"Source file not found: {srcPath}");
                }
            }
        }

        [Fact]
        public void FindToolsWithPracticalApps()
        {
            string rootPath = @"d:\JOB\QA SmartClass -062026\QASmartClass\LearningTools";
            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            using (var writer = new StreamWriter(@"d:\JOB\QA SmartClass -062026\practical_apps_report.txt"))
            {
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext != ".cs" && ext != ".xaml") continue;

                    string content = File.ReadAllText(file);
                    if (content.Contains("_7_") || content.Contains("_8_"))
                    {
                        writer.WriteLine($"File: {file}");
                        
                        var lines = File.ReadAllLines(file);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains("_7_") || lines[i].Contains("_8_") || lines[i].Contains("Title =") || lines[i].Contains("Title="))
                            {
                                writer.WriteLine($"  Line {i+1}: {lines[i].Trim()}");
                            }
                        }
                        writer.WriteLine(new string('-', 50));
                    }
                }
            }
        }
    }
}
