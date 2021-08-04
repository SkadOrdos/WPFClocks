using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WClocks
{
    /// <summary>
    /// Interaction logic for PositionWindow.xaml
    /// </summary>
    public partial class PositionWindow : Window
    {
        Window owner;

        Point currentWindowPosition;
        public PositionWindow(Window ownerWindow)
        {
            owner = ownerWindow;
            InitializeComponent();
            FillButtons();

            this.Topmost = true;
            currentWindowPosition = new Point(owner.Left, owner.Top);
        }

        private void FillButtons()
        {
            int gridSize = 3;
            for (int i = 1; i <= gridSize; i++)
            {
                positionGrid.ColumnDefinitions.Add(new ColumnDefinition());
                positionGrid.RowDefinitions.Add(new RowDefinition());
            }

            // Rotate image from top left by clock arrow
            var rotateSideMap = new[] { -1, 0, 1 };
            int gridLenght = gridSize * gridSize;
            int centerIndex = Convert.ToInt32(gridLenght / 2);
            for (int i = 0; i < gridLenght; i++)
            {
                int colIndex = i % gridSize;
                int rowIndex = (int)(i / gridSize);

                Button button = new Button();
                button.Background = this.Background;
                button.Click += PositionButton_Click;
                button.Tag = new Point(rowIndex, colIndex); // Write button location in tag

                // Set transform to all buttons except center
                string imageUri = @"icons/arrow-position.png";
                Transform imageTransform = null;
                if (i != centerIndex)
                {
                    int colNumber = (colIndex + 1);
                    int rowNumber = (rowIndex + 1);
                    // Need start with rotation -45 (360/8) degrees
                    int rotateSide = (colIndex == 1) ? (int)Math.Pow(rowIndex, 2) : rotateSideMap[colIndex];
                    int rotateAngle = 45 * rowNumber * rotateSide;

                    imageTransform = new RotateTransform(rotateAngle, 0, 0);
                }
                else
                {
                    imageUri = @"icons/arrow-x.png";
                }

                // Add images to button content
                button.Content = GetButtonImageContent(imageUri, imageTransform);

                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, colIndex);
                positionGrid.Children.Add(button);
            }
        }

        private FrameworkElement GetButtonImageContent(string imageUri, Transform imageTransform)
        {
            Image contentImage = new Image();
            contentImage.Stretch = Stretch.Fill;
            contentImage.Source = new BitmapImage(new Uri(imageUri, UriKind.Relative));

            StackPanel stackPnl = new StackPanel();
            stackPnl.Orientation = Orientation.Horizontal;
            stackPnl.HorizontalAlignment = HorizontalAlignment.Center;

            stackPnl.RenderTransform = imageTransform;
            stackPnl.RenderTransformOrigin = new Point(0.5, 0.5);
            stackPnl.Margin = new Thickness(0);
            stackPnl.Children.Add(contentImage);
            return stackPnl;
        }

        private void PositionButton_Click(object sender, RoutedEventArgs e)
        {
            Rect displayArea = System.Windows.SystemParameters.WorkArea;
            var selectedPosition = (Point)(sender as Button).Tag;

            // Convert button array index to screen position like TopLeft, TopCenter, TopRight...
            double newX = selectedPosition.Y * (displayArea.Width / 2) - selectedPosition.Y * (owner.Width / 2);
            double newY = selectedPosition.X * (displayArea.Height / 2) - selectedPosition.X * (owner.Height / 2);
            // Apply powition to owner window
            owner.Left = (int)newX;
            owner.Top = (int)newY;
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public ObjectArgs GetPositionArgs()
        {
            if (this.DialogResult == true)
                return new ObjectArgs(new Point(owner.Left, owner.Top));
            return new ObjectArgs(currentWindowPosition);
        }

    }
}
