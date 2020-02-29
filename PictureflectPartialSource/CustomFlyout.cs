using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PictureflectPartialSource {

    public class CustomFlyout : Flyout {

        //This works around a bug in Windows 10 1903 and 1909 which causes a flyout to draw its shadow on top of a sub-flyout at the same z-index. This class should be used where a nested flyout is needed instead of the Flyout class.
        protected override Control CreatePresenter() {
            var presenter = base.CreatePresenter();
            if (presenter == null || !ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "Translation")) {
                return presenter;
            }
            UIElement targetParent = Target;
            while (targetParent != null && !(targetParent is FlyoutPresenter)) {
                targetParent = VisualTreeHelper.GetParent(targetParent) as UIElement;
            }
            if (targetParent is FlyoutPresenter && targetParent.Translation.Z == presenter.Translation.Z) {
                var translation = presenter.Translation;
                translation.Z += 1;
                presenter.Translation = translation;
            }
            return presenter;
        }

    }

}
