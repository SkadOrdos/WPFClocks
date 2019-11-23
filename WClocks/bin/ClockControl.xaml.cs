﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WClocks
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ClockControl : UserControl
    {
        public double DefaultWidth = 300;
        public double DefaultHeight = 200;

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


        private DispatcherTimer mainTimer = new DispatcherTimer();
        public ClockControl() : base()
        {
            InitializeComponent();
            this.DataContext = this;
            InitClockFace();

            mainTimer.Interval = TimeSpan.FromMilliseconds(100);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();

            this.SizeChanged += MainGrid_SizeChanged;
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
                linePath.Stroke = new SolidColorBrush(Colors.WhiteSmoke);
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
            //int screenPosition = (this.Left > SystemParameters.WorkArea.Width / 2) ? 1 : -1;
            // 3 = Shadow offset from arrow, 180 - half of circle in degrees, source of light on the top
            double shadowLineOffset = shadowOffset * (Math.Sin(parentAngle / 180 * Math.PI));

            shadowLine.X1 = parent.X1 + shadowLineOffset;
            shadowLine.Y1 = parent.Y1 - shadowLineOffset * 1;
            shadowLine.X2 = parent.X2 + shadowLineOffset;
            shadowLine.Y2 = parent.Y2 - shadowLineOffset * 1;
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

        private void SetLocationOptions(string location)
        {
            // Find menu item by Tag, otherwise set Float window by default
            var foundMenu = FindMenuItem(clocksLocationMenu, location) ?? itemLocationFloat;
            SetMenuCheckState(foundMenu);
            //settings.Location = foundMenu.Tag as String;

            //this.Topmost = String.Equals(location, Locations.TopMost, StringComparison.OrdinalIgnoreCase);
            //this.ShowInTaskbar = String.Equals(location, Locations.Float, StringComparison.OrdinalIgnoreCase);
            if (String.Equals(location, Locations.Desktop, StringComparison.OrdinalIgnoreCase))
                WinExtern.SendWindowBack(Application.Current.MainWindow); // Place window at the back of screen
        }

        private void MenuLocation_Click(object sender, RoutedEventArgs e)
        {
            SetLocationOptions(((MenuItem)sender).Tag as String);
            //SaveSettings();
        }


        private void SetSizeOptions(string size)
        {
            // Find menu item by Tag, otherwise set 100 window by default
            var foundMenu = FindMenuItem(clocksSizeMenu, size) ?? itemSizeDefault;
            SetMenuCheckState(foundMenu);
            int sizeValue = Int32.Parse(foundMenu.Tag as String);

            //this.clocksGrid.Height = this.clocksGrid.Width = defaultWindowWidth * settings.Size / 100;
            this.Width = DefaultWidth * sizeValue / 100;
            this.Height = DefaultHeight * sizeValue / 100;
        }

        private void MenuSize_Click(object sender, RoutedEventArgs e)
        {
            SetSizeOptions(((MenuItem)sender).Tag as String);
            //SaveSettings();
        }


        private Color GetUserDrawingColor(Brush brush)
        {
            var mediaColor = ((SolidColorBrush)brush).Color;

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

            //settings.FaceColor.SetColor(mediaColor);
            //SaveSettings();
        }

        private void ColorDialog_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            SetElementColor(e.NewValue);
        }

        private void MenuColor_Click(object sender, RoutedEventArgs e)
        {
            SetElementColor(GetUserDrawingColor(pathEllipse.Fill));
        }


        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateClock();
            this.UpdateLayout();
        }

        private void MenuClose_Click(object sender, RoutedEventArgs e)
        {
            //this.pa.Close();
        }

        #endregion


        #region ScaleValue Dependency Property

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(ClockControl), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            ClockControl mainWindow = o as ClockControl;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ClockControl mainWindow = o as ClockControl;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {

        }

        public double ScaleValue
        {
            get { return (double)GetValue(ScaleValueProperty); }
            set { SetValue(ScaleValueProperty, value); }
        }

        #endregion

        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            //CalculateScale();
        }

        private void CalculateScale()
        {
            double yScale = ActualHeight / DefaultWidth;
            double xScale = ActualWidth / DefaultHeight;
            double value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(this, value);
        }
    }
}