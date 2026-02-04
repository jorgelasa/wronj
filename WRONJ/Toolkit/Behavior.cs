
namespace WRONJ.Toolkit
{
    public class PositiveDoubleValidationBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnEntryTextChanged;
        }
        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnEntryTextChanged;
        }
        void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            bool isValid = string.IsNullOrWhiteSpace(args.NewTextValue) || Double.TryParse(args.NewTextValue, out double result) && result >= 0;
            if (!isValid && args.NewTextValue != args.OldTextValue) ((Entry)sender).Text = args.OldTextValue;
        }
    }
    public class IntValidationBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnEntryTextChanged;
        }
        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnEntryTextChanged;
        }
        void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            bool isValid = string.IsNullOrWhiteSpace(args.NewTextValue) || Int32.TryParse(args.NewTextValue, out int result) && result >= 0; ;
            if (!isValid && args.NewTextValue != args.OldTextValue) ((Entry)sender).Text = args.OldTextValue;
        }
    }
}
