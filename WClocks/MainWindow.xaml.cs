using System;
using System.IO;
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
        public const string APP_NAME = "WClocks";

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


        private WClockSet settings;
        readonly AutorunManager autorunManager;
        readonly DispatcherTimer mainTimer = new DispatcherTimer();
        public static readonly string UserAppFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        public static readonly string ApplicationFolder = System.IO.Path.Combine(UserAppFolder, APP_NAME);

        const double DefaultWindowWidth = 250f;
        const double DefaultWindowHeight = DefaultWindowWidth + IconsHeaderHeight;
        const int IconsHeaderHeight = 20;


        private string GetConfigPath()
        {
            string fileName = "wclock.xml";
#if DEBUG
            return fileName;
#endif
            return System.IO.Path.Combine(ApplicationFolder, "wclock.xml");
        }

        private void LoadSettings()
        {
            string configPath = GetConfigPath();
            try
            {
                settings = Serializer.SafeLoadFromXml<WClockSet>(configPath, null) ?? new WClockSet();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Launch error\n\nError load config from\n{configPath}\n\nDetails: {ex.Message}", APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SetLocationOptions(settings.Location);
            SetSizeOptions(settings.Size.ToString());
            SetShapeColor(settings.FaceColor.CurrentColor);

            var screenRect = new Rect(0, 0, SystemParameters.WorkArea.Width - this.Width, SystemParameters.WorkArea.Height - this.Height);
            if (screenRect.Contains(settings.Position))
            {
                this.Left = settings.Position.X;
                this.Top = settings.Position.Y;
            }
            else this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void SaveSettings()
        {
            string configPath = GetConfigPath();
            Serializer.SaveToXml(configPath, settings);
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings();
            InitClock();

            autorunManager = new AutorunManager(APP_NAME, ApplicationFolder);

            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
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
            int clockSideLenght = Convert.ToInt32(DefaultWindowWidth);
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

            SetShapeColor(settings.FaceColor.CurrentColor);
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
            UpdateShadowLine(ShadowLineSecond, LineSecond);
            UpdateShadowLine(ShadowLineMinute, LineMinute);
            UpdateShadowLine(ShadowLineHour, LineHour);
        }

        int shadowOffset = 3;
        private void UpdateShadowLine(Line shadowLine, Line parent)
        {
            int screenPosition = (this.Left > SystemParameters.WorkArea.Width / 2) ? 1 : -1;
            // 3 = Shadow offset from arrow, 180 - half of circle in degrees, source of light on the top of screen
            double parentAngle = Convert.ToDouble((parent.RenderTransform as RotateTransform)?.Angle);
            double shadowLineOffset = shadowOffset * Math.Round(Math.Sin(parentAngle / 180 * Math.PI), 6);

            shadowLine.X1 = parent.X1 + shadowLineOffset;
            shadowLine.Y1 = parent.Y1 - shadowLineOffset * screenPosition;
            shadowLine.X2 = parent.X2 + shadowLineOffset;
            shadowLine.Y2 = parent.Y2 - shadowLineOffset * screenPosition;
        }

        #region// Menu

        private void Clocks_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            itemAutorun.IsChecked = autorunManager.IsAutorunEnabled;
        }

        private MenuItem FindMenuItem(MenuItem ownerMenu, string tag)
        {
            return ownerMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => String.Equals(mi.Tag as String, tag, StringComparison.OrdinalIgnoreCase));
        }

        private void SetMenuItemCheckState(MenuItem ownerMenu, Func<MenuItem, bool> predicate, bool checkState = true)
        {
            foreach (MenuItem item in ownerMenu.Items)
            {
                // Reset all items
                item.IsChecked = !checkState;
                if (predicate(item))
                    item.IsChecked = checkState;
            }
        }

        private void SetMenuItemCheckState(MenuItem menuItem, bool checkState = true)
        {
            var ownerMenu = (MenuItem)menuItem.Parent;
            foreach (MenuItem item in ownerMenu.Items)
            {
                // Reset all items
                item.IsChecked = !checkState;
            }
            menuItem.IsChecked = checkState;
        }

        private string SetLocationOptions(string location)
        {
            // Find menu item by Tag, otherwise set Float window by default
            string newLocation = FindMenuItem(clocksLocationMenu, location)?.Tag as String ?? Locations.Float;
            SetMenuItemCheckState(clocksLocationMenu, (item) => String.Equals(item.Tag, newLocation));

            this.Topmost = String.Equals(location, Locations.TopMost, StringComparison.OrdinalIgnoreCase);
            this.ShowInTaskbar = String.Equals(location, Locations.Float, StringComparison.OrdinalIgnoreCase);
            if (String.Equals(location, Locations.Desktop, StringComparison.OrdinalIgnoreCase))
                WinExtern.SendWindowBack(Application.Current.MainWindow); // Place window at the back of screen

            return newLocation;
        }

        private void MenuLocation_Click(object sender, RoutedEventArgs e)
        {
            string validLocation = SetLocationOptions(((MenuItem)sender).Tag as String);
            settings.Location = validLocation;
            SaveSettings();
        }


        private void MenuPosition_Click(object sender, RoutedEventArgs e)
        {
            var positionScreen = new PositionWindow(this);
            positionScreen.ShowDialog();
            Point newPosition = (Point)positionScreen.GetPositionArgs().Object;

            this.Left = newPosition.X;
            this.Top = newPosition.Y;
        }

        private double GetSizedWindowHeight(int size) => Math.Round(DefaultWindowWidth * size / 100f + IconsHeaderHeight);
        private double GetSizedWindowWidth(int size) => Math.Round(DefaultWindowWidth * size / 100f);

        private int SetSizeOptions(string size)
        {
            double prevWidth = GetSizedWindowWidth(settings.Size);
            double prevHeight = GetSizedWindowHeight(settings.Size);

            // Find menu item by Tag, otherwise set 100 window by default
            string newSizeStr = FindMenuItem(clocksSizeMenu, size)?.Tag as String ?? "100";
            SetMenuItemCheckState(clocksSizeMenu, (item) => String.Equals(item.Tag, newSizeStr));
            int newSizeValue = Int32.Parse(newSizeStr);
            GridScaleValue = newSizeValue / 100f;

            // Set window to previous center
            this.Width = GetSizedWindowWidth(newSizeValue);
            this.Height = GetSizedWindowHeight(newSizeValue);
            if (Math.Abs(prevWidth - this.Width) > Double.Epsilon) this.Left += (prevWidth - this.Width) / 2;
            if (Math.Abs(prevHeight - this.Height) > Double.Epsilon) this.Top += (prevHeight - this.Height) / 2;

            return newSizeValue;
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

        private void SetShapeColor(System.Windows.Media.Color mediaColor)
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
            SetShapeColor(e.NewValue);
        }

        private void MenuColor_Click(object sender, RoutedEventArgs e)
        {
            SetShapeColor(GetUserDrawingColor((SolidColorBrush)pathEllipse.Fill));
        }

        private void MenuAutorun_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            bool autorun = autorunManager.IsAutorunEnabled;
            if (autorun) autorunManager.DisableAutorun();
            else autorunManager.EnableAutorun();

            menuItem.IsChecked = !(autorun);
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
