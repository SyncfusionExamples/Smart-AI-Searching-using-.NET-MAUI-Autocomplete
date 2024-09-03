using SmartAIAutocomplete.SmartAIAutocomplete.View;

namespace SmartAIAutocomplete
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Add the Syncfusion License in here
            MainPage = new SmartAIAutocompletePage();
        }
    }
}
