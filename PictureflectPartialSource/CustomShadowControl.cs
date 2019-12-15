//Based originally on the version in the Windows Community Toolkit, which uses the MIT license. However, it has been heavily modified and shares little code, so acknowledgement probably isn't required.

using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Effects;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;

namespace PictureflectPartialSource {

    [TemplatePart(Name = PartShadow, Type = typeof(FrameworkElement))]
    public class CustomShadowControl : ContentControl {
        private const string PartShadow = "ShadowElement";

        public static readonly DependencyProperty IsShadowEnabledProperty = DependencyProperty.Register(nameof(IsShadowEnabled), typeof(bool), typeof(CustomShadowControl), new PropertyMetadata(true, OnIsShadowEnabledChanged));

        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(0.0, OnShadowBlurRadiusChanged));

        public static readonly DependencyProperty SpreadRadiusProperty = DependencyProperty.Register(nameof(SpreadRadius), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(0.0, OnShadowSpreadRadiusChanged));

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(Color), typeof(CustomShadowControl), new PropertyMetadata(Colors.Black, OnShadowColorChanged));

        public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(1.0, OnShadowOpacityChanged));

        public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(0.0, OnShadowOffsetChanged));

        public static readonly DependencyProperty OffsetYProperty = DependencyProperty.Register(nameof(OffsetY), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(0.0, OnShadowOffsetChanged));

        public static readonly DependencyProperty OffsetZProperty = DependencyProperty.Register(nameof(OffsetZ), typeof(double), typeof(CustomShadowControl), new PropertyMetadata(0.0, OnShadowOffsetChanged));

        public static readonly DependencyProperty IsMaskedProperty = DependencyProperty.Register(nameof(IsMasked), typeof(bool), typeof(CustomShadowControl), new PropertyMetadata(true, OnIsMaskedChanged));

        public bool IsShadowEnabled {
            get {
                return (bool)GetValue(IsShadowEnabledProperty);
            }
            set {
                SetValue(IsShadowEnabledProperty, value);
            }
        }

        CompositionBrush mask = null;
        public CompositionBrush Mask {
            get {
                return mask;
            }
            set {
                if(mask != value) {
                    mask = value;
                    UpdateShadowMask();
                }
            }
        }

        public double BlurRadius {
            get {
                return (double)GetValue(BlurRadiusProperty);
            }
            set {
                SetValue(BlurRadiusProperty, value);
            }
        }

        public double SpreadRadius {
            get {
                return (double)GetValue(SpreadRadiusProperty);
            }
            set {
                SetValue(SpreadRadiusProperty, value);
            }
        }

        public Color Color {
            get {
                return (Color)GetValue(ColorProperty);
            }
            set {
                SetValue(ColorProperty, value);
            }
        }

        public double ShadowOpacity {
            get {
                return (double)GetValue(ShadowOpacityProperty);
            }
            set {
                SetValue(ShadowOpacityProperty, value);
            }
        }

        public double OffsetX {
            get {
                return (double)GetValue(OffsetXProperty);
            }
            set {
                SetValue(OffsetXProperty, value);
            }
        }

        public double OffsetY {
            get {
                return (double)GetValue(OffsetYProperty);
            }
            set {
                SetValue(OffsetYProperty, value);
            }
        }

        public double OffsetZ {
            get {
                return (double)GetValue(OffsetZProperty);
            }
            set {
                SetValue(OffsetZProperty, value);
            }
        }

        public bool IsMasked {
            get { 
                return (bool)GetValue(IsMaskedProperty);
            }
            set { 
                SetValue(IsMaskedProperty, value);
            }
        }

        private static void OnIsShadowEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                if (panel.IsShadowEnabled) {
                    panel.CreateShadowVisualIfNeeded();
                } else {
                    panel.RemoveShadowVisual();
                }
            }
        }

        private static void OnShadowBlurRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                if(panel.BlurRadius < 0) {
                    panel.BlurRadius = 0;
                    return;
                }
                panel.UpdateShadowEffect();
            }
        }

        private static void OnShadowSpreadRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                if(panel.SpreadRadius < 0) {
                    panel.SpreadRadius = 0;
                    return;
                }
                panel.UpdateShadowEffect();
            }
        }

        private static void OnShadowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel && !Equals(e.NewValue, e.OldValue)) { //It appears that this check for different colors is needed.
                panel.UpdateShadowEffect();
            }
        }

        private static void OnShadowOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                if(panel.ShadowOpacity < 0.0) {
                    panel.ShadowOpacity = 0.0;
                    return;
                }else if (panel.ShadowOpacity > 1.0) {
                    panel.ShadowOpacity = 1.0;
                    return;
                }
                panel.UpdateShadowVisualProperties();
            }
        }

        private static void OnShadowOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                panel.UpdateShadowSizeAndOffset();
            }
        }

        private static void OnIsMaskedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is CustomShadowControl panel) {
                panel.UpdateShadowEffect();
            }
        }

        static readonly string effectSourceName = "AlphaMask";
        static readonly string transformEffectName = "TransformEffect";
        static readonly string transformEffectMatrixName = "TransformEffect.TransformMatrix";
        Compositor compositor = null;
        SpriteVisual shadowVisual = null;
        FrameworkElement hostElement = null;
        bool isUsingDropShadow = false;

        public CustomShadowControl() {
            this.DefaultStyleKey = typeof(CustomShadowControl);
        }

        protected override void OnApplyTemplate() {
            base.OnApplyTemplate();
            hostElement = GetTemplateChild(PartShadow) as FrameworkElement;
            CreateShadowVisualIfNeeded();
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var size = base.ArrangeOverride(finalSize);
            UpdateShadowOffset();
            return size;
        }

        protected override void OnContentChanged(object oldContent, object newContent) {
            base.OnContentChanged(oldContent, newContent);
            if (oldContent != null) {
                if (oldContent is FrameworkElement oldElement) {
                    oldElement.SizeChanged -= OnSizeChanged;
                }
            }
            if (newContent != null) {
                if (newContent is FrameworkElement newElement) {
                    newElement.SizeChanged += OnSizeChanged;
                }
            }
            cachedContentSize = null;
            bool created = CreateShadowVisualIfNeeded();
            if (!created) {
                UpdateShadowEffect();
            }
        }

        Size? cachedContentSize = null;
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            cachedContentSize = e.NewSize;
            bool created = CreateShadowVisualIfNeeded();
            if (!created) {
                UpdateShadowSizeAndOffset();
            }
        }

        bool ShouldUseMask {
            get {
                return IsMasked && (Content is Image || Content is Shape || Content is TextBlock);
            }
        }

        void RemoveShadowVisual() {
            if(hostElement == null || shadowVisual == null) {
                return;
            }
            ElementCompositionPreview.SetElementChildVisual(hostElement, null);
            shadowVisual.Dispose();
            shadowVisual = null;
        }

        bool CreateShadowVisualIfNeeded() { //Returns false if shadow is not created (either already exists or not needed). If created it calls UpdateShadowVisualProperties and UpdateShadowEffect. Note that the shadow is not created if the content size is zero.
            if (!IsShadowEnabled || hostElement == null || shadowVisual != null) {
                return false;
            }
            var size = GetShadowVisualTargetSize();
            if(size.X <= 0 || size.Y <= 0) {
                return false;
            }
            if (compositor == null) {
                compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            }
            shadowVisual = compositor.CreateSpriteVisual();
            ElementCompositionPreview.SetElementChildVisual(hostElement, shadowVisual);
            UpdateShadowEffect();
            return true;
        }

        void UpdateShadowEffect() { //This calls UpdateShadowVisualProperties, UpdateShadowMask and UpdateShadowSizeAndOffset.
            if (compositor == null || shadowVisual == null) {
                return;
            }
            if (ShouldUseMask) {
                isUsingDropShadow = false;
                if (shadowVisual.Shadow != null) {
                    var oldShadow = shadowVisual.Shadow;
                    shadowVisual.Shadow = null;
                    oldShadow.Dispose();
                }
                IGraphicsEffectSource alphaMaskSource = new Transform2DEffect() { Name = transformEffectName, TransformMatrix = Matrix3x2.Identity, Source = new CompositionEffectSourceParameter(effectSourceName) };
                if (SpreadRadius > 0) {
                    var spreadEffectBlur = new GaussianBlurEffect() { BlurAmount = (float)SpreadRadius / 3.0f, Source = alphaMaskSource };
                    alphaMaskSource = new ColorMatrixEffect() { ClampOutput = true, ColorMatrix = new Matrix5x4() { M44 = 10.0f }, Source = spreadEffectBlur }; //The number 10 seems about right
                }
                if (BlurRadius > 0) {
                    alphaMaskSource = new GaussianBlurEffect() { BlurAmount = (float)BlurRadius / 3.0f, Source = alphaMaskSource };
                }
                var colorSourceEffect = new ColorSourceEffect() { Color = Color };
                var finalEffect = new AlphaMaskEffect { AlphaMask = alphaMaskSource, Source = colorSourceEffect };
                CompositionEffectFactory effectFactory;
                try {
                    effectFactory = compositor.CreateEffectFactory(finalEffect, new string[] { transformEffectMatrixName });
                } catch (Exception) {
                    return;
                }
                var shadowEffectBrush = effectFactory.CreateBrush();
                var oldBrush = shadowVisual.Brush;
                shadowVisual.Brush = shadowEffectBrush;
                oldBrush?.Dispose();
                UpdateShadowMask();
            } else {
                isUsingDropShadow = true;
                if (shadowVisual.Brush != null) {
                    var oldBrush = shadowVisual.Brush;
                    shadowVisual.Brush = null;
                    oldBrush.Dispose();
                }
                var dropShadow = shadowVisual.Shadow as DropShadow;
                if (dropShadow == null) {
                    dropShadow = compositor.CreateDropShadow();
                    var oldShadow = shadowVisual.Shadow;
                    shadowVisual.Shadow = dropShadow;
                    oldShadow?.Dispose();
                }
                dropShadow.BlurRadius = (float)Math.Max(BlurRadius, 0.0);
                dropShadow.Color = Color;
            }
            UpdateShadowVisualProperties();
            UpdateShadowSizeAndOffset();
        }

        void UpdateShadowVisualProperties() { //This updates only the properties of the visual itself.
            if (shadowVisual == null) {
                return;
            }
            shadowVisual.Opacity = (float)Math.Max(0.0, Math.Min(1.0, ShadowOpacity));
        }

        private void UpdateShadowMask() {
            if(shadowVisual == null || !(shadowVisual.Brush is CompositionEffectBrush effectBrush)) {
                return;
            }
            CompositionBrush newMask = null;
            if (IsMasked && Mask != null) {
                newMask = Mask;
            }else if (IsMasked && Content != null) {
                if (Content is Image) {
                    newMask = ((Image)Content).GetAlphaMask();
                } else if (Content is Shape) {
                    newMask = ((Shape)Content).GetAlphaMask();
                } else if (Content is TextBlock) {
                    newMask = ((TextBlock)Content).GetAlphaMask();
                }
            }
            try {
                effectBrush.SetSourceParameter(effectSourceName, newMask);
            } catch (Exception) { }
        }

        void SetShadowSizeToZero() {
            if (shadowVisual == null) {
                return;
            }
            shadowVisual.Size = new Vector2(0, 0);
            shadowVisual.Offset = new Vector3(0, 0, 0);
            if(!(shadowVisual.Brush is CompositionEffectBrush effectBrush)) {
                return;
            }
            try {
                effectBrush.Properties.InsertMatrix3x2(transformEffectMatrixName, Matrix3x2.Identity);
            } catch (Exception) { }
        }

        private Vector2 GetShadowVisualTargetSize() { //Must return 0 if no content or shadow disabled, else must return the content size.
            Vector2 size = new Vector2(0, 0);
            if (!IsShadowEnabled || !(Content is FrameworkElement element)) {
                return size;
            }
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ActualSize")) {
                size = element.ActualSize;
            } else if (cachedContentSize != null) {
                size = new Vector2((float)cachedContentSize.Value.Width, (float)cachedContentSize.Value.Height);
            } else {
                size = new Vector2((float)element.ActualWidth, (float)element.ActualHeight); //Due to a bug, this is sometimes wrong
            }
            return size;
        }

        private void UpdateShadowSizeAndOffset() {
            if (shadowVisual == null) {
                return;
            }
            Vector2 newSize = GetShadowVisualTargetSize();
            if (newSize.X <= 0 || newSize.Y <= 0) {
                SetShadowSizeToZero();
                return;
            }
            float extraHalfLength = GetShadowVisualExtraHalfLength();
            Vector2 newSizeExtra = new Vector2(newSize.X + extraHalfLength * 2.0f, newSize.Y + extraHalfLength * 2.0f);
            shadowVisual.Size = newSizeExtra;
            UpdateShadowOffset();
            if (!(shadowVisual.Brush is CompositionEffectBrush effectBrush)) {
                return;
            }
            var enlargeScale = Math.Min(newSizeExtra.X / newSize.X, newSizeExtra.Y / newSize.Y);
            var preTranslation = new Vector2(-(newSizeExtra.X - newSize.X * enlargeScale) / 2.0f, -(newSizeExtra.Y - newSize.Y * enlargeScale) / 2.0f);
            var postTranslation = new Vector2(extraHalfLength, extraHalfLength);
            var transformMatrix = Matrix3x2.Multiply(Matrix3x2.CreateTranslation(preTranslation), Matrix3x2.Multiply(Matrix3x2.CreateScale(1.0f / enlargeScale), Matrix3x2.CreateTranslation(postTranslation)));
            try {
                effectBrush.Properties.InsertMatrix3x2(transformEffectMatrixName, transformMatrix);
            } catch (Exception) { }
        }

        private Vector3 GetContentOffset() { //Must return 0 if no content or shadow disabled, else must return the offset.
            Vector3 offset = new Vector3(0, 0, 0);
            if (!IsShadowEnabled || !(Content is UIElement element) || hostElement == null) {
                return offset;
            }
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ActualOffset")) {
                offset = Vector3.Subtract(element.ActualOffset, hostElement.ActualOffset);
            } else {
                try {
                    var transform = element.TransformToVisual(hostElement);
                    var point = transform.TransformPoint(new Point(0, 0));
                    offset = new Vector3((float)point.X, (float)point.Y, 0);
                } catch (Exception) { }
            }
            return offset;
        }

        void UpdateShadowOffset() {
            if (shadowVisual == null) {
                return;
            }
            Vector3 extraOffset = GetContentOffset();
            float extraHalfLength = GetShadowVisualExtraHalfLength();
            var newOffset = new Vector3((float)OffsetX - extraHalfLength + extraOffset.X, (float)OffsetY - extraHalfLength + extraOffset.Y, (float)OffsetZ + extraOffset.Z);
            if (shadowVisual.Offset != newOffset) {
                shadowVisual.Offset = newOffset;
            }
        }

        float GetShadowVisualExtraHalfLength() {
            if (shadowVisual == null) {
                return 0.0f;
            }
            return (float)(Math.Max(SpreadRadius, 0.0) + (!isUsingDropShadow ? Math.Max(BlurRadius, 0.0) : 0.0));
        }

    }

}