using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfAppCanvasDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    List<Point> points = new();
    double minX = 0;
    double minY = 0;
    double maxX = 1000;
    double maxY = 1000;
    Random r = new Random();
    public MainWindow()
    {
        InitializeComponent();
    }
    private void DrawRectangle(Point p1, Point p2)
    {
        // Calculate top-left corner
        double x = Math.Min(p1.X, p2.X);
        double y = Math.Min(p1.Y, p2.Y);

        // Calculate width & height
        double width = Math.Abs(p2.X - p1.X);
        double height = Math.Abs(p2.Y - p1.Y);

        // Create a Rectangle
        Rectangle rect = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(50, 55,25, 150)) // Transparent red fill
        };

        // Set position on Canvas
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);

        // Add to Canvas
        drawingCanvas.Children.Add(rect);
    }
    private List<Point> ScalePointsToFitCanvas(List<Point> points)
    {
        double canvasWidth = drawingCanvas.ActualWidth;
        double canvasHeight = drawingCanvas.ActualHeight;

        // Compute original width & height
        double dataWidth = maxX - minX;
        double dataHeight = maxY - minY;

        // Avoid division by zero
        if (dataWidth == 0 || dataHeight == 0)
            return points;

        // Compute scale factors for both dimensions
        double scaleX = canvasWidth / dataWidth;
        double scaleY = canvasHeight / dataHeight;

        // Use the smallest scale to maintain aspect ratio
        double scale = Math.Min(scaleX, scaleY); // 90% to avoid touching edges

        // Compute offset to center the drawing
        double offsetX = (canvasWidth - (dataWidth * scale)) / 2;
        double offsetY = (canvasHeight - (dataHeight * scale)) / 2;

        // Scale and translate each point
        List<Point> scaledPoints = points.Select(p =>
            new Point(
                (p.X - minX) * scale + offsetX, // Scale X and apply offset
                (p.Y - minY) * scale + offsetY  // Scale Y and apply offset
            )).ToList();

        return scaledPoints;
    }
    private Point ScalePointFromCanvas(Point p)
    {
        double canvasWidth = drawingCanvas.ActualWidth;
        double canvasHeight = drawingCanvas.ActualHeight;

        // Compute original width & height
        double dataWidth = maxX - minX;
        double dataHeight = maxY - minY;

        // Compute scale factors for both dimensions
        double scaleX = dataWidth/canvasWidth;
        double scaleY = dataHeight/canvasHeight;

        double scale = Math.Max(scaleX, scaleY); 

        var rp = ScalePointsToFitCanvas(new List<Point>() { new Point(minX, minY) })[0];
               
        var pp= new Point(
                (p.X - rp.X) * scale + minX, // Scale X and apply offset
                (p.Y - rp.Y) * scale + minY  // Scale Y and apply offset
            );
        return pp;
    }
    private void DrawPoints(List<Point> points)
    {
        foreach (var p in points)
        {
            Ellipse e = new Ellipse()
            {
                Width = 5,
                Height = 5,
                Fill = Brushes.Blue,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
            };
            Canvas.SetLeft(e, p.X - e.Width / 2);
            Canvas.SetTop(e, p.Y - e.Height / 2);
            drawingCanvas.Children.Add(e);
        }        
    }
    private Point CreatePoint()
    {
        return new Point(minX + (r.NextDouble() * (maxX - minY)), minY + (r.NextDouble() * (maxY - minY)));
    }
    private void DrawLine(Point p1, Point p2)
    {
        // Create a new Line
        Line line = new Line
        {
            Stroke = Brushes.Red,
            StrokeThickness = 2,
            X1 = p1.X,                // Start at the left of the canvas
            Y1 = p1.Y,                // Start at the top of the canvas
            X2 = p2.X,      // End at the right of the canvas
            Y2 = p2.Y      // End at the bottom of the canvas
        };

        // Add the line to the canvas
        drawingCanvas.Children.Add(line);
    }
    private void DrawAllLines(List<Point> points)
    {
        for (int i = 1; i < points.Count; i++)
        {
            DrawLine(points[i - 1], points[i]);
        }
    }
    private void Draw()
    {
        // Get the size of the canvas
        double canvasWidth = drawingCanvas.ActualWidth;
        double canvasHeight = drawingCanvas.ActualHeight;

        // Clear previous drawing (if any)
        drawingCanvas.Children.Clear();
        var rp = ScalePointsToFitCanvas(new List<Point>() { new Point(minX,minY), new Point(maxX, maxY) });
        DrawRectangle(rp[0], rp[1]);
        var p = ScalePointsToFitCanvas(points);
        DrawPoints(p);
        DrawAllLines(p);
    }
    private bool WithinBox(Point p)
    {
        Rect boundingBox = new Rect(minX,minY,maxX,maxY);
        return boundingBox.Contains(p);
    }
    private void ButtonSave_Click(object sender, RoutedEventArgs e)
    {
        var rp = ScalePointsToFitCanvas(new List<Point>() { new Point(minX, minY), new Point(maxX, maxY) });
        Rect areaToSave = new Rect(rp[0], rp[1]);
        // Maak een RenderTargetBitmap om het geselecteerde gebied van de Canvas vast te leggen
        RenderTargetBitmap rtb = new RenderTargetBitmap(
            (int)areaToSave.Width,  // Breedte van het geselecteerde gebied
            (int)areaToSave.Height, // Hoogte van het geselecteerde gebied
            96, 96, // Resolutie van de afbeelding
            PixelFormats.Pbgra32);  // Pixel formaat

        // Maak een Visual om het geselecteerde gebied van de Canvas vast te leggen
        DrawingVisual drawingVisual = new DrawingVisual();
        using (DrawingContext dc = drawingVisual.RenderOpen())
        {
            // Vertaal de canvas-inhoud naar de juiste positie
            dc.DrawRectangle(new VisualBrush(drawingCanvas), null, new Rect(-areaToSave.X, -areaToSave.Y, drawingCanvas.ActualWidth, drawingCanvas.ActualHeight));
        }

        // Render de inhoud naar de RenderTargetBitmap
        rtb.Render(drawingVisual);

        // Maak een JpegBitmapEncoder om de afbeelding als JPG op te slaan
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        // Save the image to a file
        using (FileStream fs = new FileStream(@"C:\VisualStudioProjects\PG_cursus\data\test.jpg", FileMode.Create))
        {
            encoder.Save(fs);
        }

        MessageBox.Show("Canvas saved as JPG!");
    }

    private void drawingCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw();
    }

    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        //add circle at random
        points.Add(CreatePoint());
        Draw();
    }

    private void drawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Get the mouse click position
        Point clickPosition = e.GetPosition(drawingCanvas);
        Point p=ScalePointFromCanvas(clickPosition);
        if (WithinBox(p))
            points.Add(p);
       
        Draw();
    }

    private void drawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        points = new();
        drawingCanvas.Children.Clear();
        Draw();
    }
}