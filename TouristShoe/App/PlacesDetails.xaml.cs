using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace App
{
    public partial class PlacesDetails : PhoneApplicationPage
    {
        public PlacesDetails()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Set data context containing the list of images
        }
    }
}