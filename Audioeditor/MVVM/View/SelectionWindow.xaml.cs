using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Audioeditor.MVVM.View
{
    public partial class SelectionWindow : UserControl
    {
        public SelectionWindow()
        {
            InitializeComponent();
        }

        public event EventHandler<string> SelectionMade;

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedValue = (string)listBox.SelectedItem;
            SelectionMade?.Invoke(this, selectedValue);
            
            var parentWindow = FindVisualParent<Window>(this);
            parentWindow.Close();
        }

        public void SetItems(List<string> items)
        {
            listBox.ItemsSource = items;
        }
        
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            var parent = parentObject as T;
            return parent ?? FindVisualParent<T>(parentObject);
        }
    }
}