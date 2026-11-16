using marine_nav_trainer.Navigation.Models;
using System.Windows;
using System.Windows.Controls;

namespace marine_nav_trainer.Windows.Views {
    // opis zadania przy porównaniu - przy różnicach oba opisy, jeden pod drugim

    public partial class TaskCompareDescriptionView : UserControl {
        public TaskCompareDescriptionView() {
            InitializeComponent();
        }

        public void SetMeta(TaskMeta reference, TaskMeta checkedMeta) {
            Visibility checkedVisibility = AreEqual(reference, checkedMeta) ? Visibility.Collapsed : Visibility.Visible;
            ReferenceCaption.Visibility = checkedVisibility;
            CheckedSection.Visibility = checkedVisibility;

            ReferenceTitleText.Text = reference.Title;
            ReferenceAuthorText.Text = reference.Author;
            ReferenceContentPreview.Markdown = reference.ContentMd;

            CheckedTitleText.Text = checkedMeta.Title;
            CheckedAuthorText.Text = checkedMeta.Author;
            CheckedContentPreview.Markdown = checkedMeta.ContentMd;

            ReferenceMemoBox.Text = reference.CommentMd;
            ReferenceMemoSection.Visibility = GetMemoVisibility(reference);

            CheckedMemoBox.Text = checkedMeta.CommentMd;
            CheckedMemoSection.Visibility = GetMemoVisibility(checkedMeta);
        }

        private static Visibility GetMemoVisibility(TaskMeta meta) {
            return string.IsNullOrWhiteSpace(meta.CommentMd) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static bool AreEqual(TaskMeta reference, TaskMeta checkedMeta) {
            return reference.Title == checkedMeta.Title
                && reference.Author == checkedMeta.Author
                && reference.ContentMd == checkedMeta.ContentMd;
        }
    }
}
