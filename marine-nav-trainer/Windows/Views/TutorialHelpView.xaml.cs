using System.Windows;

namespace marine_nav_trainer.Windows.Views {
    public record TutorialStep(int Number, string Title, string Description);

    public partial class TutorialHelpView : Window {
        private static TutorialHelpView? _instance;

        public TutorialHelpView() {
            InitializeComponent();

            StepsList.ItemsSource = new List<TutorialStep> {
                new(1, "Otwórz kreator zadania",
                    "Z menu „Zadania nawigacyjne” wybierz „Utwórz nowe zadanie”."),
                new(2, "Wybierz plik mapy",
                    "Kliknij „Wybierz mapę” i wskaż mapę w formacie .pdf."),
                new(3, "Dostosuj krawędzie i marginesy mapy",
                    "Kliknij „Krawędzie mapy”, przeciągnij linie kalibracyjne (Alt + LPM) na wewnętrzną stronę ramki siatki kartograficznej mapy i wpisz współrzędne φ/λ krawędzi. Bez tego współrzędne na mapie będą błędne."),
                new(4, "Ustaw wartości skrajne mapy",
                    "Uzupełnij skrajne wartości współrzędnych geograficznych (φ/λ) krawędzi. Bez tego współrzędne odczytywane na mapie będą błędne."),
                new(5, "Ustaw czas i deklinację",
                    "W „Deklinacja i czas” wybierz strefę czasową i datę rozpoczęcia rejsu, uzupełnij deklinację główną oraz w razie potrzeby deklinacje lokalne."),
                new(6, "Uzupełnij tabele dewiacji",
                    "W „Tabela dewiacji” wpisz dewiację dla kursów kompasowych."),
                new(7, "Uzupełnij tabele dryfu",
                    "W „Tabela dryfu” wpisz zakresy wiatru oraz odpowiadający im dryf."),
                new(8, "Napisz treść zadania",
                    "W „Opis zadania” wpisz treść zadania. Tekst obsługuje formatowanie Markdown. Aby zobaczyć sformatowany teskt nalerzy przełączyć podgląd"),
                new(9, "Skorzystaj z narzędzi nawigacyjnych",
                    "Trójkąt nawigacyjny, kroczek, linia pomocnicza ułatwiają pracę na mapie. Klawiszologia opisana została w oknie: Pomoc → Sterowanie."),
                new(10, "Użyj kalkulatorów",
                    "Dla bardziej złożonych obliczeń skorzystaj z gotowych kalkulatorów. Otwórz „Kalkulatory” (kurs, wiatr pozorny, trawers, pozycja obserwowana, przesunięcie LOP, pozycja zliczona)."),
                new(11, "Wstaw punkty na mapie",
                    "Z przybornika wybierz „Wstaw punkt” i kliknij miejsce na mapie."),
                new(12, "Dostosuj punkty",
                    "W „Lista punktów” uzupełnij: czas, log, popraw współrzędne punktu, jego kolejność albo usuń."),
                new(13, "Wyznacz linie kursu",
                    "Z przybornika wybierz „Linia kursu” i narysuj ją na mapie zaczynając od pozycji początkowej (Punktu nawigacyjnego), a kończąc w dowolnym już miejscu na mapie."),
                new(14, "Dostosuj linię kursu",
                    "W „Lista kursów” uzupełnij: kurs kompasowy, prędkość wiatr rzeczywisty oraz pozorny. Kąt drogi nad dnem oraz dystans uzupełniają się automatycznie."),
                new(15, "Zapisz zadanie",
                    "Kliknij „Zapisz zadanie”, zapisanie zadania nie spowoduje jego zamknięcia, więc bez obaw można dalej kontynuować.")
            };
        }

        public static void ShowSingle(Window owner) {
            if (_instance == null) {
                _instance = new TutorialHelpView { Owner = owner };
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
    }
}
