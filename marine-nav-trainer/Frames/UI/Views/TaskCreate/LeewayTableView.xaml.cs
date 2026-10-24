using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace marine_nav_trainer.Navigation.UI.Views {
    public partial class LeewayTableView : UserControl {
        public LeewayTableView() {
            InitializeComponent();
        }

        public List<Leeway> GetEntries() =>
            Rows().Select(row => new Leeway {
                Key = row.Key,
                Name = row.Name,
                MinDeg = row.MinBox.Value ?? 0,
                MaxDeg = row.MaxBox.Value ?? 0,
                ValueDeg = row.ValueBox.Value ?? 0
            }).ToList();

        public void SetEntries(IEnumerable<Leeway> entries) {
            var loaded = entries.ToList();

            foreach (var row in Rows()) {
                var entry = loaded.FirstOrDefault(item => item.Key == row.Key);
                if (entry == null)
                    continue;

                row.MinBox.Value = entry.MinDeg;
                row.MaxBox.Value = entry.MaxDeg;
                row.ValueBox.Value = entry.ValueDeg;
            }
        }

        //private void RestoreDefaultsOnClick(object sender, RoutedEventArgs e) {
        //    SetEntries(LeewayEntry.CreateDefaults());
        //}

        // pola z XAML powiązane z kluczami JSON-a
        private IEnumerable<(string Key, string Name, NumberBox MinBox, NumberBox MaxBox, NumberBox ValueBox)> Rows() {
            yield return (Leeway.CloseHauledKey, "Bajdewind", CloseHauledMinBox, CloseHauledMaxBox, CloseHauledValueBox);
            yield return (Leeway.BeamReachKey, "Półwiatr", BeamReachMinBox, BeamReachMaxBox, BeamReachValueBox);
            yield return (Leeway.BroadReachKey, "Baksztag", BroadReachMinBox, BroadReachMaxBox, BroadReachValueBox);
            yield return (Leeway.RunningKey, "Fordewind", RunningMinBox, RunningMaxBox, RunningValueBox);
        }
    }
}
