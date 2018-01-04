// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;
using OpenTK.Input;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.UI
{
    /// <summary>
    /// A type of <see cref="Playfield"/> specialized towards scrolling <see cref="DrawableHitObject"/>s.
    /// </summary>
    public class ScrollingPlayfield : Playfield
    {
        /// <summary>
        /// The default span of time visible by the length of the scrolling axes.
        /// This is clamped between <see cref="time_span_min"/> and <see cref="time_span_max"/>.
        /// </summary>
        private const double time_span_default = 1500;
        /// <summary>
        /// The minimum span of time that may be visible by the length of the scrolling axes.
        /// </summary>
        private const double time_span_min = 50;
        /// <summary>
        /// The maximum span of time that may be visible by the length of the scrolling axes.
        /// </summary>
        private const double time_span_max = 10000;
        /// <summary>
        /// The step increase/decrease of the span of time visible by the length of the scrolling axes.
        /// </summary>
        private const double time_span_step = 50;

        /// <summary>
        /// The span of time that is visible by the length of the scrolling axes.
        /// For example, only hit objects with start time less than or equal to 1000 will be visible with <see cref="VisibleTimeRange"/> = 1000.
        /// </summary>
        public readonly BindableDouble VisibleTimeRange = new BindableDouble(time_span_default)
        {
            Default = time_span_default,
            MinValue = time_span_min,
            MaxValue = time_span_max
        };

        /// <summary>
        /// The container that contains the <see cref="SpeedAdjustmentContainer"/>s and <see cref="DrawableHitObject"/>s.
        /// </summary>
        public new readonly ScrollingHitObjectContainer HitObjects;

        /// <summary>
        /// Creates a new <see cref="ScrollingPlayfield"/>.
        /// </summary>
        /// <param name="scrollingAxes">The axes on which <see cref="DrawableHitObject"/>s in this container should scroll.</param>
        /// <param name="customWidth">Whether we want our internal coordinate system to be scaled to a specified width</param>
        protected ScrollingPlayfield(ScrollingDirection direction, float? customWidth = null)
            : base(customWidth)
        {
            base.HitObjects = HitObjects = new ScrollingHitObjectContainer(direction) { RelativeSizeAxes = Axes.Both };
            HitObjects.TimeRange.BindTo(VisibleTimeRange);
        }

        private List<ScrollingPlayfield> nestedPlayfields;
        /// <summary>
        /// All the <see cref="ScrollingPlayfield"/>s nested inside this playfield.
        /// </summary>
        public IEnumerable<ScrollingPlayfield> NestedPlayfields => nestedPlayfields;

        /// <summary>
        /// Adds a <see cref="ScrollingPlayfield"/> to this playfield. The nested <see cref="ScrollingPlayfield"/>
        /// will be given all of the same speed adjustments as this playfield.
        /// </summary>
        /// <param name="otherPlayfield">The <see cref="ScrollingPlayfield"/> to add.</param>
        protected void AddNested(ScrollingPlayfield otherPlayfield)
        {
            if (nestedPlayfields == null)
                nestedPlayfields = new List<ScrollingPlayfield>();

            nestedPlayfields.Add(otherPlayfield);
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (state.Keyboard.ControlPressed)
            {
                switch (args.Key)
                {
                    case Key.Minus:
                        transformVisibleTimeRangeTo(VisibleTimeRange + time_span_step, 200, Easing.OutQuint);
                        break;
                    case Key.Plus:
                        transformVisibleTimeRangeTo(VisibleTimeRange - time_span_step, 200, Easing.OutQuint);
                        break;
                }
            }

            return false;
        }

        private void transformVisibleTimeRangeTo(double newTimeRange, double duration = 0, Easing easing = Easing.None)
        {
            this.TransformTo(this.PopulateTransform(new TransformVisibleTimeRange(), newTimeRange, duration, easing));
        }

        private class TransformVisibleTimeRange : Transform<double, ScrollingPlayfield>
        {
            private double valueAt(double time)
            {
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }

            public override string TargetMember => "VisibleTimeRange.Value";

            protected override void Apply(ScrollingPlayfield d, double time) => d.VisibleTimeRange.Value = valueAt(time);
            protected override void ReadIntoStartValue(ScrollingPlayfield d) => StartValue = d.VisibleTimeRange.Value;
        }
    }
}
