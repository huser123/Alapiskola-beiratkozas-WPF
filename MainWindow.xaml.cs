using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml;  // EnumValue<> osztályhoz
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;  // XTextFormatter-hez
using Microsoft.Win32;
using System.Text;
using System.Diagnostics;
using MessageBox = System.Windows.MessageBox;

// Explicit névtér megadás az OpenXml osztályokhoz
using WordprocessingDocument = DocumentFormat.OpenXml.Packaging.WordprocessingDocument;
using WordprocessingDocumentType = DocumentFormat.OpenXml.WordprocessingDocumentType;
using Body = DocumentFormat.OpenXml.Wordprocessing.Body;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using TableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using TableBorders = DocumentFormat.OpenXml.Wordprocessing.TableBorders;
using TableWidth = DocumentFormat.OpenXml.Wordprocessing.TableWidth;
using TopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;
using BottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using LeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using RightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using InsideHorizontalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder;
using InsideVerticalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder;
using TableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using TableCellMargin = DocumentFormat.OpenXml.Wordprocessing.TableCellMargin;
using TopMargin = DocumentFormat.OpenXml.Wordprocessing.TopMargin;
using BottomMargin = DocumentFormat.OpenXml.Wordprocessing.BottomMargin;
using SectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using PageMargin = DocumentFormat.OpenXml.Wordprocessing.PageMargin;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using Justification = DocumentFormat.OpenXml.Wordprocessing.Justification;
using JustificationValues = DocumentFormat.OpenXml.Wordprocessing.JustificationValues;
using BorderValues = DocumentFormat.OpenXml.Wordprocessing.BorderValues;
using TableWidthUnitValues = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace IskolaiBeiratkozasGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // PDF-hez szükséges kódolás beállítás
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Kezdeti értékek
            dpSzuletesiDatum.SelectedDate = DateTime.Today.AddYears(-6); // Alapértelmezett életkor (6 év)

            // ComboBox feltöltése
            cmbTantargy.Items.Add("Katolikus hittan");
            cmbTantargy.Items.Add("Református hittan");
            cmbTantargy.Items.Add("Evangélikus hittan");
            cmbTantargy.Items.Add("Etika");
            cmbTantargy.Items.Add("Még nem tudom");
            cmbTantargy.SelectedIndex = 0;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Alapvető ellenőrzés
                if (string.IsNullOrWhiteSpace(txtTanuloNev.Text))
                {
                    MessageBox.Show("Kérjük, adja meg a tanuló nevét!", "Hiányzó adat", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTanuloNev.Focus();
                    return;
                }

                // Adatok összegyűjtése
                var adatok = new FormAdatok
                {
                    TanuloNev = txtTanuloNev.Text,
                    SzuletesiHelyDatum = $"{txtSzuletesiHely.Text}, {dpSzuletesiDatum.SelectedDate?.ToString("yyyy.MM.dd") ?? ""}",
                    SzuletesiSzam = txtSzuletesiSzam.Text,
                    AllandoLakhely = txtLakhely.Text,
                    Allampolgarsag = txtAllampolgarsag.Text,
                    Nemzetiseg = txtNemzetiseg.Text,
                    ApaNev = txtApaNev.Text,
                    ApaEmail = txtApaEmail.Text,
                    ApaTelefon = txtApaTelefon.Text,
                    ApaLakhely = txtApaLakhely.Text,
                    AnyaNev = txtAnyaNev.Text,
                    AnyaEmail = txtAnyaEmail.Text,
                    AnyaTelefon = txtAnyaTelefon.Text,
                    AnyaLakhely = txtAnyaLakhely.Text,
                    OvodaNev = txtOvoda.Text,
                    OsztalyTipus = GetValasztottOsztalyok(),
                    ValasztottTantargy = cmbTantargy.Text,
                    Napkozi = rbNapkoziIgen.IsChecked == true ? "Igen" : "Nem",
                    Etkeztetes = rbEtkeztetesIgen.IsChecked == true ? "Igen" : "Nem",
                    Allergia = txtAllergia.Text,
                    SzulokEgyutt = rbSzulokEgyuttIgen.IsChecked == true ? "Igen" : "Nem",
                    KapcsolattartoNev = txtKapcsolattartoNev.Text,
                    KapcsolattartoTelefon = txtKapcsolattartoTelefon.Text,
                    KapcsolattartoEmail = txtKapcsolattartoEmail.Text,
                    LevelezesNev = txtLevelezesNev.Text,
                    Megjegyzes = txtMegjegyzes.Text,
                    Elfogadom = "IGEN" // Alapértelmezetten IGEN
                };

                // Fájl mentési hely kiválasztása
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Word dokumentum (*.docx)|*.docx",
                    Title = "Formanyomtatvány mentése",
                    FileName = $"Beiratkozasi_lap_{adatok.TanuloNev.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string docxFajlnev = saveDialog.FileName;
                    string pdfFajlnev = Path.ChangeExtension(docxFajlnev, ".pdf");

                    // Word dokumentum generálása
                    GenerateDocx(docxFajlnev, adatok);

                    // PDF generálása
                    GeneratePdf(pdfFajlnev, adatok);

                    MessageBox.Show($"A dokumentumok sikeresen elkészültek:\n- {docxFajlnev}\n- {pdfFajlnev}",
                                    "Sikeres generálás",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                    // Dokumentum megnyitása, ha a felhasználó kéri
                    if (chkMegnyitas.IsChecked == true)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = docxFajlnev,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a dokumentum generálása közben: {ex.Message}\n\n{ex.StackTrace}",
                                "Hiba",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private string GetValasztottOsztalyok()
        {
            List<string> valasztottOsztalyok = new List<string>();

            if (chkOsztalyHagyomanyos.IsChecked == true)
                valasztottOsztalyok.Add("hagyományos");

            if (chkOsztalySportos.IsChecked == true)
                valasztottOsztalyok.Add("sportos");

            if (chkOsztalyEgeszNapos.IsChecked == true)
                valasztottOsztalyok.Add("egész napos");

            return string.Join(", ", valasztottOsztalyok);
        }

        private void GenerateDocx(string filePath, FormAdatok adatok)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                // Fő dokumentum rész hozzáadása
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Dokumentum formázása és margók beállítása
                SectionProperties sectionProps = new SectionProperties();
                PageMargin pageMargin = new PageMargin
                {
                    Top = 567,    // 1 cm
                    Right = 850,  // 1.5 cm
                    Bottom = 567, // 1 cm
                    Left = 567,   // 1 cm
                };
                sectionProps.AppendChild(pageMargin);

                // Táblázat létrehozása
                Table table = new Table();

                // Táblázat tulajdonságai
                TableProperties tableProps = new TableProperties();
                TableBorders tableBorders = new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 12 },
                    new RightBorder() { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
                );
                TableWidth tableWidth = new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct };

                tableProps.AppendChild(tableBorders);
                tableProps.AppendChild(tableWidth);
                table.AppendChild(tableProps);

                // Formanyomtatvány adatainak hozzáadása táblázatba
                AddTableRow(table, "A tanuló neve:", adatok.TanuloNev);
                AddTableRow(table, "A tanuló születési helye és dátuma:", adatok.SzuletesiHelyDatum);
                AddTableRow(table, "A tanuló születési száma:", adatok.SzuletesiSzam);
                AddTableRow(table, "A tanuló állandó lakhelye:", adatok.AllandoLakhely);
                AddTableRow(table, "A tanuló állampolgársága:", adatok.Allampolgarsag);
                AddTableRow(table, "A tanuló nemzetisége:", adatok.Nemzetiseg);
                AddTableRow(table, "Az apa neve:", adatok.ApaNev);
                AddTableRow(table, "Az apa e-mail címe:", adatok.ApaEmail);
                AddTableRow(table, "Az apa telefonszáma:", adatok.ApaTelefon);
                AddTableRow(table, "Az apa állandó lakhelye:", adatok.ApaLakhely);
                AddTableRow(table, "Az anya neve:", adatok.AnyaNev);
                AddTableRow(table, "Az anya e-mail címe:", adatok.AnyaEmail);
                AddTableRow(table, "Az anya telefonszáma:", adatok.AnyaTelefon);
                AddTableRow(table, "Az anya állandó lakhelye:", adatok.AnyaLakhely);
                AddTableRow(table, "Melyik óvodába járt a gyermek:", adatok.OvodaNev);
                AddTableRow(table, "Milyen jellegű osztályt választana (több lehetőség is választható):", adatok.OsztalyTipus);
                AddTableRow(table, "Választható tantárgy:", adatok.ValasztottTantargy);
                AddTableRow(table, "Napköziotthon:", adatok.Napkozi);
                AddTableRow(table, "Iskolai étkeztetésre igényt tart:", adatok.Etkeztetes);
                AddTableRow(table, "Van a gyermekének allergiája, vagy más betegsége, melyről az iskolálak tudnia kell?:", adatok.Allergia);
                AddTableRow(table, "A szülők egy háztartásban élnek?", adatok.SzulokEgyutt);
                AddTableRow(table, "Elsődleges kapcsolattartási személy vezetékneve és keresztneve (kit kereshetünk iskolai ügyekben):", adatok.KapcsolattartoNev);
                AddTableRow(table, "Elsődleges kapcsolattartási telefonszám (kit kereshetünk iskolai ügyekben):", adatok.KapcsolattartoTelefon);
                AddTableRow(table, "Elsődleges kapcsolattartási e-mail cím (kit kereshetünk iskolai ügyekben):", adatok.KapcsolattartoEmail);
                AddTableRow(table, "Az iskolalátogatásról szóló határozatot, illetve más levelezést az iskola kinek a nevére címezheti?:", adatok.LevelezesNev);

                if (!string.IsNullOrEmpty(adatok.Megjegyzes))
                {
                    AddTableRow(table, "Bármilyen egyéb megjegyzés, amiről esetleg tudnunk kellene:", adatok.Megjegyzes);
                }

                // Adatvédelmi nyilatkozat táblázat sorba
                TableRow adatvedelemRow = new TableRow();
                TableCell adatvedelemCell = new TableCell();
                TableCellProperties adatvedelemCellProps = new TableCellProperties();
                TableCellMargin adatvedelemCellMargin = new TableCellMargin();
                adatvedelemCellMargin.TopMargin = new TopMargin() { Width = "100" };
                adatvedelemCellMargin.BottomMargin = new BottomMargin() { Width = "100" };
                adatvedelemCellProps.AppendChild(adatvedelemCellMargin);
                adatvedelemCell.AppendChild(adatvedelemCellProps);

                Paragraph adatvedelemParagraph = new Paragraph();
                Run adatvedelemRun = new Run();
                adatvedelemRun.AppendChild(new Text("A Szlovák Köztársaság Nemzeti Tanácsának 18/2018-as, a személyes adatok védelméről szóló törvénye alapján hozzájárulok, hogy az iskola, mint adatkezelő (név: Alapiskola, statisztikai számjel: 123456, cím: XYZ), az elektronikus nyomtatványon megadott személyes adatokat gyűjtheti és feldolgozhatja a felvételi eljárással és az iskolalátogatással kapcsolatban."));
                adatvedelemParagraph.AppendChild(adatvedelemRun);

                Paragraph slovakAdatvedelemParagraph = new Paragraph();
                Run slovakAdatvedelemRun = new Run();
                slovakAdatvedelemRun.RunProperties = new RunProperties { Bold = new Bold() };
                slovakAdatvedelemRun.AppendChild(new Text("V zmysle zákona NR SR č. 18/2018 Z. z. o ochrane osobných údajov udeľujem súhlas škole ako spravovateľovi (Základná škola s vyučovacím jazykom maďarským, IČO: 123456 adresa: XYZ), so zberom a spracovaním poskytnutých osobných údajov uvedených v tejto elektronickej prihláške a to za účelom evidencie prihlásených žiakov v súvisloti s prijímacím konaním a školskou dochádzkou žiaka."));
                slovakAdatvedelemParagraph.AppendChild(slovakAdatvedelemRun);

                adatvedelemCell.AppendChild(adatvedelemParagraph);
                adatvedelemCell.AppendChild(slovakAdatvedelemParagraph);
                adatvedelemRow.AppendChild(adatvedelemCell);
                table.AppendChild(adatvedelemRow);

                // Elfogadás sor
                AddTableRow(table, "Elfogadom:", adatok.Elfogadom);

                // Táblázat hozzáadása a dokumentumhoz
                body.AppendChild(table);

                // Aláírási hely
                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                Paragraph signatureParagraph = body.AppendChild(new Paragraph());
                Run signatureRun = signatureParagraph.AppendChild(new Run());
                signatureRun.AppendChild(new Text("___________________________"));

                body.AppendChild(new Paragraph());

                Paragraph signatureLabelParagraph = body.AppendChild(new Paragraph());
                Run signatureLabelRun = signatureLabelParagraph.AppendChild(new Run());
                signatureLabelRun.AppendChild(new Text("Aláírás"));

                // BSSz szám legalul
                body.AppendChild(new Paragraph());

                Paragraph pageNumberParagraph = body.AppendChild(new Paragraph());
                ParagraphProperties pageNumberProps = new ParagraphProperties()
                {
                    Justification = new Justification() { Val = JustificationValues.Right }
                };
                pageNumberParagraph.AppendChild(pageNumberProps);

                Run pageNumberRun = pageNumberParagraph.AppendChild(new Run());
                pageNumberRun.AppendChild(new Text("BSSz: "));

                body.AppendChild(sectionProps);
                doc.Save();
            }
        }

        private void AddTableRow(Table table, string label, string value)
        {
            TableRow row = new TableRow();

            // Egy cella az egész sor számára
            TableCell cell = new TableCell();

            // Táblázat cella formázás
            TableCellProperties cellProperties = new TableCellProperties();
            TableCellMargin cellMargin = new TableCellMargin();
            cellMargin.TopMargin = new TopMargin() { Width = "100" };
            cellMargin.BottomMargin = new BottomMargin() { Width = "100" };
            cellProperties.AppendChild(cellMargin);
            cell.AppendChild(cellProperties);

            // Bekezdés a címkének és értéknek
            Paragraph paragraph = new Paragraph();

            // Címke formázás
            Run labelRun = new Run();
            labelRun.RunProperties = new RunProperties { Bold = new Bold() };
            labelRun.AppendChild(new Text(label + " "));
            paragraph.AppendChild(labelRun);

            // Érték hozzáadása
            Run valueRun = new Run();
            valueRun.AppendChild(new Text(value));
            paragraph.AppendChild(valueRun);

            cell.AppendChild(paragraph);
            row.AppendChild(cell);
            table.AppendChild(row);
        }

        private void GeneratePdf(string filePath, FormAdatok adatok)
        {
            // PDF dokumentum létrehozása
            PdfDocument document = new PdfDocument();
            document.Info.Title = "Iskolai beiratkozási adatlap";
            document.Info.Subject = "Beiratkozási adatlap - " + adatok.TanuloNev;

            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Betűtípusok
            XFont normalFont = new XFont("Arial", 10, XFontStyle.Regular);
            XFont boldFont = new XFont("Arial", 10, XFontStyle.Bold);
            XFont smallFont = new XFont("Arial", 8, XFontStyle.Regular);
            XFont smallBoldFont = new XFont("Arial", 8, XFontStyle.Bold);

            // Margók és táblázat beállítások
            double topMargin = 30;
            double leftMargin = 30;
            double rightMargin = 30;
            double bottomMargin = 30;
            double tableWidth = page.Width - leftMargin - rightMargin;
            double contentLeftMargin = leftMargin + 10; // tartalom bal margója
            double spacing = 5; // térköz

            // Teljes táblázat keret
            XRect tableRect = new XRect(leftMargin, topMargin, tableWidth, page.Height - topMargin - bottomMargin);
            gfx.DrawRectangle(XPens.Black, tableRect);

            // Kezdő Y pozíció a sorokhoz
            double currentY = topMargin;

            // Táblázat sorok magassága
            double standardRowHeight = 20;

            // Információk kiírása
            currentY = DrawRow(gfx, "A tanuló neve:", adatok.TanuloNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "A tanuló születési helye és dátuma:", adatok.SzuletesiHelyDatum, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "A tanuló születési száma:", adatok.SzuletesiSzam, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "A tanuló állandó lakhelye:", adatok.AllandoLakhely, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "A tanuló állampolgársága:", adatok.Allampolgarsag, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "A tanuló nemzetisége:", adatok.Nemzetiseg, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az apa neve:", adatok.ApaNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az apa e-mail címe:", adatok.ApaEmail, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az apa telefonszáma:", adatok.ApaTelefon, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az apa állandó lakhelye:", adatok.ApaLakhely, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az anya neve:", adatok.AnyaNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az anya e-mail címe:", adatok.AnyaEmail, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az anya telefonszáma:", adatok.AnyaTelefon, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Az anya állandó lakhelye:", adatok.AnyaLakhely, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Melyik óvodába járt a gyermek:", adatok.OvodaNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);

            // Osztálytípus speciális kezeléssel - nagyobb eltolással a válasznak
            currentY = DrawRowWithOffset(gfx, "Milyen jellegű osztályt választana (több lehetőség is választható):", adatok.OsztalyTipus, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight, 350);

            currentY = DrawRow(gfx, "Választható tantárgy:", adatok.ValasztottTantargy, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Napköziotthon:", adatok.Napkozi, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);
            currentY = DrawRow(gfx, "Iskolai étkeztetésre igényt tart:", adatok.Etkeztetes, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);

            // Allergia - nagyobb sormagasság
            currentY = DrawMultiLineRow(gfx, "Van a gyermekének allergiája, vagy más betegsége, melyről az iskolálak tudnia kell?:",
                adatok.Allergia, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);

            currentY = DrawRow(gfx, "A szülők egy háztartásban élnek?", adatok.SzulokEgyutt, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);

            // Kapcsolattartó adatok - több sor
            currentY = DrawMultiLineRow(gfx, "Elsődleges kapcsolattartási személy vezetékneve és keresztneve (kit kereshetünk iskolai ügyekben):",
                adatok.KapcsolattartoNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);

            currentY = DrawMultiLineRow(gfx, "Elsődleges kapcsolattartási telefonszám (kit kereshetünk iskolai ügyekben):",
                adatok.KapcsolattartoTelefon, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);

            currentY = DrawMultiLineRow(gfx, "Elsődleges kapcsolattartási e-mail cím (kit kereshetünk iskolai ügyekben):",
                adatok.KapcsolattartoEmail, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);

            currentY = DrawMultiLineRow(gfx, "Az iskolalátogatásról szóló határozatot, illetve más levelezést az iskola kinek a nevére címezheti?:",
                adatok.LevelezesNev, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);

            // Megjegyzés ha van
            if (!string.IsNullOrEmpty(adatok.Megjegyzes))
            {
                currentY = DrawMultiLineRow(gfx, "Bármilyen egyéb megjegyzés, amiről esetleg tudnunk kellene:",
                    adatok.Megjegyzes, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight * 1.5);
            }

            // Adatvédelmi nyilatkozat magyar
            XRect adatvedelmiRect = new XRect(contentLeftMargin, currentY + spacing, tableWidth - 20, standardRowHeight * 2);

            XTextFormatter tf1 = new XTextFormatter(gfx);
            string magyarSzoveg = "A Szlovák Köztársaság Nemzeti Tanácsának 18/2018-as, a személyes adatok védelméről szóló törvénye alapján hozzájárulok, hogy az iskola, mint adatkezelő (név: Alapiskola, statisztikai számjel: 123456, cím: XYZ), az elektronikus nyomtatványon megadott személyes adatokat gyűjtheti és feldolgozhatja a felvételi eljárással és az iskolalátogatással kapcsolatban.";
            tf1.DrawString(magyarSzoveg, smallFont, XBrushes.Black, adatvedelmiRect);

            currentY += standardRowHeight * 2 + spacing;
            DrawHorizontalLine(gfx, leftMargin, currentY, tableWidth);

            // Adatvédelmi nyilatkozat szlovák
            XRect szlovakRect = new XRect(contentLeftMargin, currentY + spacing, tableWidth - 20, standardRowHeight * 2);

            XTextFormatter tf2 = new XTextFormatter(gfx);
            string szlovakSzoveg = "V zmysle zákona NR SR č. 18/2018 Z. z. o ochrane osobných údajov udeľujem súhlas škole ako spravovateľovi (Základná škola s vyučovacím jazykom maďarským, IČO: 123456 adresa: XYZ), so zberom a spracovaním poskytnutých osobných údajov uvedených v tejto elektronickej prihláške a to za účelom evidencie prihlásených žiakov v súvisloti s prijímacím konaním a školskou dochádzkou žiaka.";
            tf2.DrawString(szlovakSzoveg, smallBoldFont, XBrushes.Black, szlovakRect);

            currentY += standardRowHeight * 2 + spacing;
            DrawHorizontalLine(gfx, leftMargin, currentY, tableWidth);

            // Elfogadom
            currentY = DrawRow(gfx, "Elfogadom:", adatok.Elfogadom, boldFont, normalFont, leftMargin, contentLeftMargin, currentY, tableWidth, standardRowHeight);

            // Az utolsó vonalat már nem kell rajzolni, az a táblázat kerete

            // Aláírás
            double signatureY = page.Height - bottomMargin - standardRowHeight * 3;
            gfx.DrawLine(new XPen(XColors.Black, 1), new XPoint(leftMargin + 50, signatureY), new XPoint(leftMargin + 250, signatureY));
            gfx.DrawString("Aláírás", normalFont, XBrushes.Black, new XPoint(leftMargin + 130, signatureY + 20));

            // BSSz szám - csak a felirat kiírása
            gfx.DrawString("BSSz: ", normalFont, XBrushes.Black, new XPoint(page.Width - rightMargin - 80, page.Height - bottomMargin - 10));

            // Mentés
            document.Save(filePath);
        }

        // Egy sor kirajzolása
        private double DrawRow(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont,
                             double tableX, double contentX, double y, double width, double height)
        {
            // Vízszintes vonal a sor alatt
            DrawHorizontalLine(gfx, tableX, y + height, width);

            // Címke
            gfx.DrawString(label + " ", labelFont, XBrushes.Black, new XPoint(contentX, y + height / 2 + 3));

            // Érték kicsit jobbra húzva
            double valueX = contentX + 200; // Ez az eltolás a címke és érték között
            gfx.DrawString(value, valueFont, XBrushes.Black, new XPoint(valueX, y + height / 2 + 3));

            return y + height;
        }

        // Sor speciális eltolással (hosszú kérdésekhez)
        private double DrawRowWithOffset(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont,
                             double tableX, double contentX, double y, double width, double height, double valueOffset)
        {
            // Vízszintes vonal a sor alatt
            DrawHorizontalLine(gfx, tableX, y + height, width);

            // Címke
            gfx.DrawString(label + " ", labelFont, XBrushes.Black, new XPoint(contentX, y + height / 2 + 3));

            // Érték speciális eltolással
            gfx.DrawString(value, valueFont, XBrushes.Black, new XPoint(contentX + valueOffset, y + height / 2 + 3));

            return y + height;
        }

        // Többsoros sor kirajzolása
        private double DrawMultiLineRow(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont,
                             double tableX, double contentX, double y, double width, double height)
        {
            // Vízszintes vonal a sor alatt
            DrawHorizontalLine(gfx, tableX, y + height, width);

            // Címke
            gfx.DrawString(label + " ", labelFont, XBrushes.Black, new XPoint(contentX, y + 12));

            // Érték - lejjebb, a következő sorban
            if (!string.IsNullOrEmpty(value))
            {
                gfx.DrawString(value, valueFont, XBrushes.Black, new XPoint(contentX + 20, y + 25));
            }

            return y + height;
        }

        // Vízszintes vonal rajzolása
        private void DrawHorizontalLine(XGraphics gfx, double x, double y, double width)
        {
            gfx.DrawLine(new XPen(XColors.Black, 0.5), new XPoint(x, y), new XPoint(x + width, y));
        }
    }

    public class FormAdatok
    {
        public string TanuloNev { get; set; }
        public string SzuletesiHelyDatum { get; set; }
        public string SzuletesiSzam { get; set; }
        public string AllandoLakhely { get; set; }
        public string Allampolgarsag { get; set; }
        public string Nemzetiseg { get; set; }
        public string ApaNev { get; set; }
        public string ApaEmail { get; set; }
        public string ApaTelefon { get; set; }
        public string ApaLakhely { get; set; }
        public string AnyaNev { get; set; }
        public string AnyaEmail { get; set; }
        public string AnyaTelefon { get; set; }
        public string AnyaLakhely { get; set; }
        public string OvodaNev { get; set; }
        public string OsztalyTipus { get; set; }
        public string ValasztottTantargy { get; set; }
        public string Napkozi { get; set; }
        public string Etkeztetes { get; set; }
        public string Allergia { get; set; }
        public string SzulokEgyutt { get; set; }
        public string KapcsolattartoNev { get; set; }
        public string KapcsolattartoTelefon { get; set; }
        public string KapcsolattartoEmail { get; set; }
        public string LevelezesNev { get; set; }
        public string Megjegyzes { get; set; }
        public string Elfogadom { get; set; }
    }
}