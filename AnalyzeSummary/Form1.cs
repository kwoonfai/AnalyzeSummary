using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace AnalyzeSummary
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string fileAnalyze = @"C:\Temp\00D_DEU.htm";
        private void Button1_Click(object sender, EventArgs e)
        {
            List<string> lines = new List<string>();
            lines.Add("Company,Symbol,Country,Exchange");

            DirectoryInfo d = new DirectoryInfo(@"C:\Users\kwoon\Desktop\Programming\downloadSummary\downloadSummary\bin\Debug");
            FileInfo[] Files = d.GetFiles("*.htm");
            int a = 1;

            foreach (FileInfo file in Files)
            {
                string thisLine = GetCSVLine(file.FullName);
                if (thisLine == "") continue;

                //finalCSV += thisLine + "\r\n";
                lines.Add(thisLine);

                Debug.WriteLine(a);
                a++;
            }

            File.WriteAllLines("FT_CompaniesSymbol.csv", lines.ToArray());
            //File.WriteAllText("FT_CompaniesSymbol.csv", finalCSV);

            Debug.WriteLine("Finished");
        }

        string GetCSVLine (string path)
        {
            string input = File.ReadAllText(path);
            if (input.Contains("There were no results found for")) return "";

            input = System.Text.RegularExpressions.Regex.Unescape(input);
            input = HttpUtility.HtmlDecode(input);

            string thisCompanyName = ExtractAndParse(ref input, "<h1", "</h1>");
            if (thisCompanyName.Contains(','))
            {
                Debug.WriteLine("Contains comma - " + thisCompanyName);
                thisCompanyName = thisCompanyName.Replace(",", ""); // fix
            }

            string extract = ExtractAndParse(ref input, @"<div class=""mod-ui-symbol-chain"">", @"<h1 class=""mod-tearsheet-overview__header__name mod-tearsheet-overview__header__name--small"">");

            string thisSymbol = ExtractAndParse(ref extract, "<span", "</span>");
            {
                Match m = Regex.Match(thisSymbol, @"(.*?)<i");
                if (m.Success) thisSymbol = m.Groups[1].Value;
                else return ""; // fix

                if (thisSymbol.Contains(',')) // fix - skip symbols with , ex BRKX,A:GER
                    return "";
            }

            string selectSymbolUnused = ExtractAndParse(ref extract, "<span", "</span>");

            Dictionary<string, string> countryExchange = new Dictionary<string, string>();
            string lastCountry = "";

            while (true)
            {
                string snippet = ExtractAndParse(ref extract, "<li", "</li>");
                if (snippet == "") break;

                if (snippet.Contains("</i>"))
                {
                    var match = Regex.Match(snippet, @"<\/i>(.*)");
                    if (match.Success)
                    {
                        lastCountry = match.Groups[1].Value;
                    }
                    else
                        continue;
                }
                else
                {
                    string innerSymbol = ExtractAndParse(ref snippet, "<span>", "</span>");
                    string exchange = ExtractAndParse(ref snippet, "<span>", "</span>");

                    if (innerSymbol == thisSymbol)
                    {
                        countryExchange.Add(lastCountry, exchange);
                    }

                }
            }

            string finalString="";

            foreach (var v in countryExchange)
            {
                finalString = $"{thisCompanyName},{thisSymbol},{v.Key},{v.Value}";
            }
            return finalString;
        }

        void DumpOutput()
        {
            var firstLine = "Company,Symbol,Country,Exchange";
        }

        string ExtractAndParse(ref string input, string tag, string endTag)
        {
            string extracted = Extract(ref input, tag, endTag);
            if (extracted == "") return "";

            string extractedWithoutTags = Parser(extracted, tag, endTag);
            return extractedWithoutTags;
        }

        // "gibberish<test>hahaha</test>gibberish";
        // f("<test>", "</test>) - hahaha
        string Parser(string input, string tag, string endTag)
        {
            if (!tag.EndsWith(">"))
            {
                int pos1 = input.IndexOf(tag); if (pos1 == -1) return ""; // LKF: 26 Jul
                int pos2 = input.IndexOf(">", pos1 + 1); if (pos2 == -1) return ""; // LKF: 26 Jul
                tag = input.Substring(pos1, pos2 - pos1 + 1);
                if (tag == "") return "";
            }

            if (input.IndexOf(tag) == -1) return "";
            if (input.IndexOf(endTag) == -1) return "";

            int start = input.IndexOf(tag);
            start += tag.Length;

            int len = input.Length - (start + (input.Length - input.IndexOf(endTag, input.IndexOf(tag) + 1))); //

            string extract = input.Substring(start, len);
            return extract;
        }

        // extract text including tag
        string Extract(ref string input, string tag, string endTag)
        {
            if (!tag.EndsWith(">"))
            {
                int pos1 = input.IndexOf(tag); if (pos1 == -1) return ""; // LKF: 26 Jul
                int pos2 = input.IndexOf(">", pos1 + 1); if (pos2 == -1) return ""; // LKF: 26 Jul
                tag = input.Substring(pos1, pos2 - pos1 + 1);
                if (tag == "") return "";
            }

            if (input.IndexOf(tag) == -1) return "";
            if (input.IndexOf(endTag) == -1) return "";

            int start = input.IndexOf(tag);

            int len = input.Length - (input.IndexOf(tag)) - (input.Length - input.IndexOf(endTag, input.IndexOf(tag) + 1) - endTag.Length); //

            string extract = input.Substring(start, len);

            input = input.Replace(extract, "");

            return extract;
        }
    }
}
