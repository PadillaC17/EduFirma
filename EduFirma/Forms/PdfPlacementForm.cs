using PdfSharp.Pdf.IO;
using System.Drawing;
using System.Windows.Forms;

namespace EduFirma.Forms;

public sealed class PdfPlacementForm : Form
{
    private readonly PictureBox image = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(225, 228, 238) };
    private readonly NumericUpDown pageNumber = new() { Minimum = 1, Width = 72 };
    private readonly Label info = new() { AutoSize = true, Text = "Haz clic sobre la página para colocar el sello" };
    private readonly string path;
    private readonly int count;
    private float widthPoints;
    private float heightPoints;
    private double fractionX = 0.1, fractionY = 0.1;
    private const double StampWidth = 185;
    private const double StampHeight = 65;

    public int SelectedPage => (int)pageNumber.Value;
    public double SelectedX => Math.Clamp(fractionX * widthPoints, 0, Math.Max(0, widthPoints - StampWidth));
    public double SelectedY => Math.Clamp((1 - fractionY) * heightPoints - StampHeight, 0, Math.Max(0, heightPoints - StampHeight));

    public PdfPlacementForm(string pdf, int initialPage)
    {
        path = pdf;
        using (var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import))
            count = doc.PageCount;
        if (count == 0) throw new InvalidOperationException("El PDF no tiene páginas.");
        Text = "EduFirma — Elegir posición del sello";
        Width = 890; Height = 980; MinimumSize = new Size(580, 500);
        StartPosition = FormStartPosition.CenterParent;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var header = new FlowLayoutPanel { Dock = DockStyle.Fill };
        header.Controls.Add(new Label { Text = "Página", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        pageNumber.Maximum = count;
        pageNumber.Value = Math.Clamp(initialPage, 1, count);
        header.Controls.Add(pageNumber);
        header.Controls.Add(new Label { Text = "/ " + count, AutoSize = true, Padding = new Padding(0, 8, 15, 0) });
        header.Controls.Add(info);
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(image, 0, 1);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var accept = new Button { Text = "Aplicar posición", AutoSize = true, DialogResult = DialogResult.OK };
        footer.Controls.Add(accept);
        footer.Controls.Add(new Button { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel });
        layout.Controls.Add(footer, 0, 2);
        Controls.Add(layout);
        AcceptButton = accept; CancelButton = footer.Controls.OfType<Button>().Last();
        pageNumber.ValueChanged += (_, _) => RenderPage();
        image.MouseClick += (_, e) => Place(e.Location);
        image.Paint += (_, e) => DrawStamp(e.Graphics);
        image.Resize += (_, _) => image.Invalidate();
        Shown += (_, _) => RenderPage();
    }

    private RectangleF DisplayRect()
    {
        if (image.Image == null || image.ClientSize.Width <= 0 || image.ClientSize.Height <= 0)
            return RectangleF.Empty;
        float scale = Math.Min((float)image.ClientSize.Width / image.Image.Width, (float)image.ClientSize.Height / image.Image.Height);
        float w = image.Image.Width * scale, h = image.Image.Height * scale;
        return new RectangleF((image.ClientSize.Width - w) / 2f, (image.ClientSize.Height - h) / 2f, w, h);
    }

    private void Place(Point cursor)
    {
        RectangleF rect = DisplayRect();
        if (rect.IsEmpty || !rect.Contains(cursor)) return;
        fractionX = Math.Clamp((cursor.X - rect.X) / rect.Width, 0, 1);
        fractionY = Math.Clamp((cursor.Y - rect.Y) / rect.Height, 0, 1);
        info.Text = $"Sello: página {SelectedPage}, X {SelectedX:F0} pt, Y {SelectedY:F0} pt";
        image.Invalidate();
    }

    private void DrawStamp(Graphics graphics)
    {
        RectangleF rect = DisplayRect();
        if (rect.IsEmpty || widthPoints <= 0 || heightPoints <= 0) return;
        float x = rect.X + (float)(SelectedX / widthPoints) * rect.Width;
        float y = rect.Y + (float)((heightPoints - SelectedY - StampHeight) / heightPoints) * rect.Height;
        float w = (float)(StampWidth / widthPoints) * rect.Width;
        float h = (float)(StampHeight / heightPoints) * rect.Height;
        using var pen = new Pen(Color.FromArgb(130, 75, 75, 190), 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var brush = new SolidBrush(Color.FromArgb(55, 80, 80, 190));
        graphics.FillRectangle(brush, x, y, w, h);
        graphics.DrawRectangle(pen, x, y, w, h);
        graphics.DrawString("Firma digital", Font, Brushes.DarkBlue, x + 2, y + 2);
    }

    private void RenderPage()
    {
        try
        {
            using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
            {
                widthPoints = (float)doc.Pages[SelectedPage - 1].Width.Point;
                heightPoints = (float)doc.Pages[SelectedPage - 1].Height.Point;
            }
            using var fs = File.OpenRead(path);
            using var stream = new MemoryStream();
            PDFtoImage.Conversion.SavePng(stream, fs, page: SelectedPage - 1);
            stream.Position = 0;
            using var bitmap = Image.FromStream(stream);
            var previous = image.Image;
            image.Image = new Bitmap(bitmap);
            previous?.Dispose();
            image.Invalidate();
            info.Text = $"Página {SelectedPage}/{count} — clic para colocar el sello";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo mostrar esta página: " + ex.Message, "Visor PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) image.Image?.Dispose();
        base.Dispose(disposing);
    }
}
