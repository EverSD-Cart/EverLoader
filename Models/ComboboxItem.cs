namespace EverLoader.Models
{
    public class ComboboxItem
    {
        public ComboboxItem(string text, string value)
        {
            Value = value;
            Text = text;
        }
        public string Text { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
