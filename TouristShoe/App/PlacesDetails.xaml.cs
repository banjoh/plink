using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using App.ViewModels;

namespace App
{
    public partial class PlacesDetails : PhoneApplicationPage
    {
        public PlacesDetails()
        {
            InitializeComponent();
        }

        public ItemViewModel Item { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Set data context containing the list of images
            if (Item != null) DataContext = Item;
        }
    }
}