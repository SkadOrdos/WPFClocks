using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        #region ShowHeader Dependency Property

        public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register("ShowHeader", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
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
            //return fileName;
#endif
            return System.IO.Path.Combine(ApplicationFolder, fileName);
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

            headerGrid.Visibility = new XBoolVisibilityConverter().Convert(settings.ShowHeader);

            var screenRect = new Rect(0, 0, SystemParameters.WorkArea.Width - this.Width, SystemParameters.WorkArea.Height - this.Height);
            if (screenRect.Contains(settings.Position))
            {
                SetWindowPosition(settings.Position);
            }
            else this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void SaveSettings()
        {
            UpdateSettings(sett => { });
        }

        private void UpdateSettings(Action<WClockSet> updateSettings)
        {
            string configPath = GetConfigPath();
            try
            {
                updateSettings?.Invoke(settings);
                Serializer.SaveToXml(configPath, settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings();

            autorunManager = new AutorunManager(APP_NAME, ApplicationFolder);

            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            this.MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WinExtern.DisableWindowSwitcher(Application.Current.MainWindow);
            // Init timer only after load window
            InitClock();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source is Image controlImage && controlImage.Tag != null)
                return;

            this.DragMove();
        }

        private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Update location
            settings.Position = new Point(Math.Max(0, Math.Min(this.Left, SystemParameters.WorkArea.Width - this.Width)),
                Math.Max(0, Math.Min(this.Top, SystemParameters.WorkArea.Height - this.Height)));
            this.Left = settings.Position.X;
            this.Top = settings.Position.Y;
            SetSizeOptions(settings.Size.ToString());
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void InitClock()
        {
            InitClockFace();

            mainTimer.Interval = TimeSpan.FromMilliseconds(125);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();

            UpdateClock(true);
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
        private void UpdateClock(bool force = false)
        {
            DateTime dt = DateTime.Now;
            if (dt.Second == lastTime.Second && !force)
                return;

            // 360 degrees in circle / 60 seconds - 60 parts in clock face * 1000 ms / TimerInterval
            //double arcCoef = 360 / (60 * 1000 / mainTimer.Interval.TotalMilliseconds);
            //double millisecondShift = arcCoef * dt.Millisecond / mainTimer.Interval.TotalMilliseconds;

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

        const int ShadowOffset = 3;
        private void UpdateShadowLine(Line shadowLine, Line parent)
        {
            double middleScreenX = Convert.ToDouble(SystemParameters.WorkArea.Width / 2);
            int screenPosition = ConvertNaN(this.Left) < middleScreenX ? -1 : 1;
            double horizontalOffset = ShadowOffset * (Math.Abs(ConvertNaN(this.Left) - middleScreenX) / middleScreenX);
            // 3 = Shadow offset from arrow, 180 - half of circle in degrees, source of light on the top of screen
            double parentAngle = ConvertNaN((parent.RenderTransform as RotateTransform)?.Angle);
            double displacementFactor = Math.Round(Math.Sin(parentAngle / 180 * Math.PI), 6);
            double shadowLineXOffset = ShadowOffset * displacementFactor;
            double shadowLineYOffset = horizontalOffset * displacementFactor;

            shadowLine.X1 = parent.X1 + shadowLineXOffset;
            shadowLine.Y1 = parent.Y1 - shadowLineYOffset * screenPosition;
            shadowLine.X2 = parent.X2 + shadowLineXOffset;
            shadowLine.Y2 = parent.Y2 - shadowLineYOffset * screenPosition;
        }

        private Point SetWindowPosition(Point newPosition)
        {
            this.Left = newPosition.X;
            this.Top = newPosition.Y;
            return newPosition;
        }

        #region// Menu

        private void Clocks_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            itemAutorun.IsChecked = autorunManager.IsAutorunEnabled;
            itemShowHeader.IsChecked = this.ShowHeader = settings.ShowHeader;
        }

        private MenuItem FindMenuItem(MenuItem ownerMenu, string tag)
        {
            return ownerMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => String.Equals(mi.Tag as String, tag, StringComparison.OrdinalIgnoreCase));
        }

        private void SetMenuItemCheckState(MenuItem ownerMenu, string itemTag, bool checkState = true)
        {
            foreach (MenuItem item in ownerMenu.Items)
            {
                // Reset all items
                item.IsChecked = !checkState;
                if (String.Equals(item.Tag?.ToString(), itemTag))
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


        private void MenuShowHeader_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            this.ShowHeader = settings.ShowHeader = !menuItem.IsChecked;
            headerGrid.Visibility = new XBoolVisibilityConverter().Convert(settings.ShowHeader);
            SaveSettings();
        }

        private string SetLocationOptions(string newLocation)
        {
            // Find menu item by Tag, otherwise set Float window by default
            string location = FindMenuItem(clocksLocationMenu, newLocation)?.Tag as String ?? Locations.Float;
            SetMenuItemCheckState(clocksLocationMenu, location);

            this.Topmost = String.Equals(location, Locations.TopMost, StringComparison.OrdinalIgnoreCase);
            this.ShowInTaskbar = String.Equals(location, Locations.Float, StringComparison.OrdinalIgnoreCase);
            if (String.Equals(location, Locations.Desktop, StringComparison.OrdinalIgnoreCase))
                WinExtern.SendWindowBack(Application.Current.MainWindow); // Place window at the back of screen

            return location;
        }

        private void MenuLocation_Click(object sender, RoutedEventArgs e)
        {
            string validLocation = SetLocationOptions(((MenuItem)sender).Tag as String);
            UpdateSettings(sett => sett.Location = validLocation);
        }


        private void MenuPosition_Click(object sender, RoutedEventArgs e)
        {
            var positionScreen = new PositionWindow(this);
            positionScreen.SetPosition += (ps, pArgs) => { SetWindowPosition(pArgs.NewPoint); };

            bool okResult = (positionScreen.ShowDialog() == true);
            Point newPosition = (Point)positionScreen.GetPositionArgs().Object;
            SetWindowPosition(newPosition);

            if (okResult)
            {
                UpdateSettings(sett => sett.Position = newPosition);
            }
        }

        private double GetSizedWindowHeight(double scale) => Math.Round(DefaultWindowWidth * scale + IconsHeaderHeight);
        private double GetSizedWindowWidth(double scale) => Math.Round(DefaultWindowWidth * scale);

        private int SetSizeOptions(string newSizeStr)
        {
            double prevWidth = GetSizedWindowWidth(settings.GetSizeScale);
            double prevHeight = GetSizedWindowHeight(settings.GetSizeScale);

            // Find menu item by Tag, otherwise set 100 window by default
            string sizeStr = FindMenuItem(clocksSizeMenu, newSizeStr)?.Tag as String ?? "100";
            SetMenuItemCheckState(clocksSizeMenu, sizeStr);
            int size = Int32.Parse(sizeStr);
            GridScaleValue = size / 100f;

            // Set window to previous center
            this.Width = GetSizedWindowWidth(GridScaleValue);
            this.Height = GetSizedWindowHeight(GridScaleValue);
            if (Math.Abs(prevWidth - this.Width) > Double.Epsilon) this.Left += (prevWidth - this.Width) / 2;
            if (Math.Abs(prevHeight - this.Height) > Double.Epsilon) this.Top += (prevHeight - this.Height) / 2;

            return size;
        }

        private void MenuSize_Click(object sender, RoutedEventArgs e)
        {
            int sizeValue = SetSizeOptions(((MenuItem)sender).Tag as String);
            UpdateSettings(sett => sett.Size = sizeValue);
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

            UpdateSettings(sett => sett.FaceColor.SetColor(mediaColor));
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
            bool newAutorunStatus = !(autorunManager.IsAutorunEnabled);
            autorunManager.SetAutorun(newAutorunStatus);

            menuItem.IsChecked = newAutorunStatus;
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateClock(true);
            this.UpdateLayout();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutScr = new About();
            aboutScr.WindowStartupLocation = WindowStartupLocation.Manual;
            aboutScr.Top = this.Top;
            aboutScr.Left = this.Left;
            aboutScr.ShowDialog();
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

        private double ConvertNaN(double? dVal)
        {
            return (dVal != null && !Double.IsNaN(dVal.Value)) ? dVal.Value : 0.0;
        }
    }
}
