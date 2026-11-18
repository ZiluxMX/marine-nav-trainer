using System.Diagnostics;
using System.Windows;

namespace marine_nav_trainer.Windows.Views {
    public partial class ContactAndSupportView : Window {
        private const string MailAddress = "zilux@interia.pl";
        private const string CoffeeUrl = "https://buycoffee.to/zilux";

        private static ContactAndSupportView? _instance;

        public ContactAndSupportView() {
            InitializeComponent();
            MailText.Text = MailAddress;
        }

        public static void ShowSingle(Window owner) {
            if (_instance == null) {
                _instance = new ContactAndSupportView { Owner = owner };
                _instance.Show();
                return;
            }

            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            _instance = null;
        }

        private void MailButton_Click(object sender, RoutedEventArgs e) {
            OpenLink($"mailto:{MailAddress}?subject=Marine Nav Trainer");
        }

        private void CoffeeButton_Click(object sender, RoutedEventArgs e) {
            OpenLink(CoffeeUrl);
        }

        private static void OpenLink(string link) {
            try {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            catch (Exception) {
                MessageBox.Show($"Nie udało się otworzyć: {link}", "Kontakt i wsparcie");
            }
        }
    }
}
