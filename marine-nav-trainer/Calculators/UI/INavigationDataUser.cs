using marine_nav_trainer.Navigation.Models;

namespace marine_nav_trainer.Calculators.UI {
    // zakładka prosi widok nadrzędny o tabele i deklinację z ramek zadania
    public interface INavigationDataUser {
        event EventHandler<NavigationData>? NavigationDataRequested;
    }
}
