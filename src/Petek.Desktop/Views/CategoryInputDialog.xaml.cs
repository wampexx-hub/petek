using System.Windows;
using System.Windows.Input;

namespace Petek.Desktop.Views;

public partial class CategoryInputDialog : Window
{
    public string CategoryName => CategoryNameBox.Text;

    public CategoryInputDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => CategoryNameBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CategoryNameBox.Text))
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CategoryNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            Cancel_Click(sender, e);
        }
    }
}
