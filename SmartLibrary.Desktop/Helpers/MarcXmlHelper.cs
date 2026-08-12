using System.IO;
using System.Xml;

namespace SmartLibrary.Desktop.Helpers
{
    public static class MarcXmlHelper
    {
        /// <summary>
        /// Sinh chuỗi XML bản ghi biên mục chuẩn MARC 21 từ các thông tin cơ bản của sách
        /// </summary>
        public static string GenerateMarc21Xml(string isbn, string title, string author, string publisher, int publishYear)
        {
            var settings = new XmlWriterSettings 
            { 
                OmitXmlDeclaration = true, 
                Indent = true,
                CloseOutput = true
            };
            
            using (var stringWriter = new StringWriter())
            {
                using (var writer = XmlWriter.Create(stringWriter, settings))
                {
                    writer.WriteStartElement("record", "http://www.loc.gov/MARC21/slim");
                    
                    // Leader
                    writer.WriteStartElement("leader");
                    writer.WriteString("00000nam a2200000   4500");
                    writer.WriteEndElement();

                    // Field 020 - ISBN
                    WriteDataField(writer, "020", ' ', ' ', "a", isbn);

                    // Field 100 - Tác giả
                    if (!string.IsNullOrEmpty(author))
                    {
                        WriteDataField(writer, "100", '1', ' ', "a", author);
                    }

                    // Field 245 - Tựa đề sách
                    WriteDataField(writer, "245", '1', '0', "a", title);

                    // Field 260 - Chi tiết xuất bản (Publisher, Year)
                    writer.WriteStartElement("datafield");
                    writer.WriteAttributeString("tag", "260");
                    writer.WriteAttributeString("ind1", " ");
                    writer.WriteAttributeString("ind2", " ");
                    
                    writer.WriteStartElement("subfield");
                    writer.WriteAttributeString("code", "b");
                    writer.WriteString(publisher);
                    writer.WriteEndElement();
                    
                    writer.WriteStartElement("subfield");
                    writer.WriteAttributeString("code", "c");
                    writer.WriteString(publishYear.ToString());
                    writer.WriteEndElement();
                    
                    writer.WriteEndElement(); // datafield 260

                    writer.WriteEndElement(); // record
                }
                return stringWriter.ToString();
            }
        }

        private static void WriteDataField(XmlWriter writer, string tag, char ind1, char ind2, string code, string value)
        {
            writer.WriteStartElement("datafield");
            writer.WriteAttributeString("tag", tag);
            writer.WriteAttributeString("ind1", ind1.ToString());
            writer.WriteAttributeString("ind2", ind2.ToString());
            
            writer.WriteStartElement("subfield");
            writer.WriteAttributeString("code", code);
            writer.WriteString(value);
            writer.WriteEndElement();
            
            writer.WriteEndElement(); // datafield
        }
    }
}
