using System;
using System.Linq;
using System.Collections.Generic;

#if __UNIFIED__
using UIKit;
using Foundation;
using CoreGraphics;

#else
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;

using System.Drawing;
using CGRect = global::System.Drawing.RectangleF;
using CGPoint = global::System.Drawing.PointF;
using CGSize = global::System.Drawing.SizeF;
using nfloat = global::System.Single;
using nint = global::System.Int32;
using nuint = global::System.UInt32;
#endif

namespace PatridgeDev
{
   public class RatingConfig
   {
      public UIImage EmptyImage { get; set; }

      /// <summary>
      /// Image shown for the current average rating, when a rating is provided.
      /// </summary>
      public UIImage FilledImage { get; set; }

      /// <summary>
      /// Image shown when a user has chosen a rating.
      /// </summary>
      public UIImage ChosenImage { get; set; }

      /// <summary>
      /// Padding between the rendering of the items. The default is none (0f).
      /// </summary>
      public float ItemPadding { get; set; }

      /// <summary>
      /// Number of rating items in the scale (5 stars, 10 cows, whatever). The default is five.
      /// </summary>
      public int ScaleSize { get; set; }

      public RatingConfig(UIImage emptyImage, UIImage filledImage, UIImage chosenImage)
      {
         EmptyImage = emptyImage;
         FilledImage = filledImage;
         ChosenImage = chosenImage;
         ScaleSize = 5;
         ItemPadding = 0f;
      }
   }

   class RatingItemView : UIView
   {
      UIImageView EmptyImageView;
      UIView FilledImageViewObscuringWrapper;
      UIView FilledImageViewSizingHolder;
      UIImageView FilledImageView;
      UIImageView SelectedImageView;
      private float _PercentFilled = 0f;

      public int StarRating { get; set; }

      public float PercentFilled
      {
         get {
            return _PercentFilled;
         }
         set {
            _PercentFilled = value;
            SetNeedsLayout();
         }
      }

      private bool _Chosen = false;

      public bool Chosen
      {
         get {
            return _Chosen;
         }
         set {
            _Chosen = value;
            SetNeedsLayout();
         }
      }

      PDRatingView _parentView;

      public RatingItemView(UIImage emptyImage, UIImage filledImage, UIImage chosenImage, PDRatingView parentView)
      {
         _parentView = parentView;
         UserInteractionEnabled = false;
         MultipleTouchEnabled = true;

         AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
         EmptyImageView = new UIImageView(emptyImage) {
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
            UserInteractionEnabled = false,
         };

         Add(EmptyImageView);
         FilledImageView = new UIImageView(filledImage) {
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
            UserInteractionEnabled = false,
         };
         FilledImageViewSizingHolder = new UIView() {
            UserInteractionEnabled = false,
         };
         FilledImageViewSizingHolder.Add(FilledImageView);
         FilledImageViewObscuringWrapper = new UIView() {
            ClipsToBounds = true,
            UserInteractionEnabled = false,
         };
         FilledImageViewObscuringWrapper.Add(FilledImageViewSizingHolder);
         Add(FilledImageViewObscuringWrapper);
         SelectedImageView = new UIImageView(chosenImage) {
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
            UserInteractionEnabled = false,
         };
         Add(SelectedImageView);
         PercentFilled = 0;
      }

      public override void LayoutSubviews()
      {
         base.LayoutSubviews();

         // Layout everything to their appropriate sizes.
         SelectedImageView.Frame = new CGRect(CGPoint.Empty, Bounds.Size);
         EmptyImageView.Frame = new CGRect(CGPoint.Empty, Bounds.Size);
         FilledImageViewObscuringWrapper.Frame = new CGRect(CGPoint.Empty, Bounds.Size);
         FilledImageViewSizingHolder.Frame = new CGRect(CGPoint.Empty, FilledImageViewObscuringWrapper.Bounds.Size);
         FilledImageView.Frame = new CGRect(CGPoint.Empty, FilledImageViewSizingHolder.Bounds.Size);

         // Hide/Show things accordingly.
         if (Chosen)
         {
            // If selected, only show that view completely and hide the rest.
            SelectedImageView.SetAspectFitAsNeeded(UIViewContentMode.Center);
            EmptyImageView.Hidden = true;
            FilledImageViewObscuringWrapper.Hidden = true;
            SelectedImageView.Hidden = false;
         }
         else
         {
            // If not selected, hide selected, show empty and portion of filled.
            EmptyImageView.SetAspectFitAsNeeded(UIViewContentMode.Center);
            if (PercentFilled > 0f)
            {
               FilledImageView.SetAspectFitAsNeeded(UIViewContentMode.Center);
               if (PercentFilled < 1f)
               {
                  // Obscure a portion of the filled image based on the percent.
                  nfloat revealWidth;
                  if (FilledImageView.Image.Size.Width < FilledImageView.Bounds.Width)
                  {
                     revealWidth = ((FilledImageView.Bounds.Width - FilledImageView.Image.Size.Width) / 2f) + (FilledImageView.Image.Size.Width * PercentFilled);
                  }
                  else
                  {
                     revealWidth = FilledImageView.Bounds.Width * PercentFilled;
                  }
                  FilledImageViewObscuringWrapper.Frame = new CGRect(FilledImageViewSizingHolder.Frame.Location, new CGSize(revealWidth, FilledImageViewSizingHolder.Frame.Height));
               }
               FilledImageViewObscuringWrapper.Hidden = false;
            }
            else
            {
               FilledImageViewObscuringWrapper.Hidden = true;
            }
            SelectedImageView.Hidden = true;
            EmptyImageView.Hidden = false;
         }
      }
   }

