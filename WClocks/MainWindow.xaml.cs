using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WClocks
{
    public class Locations
    {
        public const string Desktop = "Desktop";
        public const string TopMost = "TopMost";
        public const string Float = "Float";
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Arrows Dependency Property

        public static readonly DependencyProperty AngleSecondProperty = DependencyProperty.Register("AngleSecond", typeof(double), typeof(MainWindow), new UIPropertyMetadata(default(double)));
        public static readonly DependencyProperty AngleMinuteProperty = DependencyProperty.Register("AngleMinute", typeof(double), typeof(MainWindow), new UIPropertyMetadata(default(double)));
        public static readonly DependencyProperty AngleHourProperty = DependencyProperty.Register("AngleHour", typeof(double), typeof(MainWindow), new UIPropertyMetadata(default(double)));

        public double AngleSecond
        {
            get { return (double)GetValue(AngleSecondProperty); }
            set { SetValue(AngleSecondProperty, value); }
        }

        public double AngleMinute
        {
            get { return (double)GetValue(AngleMinuteProperty); }
            set { SetValue(AngleMinuteProperty, value); }
        }

        public double AngleHour
        {
            get { return (double)GetValue(AngleHourProperty); }
            set { SetValue(AngleHourProperty, value); }
        }

        #endregion

        #region GridScaleValue Dependency Property

        public static readonly DependencyProperty GridScaleValueProperty = DependencyProperty.Register("GridScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0));

        public double GridScaleValue
        {
            get { return (double)GetValue(GridScaleValueProperty); }
            set { SetValue(GridScaleValueProperty, value); }
        }

        #endregion


        private void LoadSettings()
        {
            settings = Serializer.SafeLoadFromXml<WClockSet>(config, null) ?? new WClockSet();

            SetLocationOptions(settings.Location);
            SetSizeOptions(settings.Size.ToString());
            SetElementColor(settings.FaceColor.CurrentColor);
            if (settings.IsScreenPosition)
            {
                this.Left = settings.Position.X;
                this.Top = settings.Position.Y;
            }
            else this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void SaveSettings()
        {
            Serializer.SaveToXml(config, settings);
        }

        private WClockSet settings;
        private DispatcherTimer mainTimer = new DispatcherTimer();
        readonly string config = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wclock.xml");

        double DefaultWindowWidth => 250f;
        double DefaultWindowHeight => 270f;
        int IconsHeaderHeight => 20;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitClock();

            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            LoadSettings();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WinExtern.DisableWindowSwitcher(Application.Current.MainWindow);
        }

        private void MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source is Image controlImage && controlImage.Tag != null)
                return;

            this.DragMove();
            // Update location
            settings.Position = new Point(this.Left, this.Top);
            SaveSettings();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void InitClock()
        {
            InitClockFace();
            UpdateClock();

            mainTimer.Interval = TimeSpan.FromMilliseconds(100);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();
        }

        private void InitClockFace()
        {
            int clockSideLenght = Convert.ToInt32(Width);
            int xCenter = clockSideLenght / 2; // Center of clocks grid
            int yCenter = clockSideLenght / 2; // Center of clocks grid
            int yStartPoint = clockSideLenght / 25; // 1/25 of side lenght
            int yEndPoint = yStartPoint + (int)(clockSideLenght / 12); // Line lenght = 1/12 of side lenght

            for (int i = 1; i <= 60; i++)
            {
                Line linePath = new Line();
                linePath.StrokeThickness = 1;
                linePath.SnapsToDevicePixels = true;
                linePath.RenderTransform = new RotateTransform(6 * i, xCenter, yCenter); // Line every 5 minute
                linePath.X1 = xCenter;
                linePath.Y1 = yStartPoint;

                int modYEndPoint = yEndPoint;
                if (i % 5 == 0)
                {
                    modYEndPoint += 2; // More lenght and bold every hour
                    linePath.StrokeThickness = 2;
                }
                if (i % 15 == 0) modYEndPoint += 3; // Even more lenght every 3 hour =)

                linePath.X2 = xCenter;
                linePath.Y2 = modYEndPoint;

                Panel.SetZIndex(linePath, 0);
                this.clocksGrid.Children.Add(linePath);
            }
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        DateTime lastTime;
        private void UpdateClock()
        {
            DateTime dt = DateTime.Now;
            if (dt.Second == lastTime.Second)
                return;

            AngleSecond = 6 * (dt.Second);
            AngleMinute = 6 * dt.Minute + AngleSecond / 60;
            AngleHour = 30 * (dt.Hour % 12) + AngleMinute / 12;
            UpdateShadows();
            lastTime = dt;
        }

        private void UpdateShadows()
        {
            UpdateShadowLine(ShadowLineSecond, LineSecond, AngleSecond);
            UpdateShadowLine(ShadowLineMinute, LineMinute, AngleMinute);
            UpdateShadowLine(ShadowLineHour, LineHour, AngleHour);
        }

        int shadowOffset = 3;
        private void UpdateShadowLine(Line shadowLine, Line parent, double parentAngle)
        {
            int screenPosition = (this.Left > SystemParameters.WorkArea.Width / 2) ? 1 : -1;
            // 3 = Shadow offset from arrow, 180 - half of circle in degrees, source of light on the top
            double shadowLineOffset = shadowOffset * (Math.Sin(parentAngle / 180 * Math.PI));

            shadowLine.X1 = parent.X1 + shadowLineOffset;
            shadowLine.Y1 = parent.Y1 - shadowLineOffset * screenPosition;
            shadowLine.X2 = parent.X2 + shadowLineOffset;
            shadowLine.Y2 = parent.Y2 - shadowLineOffset * screenPosition;
        }

        #region// Menu

        private MenuItem FindMenuItem(MenuItem ownerMenu, string tag)
        {
            return ownerMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => String.Equals(mi.Tag as String, tag, StringComparison.OrdinalIgnoreCase));
        }

        private void SetMenuCheckState(MenuItem menuItem)
        {
            var ownerMenu = (MenuItem)menuItem.Parent;
            foreach (MenuItem item in ownerMenu.Items)
            {
                // Reset all items
                item.IsChecked = false;
            }
            menuItem.IsChecked = true;
        }

        private string SetLocationOptions(string location)
        {
            // Find menu item by Tag, otherwise set Float window by default
            var foundMenu = FindMenuItem(clocksLocationMenu, location) ?? itemLocationFloat;
            SetMenuCheckState(foundMenu);

            this.Topmost = String.Equals(location, Locations.TopMost, StringComparison.OrdinalIgnoreCase);
            this.ShowInTaskbar = String.Equals(location, Locations.Float, StringComparison.OrdinalIgnoreCase);
            if (String.Equals(location, Locations.Desktop, StringComparison.OrdinalIgnoreCase))
                WinExtern.SendWindowBack(Application.Current.MainWindow); // Place window at the back of screen

            return foundMenu.Tag as String;
        }

        private void MenuLocation_Click(object sender, RoutedEventArgs e)
        {
            string validLocation = SetLocationOptions(((MenuItem)sender).Tag as String);
            settings.Location = validLocation;
            SaveSettings();
        }


        private int SetSizeOptions(string size)
        {
            // Find menu item by Tag, otherwise set 100 window by default
            var foundMenu = FindMenuItem(clocksSizeMenu, size) ?? itemSizeDefault;
            SetMenuCheckState(foundMenu);
            int sizeValue = Int32.Parse(foundMenu.Tag as String);

            GridScaleValue = sizeValue / 100f;
            this.Width = DefaultWindowWidth * GridScaleValue;
            this.Height = DefaultWindowWidth * GridScaleValue + IconsHeaderHeight;

            return sizeValue;
        }

        private void MenuSize_Click(object sender, RoutedEventArgs e)
        {
            int sizeValue = SetSizeOptions(((MenuItem)sender).Tag as String);
            settings.Size = sizeValue;
            SaveSettings();
        }


        private Color GetUserDrawingColor(SolidColorBrush brush)
        {
            var mediaColor = brush.Color;

            var colorDialog = new Samples.ColorPicker.ColorPickerDialog();
            colorDialog.StartingColor = mediaColor;
            colorDialog.SelectedColorChanged += ColorDialog_SelectedColorChanged;

            if (colorDialog.ShowDialog() == true)
                mediaColor = colorDialog.SelectedColor;

            colorDialog.SelectedColorChanged -= ColorDialog_SelectedColorChanged;
            return mediaColor;
        }

        private void SetElementColor(System.Windows.Media.Color mediaColor)
        {
            var userColorBrush = new SolidColorBrush(mediaColor);
            foreach (var shapeElement in clocksGrid.Children.OfType<Shape>())
            {
                if (shapeElement.Tag == null)
                    shapeElement.Fill = shapeElement.Stroke = userColorBrush;
            }

            settings.FaceColor.SetColor(mediaColor);
            SaveSettings();
        }

        private void ColorDialog_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            SetElementColor(e.NewValue);
        }

        private void MenuColor_Click(object sender, RoutedEventArgs e)
        {
            SetElementColor(GetUserDrawingColor((SolidColorBrush)pathEllipse.Fill));
        }


        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateClock();
            this.UpdateLayout();
        }

        private void MenuClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region// Header buttons

        private void ImageSettings_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            clocksContextMenu.IsOpen = true;
        }

        private void ImageHide_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.ShowInTaskbar)
                this.WindowState = WindowState.Minimized;
        }

        private void ImageClose_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
