using System.Drawing;
using System.Drawing.Printing;
using System.Formats.Tar;
using System.Text;
using System.Text.Json;

namespace FrameCodeGenerator
{
    public partial class Form1 : Form
    {
        PrintDocument pd = new PrintDocument();
        private List<FramePreset> presets = new List<FramePreset>();
        public Form1()
        {
            InitializeComponent();
            labPrinterName.Text = pd.PrinterSettings.PrinterName;
            cbPresets.SelectedIndexChanged += cbPresets_SelectedIndexChanged;
            LoadPresets();
        }
        private void LoadPresets()
        {
            if (File.Exists("presets.json"))
            {
                var json = File.ReadAllText("presets.json");
                presets = JsonSerializer.Deserialize<List<FramePreset>>(json) ?? new List<FramePreset>();
            }
            else
            {
                presets = new List<FramePreset>
        {
            new FramePreset { Name = "A4 Portrait", FrameWidth = 198, FrameHeight = 288, Landscape = false },
            new FramePreset { Name = "A4 Landscape", FrameWidth = 288, FrameHeight = 198, Landscape = true },
            new FramePreset { Name = "Square", FrameWidth = 150, FrameHeight = 150, Landscape = false }
        };
            }
            cbPresets.DataSource = null;
            cbPresets.DataSource = presets;
        }
        private void SavePresets()
        {
            var json = JsonSerializer.Serialize(presets);
            File.WriteAllText("presets.json", json);
        }
        private void cbPresets_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbPresets.SelectedItem is FramePreset preset)
            {
                txtFrameWidth.Text = preset.FrameWidth.ToString();
                txtFrameHeight.Text = preset.FrameHeight.ToString();
                chkLandscape.Checked = preset.Landscape;
                chkPrintId.Checked = preset.PrintId;
            }
        }
        private void butSavePreset_Click(object sender, EventArgs e)
        {
            FramePreset preset;
            if (presets.Any(p => p.Name == cbPresets.Text))
            {
                //update existing preset
                preset = presets.First(p => p.Name == cbPresets.Text);
                preset.FrameWidth = float.Parse(txtFrameWidth.Text);
                preset.FrameHeight = float.Parse(txtFrameHeight.Text);
                preset.Landscape = chkLandscape.Checked;
                preset.PrintId = chkPrintId.Checked;
            }
            else
            {
                preset = new FramePreset
                {
                    Name = cbPresets.Text,
                    FrameWidth = float.Parse(txtFrameWidth.Text),
                    FrameHeight = float.Parse(txtFrameHeight.Text),
                    Landscape = chkLandscape.Checked,
                    PrintId = chkPrintId.Checked
                };
                presets.Add(preset);
            }

            cbPresets.DataSource = null;
            cbPresets.DataSource = presets;
            cbPresets.SelectedItem = presets.First(p => p.Name == preset.Name);
            SavePresets();
        }

        void PreviewDoc()
        {
            pd = new PrintDocument();
            pd.OriginAtMargins = false;
            pd.DefaultPageSettings.Landscape = chkLandscape.Checked;
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.PrintPage += Pd_PrintPage;
            printPreviewControl1.Document = pd;
        }


        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if(e.Graphics == null) return;

            int frameId = (int)nudFrameId.Value;
            int cornerStartId = frameId * 4;

            // Get the printable area
            var pageBounds = e.PageBounds; 

            float cornerSize = Convert.ToSingle(nudFrameSize.Value);

            // Set the size of the corner images (in hundredths of an inch)
            float imgWidth = cornerSize / 0.254f;
            float imgHeight = cornerSize / 0.254f;

            var xCentre = pageBounds.Left + pageBounds.Width / 2f;
            var yCentre = pageBounds.Top + pageBounds.Height / 2f;
            var frameWidth = Convert.ToSingle(txtFrameWidth.Text) / 0.254f;
            var frameHeight = Convert.ToSingle(txtFrameHeight.Text) / 0.254f;

            // Calculate center positions for each corner
            var centres = new[]
            {
                // i=0: Top-left
                new PointF(xCentre - frameWidth/2f + imgWidth / 2f, yCentre - frameHeight/2 + imgHeight / 2f),
                // i=1: Top-right
                new PointF(xCentre + frameWidth/2f - imgWidth / 2f, yCentre - frameHeight/2 + imgHeight / 2f),
                // i=2: Bottom-right
                new PointF(xCentre + frameWidth/2f - imgWidth / 2f, yCentre + frameHeight/2 - imgHeight / 2f),
                // i=3: Bottom-left
                new PointF(xCentre - frameWidth/2f + imgWidth / 2f, yCentre + frameHeight/2 - imgHeight / 2f)
            };

            if (chkPrintId.Checked)
                e.Graphics.DrawString($"{frameId}", new Font("Arial", 16), Brushes.Black,
                new RectangleF(xCentre - 40, yCentre - frameHeight / 2, 80, 50),
                 new StringFormat()
                 {
                     Alignment = StringAlignment.Center,
                 });

            // Rotation angles for each corner
            var angles = new[] { 0f, 90f, 180f, 270f };

            for (ushort i = 0; i < 4; i++)
            {
                using var bmp = GenBitmap((ushort)(cornerStartId + i), (int)imgWidth);
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(centres[i].X, centres[i].Y);
                e.Graphics.RotateTransform(angles[i]);
                e.Graphics.DrawImage(bmp, -imgWidth / 2f, -imgHeight / 2f, imgWidth, imgHeight);

                e.Graphics.Restore(state);
            }
        }

        private Bitmap GenBitmap(UInt16 code, int size)
        {
            int gridSize = 9;
            float cellSize = size / (float)gridSize;
            var bitmap = new Bitmap(size, size);
            var g = Graphics.FromImage(bitmap);

            // draw borders
            g.FillRectangle(Brushes.Black, 0, 0, size, cellSize);
            g.FillRectangle(Brushes.Black, 0, 0, cellSize, size);

            //draw barcodes
            for (int y = gridSize - 1; y > 0; y--)
            {
                if ((code & (1 << (gridSize - 1 - y))) != 0)
                {
                    g.FillRectangle(Brushes.Black, cellSize * 1, cellSize * y, cellSize, cellSize);
                }
            }
            for (int x = 2; x < gridSize - 1; x++)
            {
                if ((code & (1 << (gridSize - 1 + x))) != 0)
                {
                    g.FillRectangle(Brushes.Black, cellSize * x, cellSize * 1, cellSize, cellSize);
                }
            }
            var setBits = System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(code);
            if ((setBits % 2) == 0)
            {
                g.FillRectangle(Brushes.Black, cellSize * (gridSize - 1), cellSize * 1, cellSize, cellSize);
            }

            return bitmap;
        }
        private void butPreview_Click(object sender, EventArgs e)
        {
            PreviewDoc();
        }

        private void butPrint_Click(object sender, EventArgs e)
        {
            pd.Print();
        }

        private void butPrintSettings_Click(object sender, EventArgs e)
        {
            var printDlg = new PrintDialog();

            printDlg.Document = pd;
            if (printDlg.ShowDialog() == DialogResult.OK)
            {
                pd.DefaultPageSettings = printDlg.Document.DefaultPageSettings;
                labPrinterName.Text = pd.PrinterSettings.PrinterName;
            }
        }

        private void butDeletePreset_Click(object sender, EventArgs e)
        {
            if (cbPresets.SelectedItem is FramePreset preset)
            {
                presets.Remove(preset);
                cbPresets.DataSource = null;
                cbPresets.DataSource = presets;
                SavePresets();
            }
        }
    }
    public class FramePreset
    {
        public string Name { get; set; } = "";
        public float FrameWidth { get; set; }
        public float FrameHeight { get; set; }
        public bool Landscape { get; set; }
        public bool PrintId { get; set; } = true;

        public override string ToString() => Name;
    }
}