   public class PDRatingView : UIView
   {
      public event EventHandler ChosenRatingChanged;

      readonly RatingConfig StarRatingConfig;
      private decimal _AverageRating = 0m;

      public decimal AverageRating
      {
         get {
            return _AverageRating;
         }
         set {
            _AverageRating = value;
            SetNeedsLayout();
         }
      }

      private int? _ChosenRating = null;

      public int? ChosenRating
      {
         get {
            return _ChosenRating;
         }
         set {
            _ChosenRating = value;

            if (ChosenRatingChanged != null)
               ChosenRatingChanged.Invoke(this, null);
            
            SetNeedsLayout();
         }
      }

      List<RatingItemView> StarViews;

      public PDRatingView(CGRect frame, RatingConfig config) : this(frame, config, 0m)
      {
      }

      public PDRatingView(CGRect frame, RatingConfig config, decimal averageRating) : this(config, averageRating)
      {
         Frame = frame;
      }

      public PDRatingView(RatingConfig config, decimal averageRating) : this(config)
      {
         AverageRating = averageRating;
      }

      public PDRatingView(RatingConfig config)
      {
         UserInteractionEnabled = true;
         MultipleTouchEnabled = true;
         ExclusiveTouch = true;
         StarRatingConfig = config;
         StarViews = new List<RatingItemView>();
         Enumerable.Range(0, StarRatingConfig.ScaleSize).ToList().ForEach(i =>
         {
            int starRating = i + 1;
            RatingItemView starView = new RatingItemView(StarRatingConfig.EmptyImage, StarRatingConfig.FilledImage,
                                         StarRatingConfig.ChosenImage, this);
            StarViews.Add(starView);
            StarViews[i].StarRating = starRating;
            Add(starView);
         });
      }

      public override void LayoutSubviews()
      {
         nfloat starAreaWidth = Bounds.Width / StarRatingConfig.ScaleSize;
         nfloat starAreaHeight = Bounds.Height - (2 * StarRatingConfig.ItemPadding);
         nfloat starImageMaxWidth = starAreaWidth - (2 * StarRatingConfig.ItemPadding);
         nfloat starImageMaxHeight = starAreaHeight - (2 * StarRatingConfig.ItemPadding);
         CGSize starAreaScaled = StarRatingConfig.EmptyImage.Size.ScaleProportional(starImageMaxWidth, starImageMaxHeight);
         nfloat top = (Bounds.Height / 2f) - (starAreaScaled.Height / 2f);
         int i = 0;
         StarViews.ForEach(v =>
         {
            nfloat x = (i * starAreaWidth) + StarRatingConfig.ItemPadding;
            v.Frame = new CGRect(new CGPoint(x, top), starAreaScaled);

            // Choose between showing a chosen rating and the average rating.
            if (ChosenRating != null)
            {
               v.Chosen = ChosenRating.Value > i;
               v.PercentFilled = 0f;
            }
            else
            {
               v.Chosen = false;
               float percentFilled = (AverageRating - 1) > i ? 1.0f : (float)(AverageRating - i);
               v.PercentFilled = percentFilled;
            }
            i += 1;
         });

         base.LayoutSubviews();
      }

      public override void TouchesBegan(NSSet touches, UIEvent evt)
      {
         foreach (UITouch obj in touches)
         {
            var loc = obj.LocationInView(this);
            foreach (var starview in StarViews)
            {
               if (starview.Frame.Contains(loc))
               {
                  if (ChosenRating != starview.StarRating)
                     ChosenRating = starview.StarRating;
                  return;
               }
            }
         }
      }

      public override void TouchesMoved(NSSet touches, UIEvent evt)
      {
         foreach (UITouch obj in touches)
         {
            var loc = obj.LocationInView(this);
            foreach (var starview in StarViews)
            {
               if (starview.Frame.Contains(loc))
               {
                  if (ChosenRating != starview.StarRating)
                     ChosenRating = starview.StarRating;
                  return;
               }
            }
         }

         if (ChosenRating != 0 && StarViews.Count > 0)
         {
            foreach (UITouch obj in touches)
            {
               if (obj.LocationInView(StarViews[0]).X < 0)
               {
                  ChosenRating = 0;
                  return;
               }
            }
         }
      }
   }
}